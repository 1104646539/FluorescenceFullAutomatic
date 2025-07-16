using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Core.Config
{
    public class UploadConfig : JsonConfigBase
    {
        private static UploadConfig _instance;

        // 单例模式
        public static UploadConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UploadConfig();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 是否上传
        /// </summary>
        private bool openUpload = false;
        public bool OpenUpload
        {
            get => openUpload;
            set
            {
                if (openUpload != value)
                {
                    openUpload = value;
                    MarkDirty();
                }
            }
        }


        /// <summary>
        /// 自动上传
        /// </summary>
        private bool autoUpload = false;
        public bool AutoUpload
        {
            get => autoUpload;
            set
            {
                if (autoUpload != value)
                {
                    autoUpload = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 自动重连
        /// </summary>
        private bool autoReconnection = false;
        public bool AutoReconnection
        {
            get => autoReconnection;
            set
            {
                if (autoReconnection != value)
                {
                    autoReconnection = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 自动重连间隔时间
        /// </summary>
        private int autoReconnectionIntervalTime = 30;
        public int AutoReconnectionIntervalTime
        {
            get => autoReconnectionIntervalTime;
            set
            {
                if (autoReconnectionIntervalTime != value)
                {
                    autoReconnectionIntervalTime = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 查询回复总等待时间
        /// 查询收到Lis回复后，等待多少秒后才显示超时
        /// </summary>
        private int queryReplyIntervalTime = 30;
        public int QueryReplyIntervalTime
        {
            get => queryReplyIntervalTime;
            set
            {
                if (queryReplyIntervalTime != value)
                {
                    queryReplyIntervalTime = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 上传间隔时间
        /// </summary>
        private int uploadIntervalTime = 1000;
        public int UploadIntervalTime
        {
            get => uploadIntervalTime;
            set
            {
                if (uploadIntervalTime != value)
                {
                    uploadIntervalTime = value;
                    MarkDirty();
                }
            }
        }


        /// <summary>
        /// 是否双向上传
        /// </summary>
        private bool twoWay = false;
        public bool TwoWay
        {
            get => twoWay;
            set
            {
                if (twoWay != value)
                {
                    twoWay = value;
                    MarkDirty();
                }
            }
        }


        /// <summary>
        /// 超时重试次数
        /// </summary>
        private int overtimeRetryCount = 3;
        public int OvertimeRetryCount
        {
            get => overtimeRetryCount;
            set
            {
                if (overtimeRetryCount != value)
                {
                    overtimeRetryCount = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 超时时长
        /// </summary>
        private int overtime = 3000;
        public int Overtime
        {
            get => overtime;
            set
            {
                if (overtime != value)
                {
                    overtime = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 自动获取申请信息
        /// </summary>
        private bool autoGetApplyTest = false;
        public bool AutoGetApplyTest
        {
            get => autoGetApplyTest;
            set
            {
                if (autoGetApplyTest != value)
                {
                    autoGetApplyTest = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 匹配依据 true 条码 false 编号
        /// </summary>
        private bool matchBarcode = false;
        public bool MatchBarcode
        {
            get => matchBarcode;
            set
            {
                if (matchBarcode != value)
                {
                    matchBarcode = value;
                    MarkDirty();
                }
            }
        }


        /// <summary>
        /// 通讯方式 true 串口 false 网口
        /// </summary>
        private bool serialPort = false;
        public bool SerialPort
        {
            get => serialPort;
            set
            {
                if (serialPort != value)
                {
                    serialPort = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 字符编码 utf-8 gbk
        /// </summary>
        private string charset = "utf-8";
        public string Charset
        {
            get => charset;
            set
            {
                if (charset != value)
                {
                    charset = value;
                    MarkDirty();
                }
            }
        }
        
        /// <summary>
        /// 串口名
        /// </summary>
        private string serialPortName = "COM3";
        public string SerialPortName
        {
            get => serialPortName;
            set
            {
                if (serialPortName != value)
                {
                    serialPortName = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 串口 波特率
        /// </summary>
        private string baudRate = "115200";
        public string BaudRate
        {
            get => baudRate;
            set
            {
                if (baudRate != value)
                {
                    baudRate = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 串口 数据位
        /// </summary>
        private string dataBit = "8";
        public string DataBit
        {
            get => dataBit;
            set
            {
                if (dataBit != value)
                {
                    dataBit = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 串口 停止位
        /// </summary>
        private string stopBit = "1";
        public string StopBit
        {
            get => stopBit;
            set
            {
                if (stopBit != value)
                {
                    stopBit = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 串口 奇偶校验
        /// </summary>
        private string oddEven = "1";
        public string OddEven
        {
            get => oddEven;
            set
            {
                if (oddEven != value)
                {
                    oddEven = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 网口 IP
        /// </summary>
        private string serviceIP = "192.168.1.1";
        public string ServiceIP
        {
            get => serviceIP;
            set
            {
                if (serviceIP != value)
                {
                    serviceIP = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 网口 Port
        /// </summary>
        private string servicePort = "21110";
        public string ServicePort
        {
            get => servicePort;
            set
            {
                if (servicePort != value)
                {
                    servicePort = value;
                    MarkDirty();
                }
            }
        }
        private UploadConfig() : base()
        {
        }
    }
}
