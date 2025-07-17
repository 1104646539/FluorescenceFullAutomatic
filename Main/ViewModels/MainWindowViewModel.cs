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
        /// ֻ��������ʱ��ļ�ʱ��
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
                //���ĵ���ģʽ
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
                        string.IsNullOrEmpty(s) ? "СƱ��ӡ�����ӳɹ�" : $"СƱ��ӡ������ʧ�� {s}"
                    );
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"СƱ��ӡ������ʧ�� {s}");
                        return;
                    }
                }
            );
            serialPortService.AddTicketExceptionReceived(
                (s) =>
                {
                    Log.Information("СƱ��ӡ�� exception " + s);
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"СƱ��ӡ������ʧ��2 {s}");
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
        /// ��ʼ������
        /// </summary>
        private void InitConfig()
        {
            try
            {
                if (string.IsNullOrEmpty(GlobalConfig.Instance.PrinterName))
                {
                    //��ӡ
                    GlobalConfig.Instance.PrinterName =
                        new System.Drawing.Printing.PrinterSettings().PrinterName;
                }
            }
            catch {
                //��ӡ
                GlobalConfig.Instance.PrinterName =
                    new System.Drawing.Printing.PrinterSettings().PrinterName;
            }
            try
            {
                if (!File.Exists(GlobalConfig.Instance.ReportTemplatePath))
                {
                    //Ϊ��
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
                    //Ϊ��
                    GlobalConfig.Instance.ReportDoubleTemplatePath = SystemGlobal.DoubleTemplate_Path;
                }
            }
            catch {
                GlobalConfig.Instance.ReportDoubleTemplatePath = SystemGlobal.DoubleTemplate_Path;
            }
        }

        private void InitHl7()
        {
            //ע��HL7����״̬�����¼�

            lisService.AddConnectionSucceededHandler(OnHL7ConnectionSucceeded);
            lisService.AddConnectionClosedHandler(OnHL7ConnectionClosed);
            lisService.AddConnectionFailedHandler(OnHL7ConnectionFailed);
            ////��������״̬ͼ��
            //UpdateUploadStatusIcon();
            //��ʼ����
            lisService.Connect();
        }

        /// <summary>
        /// �����������Ƴ��¼�����
        /// </summary>
        ~MainWindowViewModel()
        {
            // �Ƴ�HL7���ӻص�
            lisService.RemoveConnectionSucceededHandler(OnHL7ConnectionSucceeded);
            lisService.RemoveConnectionClosedHandler(OnHL7ConnectionClosed);
            lisService.RemoveConnectionFailedHandler(OnHL7ConnectionFailed);
        }

        /// <summary>
        /// HL7���ӳɹ��ص�
        /// </summary>
        /// <param name="isConnected">�Ƿ�������</param>
        /// <param name="message">�����Ϣ</param>
        private void OnHL7ConnectionSucceeded(bool isConnected, string message)
        {
            Log.Information($"HL7���ӳɹ�: {message}");
            // ����UI�ϵ�����״̬ͼ��
            dispatcherService.Invoke(() =>
            {
                UpdateUploadStatusIcon();
            });
        }

        /// <summary>
        /// HL7���ӶϿ��ص�
        /// </summary>
        /// <param name="isConnected">�Ƿ�������</param>
        /// <param name="message">�����Ϣ</param>
        private void OnHL7ConnectionClosed(bool isConnected, string message)
        {
            Log.Information($"HL7���ӶϿ�: {message}");
            // ����UI�ϵ�����״̬ͼ��
            dispatcherService.Invoke(() =>
            {
                UpdateUploadStatusIcon();
            });
        }

        /// <summary>
        /// HL7����ʧ�ܻص�
        /// </summary>
        /// <param name="errorMessage">������Ϣ</param>
        /// <param name="exception">�쳣����</param>
        private void OnHL7ConnectionFailed(string errorMessage, Exception exception)
        {
            Log.Information($"HL7����ʧ��: {errorMessage} {exception?.ToString() ?? ""}");

            // ����UI�ϵ�����״̬ͼ��
            dispatcherService.Invoke(() =>
            {
                UpdateUploadStatusIcon();
            });
        }

        /// <summary>
        /// �����ϴ�״̬ͼ��
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
            CurrentMsg = "������Ϣ";
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

            // �����ϴ�״̬ͼ��
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
                        string.IsNullOrEmpty(s) ? "ɨ��ǹ���ӳɹ�" : $"ɨ��ǹ����ʧ�� {s}"
                    );
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"ɨ��ǹ����ʧ�� {s}");
                        return;
                    }
                }
            );
            serialPortService.AddBarcodeExceptionReceived(
                (s) =>
                {
                    Log.Information("ɨ��ǹ exception " + s);
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"ɨ��ǹ����ʧ��2 {s}");
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
                        string.IsNullOrEmpty(s) ? "ͨѶ�������ӳɹ�" : $"ͨѶ��������ʧ�� {s}"
                    );
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"ͨѶ��������ʧ�� {s}");
                        return;
                    }
                }
            );

            serialPortService.AddSerialPortExceptionReceived(
                (s) =>
                {
                    Log.Information("ͨѶ���� " + s);
                    if (!string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show($"ͨѶ���� {s}");
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
            //// encodedData ������ԭʼ���� + 2�ֽ�CRCУ����
            //bool isValid = Crc16.Decode(encodedData);
            //// isValid Ϊtrue��ʾУ��ͨ��
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
                //����ʿ�
                WeakReferenceMessenger.Default.Send(
                    new MainStatusChangeMsg() { What = MainStatusChangeMsg.What_ClickQC }
                );
            }
            else
            {
                //���
                WeakReferenceMessenger.Default.Send(
                    new MainStatusChangeMsg() { What = MainStatusChangeMsg.What_ClickTest }
                );
            }
        }

        [RelayCommand]
        public void ClickShutdown()
        {
            
            if (!VerifyShutdown()) {
                dialogService.ShowHiltDialog(this, "��ʾ", "���ڼ�⣬��ȴ������ϡ�", "�õ�", (v,d) => { 

                });
                return;
            }

            dialogService.ShowHiltDialog(
                this,
                "��ʾ",
                "ȷ��Ҫ�ر�ϵͳ��",
                "ȷ��",
                (v, d) =>
                {
                    serialPortService.Shutdown();
                },
                "ȡ��",
                (v, d) => { }
            );
            
        }

        private bool VerifyShutdown()
        {
            return !SystemGlobal.MachineStatus.IsRunning() && reactionAreaQueueService.Count() == 0;
        }
    }
}
