using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Services;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;

namespace FluorescenceFullAutomatic.UploadModule.Upload
{
    /// <summary>
    /// TCPͨ�ŷ���ʵ����
    /// ʵ��HL7ͨ�ŷ���ӿڣ�ʹ��TCP���ӽ���ͨ��
    /// </summary>
    public class TcpCommunicationService : BaseCommunicationService
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private readonly string _serverAddress;
        private readonly int _serverPort;

        // ���������ر���
        private Timer _heartbeatTimer;
        private const int HeartbeatInterval = 5000; // ���������5��
     
        private readonly ILogService logService;

        public override event ConnectionStatusChangedHandler ConnectionSucceeded;
        public override event ConnectionStatusChangedHandler ConnectionClosed;
        public override event ConnectionErrorHandler ConnectionFailed;

        /// <summary>
        /// ���캯��
        /// </summary>
        public TcpCommunicationService(ILogService logService   )
            : base(logService)
        {
            this.logService = logService;
            // �������л�ȡTCP���Ӳ���
            _serverAddress = UploadConfig.Instance.ServiceIP;
            _serverPort = int.Parse(UploadConfig.Instance.ServicePort);
        }

        /// <summary>
        /// ���ӵ�LIS������
        /// </summary>
        /// <returns>�����Ƿ�ɹ�</returns>
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

                    // �������ӳ�ʱ
                    var connectTask = _tcpClient.ConnectAsync(_serverAddress, _serverPort);
                    if (!connectTask.Wait(_timeout))
                    {
                        throw new TimeoutException(
                            $"���ӵ������� {_serverAddress}:{_serverPort} ��ʱ"
                        );
                    }

                    _networkStream = _tcpClient.GetStream();
                    _networkStream.ReadTimeout = _timeout;
                    _networkStream.WriteTimeout = _timeout;

                    _isConnected = true;

                    // ��������ѭ��
                    _receiveLoopCts = new CancellationTokenSource();
                    _receiveLoopTask = Task.Run(() => ReceiveMessagesLoop(_receiveLoopCts.Token));

                    // �����������
                    StartHeartbeat();

                    logService.Info($"�ѳɹ����ӵ�������: {_serverAddress}:{_serverPort}");

                    ConnectionSucceeded?.Invoke(true,
                        $"�ѳɹ����ӵ�������: {_serverAddress}:{_serverPort}");
                    //// ֪ͨHL7Helper���ӳɹ�
                    //HL7Helper.Instance.NotifyConnectionState(
                    //    true,
                    //    $"�ѳɹ����ӵ�������: {_serverAddress}:{_serverPort}"
                    //);

                    return true;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    logService.Info($"���ӷ�����ʧ��: {_serverAddress}:{_serverPort}{ex}");

                    ConnectionFailed?.Invoke(
                        $"���ӷ�����ʧ��: {_serverAddress}:{_serverPort}", ex);
                    // ֪ͨHL7Helper����ʧ��
                    //HL7Helper.Instance.NotifyConnectionState(
                    //    false,
                    //    $"���ӷ�����ʧ��: {_serverAddress}:{_serverPort}"
                    //);

                    return false;
                }
            }
        }

        /// <summary>
        /// �Ͽ���LIS������������
        /// </summary>
        public override void Disconnect()
        {
            lock (_lockObject)
            {
                try
                {
                    // ֹͣ�������
                    StopHeartbeat();

                    // ȡ������ѭ��
                    if (_receiveLoopCts != null)
                    {
                        _receiveLoopCts.Cancel();
                        try
                        {
                            _receiveLoopTask.Wait(1000); // �ȴ�����ѭ������
                        }
                        catch
                        { /* ��������ȡ���쳣 */
                        }
                        _receiveLoopCts.Dispose();
                        _receiveLoopCts = null;
                    }

                    // ������еȴ�����Ӧ
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
                    logService.Info("�ѶϿ�����������");

                    // ֪ͨHL7Helper�����ѶϿ�
                    //HL7Helper.Instance.NotifyConnectionState(false, "���ֶ��Ͽ�����������");
                    ConnectionClosed?.Invoke(false,
                       $"���ֶ��Ͽ�����������");
                }
                catch (Exception ex)
                {
                    logService.Info($"�Ͽ�����������ʱ��������{ex}");
                }
            }
        }

        /// <summary>
        /// �����������
        /// </summary>
        private void StartHeartbeat()
        {
            StopHeartbeat(); // ȷ��֮ǰ��������ֹͣ

            _heartbeatTimer = new Timer(
                CheckConnectionStatus,
                null,
                HeartbeatInterval,
                HeartbeatInterval
            );

            logService.Info("������TCP�����������");
        }

        /// <summary>
        /// ֹͣ�������
        /// </summary>
        private void StopHeartbeat()
        {
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Dispose();
                _heartbeatTimer = null;
                logService.Info("��ֹͣTCP�����������");
            }
        }

        /// <summary>
        /// �������״̬
        /// </summary>
        /// <param name="state">��ʱ��״̬����</param>
        private void CheckConnectionStatus(object state)
        {
            if (_tcpClient == null || _networkStream == null)
            {
                HandleDisconnection("TCP�ͻ��˻�������Ϊ��");
                return;
            }

            try
            {
                // �������״̬�ķ���һ�����Socket����
                if (!_tcpClient.Connected)
                {
                    HandleDisconnection("TCP�ͻ��������ѶϿ�");
                    return;
                }

                // �������״̬�ķ����������Է���/����0�ֽ�����
                // �˷������Լ�����������Ƿ���������
                try
                {
                    if (
                        _tcpClient.Client.Poll(1, SelectMode.SelectRead)
                        && _tcpClient.Available == 0
                    )
                    {
                        // ���Poll����true��AvailableΪ0����ʾ�����ѹر�
                        HandleDisconnection("TCP�����ѹرգ�Poll��⣩");
                        return;
                    }
                }
                catch (SocketException se)
                {
                    HandleDisconnection($"Socket�쳣: {se.Message}");
                    return;
                }

                // ���ִ�е����˵��������Ȼ����
                if (!_isConnected)
                {
                    // ����ڲ�״̬Ϊ�Ͽ���ʵ����������������״̬
                    logService.Info("��⵽TCP�����ѻָ�");
                    _isConnected = true;
                }
            }
            catch (Exception ex)
            {
                logService.Info($"���TCP����״̬ʱ��������{ex}");
                HandleDisconnection($"�������ʱ�����쳣: {ex.Message}");
            }
        }

        /// <summary>
        /// �������ӶϿ�
        /// </summary>
        /// <param name="reason">�Ͽ�ԭ��</param>
        private void HandleDisconnection(string reason)
        {
            if (_isConnected)
            {
                logService.Info($"��⵽�����������ѶϿ�: {reason}");

                _isConnected = false;

                // ֪ͨHL7Helper�����ѶϿ�
                //HL7Helper.Instance.NotifyConnectionState(false, $"�����������ѶϿ�: {reason}");
                ConnectionClosed?.Invoke(
                    false,
                    $"�����������ѶϿ�: {reason}"
                );
                // ������Դ
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
                    logService.Info($"������Դʱ��������{ex}");
                }
            }
        }

        /// <summary>
        /// ��Ϣ����ѭ��������������������յ�����Ϣ
        /// </summary>
        private async Task ReceiveMessagesLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isConnected)
            {
                try
                {
                    if (_networkStream == null || !_tcpClient.Connected)
                    {
                        logService.Info("���ӶϿ�~");
                        // TCP�����ѶϿ����ȴ�һ��ʱ�������
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    // ���ղ�����MLLP��Ϣ
                    var memoryStream = new MemoryStream();
                    bool startFound = false;
                    bool endFound = false;
                    byte previousByte = 0;

                    // ���ó�ʱʱ�䣬����ǿ��ÿ����Ϣ�������ڴ�ʱ�������
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
                                // �����ѹر�
                                logService.Info("TCP�����ѹر�");
                                HandleDisconnection("������Ϣʱ���ӹر�");
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
                            // ���û�����ݣ����ݵȴ���������
                            await Task.Delay(10, cancellationToken);

                            // �������30�붼û�н��յ��κ����ݣ�����״̬�����¿�ʼ����
                            if (startFound && (DateTime.Now - readStartTime).TotalSeconds > 30)
                            {
                                logService.Info("������Ϣ����30��δ��ɣ����ý���״̬");
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
                            memoryStream.SetLength(memoryStream.Length - 2); // �Ƴ�FS��CR
                            string response = GetEncoding().GetString(memoryStream.ToArray());
                            logService.Info($"���յ�HL7��Ӧ: {response}");

                            // ������Ϣ
                            IMessage message = _parser.Parse(response);

                            // ������Ӧ��Ϣ
                            ProcessReceivedMessage(message);
                        }
                        catch (Exception ex)
                        {
                            logService.Info($"������յ���HL7��Ϣʱ����{ex}");
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    // ������ȡ�����˳�ѭ��
                    logService.Info($"������ȡ�����˳�ѭ��{ex}");
                    break;
                }
                catch (Exception ex)
                {
                    logService.Info($"����HL7��Ϣѭ���з�������{ex}");

                    // �������󣬵ȴ�һ��ʱ������
                    try
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (OperationCanceledException ex2)
                    {
                        logService.Info($"������ȡ�����˳�ѭ��2{ex2}");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// �����ֽ����鵽TCP����
        /// </summary>
        /// <param name="bytes">Ҫ���͵��ֽ�����</param>
        /// <returns>�첽����</returns>
        protected override async Task SendBytesAsync(byte[] bytes)
        {
            if (_networkStream == null || !_tcpClient.Connected)
            {
                throw new InvalidOperationException("TCP����δ��");
            }

            try
            {
                await _networkStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                logService.Info($"��������ʱ��������{ex}");
                HandleDisconnection($"��������ʱ��������: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// �������״̬
        /// </summary>
        /// <returns>�����Ƿ�����</returns>
        public override bool IsConnected()
        {
          
                // ���Ӹ��ϸ�����Ӽ��
                if (!_isConnected || _tcpClient == null || !_tcpClient.Connected)
                {
                    return false;
                }

                // ���Ը����εļ��
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
