using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Services;
using FluorescenceFullAutomatic.Utils;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class UploadSettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool openUpload;

        [ObservableProperty]
        private bool autoUpload;

        [ObservableProperty]
        private bool autoReconnection;

        [ObservableProperty]
        private int uploadIntervalTime;

        [ObservableProperty]
        private bool twoWay;

        [ObservableProperty]
        private int overtimeRetryCount;

        [ObservableProperty]
        private int overtime;

        [ObservableProperty]
        private bool autoGetApplyTest;

        [ObservableProperty]
        private bool matchBarcode;

        [ObservableProperty]
        private bool serialPort;

        [ObservableProperty]
        private string charset;

        [ObservableProperty]
        private string baudRate;

        [ObservableProperty]
        private string dataBit;

        [ObservableProperty]
        private string stopBit;

        [ObservableProperty]
        private string oddEven;

        [ObservableProperty]
        private string serviceIP;

        [ObservableProperty]
        private string servicePort;

        [ObservableProperty]
        private bool isConnected;

        [ObservableProperty]
        private string connectionStatus = "未连接";

        public ObservableCollection<string> BaudRates { get; } = new ObservableCollection<string> { "9600", "115200" };
        public ObservableCollection<string> Charsets { get; } = new ObservableCollection<string> { "GBK", "UTF-8" };

        private readonly IUploadService _uploadService;

        public UploadSettingsViewModel(IUploadService uploadService)
        {
            _uploadService = uploadService;
            LoadSettings();
            WeakReferenceMessenger.Default.Register<EventMsg<string>>(this, (r, m) =>
            {
                if (m.What == EventWhat.WHAT_CLICK_SETTINGS)
                {
                    LoadSettings();
                }
            });
        }

        private void LoadSettings()
        {
            OpenUpload = _uploadService.GetOpenUpload();
            AutoUpload = _uploadService.GetAutoUpload();
            AutoReconnection = _uploadService.GetAutoReconnection();
            UploadIntervalTime = _uploadService.GetUploadIntervalTime();
            TwoWay = _uploadService.GetTwoWay();
            OvertimeRetryCount = _uploadService.GetOvertimeRetryCount();
            Overtime = _uploadService.GetOvertime();
            AutoGetApplyTest = _uploadService.GetAutoGetApplyTest();
            MatchBarcode = _uploadService.GetMatchBarcode();
            SerialPort = _uploadService.GetSerialPort();
            Charset = _uploadService.GetCharset();
            BaudRate = _uploadService.GetBaudRate();
            DataBit = _uploadService.GetDataBit();
            StopBit = _uploadService.GetStopBit();
            OddEven = _uploadService.GetOddEven();
            ServiceIP = _uploadService.GetServiceIP();
            ServicePort = _uploadService.GetServicePort();
            IsConnected = _uploadService.IsConnected();
            ConnectionStatus = IsConnected ? "已连接" : "未连接";

          
        }

        [RelayCommand]
        private void SaveSettings()
        {
            _uploadService.SaveAllSettings(
                OpenUpload,
                AutoUpload,
                AutoReconnection,
                UploadIntervalTime,
                TwoWay,
                OvertimeRetryCount,
                Overtime,
                AutoGetApplyTest,
                MatchBarcode,
                SerialPort,
                Charset,
                BaudRate,
                DataBit,
                StopBit,
                OddEven,
                ServiceIP,
                ServicePort
            );

            GlobalUtil.ShowHiltDialog("提示", "设置已保存！", "确定", (m, d) => { });
        }

        [RelayCommand]
        private void Connect()
        {
            try
            {
                bool success = _uploadService.Connect();
                if (success)
                {
                    IsConnected = true;
                    ConnectionStatus = "已连接";
                    GlobalUtil.ShowHiltDialog("提示", "连接成功！", "确定", (m, d) => { });
                }
                else
                {
                    IsConnected = false;
                    ConnectionStatus = "连接失败";
                    GlobalUtil.ShowHiltDialog("错误", "连接失败", "确定", (m, d) => { });
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatus = "连接失败";
                GlobalUtil.ShowHiltDialog("错误", $"连接失败：{ex.Message}", "确定", (m, d) => { });
            }
        }

        [RelayCommand]
        private void Disconnect()
        {
            try
            {
                bool success = _uploadService.Disconnect();
                if (success)
                {
                    IsConnected = false;
                    ConnectionStatus = "已断开";
                    GlobalUtil.ShowHiltDialog("提示", "已断开连接！", "确定", (m, d) => { });
                }
                else
                {
                    GlobalUtil.ShowHiltDialog("错误", "断开连接失败", "确定", (m, d) => { });
                }
            }
            catch (Exception ex)
            {
                GlobalUtil.ShowHiltDialog("错误", $"断开连接失败：{ex.Message}", "确定", (m, d) => { });
            }
        }
    }
}
