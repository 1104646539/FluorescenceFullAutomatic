using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Web.UI;

namespace FluorescenceFullAutomatic.Platform.Utils
{
    public class TicketReportHelper : ISerialPort
    {
        private static readonly Lazy<TicketReportHelper> _instance = new Lazy<TicketReportHelper>(
            () => SystemGlobal.IsCodeDebug ? new TicketReportHelper(
                  new FakaTicketSerialPort()
                ) : new TicketReportHelper(
                  new SerialPortImpl()
                ));

        public static TicketReportHelper Instance => _instance.Value;

        public event Action<byte[]> DataReceived;
        public event Action<string> SerialPortConnectReceived;
        public event Action<string> SerialPortOriginDataReceived;
        public event Action<string> SerialPortOriginDataSend;
        public event Action<string> SerialPortExceptionReceived;

        private readonly ISerialPort SerialPort;
        private System.Timers.Timer statusTimeoutTimer;
        private const int STATUS_TIMEOUT_MS = 2000; // 2秒超时
        private bool IsWaitingStatus = false;

        // 获取打印机状态命令
        public static readonly byte[] GetStatusCommand = new byte[] { 0x1C, 0x76 }; 

        public static byte PageOut = 0x55;
        public static byte PageFull = 0x04;

        // 数据接收缓冲区
        private List<byte> _receiveBuffer = new List<byte>();

        private TicketReportHelper(ISerialPort serialPort)
        {
            this.SerialPort = serialPort;
            this.SerialPort.DataReceived += SerialPort_DataReceived;
            this.SerialPort.SerialPortConnectReceived += SerialPort_SerialPortConnectReceived;
            
            // 初始化定时器
            statusTimeoutTimer = new System.Timers.Timer(STATUS_TIMEOUT_MS);
            statusTimeoutTimer.Elapsed += StatusTimeoutTimer_Elapsed;
            statusTimeoutTimer.AutoReset = false;
        }

        private void StatusTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsWaitingStatus)
            {
                Log.Warning("获取打印机状态超时");
                IsWaitingStatus = false;
                failedAction?.Invoke("打印机响应超时");
            }
        }

        private void SerialPort_SerialPortConnectReceived(string obj)
        {
            SerialPortConnectReceived?.Invoke(obj);
        }

        private void SerialPort_DataReceived(byte[] obj)
        {
            if (obj == null || obj.Length == 0)
                return;

            // 将新接收的数据添加到缓冲区
            _receiveBuffer.AddRange(obj);

            // 如果不是在等待状态响应，直接清空缓冲区
            if (!IsWaitingStatus)
            {
                _receiveBuffer.Clear();
                return;
            }

            // 处理打印机状态响应
            ProcessStatusResponse();
        }

        private void ProcessStatusResponse()
        {
            if (_receiveBuffer.Count < 1) // 假设状态响应至少1字节
                return;

            // 这里需要根据实际打印机协议解析状态响应
            byte status = _receiveBuffer[0];
            if(status == PageFull){
                Print(msg);
            }else if(status == PageOut){
                failedAction?.Invoke("打印机缺纸");
            }
            // 清空缓冲区
            _receiveBuffer.Clear();
            // 停止定时器
            statusTimeoutTimer.Stop();
            IsWaitingStatus = false;
            successAction?.Invoke("");

        }

        private void Print(string msg)
        {
            SendData(Encoding.GetEncoding("GBK").GetBytes(msg));
        }


        string msg = "";
        Action<string> failedAction;
        Action<string> successAction;
        /// <summary>
        /// 打印小票
        /// </summary>
        /// <param name="trs"></param>
        /// <param name="failedAction"></param>
        public void PrintTicket(List<TestResult> trs, Action<string> successAction, Action<string> failedAction){
            int index = 0;
            int count = trs.Count;
            Action nextAction = null;
            Action<string> failed = (err)=>{
                failedAction.Invoke(err);
                Log.Information(err);
            };
            Action<string> success = (err)=>{
                nextAction();
            };
            nextAction = ()=>{
                if (index < trs.Count)
                {
                    PrintTicket(trs[index++], success, failed);
                }
                else {
                    Log.Information("打印完成");
                    successAction?.Invoke("打印完成");
                }
            };
           nextAction();
        }
      
        public void PrintTicket(TestResult tr,Action<string> successAction,Action<string> failedAction){
            this.failedAction = failedAction;
            this.successAction = successAction;
            msg = GetPrintTestResultMsg(tr);
            Log.Information(msg);
            GetStatus();
        }

        private string GetPrintTestResultMsg(TestResult tr)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\n\n\n\n");
            sb.Append("检测报告\n\n");
            sb.Append($"检测时间：{GlobalUtil.ToStringOrNull(tr?.TestTime.GetDateTimeString())}\n");
            sb.Append($"检测项目：{GlobalUtil.ToStringOrNull(tr?.Project?.ProjectName)}\n");
            sb.Append($"检测值(Fob)：{GlobalUtil.ToStringOrNull(tr?.Con)} {GlobalUtil.ToStringOrNull(tr?.Project?.ProjectUnit)}\n");
            if (tr?.Project?.ProjectType == Project.Project_Type_Double) {
                sb.Append($"检测值(Trf)：{GlobalUtil.ToStringOrNull(tr?.Con2)} {GlobalUtil.ToStringOrNull(tr?.Project?.ProjectUnit2)}\n");
            }
            sb.Append($"姓名：{GlobalUtil.ToStringOrNull(tr?.Patient?.PatientName)}\n");
            sb.Append($"性别：{GlobalUtil.ToStringOrNull(tr?.Patient?.PatientGender)}\n");
            sb.Append($"年龄：{GlobalUtil.ToStringOrNull(tr?.Patient?.PatientAge)}\n");
            sb.Append($"结果判定：{GlobalUtil.ToStringOrNull(tr?.TestVerdict)}\n");
            sb.Append("\n\n\n\n");
            return sb.ToString();
        }


        public void PrintQuality(){}

        public event Action<bool, string> StatusReceived;

        private void OnStatusReceived(bool isReady, string message)
        {
            StatusReceived?.Invoke(isReady, message);
        }

        public void Connect(string portName, int baudRate)
        {
            SerialPort.Connect(portName, baudRate);
        }

        public void Disconnect()
        {
            SerialPort.Disconnect();
        }

        public void SendData(string data)
        {
            SerialPort.SendData(data);
        }

        public void SendData(byte[] data)
        {
            SerialPort.SendData(data);
        }

        public bool IsOpen()
        {
            return SerialPort.IsOpen();
        }

        /// <summary>
        /// 发送打印内容
        /// </summary>
        /// <param name="content">打印内容</param>
        public void SendPrintContent(byte[] content)
        {
            if (!IsOpen())
            {
                Log.Warning("打印机串口未打开");
                return;
            }

            SendData(content);
        }

        /// <summary>
        /// 获取打印机状态
        /// </summary>
        public void GetStatus()
        {
            if (!IsOpen())
            {
                Log.Warning("打印机串口未打开");
                OnStatusReceived(false, "打印机串口未打开");
                failedAction?.Invoke("打印机串口未打开");
                return;
            }

            if (IsWaitingStatus)
            {
                Log.Warning("正在等待打印机状态响应");
                return;
            }

            IsWaitingStatus = true;
            // 发送获取状态命令
            SendData(GetStatusCommand);
            // 启动超时定时器
            statusTimeoutTimer.Start();
        }
    }
}
