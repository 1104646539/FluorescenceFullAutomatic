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
    /// ����ͨ�ŷ���ʵ����
    /// ʵ��HL7ͨ�ŷ���ӿڣ�ʹ�ô��ڽ���ͨ��
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
        /// ���캯��
        /// </summary>
        public SerialCommunicationService() : base()
        {
            // �������л�ȡ���ڲ���
            _portName = UploadConfig.Instance.SerialPortName; 
            _baudRate = int.Parse(UploadConfig.Instance.BaudRate);
            _dataBits = int.Parse(UploadConfig.Instance.DataBit);
            
            // ����ֹͣλ
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
            
            // ����У��λ
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

                    _serialPort = new SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits)
                    {
                        ReadTimeout = _timeout,
                        WriteTimeout = _timeout
                    };

                    _serialPort.Open();
                    _isConnected = true;
                    
                    // ��������ѭ��
                    _receiveLoopCts = new CancellationTokenSource();
                    _receiveLoopTask = Task.Run(() => ReceiveMessagesLoop(_receiveLoopCts.Token));
                    
                    Log.Information($"�ѳɹ����ӵ�����: {_portName}");
                    return true;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    Log.Error(ex, $"���Ӵ���ʧ��: {_portName}");
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
                    // ȡ������ѭ��
                    if (_receiveLoopCts != null)
                    {
                        _receiveLoopCts.Cancel();
                        try
                        {
                            _receiveLoopTask.Wait(1000); // �ȴ�����ѭ������
                        }
                        catch { /* ��������ȡ���쳣 */ }
                        _receiveLoopCts.Dispose();
                        _receiveLoopCts = null;
                    }
                    
                    // ������еȴ�����Ӧ
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
                    Log.Information("�ѶϿ���������");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "�Ͽ���������ʱ��������");
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
                    if (_serialPort == null || !_serialPort.IsOpen)
                    {
                        // ����δ�򿪣��ȴ�һ��ʱ�������
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
                            // ���û�����ݣ����ݵȴ���������
                            await Task.Delay(10, cancellationToken);
                            
                            // �������30�붼û�н��յ��κ����ݣ�����״̬�����¿�ʼ����
                            if (startFound && (DateTime.Now - readStartTime).TotalSeconds > 30)
                            {
                                Log.Warning("������Ϣ����30��δ��ɣ����ý���״̬");
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
                            Log.Information($"���յ�HL7��Ӧ: {response}");
                            
                            // ������Ϣ
                            IMessage message = _parser.Parse(response);
                            
                            // ������Ӧ��Ϣ
                            ProcessReceivedMessage(message);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "������յ���HL7��Ϣʱ����");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // ������ȡ�����˳�ѭ��
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "����HL7��Ϣѭ���з�������");
                    
                    // �������󣬵ȴ�һ��ʱ������
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
        /// �����ֽ����鵽����
        /// </summary>
        /// <param name="bytes">Ҫ���͵��ֽ�����</param>
        /// <returns>�첽����</returns>
        protected override Task SendBytesAsync(byte[] bytes)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                throw new InvalidOperationException("����δ��");
            }
            
            _serialPort.Write(bytes, 0, bytes.Length);
            return Task.CompletedTask; // ����д����ͬ���ģ���������ɵ�����
        }

        /// <summary>
        /// �������״̬
        /// </summary>
        /// <returns>�����Ƿ�����</returns>
        public override bool IsConnected()
        {
            lock (_lockObject)
            {
                return _isConnected && _serialPort != null && _serialPort.IsOpen;
            }
        }
    }
} 