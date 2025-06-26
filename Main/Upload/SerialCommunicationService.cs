using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Ports;
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
using System.Linq;
using System.Collections.Generic;
using static Main.Upload.Hl7Result;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// 串口通信服务实现类
    /// 实现HL7通信服务接口，使用串口进行通信
    /// </summary>
    public class SerialCommunicationService : BaseCommunicationService
    {
        private SerialPort _serialPort;
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly int _dataBits;
        private readonly StopBits _stopBits;
        private readonly Parity _parity;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public SerialCommunicationService() : base()
        {
            // 从配置中获取串口参数
            _portName = UploadConfig.Instance.SerialPortName; 
            _baudRate = int.Parse(UploadConfig.Instance.BaudRate);
            _dataBits = int.Parse(UploadConfig.Instance.DataBit);
            
            // 解析停止位
            switch (UploadConfig.Instance.StopBit)
            {
                case "1":
                    _stopBits = StopBits.One;
                    break;
                case "1.5":
                    _stopBits = StopBits.OnePointFive;
                    break;
                case "2":
                    _stopBits = StopBits.Two;
                    break;
                default:
                    _stopBits = StopBits.One;
                    break;
            }
            
            // 解析校验位
            switch (UploadConfig.Instance.OddEven)
            {
                case "0":
                    _parity = Parity.None;
                    break;
                case "1":
                    _parity = Parity.Odd;
                    break;
                case "2":
                    _parity = Parity.Even;
                    break;
                case "3":
                    _parity = Parity.Mark;
                    break;
                case "4":
                    _parity = Parity.Space;
                    break;
                default:
                    _parity = Parity.None;
                    break;
            }
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

                    _serialPort = new SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits)
                    {
                        ReadTimeout = _timeout,
                        WriteTimeout = _timeout
                    };

                    _serialPort.Open();
                    _isConnected = true;
                    
                    // 启动接收循环
                    _receiveLoopCts = new CancellationTokenSource();
                    _receiveLoopTask = Task.Run(() => ReceiveMessagesLoop(_receiveLoopCts.Token));
                    
                    Log.Information($"已成功连接到串口: {_portName}");
                    return true;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    Log.Error(ex, $"连接串口失败: {_portName}");
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
                    // 取消接收循环
                    if (_receiveLoopCts != null)
                    {
                        _receiveLoopCts.Cancel();
                        try
                        {
                            _receiveLoopTask.Wait(1000); // 等待接收循环结束
                        }
                        catch { /* 忽略任务取消异常 */ }
                        _receiveLoopCts.Dispose();
                        _receiveLoopCts = null;
                    }
                    
                    // 清空所有等待的响应
                    foreach (var tcs in _pendingResponses.Values)
                    {
                        tcs.TrySetCanceled();
                    }
                    _pendingResponses.Clear();
                    
                    if (_serialPort != null && _serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        _serialPort.Dispose();
                        _serialPort = null;
                    }

                    _isConnected = false;
                    Log.Information("已断开串口连接");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "断开串口连接时发生错误");
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
                    if (_serialPort == null || !_serialPort.IsOpen)
                    {
                        // 串口未打开，等待一段时间后重试
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
                    
                    while (!endFound && !cancellationToken.IsCancellationRequested)
                    {
                        if (_serialPort.BytesToRead > 0)
                        {
                            byte currentByte = (byte)_serialPort.ReadByte();
                            
                            if (!startFound)
                            {
                                if (currentByte == MLLPProtocol.VT)
                                {
                                    startFound = true;
                                }
                                continue;
                            }
                            
                            memoryStream.WriteByte(currentByte);
                            
                            if (previousByte == MLLPProtocol.FS && currentByte == MLLPProtocol.CR)
                            {
                                endFound = true;
                                break;
                            }
                            
                            previousByte = currentByte;
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
                catch (OperationCanceledException)
                {
                    // 操作被取消，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "接收HL7消息循环中发生错误");
                    
                    // 发生错误，等待一段时间后继续
                    try
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// 发送字节数组到串口
        /// </summary>
        /// <param name="bytes">要发送的字节数组</param>
        /// <returns>异步任务</returns>
        protected override Task SendBytesAsync(byte[] bytes)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                throw new InvalidOperationException("串口未打开");
            }
            
            _serialPort.Write(bytes, 0, bytes.Length);
            return Task.CompletedTask; // 串口写入是同步的，返回已完成的任务
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <returns>连接是否正常</returns>
        public override bool IsConnected()
        {
            lock (_lockObject)
            {
                return _isConnected && _serialPort != null && _serialPort.IsOpen;
            }
        }
    }
} 