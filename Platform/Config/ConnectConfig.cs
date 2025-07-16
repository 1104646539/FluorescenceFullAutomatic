using FluorescenceFullAutomatic.Platform.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Config
{
    public partial class ConnectConfig : JsonConfigBase
    {
        private static ConnectConfig _instance;

        // 单例模式
        public static ConnectConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConnectConfig();
                }
                return _instance;
            }
        }

        private ConnectConfig() : base()
        {
        }
        /// <summary>
        /// 是否双向通讯
        /// </summary>
        private bool _twoWay = false;
        public bool TwoWay
        {
            get => _twoWay;
            set
            {
                if (_twoWay != value)
                {
                    _twoWay = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 是否使用连接
        /// </summary>
        private bool _enable = false;
        public bool Enable
        {
            get => _enable;
            set
            {
                if (_enable != value)
                {
                    _enable = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 串口名称 上传
        /// </summary>
        private string _connectPortName = "COM16";
        public string ConnectPortName
        {
            get => _connectPortName;
            set
            {
                if (_connectPortName != value)
                {
                    _connectPortName = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 串口波特率 上传
        /// </summary>
        private int _connectPortBaudRate = 9600;
        public int ConnectPortBaudRate
        {
            get => _connectPortBaudRate;
            set
            {
                if (_connectPortBaudRate != value)
                {
                    _connectPortBaudRate = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// IP 上传
        /// </summary>
        private string _ip = "192.168.1.1";
        public string IP
        {
            get => _ip;
            set
            {
                if (_ip != value)
                {
                    _ip = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 端口 上传
        /// </summary>
        private string _port = "6699";
        public string Port
        {
            get => _port;
            set
            {
                if (_port != value)
                {
                    _port = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 是否启动响应 上传
        /// </summary>
        private bool _enableResponse = true;
        public bool EnableResponse
        {
            get => _enableResponse;
            set
            {
                if (_enableResponse != value)
                {
                    _enableResponse = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 是否开启信息匹配 
        /// </summary>
        private bool _enableMatchInfo = false;
        public bool EnableMatchInfo
        {
            get => _enableMatchInfo;
            set
            {
                if (_enableMatchInfo != value)
                {
                    _enableMatchInfo = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 是否是使用条码匹配 
        /// </summary>
        private bool _matchBarcode = true;
        public bool MatchBarcode
        {
            get => _matchBarcode;
            set
            {
                if (_matchBarcode != value)
                {
                    _matchBarcode = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 串口名称 上传
        /// </summary>
        //private string _idfa = "COM16";
        //public string sdfs
        //{
        //    get => _idfa;
        //    set
        //    {
        //        if (_idfa != value)
        //        {
        //            _idfa = value;
        //            MarkDirty();
        //        }
        //    }
        //}

    }
}
