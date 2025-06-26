using System;
using FluorescenceFullAutomatic.Config;
using Serilog;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// HL7���ø��·���
    /// �ṩ�޸�HL7ͨ�����õĹ���
    /// </summary>
    public class HL7ConfigService
    {
        private static HL7ConfigService _instance;
        private static readonly object _lockObject = new object();
        private readonly HL7Helper _hl7Helper;

        /// <summary>
        /// ˽�й��캯������ֹ�ⲿʵ����
        /// </summary>
        private HL7ConfigService()
        {
            _hl7Helper = HL7Helper.Instance;
        }

        /// <summary>
        /// ��ȡHL7ConfigServiceʵ��������ģʽ��
        /// </summary>
        /// <returns>HL7ConfigServiceʵ��</returns>
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
        /// �������ò���������
        /// �ⲿ���޸�UploadConfig.Instance����ô˷���Ӧ�ø���
        /// </summary>
        public void UpdateConfig()
        {
            try
            {
                // ���⴦��OpenUpload����
                if (!UploadConfig.Instance.OpenUpload)
                {
                    // ��������ϴ���ֹֻͣHL7����
                    Log.Information("HL7�ϴ������ѽ��ã�ֹͣ����");
                    _hl7Helper.Stop();
                    return;
                }
                
                // ��������
                Log.Information("Ӧ��HL7���ø��ģ���������");
                
                // ��������������У���ֹͣ
                if (_hl7Helper.IsRunning())
                {
                    _hl7Helper.Stop();
                }
                
                // ���³�ʼ������Ӧ�������ã�
                _hl7Helper.InitializeService();
                
                // ��������
                _hl7Helper.Start();
                
                Log.Information("HL7���ø������");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "����HL7����ʱ��������");
            }
        }
        
        /// <summary>
        /// ���û����HL7�ϴ�����
        /// ����һ����ݷ�����ֱ���޸����ò�Ӧ�ø���
        /// </summary>
        /// <param name="enable">�Ƿ�����</param>
        public void SetUploadEnabled(bool enable)
        {
            try
            {
                UploadConfig.Instance.OpenUpload = enable;
                UpdateConfig();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"����HL7�ϴ�����״̬Ϊ{enable}ʱ��������");
            }
        }
        
        /// <summary>
        /// ����ͨ�ŷ�ʽ�����ڻ����磩
        /// </summary>
        /// <param name="useSerialPort">�Ƿ�ʹ�ô���</param>
        public void SetCommunicationType(bool useSerialPort)
        {
            try
            {
                if (UploadConfig.Instance.SerialPort != useSerialPort)
                {
                    UploadConfig.Instance.SerialPort = useSerialPort;
                    Log.Information($"ͨ�ŷ�ʽ�Ѹ���Ϊ: {(useSerialPort ? "����" : "����")}");
                    UpdateConfig();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"����ͨ�ŷ�ʽΪ{(useSerialPort ? "����" : "����")}ʱ��������");
            }
        }
        
        /// <summary>
        /// ���ô��ڲ���
        /// </summary>
        /// <param name="portName">��������</param>
        /// <param name="baudRate">������</param>
        /// <param name="dataBit">����λ</param>
        /// <param name="stopBit">ֹͣλ</param>
        /// <param name="parity">У��λ</param>
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
                    Log.Information("���ڲ����Ѹ���");
                    // �����ǰʹ�õ��Ǵ��ڣ�����Ҫ��������Ӧ��������
                    if (UploadConfig.Instance.SerialPort)
                    {
                        UpdateConfig();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "���ô��ڲ���ʱ��������");
            }
        }
        
        /// <summary>
        /// �����������
        /// </summary>
        /// <param name="serverIp">������IP��ַ</param>
        /// <param name="serverPort">�������˿�</param>
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
                    Log.Information("��������Ѹ���");
                    // �����ǰʹ�õ����������ӣ�����Ҫ��������Ӧ��������
                    if (!UploadConfig.Instance.SerialPort)
                    {
                        UpdateConfig();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "�����������ʱ��������");
            }
        }
        
        /// <summary>
        /// ����ͨ��HL7����
        /// </summary>
        /// <param name="charset">�ַ�����</param>
        /// <param name="timeout">��ʱʱ��(����)</param>
        /// <param name="retryCount">���Դ���</param>
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
                    Log.Information("HL7ͨ�ò����Ѹ���");
                    UpdateConfig();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "����HL7ͨ�ò���ʱ��������");
            }
        }
        
        /// <summary>
        /// ��ȡ��ǰ����״̬
        /// </summary>
        /// <returns>�����Ƿ�����</returns>
        public bool IsConnected()
        {
            try
            {
                return _hl7Helper.IsConnected();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "��ȡ����״̬ʱ��������");
                return false;
            }
        }
        
        /// <summary>
        /// ��ȡ��������״̬
        /// </summary>
        /// <returns>�����Ƿ���������</returns>
        public bool IsServiceRunning()
        {
            try
            {
                return _hl7Helper.IsRunning();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "��ȡ��������״̬ʱ��������");
                return false;
            }
        }
    }
} 