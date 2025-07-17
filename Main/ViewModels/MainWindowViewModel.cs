using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.HomeModule.Services;
using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.Utils;
using FluorescenceFullAutomatic.UploadModule.Upload;
using FluorescenceFullAutomatic.Views;
using MahApps.Metro.Controls.Dialogs;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Unity;
using Serilog;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
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
        private readonly IConfigService configRepository;
        private readonly ILisService lisService;
        private readonly IRegionManager regionManager;
        private readonly ISerialPortService serialPortService;
        private readonly IDispatcherService dispatcherService;
        private readonly IDialogService dialogService;
        private readonly ILogService logService;
        private readonly IReactionAreaQueueService reactionAreaQueueService;
        private readonly IToolService toolService;
        private int _selectedIndex;
        Dictionary<int, int> notity_What = new Dictionary<int, int>() { };
        private readonly List<string> regions = new List<string>()
        {
            "HomeView",
            "ApplyTestView",
            "DataManagerView",
            "QCView",
            "SettingsView",
            "TestView",
        };
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
        private string path =
            "pack://application:,,,/FluorescenceFullAutomatic.Platform;component/Image/";

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
        [ObservableProperty]
        private bool showCloseButton;
        [RelayCommand]
        private void ClickLogo()
        {
            var now = DateTime.Now;
            if (_lastLogoClickTime == DateTime.MinValue) {
                _lastLogoClickTime = now;
            }
            double interval = (now - _lastLogoClickTime).TotalSeconds;
            logService.Info($"interval={interval} _logoClickCount={_logoClickCount}");
            if (interval > 3)
            {
                _logoClickCount = 0;
            }

            _logoClickCount++;
            _lastLogoClickTime = now;

            if (_logoClickCount >= 5)
            {
                //更改调试模式
                _logoClickCount = 0;
                configRepository.SetDebugModeChnage();
                //Task.Run(() =>
                //{
                //    Task.Delay(1000);
                //    WeakReferenceMessenger.Default.Send(
                //        new EventMsg<string>("") { What = EventWhat.WHAT_CLICK_DEBUG_MODE }
                //    );
                //});
            }
        }

        public MainWindowViewModel(
            IConfigService configRepository,
            IRegionManager regionManager,
            IContainerProvider containerProvider,
            ILisService lisService,
            ISerialPortService serialPortService,
            IDispatcherService dispatcherService,
            IDialogService dialogService,
            IReactionAreaQueueService reactionAreaQueueService,
            ILogService logService,
            IToolService toolService
        )
        {
            this.toolService = toolService;
            this.logService = logService;
            this.reactionAreaQueueService = reactionAreaQueueService;
            this.dialogService = dialogService;
            this.dispatcherService = dispatcherService;
            this.serialPortService = serialPortService;
            this.lisService = lisService;
            this.configRepository = configRepository;
            this.regionManager = regionManager;
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
                    //Content = containerProvider.Resolve<HomeView>(),
                },
                new NavItem
                {
                    Header = GlobalUtil.GetString(Keys.ApplyTestTitle),
                    Index = 1,
                    //Content = containerProvider.Resolve<ApplyTestView>(),
                },
                new NavItem
                {
                    Header = GlobalUtil.GetString(Keys.DataManagerTitle),
                    Index = 2,
                    //Content = containerProvider.Resolve<DataManagerView>(),
                },
                new NavItem
                {
                    Header = GlobalUtil.GetString(Keys.QCTitle),
                    Index = 3,
                    //Content = containerProvider.Resolve<QCView>(),
                },
                new NavItem
                {
                    Header = GlobalUtil.GetString(Keys.SettingsTitle),
                    Index = 4,
                    //Content = containerProvider.Resolve<SettingsView>(),
                },
            };

            NavCommand = new RelayCommand<int>(ExecuteNavCommand);

            InitBottomStatus();
            RegisterMsg();
            InitHl7();
            configRepository.AddDebugModeChangedListener(DebugModeChange);
            InitWindow();
            DebugModeChange(configRepository.GetDebugMode());
        }

        private void DebugModeChange(bool debugMode)
        {
            if (debugMode)
            {
                toolService.ShowTaskBar();
                ShowCloseButton = true;
            }
            else {
                toolService.HideTaskBar();
                ShowCloseButton = false;
            }
        }

        private void InitWindow()
        {
            toolService.HideTaskBar();
        }

        [RelayCommand]
        public void Loaded()
        {
            SelectedIndex = 0;
            UpdateSelectedContent();
        }

        private void InitTicketSerialPort()
        {
            serialPortService.AddTicketConnectReceived(
                (s) =>
                {
                    Log.Information(
                        string.IsNullOrEmpty(s) ? "小票打印机连接成功" : $"小票打印机连接失败 {s}"
                    );
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"小票打印机连接失败 {s}");
                        return;
                    }
                }
            );
            serialPortService.AddTicketExceptionReceived(
                (s) =>
                {
                    Log.Information("小票打印机 exception " + s);
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"小票打印机连接失败2 {s}");
                        return;
                    }
                }
            );
            serialPortService.ConnectTicket(
                configRepository.TicketPortName(),
                configRepository.TicketPortBaudRate()
            );
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        private void InitConfig()
        {
            try
            {
                if (string.IsNullOrEmpty(GlobalConfig.Instance.PrinterName))
                {
                    //打印
                    GlobalConfig.Instance.PrinterName =
                        new System.Drawing.Printing.PrinterSettings().PrinterName;
                }
            }
            catch {
                //打印
                GlobalConfig.Instance.PrinterName =
                    new System.Drawing.Printing.PrinterSettings().PrinterName;
            }
            try
            {
                if (!File.Exists(GlobalConfig.Instance.ReportTemplatePath))
                {
                    //为空
                    GlobalConfig.Instance.ReportTemplatePath = SystemGlobal.Template_Path;
                }
            }
            catch {
                GlobalConfig.Instance.ReportTemplatePath = SystemGlobal.Template_Path;
            }
            try
            {
                if (!File.Exists(GlobalConfig.Instance.ReportDoubleTemplatePath))
                {
                    //为空
                    GlobalConfig.Instance.ReportDoubleTemplatePath = SystemGlobal.DoubleTemplate_Path;
                }
            }
            catch {
                GlobalConfig.Instance.ReportDoubleTemplatePath = SystemGlobal.DoubleTemplate_Path;
            }
        }

        private void InitHl7()
        {
            //注册HL7连接状态监听事件

            lisService.AddConnectionSucceededHandler(OnHL7ConnectionSucceeded);
            lisService.AddConnectionClosedHandler(OnHL7ConnectionClosed);
            lisService.AddConnectionFailedHandler(OnHL7ConnectionFailed);
            ////更新连接状态图标
            //UpdateUploadStatusIcon();
            //开始连接
            lisService.Connect();
        }

        /// <summary>
        /// 析构函数，移除事件订阅
        /// </summary>
        ~MainWindowViewModel()
        {
            // 移除HL7连接回调
            lisService.RemoveConnectionSucceededHandler(OnHL7ConnectionSucceeded);
            lisService.RemoveConnectionClosedHandler(OnHL7ConnectionClosed);
            lisService.RemoveConnectionFailedHandler(OnHL7ConnectionFailed);
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
            dispatcherService.Invoke(() =>
            {
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
            dispatcherService.Invoke(() =>
            {
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
            Log.Information($"HL7连接失败: {errorMessage} {exception?.ToString() ?? ""}");

            // 更新UI上的连接状态图标
            dispatcherService.Invoke(() =>
            {
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
                ImgUpload = lisService.IsConnected()
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
            serialPortService.AddBarcodeConnectReceived(
                (s) =>
                {
                    Log.Information(
                        string.IsNullOrEmpty(s) ? "扫码枪连接成功" : $"扫码枪连接失败 {s}"
                    );
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"扫码枪连接失败 {s}");
                        return;
                    }
                }
            );
            serialPortService.AddBarcodeExceptionReceived(
                (s) =>
                {
                    Log.Information("扫码枪 exception " + s);
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"扫码枪连接失败2 {s}");
                        return;
                    }
                }
            );
            serialPortService.ConnectBarcode(
                configRepository.BarcodePortName(),
                configRepository.BarcodePortBaudRate()
            );
        }

        private void InitSerialPort()
        {
            serialPortService.AddSerialPortConnectReceived(
                (s) =>
                {
                    Log.Information(
                        string.IsNullOrEmpty(s) ? "通讯串口连接成功" : $"通讯串口连接失败 {s}"
                    );
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"通讯串口连接失败 {s}");
                        return;
                    }
                }
            );

            serialPortService.AddSerialPortExceptionReceived(
                (s) =>
                {
                    Log.Information("通讯串口 " + s);
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"通讯串口 {s}");
                        SystemGlobal.MachineStatus = MachineStatus.RunningError;
                        SystemGlobal.ErrorContinueTest = false;
                        return;
                    }
                }
            );

            serialPortService.Connect(
                configRepository.MainPortName(),
                configRepository.MainPortBaudRate()
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

    
        private void ExecuteNavCommand(int index)
        {
            SelectedIndex = index;
            bool ret = notity_What.TryGetValue(index, out int what);
            if (ret)
            {
                WeakReferenceMessenger.Default.Send(new EventMsg<string>("") { What = what });
            }
        }

        private void UpdateSelectedContent()
        {
            //if (NavItems != null && NavItems.Count > 0)
            //{
            //    var selectedItem = NavItems.FirstOrDefault(item => item.Index == SelectedIndex);
            //    if (selectedItem != null)
            //    {
            //SelectedContent = selectedItem.Content;

            regionManager.RequestNavigate(Regions.MainContent, regions[SelectedIndex]);
            //    }
            //}
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
            
            if (!VerifyShutdown()) {
                dialogService.ShowHiltDialog(this, "提示", "正在检测，请等待检测完毕。", "好的", (v,d) => { 

                });
                return;
            }

            dialogService.ShowHiltDialog(
                this,
                "提示",
                "确定要关闭系统吗？",
                "确定",
                (v, d) =>
                {
                    serialPortService.Shutdown();
                },
                "取消",
                (v, d) => { }
            );
            
        }

        private bool VerifyShutdown()
        {
            return !SystemGlobal.MachineStatus.IsRunning() && reactionAreaQueueService.Count() == 0;
        }
    }
}
