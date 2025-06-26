using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Upload;

namespace FluorescenceFullAutomatic.Services
{
    public interface IUploadService
    {
        // 基本配置
        bool GetOpenUpload();
        void SetOpenUpload(bool value);
        
        bool GetAutoUpload();
        void SetAutoUpload(bool value);
        
        bool GetAutoReconnection();
        void SetAutoReconnection(bool value);
        
        int GetUploadIntervalTime();
        void SetUploadIntervalTime(int value);
        
        bool GetTwoWay();
        void SetTwoWay(bool value);
        
        int GetOvertimeRetryCount();
        void SetOvertimeRetryCount(int value);
        
        int GetOvertime();
        void SetOvertime(int value);
        
        bool GetAutoGetApplyTest();
        void SetAutoGetApplyTest(bool value);
        
        bool GetMatchBarcode();
        void SetMatchBarcode(bool value);
        
        // 通讯方式
        bool GetSerialPort();
        void SetSerialPort(bool value);
        
        // 编码设置
        string GetCharset();
        void SetCharset(string value);
        
        // 串口设置
        string GetBaudRate();
        void SetBaudRate(string value);
        
        string GetDataBit();
        void SetDataBit(string value);
        
        string GetStopBit();
        void SetStopBit(string value);
        
        string GetOddEven();
        void SetOddEven(string value);
        
        // 网口设置
        string GetServiceIP();
        void SetServiceIP(string value);
        
        string GetServicePort();
        void SetServicePort(string value);
        
        // 连接管理
        bool Connect();
        bool Disconnect();
        bool IsConnected();
        
        // 保存所有设置
        void SaveAllSettings(
            bool openUpload,
            bool autoUpload,
            bool autoReconnection,
            int uploadIntervalTime,
            bool twoWay,
            int overtimeRetryCount,
            int overtime,
            bool autoGetApplyTest,
            bool matchBarcode,
            bool serialPort,
            string charset,
            string baudRate,
            string dataBit,
            string stopBit,
            string oddEven,
            string serviceIP,
            string servicePort
        );
    }
    
    public class UploadService : IUploadService
    {
        private bool _isConnected = false;
        
        // 基本配置
        public bool GetOpenUpload() => UploadConfig.Instance.OpenUpload;
        public void SetOpenUpload(bool value) => UploadConfig.Instance.OpenUpload = value;
        
        public bool GetAutoUpload() => UploadConfig.Instance.AutoUpload;
        public void SetAutoUpload(bool value) => UploadConfig.Instance.AutoUpload = value;
        
        public bool GetAutoReconnection() => UploadConfig.Instance.AutoReconnection;
        public void SetAutoReconnection(bool value) => UploadConfig.Instance.AutoReconnection = value;
        
        public int GetUploadIntervalTime() => UploadConfig.Instance.UploadIntervalTime;
        public void SetUploadIntervalTime(int value) => UploadConfig.Instance.UploadIntervalTime = value;
        
        public bool GetTwoWay() => UploadConfig.Instance.TwoWay;
        public void SetTwoWay(bool value) => UploadConfig.Instance.TwoWay = value;
        
        public int GetOvertimeRetryCount() => UploadConfig.Instance.OvertimeRetryCount;
        public void SetOvertimeRetryCount(int value) => UploadConfig.Instance.OvertimeRetryCount = value;
        
        public int GetOvertime() => UploadConfig.Instance.Overtime;
        public void SetOvertime(int value) => UploadConfig.Instance.Overtime = value;
        
        public bool GetAutoGetApplyTest() => UploadConfig.Instance.AutoGetApplyTest;
        public void SetAutoGetApplyTest(bool value) => UploadConfig.Instance.AutoGetApplyTest = value;
        
        public bool GetMatchBarcode() => UploadConfig.Instance.MatchBarcode;
        public void SetMatchBarcode(bool value) => UploadConfig.Instance.MatchBarcode = value;
        
        // 通讯方式
        public bool GetSerialPort() => UploadConfig.Instance.SerialPort;
        public void SetSerialPort(bool value) => UploadConfig.Instance.SerialPort = value;
        
        // 编码设置
        public string GetCharset() => UploadConfig.Instance.Charset;
        public void SetCharset(string value) => UploadConfig.Instance.Charset = value;
        
        // 串口设置
        public string GetBaudRate() => UploadConfig.Instance.BaudRate;
        public void SetBaudRate(string value) => UploadConfig.Instance.BaudRate = value;
        
        public string GetDataBit() => UploadConfig.Instance.DataBit;
        public void SetDataBit(string value) => UploadConfig.Instance.DataBit = value;
        
        public string GetStopBit() => UploadConfig.Instance.StopBit;
        public void SetStopBit(string value) => UploadConfig.Instance.StopBit = value;
        
        public string GetOddEven() => UploadConfig.Instance.OddEven;
        public void SetOddEven(string value) => UploadConfig.Instance.OddEven = value;
        
        // 网口设置
        public string GetServiceIP() => UploadConfig.Instance.ServiceIP;
        public void SetServiceIP(string value) => UploadConfig.Instance.ServiceIP = value;
        
        public string GetServicePort() => UploadConfig.Instance.ServicePort;
        public void SetServicePort(string value) => UploadConfig.Instance.ServicePort = value;
        
        // 连接管理
        public bool Connect()
        {
            try
            {
                // 这里实现连接逻辑
                // 模拟连接成功
                _isConnected = true;
                return true;
            }
            catch
            {
                _isConnected = false;
                return false;
            }
        }
        
        public bool Disconnect()
        {
            try
            {
                // 这里实现断开连接逻辑
                _isConnected = false;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public bool IsConnected() => _isConnected;
        
        // 保存所有设置
        public void SaveAllSettings(
            bool openUpload,
            bool autoUpload,
            bool autoReconnection,
            int uploadIntervalTime,
            bool twoWay,
            int overtimeRetryCount,
            int overtime,
            bool autoGetApplyTest,
            bool matchBarcode,
            bool serialPort,
            string charset,
            string baudRate,
            string dataBit,
            string stopBit,
            string oddEven,
            string serviceIP,
            string servicePort
        )
        {
            var config = UploadConfig.Instance;
            config.OpenUpload = openUpload;
            config.AutoUpload = autoUpload;
            config.AutoReconnection = autoReconnection;
            config.UploadIntervalTime = uploadIntervalTime;
            config.TwoWay = twoWay;
            config.OvertimeRetryCount = overtimeRetryCount;
            config.Overtime = overtime;
            config.AutoGetApplyTest = autoGetApplyTest;
            config.MatchBarcode = matchBarcode;
            config.SerialPort = serialPort;
            config.Charset = charset;
            config.BaudRate = baudRate;
            config.DataBit = dataBit;
            config.StopBit = stopBit;
            config.OddEven = oddEven;
            config.ServiceIP = serviceIP;
            config.ServicePort = servicePort;

            HL7Helper.Instance.InitializeService();
        }
    }
}
