using System;
using FluorescenceFullAutomatic.Config;
using Serilog;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// HL7ͨ�ŷ��񹤳���
    /// �������ô�����Ӧ��ͨ�ŷ���ʵ��
    /// </summary>
    public class HL7CommunicationFactory
    {
        private static HL7CommunicationFactory _instance;
        private static readonly object _lockObject = new object();
        private IHL7CommunicationService _communicationService;

        /// <summary>
        /// ˽�й��캯������ֹ�ⲿʵ����
        /// </summary>
        private HL7CommunicationFactory()
        {
        }

        /// <summary>
        /// ��ȡ����ʵ��������ģʽ��
        /// </summary>
        /// <returns>����ʵ��</returns>
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
        /// ����HL7ͨ�ŷ���ʵ��
        /// </summary>
        /// <returns>HL7ͨ�ŷ���ʵ��</returns>
        public IHL7CommunicationService CreateCommunicationService()
        {
            try
            {
                if (_communicationService != null)
                {
                    return _communicationService;
                }

                // ��������ȷ��ʹ������ͨ�ŷ�ʽ
                bool useSerialPort = UploadConfig.Instance.SerialPort;

                if (useSerialPort)
                {
                    _communicationService = new SerialCommunicationService();
                    Log.Information("�Ѵ�������ͨ�ŷ���");
                }
                else
                {
                    _communicationService = new TcpCommunicationService();
                    Log.Information("�Ѵ���TCP����ͨ�ŷ���");
                }

                return _communicationService;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "����ͨ�ŷ���ʵ��ʱ��������");
                throw;
            }
        }

        /// <summary>
        /// ���´���HL7ͨ�ŷ���ʵ��
        /// </summary>
        /// <returns>HL7ͨ�ŷ���ʵ��</returns>
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
                    Log.Error(ex, "�Ͽ�������ʱ��������");
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