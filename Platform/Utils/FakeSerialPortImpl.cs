using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Model;
using Newtonsoft.Json;
using Serilog;

namespace FluorescenceFullAutomatic.Platform.Utils
{
    public class FakeSerialPortImpl : ISerialPort
    {
        
        public event Action<byte[]> DataReceived;
        public event Action<string> SerialPortConnectReceived;
        public event Action<string> SerialPortConnectExceptionReceived;
        public event Action<string> SerialPortOriginDataReceived;
        public event Action<string> SerialPortOriginDataSend;
        public event Action<string> SerialPortExceptionReceived;

        public void Connect(string portName, int baudRate)
        {
            _isOpen = true;
            // 模拟连接成功
            SerialPortConnectReceived?.Invoke(null);
        }

        public void Disconnect()
        {
            // 模拟断开连接
            _isOpen = false;
        }

        public void SendData(byte[] data) { }

        /// <summary>
        /// 回复
        /// </summary>
        /// <param name="commandStr"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ReplyCMD(string[] commandStr)
        {
            GenerateMockData(
                commandStr,
                (data) =>
                {
                    data += Crc16.GetCrcString(SerialGlobal.Encoding.GetBytes(data));
                    data += SerialGlobal.EndStr;
                    byte[] responseBytes = SerialGlobal.Encoding.GetBytes(data);

                    // 触发数据接收事件
                    Log.Information("Fake下位机发出回复:" + data.Replace(SerialGlobal.EndStr, ""));
                    DataReceived?.Invoke(responseBytes);
                    SerialPortOriginDataReceived?.Invoke(
                        SerialGlobal.Encoding.GetString(responseBytes)
                    );
                }
            );
        }

        /// <summary>
        /// 响应
        /// </summary>
        /// <param name="commandStr"></param>
        private void ResponseCMD(string[] commandStr)
        {
            //根据发送的数据生成不同的模拟响应
            BaseResponseModel<string> response = new BaseResponseModel<string>
            {
                Code = commandStr[0],
                Type = BaseResponseModel<dynamic>.Type_Response,
            };
            //序列化为JSON并添加结束符
            string responseStr = JsonConvert.SerializeObject(response);
            responseStr += Crc16.GetCrcString(SerialGlobal.Encoding.GetBytes(responseStr));
            responseStr += SerialGlobal.EndStr;
            byte[] responseBytes = SerialGlobal.Encoding.GetBytes(responseStr);
            // 触发数据接收事件
            //Log.Information("Fake下位机发出响应:" + responseStr);
            DataReceived?.Invoke(responseBytes);
            SerialPortOriginDataReceived?.Invoke(SerialGlobal.Encoding.GetString(responseBytes));
        }

        int index = 0;

        private void GenerateMockData(string[] commandStr, Action<string> exec)
        {
            Task.Run(() =>
            {
                BaseResponseModel<dynamic> reply = new BaseResponseModel<dynamic>
                {
                    Code = commandStr[0],
                    Type = BaseResponseModel<dynamic>.Type_Reply,
                    State = "1", // 默认状态为成功
                };

                switch (commandStr[0])
                {
                    case SerialGlobal.CMD_GetSelfInspectionState:
                        Thread.Sleep(2000);
                        List<string> errors = new List<string>();
                        // errors.Add("101");
                        //errors.Add("201");
                        //errors.Add("302");
                        //errors.Add("401");
                        //errors.Add("502");
                        //errors.Add("902");
                        //errors.Add("2101");
                        //errors.Add("2201");
                        // 模拟自检成功，无错误
                        reply.Data = errors;
                        break;

                    case SerialGlobal.CMD_GetMachineState:
                        Thread.Sleep(100);
                        string CardExist = "1"; // 1卡仓存在
                        string CardNum = new Random().Next(5) + 5 + ""; // 30张卡
                        if (index > 1)
                        {
                            CardNum = "0";
                        }
                        string CleanoutFluid = "1";// 清洗液存在
                        List<int> SamleShelf = new List<int> { 0, 1, 0, 0, 0, 0 };
                        MachineStatusModel machineStatus = new MachineStatusModel
                        {
                            CardExist = CardExist, // 卡仓存在
                            CardNum = CardNum, // 30张卡
                            CleanoutFluid = CleanoutFluid, // 清洗液存在
                            SamleShelf = SamleShelf, // 样本架是否存在
                        };
                        reply.Data = machineStatus;
                        break;

                    case SerialGlobal.CMD_MoveSampleShelf:

                        MoveSampleShelfModel moveShelf = new MoveSampleShelfModel { };
                        reply.Data = moveShelf;
                        break;

                    case SerialGlobal.CMD_MoveSample:

                        MoveSampleModel moveSample = new MoveSampleModel
                        {
                            SampleType = MoveSampleModel.SampleTube,
                        };
                        reply.Data = moveSample;
                        if (index == 0)
                        {
                            reply.Data.SampleType = MoveSampleModel.SampleTube;
                        }
                        else {
                            reply.Data.SampleType = MoveSampleModel.None;
                        }
                        index++;
                        break;

                    case SerialGlobal.CMD_Sampling:
                        SamplingModel sampling = new SamplingModel
                        {
                            Result = "1", // 取样成功
                        };
                        reply.Data = sampling;
                        break;

                    case SerialGlobal.CMD_CleanoutSamplingProbe:

                        CleanoutSamplingProbeModel cleanout = new CleanoutSamplingProbeModel();
                        reply.Data = cleanout;
                        break;

                    case SerialGlobal.CMD_AddingSample:
                        //模拟加样失败
                        //if (index++ > 2)
                        //{
                        //    reply.State = "2";
                        //reply.Error = "2601";
                        //}
                        AddingSampleModel adding = new AddingSampleModel();
                        reply.Data = adding;
                        break;

                    case SerialGlobal.CMD_Drainage:

                        DrainageModel drainage = new DrainageModel();
                        reply.Data = drainage;
                        break;

                    case SerialGlobal.CMD_PushCard:
                        string Success = "1";
                        //if (index++ >= 2)
                        //{
                        //    Success = "0";
                        //}
                        PushCardModel pushCard = new PushCardModel
                        {
                            Success = Success,
                            //QrCode = "FOB2,20221003,123963,0.1,76680.6,38051200,0.85", // 模拟二维码
                            //QrCode = "DFT210019VgMC0.1018.8054.1971.100.0015.3001.4830.75",
                            QrCode = "QC,20250111,233f,200,800,123,145",
                            //QrCode = "DQC"
                        };
                        reply.Data = pushCard;
                        break;

                    case SerialGlobal.CMD_MoveReactionArea:
                        Thread.Sleep(3000);
                        MoveReactionAreaModel moveReaction = new MoveReactionAreaModel();
                        reply.Data = moveReaction;
                        break;

                    case SerialGlobal.CMD_Test:
                        //if (index++ > 1)
                        //{
                        //    reply.State = "2";
                        //    reply.Error = "2501";
                        //}
                        Thread.Sleep(0);
                        string cardtype = commandStr[3];
                        string testtype = commandStr[4];
                        string scanStart = commandStr[5];
                        string scanEnd = commandStr[6];
                        TestModel test = new TestModel();
                        //test.T = new Random().Next(200) + "";
                        //test.C = new Random().Next(100) + "";
                        //test.T2 = new Random().Next(100) + "";
                        //test.C2 = new Random().Next(200) + "";
                        test.T = "2132";
                        test.C = "265414";
                        test.T2 = "0";
                        test.C2 = "0";
                        test.CardType = cardtype;
                        test.TestType = testtype;
                        int start = int.Parse(scanStart);
                        int end = int.Parse(scanEnd);
                        int[] point = new int[end - start];
                        for (int i = 0; i < point.Length; i++)
                        {
                            point[i] = 200 + new Random().Next(800);
                        }
                        test.Location = new int[] { 100, 270, 400, 560 };
                        test.Point = point.ToList();
                        reply.Data = test;
                        break;

                    case SerialGlobal.CMD_GetReactionTemp:
                        ReactionTempModel temp = new ReactionTempModel
                        {
                            Temp = new Random().Next(370) + "", // 模拟温度37.0度
                        };
                        reply.Data = temp;
                        break;

                    case SerialGlobal.CMD_ClearReactionArea:

                        ClearReactionAreaModel clear = new ClearReactionAreaModel();
                        reply.Data = clear;
                        break;

                    case SerialGlobal.CMD_Motor:

                        MotorModel motor = new MotorModel
                        {
                            RestState = "1", // 电机复位成功
                        };
                        reply.Data = motor;
                        break;

                    case SerialGlobal.CMD_ResetParams:

                        ResetParamsModel reset = new ResetParamsModel();
                        reply.Data = reset;
                        break;

                    case SerialGlobal.CMD_Update:
                        UpdateModel update = new UpdateModel
                        {
                            Ready = "1", // 升级准备就绪
                        };
                        reply.Data = update;
                        break;

                    case SerialGlobal.CMD_Squeezing:

                        SqueezingModel squeezing = new SqueezingModel();
                        reply.Data = squeezing;
                        break;

                    case SerialGlobal.CMD_Pierced:

                        PiercedModel pierced = new PiercedModel();
                        reply.Data = pierced;
                        break;
                    case SerialGlobal.CMD_Version:

                        VersionModel version = new VersionModel();
                        version.Ver = "1.0.0.0";
                        reply.Data = version;
                        break;
                    case SerialGlobal.CMD_Shutdown:

                        ShutdownModel shutdown = new ShutdownModel();
                        reply.Data = shutdown;
                        break;
                }

                // 序列化为JSON
                string jsonResponse = JsonConvert.SerializeObject(reply);
                //return jsonResponse;
                exec.Invoke(jsonResponse);
            });
        }

        public void SendData(string data)
        {
            // 模拟设备处理延迟
            Thread.Sleep(200);
            SerialPortOriginDataSend?.Invoke(data);

            data = VerifyCrc(data);

            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            //解析发送的数据
            string[] commandStr = data.Split(' ');
            if (!SerialUtils.IsNeedRetry(data))
            {
                //如果不需要重发，则直接返回
                return;
            }
            //响应
            ResponseCMD(commandStr);

            //回复
            ReplyCMD(commandStr);
        }

        /// <summary>
        /// crc校验
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string VerifyCrc(string data)
        {
            //Log.Information("Fake下位机收到消息:" + data);
            data = data.Replace(SerialGlobal.EndStr, "");
            string crc = data.Substring(data.Length - 4);
            //Log.Information("Fake下位机收到消息 crc:" + crc);

            data = data.Substring(0, data.Length - 5);
            //Log.Information("Fake下位机收到消息 data:" + data);

            string ncrc = Crc16.GetCrcString(SerialGlobal.Encoding.GetBytes(data));
            //Log.Information("Fake下位机收到消息 ncrc:" + ncrc);

            if (!ncrc.Equals(crc))
            {
                Log.Information(
                    $"Fake下位机收到消息 CRC校验失败 crc={crc} ncrc={ncrc} data={data}"
                );
                return "";
            }
            else
            {
                return data;
            }
        }

        bool _isOpen = false;

        public bool IsOpen()
        {
            return _isOpen;
        }
    }
}
