using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Model;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;
using Serilog;
using static Main.Upload.Hl7Result;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// TCP通信服务实现类
    /// 实现HL7通信服务接口，使用TCP连接进行通信
    /// </summary>
    public class TcpCommunicationService : BaseCommunicationService
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private readonly string _serverAddress;
        private readonly int _serverPort;

        // 心跳检测相关变量
        private Timer _heartbeatTimer;
        private const int HeartbeatInterval = 5000; // 心跳检测间隔5秒

        /// <summary>
        /// 构造函数
        /// </summary>
        public TcpCommunicationService()
            : base()
        {
            // 从配置中获取TCP连接参数
            _serverAddress = UploadConfig.Instance.ServiceIP;
            _serverPort = int.Parse(UploadConfig.Instance.ServicePort);
        }

        /// <summary>
        /// 连接到LIS服务器
        /// </summary>
        /// <returns>连接是否成功</returns>
        public override bool Connect()
        {
            lock (_lockObject)
            {
                try
                {
                    if (_isConnected)
                    {
                        return true;
                    }

                    _tcpClient = new TcpClient();

                    // 设置连接超时
                    var connectTask = _tcpClient.ConnectAsync(_serverAddress, _serverPort);
                    if (!connectTask.Wait(_timeout))
                    {
                        throw new TimeoutException(
                            $"连接到服务器 {_serverAddress}:{_serverPort} 超时"
                        );
                    }

                    _networkStream = _tcpClient.GetStream();
                    _networkStream.ReadTimeout = _timeout;
                    _networkStream.WriteTimeout = _timeout;

                    _isConnected = true;

                    // 启动接收循环
                    _receiveLoopCts = new CancellationTokenSource();
                    _receiveLoopTask = Task.Run(() => ReceiveMessagesLoop(_receiveLoopCts.Token));

                    // 启动心跳检测
                    StartHeartbeat();

                    Log.Information($"已成功连接到服务器: {_serverAddress}:{_serverPort}");

                    // 通知HL7Helper连接成功
                    HL7Helper.Instance.NotifyConnectionState(
                        true,
                        $"已成功连接到服务器: {_serverAddress}:{_serverPort}"
                    );

                    return true;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    Log.Error(ex, $"连接服务器失败: {_serverAddress}:{_serverPort}");

                    // 通知HL7Helper连接失败
                    HL7Helper.Instance.NotifyConnectionState(
                        false,
                        $"连接服务器失败: {_serverAddress}:{_serverPort}"
                    );

                    return false;
                }
            }
        }

        /// <summary>
        /// 断开与LIS服务器的连接
        /// </summary>
        public override void Disconnect()
        {
            lock (_lockObject)
            {
                try
                {
                    // 停止心跳检测
                    StopHeartbeat();

                    // 取消接收循环
                    if (_receiveLoopCts != null)
                    {
                        _receiveLoopCts.Cancel();
                        try
                        {
                            _receiveLoopTask.Wait(1000); // 等待接收循环结束
                        }
                        catch
                        { /* 忽略任务取消异常 */
                        }
                        _receiveLoopCts.Dispose();
                        _receiveLoopCts = null;
                    }

                    // 清空所有等待的响应
                    foreach (var tcs in _pendingResponses.Values)
                    {
                        tcs.TrySetCanceled();
                    }
                    _pendingResponses.Clear();

                    if (_networkStream != null)
                    {
                        _networkStream.Close();
                        _networkStream.Dispose();
                        _networkStream = null;
                    }

                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                        _tcpClient.Dispose();
                        _tcpClient = null;
                    }

                    _isConnected = false;
                    Log.Information("已断开服务器连接");

                    // 通知HL7Helper连接已断开
                    HL7Helper.Instance.NotifyConnectionState(false, "已手动断开服务器连接");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "断开服务器连接时发生错误");
                }
            }
        }

        /// <summary>
        /// 启动心跳检测
        /// </summary>
        private void StartHeartbeat()
        {
            StopHeartbeat(); // 确保之前的心跳已停止

            _heartbeatTimer = new Timer(
                CheckConnectionStatus,
                null,
                HeartbeatInterval,
                HeartbeatInterval
            );

            Log.Information("已启动TCP连接心跳检测");
        }

        /// <summary>
        /// 停止心跳检测
        /// </summary>
        private void StopHeartbeat()
        {
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Dispose();
                _heartbeatTimer = null;
                Log.Information("已停止TCP连接心跳检测");
            }
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <param name="state">定时器状态参数</param>
        private void CheckConnectionStatus(object state)
        {
            if (_tcpClient == null || _networkStream == null)
            {
                HandleDisconnection("TCP客户端或网络流为空");
                return;
            }

            try
            {
                // 检查连接状态的方法一：检查Socket属性
                if (!_tcpClient.Connected)
                {
                    HandleDisconnection("TCP客户端连接已断开");
                    return;
                }

                // 检查连接状态的方法二：尝试发送/接收0字节数据
                // 此方法可以检测网络连接是否真正可用
                try
                {
                    if (
                        _tcpClient.Client.Poll(1, SelectMode.SelectRead)
                        && _tcpClient.Available == 0
                    )
                    {
                        // 如果Poll返回true且Available为0，表示连接已关闭
                        HandleDisconnection("TCP连接已关闭（Poll检测）");
                        return;
                    }
                }
                catch (SocketException se)
                {
                    HandleDisconnection($"Socket异常: {se.Message}");
                    return;
                }

                // 如果执行到这里，说明连接仍然正常
                if (!_isConnected)
                {
                    // 如果内部状态为断开但实际连接正常，更新状态
                    Log.Information("检测到TCP连接已恢复");
                    _isConnected = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "检查TCP连接状态时发生错误");
                HandleDisconnection($"检查连接时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理连接断开
        /// </summary>
        /// <param name="reason">断开原因</param>
        private void HandleDisconnection(string reason)
        {
            if (_isConnected)
            {
                Log.Information($"检测到服务器连接已断开: {reason}");

                _isConnected = false;

                // 通知HL7Helper连接已断开
                HL7Helper.Instance.NotifyConnectionState(false, $"服务器连接已断开: {reason}");

                // 清理资源
                try
                {
                    if (_networkStream != null)
                    {
                        _networkStream.Close();
                        _networkStream.Dispose();
                        _networkStream = null;
                    }

                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                        _tcpClient.Dispose();
                        _tcpClient = null;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "清理资源时发生错误");
                }
            }
        }

        /// <summary>
        /// 消息接收循环，持续监听并处理接收到的消息
        /// </summary>
        private async Task ReceiveMessagesLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isConnected)
            {
                try
                {
                    if (_networkStream == null || !_tcpClient.Connected)
                    {
                        Log.Information("连接断开~");
                        // TCP连接已断开，等待一段时间后重试
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    // 接收并处理MLLP消息
                    var memoryStream = new MemoryStream();
                    bool startFound = false;
                    bool endFound = false;
                    byte previousByte = 0;

                    // 设置超时时间，但不强制每个消息都必须在此时间内完成
                    var readStartTime = DateTime.Now;

                    byte[] buffer = new byte[1024];

                    while (
                        !endFound
                        && !cancellationToken.IsCancellationRequested
                        && _networkStream != null
                    )
                    {
                        if (_tcpClient.Available > 0 || _networkStream.DataAvailable)
                        {
                            int bytesRead = await _networkStream.ReadAsync(
                                buffer,
                                0,
                                buffer.Length,
                                cancellationToken
                            );

                            if (bytesRead == 0)
                            {
                                // 连接已关闭
                                Log.Warning("TCP连接已关闭");
                                HandleDisconnection("接收消息时连接关闭");
                                break;
                            }

                            for (int i = 0; i < bytesRead; i++)
                            {
                                byte currentByte = buffer[i];

                                if (!startFound)
                                {
                                    if (currentByte == MLLPProtocol.VT)
                                    {
                                        startFound = true;
                                    }
                                    continue;
                                }

                                memoryStream.WriteByte(currentByte);

                                if (
                                    previousByte == MLLPProtocol.FS
                                    && currentByte == MLLPProtocol.CR
                                )
                                {
                                    endFound = true;
                                    break;
                                }

                                previousByte = currentByte;
                            }
                        }
                        else
                        {
                            // 如果没有数据，短暂等待后继续检查
                            await Task.Delay(10, cancellationToken);

                            // 如果超过30秒都没有接收到任何数据，重置状态，重新开始接收
                            if (startFound && (DateTime.Now - readStartTime).TotalSeconds > 30)
                            {
                                Log.Warning("接收消息超过30秒未完成，重置接收状态");
                                startFound = false;
                                memoryStream.SetLength(0);
                                readStartTime = DateTime.Now;
                            }
                        }
                    }

                    if (endFound)
                    {
                        try
                        {
                            memoryStream.SetLength(memoryStream.Length - 2); // 移除FS和CR
                            string response = GetEncoding().GetString(memoryStream.ToArray());
                            Log.Information($"接收到HL7响应: {response}");

                            // 解析消息
                            IMessage message = _parser.Parse(response);

                            // 处理响应消息
                            ProcessReceivedMessage(message);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "处理接收到的HL7消息时出错");
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    // 操作被取消，退出循环
                    Log.Information(ex, "操作被取消，退出循环");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Information(ex, "接收HL7消息循环中发生错误");

                    // 发生错误，等待一段时间后继续
                    try
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (OperationCanceledException ex2)
                    {
                        Log.Information(ex2, "操作被取消，退出循环2");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 发送字节数组到TCP连接
        /// </summary>
        /// <param name="bytes">要发送的字节数组</param>
        /// <returns>异步任务</returns>
        protected override async Task SendBytesAsync(byte[] bytes)
        {
            if (_networkStream == null || !_tcpClient.Connected)
            {
                throw new InvalidOperationException("TCP连接未打开");
            }

            try
            {
                await _networkStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "发送数据时发生错误");
                HandleDisconnection($"发送数据时发生错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <returns>连接是否正常</returns>
        public override bool IsConnected()
        {
          
                // 增加更严格的连接检测
                if (!_isConnected || _tcpClient == null || !_tcpClient.Connected)
                {
                    return false;
                }

                // 尝试更深层次的检测
                try
                {
                    return !(
                        _tcpClient.Client.Poll(1, SelectMode.SelectRead)
                        && _tcpClient.Available == 0
                    );
                }
                catch
                {
                    return false;
            }
        }
    }
}
