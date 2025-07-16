using FluorescenceFullAutomatic.Core.Config;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FluorescenceFullAutomatic.Platform.Utils
{
    public class BarcodeHelper : ISerialPort
    {
        private static readonly Lazy<BarcodeHelper> _instance = new Lazy<BarcodeHelper>(
            () => SystemGlobal.IsCodeDebug ? new BarcodeHelper(
                  new FakaBarcodeSerialPort()
                ) : new BarcodeHelper(
                  new SerialPortImpl()
                ));

        /// <summary>
        /// 收到数据
        /// </summary>

        public event Action<string> ScanSuccess;
        public event Action<string> ScanFailed;
        public event Action<byte[]> DataReceived;
        public event Action<string> SerialPortConnectReceived;
        public event Action<string> SerialPortOriginDataReceived;
        public event Action<string> SerialPortOriginDataSend;
        public event Action<string> SerialPortExceptionReceived;

        public static BarcodeHelper Instance => _instance.Value;
        readonly ISerialPort SerialPort;
        private Timer scanTimeoutTimer;
        private const int SCAN_TIMEOUT_MS = 3000; // 3秒超时
        /// <summary>
        /// 是否正在扫码
        /// </summary>
        private bool IsScanning = false;

        /// <summary>
        /// 开始扫码命令
        /// </summary>
        private readonly byte[] StartScanCommand = new byte[] { 0x02, 0x2B, 0x0D, 0x0A };

        /// <summary>
        /// 停止扫码命令
        /// </summary>
        private readonly byte[] StopScanCommand = new byte[] { 0x02, 0x2D, 0x0D, 0x0A };

        /// <summary>
        /// 关闭扫码返回的命令
        /// </summary>
        private readonly byte[] CloseReturnCommand = new byte[] { 0x02, 0x3F, 0x0D, 0x0A };

        /// <summary>
        /// 数据接收缓冲区
        /// </summary>
        private List<byte> _receiveBuffer = new List<byte>();

        private BarcodeHelper(ISerialPort serialPort)
        {
            this.SerialPort = serialPort;
            this.SerialPort.DataReceived += SerialPort_DataReceived;
            this.SerialPort.SerialPortConnectReceived += SerialPort_SerialPortConnectReceived;
            
            // 初始化定时器
            scanTimeoutTimer = new Timer(SCAN_TIMEOUT_MS);
            scanTimeoutTimer.Elapsed += ScanTimeoutTimer_Elapsed;
            scanTimeoutTimer.AutoReset = false;
        }

        private void ScanTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log.Warning("ScanTimeoutTimer_Elapsed");
            if (IsScanning)
            {
                Log.Warning("扫码超时，发起关闭扫码命令");
                StopScan();
                OnScanFailed("扫码超时");
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

            // 查找0x0A的位置
            int endIndex = _receiveBuffer.IndexOf(0x0A);
            while (endIndex != -1)
            {
                // 提取一条完整的数据（包含0x0A）
                byte[] completeData = _receiveBuffer.GetRange(0, endIndex + 1).ToArray();
                
                // 从缓冲区中移除已处理的数据
                _receiveBuffer.RemoveRange(0, endIndex + 1);

                // 处理完整的数据
                ProcessCompleteData(completeData);

                // 继续查找下一条数据
                endIndex = _receiveBuffer.IndexOf(0x0A);
            }
        }

        private void ProcessCompleteData(byte[] data)
        {
            if (data.Length < 3)
                return;

            // 检查起始和结束字节
            if (data[0] != 0x02 || data[data.Length - 2] != 0x0D || data[data.Length - 1] != 0x0A)
                return;

            if(SerialUtils.AreByteArraysEqual(data, CloseReturnCommand)){
                // 关闭扫码返回的命令
                Log.Information("扫码枪关闭成功");
                return;
            }

            string hexString = BitConverter.ToString(data).Replace("-", " ");
            Log.Information($"收到条码：{hexString}");

            // 提取中间的内容（去掉起始字节0x02和结束字节0x0D,0x0A）
            byte[] contentBytes = new byte[data.Length - 3];
            Array.Copy(data, 1, contentBytes, 0, data.Length - 3);

            // 将字节数组转换为字符串
            string barcodeContent = Encoding.ASCII.GetString(contentBytes);

            // 触发条码接收事件
            OnBarcodeReceived(barcodeContent);
        }

        public void OnBarcodeReceived(string barcodeContent)
        {
            if(IsScanning == false)
            {
                Log.Warning($"扫码未开始,收到未知信息:{barcodeContent}");
                return;
            }
            //Log.Information("收到条码：{BarcodeContent}", barcodeContent);
            // 停止定时器
            IsScanning = false;
            scanTimeoutTimer.Stop();
            OnScanSuccess(barcodeContent);
            
        }

        public void OnScanSuccess(string barcodeContent)
        {
            //Log.Information("扫码成功：{BarcodeContent}", barcodeContent);
            ScanSuccess?.Invoke(barcodeContent);
        }

        public void OnScanFailed(string error)
        {

            IsScanning = false;
            //Log.Information("扫码失败：{error}", error);
            ScanFailed?.Invoke(error);
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

       
        /// 开始扫码
        /// </summary>
        public void StartScan() {
            if (IsScanning)
            {
                Log.Warning("扫码已经开始");
                return;
            }
            SendData(StartScanCommand);
            IsScanning = true;
            // 启动定时器
            Log.Warning("启动定时器 ScanTimeoutTimer_Elapsed");
            scanTimeoutTimer.Start();
        }

        /// <summary>
        /// 停止扫码
        /// </summary>
        public void StopScan(){
            if (!IsScanning)
            {
                Log.Warning("扫码未开始");
                return;
            }
            SendData(StopScanCommand);
            // 停止定时器
            scanTimeoutTimer.Stop();
            
        }

     
    }
}
