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
        /// ֻ��������ʱ��ļ�ʱ��
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
                //���ĵ���ģʽ
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
                Log.Information(string.IsNullOrEmpty(s) ? "СƱ��ӡ�����ӳɹ�" : $"СƱ��ӡ������ʧ�� {s}");
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"СƱ��ӡ������ʧ�� {s}");
                    return;
                }
            };
            TicketReportUtil.Instance.SerialPortConnectExceptionReceived += (s) =>
            {
                Log.Information("СƱ��ӡ�� exception " + s);
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"СƱ��ӡ������ʧ��2 {s}");
                    return;
                }
            };
            TicketReportUtil.Instance.Connect(
                configService.TicketPortName(),
                configService.TicketPortBaudRate()
            );
        }

        /// <summary>
        /// ��ʼ������
        /// </summary>
        private void InitConfig()
        {
            if(string.IsNullOrEmpty(GlobalConfig.Instance.PrinterName)){
                //��ӡ
                GlobalConfig.Instance.PrinterName = new System.Drawing.Printing.PrinterSettings().PrinterName;
            }
            if (!File.Exists(GlobalConfig.Instance.ReportTemplatePath))
            {
                //Ϊ��
                GlobalConfig.Instance.ReportTemplatePath = SystemGlobal.Template_Path;
            }
            if (!File.Exists(GlobalConfig.Instance.ReportDoubleTemplatePath))
            {
                //Ϊ��
                GlobalConfig.Instance.ReportDoubleTemplatePath = SystemGlobal.DoubleTemplate_Path;
            }
        }

        private void InitHl7()
        {
            //ע��HL7����״̬�����¼�
            HL7Helper.Instance.AddConnectionSucceededHandler(OnHL7ConnectionSucceeded);
            HL7Helper.Instance.AddConnectionClosedHandler(OnHL7ConnectionClosed);
            HL7Helper.Instance.AddConnectionFailedHandler(OnHL7ConnectionFailed);
            ////��������״̬ͼ��
            //UpdateUploadStatusIcon();
            //��ʼ����
            HL7Helper.Instance.InitializeService();
        }

        /// <summary>
        /// �����������Ƴ��¼�����
        /// </summary>
        ~MainViewModel()
        {
            // �Ƴ�HL7���ӻص�
            HL7Helper.Instance.RemoveConnectionSucceededHandler(OnHL7ConnectionSucceeded);
            HL7Helper.Instance.RemoveConnectionClosedHandler(OnHL7ConnectionClosed);
            HL7Helper.Instance.RemoveConnectionFailedHandler(OnHL7ConnectionFailed);
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
            App.Current.Dispatcher.Invoke(() => {
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
            App.Current.Dispatcher.Invoke(() => {
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
            Log.Information($"HL7����ʧ��: {errorMessage} {exception?.ToString()??""}");
           
            // ����UI�ϵ�����״̬ͼ��
            App.Current.Dispatcher.Invoke(() => {
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
            BarcodeHelper.Instance.SerialPortConnectReceived += (s) =>
            {
                Log.Information(string.IsNullOrEmpty(s) ? "ɨ��ǹ���ӳɹ�" : $"ɨ��ǹ����ʧ�� {s}");
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"ɨ��ǹ����ʧ�� {s}");
                    return;
                }
            };
            BarcodeHelper.Instance.SerialPortConnectExceptionReceived += (s) =>
            {
                Log.Information("ɨ��ǹ exception " + s);
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"ɨ��ǹ����ʧ��2 {s}");
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
                    string.IsNullOrEmpty(s) ? "ͨѶ�������ӳɹ�" : $"ͨѶ��������ʧ�� {s}"
                );
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"ͨѶ��������ʧ�� {s}");
                    return;
                }
            };
            SerialPortHelper.Instance.SerialPortConnectExceptionReceived += (s) =>
            {
                Log.Information("ͨѶ���� exception " + s);
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"ͨѶ��������ʧ��2 {s}");
                    return;
                }
            };
            SerialPortHelper.Instance.SerialPortExceptionReceived += (s) =>
            {
                Log.Information("ͨѶ���� " + s);
                if (!string.IsNullOrEmpty(s))
                {
                    MessageBox.Show($"ͨѶ���� {s}");
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
            MessageBox.Show("�ػ�");
        }

        /// <summary>
        /// ����HL7����״̬
        /// </summary>
        [RelayCommand]
        public void ClickTestHL7()
        {
            //if (HL7Helper.Instance.IsConnected())
            //{
            //    Log.Information("HL7��ǰ�����ӣ����ڶϿ�����...");
            //    HL7Helper.Instance.Disconnect();
            //}
            //else
            //{
            //    Log.Information("HL7��ǰδ���ӣ����ڳ�������...");
            //    HL7Helper.Instance.Start();
            //}
            
            //// ��������״̬ͼ��
            //UpdateUploadStatusIcon();
        }
    }
}
