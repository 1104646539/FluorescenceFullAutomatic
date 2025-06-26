using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FluorescenceFullAutomatic.Utils
{
    /// <summary>
    /// 用来模拟扫码枪串口返回
    /// </summary>
    public class FakaBarcodeSerialPort : ISerialPort
    {
        private System.Timers.Timer _responseTimer;
        private bool _isConnected;
        private string _portName;
        private int _baudRate;
        private bool _isScanning;
        private readonly Random _random = new Random();

        public event Action<byte[]> DataReceived;
        public event Action<string> SerialPortConnectReceived;
        public event Action<string> SerialPortConnectExceptionReceived;
        public event Action<string> SerialPortOriginDataReceived;
        public event Action<string> SerialPortOriginDataSend;
        public event Action<string> SerialPortExceptionReceived;

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
        // 模拟的条码数据
        private readonly string[] _fakeBarcodes = new[]
        {
            "ABCD0",
            "ABCD1",
            "ABCD2",
            "ABCD3",
            "ABCD4",
        };
        private readonly bool[] _fakeScanSuccess = new[]
       {
            true,
            true,
            true,
            true,
            true,
        };
        public FakaBarcodeSerialPort()
        {
            _responseTimer = new System.Timers.Timer(1000); // 1秒响应时间
            _responseTimer.Elapsed += ResponseTimer_Elapsed;
            _responseTimer.AutoReset = false;
        }
        int index =  -1;
        private void ResponseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Log.Information($"ResponseTimer_Elapsed2 {_isConnected} {_isScanning}");
            if (!_isConnected || !_isScanning) return;
            try
            {
                index = index % _fakeScanSuccess.Length;
                Log.Information($"ResponseTimer_Elapsed index=${index}");
                if (_fakeScanSuccess[index]) {
                    string barcode = _fakeBarcodes[index];

                    // 构造响应数据：0x02 + 条码内容 + 0x0D + 0x0A
                    byte[] response = new byte[barcode.Length + 3];
                    response[0] = 0x02; // 起始字节
                    Encoding.ASCII.GetBytes(barcode).CopyTo(response, 1);
                    response[response.Length - 2] = 0x0D; // 结束字节
                    response[response.Length - 1] = 0x0A; // 结束字节

                    // 触发数据接收事件
                    DataReceived?.Invoke(response);
                }
                else {
                    //扫码超时，扫码失败
                    //App.Current.Dispatcher.Invoke(() =>
                    //{
                    //    // 触发数据接收事件
                    //    DataReceived?.Invoke(response);
                    //});
                }
            }
            catch (Exception ex)
            {
                SerialPortConnectExceptionReceived?.Invoke($"模拟条码扫描错误: {ex.Message}");
            }
        }

        public void Connect(string portName, int baudRate)
        {
            _portName = portName;
            _baudRate = baudRate;
            _isConnected = true;
            SerialPortConnectReceived?.Invoke("");
        }

        public void Disconnect()
        {
            _isConnected = false;
            _isScanning = false;
            _responseTimer.Stop();
        }

        public bool IsOpen()
        {
            return _isConnected;
        }

        public void SendData(string data)
        {
            if (!_isConnected) return;

        }

        public void SendData(byte[] data)
        {
            //Log.Information($"SendData {_isConnected}");
            if (!_isConnected) return;
            // 模拟处理扫码命令
            if (SerialUtils.AreByteArraysEqual(data,StartScanCommand)) // 开始扫码命令
            {
                index++;
                _isScanning = true;
                _responseTimer.Start();
            }
            else if (SerialUtils.AreByteArraysEqual(data, StopScanCommand)) // 停止扫码命令
            {
                DataReceived?.Invoke(CloseReturnCommand);
                _isScanning = false;
                _responseTimer.Stop();

            }
        }
    }
}
