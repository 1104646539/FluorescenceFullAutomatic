using System;
using FluorescenceFullAutomatic.Config;
using Serilog;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// HL7配置更新服务
    /// 提供修改HL7通信配置的功能
    /// </summary>
    public class HL7ConfigService
    {
        private static HL7ConfigService _instance;
        private static readonly object _lockObject = new object();
        private readonly HL7Helper _hl7Helper;

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private HL7ConfigService()
        {
            _hl7Helper = HL7Helper.Instance;
        }

        /// <summary>
        /// 获取HL7ConfigService实例（单例模式）
        /// </summary>
        /// <returns>HL7ConfigService实例</returns>
        public static HL7ConfigService GetInstance()
        {
            if (_instance == null)
            {
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = new HL7ConfigService();
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// 更新配置并重启服务
        /// 外部类修改UploadConfig.Instance后调用此方法应用更改
        /// </summary>
        public void UpdateConfig()
        {
            try
            {
                // 特殊处理OpenUpload参数
                if (!UploadConfig.Instance.OpenUpload)
                {
                    // 如果禁用上传，只停止HL7服务
                    Log.Information("HL7上传功能已禁用，停止服务");
                    _hl7Helper.Stop();
                    return;
                }
                
                // 重启服务
                Log.Information("应用HL7配置更改，重启服务");
                
                // 如果服务正在运行，先停止
                if (_hl7Helper.IsRunning())
                {
                    _hl7Helper.Stop();
                }
                
                // 重新初始化服务（应用新配置）
                _hl7Helper.InitializeService();
                
                // 启动服务
                _hl7Helper.Start();
                
                Log.Information("HL7配置更新完成");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新HL7配置时发生错误");
            }
        }
        
        /// <summary>
        /// 启用或禁用HL7上传功能
        /// 这是一个便捷方法，直接修改配置并应用更改
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void SetUploadEnabled(bool enable)
        {
            try
            {
                UploadConfig.Instance.OpenUpload = enable;
                UpdateConfig();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"设置HL7上传功能状态为{enable}时发生错误");
            }
        }
        
        /// <summary>
        /// 设置通信方式（串口或网络）
        /// </summary>
        /// <param name="useSerialPort">是否使用串口</param>
        public void SetCommunicationType(bool useSerialPort)
        {
            try
            {
                if (UploadConfig.Instance.SerialPort != useSerialPort)
                {
                    UploadConfig.Instance.SerialPort = useSerialPort;
                    Log.Information($"通信方式已更改为: {(useSerialPort ? "串口" : "网络")}");
                    UpdateConfig();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"设置通信方式为{(useSerialPort ? "串口" : "网络")}时发生错误");
            }
        }
        
        /// <summary>
        /// 设置串口参数
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBit">数据位</param>
        /// <param name="stopBit">停止位</param>
        /// <param name="parity">校验位</param>
        public void SetSerialPortConfig(string portName, string baudRate, string dataBit, string stopBit, string parity)
        {
            try
            {
                bool changed = false;
                
                if (UploadConfig.Instance.SerialPortName != portName)
                {
                    UploadConfig.Instance.SerialPortName = portName;
                    changed = true;
                }
                
                if (UploadConfig.Instance.BaudRate != baudRate)
                {
                    UploadConfig.Instance.BaudRate = baudRate;
                    changed = true;
                }
                
                if (UploadConfig.Instance.DataBit != dataBit)
                {
                    UploadConfig.Instance.DataBit = dataBit;
                    changed = true;
                }
                
                if (UploadConfig.Instance.StopBit != stopBit)
                {
                    UploadConfig.Instance.StopBit = stopBit;
                    changed = true;
                }
                
                if (UploadConfig.Instance.OddEven != parity)
                {
                    UploadConfig.Instance.OddEven = parity;
                    changed = true;
                }
                
                if (changed)
                {
                    Log.Information("串口参数已更新");
                    // 如果当前使用的是串口，则需要重启服务应用新配置
                    if (UploadConfig.Instance.SerialPort)
                    {
                        UpdateConfig();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "设置串口参数时发生错误");
            }
        }
        
        /// <summary>
        /// 设置网络参数
        /// </summary>
        /// <param name="serverIp">服务器IP地址</param>
        /// <param name="serverPort">服务器端口</param>
        public void SetNetworkConfig(string serverIp, string serverPort)
        {
            try
            {
                bool changed = false;
                
                if (UploadConfig.Instance.ServiceIP != serverIp)
                {
                    UploadConfig.Instance.ServiceIP = serverIp;
                    changed = true;
                }
                
                if (UploadConfig.Instance.ServicePort != serverPort)
                {
                    UploadConfig.Instance.ServicePort = serverPort;
                    changed = true;
                }
                
                if (changed)
                {
                    Log.Information("网络参数已更新");
                    // 如果当前使用的是网络连接，则需要重启服务应用新配置
                    if (!UploadConfig.Instance.SerialPort)
                    {
                        UpdateConfig();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "设置网络参数时发生错误");
            }
        }
        
        /// <summary>
        /// 设置通用HL7参数
        /// </summary>
        /// <param name="charset">字符编码</param>
        /// <param name="timeout">超时时间(毫秒)</param>
        /// <param name="retryCount">重试次数</param>
        public void SetHL7Config(string charset, int timeout, int retryCount)
        {
            try
            {
                bool changed = false;
                
                if (UploadConfig.Instance.Charset != charset)
                {
                    UploadConfig.Instance.Charset = charset;
                    changed = true;
                }
                
                if (UploadConfig.Instance.Overtime != timeout)
                {
                    UploadConfig.Instance.Overtime = timeout;
                    changed = true;
                }
                
                if (UploadConfig.Instance.OvertimeRetryCount != retryCount)
                {
                    UploadConfig.Instance.OvertimeRetryCount = retryCount;
                    changed = true;
                }
                
                if (changed)
                {
                    Log.Information("HL7通用参数已更新");
                    UpdateConfig();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "设置HL7通用参数时发生错误");
            }
        }
        
        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        /// <returns>连接是否正常</returns>
        public bool IsConnected()
        {
            try
            {
                return _hl7Helper.IsConnected();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取连接状态时发生错误");
                return false;
            }
        }
        
        /// <summary>
        /// 获取服务运行状态
        /// </summary>
        /// <returns>服务是否正在运行</returns>
        public bool IsServiceRunning()
        {
            try
            {
                return _hl7Helper.IsRunning();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取服务运行状态时发生错误");
                return false;
            }
        }
    }
} 