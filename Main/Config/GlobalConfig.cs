using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Config
{
    public class GlobalConfig : JsonConfigBase
    {
        private static GlobalConfig _instance;

        // 单例模式
        public static GlobalConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GlobalConfig();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 串口名 下位机
        /// </summary>
        private string _mainPortName = "COM29";
        public string MainPortName
        {
            get => _mainPortName;
            set
            {
                if (_mainPortName != value)
                {
                    _mainPortName = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 串口波特率 下位机
        /// </summary>
        private int _mainPortBaudRate = 115200;
        public int MainPortBaudRate
        {
            get => _mainPortBaudRate;
            set
            {
                if (_mainPortBaudRate != value){
                    _mainPortBaudRate = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 串口名称 条码
        /// </summary>
        private string _barcodePortName = "COM16";
        public string BarcodePortName
        {
            get => _barcodePortName;
            set
            {
                if (_barcodePortName != value)
                {
                    _barcodePortName = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 串口波特率 条码
        /// </summary>
        private int _barcodePortBaudRate = 9600;
        public int BarcodePortBaudRate
        {
            get => _barcodePortBaudRate;
            set{
                if (_barcodePortBaudRate != value){
                    _barcodePortBaudRate = value;
                    MarkDirty();
                }
            }
        }
          /// <summary>
        /// 串口名称 小票
        /// </summary>
        private string _ticketPortName = "COM18";
        public string TicketPortName
        {
            get => _ticketPortName;
            set
            {
                if (_ticketPortName != value)
                {
                    _ticketPortName = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 串口波特率 小票
        /// </summary>
        private int _ticketPortBaudRate = 9600;
        public int TicketPortBaudRate
        {
            get => _ticketPortBaudRate;
            set{
                if (_ticketPortBaudRate != value){
                    _ticketPortBaudRate = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 检测编号
        /// </summary>
        private int _testNum = 1;
        public int TestNum
        {
            get => _testNum;
            set{
                if (_testNum != value){
                    _testNum = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 加样量
        /// </summary>
        private int _samplingVolume = 100;
        public int SamplingVolume
        {
            get => _samplingVolume;
            set{
                if (_samplingVolume != value){
                    _samplingVolume = value;
                    MarkDirty();
                }
            }
        }
        
        /// <summary>
        /// 是否扫码
        /// </summary>
        private bool _scanBarcode = true;
        public bool ScanBarcode
        {
            get => _scanBarcode;
            set{
                if (_scanBarcode != value){
                    _scanBarcode = value;
                    MarkDirty();
                }
            }
        }
        
        /// <summary>
        /// 是否自动打印小票
        /// </summary>
        private bool _autoPrintTicket = false;
        public bool AutoPrintTicket
        {
            get => _autoPrintTicket;
            set{
                if (_autoPrintTicket != value){
                    _autoPrintTicket = value;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 是否自动打印A4报告
        /// </summary>
        private bool _autoPrintA4Report = false;
        public bool AutoPrintA4Report
        {
            get => _autoPrintA4Report;
            set{
                if (_autoPrintA4Report != value){
                    _autoPrintA4Report = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 打印机
        /// </summary>
        private string _printerName = "";
        public string PrinterName
        {
            get => _printerName;
            set
            {
                if (_printerName != value)
                {
                    _printerName = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 医院名称
        /// </summary>
        private string _hospitalName = "xxx机构";
        public string HospitalName
        {
            get => _hospitalName;
            set
            {
                if (_hospitalName != value)
                {
                    _hospitalName = value;
                    MarkDirty();
                }
            }
        }
        
        /// <summary>
        /// A4模板路径
        /// </summary>
        private string _reportTemplatePath = "";
        public string ReportTemplatePath
        {
            get => _reportTemplatePath;
            set
            {
                if (_reportTemplatePath != value)
                {
                    _reportTemplatePath = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// A4模板路径-双联卡
        /// </summary>
        private string _reportDoubleTemplatePath = "";
        public string ReportDoubleTemplatePath
        {
            get => _reportDoubleTemplatePath;
            set
            {
                if (_reportDoubleTemplatePath != value)
                {
                    _reportDoubleTemplatePath = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 清洗时长 取样针
        /// </summary>
        private int _cleanoutDuration = 1000;
        public int CleanoutDuration
        {
            get => _cleanoutDuration;
            set
            {
                if (_cleanoutDuration != value)
                {
                    _cleanoutDuration = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 反应时长
        /// </summary>
        private int _reactionDuration = 10;
        public int ReactionDuration
        {
            get => _reactionDuration;
            set
            {
                if (_reactionDuration != value)
                {
                    _reactionDuration = value;
                    MarkDirty();
                }
            }
        }
        /// <summary>
        /// 是否是调试模式。调试模式下可以显示调试功能
        /// </summary>
        public bool _isDebug = false;
        public bool IsDebug
        {
            get => _isDebug;
            set
            {
                if (_isDebug != value)
                {
                    _isDebug = value;
                    MarkDirty();
                }
            }
        }
        private GlobalConfig() : base()
        {
        }
    }
}
