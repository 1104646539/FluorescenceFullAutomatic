using System;
using FluorescenceFullAutomatic.Config;
using Serilog;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// HL7通信服务工厂类
    /// 根据配置创建对应的通信服务实例
    /// </summary>
    public class HL7CommunicationFactory
    {
        private static HL7CommunicationFactory _instance;
        private static readonly object _lockObject = new object();
        private IHL7CommunicationService _communicationService;

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private HL7CommunicationFactory()
        {
        }

        /// <summary>
        /// 获取工厂实例（单例模式）
        /// </summary>
        /// <returns>工厂实例</returns>
        public static HL7CommunicationFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new HL7CommunicationFactory();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 创建HL7通信服务实例
        /// </summary>
        /// <returns>HL7通信服务实例</returns>
        public IHL7CommunicationService CreateCommunicationService()
        {
            try
            {
                if (_communicationService != null)
                {
                    return _communicationService;
                }

                // 根据配置确定使用哪种通信方式
                bool useSerialPort = UploadConfig.Instance.SerialPort;

                if (useSerialPort)
                {
                    _communicationService = new SerialCommunicationService();
                    Log.Information("已创建串口通信服务");
                }
                else
                {
                    _communicationService = new TcpCommunicationService();
                    Log.Information("已创建TCP网络通信服务");
                }

                return _communicationService;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建通信服务实例时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 重新创建HL7通信服务实例
        /// </summary>
        /// <returns>HL7通信服务实例</returns>
        public IHL7CommunicationService RecreateService()
        {
            if (_communicationService != null)
            {
                try
                {
                    _communicationService.Disconnect();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "断开旧连接时发生错误");
                }
                finally
                {
                    _communicationService = null;
                }
            }

            return CreateCommunicationService();
        }
    }
} 