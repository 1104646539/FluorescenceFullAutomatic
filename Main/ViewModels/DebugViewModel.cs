using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlzEx.Standard;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Views.Ctr;
using FluorescenceFullAutomatic.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Serilog;
using SqlSugar;
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
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Utils;
using FluorescenceFullAutomatic.Core.Model;
using Point = FluorescenceFullAutomatic.Platform.Model.Point;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class DebugViewModel : ObservableObject, IReceiveData
    {
        #region 字段
        [ObservableProperty]
        ObservableCollection<string> motors = new ObservableCollection<string>(){
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
        ObservableCollection<string> dirs = new ObservableCollection<string>(){
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
        // 系统控制 
        /// <summary>
        /// 样本架位置
        /// </summary>
        [ObservableProperty]
        private string sampleShelfPosition;

        /// <summary>
        /// 样本步数
        /// </summary>          
        [ObservableProperty]
        private string samplePos;

        /// <summary>
        /// 样本方向
        /// </summary>
        [ObservableProperty]
        private bool isSampleForward = true;

        [ObservableProperty]
        private bool isSampleBackward;

        [ObservableProperty]
        private string sampleVolume;

        [ObservableProperty]
        private ObservableCollection<string> sampleTypes = new ObservableCollection<string> { "样本管", "样本杯" };

        [ObservableProperty]
        private string selectedSampleType;

        [ObservableProperty]
        private string addingVolume;

        [ObservableProperty]
        private ObservableCollection<string> addingTypes = new ObservableCollection<string> { "样本管", "样本杯" };

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
        private ObservableCollection<string> cardTypes = new ObservableCollection<string> { "单联卡", "双联卡" };

        [ObservableProperty]
        private string selectedCardType;

        [ObservableProperty]
        private ObservableCollection<string> testTypes = new ObservableCollection<string> { "普通卡", "质控卡" };

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
        private ObservableCollection<string> squeezingTypes = new ObservableCollection<string> { "样本管", "样本杯" };

        [ObservableProperty]
        private string selectedSqueezingType;

        [ObservableProperty]
        private ObservableCollection<string> piercedTypes = new ObservableCollection<string> { "样本管", "样本杯" };

        [ObservableProperty]
        private string selectedPiercedType;

        [ObservableProperty]
        private string cleanoutDuration;

        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IConfigService _configRepository;

        
        #endregion
        public DebugViewModel(ISerialPortService serialPortService,IConfigService configRepository,IDialogCoordinator dialogCoordinator)
        {
            this.defaultSerialPortService = serialPortService;
            this._dialogCoordinator = dialogCoordinator;
            this._configRepository = configRepository;
            MotorSelected = Motors.First();
            DirSelected = Dirs.First();
            

            Value = "0";
            OnLoaded();
            //可选则的
            IsDefaultSerialPort = true;

            selectSerialPortService = new SerialPortService();
            selectSerialPortService.AddConnectStateListener((str) =>
            {
                if (IsDefaultSerialPort) { return; }
                if (string.IsNullOrEmpty(str))
                {
                    Msg = "连接成功";
                }
                else
                {
                    Msg = "连接失败";
                }
            });
            
            //默认的
            defaultSerialPortService.AddConnectStateListener((str) =>
            {
                if (!IsDefaultSerialPort) { return; }
                if (string.IsNullOrEmpty(str))
                {
                    Msg = "连接成功";
                }
                else
                {
                    Msg = "连接失败";
                }
            });
            currentSerialPortService = defaultSerialPortService;
            IsCmdRunningFinish = true;
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
        public void Closed() {
            //selectSerialPortService?.RemoveReceiveData(this);
            //selectSerialPortService?.Disconnect();
            //currentSerialPortService?.RemoveReceiveData(this);
            //currentSerialPortService?.RemoveOriginReceiveDataListener(OnOriginReceiveDataListener);
            //currentSerialPortService.RemoveOriginSendDataListener(OnOriginSendDataListener);
            SystemGlobal.TestType = TestType.None;
        }
        public void OnLoaded()
        {
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                SerialPorts.Add(port);
            }
            SelectedSerialPort = SerialPorts.First();
        }
        [RelayCommand]
        public void ClickChangeSerialPort() {
            ChangeSerialPort(!IsDefaultSerialPort);
        }
        public void ChangeSerialPort(bool isDefault) {
            IsDefaultSerialPort = isDefault;
            BtnChangeSerialPortMsg = IsDefaultSerialPort ? "切换到可选串口" : "切换到默认串口";
            //currentSerialPortService?.RemoveReceiveData(this);
            //currentSerialPortService?.Disconnect();
            //currentSerialPortService?.RemoveOriginReceiveDataListener(OnOriginReceiveDataListener);
            //currentSerialPortService.RemoveOriginSendDataListener(OnOriginSendDataListener);

            currentSerialPortService = IsDefaultSerialPort ? defaultSerialPortService : selectSerialPortService;
            if (currentSerialPortService == defaultSerialPortService) {
                ReconnecntDefault();
            }
            currentSerialPortService.AddReceiveData(this);
            currentSerialPortService.AddOriginReceiveDataListener(OnOriginReceiveDataListener);
            currentSerialPortService.AddOriginSendDataListener(OnOriginSendDataListener);
            currentSerialPortService.AddScanSuccessListener(OnScanSuccess);
            currentSerialPortService.AddScanFailedListener(OnScanFailed);
        }

        private void OnScanFailed(string msg)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到扫码失败 {msg}";
            SystemGlobal.TestType = TestType.Test;
        }

        private void OnScanSuccess(string msg)
        {
            if (!IsDebugTestType()) return;
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
            if (!ShowResponse) {
                if (str.ToArray()[0] == '0') {
                    return;
                }
            }
            OriginMsg+=$"{DateTime.Now.GetDateTimeString2()} 发出:{str}";
        }
        public void OnOriginReceiveDataListener(string str) {
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
                OriginMsg +=$"{str}";
            }
        }
        [RelayCommand]
        public void ClickClearInfo() {
            OriginMsg = "";
        }
        public void ReconnecntDefault() {
            //if (IsDefaultSerialPort) {
            //    currentSerialPortService.Connect(_configService.MainPortName(), _configService.MainPortBaudRate());
            //}
        }
        [RelayCommand]
        public void OpenSerila()
        {
            if (IsDefaultSerialPort) return;
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
                
                SerialPortHelper.Instance.Connect(SelectedSerialPort, _configRepository.MainPortBaudRate());
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

            if (IsDefaultSerialPort) return;
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

        private void ExecuteCommand(Action action)
        {
            if (!IsCmdRunningFinish)
            {
                Msg = "上一个命令还未执行完成";
                return;
            }

            IsCmdRunningFinish = false;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Msg = $"执行命令出错: {ex.Message}";
                IsCmdRunningFinish = true;
            }
        }
        [RelayCommand]
        public void Exec()
        {
            ExecuteSerialPortCommand(() =>
            {
                int indexMotor = Motors.IndexOf(MotorSelected);
                int indexDir = Dirs.IndexOf(DirSelected);
                if (indexMotor == -1 || indexDir == -1)
                {
                    MessageBox.Show("请选择电机和方向");
                    return;
                }
                int intValue = 0;
                int.TryParse(Value, out intValue);

                Msg = $"正在执行 {indexMotor} , {indexDir} , {intValue}";

                currentSerialPortService.Motor(indexMotor + "", indexDir + "", intValue + "");
                Msg = "电机执行命令已发送";
            });
            
        }
        
        [RelayCommand]
        public void OpenScan() {
            Msg = "扫码已发送";
            ExecuteSerialPortCommand(() => {
                currentSerialPortService?.ScanBarcode();
            });
        }

        [RelayCommand]
        public void StopScan()
        {
            Msg = "停止扫码已发送";
            ExecuteSerialPortCommand(() => {
                currentSerialPortService?.StopScanBarcode();
            });
        }
        /// <summary>
        /// 执行串口命令的通用方法
        /// </summary>
        /// <param name="action">要执行的命令</param>
        private void ExecuteSerialPortCommand(Action action)
        {
            if (!CheckSerialPortOpen()) return;
            
            // 设置为调试模式
            SystemGlobal.TestType = TestType.Debug;
            
            ExecuteCommand(() =>
            {
                action();
            });
        }

        [RelayCommand]
        public void GetSelfInspectionState()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.GetSelfInspectionState(_configRepository.ClearReactionArea());
                Msg = "获取自检状态命令已发送";
            });
        }
        [RelayCommand]
        public void GetVersion()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.GetVersion();
                Msg = "获取版本号命令已发送";
            });
        }

        [RelayCommand]
        public void Shutdown()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.Shutdown();
                Msg = "获取关机命令已发送";
            });
        }

        [RelayCommand]
        public void GetMachineState()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.GetMachineState();
                Msg = "获取机器状态命令已发送";
            });
        }

        [RelayCommand]
        public void GetCleanoutFluid()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.GetCleanoutFluid();
                Msg = "获取清洗液状态命令已发送";
            });
        }

        [RelayCommand]
        public void GetSampleShelf()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.GetSampleShelf();
                Msg = "获取样本架状态命令已发送";
            });
        }

        [RelayCommand]
        public void MoveSampleShelf()
        {
            ExecuteSerialPortCommand(() =>
            {
                int position = 0;
                int.TryParse(SampleShelfPosition, out position);
                currentSerialPortService.MoveSampleShelf(position);
                Msg = "移动样本架命令已发送";
            });
        }

        [RelayCommand]
        public void ResetSampleShelf() {
            ExecuteSerialPortCommand(() =>
            {
                int position = 0;
                currentSerialPortService.MoveSampleShelf(position);
                Msg = "移动样本架命令已发送";
            });
        }

        [RelayCommand]
        public void MoveSample()
        {
            ExecuteSerialPortCommand(() =>
            {
                int pos = 0;
                int.TryParse(SamplePos, out pos);
                currentSerialPortService.MoveSample(pos);
                Msg = "移动样本命令已发送";
            });
        }
        

        [RelayCommand]
        public void Sampling()
        {
            ExecuteSerialPortCommand(() =>
            {
                int volume = 0;
                int.TryParse(SampleVolume, out volume);
                string sampleType = IsSampleTube ? "0" : "1";
                currentSerialPortService.Sampling(sampleType, volume);
                Msg = "采样命令已发送";
            });
        }

        [RelayCommand]
        public void CleanoutSamplingProbe()
        {
            ExecuteSerialPortCommand(() =>
            {
                int duration = 0;
                int.TryParse(CleanoutDuration, out duration);
                currentSerialPortService.CleanoutSamplingProbe(duration);
                Msg = "清洗取样针命令已发送";
            });
        }

        [RelayCommand]
        public void AddingSample()
        {
            ExecuteSerialPortCommand(() =>
            {
                int volume = 0;
                int.TryParse(AddingVolume, out volume);
                string addingType = IsAddingAtSampling ? "0" : "1";
                currentSerialPortService.AddingSample(volume, addingType);
                Msg = "加样命令已发送";
            });
        }

        [RelayCommand]
        public void Drainage()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.Drainage();
                Msg = "排液命令已发送";
            });
        }

        [RelayCommand]
        public void PushCard()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.PushCard();
                Msg = "推卡命令已发送";
            });
        }

        [RelayCommand]
        public void MoveReactionArea()
        {
            ExecuteSerialPortCommand(() =>
            {
                int x = 0, y = 0;
                int.TryParse(ReactionAreaX, out x);
                int.TryParse(ReactionAreaY, out y);
                currentSerialPortService.MoveReactionArea(x, y);
                Msg = "移动反应区命令已发送";
            });
        }

        [RelayCommand]
        public void Test()
        {
            ExecuteSerialPortCommand(() =>
            {
                int x = 0, y = 0;
                int.TryParse(TestX, out x);
                int.TryParse(TestY, out y);
                string cardType = IsSingleCard ? "0" : "1";
                string testType = IsNormalTest ? "0" : "1";
                string scanStart = ScanStart;
                string scanEnd = ScanEnd;
                string peakWidth = PeakWidth;
                string peakDistance = PeakDistance;
                currentSerialPortService.Test(x, y, cardType, testType, scanStart, scanEnd, peakWidth, peakDistance);
                Msg = "测试命令已发送";
            });
        }

        [RelayCommand]
        public void GetReactionTemp()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.GetReactionTemp(ReactionTemp);
                Msg = "获取反应温度命令已发送";
            });
        }

        [RelayCommand]
        public void ClearReactionArea()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.ClearReactionArea();
                Msg = "清空反应区命令已发送";
            });
        }

        [RelayCommand]
        public void ResetParams()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.ResetParams();
                Msg = "重置参数命令已发送";
            });
        }

        [RelayCommand]
        public void OpenUpdate()
        {
            ExecuteSerialPortCommand(() =>
            {
                currentSerialPortService.Update();
                Msg = "更新命令已发送";
            });
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
                string flash = GlobalUtil.GetUpdateFlash();
                if (string.IsNullOrEmpty(flash) || !Directory.Exists(flash)) {
                    MessageBox.Show("升级失败，没有找到升级盘符");
                    return;
                }
                try
                {
                    string targetPath = Path.Combine(flash, SystemGlobal.UpdateFileName);
                    File.Copy(UpdateFilePath, targetPath, true);
                    // 如果目标文件存在且为只读，则修改为可写
                    if (File.Exists(targetPath))
                    {
                        FileAttributes attributes = File.GetAttributes(targetPath);
                        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            File.SetAttributes(targetPath, attributes & ~FileAttributes.ReadOnly);
                        }
                        MessageBox.Show("升级文件已烧录,请重启仪器");

                    }
                    else {
                        MessageBox.Show($"升级失败：文件传输失败");

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"升级失败：{ex.Message}");
                }
            }
        }

       
        [RelayCommand]
        public void Squeezing()
        {
            ExecuteSerialPortCommand(() =>
            {
                string type = IsTubeSqueezing ? "0" : "1";
                currentSerialPortService.Squeezing(type);
                Msg = "挤压命令已发送";
            });
        }

        [RelayCommand]
        public void Pierced()
        {
            ExecuteSerialPortCommand(() =>
            {
                string type = IsTubePierced ? "0" : "1";
                currentSerialPortService.Pierced(type);
                Msg = "刺破命令已发送";
            });
        }

        // 接收数据的方法
        public void ReceiveGetSelfMachineStatusModel(BaseResponseModel<List<string>> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到自检状态响应: {string.Join(", ", model.Data)}";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveMachineStatusModel(BaseResponseModel<MachineStatusModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到机器状态响应: 卡仓存在={model.Data.CardExist}, 卡仓数量={model.Data.CardNum}, 清洗液存在={model.Data.CleanoutFluid}, 样本架状态={string.Join(",", model.Data.SamleShelf)}";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveMoveSampleShelfModel(BaseResponseModel<MoveSampleShelfModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到移动样本架响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveMoveSampleModel(BaseResponseModel<MoveSampleModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到移动样本响应: 样本类型={model.Data.SampleType}";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveSamplingModel(BaseResponseModel<SamplingModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到采样响应: 结果={model.Data.Result}";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveCleanoutSamplingProbeModel(BaseResponseModel<CleanoutSamplingProbeModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到清洗采样针响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到加样响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveDrainageModel(BaseResponseModel<DrainageModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到排液响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceivePushCardModel(BaseResponseModel<PushCardModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到推卡响应: 成功={model.Data.Success} {model.Data.QrCode}";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveMoveReactionAreaModel(BaseResponseModel<MoveReactionAreaModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到移动反应区响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveTestModel(BaseResponseModel<TestModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到测试响应: T={model.Data.T}, C={model.Data.C}, T2={model.Data.T2}, C2={model.Data.C2}, 卡片类型={model.Data.CardType}, 测试类型={model.Data.TestType}, 点数={model.Data.Point?.Count ?? 0}";
            SystemGlobal.TestType = TestType.Test;

            ShowResultDetails(new TestResult() {
                Point = new Point() {
                    Points = model.Data.Point.ToArray(),
                    Location = model.Data.Location.ToArray(),
                },
                T = model.Data.T,
                C = model.Data.C
            });
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
        public void ReceiveReactionTempModel(BaseResponseModel<ReactionTempModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到反应区温度响应: 温度={model.Data.Temp}";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveClearReactionAreaModel(BaseResponseModel<ClearReactionAreaModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到清空反应区响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveMotorModel(BaseResponseModel<MotorModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"电机执行结束: 复位状态={model.Data.RestState}";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveResetParamsModel(BaseResponseModel<ResetParamsModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到重置参数响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveUpdateModel(BaseResponseModel<UpdateModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到更新响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveSqueezingModel(BaseResponseModel<SqueezingModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到挤压响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceivePiercedModel(BaseResponseModel<PiercedModel> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到穿刺响应";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveStateError(BaseResponseModel<dynamic> model)
        {
            if (!IsDebugTestType()) return;
            IsCmdRunningFinish = true;
            Msg = $"收到错误响应 code={model.Code} state={model.State} error={model.Error}";
            SystemGlobal.TestType = TestType.Test;
        }
        public void ReceiveVersionModel(BaseResponseModel<VersionModel> model)
        {
            if (!IsDebugTestType())
                return;
            IsCmdRunningFinish = true;
            Msg = $"收到版本号响应 {model.Data.Ver}";
            SystemGlobal.TestType = TestType.Test;
        }

        public void ReceiveShutdownModel(BaseResponseModel<ShutdownModel> model)
        {
            if (!IsDebugTestType())
                return;

            IsCmdRunningFinish = true;
            Msg = $"收到关机响应";
            SystemGlobal.TestType = TestType.Test;
        }
    }
}
