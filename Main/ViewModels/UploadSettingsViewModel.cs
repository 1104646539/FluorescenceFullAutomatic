using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.Utils;

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

        private readonly ILisService lisService;

        private readonly IDialogService dialogRepository;
        public UploadSettingsViewModel(ILisService lisService, IDialogService dialogRepository  )
        {
            this.lisService = lisService;
            this.dialogRepository = dialogRepository;
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
            OpenUpload = lisService.GetOpenUpload();
            AutoUpload = lisService.GetAutoUpload();
            AutoReconnection = lisService.GetAutoReconnection();
            UploadIntervalTime = lisService.GetUploadIntervalTime();
            TwoWay = lisService.GetTwoWay();
            OvertimeRetryCount = lisService.GetOvertimeRetryCount();
            Overtime = lisService.GetOvertime();
            AutoGetApplyTest = lisService.GetAutoGetApplyTest();
            MatchBarcode = lisService.GetMatchBarcode();
            SerialPort = lisService.GetSerialPort();
            Charset = lisService.GetCharset();
            BaudRate = lisService.GetBaudRate();
            DataBit = lisService.GetDataBit();
            StopBit = lisService.GetStopBit();
            OddEven = lisService.GetOddEven();
            ServiceIP = lisService.GetServiceIP();
            ServicePort = lisService.GetServicePort();
            IsConnected = lisService.IsConnected();
            ConnectionStatus = IsConnected ? "已连接" : "未连接";

          
        }

        [RelayCommand]
        private void SaveSettings()
        {
            lisService.SaveAllSettings(
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

            dialogRepository.ShowHiltDialog(this,"提示", "设置已保存！", "确定", (m, d) => { });
        }

        [RelayCommand]
        private void Connect()
        {
            try
            {
                bool success = lisService.IsConnected();
                if (success)
                {
                    IsConnected = true;
                    ConnectionStatus = "已连接";
                    dialogRepository.ShowHiltDialog(this,"提示", "连接成功！", "确定", (m, d) => { });
                }
                else
                {
                    IsConnected = false;
                    ConnectionStatus = "连接失败";
                    dialogRepository.ShowHiltDialog(this,"错误", "连接失败", "确定", (m, d) => { });
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatus = "连接失败";
                dialogRepository.ShowHiltDialog(this,"错误", $"连接失败：{ex.Message}", "确定", (m, d) => { });
            }
        }

        [RelayCommand]
        private void Disconnect()
        {
            lisService.Disconnect();
            IsConnected = false;
            ConnectionStatus = "已断开";
            //try
            //{
            //    bool success = lisService.Disconnect();
            //    if (success)
            //    {
            //        IsConnected = false;
            //        ConnectionStatus = "已断开";
            //        dialogRepository.ShowHiltDialog(this,"提示", "已断开连接！", "确定", (m, d) => { });
            //    }
            //    else
            //    {
            //        dialogRepository.ShowHiltDialog(this,"错误", "断开连接失败", "确定", (m, d) => { });
            //    }
            //}
            //catch (Exception ex)
            //{
            //        dialogRepository.ShowHiltDialog(this,"错误", $"断开连接失败：{ex.Message}", "确定", (m, d) => { });
            //}
        }
    }
}
