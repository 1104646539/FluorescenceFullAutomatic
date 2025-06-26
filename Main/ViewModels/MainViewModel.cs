using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Ex;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Properties;
using FluorescenceFullAutomatic.Services;
using FluorescenceFullAutomatic.Utils;
using FluorescenceFullAutomatic.Views;
using FluorescenceFullAutomatic.Upload;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using Point = FluorescenceFullAutomatic.Model.Point;
using NHapi.Model.V21.Segment;
using System.IO;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string imgTemp;

        [ObservableProperty]
        private string imgUpload;

        [ObservableProperty]
        private string imgPrinter;

        [ObservableProperty]
        private string imgBarcode;

        [ObservableProperty]
        private string currentDate;

        [ObservableProperty]
        private string currentMsg;

        [ObservableProperty]
        private string currentStatus;

        [ObservableProperty]
        private string imgStartTest;
        private IConfigService configService;
        private readonly IDialogCoordinator _dialogCoordinator;
        private int _selectedIndex;
        Dictionary<int, int> notity_What = new Dictionary<int, int>() { };

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    OnPropertyChanged();
                    UpdateSelectedContent();
                }
            }
        }
        private string path = "../Image/";
        [ObservableProperty]
        public object selectedContent;
        /// <summary>
        /// 只用来更新时间的计时器
        /// </summary>
        private DispatcherTimer dispatcherTimer;
        public ObservableCollection<NavItem> NavItems { get; }

        public ICommand NavCommand { get; }
        
        private int _logoClickCount = 0;
        private DateTime _lastLogoClickTime = DateTime.MinValue;
        
        [RelayCommand]
        private void ClickLogo()
        {
            var now = DateTime.Now;
            if ((now - _lastLogoClickTime).TotalSeconds > 2)
            {
                _logoClickCount = 0;
            }
            
            _logoClickCount++;
            _lastLogoClickTime = now;
            
            if (_logoClickCount >= 10)
            {
                //更改调试模式
                _logoClickCount = 0;
                configService.SetDebugModeChnage();
                Task.Run(()=>{
                    Task.Delay(1000);
                    WeakReferenceMessenger.Default.Send(new EventMsg<string>(""){What = EventWhat.WHAT_CLICK_DEBUG_MODE});
                });
            }
        }
        
        public MainViewModel(
            IHomeService homeService,
            IConfigService configService,
            IDialogCoordinator dialogCoordinator
        )
        {
            this.configService = configService;
            _dialogCoordinator = dialogCoordinator;
            notity_What.Add(0, EventWhat.WHAT_CLICK_HOME);
            notity_What.Add(1, EventWhat.WHAT_CLICK_APPLY_TEST);
            notity_What.Add(2, EventWhat.WHAT_CLICK_DATA_MANAGER);
            notity_What.Add(3, EventWhat.WHAT_CLICK_QC);
            notity_What.Add(4, EventWhat.WHAT_CLICK_SETTINGS);
            InitSerialPort();
            InitBarcodeSerialPort();
            InitTicketSerialPort();
            InitConfig();
            test();

            NavItems = new ObservableCollection<NavItem>
            {
                new NavItem
                {
                    Header = GlobalUtil.GetString(Keys.HomeTitle),
                    Index = 0,
                    Content = new HomeView(),
                },
                new NavItem
                {
                    Header = GlobalUtil.GetString(Keys.ApplyTestTitle),
                    Index = 1,
                    Content = new ApplyTestView(),
                },
                new NavItem
                {
                    Header = GlobalUtil.GetString(Keys.DataManagerTitle),
                    Index = 2,
                    Content = new DataManagerView(),
                },
                new NavItem
                {
                    Header = GlobalUtil.GetString(Keys.QCTitle),
                    Index = 3,
                    Content = new QCView(),
                },
                new NavItem
                {
                    Header = GlobalUtil.GetString(Keys.SettingsTitle),
                    Index = 4,
                    Content = new SettingsView(),
                },
            };

            NavCommand = new RelayCommand<int>(ExecuteNavCommand);
            SelectedIndex = 0;
            UpdateSelectedContent();
            InitBottomStatus();
            RegisterMsg();
            InitHl7();
          
        }

        private void InitTicketSerialPort()
        {
             TicketReportUtil.Instance.SerialPortConnectReceived += (s) =>
            {
                Log.Information(string.IsNullOrEmpty(s) ? "小票打印机连接成功" : $"小票打印机连接失败 {s}");
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"小票打印机连接失败 {s}");
                    return;
                }
            };
            TicketReportUtil.Instance.SerialPortConnectExceptionReceived += (s) =>
            {
                Log.Information("小票打印机 exception " + s);
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"小票打印机连接失败2 {s}");
                    return;
                }
            };
            TicketReportUtil.Instance.Connect(
                configService.TicketPortName(),
                configService.TicketPortBaudRate()
            );
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        private void InitConfig()
        {
            if(string.IsNullOrEmpty(GlobalConfig.Instance.PrinterName)){
                //打印
                GlobalConfig.Instance.PrinterName = new System.Drawing.Printing.PrinterSettings().PrinterName;
            }
            if (!File.Exists(GlobalConfig.Instance.ReportTemplatePath))
            {
                //为空
                GlobalConfig.Instance.ReportTemplatePath = SystemGlobal.Template_Path;
            }
            if (!File.Exists(GlobalConfig.Instance.ReportDoubleTemplatePath))
            {
                //为空
                GlobalConfig.Instance.ReportDoubleTemplatePath = SystemGlobal.DoubleTemplate_Path;
            }
        }

        private void InitHl7()
        {
            //注册HL7连接状态监听事件
            HL7Helper.Instance.AddConnectionSucceededHandler(OnHL7ConnectionSucceeded);
            HL7Helper.Instance.AddConnectionClosedHandler(OnHL7ConnectionClosed);
            HL7Helper.Instance.AddConnectionFailedHandler(OnHL7ConnectionFailed);
            ////更新连接状态图标
            //UpdateUploadStatusIcon();
            //开始连接
            HL7Helper.Instance.InitializeService();
        }

        /// <summary>
        /// 析构函数，移除事件订阅
        /// </summary>
        ~MainViewModel()
        {
            // 移除HL7连接回调
            HL7Helper.Instance.RemoveConnectionSucceededHandler(OnHL7ConnectionSucceeded);
            HL7Helper.Instance.RemoveConnectionClosedHandler(OnHL7ConnectionClosed);
            HL7Helper.Instance.RemoveConnectionFailedHandler(OnHL7ConnectionFailed);
        }

        /// <summary>
        /// HL7连接成功回调
        /// </summary>
        /// <param name="isConnected">是否已连接</param>
        /// <param name="message">相关消息</param>
        private void OnHL7ConnectionSucceeded(bool isConnected, string message)
        {
            Log.Information($"HL7连接成功: {message}");
            // 更新UI上的连接状态图标
            App.Current.Dispatcher.Invoke(() => {
                UpdateUploadStatusIcon();
            });
        }

        /// <summary>
        /// HL7连接断开回调
        /// </summary>
        /// <param name="isConnected">是否已连接</param>
        /// <param name="message">相关消息</param>
        private void OnHL7ConnectionClosed(bool isConnected, string message)
        {
            Log.Information($"HL7连接断开: {message}");
            // 更新UI上的连接状态图标
            App.Current.Dispatcher.Invoke(() => {
                UpdateUploadStatusIcon();
            });
        }

        /// <summary>
        /// HL7连接失败回调
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="exception">异常对象</param>
        private void OnHL7ConnectionFailed(string errorMessage, Exception exception)
        {
            Log.Information($"HL7连接失败: {errorMessage} {exception?.ToString()??""}");
           
            // 更新UI上的连接状态图标
            App.Current.Dispatcher.Invoke(() => {
                UpdateUploadStatusIcon();
            });
        }

        /// <summary>
        /// 更新上传状态图标
        /// </summary>
        private void UpdateUploadStatusIcon()
        {
            if (UploadConfig.Instance.OpenUpload)
            {
                ImgUpload = HL7Helper.Instance.IsConnected()
                    ? path + "upload_connected.png"
                    : path + "upload_error.png";
            }
            else
            {
                ImgUpload = path + "upload_close.png";
            }
        }

        private void RegisterMsg()
        {
            WeakReferenceMessenger.Default.Register<MainStatusChangeMsg>(
                this,
                (r, m) =>
                {
                    if (m.What == MainStatusChangeMsg.What_ChangeState)
                    {
                        UpdateBottomStatus();
                    }
                }
            );
        }

        private void InitBottomStatus()
        {
            CurrentDate = DateTime.Now.GetDateTimeString4();
            CurrentStatus = SystemGlobal.MachineStatus.GetDescription();
            CurrentMsg = "暂无信息";
            UpdateBottomStatus();
            StartTimeer();
        }
     

        private void StartTimeer()
        {
            if (dispatcherTimer != null)
                return;
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(10);
            dispatcherTimer.Tick += (s, e) =>
            {
                CurrentDate = DateTime.Now.GetDateTimeString4();
            };
            dispatcherTimer.Start();
        }

    

        private void UpdateBottomStatus()
        {
            CurrentStatus = SystemGlobal.MachineStatus.GetDescription();
            ImgBarcode = GlobalConfig.Instance.ScanBarcode
                ? path + "barcode_open.png"
                : path + "barcode_close.png";
            ImgPrinter = path + "printer_success.png";
            
            // 更新上传状态图标
            UpdateUploadStatusIcon();
            
            ImgTemp = SystemGlobal.TempStandard
                ? path + "temp_standard.png"
                : path + "temp_error.png";
            if (SystemGlobal.MachineStatus.IsPrepare())
            {
                ImgStartTest = path + "start_success.png";
            }
            else if (SystemGlobal.MachineStatus.IsRunning() && !SystemGlobal.IsRunningtStop)
            {
                ImgStartTest = path + "stop_success.png";
            }
            else if (SystemGlobal.MachineStatus.IsRunning() && SystemGlobal.IsRunningtStop)
            {
                ImgStartTest = path + "stop_error.png";
            }
            else
            {
                ImgStartTest = path + "start_error.png";
            }
        }

        private void InitBarcodeSerialPort()
        {
            BarcodeHelper.Instance.SerialPortConnectReceived += (s) =>
            {
                Log.Information(string.IsNullOrEmpty(s) ? "扫码枪连接成功" : $"扫码枪连接失败 {s}");
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"扫码枪连接失败 {s}");
                    return;
                }
            };
            BarcodeHelper.Instance.SerialPortConnectExceptionReceived += (s) =>
            {
                Log.Information("扫码枪 exception " + s);
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"扫码枪连接失败2 {s}");
                    return;
                }
            };
            BarcodeHelper.Instance.Connect(
                configService.BarcodePortName(),
                configService.BarcodePortBaudRate()
            );
        }

        private void InitSerialPort()
        {
            SerialPortHelper.Instance.SerialPortConnectReceived += (s) =>
            {
                Log.Information(
                    string.IsNullOrEmpty(s) ? "通讯串口连接成功" : $"通讯串口连接失败 {s}"
                );
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"通讯串口连接失败 {s}");
                    return;
                }
            };
            SerialPortHelper.Instance.SerialPortConnectExceptionReceived += (s) =>
            {
                Log.Information("通讯串口 exception " + s);
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"通讯串口连接失败2 {s}");
                    return;
                }
            };
            SerialPortHelper.Instance.SerialPortExceptionReceived += (s) =>
            {
                Log.Information("通讯串口 " + s);
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"通讯串口 {s}");
                    SystemGlobal.MachineStatus = MachineStatus.RunningError;
                    SystemGlobal.ErrorContinueTest = false;
                    return;
                }
            };
            SerialPortHelper.Instance.Connect(
                configService.MainPortName(),
                configService.MainPortBaudRate()
            );
        }

        private void test()
        {
            //byte[] originalData =  Encoding.GetEncoding("gb2312").GetBytes("{\"Code\":1,\"Type\":1}");
            //byte[] encodedData = Crc16.Encode(originalData);

            //Log.Information("originalData " + Crc16.ByteArrayToHexString(originalData));
            //Log.Information("encodedData " + Crc16.ByteArrayToHexString(encodedData));
            //// encodedData 将包含原始数据 + 2字节CRC校验码
            //bool isValid = Crc16.Decode(encodedData);
            //// isValid 为true表示校验通过
            //Log.Information("isValid " + isValid);

            
        }

        public void changeEN()
        {
            string path =
                "pack://application:,,,/FluorescenceFullAutomatic;component/Style/en-US.xaml";
            App.Current.Resources.MergedDictionaries[0].Source = new Uri(path);
        }

        public void changeCN()
        {
            string path =
                "pack://application:,,,/FluorescenceFullAutomatic;component/Style/zh-CN.xaml";
            App.Current.Resources.MergedDictionaries[0].Source = new Uri(path);
        }

        private void Serial_DataReceived(byte[] obj)
        {
            //Log.Information(READN_CODING.GetString(obj));
        }

        private void ted()
        {
            //serialPortUtil.Connect("COM3", 115200);
        }

        private void close()
        {
            //serialPortUtil.Disconnect();
            string title = App.Current.FindResource("Title") as string;
            Log.Information("res " + title);
        }

        private void send()
        {
            SerialPortHelper.Instance.GetSelfInspectionState();
        }
        private void ExecuteNavCommand(int index)
        {
            SelectedIndex = index;
            bool ret = notity_What.TryGetValue(index,out int what);
            if (ret) { 
                WeakReferenceMessenger.Default.Send(new EventMsg<string>("") { What = what });
            }
        }

        private void UpdateSelectedContent()
        {
            if (NavItems != null && NavItems.Count > 0)
            {
                var selectedItem = NavItems.FirstOrDefault(item => item.Index == SelectedIndex);
                if (selectedItem != null)
                {
                    SelectedContent = selectedItem.Content;
                }
            }
        }

        [RelayCommand]
        public void ClickTest()
        {
            var selectedItem = NavItems.FirstOrDefault(item => item.Index == SelectedIndex);
            if (selectedItem != null && selectedItem.Header == GlobalUtil.GetString(Keys.QCTitle))
            {
                //拟合质控
                WeakReferenceMessenger.Default.Send(
                    new MainStatusChangeMsg() { What = MainStatusChangeMsg.What_ClickQC }
                );
            }
            else
            {
                //检测
                WeakReferenceMessenger.Default.Send(
                    new MainStatusChangeMsg() { What = MainStatusChangeMsg.What_ClickTest }
                );
            }
        }

        [RelayCommand]
        public void ClickShutdown()
        {
            MessageBox.Show("关机");
        }

        /// <summary>
        /// 测试HL7连接状态
        /// </summary>
        [RelayCommand]
        public void ClickTestHL7()
        {
            //if (HL7Helper.Instance.IsConnected())
            //{
            //    Log.Information("HL7当前已连接，正在断开连接...");
            //    HL7Helper.Instance.Disconnect();
            //}
            //else
            //{
            //    Log.Information("HL7当前未连接，正在尝试连接...");
            //    HL7Helper.Instance.Start();
            //}
            
            //// 更新连接状态图标
            //UpdateUploadStatusIcon();
        }
    }
}
