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
        /// 收到数据
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
            //原始数据
            SerialPortOriginDataReceived?.Invoke(obj);
        }

        private void SerialPort_SerialPortExceptionReceived(string obj)
        {
            //通讯异常
            SerialPortExceptionReceived?.Invoke(obj);
        }

        private void SerialPort_SerialPortConnectReceived(string msg)
        {
            SerialPortConnectReceived?.Invoke(msg);
            if (String.IsNullOrEmpty(msg))
            {
                //连接成功
                Start();
            }
            else
            {
                //连接失败
            }
        }

        private void Start()
        {
            StartSendQueue();
        }

        /// <summary>
        /// 开始发送队列，最低间隔100ms发送一次
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

            // 将接收到的数据添加到缓冲区
            _receivedDataBuffer.AddRange(obj);

            // 检查是否包含结束符
            int endIndex = _receivedDataBuffer.IndexOf(0x0A);
            if (endIndex != -1)
            {
                //Log.Information("收到信息:endIndex != -1");
                // 获取完整消息（包含结束符）
                byte[] fullMessageBytes = _receivedDataBuffer.GetRange(0, endIndex + 1).ToArray();
                // 从缓冲区中移除已处理的数据
                _receivedDataBuffer.RemoveRange(0, endIndex + 1);
                //Log.Information("收到信息:endIndex + 1 "+(endIndex + 1));

                // 转换为字符串
                string fullMessage = READN_CODING.GetString(fullMessageBytes);
                int lastIndex = fullMessage.LastIndexOf("}");
                //Log.Information("收到信息:lastIndex" + (lastIndex)+" "+ fullMessage);

                if (lastIndex < 0 || lastIndex + 4 >= fullMessage.Length)
                {
                    Log.Information("不完整 " + fullMessage);
                    return;
                }
                string crc = fullMessage.Substring(lastIndex + 1, 4);
                fullMessage = fullMessage.Substring(0, lastIndex + 1);
                Log.Information("收到信息:" + fullMessage + " crc=" + crc);
                string ncrc = Crc16.GetCrcString(SerialGlobal.Encoding.GetBytes(fullMessage));

                if (!crc.Equals(ncrc))
                {
                    Log.Information("crc校验失败 " + fullMessage + " ncrc:" + ncrc);
                    return;
                }
                try
                {
                    var response = SerialUtils.TranToBaseT<dynamic>(fullMessage);
                    if (response == null)
                    {
                        Log.Information($"反序列化失败:{fullMessage}");
                        return;
                    }
                    if (response.Type == BaseResponseModel<dynamic>.Type_Response)
                    { //响应
                        TaskCompletionSource<bool> tcs;
                        if (_pendingRequests.TryRemove(response.Code, out tcs))
                        {
                            tcs.SetResult(true);
                        }
                    }
                    else if (response.Type == BaseResponseModel<dynamic>.Type_Reply)
                    { //回复
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
                    Log.Information($"反序列化失败: {e.Message}");
                    return;
                }
            }
        }

        /// <summary>
        /// 接收到下位机回复，分发
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
                        //自检
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
                        //获取仪器状态
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
                    //     //获取清洗液状态
                    //     DispatchReceiveData((item) => {
                    //         item.ReceiveCleanoutFluidModel(SerialUtils.TranToBaseT<CleanoutFluidModel>(str));
                    //     });
                    //     break;
                    // case SerialGlobal.CMD_GetSampleShelf:
                    //     //获取样本架状态
                    //     DispatchReceiveData((item) => {
                    //         item.ReceiveSampleShelfModel(SerialUtils.TranToBaseT<SamleShelfModel>(str));
                    //     });
                    //     break;
                    case SerialGlobal.CMD_MoveSampleShelf:
                        //获取移动样本架
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
                        //获取移动样本
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
                        //获取取样
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
                        //获取清洗取样针
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
                        //获取加样
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
                        //获取排水
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
                        //获取推卡
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
                        //获取移动检测卡到反应区
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
                        //获取检测
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveTestModel(SerialUtils.TranToBaseT<TestModel>(str));
                            }
                        );
                        break;
                    case SerialGlobal.CMD_GetReactionTemp:
                        //获取反应区温度
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
                        //获取清空反应区
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
                        //获取控制电机
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveMotorModel(SerialUtils.TranToBaseT<MotorModel>(str));
                            }
                        );
                        break;
                    case SerialGlobal.CMD_ResetParams:
                        //获取重载参数
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
                        //获取升级
                        DispatchReceiveData(
                            (item) =>
                            {
                                item.ReceiveUpdateModel(SerialUtils.TranToBaseT<UpdateModel>(str));
                            }
                        );
                        break;
                    case SerialGlobal.CMD_Squeezing:
                        //获取挤压
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
                        //获取刺破
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
                        //版本号
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
                        //关机
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
                        Log.Error($"Reply 未找到 code={code} str={str}");
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
            Log.Information("发出信息:" + data.Replace(SerialGlobal.EndStr, ""));

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
                        //Log.Information("第" + (attempt) + "次已回复");

                        return true;
                    }
                    else
                    {
                        if (attempt == retry - 1)
                        {
                            Log.Information("超过三次都没回复");
                            _replyMap.Remove(code);
                            return false;
                        }
                        else
                        {
                            Log.Information("超时未回复，正在重发" + attempt);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (attempt == retry - 1)
                    {
                        Log.Information("超过三次都没回复");
                        _replyMap.Remove(code);
                        return false;
                    }
                    else
                    {
                        Log.Information("超时未回复，正在重发" + attempt);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 自检
        /// </summary>
        public void GetSelfInspectionState()
        {
            SendCmd(SerialGlobal.CMD_GetSelfInspectionState);
        }

        /// <summary>
        /// 获取仪器状态
        /// </summary>
        public void GetMachineState()
        {
            SendCmd(SerialGlobal.CMD_GetMachineState);
        }

        /// <summary>
        /// 获取清洗液状态
        /// </summary>
        public void GetCleanoutFluid()
        {
            SendCmd(SerialGlobal.CMD_GetCleanoutFluid);
        }

        /// <summary>
        /// 获取样本架状态
        /// </summary>
        public void SampleShelf()
        {
            SendCmd(SerialGlobal.CMD_GetSampleShelf);
        }

        /// <summary>
        /// 移动样本架
        /// </summary>
        /// <param name="pos">移动到的位置，0-5,0为复位</param>

        public void MoveSampleShelf(int pos)
        {
            SendCmd(SerialGlobal.CMD_MoveSampleShelf, "" + pos);
        }

        /// <summary>
        /// 移动样本
        /// </summary>
        /// <param name="pos">移动到的位置，1-5</param>
        public void MoveSample(int pos)
        {
            SendCmd(SerialGlobal.CMD_MoveSample, "" + pos);
        }

        /// <summary>
        /// 取样
        /// </summary>
        /// <param name="type">类型，0样本管，1样本杯</param>
        /// <param name="volume">取样量，单位mL/10</param>
        public void Sampling(string type, int volume)
        {
            SendCmd(SerialGlobal.CMD_Sampling, type, "" + volume);
        }

        /// <summary>
        /// 清洗取样针
        /// </summary>
        public void CleanoutSamplingProbe(int duration)
        {
            SendCmd(SerialGlobal.CMD_CleanoutSamplingProbe, "" + duration);
        }

        /// <summary>
        /// 加样
        /// </summary>
        /// <param name="volume">加样量，单位mL/10</param>
        /// <param name="type">加样位置，0表示在取样处加样，1表示在检测卡处加样</param>

        public void AddingSample(int volume, string type)
        {
            SendCmd(SerialGlobal.CMD_AddingSample, "" + volume, type);
        }

        /// <summary>
        /// 排水
        /// </summary>
        public void Drainage()
        {
            SendCmd(SerialGlobal.CMD_Drainage);
        }

        /// <summary>
        /// 推卡
        /// </summary>
        public void PushCard()
        {
            SendCmd(SerialGlobal.CMD_PushCard);
        }

        /// <summary>
        /// 移动到待反应区
        /// </summary>
        /// <param name="x">放卡的位置X,0-2</param>
        /// <param name="y">放卡的位置Y,0-9</param>
        public void MoveReactionArea(int x, int y)
        {
            SendCmd(SerialGlobal.CMD_MoveReactionArea, "" + x, "" + y);
        }

        /// <summary>
        /// 检测
        /// </summary>
        /// <param name="x">拿卡的位置X,0-2</param>
        /// <param name="y">拿卡的位置Y,0-9</param>
        /// <param name="cardType">卡片类型,0单联卡，1双联卡</param>
        /// <param name="testType">检测类型,0普通卡，1质控</param>
        /// <param name="scanStart">扫描参数，起点</param>
        /// <param name="scanEnd">扫描参数，终点</param>
        /// <param name="peakWidth">扫描参数，峰宽</param>
        /// <param name="peakDistance">扫描参数，间距</param>
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
        /// 获取反应区温度
        /// </summary>
        /// <param name="temp">0为获取温度，否则为设置偏移，单位为℃*10</param>
        public void GetReactionTemp(string temp)
        {
            SendCmd(SerialGlobal.CMD_GetReactionTemp, temp);
        }

        /// <summary>
        /// 清空反应区
        /// </summary>
        public void ClearReactionArea()
        {
            SendCmd(SerialGlobal.CMD_ClearReactionArea);
        }

        /// <summary>
        /// 控制电机
        /// </summary>
        /// <param name="motor">电机号</param>
        /// <param name="diraction">方向，1复位，2正向，3反向</param>
        /// <param name="value">距离，单位为mm*10,如205表示20.5mm</param>
        public void Motor(string motor, string diraction, string value)
        {
            SendCmd(SerialGlobal.CMD_Motor, motor, diraction, value);
        }

        /// <summary>
        /// 重载参数
        /// </summary>
        public void ResetParams()
        {
            SendCmd(SerialGlobal.CMD_ResetParams);
        }

        /// <summary>
        /// 升级
        /// </summary>
        public void Update()
        {
            SendCmd(SerialGlobal.CMD_Update);
        }

        /// <summary>
        /// 挤压
        /// </summary>
        /// <param name="type">0表示样本管，需要挤压动作；1表示样本杯，不需要挤压动作</param>
        public void Squeezing(string type)
        {
            SendCmd(SerialGlobal.CMD_Squeezing, type);
        }

        /// <summary>
        /// 刺破
        /// </summary>
        /// <param name="type">0表示样本管，需要刺破动作；1表示样本杯，不需要刺破动作</param>
        public void Pierced(string type)
        {
            SendCmd(SerialGlobal.CMD_Pierced, type);
        }

        /// <summary>
        /// 获取版本号
        /// </summary>
        public void GetVersion()
        {
            SendCmd(SerialGlobal.CMD_Version);
        }

        /// <summary>
        /// 刺破
        /// </summary>
        public void Shutdown()
        {
            SendCmd(SerialGlobal.CMD_Shutdown);
        }

        /// <summary>
        /// 接收到下位机回复，响应下位机
        /// </summary>
        /// <param name="v"></param>
        private void ResponseReply(string code)
        {
            //Log.Information("ResponseReply code "+ code);
            SendCmd(SerialGlobal.CMD_ResponseReply, code);
        }

        /// <summary>
        /// 拼接命令
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
            Log.Information("发出信息2:" + SerialGlobal.Encoding.GetString(data));

            SerialPort.SendData(data);
        }

        public bool IsOpen()
        {
            return SerialPort.IsOpen();
        }
    }
}
