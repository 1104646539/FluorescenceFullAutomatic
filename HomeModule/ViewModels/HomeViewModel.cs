using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.HomeModule.Services;
using FluorescenceFullAutomatic.HomeModule.StateMachine;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Model.Events;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.StateMachine;
using FluorescenceFullAutomatic.Platform.Utils;
using FluorescenceFullAutomatic.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using static FluorescenceFullAutomatic.UploadModule.Upload.Hl7Result;

namespace FluorescenceFullAutomatic.HomeModule.ViewModels
{
    /// <summary>
    /// 首页
    /// </summary>
    public partial class HomeViewModel : ObservableRecipient
    {
        #region 属性
        [ObservableProperty]
        public string title;

        private readonly IHomeService homeService;
        private readonly ISerialPortService serialPortService;
        private readonly ISerialPortCommandFacade serialPortCommandFacade;
        private readonly IDispatcherService dispatcherService;
        private readonly IConfigService configRepository;
        private readonly IToolService toolRepository;
        private readonly IProjectService projectRepository;
        private readonly ILogService logService;
        private readonly IEventMailboxService mailboxService;
        private readonly DetectionStateMachine detectionStateMachine;

        [ObservableProperty]
        public SampleShelfViewModel sampleShelfViewModel;

        /// <summary>
        /// 获取温度间隔
        /// </summary>
        private const int GetReactionTempInterval = 1000 * 30; // 30秒

        // 命令执行 状态跟踪（保留必要的标志位）
        /// <summary>
        /// 自检命令是否完成
        /// </summary>
        private bool SelfInspectionFinished { get; set; }

        /// <summary>
        /// 获取/设置反应区温度命令是否完成
        /// </summary>
        private bool ReactionTempFinished { get; set; }

        /// <summary>
        /// 卡仓数量
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private int cardNum;

        /// <summary>
        /// 清洗液是否存在
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private bool cleanoutFluidExist;

        /// <summary>
        /// 反应区温度
        /// </summary>
        [ObservableProperty]
        private string reactionTemp;

        [ObservableProperty]
        private string stateMsg;

        /// <summary>
        /// 是否是第一次加载
        /// </summary>
        private bool FirstLoad = true;

        /// <summary>
        /// 是否是检测前想要自检的（保留：用于 ReceiveGetSelfMachineStatusModel 判断是否处理响应）
        /// </summary>
        private bool IsTestGetSelfMachineState = true;
     

        /// <summary>
        /// 获取反应区温度定时器
        /// </summary>
        private Timer _getReactionTempTimer;

        [ObservableProperty]
        private string imgCard;

        [ObservableProperty]
        private string imgCleanout;

        [ObservableProperty]
        private Visibility showDebugView;
        #endregion
        public HomeViewModel(
            ISerialPortService serialPortService,
            ISerialPortCommandFacade serialPortCommandFacade,
            IHomeService homeService,
            IConfigService configRepository,
            IDispatcherService dispatcherService,
            IToolService toolRepository,
            IProjectService projectRepository,
            ILogService logService,
            IEventMailboxService mailboxService
        )
        {
            this.projectRepository = projectRepository;
            this.logService = logService;
            this.configRepository = configRepository;
            this.toolRepository = toolRepository;
            this.serialPortService = serialPortService;
            this.serialPortCommandFacade = serialPortCommandFacade;
            this.homeService = homeService;
            this.dispatcherService = dispatcherService;
            this.mailboxService = mailboxService;
            //线程邮箱
            this.mailboxService.Subscribe(HandlerEventAsync);
            detectionStateMachine = new DetectionStateMachine(logService, mailboxService, serialPortCommandFacade, serialPortService, homeService, configRepository, projectRepository, toolRepository);
            this.mailboxService.Start();
            SampleShelfViewModel = new SampleShelfViewModel();
            this.configRepository.AddDebugModeChangedListener(OnDebugModeChange);
            ChangeTestSettings();
            this.SampleShelfViewModel.onSelectedSampleItem += OnSampleItemSelected;
            OnSampleItemSelected(null);
            RegisterMsg();
            UpdateState();

            homeService.Hl7IsRunning();
            OnDebugModeChange(configRepository.GetDebugMode());
        }

        private async Task HandlerEventAsync(ITestEvent evt)
        {
            try
            {
                dispatcherService.Invoke(async () =>
                {
                    switch (evt)
                    {
                        // ========== 用户操作事件 ==========
                        case StartTestEvent e:
                            await detectionStateMachine.FireAsync(DetectionTrigger.StartDetection);
                            break;
                        case SelfInspectionRequestEvent e:
                            await detectionStateMachine.FireAsync(DetectionTrigger.RequestSelfInspection);
                            break;
                        case CardAddedConfirmEvent e:
                            await detectionStateMachine.FireAsync(DetectionTrigger.CardAvailable);
                            break;
                        case CancelDetectionRequestEvent e:
                            await detectionStateMachine.FireAsync(DetectionTrigger.CancelDetection);
                            break;
                        // ========== UI 事件（状态机通知 UI 显示/隐藏对话框） ==========
                        case SelfInspectionStartedEvent e:
                            ShowSelfMachineDialog();
                            break;
                        case DetectionValidationErrorEvent e:
                            HandleDetectionValidationError(e.ErrorType);
                            break;
                        case MachineStatusValidationErrorEvent e:
                            HandleMachineStatusValidationError(e.ErrorMessage);
                            break;
                            
                        // ========== 硬件反馈事件（仅更新 UI 状态，逻辑已迁移到状态机回调中直接处理） ==========
                        case SelfInspectionCompletedEvent e:
                            ReceiveGetSelfMachineStatusModel(e.Result);
                            break;
                        case MachineStatusReceivedEvent e:
                            ReceiveMachineStatusModel(e.Result,e.Reason);
                            break;
                        case MoveSampleShelfCompletedEvent e:
                            ReceiveMoveSampleShelfModel(e.Result);
                            break;
                        case MoveSampleCompletedEvent e:
                            ReceiveMoveSampleModel(e.Result);
                            break;
                        case SamplingCompletedEvent e:
                            ReceiveSamplingModel(e.Result);
                            break;
                        case AddingSampleCompletedEvent e:
                            ReceiveAddingSampleModel(e.Result);
                            break;
                        case PushCardCompletedEvent e:
                            ReceivePushCardModel(e.Result);
                            break;
                        case GetReactionTempCompletedEvent e:
                            ReceiveReactionTempModel(e.Result);
                            break;
                        case BarcodeScanCompletedEvent e:
                            ReceiveBarcodeScannedModel(e);
                            break;
                        case NoCardAvailableEvent e:
                            HandleNoCardAvailable(e.CurrentCardNum);
                            break;
                        case SamplingFinishedEvent e:
                            HandleSamplingFinished(e.HintMessage);
                            break;
                        case TestResultAddedEvent e:
                            HandleTestResultAdded(e.TestResult);
                            break;
                        case TestResultUpdatedEvent e:
                            HandleTestResultUpdated(e.TestResultId);
                            break;
                        case SerialPortErrorEvent e:
                            HandleSerialPortError(e);
                            break;
                        default:
                            logService.Info($"未处理的事件: {evt.GetType().Name}");
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                logService.Error($"处理失败 error: {ex}");
            }
        }
        private void ReceiveBarcodeScannedModel(BarcodeScanCompletedEvent e)
        {
            logService.Info($"[VM] 收到扫码反馈 Success={e.Success} Barcode={e.Barcode}");
            // 扫码逻辑已下沉到 SM.HandleBarcodeReceived
            // 这里仅需更新样本架 UI 状态
            SampleShelfViewModel.UpdateSampleItems(
                detectionStateMachine.Context.CurrentShelfPos,
                detectionStateMachine.Context.CurrentSamplePos,
                (item) =>
                {
                    item.State = e.Success ? SampleState.ScanSuccess : SampleState.ScanFailed;
                    return item;
                }
            );
        }
   

        /// <summary>
        /// 处理检测卡不足事件，提示用户添加检测卡
        /// </summary>
        private void HandleNoCardAvailable(int currentCardNum)
        {
            logService.Warning($"[VM] 检测卡不足，当前数量: {currentCardNum}");

            dispatcherService.Invoke(async () =>
            {
                // 显示提示对话框，让用户添加检测卡后点击继续
                var result = MessageBox.Show(
                    $"检测卡不足，请添加检测卡后点击[确定]继续\n当前检测卡数量: {currentCardNum}",
                    "提示",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.OK)
                {
                    // 用户点击确定，发送事件通知状态机继续流程
                    logService.Info("[VM] 用户确认已添加检测卡，继续流程");
                    mailboxService.Post(new CardAddedConfirmEvent());
                }
                else
                {
                    // 用户取消，暂不处理，等待下一次操作
                    logService.Info("[VM] 用户取消添加检测卡，等待下一次操作");
                }
            });
        }

        /// <summary>
        /// 处理取样结束事件
        /// </summary>
        private void HandleSamplingFinished(string hintMessage)
        {
            logService.Info($"[VM] 取样结束: {hintMessage}");

            dispatcherService.Invoke(() =>
            {
                // 显示取样结束提示
                homeService.ShowHiltDialog(
                    this,
                    "提示",
                    hintMessage,
                    "确定",
                    (d, dialog) => { }
                );
            });
        }

        private void OnDebugModeChange(bool debugMode)
        {
            this.ShowDebugView = debugMode ? Visibility.Visible : Visibility.Collapsed;
        }


        [RelayCommand]
        public void ClickTest1()
        {
            toolRepository.HideTaskBar();
        }

        [RelayCommand]
        public async void ClickTest2()
        {
            toolRepository.ShowTaskBar();
        }

        [RelayCommand]
        public void ClickTest3() { }

        [RelayCommand]
        public void ClickTest4() { }

        public void GetDeviceInfos()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (d.DriveType == DriveType.Removable) // 检测是否为可移动驱动器，如U盘
                {
                    //MessageBox.Show($"Found removable drive: {d.Name}");
                    logService.Info($"Found removable drive: {d.Name} {d.VolumeLabel}");
                }
            }
        }

        [ObservableProperty]
        SampleItem selectedSampleItem;

        private void OnSampleItemSelected(SampleItem item)
        {
            if (item != null)
            {
                item.TestResult = homeService.GetTestResult(item.ResultId);
            }
            SelectedSampleItem = item;
        }

        protected override void Broadcast<T>(T oldValue, T newValue, string? propertyName)
        {
            base.Broadcast(oldValue, newValue, propertyName);

            if (propertyName == nameof(CleanoutFluidExist) || propertyName == nameof(CardNum))
            {
                UpdateState();
            }
        }

        private void UpdateState()
        {
            ImgCard =
                CardNum > 0 ? "../Image/cardhouse_success.png" : "../Image/cardhouse_error.png";
            ImgCleanout = CleanoutFluidExist
                ? "../Image/cleanout_success.png"
                : "../Image/cleanout_error.png";
        }

        private void RegisterMsg()
        {
            WeakReferenceMessenger.Default.Register<MainStatusChangeMsg>(
                this,
                (r, m) =>
                {
                    if (m.What == MainStatusChangeMsg.What_ClickTest)
                    {
                        ClickStart();
                    }
                    else if (m.What == MainStatusChangeMsg.What_ChangeState)
                    {
                        // 状态变更时，统一更新本地状态文字描述
                        StateMsg = SystemGlobal.MachineStatus.GetDescription();
                    }
                }
            );
            WeakReferenceMessenger.Default.Register<EventMsg<string>>(
                this,
                (r, m) =>
                {
                    if (m.What == EventWhat.WHAT_CHANGE_TEST_SETTINGS)
                    {
                        ChangeTestSettings();
                    }
                }
            );
        }

        private void ChangeTestSettings()
        {
            this.homeService.SetDequeueDuration(configRepository.ReactionDuration());
        }

        [RelayCommand]
        public void Loaded()
        {
            logService.Info($"Loaded FirstLoad={FirstLoad}");
            if (FirstLoad)
            {
                FirstLoad = false;

                requestSelfMachineState();
            }
        }

        [RelayCommand]
        public void ClickSelfMachineStatus()
        {
            FirstLoad = true;
            logService.Info($"Loaded FirstLoad={FirstLoad}");
            IsTestGetSelfMachineState = true;
            requestSelfMachineState();
        }
        

        private void SetMachineStatus(MachineStatus state)
        {
            SystemGlobal.MachineStatus = state;
            // 通知 UI 变更（现在主要由状态机触发，VM 手动触发时也保持一致）
            WeakReferenceMessenger.Default.Send(
                new MainStatusChangeMsg() { What = MainStatusChangeMsg.What_ChangeState }
            );
        }

        /// <summary>
        /// 初始化 VM 状态
        /// 注：核心位置变量已迁移到 StateContext，此处仅重置 VM 本地 UI 相关状态
        /// 位置信息通过 SamplePositionChangedEvent 从状态机同步
        /// </summary>
        private void InitState()
        {
            SetMachineStatus(MachineStatus.Sampling);
            SystemGlobal.TestType = TestType.Test;
            SampleShelfViewModel.Clear();

            // 重置所有状态标志为 false
            ResetAllStatusFlags();
        }

        /// <summary>
        /// 重置所有命令状态标志 false
        /// </summary>
        private void ResetAllStatusFlags()
        {
            SelfInspectionFinished = false;
            ReactionTempFinished = false;
        }

        [RelayCommand]
        public void ClickStart()
        {
            mailboxService.Post(new StartTestEvent());
        }

        /// <summary>
        /// 处理检测启动校验失败的情况
        /// </summary>
        private void HandleDetectionValidationError(DetectionValidationErrorType errorType)
        {
            switch (errorType)
            {
                case DetectionValidationErrorType.SelfInspectionFailed:
                    ShowSelfMachineErrorDialog();
                    break;
                case DetectionValidationErrorType.NotSelfInspected:
                    homeService.ShowHiltDialog(this, "提示", "仪器尚未完成自检，请先进行自检。", "确定", (d, dialog) => { });
                    break;
                case DetectionValidationErrorType.AlreadyTesting:
                    homeService.ShowHiltDialog(this, "提示", "仪器正在检测中，请等待检测完成。", "确定", (d, dialog) => { });
                    break;
                case DetectionValidationErrorType.ReactionAreaFull:
                    homeService.ShowHiltDialog(this, "提示", "反应区已满，请等待部分检测完成后再开始。", "确定", (d, dialog) => { });
                    break;
                case DetectionValidationErrorType.RunningError:
                    homeService.ShowHiltDialog(this, "提示", "仪器处于错误状态，请检查硬件。", "确定", (d, dialog) => { });
                    break;
                default:
                    homeService.ShowHiltDialog(this, "提示", $"无法开始检测: {errorType}", "确定", (d, dialog) => { });
                    break;
            }
        }

        /// <summary>
        /// 处理串口错误事件，显示用户提示
        /// </summary>
        private void HandleSerialPortError(SerialPortErrorEvent e)
        {
            string title = "串口通信错误";
            string message;

            switch (e.ErrorType)
            {
                case SerialPortErrorType.Timeout:
                    message = $"串口通信超时\n{e.ErrorMessage}";
                    break;
                case SerialPortErrorType.CommandDuplicate:
                    message = $"串口命令重复发送\n{e.ErrorMessage}";
                    break;
                case SerialPortErrorType.DeviceError:
                    message = $"设备执行错误\n命令码: {e.CommandCode}\n{e.ErrorMessage}";
                    break;
                default:
                    message = $"串口通信异常\n{e.ErrorMessage}";
                    break;
            }

            logService.Error($"[VM] 串口错误: {e.ErrorType} - {e.ErrorMessage}");

            dispatcherService.Invoke(() =>
            {
                homeService.ShowHiltDialog(
                    this,
                    title,
                    message,
                    "确定",
                    (d, dialog) => { }
                );
            });
        }

        /// <summary>
        /// 处理仪器状态异常弹窗提示
        /// </summary>
        private void HandleMachineStatusValidationError(string errorMessage)
        {
            homeService.ShowHiltDialog(
                this,
                "提示",
                errorMessage,
                "重新获取",
                (d, dialog) =>
                {
                    logService.Info("状态异常 点击 重新获取状态");
                    requestSelfMachineState();
                },
                "结束检测",
                (d, dialog) =>
                {
                    logService.Info("状态异常 点击 结束检测");
                    mailboxService.Post(new CancelDetectionRequestEvent());
                }
            );
        }

      
        private void requestSelfMachineState()
        {
            InitState();
            mailboxService.Post(new SelfInspectionRequestEvent());
        }
        [RelayCommand]
        public void Insert() { }

        private void RefreshAdd(int id)
        {
            logService.Info($"data 发出添加={id}");
            WeakReferenceMessenger.Default.Send(
                new EventMsg<DataChangeMsg>(new DataChangeMsg() { ID = id })
                {
                    What = EventWhat.WHAT_ADD_DATA,
                }
            );
        }

        private void HandleTestResultAdded(TestResult tr)
        {
            dispatcherService.Invoke(() =>
            {
                TestResults.Add(tr);
            });
        }

        private void HandleTestResultUpdated(int id)
        {
            dispatcherService.Invoke(() =>
            {
                RefreshChange(id);
            });
        }

        private void RefreshChange(int id)
        {
            logService.Info($"data 发出更新={id}");
            WeakReferenceMessenger.Default.Send(
                new EventMsg<DataChangeMsg>(new DataChangeMsg() { ID = id })
                {
                    What = EventWhat.WHAT_CHANGE_DATA,
                }
            );
        }

        [RelayCommand]
        public void ShowDialog() { }

        /// <summary>
        /// 检查是否为正常检测类型
        /// </summary>
        /// <returns>如果是正常检测类型返回true，否则返回false</returns>
        private bool IsNormalTestType()
        {
            if (SystemGlobal.TestType != TestType.Test)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 自检失败错误
        /// </summary>
        string SelfMachineStateError = "";

        public async void ReceiveGetSelfMachineStatusModel(BaseResponseModel<List<string>> model)
        {
            //如果是自己获取，才处理，否则不处理
            if (!IsTestGetSelfMachineState)
            {
                return;
            }
            ClearWaitTestCard();
            IsTestGetSelfMachineState = false;
            SelfInspectionFinished = true;
            logService.Info($"接收到 自检: {JsonConvert.SerializeObject(model)}");
            await CloseSelfMachineDialog();

            StartGetReactionTempTask();
            SelfMachineStateError = JoinSelfMachineError(model.Data);
            
            if (string.IsNullOrEmpty(SelfMachineStateError))
            {
                SetMachineStatus(MachineStatus.SelfInspectionSuccess);
            }
            else
            {
                ShowSelfMachineErrorDialog();
                SetMachineStatus(MachineStatus.SelfInspectionFailed);
            }
        }

        private void ShowSelfMachineErrorDialog()
        {
            homeService.ShowHiltDialog(
                this,
                "提示",
                $"自检失败{SelfMachineStateError}",
                "重新自检",
                async (d, dialog) =>
                {
                    await homeService.HideMetroDialogAsync(this, dialog);
                    logService.Info("自检失败，点击 重新自检");
                    requestSelfMachineState();
                },
                "暂不自检",
                (d, dialog) =>
                {
                    logService.Info("自检失败，点击 暂不自检");
                }
            );
        }

        /// <summary>
        /// 清空待检测的检测卡列表
        /// </summary>
        private void ClearWaitTestCard()
        {
            homeService.ReactionAreaQueueClear();
        }

    
        public string JoinSelfMachineError(List<string> data)
        {
            if (data == null || data.Count == 0)
            {
                return "";
            }

            // 将代号转换为实际错误信息，并格式化输出
            var errorMessages = data.Select(item =>
                    $"错误代码：{item}，错误信息：{toolRepository.GetString($"error_{item}")}"
                )
                .ToList();
            return "\n" + string.Join("\n", errorMessages);
        }

        /// <summary>
        /// 开始获取反应区温度任务
        /// </summary>
        private void StartGetReactionTempTask()
        {
            if (_getReactionTempTimer != null)
            {
                _getReactionTempTimer.Dispose();
            }
            _getReactionTempTimer = new Timer(
                (state) => { },
                null,
                0,
                GetReactionTempInterval
            );
        }
        /// <summary>
        /// 接收仪器状态数据
        /// </summary>
        /// <param name="model"></param>
        /// <param name="reason">获取仪器状态原因</param>
        public async void ReceiveMachineStatusModel(BaseResponseModel<MachineStatusModel> model, MachineStatusRequestReason reason)
        {
            if (!ContinueTest())
                return;
            logService.Info(
                $"接收到 仪器状态: {JsonConvert.SerializeObject(model)} SampleCurrentPos={detectionStateMachine.Context.CurrentSamplePos}"
            );
             // 更新 UI 状态展示
            switch (reason)
            {
                case MachineStatusRequestReason.AfterSelfInspection:// 自检后
                    ParseMachineStatusCard(model.Data);
                    break;
                case MachineStatusRequestReason.BeforeTest:// 检测前
                    ParseMachineStatus(model.Data);
                    break;
                case MachineStatusRequestReason.BeforePushCard:// 推卡前
                    ParseMachineStatusCard(model.Data);
                    break;
                case MachineStatusRequestReason.BeforeMoveSample:// 移动样本前
                    ParseMachineStatusCard(model.Data);  
                    break;
                default:
                    break;
            }
        }

 
        private void requestMoveSampleShelf(int position)
        {
            mailboxService.Post(new MoveSampleShelfRequestEvent() { Position = position });
        }
        
    
        /// <summary>
        /// 解析仪器状态数据（更新 VM 的 UI 绑定属性）
        /// </summary>
        /// <param name="data"></param>
        private void ParseMachineStatus(MachineStatusModel data)
        {
            ParseMachineStatusCard(data);
            ParseMachineStatusSampleShelf(data);
        }
        /// <summary>
        /// 解析样本架状态（更新 UI 绑定属性）
        /// 注：样本架状态已迁移到 StateContext.SampleShelfStates，此处仅更新 UI 显示相关属性
        /// </summary>
        /// <param name="data"></param>
        private void ParseMachineStatusSampleShelf(MachineStatusModel data)
        {
            InitCurrentSampleShelfState();
            SampleShelfViewModel.UpdateShelfState(data.SampleShelf.Select(x => x == 1).ToArray());
        }

        /// <summary>
        /// 解析卡仓状态（更新 UI 绑定属性）
        /// </summary>
        /// <param name="data"></param>
        private void ParseMachineStatusCard(MachineStatusModel data)
        {
         
            // 状态机会在 context.UpdateFromModel(data) 中更新
            CleanoutFluidExist = data.CleanoutFluid == "1";
            int tempNum = 0;
            int.TryParse(data.CardNum, out tempNum);
            CardNum = tempNum;
        }

        public void ReceiveMoveSampleShelfModel(BaseResponseModel<MoveSampleShelfModel> model)
        {
            if (!ContinueTest())
                return;
            logService.Info($"接收到 移动样本架: {JsonConvert.SerializeObject(model)}");
            
            // 处理移动样本架数据
            InitCurrentSampleShelfState();
        }

        /// <summary>
        /// 初始化当前样本架状态
        /// 注：SampleCurrentPos 由状态机通过 SamplePositionChangedEvent 同步，不在此处重置
        /// </summary>
        private void InitCurrentSampleShelfState()
        {
            TestResults.Clear();
        }

     
        private List<TestResult> TestResults = new List<TestResult>();

        public void ReceiveMoveSampleModel(BaseResponseModel<MoveSampleModel> model)
        {
            if (!ContinueTest())
                return;
            logService.Info(
                $"接收到 移动样本: {JsonConvert.SerializeObject(model)} SampleShelfPos={detectionStateMachine.Context.CurrentShelfPos} SampleCurrentPos={detectionStateMachine.Context.CurrentSamplePos}"
            );

            // 更新 UI 状态
            SampleShelfViewModel.UpdateSampleItems(
               detectionStateMachine.Context.CurrentShelfPos,
                detectionStateMachine.Context.CurrentSamplePos,
                (item) =>
                {
                    if (model.Data.SampleType == MoveSampleModel.None)
                    {
                        item.State = SampleState.NotExist;
                    }
                    else
                    {
                        item.State = SampleState.Exist;
                        item.SampleType = model.Data.SampleType == MoveSampleModel.SampleTube ? SampleType.SampleTube : SampleType.SampleCup;
                        
                        var currentTr = detectionStateMachine.Context.CurrentAddingSampleTestResult;
                        if (currentTr != null)
                        {
                            item.ResultId = currentTr.Id;
                            item.TestResult = currentTr;
                        }
                    }
                    return item;
                }
            );
        }



      
        private void requestSampling(string type, int volume)
        {
            mailboxService.Post(new SamplingRequestEvent() { Type = type, Volume = volume });
        }
        private void requestScanBarcode()
        {
            mailboxService.Post(new ScanBarcodeRequestEvent());
        }

        /// <summary>
        /// 是否需要扫码
        /// </summary>
        /// <returns></returns>
        private bool IsNeedScanBarcode()
        {
            return configRepository.IsScanBarcode();
        }

      
        /// <summary>
        /// 反应区是否已满
        /// </summary>
        /// <returns></returns>
        private bool ReactionAreaIsFull()
        {
            return homeService.ReactionAreaQueueIsFull();
        }

      
        /// <summary>
        /// 样本架复位
        /// </summary>
        private void MoveSampleShelfReset()
        {
            //样本架复位
            requestMoveSampleShelf(-1);
        }

        public void ReceiveSamplingModel(BaseResponseModel<SamplingModel> model)
        {
            if (!ContinueTest())
                return;
            logService.Info($"接收到 取样: {JsonConvert.SerializeObject(model)}");
            // 更新 UI 状态
            // SampleShelfViewModel.UpdateSampleItems(
            //    detectionStateMachine.Context.CurrentShelfPos,
            //     detectionStateMachine.Context.CurrentSamplePos,
            //     (item) =>
            //     {
            //         item.State = SampleState.SamplingCompleted;
            //         return item;
            //     }
            // );
        }

        public void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model)
        {
            if (!ContinueTest())
                return;
            logService.Info($"接收到 加样: {JsonConvert.SerializeObject(model)}");
        }

        /// <summary>
        /// 清洗取样针
        /// </summary>
        private void GoCleanoutSamplingProbe()
        {
            requestCleanoutSamplingProbe();
        }
        private void requestCleanoutSamplingProbe()
        {
            mailboxService.Post(new CleanoutSamplingProbeRequestEvent());
        }
      
        private void requestMoveReactionArea(int x, int y)
        {
            mailboxService.Post(new MoveReactionAreaRequestEvent() { X = x, Y = y });
        }

        public async void ReceivePushCardModel(BaseResponseModel<PushCardModel> model)
        {
            if (!ContinueTest())
                return;
            logService.Info($"接收到 推卡完成: CurrentAddingSampleTestResult {JsonConvert.SerializeObject(model)}");

            if (detectionStateMachine.Context.PushCardSuccess)
            {
                // 更新 UI 状态
                var project = detectionStateMachine.Context.CurrentAddingSampleTestResult?.Project;
                if (project != null)
                {
                    SampleShelfViewModel.UpdateSampleItems(
                     detectionStateMachine.Context.CurrentShelfPos,
                detectionStateMachine.Context.CurrentSamplePos,
                        (item) =>
                        {
                            if (item.TestResult != null)
                            {
                                item.TestResult.ProjectId = project.Id;
                                item.TestResult.Project = project;
                                item.TestResult.CardQRCode = model.Data.QrCode;
                            }
                            return item;
                        }
                    );
                }
            }
        }

      
        /// <summary>
        /// 是否推卡成功
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool IsPushCardSuccess(PushCardModel data)
        {
            return data.Success == PushCardModel.PushCardSuccess
                && !string.IsNullOrEmpty(data.QrCode);
        }

     
        public void ReceiveReactionTempModel(BaseResponseModel<ReactionTempModel> model)
        {
            if (!ContinueTest())
                return;
            ReactionTempFinished = true;
            logService.Info($"接收到 反应区温度: {JsonConvert.SerializeObject(model)}");
            // 处理反应区温度数据

            ReactionTemp = model.Data.Temp;
        }

       
        private bool ContinueTest(bool isTestAction = false)
        {
            if (SystemGlobal.MachineStatus.IsRunningError())
            {
                if (isTestAction && SystemGlobal.ErrorContinueTest)
                {
                    return true;
                }
                return false;
            }
            if (!IsNormalTestType())
                return false;

            return true;
        }

        private ProgressDialogController showSelfController;

        private async void ShowSelfMachineDialog()
        {
            logService.Info("显示自检对话框");
            showSelfController = await homeService.ShowProgressAsync(this, "提示", "正在自检……");
            showSelfController.SetIndeterminate();
        }

        private async Task CloseSelfMachineDialog()
        {

            logService.Info("关闭自检对话框");
            if (showSelfController != null)
            {
                await showSelfController?.CloseAsync();
            }
        }  
      
    }
}
