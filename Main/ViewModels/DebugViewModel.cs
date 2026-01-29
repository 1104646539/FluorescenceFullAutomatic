using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlzEx.Standard;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Model.Events;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.Utils;
using FluorescenceFullAutomatic.ViewModels;
using FluorescenceFullAutomatic.Views.Ctr;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using Serilog;
using SqlSugar;
using Point = FluorescenceFullAutomatic.Platform.Model.Point;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class DebugViewModel : ObservableObject
    {
        #region 字段
        [ObservableProperty]
        ObservableCollection<string> motors = new ObservableCollection<string>()
        {
            "插卡Z",
            "柱塞泵",
            "管架Y",
            "推卡Y",
            "管架X",
            "插卡Y",
            "取样X",
            "取样Z",
            "检测Y",
            "插卡X",
        };

        [ObservableProperty]
        ObservableCollection<string> dirs = new ObservableCollection<string>()
        {
            "复位",
            "正向",
            "反向",
        };

        [ObservableProperty]
        string value;

        [ObservableProperty]
        public string motorSelected;

        [ObservableProperty]
        public string dirSelected;

        private ISerialPortService currentSerialPortService;

        private ISerialPortService selectSerialPortService;

        private ISerialPortService defaultSerialPortService;

        private ISerialPortCommandFacade _commandFacade;

        [ObservableProperty]
        ObservableCollection<string> serialPorts = new ObservableCollection<string>();

        [ObservableProperty]
        string selectedSerialPort;

        [ObservableProperty]
        string msg;

        [ObservableProperty]
        string btnChangeSerialPortMsg;

        [ObservableProperty]
        private bool isDefaultSerialPort;

        [ObservableProperty]
        private bool isCmdRunningFinish;

        [ObservableProperty]
        private bool isSampleTube = true;

        [ObservableProperty]
        private bool isSampleCup;

        [ObservableProperty]
        private bool isAddingAtSampling = true;

        [ObservableProperty]
        private bool isAddingAtCard;

        [ObservableProperty]
        private bool isSingleCard = true;

        [ObservableProperty]
        private bool isDoubleCard;

        [ObservableProperty]
        private bool isNormalTest = true;

        [ObservableProperty]
        private bool isQualityControl;

        [ObservableProperty]
        private bool isQualityControlSqueezing = true;

        [ObservableProperty]
        private bool isTubePierced;

        [ObservableProperty]
        private bool isCupPierced;

        [ObservableProperty]
        private bool isTubeSqueezing;

        [ObservableProperty]
        private bool isCupSqueezing;

        [ObservableProperty]
        private string sampleShelfPosition;

        [ObservableProperty]
        private string samplePos;

        [ObservableProperty]
        private bool isSampleForward = true;

        [ObservableProperty]
        private bool isSampleBackward;

        [ObservableProperty]
        private string sampleVolume;

        [ObservableProperty]
        private ObservableCollection<string> sampleTypes = new ObservableCollection<string>
        {
            "样本管",
            "样本杯",
        };

        [ObservableProperty]
        private string selectedSampleType;

        [ObservableProperty]
        private string addingVolume;

        [ObservableProperty]
        private ObservableCollection<string> addingTypes = new ObservableCollection<string>
        {
            "样本管",
            "样本杯",
        };

        [ObservableProperty]
        private string selectedAddingType;

        [ObservableProperty]
        private string reactionAreaX;

        [ObservableProperty]
        private string reactionAreaY;

        [ObservableProperty]
        private string testX;

        [ObservableProperty]
        private string testY;

        [ObservableProperty]
        private ObservableCollection<string> cardTypes = new ObservableCollection<string>
        {
            "单联卡",
            "双联卡",
        };

        [ObservableProperty]
        private string selectedCardType;

        [ObservableProperty]
        private ObservableCollection<string> testTypes = new ObservableCollection<string>
        {
            "普通卡",
            "质控卡",
        };

        [ObservableProperty]
        private string selectedTestType;

        [ObservableProperty]
        private string scanStart;

        [ObservableProperty]
        private string scanEnd;

        [ObservableProperty]
        private string peakWidth;

        [ObservableProperty]
        private string peakDistance;

        [ObservableProperty]
        private string reactionTemp;

        [ObservableProperty]
        private string updateFilePath;

        [ObservableProperty]
        private ObservableCollection<string> squeezingTypes = new ObservableCollection<string>
        {
            "样本管",
            "样本杯",
        };

        [ObservableProperty]
        private string selectedSqueezingType;

        [ObservableProperty]
        private ObservableCollection<string> piercedTypes = new ObservableCollection<string>
        {
            "样本管",
            "样本杯",
        };

        [ObservableProperty]
        private string selectedPiercedType;

        [ObservableProperty]
        private string cleanoutDuration;

        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IConfigService _configRepository;
        private readonly ILogService _logService;

        #endregion
        public DebugViewModel(
            ISerialPortService serialPortService,
            IConfigService configRepository,
            IDialogCoordinator dialogCoordinator,
            ILogService logService
        )
        {
            this.defaultSerialPortService = serialPortService;
            this._dialogCoordinator = dialogCoordinator;
            this._configRepository = configRepository;
            this._logService = logService;
            MotorSelected = Motors.First();
            DirSelected = Dirs.First();

            Value = "0";
            OnLoaded();

            IsDefaultSerialPort = true;

            //currentSerialPortService = defaultSerialPortService;
            //_commandFacade = new SerialPortCommandFacade(currentSerialPortService);

            IsCmdRunningFinish = true;
            //ChangeSerialPort(true); 已在上面直接初始化
            ChangeSerialPort(true);
            InitState();
        }

        private void InitState()
        {
            SampleShelfPosition = "1";
            SamplePos = "1";
            SampleVolume = "12";
            AddingVolume = "12";
            ReactionAreaX = "0";
            ReactionAreaY = "0";
            TestX = "0";
            TestY = "0";
            ScanStart = "200";
            ScanEnd = "600";
            PeakWidth = "120";
            PeakDistance = "180";
            ReactionTemp = "0";
            CleanoutDuration = "1000";
            IsTubePierced = true;
            IsTubeSqueezing = true;
        }

        [RelayCommand]
        public void Closed()
        {
            //selectSerialPortService?.RemoveReceiveData(this);
            //selectSerialPortService?.Disconnect();
            //currentSerialPortService?.RemoveReceiveData(this);
            currentSerialPortService?.RemoveOriginReceiveDataListener(OnOriginReceiveDataListener);
            currentSerialPortService.RemoveOriginSendDataListener(OnOriginSendDataListener);
            currentSerialPortService.RemoveScanSuccessListener(OnScanSuccess);
            currentSerialPortService.RemoveScanFailedListener(OnScanSuccess);
            SystemGlobal.TestType = TestType.None;
        }

        public void OnLoaded()
        {
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                SerialPorts.Add(port);
            }
            if (SerialPorts.Count > 0)
            {
                SelectedSerialPort = SerialPorts.First();
            }
        }

        [RelayCommand]
        public void ClickChangeSerialPort()
        {
            ChangeSerialPort(!IsDefaultSerialPort);
        }

        public void ChangeSerialPort(bool isDefault)
        {
            IsDefaultSerialPort = isDefault;
            BtnChangeSerialPortMsg = IsDefaultSerialPort ? "切换到可选串口" : "切换到默认串口";
            //currentSerialPortService?.RemoveReceiveData(this);
            //currentSerialPortService?.Disconnect();
            //currentSerialPortService?.RemoveOriginReceiveDataListener(OnOriginReceiveDataListener);
            //currentSerialPortService.RemoveOriginSendDataListener(OnOriginSendDataListener);

            currentSerialPortService = IsDefaultSerialPort
                ? defaultSerialPortService
                : selectSerialPortService;
            if (currentSerialPortService == defaultSerialPortService)
            {
                ReconnecntDefault();
            }

            // 更新CommandFacade
            if (currentSerialPortService != null)
            {
                _commandFacade = new SerialPortCommandFacade(currentSerialPortService);

                // Keep listeners for other events (Scan, OriginData)
                currentSerialPortService.AddOriginReceiveDataListener(OnOriginReceiveDataListener);
                currentSerialPortService.AddOriginSendDataListener(OnOriginSendDataListener);
                currentSerialPortService.AddScanSuccessListener(OnScanSuccess);
                currentSerialPortService.AddScanFailedListener(OnScanFailed);
            }
        }

        private void OnScanFailed(string msg)
        {
            if (!IsDebugTestType())
                return;
            IsCmdRunningFinish = true;
            Msg = $"收到扫码失败 {msg}";
            SystemGlobal.TestType = TestType.Test;
        }

        private void OnScanSuccess(string msg)
        {
            if (!IsDebugTestType())
                return;
            IsCmdRunningFinish = true;
            Msg = $"收到扫码成功 {msg}";
            SystemGlobal.TestType = TestType.Test;
        }

        /// <summary>
        /// 不显示响应码
        /// </summary>
        [ObservableProperty]
        public bool showResponse;

        /// <summary>
        /// 发送和接收的原始数据
        /// </summary>
        // [ObservableProperty]
        // public string originMsg;

        private string originMsg;
        public string OriginMsg
        {
            get => originMsg;
            set
            {
                if (SetProperty(ref originMsg, value))
                {
                    // 触发属性变更通知，以便行为类可以检测到变化
                    OnPropertyChanged(nameof(OriginMsg));
                }
            }
        }

        public void OnOriginSendDataListener(string str)
        {
            //收到原始数据
            Log.Information($"发送 原始数据:{str}");
            if (!ShowResponse)
            {
                if (str.ToArray()[0] == '0')
                {
                    return;
                }
            }
            OriginMsg += $"{DateTime.Now.GetDateTimeString2()} 发出:{str}";
        }

        public void OnOriginReceiveDataListener(string str)
        {
            //收到原始数据
            //Log.Information($"接收 原始数据:{str}");
            if (string.IsNullOrEmpty(str))
            {
                return;
            }
            if (!ShowResponse)
            {
                if (str.Contains("\"Type\":\"1\""))
                {
                    return;
                }
                OriginMsg += $"{str}";
            }
        }

        [RelayCommand]
        public void ClickClearInfo()
        {
            OriginMsg = "";
        }

        public void ReconnecntDefault()
        {
            //if (IsDefaultSerialPort) {
            //    currentSerialPortService.Connect(_configService.MainPortName(), _configService.MainPortBaudRate());
            //}
        }

        [RelayCommand]
        public void OpenSerila()
        {
            if (IsDefaultSerialPort)
                return;
            if (currentSerialPortService.IsOpen())
            {
                MessageBox.Show("串口已打开");
                return;
            }
            try
            {
                if (string.IsNullOrEmpty(SelectedSerialPort))
                {
                    MessageBox.Show("请选择串口");
                    return;
                }

                SerialPortHelper.Instance.Connect(
                    SelectedSerialPort,
                    _configRepository.MainPortBaudRate()
                );
                IsCmdRunningFinish = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// 检查是否为调试检测类型
        /// </summary>
        /// <returns>如果是调试检测类型返回true，否则返回false</returns>
        private bool IsDebugTestType()
        {
            if (SystemGlobal.TestType != TestType.Debug)
            {
                // Log.Information($"非调试检测类型，当前类型：{SystemGlobal.TestType}");
                return false;
            }
            return true;
        }

        [RelayCommand]
        public void CloseSerila()
        {
            if (IsDefaultSerialPort)
                return;
            SerialPortHelper.Instance.Disconnect();
            Msg = "串口已关闭";
            IsCmdRunningFinish = false;
        }

        // 检查串口是否打开的通用方法
        private bool CheckSerialPortOpen()
        {
            if (currentSerialPortService.IsOpen() == false)
            {
                MessageBox.Show("请先打开串口");
                return false;
            }
            return true;
        }


        [RelayCommand]
        public async Task Exec()
        {
            int indexMotor = Motors.IndexOf(MotorSelected);
            int indexDir = Dirs.IndexOf(DirSelected);
            if (indexMotor == -1 || indexDir == -1)
            {
                MessageBox.Show("请选择电机和方向");
                return;
            }
            int.TryParse(Value, out int intValue);

            Msg = $"正在执行 {indexMotor} , {indexDir} , {intValue}";

            await SafeSerialPortCallAsync(
                async () => await _commandFacade.MotorAsync(indexMotor + "", indexDir + "", intValue + ""),
                ret => Msg = $"收到执行电机响应: 复位状态={ret.Data?.RestState}"
            );
        }

        [RelayCommand]
        public async Task OpenScan()
        {
            Msg = "扫码已发送";
            //await SafeSerialPortCallAsync(async () =>
            //{
            //    currentSerialPortService?.ScanBarcode();
            //});
            SystemGlobal.TestType = TestType.Debug;
            currentSerialPortService?.ScanBarcode();
        }

        [RelayCommand]
        public async Task StopScan()
        {
            Msg = "停止扫码已发送";
            SystemGlobal.TestType = TestType.Debug;
            currentSerialPortService?.StopScanBarcode();
            SystemGlobal.TestType = TestType.Test;
        }
        [RelayCommand]
        public async void OpenUpdate()
        {
            await SafeSerialPortCallAsync(async () =>
            {
                return await _commandFacade?.UpdateAsync();

            }, (ret) => Msg = "更新命令已发送");
        }

        [RelayCommand]
        public void SelectUpdateFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "请选择更新文件";
            dialog.Filter = "所有文件|*.*";
            dialog.Filter = "bin|*.bin";
            if (dialog.ShowDialog() == true)
            {
                UpdateFilePath = dialog.FileName;
            }
        }

        [RelayCommand]
        public void Update()
        {
            if (UpdateFilePath != null)
            {
                string target = GlobalUtil.GetUpdateFlash();
                if (string.IsNullOrEmpty(target))
                {
                    MessageBox.Show($"升级失败，未找到升级盘");
                    return;
                }
                string ret = GlobalUtil.CopyFileToTarget(UpdateFilePath, target);

                if (string.IsNullOrEmpty(ret))
                {
                    MessageBox.Show("升级文件已烧录,请重启仪器");
                }
                else
                {
                    MessageBox.Show($"升级失败，{ret}");
                }
            }
        }
        [RelayCommand]
        public async Task GetSelfInspectionState()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.GetSelfInspectionStateAsync(
                    _configRepository.RetainReactionArea()
                ),
                ret => Msg = $"收到自检状态响应: {string.Join(", ", ret.Data ?? new List<string>())}"
            );
        }

        [RelayCommand]
        public async Task GetVersion()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.GetVersionAsync(),
                ret => Msg = $"收到版本号响应 {ret.Data?.Ver}"
            );
        }

        [RelayCommand]
        public async Task Shutdown()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.ShutdownAsync(),
                ret => Msg = $"收到关机响应"
            );
        }

        [RelayCommand]
        public async Task GetMachineState()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.GetMachineStateAsync(),
                ret => Msg = $"收到状态响应: 插卡={ret.Data?.CardExist}, 数量={ret.Data?.CardNum}, 清洗液={ret.Data?.CleanoutFluid}, 样本架状态={string.Join(",", ret.Data?.SampleShelf ?? new List<int>())}"
            );
        }

        [RelayCommand]
        public async Task GetCleanoutFluid()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.GetMachineStateAsync(),
                ret => Msg = $"收到清洗液状态响应: {ret.Data?.CleanoutFluid}"
            );
        }

        [RelayCommand]
        public async Task GetSampleShelf()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.GetMachineStateAsync(),
                ret => Msg = $"收到样本架状态响应: {string.Join(",", ret.Data?.SampleShelf ?? new List<int>())}"
            );
        }

        [RelayCommand]
        public async Task MoveSampleShelf()
        {
            int.TryParse(SampleShelfPosition, out int position);
            await SafeSerialPortCallAsync(
               async () => await _commandFacade.MoveSampleShelfAsync(position),
               ret => Msg = "收到移动样本架响应"
           );
        }

        [RelayCommand]
        public async Task ResetSampleShelf()
        {
            await SafeSerialPortCallAsync(
               async () => await _commandFacade.MoveSampleShelfAsync(0),
               ret => Msg = "收到移动样本架响应"
           );
        }

        [RelayCommand]
        public async Task MoveSample()
        {
            int.TryParse(SamplePos, out int pos);
            await SafeSerialPortCallAsync(
               async () => await _commandFacade.MoveSampleAsync(pos),
               ret => Msg = $"收到移动样本响应: 类型={ret.Data?.SampleType}"
           );
        }

        [RelayCommand]
        public async Task Sampling()
        {
            int.TryParse(SampleVolume, out int volume);
            string sampleType = IsSampleTube ? "0" : "1";

            await SafeSerialPortCallAsync(
                async () => await _commandFacade.SamplingAsync(sampleType, volume),
                ret => Msg = $"收到采样响应: 结果={ret.Data?.Result}"
            );
        }

        [RelayCommand]
        public async Task CleanoutSamplingProbe()
        {
            int.TryParse(CleanoutDuration, out int duration);
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.CleanoutSamplingProbeAsync(duration),
                ret => Msg = "收到清洗取样针响应"
            );
        }

        [RelayCommand]
        public async Task AddingSample()
        {
            int.TryParse(AddingVolume, out int volume);
            string addingType = IsAddingAtSampling ? "0" : "1";

            await SafeSerialPortCallAsync(
                async () => await _commandFacade.AddingSampleAsync(volume, addingType),
                ret => Msg = "收到加样响应"
            );
        }

        [RelayCommand]
        public async Task Drainage()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.DrainageAsync(),
                ret => Msg = "收到排液响应"
            );
        }

        [RelayCommand]
        public async Task PushCard()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.PushCardAsync(),
                ret => Msg = $"收到推卡响应: 成功={ret.Data?.Success} {ret.Data?.QrCode}"
            );
        }

        [RelayCommand]
        public async Task MoveReactionArea()
        {
            int.TryParse(ReactionAreaX, out int x);
            int.TryParse(ReactionAreaY, out int y);

            await SafeSerialPortCallAsync(
               async () => await _commandFacade.MoveReactionAreaAsync(x, y),
               ret => Msg = "收到移动反应区响应"
           );
        }

        [RelayCommand]
        public async Task Test()
        {
            int.TryParse(TestX, out int x);
            int.TryParse(TestY, out int y);
            string cardType = IsSingleCard ? "0" : "1";
            string testType = IsNormalTest ? "0" : "1";

            await SafeSerialPortCallAsync(
                async () => await _commandFacade.TestAsync(
                    x, y, cardType, testType, ScanStart, ScanEnd, PeakWidth, PeakDistance
                ),
                ret =>
                {
                    var data = ret.Data;
                    Msg = $"收到检测响应: T={data?.T}, C={data?.C}, T2={data?.T2}, C2={data?.C2}, 卡片类型={data?.CardType}, 检测类型={data?.TestType}, 点数={data?.Point?.Count ?? 0}";
                    if (data != null)
                    {
                        ShowResultDetails(
                            new TestResult()
                            {
                                Point = new Point()
                                {
                                    Points = data.Point?.ToArray(),
                                    Location = data.Location?.ToArray(),
                                },
                                T = data.T,
                                C = data.C,
                            }
                        );
                    }
                }
            );
        }

        /// <summary>
        /// 安全执行串口指令，统一处理异常 (带返回值)
        /// </summary>
        private async Task SafeSerialPortCallAsync<T>(
            Func<Task<BaseResponseModel<T>>> command,
            Action<BaseResponseModel<T>> onSuccess)
        {
            if (!CheckSerialPortOpen())
                return;

            if (!IsCmdRunningFinish)
            {
                Msg = "上一个命令还未执行完成";
                return;
            }
            try
            {
                IsCmdRunningFinish = false;
                SystemGlobal.TestType = TestType.Debug;
                var ret = await command();
                onSuccess?.Invoke(ret);
            }
            catch (SerialCommandException ex)
            {
                _logService.Error($"[状态机] 串口指令异常: {ex.Code} {ex.Message}");
                await HandleSerialError();
            }
            catch (TimeoutException ex)
            {
                _logService.Error($"[状态机] 串口超时: {ex.Message}");
                await HandleSerialError();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("串口重发"))
            {
                _logService.Error($"[状态机] 串口命令重发: {ex.Message}");
                await HandleSerialError();
            }
            catch (Exception ex)
            {
                _logService.Error($"[状态机] 串口异常: {ex.Message}");
                await HandleSerialError();
            }
            finally
            {
                IsCmdRunningFinish = true;
                SystemGlobal.TestType = TestType.Test;
            }
        }

        private async Task HandleSerialError()
        {

        }

        [RelayCommand]
        public async Task GetReactionTemp()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.GetReactionTempAsync(ReactionTemp),
                ret => Msg = $"收到反应区温度响应: 温度={ret.Data?.Temp}"
            );
        }

        [RelayCommand]
        public async Task ClearReactionArea()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.ClearReactionAreaAsync(),
                ret => Msg = "收到清空反应区响应"
            );
        }

        [RelayCommand]
        public async Task ResetParams()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.ResetParamsAsync(),
                ret => Msg = "收到重置参数响应"
            );
        }

       

        [RelayCommand]
        public async Task Squeezing()
        {
            await SafeSerialPortCallAsync(
                async () => await _commandFacade.SqueezingAsync(IsTubeSqueezing ? "0" : "1")
           ,
           (ret) => Msg = "收到挤压响应");
        }

        [RelayCommand]
        public async Task Pierced()
        {
            await SafeSerialPortCallAsync(async () =>
               await _commandFacade.PiercedAsync(IsTubePierced ? "0" : "1")
           ,
           (ret) => Msg = "收到穿刺响应");
        }

        CustomDialog customDialog = new CustomDialog();

        public void ShowResultDetails(TestResult testResult)
        {
            Log.Information($"收到检测结果: {JsonConvert.SerializeObject(testResult)}");
            //testResult = _homeService.GetTestResultAndPoint(testResult.Id);
            ResultDetailsViewModel resultDetailsViewModel = new ResultDetailsViewModel();
            resultDetailsViewModel.Result = testResult;
            resultDetailsViewModel.CloseAction = () =>
            {
                _dialogCoordinator.HideMetroDialogAsync(this, customDialog);
            };
            ResultDetailsControl resultDetailsControl = new ResultDetailsControl();
            resultDetailsControl.Update(resultDetailsViewModel);
            customDialog.Content = resultDetailsControl;

            _dialogCoordinator.ShowMetroDialogAsync(this, customDialog);
        }
    }
}
