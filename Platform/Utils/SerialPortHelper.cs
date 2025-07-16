using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.ViewModels;
using Newtonsoft.Json;
using Serilog;

namespace FluorescenceFullAutomatic.Platform.Utils
{
    public class SerialPortHelper : ISerialPort
    {
        private static readonly Lazy<SerialPortHelper> _instance = new Lazy<SerialPortHelper>(
            () =>
                SystemGlobal.IsCodeDebug
                    ? new SerialPortHelper(new FakeSerialPortImpl())
                    : new SerialPortHelper(new SerialPortImpl())
        );
        public static SerialPortHelper Instance => _instance.Value;
        readonly ISerialPort SerialPort;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingRequests =
            new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        /// <summary>
        /// �յ�����
        /// </summary>
        public event Action<byte[]> DataReceived
        {
            add { SerialPort.DataReceived += value; }
            remove { SerialPort.DataReceived -= value; }
        }

        public event Action<string> SerialPortConnectReceived;
        public event Action<string> SerialPortExceptionReceived;
        public event Action<string> SerialPortOriginDataReceived;
        public event Action<string> SerialPortOriginDataSend;

        public List<IReceiveData> SerialPortReceived = new List<IReceiveData>();

        private Encoding READN_CODING = Encoding.GetEncoding("gb2312");

        private ConcurrentQueue<string> _SendQueue = new ConcurrentQueue<string>();

        private int OverTime = 2000;
        Task SendTask;

        private SerialPortHelper(ISerialPort serialPort)
        {
            this.SerialPort = serialPort;
            this.SerialPort.DataReceived += SerialPort_DataReceived;
            this.SerialPort.SerialPortConnectReceived += SerialPort_SerialPortConnectReceived;
            this.SerialPort.SerialPortExceptionReceived += SerialPort_SerialPortExceptionReceived;
            this.SerialPort.SerialPortOriginDataReceived += SerialPort_SerialPortOriginDataReceived;
            this.SerialPort.SerialPortOriginDataSend += SerialPort_SerialPortOriginDataSend;
        }

      

        public void AddReceiveData(IReceiveData receiveData)
        {
            if (SerialPortReceived == null)
            {
                SerialPortReceived = new List<IReceiveData>();
            }
            SerialPortReceived.Add(receiveData);
        }

        public void RemoveReceiveData(IReceiveData receiveData)
        {
            if (SerialPortReceived == null)
                return;
            SerialPortReceived.Remove(receiveData);
        }

        public void DispatchReceiveData(Action<IReceiveData> action)
        {
            if (SerialPortReceived == null)
                return;
            foreach (var item in SerialPortReceived)
            {
                action.Invoke(item);
            }
        }

        private void SerialPort_SerialPortOriginDataSend(string obj)
        {
            SerialPortOriginDataSend?.Invoke(obj);
        }

        private void SerialPort_SerialPortOriginDataReceived(string obj)
        {
            //ԭʼ����
            SerialPortOriginDataReceived?.Invoke(obj);
        }

        private void SerialPort_SerialPortExceptionReceived(string obj)
        {
            //ͨѶ�쳣
            SerialPortExceptionReceived?.Invoke(obj);
        }

        private void SerialPort_SerialPortConnectReceived(string msg)
        {
            SerialPortConnectReceived?.Invoke(msg);
            if (String.IsNullOrEmpty(msg))
            {
                //���ӳɹ�
                Start();
            }
            else
            {
                //����ʧ��
            }
        }

        private void Start()
        {
            StartSendQueue();
        }

        /// <summary>
        /// ��ʼ���Ͷ��У���ͼ��100ms����һ��
        /// </summary>
        private void StartSendQueue()
        {
            SendTask = new Task(() =>
            {
                while (true)
                {
                    Thread.Sleep(100);
                    if (_SendQueue.TryDequeue(out string data))
                    {
                        if (SerialUtils.IsNeedRetry(data))
                        {
                            SendDataRetry(data);
                        }
                        else
                        {
                            SendData(data);
                        }
                    }
                }
            });
            SendTask.Start();
        }

        private Dictionary<string, bool> _replyMap = new Dictionary<string, bool>();
        private List<byte> _receivedDataBuffer = new List<byte>();

        private void SerialPort_DataReceived(byte[] obj)
        {
            if (obj.Length <= 0)
                return;

            // �����յ���������ӵ�������
            _receivedDataBuffer.AddRange(obj);

            // ����Ƿ����������
            int endIndex = _receivedDataBuffer.IndexOf(0x0A);
            if (endIndex != -1)
            {
                //Log.Information("�յ���Ϣ:endIndex != -1");
                // ��ȡ������Ϣ��������������
                byte[] fullMessageBytes = _receivedDataBuffer.GetRange(0, endIndex + 1).ToArray();
                // �ӻ��������Ƴ��Ѵ��������
                _receivedDataBuffer.RemoveRange(0, endIndex + 1);
                //Log.Information("�յ���Ϣ:endIndex + 1 "+(endIndex + 1));

                // ת��Ϊ�ַ���
                string fullMessage = READN_CODING.GetString(fullMessageBytes);
                int lastIndex = fullMessage.LastIndexOf("}");
                //Log.Information("�յ���Ϣ:lastIndex" + (lastIndex)+" "+ fullMessage);

                if (lastIndex < 0 || lastIndex + 4 >= fullMessage.Length)
                {
                    Log.Information("������ " + fullMessage);
                    return;
                }
                string crc = fullMessage.Substring(lastIndex + 1, 4);
                fullMessage = fullMessage.Substring(0, lastIndex + 1);
                Log.Information("�յ���Ϣ:" + fullMessage + " crc=" + crc);
                string ncrc = Crc16.GetCrcString(SerialGlobal.Encoding.GetBytes(fullMessage));

                if (!crc.Equals(ncrc))
                {
                    Log.Information("crcУ��ʧ�� " + fullMessage + " ncrc:" + ncrc);
                    return;
                }
                try
                {
                    var response = SerialUtils.TranToBaseT<dynamic>(fullMessage);
                    if (response == null)
                    {
                        Log.Information($"�����л�ʧ��:{fullMessage}");
                        return;
                    }
                    if (response.Type == BaseResponseModel<dynamic>.Type_Response)
                    { //��Ӧ
                        TaskCompletionSource<bool> tcs;
                        if (_pendingRequests.TryRemove(response.Code, out tcs))
                        {
                            tcs.SetResult(true);
                        }
                    }
                    else if (response.Type == BaseResponseModel<dynamic>.Type_Reply)
                    { //�ظ�
                        ResponseReply(response.Code);
                        if (_replyMap.TryGetValue(response.Code, out bool isReply))
                        {
                            if (isReply)
                            {
                                _replyMap.Remove(response.Code);
                                Reply(response.Code, fullMessage);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Information($"�����л�ʧ��: {e.Message}");
                    return;
                }
            }
        }

        /// <summary>
        /// ���յ���λ���ظ����ַ�
        /// </summary>
        /// <param name="code"></param>
        /// <param name="str"></param>
        private void Reply(string code, string str)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                BaseResponseModel<dynamic> temp = SerialUtils.TranToBaseT<dynamic>(str);
                if (temp.State != BaseResponseModel<string>.State_Success)
                {
                    DispatchReceiveData(
                        (item) =>
                        {
                            item.ReceiveStateError(temp);
                        }
                    );
                    return;
                }
                switch (code)
                {
                    case SerialGlobal.CMD_GetSelfInspectionState:
                        //�Լ�
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveGetSelfMachineStatusModel(
                                    SerialUtils.TranToBaseT<List<string>>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_GetMachineState:
                        //��ȡ����״̬
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveMachineStatusModel(
                                    SerialUtils.TranToBaseT<MachineStatusModel>(str)
                                );
                            }
                        );
                        break;
                    // case SerialGlobal.CMD_GetCleanoutFluid:
                    //     //��ȡ��ϴҺ״̬
                    //     DispatchReceiveData((item) => {
                    //         item.ReceiveCleanoutFluidModel(SerialUtils.TranToBaseT<CleanoutFluidModel>(str));
                    //     });
                    //     break;
                    // case SerialGlobal.CMD_GetSampleShelf:
                    //     //��ȡ������״̬
                    //     DispatchReceiveData((item) => {
                    //         item.ReceiveSampleShelfModel(SerialUtils.TranToBaseT<SamleShelfModel>(str));
                    //     });
                    //     break;
                    case SerialGlobal.CMD_MoveSampleShelf:
                        //��ȡ�ƶ�������
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveMoveSampleShelfModel(
                                    SerialUtils.TranToBaseT<MoveSampleShelfModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_MoveSample:
                        //��ȡ�ƶ�����
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveMoveSampleModel(
                                    SerialUtils.TranToBaseT<MoveSampleModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Sampling:
                        //��ȡȡ��
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveSamplingModel(
                                    SerialUtils.TranToBaseT<SamplingModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_CleanoutSamplingProbe:
                        //��ȡ��ϴȡ����
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveCleanoutSamplingProbeModel(
                                    SerialUtils.TranToBaseT<CleanoutSamplingProbeModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_AddingSample:
                        //��ȡ����
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveAddingSampleModel(
                                    SerialUtils.TranToBaseT<AddingSampleModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Drainage:
                        //��ȡ��ˮ
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveDrainageModel(
                                    SerialUtils.TranToBaseT<DrainageModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_PushCard:
                        //��ȡ�ƿ�
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceivePushCardModel(
                                    SerialUtils.TranToBaseT<PushCardModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_MoveReactionArea:
                        //��ȡ�ƶ���⿨����Ӧ��
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveMoveReactionAreaModel(
                                    SerialUtils.TranToBaseT<MoveReactionAreaModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Test:
                        //��ȡ���
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveTestModel(SerialUtils.TranToBaseT<TestModel>(str));
                            }
                        );
                        break;
                    case SerialGlobal.CMD_GetReactionTemp:
                        //��ȡ��Ӧ���¶�
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveReactionTempModel(
                                    SerialUtils.TranToBaseT<ReactionTempModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_ClearReactionArea:
                        //��ȡ��շ�Ӧ��
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveClearReactionAreaModel(
                                    SerialUtils.TranToBaseT<ClearReactionAreaModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Motor:
                        //��ȡ���Ƶ��
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveMotorModel(SerialUtils.TranToBaseT<MotorModel>(str));
                            }
                        );
                        break;
                    case SerialGlobal.CMD_ResetParams:
                        //��ȡ���ز���
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveResetParamsModel(
                                    SerialUtils.TranToBaseT<ResetParamsModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Update:
                        //��ȡ����
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveUpdateModel(SerialUtils.TranToBaseT<UpdateModel>(str));
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Squeezing:
                        //��ȡ��ѹ
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveSqueezingModel(
                                    SerialUtils.TranToBaseT<SqueezingModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Pierced:
                        //��ȡ����
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceivePiercedModel(
                                    SerialUtils.TranToBaseT<PiercedModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Version:
                        //�汾��
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveVersionModel(
                                    SerialUtils.TranToBaseT<VersionModel>(str)
                                );
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Shutdown:
                        //�ػ�
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveShutdownModel(
                                    SerialUtils.TranToBaseT<ShutdownModel>(str)
                                );
                            }
                        );
                        break;
                    default:
                        Log.Error($"Reply δ�ҵ� code={code} str={str}");
                        break;
                }
            });
        }

        public void Connect(string portName, int baudRate)
        {
            SerialPort.Connect(portName, baudRate);
        }

        public void Disconnect()
        {
            SerialPort.Disconnect();
            if (_SendQueue != null)
            {
                _SendQueue = new ConcurrentQueue<string>();
            }
        }

        public void SendData(string data)
        {
            Log.Information("������Ϣ:" + data.Replace(SerialGlobal.EndStr, ""));

            SerialPort.SendData(data);
        }

        private bool SendDataRetry(string data, int retry = 3)
        {
            var tcs = new TaskCompletionSource<bool>();
            string code = SerialUtils.GetDataCMD(data);
            _pendingRequests.TryAdd(code, tcs);
            _replyMap.Add(code, true);
            for (int attempt = 0; attempt < retry; attempt++)
            {
                try
                {
                    SendData(data);
                    bool ret = tcs.Task.Wait(OverTime);
                    if (ret)
                    {
                        //Log.Information("��" + (attempt) + "���ѻظ�");

                        return true;
                    }
                    else
                    {
                        if (attempt == retry - 1)
                        {
                            Log.Information("�������ζ�û�ظ�");
                            _replyMap.Remove(code);
                            return false;
                        }
                        else
                        {
                            Log.Information("��ʱδ�ظ��������ط�" + attempt);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (attempt == retry - 1)
                    {
                        Log.Information("�������ζ�û�ظ�");
                        _replyMap.Remove(code);
                        return false;
                    }
                    else
                    {
                        Log.Information("��ʱδ�ظ��������ط�" + attempt);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// �Լ�
        /// </summary>
        public void GetSelfInspectionState()
        {
            SendCmd(SerialGlobal.CMD_GetSelfInspectionState);
        }

        /// <summary>
        /// ��ȡ����״̬
        /// </summary>
        public void GetMachineState()
        {
            SendCmd(SerialGlobal.CMD_GetMachineState);
        }

        /// <summary>
        /// ��ȡ��ϴҺ״̬
        /// </summary>
        public void GetCleanoutFluid()
        {
            SendCmd(SerialGlobal.CMD_GetCleanoutFluid);
        }

        /// <summary>
        /// ��ȡ������״̬
        /// </summary>
        public void SampleShelf()
        {
            SendCmd(SerialGlobal.CMD_GetSampleShelf);
        }

        /// <summary>
        /// �ƶ�������
        /// </summary>
        /// <param name="pos">�ƶ�����λ�ã�0-5,0Ϊ��λ</param>

        public void MoveSampleShelf(int pos)
        {
            SendCmd(SerialGlobal.CMD_MoveSampleShelf, "" + pos);
        }

        /// <summary>
        /// �ƶ�����
        /// </summary>
        /// <param name="pos">�ƶ�����λ�ã�1-5</param>
        public void MoveSample(int pos)
        {
            SendCmd(SerialGlobal.CMD_MoveSample, "" + pos);
        }

        /// <summary>
        /// ȡ��
        /// </summary>
        /// <param name="type">���ͣ�0�����ܣ�1������</param>
        /// <param name="volume">ȡ��������λmL/10</param>
        public void Sampling(string type, int volume)
        {
            SendCmd(SerialGlobal.CMD_Sampling, type, "" + volume);
        }

        /// <summary>
        /// ��ϴȡ����
        /// </summary>
        public void CleanoutSamplingProbe(int duration)
        {
            SendCmd(SerialGlobal.CMD_CleanoutSamplingProbe, "" + duration);
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="volume">����������λmL/10</param>
        /// <param name="type">����λ�ã�0��ʾ��ȡ����������1��ʾ�ڼ�⿨������</param>

        public void AddingSample(int volume, string type)
        {
            SendCmd(SerialGlobal.CMD_AddingSample, "" + volume, type);
        }

        /// <summary>
        /// ��ˮ
        /// </summary>
        public void Drainage()
        {
            SendCmd(SerialGlobal.CMD_Drainage);
        }

        /// <summary>
        /// �ƿ�
        /// </summary>
        public void PushCard()
        {
            SendCmd(SerialGlobal.CMD_PushCard);
        }

        /// <summary>
        /// �ƶ�������Ӧ��
        /// </summary>
        /// <param name="x">�ſ���λ��X,0-2</param>
        /// <param name="y">�ſ���λ��Y,0-9</param>
        public void MoveReactionArea(int x, int y)
        {
            SendCmd(SerialGlobal.CMD_MoveReactionArea, "" + x, "" + y);
        }

        /// <summary>
        /// ���
        /// </summary>
        /// <param name="x">�ÿ���λ��X,0-2</param>
        /// <param name="y">�ÿ���λ��Y,0-9</param>
        /// <param name="cardType">��Ƭ����,0��������1˫����</param>
        /// <param name="testType">�������,0��ͨ����1�ʿ�</param>
        /// <param name="scanStart">ɨ����������</param>
        /// <param name="scanEnd">ɨ��������յ�</param>
        /// <param name="peakWidth">ɨ����������</param>
        /// <param name="peakDistance">ɨ����������</param>
        public void Test(
            int x,
            int y,
            string cardType,
            string testType,
            string scanStart,
            string scanEnd,
            string peakWidth,
            string peakDistance
        )
        {
            SendCmd(
                SerialGlobal.CMD_Test,
                "" + x,
                "" + y,
                cardType,
                testType,
                scanStart,
                scanEnd,
                peakWidth,
                peakDistance
            );
        }

        /// <summary>
        /// ��ȡ��Ӧ���¶�
        /// </summary>
        /// <param name="temp">0Ϊ��ȡ�¶ȣ�����Ϊ����ƫ�ƣ���λΪ��*10</param>
        public void GetReactionTemp(string temp)
        {
            SendCmd(SerialGlobal.CMD_GetReactionTemp, temp);
        }

        /// <summary>
        /// ��շ�Ӧ��
        /// </summary>
        public void ClearReactionArea()
        {
            SendCmd(SerialGlobal.CMD_ClearReactionArea);
        }

        /// <summary>
        /// ���Ƶ��
        /// </summary>
        /// <param name="motor">�����</param>
        /// <param name="diraction">����1��λ��2����3����</param>
        /// <param name="value">���룬��λΪmm*10,��205��ʾ20.5mm</param>
        public void Motor(string motor, string diraction, string value)
        {
            SendCmd(SerialGlobal.CMD_Motor, motor, diraction, value);
        }

        /// <summary>
        /// ���ز���
        /// </summary>
        public void ResetParams()
        {
            SendCmd(SerialGlobal.CMD_ResetParams);
        }

        /// <summary>
        /// ����
        /// </summary>
        public void Update()
        {
            SendCmd(SerialGlobal.CMD_Update);
        }

        /// <summary>
        /// ��ѹ
        /// </summary>
        /// <param name="type">0��ʾ�����ܣ���Ҫ��ѹ������1��ʾ������������Ҫ��ѹ����</param>
        public void Squeezing(string type)
        {
            SendCmd(SerialGlobal.CMD_Squeezing, type);
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="type">0��ʾ�����ܣ���Ҫ���ƶ�����1��ʾ������������Ҫ���ƶ���</param>
        public void Pierced(string type)
        {
            SendCmd(SerialGlobal.CMD_Pierced, type);
        }

        /// <summary>
        /// ��ȡ�汾��
        /// </summary>
        public void GetVersion()
        {
            SendCmd(SerialGlobal.CMD_Version);
        }

        /// <summary>
        /// ����
        /// </summary>
        public void Shutdown()
        {
            SendCmd(SerialGlobal.CMD_Shutdown);
        }

        /// <summary>
        /// ���յ���λ���ظ�����Ӧ��λ��
        /// </summary>
        /// <param name="v"></param>
        private void ResponseReply(string code)
        {
            //Log.Information("ResponseReply code "+ code);
            SendCmd(SerialGlobal.CMD_ResponseReply, code);
        }

        /// <summary>
        /// ƴ������
        /// </summary>
        /// <param name="args"></param>
        public void SendCmd(params string[] args)
        {
            string cmd = string.Join(" ", args);
            cmd += " " + Crc16.GetCrcString(SerialGlobal.Encoding.GetBytes(cmd));
            _SendQueue.Enqueue(cmd + SerialGlobal.EndStr);
            //Log.Information("SendCmd {args}", args);
        }

        public void SendData(byte[] data)
        {
            Log.Information("������Ϣ2:" + SerialGlobal.Encoding.GetString(data));

            SerialPort.SendData(data);
        }

        public bool IsOpen()
        {
            return SerialPort.IsOpen();
        }
    }
}
