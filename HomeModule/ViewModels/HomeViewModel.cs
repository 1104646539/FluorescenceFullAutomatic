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
        public ReactionAreaViewModel reactionAreaViewModel;

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
        /// 检测命令是否完成
        /// </summary>
        private bool TestFinished { get; set; }

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
        /// 样本架当前位置
        /// </summary>
        private int SampleShelfPos = -1;

        /// <summary>
        /// 当前样本位置
        /// </summary>
        private int SampleCurrentPos = 0;
        /// <summary>
        /// 是否是第一次加载
        /// </summary>
        private bool FirstLoad = true;

        /// <summary>
        /// 是否是检测前想要自检的（保留：用于 ReceiveGetSelfMachineStatusModel 判断是否处理响应）
        /// </summary>
        private bool IsTestGetSelfMachineState = true;
        
        /// <summary>
        /// 检测结束要显示的提示
        /// </summary>
        string TestFinishedHiltMsg = "";

        /// <summary>
        /// 是否正在检测
        /// </summary>
        private bool IsTesting = false;

       

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
            this.homeService._dequeueCallback += OnReactionAreaDequeue;
            this.mailboxService = mailboxService;
            //线程邮箱
            this.mailboxService.Subscribe(HandlerEventAsync);
            detectionStateMachine = new DetectionStateMachine(logService, mailboxService, serialPortCommandFacade, serialPortService, homeService, configRepository, projectRepository);
            this.mailboxService.Start();
            // this.serialPortService.AddReceiveData(this);
            SampleShelfViewModel = new SampleShelfViewModel();
            ReactionAreaViewModel = ReactionAreaViewModel.Instance;
            this.serialPortService.AddScanSuccessListener(OnScanSuccess);
            this.serialPortService.AddScanFailedListener(OnScanFailed);
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
                        case TestRequestEvent e:
                            // 检测操作独立于取样状态机流转，由反应区队列时间驱动
                            // 统一由状态机管理，以便监听检测结果并更新 UI
                            await detectionStateMachine.ExecuteTestAsync(e.item);
                            break;
                        // ========== UI 事件（状态机通知 UI 显示/隐藏对话框） ==========
                        case SelfInspectionStartedEvent e:
                            ShowSelfMachineDialog();
                            break;
                        case DetectionValidationErrorEvent e:
                            HandleDetectionValidationError(e.ErrorKey);
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
                        // case CleanoutSamplingProbeCompletedEvent e:
                        //     ReceiveCleanoutSamplingProbeModel(e.Result);
                        //     break;
                        case AddingSampleCompletedEvent e:
                            ReceiveAddingSampleModel(e.Result);
                            break;
                        case PushCardCompletedEvent e:
                            ReceivePushCardModel(e.Result);
                            break;
                        case TestCompletedEvent e:
                            ReceiveTestModel(e.Result);
                            break;
                        case GetReactionTempCompletedEvent e:
                            ReceiveReactionTempModel(e.Result);
                            break;
                        // case DrainageCompletedEvent e:
                        //     ReceiveDrainageModel(e.Result);
                        //     break;
                        case BarcodeScanCompletedEvent e:
                            ReceiveBarcodeScannedModel(e);
                            break;
                        case MoveReactionAreaCompletedEvent e:
                            ReceiveMoveReactionAreaModel(e.Result);
                            break;
                        case ApplyTestIdentifiedEvent e:
                            HandleApplyTestIdentified(e.ApplyTest);
                            break;
                        case NoCardAvailableEvent e:
                            HandleNoCardAvailable(e.CurrentCardNum);
                            break;
                        case SamplingFinishedEvent e:
                            HandleSamplingFinished(e.HintMessage);
                            break;
                        case SamplePositionChangedEvent e:
                            HandleSamplePositionChanged(e);
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
            if (e.Success)
            {
                // 仅更新 UI 状态
                logService.Info($"[VM] 扫码成功 UI 更新: {e.Barcode}");
                SampleShelfViewModel.UpdateSampleItems(
                    SampleShelfPos,
                    SampleCurrentPos,
                    (item) =>
                    {
                        item.State = SampleState.ScanSuccess;
                        return item;
                    }
                );
                UpdateTestResultForSamplePos(
                    SampleCurrentPos,
                    (item) =>
                    {
                        item.ResultState = ResultState.ScanSuccess;
                        item.Barcode = e.Barcode;
                        return item;
                    }
                );
            }
            else
            {
                logService.Warning($"[VM] 扫码失败 UI 更新");
                SampleShelfViewModel.UpdateSampleItems(
                    SampleShelfPos,
                    SampleCurrentPos,
                    (item) =>
                    {
                        item.State = SampleState.ScanFailed;
                        return item;
                    }
                );
            }
        }
        /// <summary>
        /// 处理状态机识别到的申请信息
        /// </summary>
        private void HandleApplyTestIdentified(ApplyTest applyTest)
        {
            if (applyTest == null) return;

            dispatcherService.Invoke(() =>
            {
                // 更新当前样本位的病人信息展示
                UpdateTestResultForSamplePos(
                    SampleCurrentPos,
                    (item) =>
                    {
                        item.Patient = applyTest.Patient;
                        item.PatientId = applyTest.PatientId;
                        return item;
                    }
                );

                // 发送数据变更通知以刷新 UI
                RefreshChange(-1); // 或者传入具体的 ID
                logService.Info($"[VM] 已同步状态机识别的申请信息: {applyTest.Patient?.PatientName}");
            });
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
                    // 用户点击确定，触发 CardAvailable 继续流程
                    logService.Info("[VM] 用户确认已添加检测卡，继续流程");
                    await detectionStateMachine.FireAsync(DetectionTrigger.CardAvailable);
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

        /// <summary>
        /// 处理样本位置变更事件（从状态机 context 同步位置信息到 VM）
        /// </summary>
        private void HandleSamplePositionChanged(SamplePositionChangedEvent e)
        {
            logService.Info($"[VM] 收到位置同步事件: ShelfPos={e.ShelfPos}, SamplePos={e.SamplePos}, SampleType={e.SampleType}");

            // 同步位置信息（从状态机中获取，不再本地维护）
            SampleShelfPos = e.ShelfPos;
            SampleCurrentPos = e.SamplePos;
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
        

        private void GoGetSelfMachineStatus()
        {
            logService.Info("自检失败，点击重新自检 GoGetSelfMachineStatus");
            IsTestGetSelfMachineState = true;
            SetMachineStatus(MachineStatus.None);

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

            // 位置变量重置为初始值，实际值由状态机通过 SamplePositionChangedEvent 同步
            SampleCurrentPos = 0;
            SampleShelfPos = -1;
            // ReactionAreaX = -1;这两个不能初始化，因为可能正在检测
            // ReactionAreaY = 0;
            TestFinishedHiltMsg = "";
            
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
            TestFinished = false;
            ReactionTempFinished = false;
            // 其他标志位已迁移到状态机的 StateContext 中管理
        }

        [RelayCommand]
        public void ClickStart()
        {
            mailboxService.Post(new StartTestEvent());
        }

        /// <summary>
        /// 处理检测启动校验失败的情况
        /// </summary>
        private void HandleDetectionValidationError(string errorKey)
        {
            switch (errorKey)
            {
                case "SelfInspectionFailed":
                    ShowSelfMachineErrorDialog();
                    break;
                case "NotSelfInspected":
                    homeService.ShowHiltDialog(this, "提示", "仪器尚未完成自检，请先进行自检。", "确定", (d, dialog) => { });
                    break;
                case "AlreadyTesting":
                    homeService.ShowHiltDialog(this, "提示", "仪器正在检测中，请等待检测完成。", "确定", (d, dialog) => { });
                    break;
                case "ReactionAreaFull":
                    homeService.ShowHiltDialog(this, "提示", "反应区已满，请等待部分检测完成后再开始。", "确定", (d, dialog) => { });
                    break;
                case "RunningError":
                    homeService.ShowHiltDialog(this, "提示", "仪器处于错误状态，请检查硬件。", "确定", (d, dialog) => { });
                    break;
                default:
                    homeService.ShowHiltDialog(this, "提示", $"无法开始检测: {errorKey}", "确定", (d, dialog) => { });
                    break;
            }
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
                    _ = detectionStateMachine.FireAsync(DetectionTrigger.CancelDetection);
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
                // logService.Info($"非正常检测类型，当前类型：{SystemGlobal.TestType}");
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
            ClearReactionAreaState();
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

        /// <summary>
        /// 清空反应区状态
        /// </summary>
        private void ClearReactionAreaState()
        {
            ReactionAreaViewModel.Clear();
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
                (state) =>
                {
                    //GetReactionTemp();
                },
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
                $"接收到 仪器状态: {JsonConvert.SerializeObject(model)} SampleCurrentPos={SampleCurrentPos}"
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
                $"接收到 移动样本: {JsonConvert.SerializeObject(model)} SampleShelfPos={SampleShelfPos} SampleCurrentPos={SampleCurrentPos}"
            );

            // 处理移动样本数据 (VM 仅负责更新 UI 状态和本地 TestResults 集合)
            if (model.Data.SampleType == MoveSampleModel.None)
            {
                InsertTestResult(null);
                detectionStateMachine.Context.CurrentAddingSampleTestResult = TestResults[SampleCurrentPos];
                SampleShelfViewModel.UpdateSampleItems(
                    SampleShelfPos,
                    SampleCurrentPos,
                    (item) =>
                    {
                        item.State = SampleState.NotExist;
                        return item;
                    }
                );
            }
            else if (model.Data.SampleType == MoveSampleModel.SampleTube)
            {
                string testNum = configRepository.TestNumIncrement() + "";
                TestResult tr = InsertTestResult(new TestResult() { TestNum = testNum });
                detectionStateMachine.Context.CurrentAddingSampleTestResult = TestResults[SampleCurrentPos];
                SampleShelfViewModel.UpdateSampleItems(
                    SampleShelfPos,
                    SampleCurrentPos,
                    (item) =>
                    {
                        item.State = SampleState.Exist;
                        item.SampleType = SampleType.SampleTube;
                        item.ResultId = tr.Id;
                        return item;
                    }
                );
                RefreshAdd(tr.Id);
            }
            else if (model.Data.SampleType == MoveSampleModel.SampleCup)
            {
                string testNum = configRepository.TestNumIncrement() + "";
                TestResult tr = InsertTestResult(new TestResult() { TestNum = testNum });
                detectionStateMachine.Context.CurrentAddingSampleTestResult = TestResults[SampleCurrentPos];
                SampleShelfViewModel.UpdateSampleItems(
                    SampleShelfPos,
                    SampleCurrentPos,
                    (item) =>
                    {
                        item.State = SampleState.Exist;
                        item.SampleType = SampleType.SampleCup;
                        item.ResultId = tr.Id;
                        return item;
                    }
                );
                RefreshAdd(tr.Id);
            }
        }

        private TestResult InsertTestResult(TestResult testResult)
        {
            if (testResult != null)
            {
                int id = homeService.InsertTestResult(testResult);
                testResult.Id = id;
            }
            TestResults.Add(testResult);
            return testResult;
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
        /// 去扫码
        /// </summary>
        private void ScanBarcode()
        {

            serialPortService.ScanBarcode();
        }

        private void OnScanFailed(string error)
        {
            mailboxService.Post(new BarcodeScanCompletedEvent() { Barcode = error, Success = false });
         
        }

        private void OnScanSuccess(string barcode)
        {
            mailboxService.Post(new BarcodeScanCompletedEvent() { Barcode = barcode, Success = true });
           
        }

        /// <summary>
        /// 扫码成功，取样推卡
        /// </summary>
        /// <param name="barcode"></param>
        private void ScanSuccess(string barcode)
        {
            logService.Info($"收到 扫码成功:{barcode}");
            //记录条码
            SampleShelfViewModel.UpdateSampleItems(
                SampleShelfPos,
                SampleCurrentPos,
                (item) =>
                {
                    item.State = SampleState.ScanSuccess;
                    return item;
                }
            );
            //更新结果
            UpdateTestResultForSamplePos(
                SampleCurrentPos,
                (item) =>
                {
                    item.ResultState = ResultState.ScanSuccess;
                    item.Barcode = barcode;
                    return item;
                }
            );
            //获取信息
            TestResult tr = TestResults[SampleCurrentPos];

            //实时获取申请信息，根据条码
            RealTimeGetApplyTest(tr);

            TestResults[SampleCurrentPos].Barcode = barcode;
        }

        /// <summary>
        /// 实时获取申请信息
        /// </summary>
        /// <param name="tr"></param>
        private void RealTimeGetApplyTest(TestResult tr)
        {
            dispatcherService.InvokeAsync(async () =>
            {
                bool isNeedLisGet = homeService.isNeedLisGet();
                bool isMatchingBarcode = homeService.isMatchingBarcode();
                QueryResult qr = await homeService.QueryApplyTestAsync(
                    isNeedLisGet,
                    isMatchingBarcode,
                    tr.Barcode ?? "",
                    tr.TestNum ?? ""
                );
                if (qr.ResultType == QueryResultType.Success)
                {
                    ApplyTest applyTest = qr.ApplyTests.FirstOrDefault();
                    //接收到一个数据，更新检测结果
                    if (applyTest != null)
                    {
                        dispatcherService.Invoke(() =>
                        {
                            applyTest.Patient.InspectDate = DateTime.Now;
                            Patient patientTemp = applyTest.Patient;
                            int patientId = homeService.InsertPatient(patientTemp);
                            patientTemp.Id = patientId;
                            applyTest.PatientId = patientId;
                            applyTest.ApplyTestType = ApplyTestType.TestEnd;
                            int applyTestId = homeService.InsertApplyTest(applyTest);
                            logService.Info(
                                $"插入了 patient={patientId} applyTestId={applyTestId}"
                            );
                            UpdateTestResultForId(
                                tr.Id,
                                (item) =>
                                {
                                    item.Patient = patientTemp;
                                    item.PatientId = patientTemp.Id;
                                    return item;
                                }
                            );
                            //更新样本架的检测结果
                            for (int i = 0; i < TestResults.Count; i++)
                            {
                                if (TestResults[i] != null && TestResults[i].Id == tr.Id)
                                {
                                    TestResults[i].Patient = patientTemp;
                                    TestResults[i].PatientId = patientTemp.Id;
                                    break;
                                }
                            }
                            //刷新申请信息
                            RefreshApplyTest(applyTest);
                            logService.Info(
                                $"申请信息={applyTest.Id} tr={tr.Id} {JsonConvert.SerializeObject(applyTest)} {JsonConvert.SerializeObject(tr)}"
                            );

                            //刷新结果
                            RefreshChange(tr.Id);
                        });
                    }
                    else
                    {
                        logService.Info(
                            $"没有获取到申请信息 isNeedLisGet={isNeedLisGet} isMatchingBarcode={isMatchingBarcode} tr={tr.Id} barcode={tr.Barcode} testNum={tr.TestNum}"
                        );
                    }
                }
                else
                {
                    logService.Info(
                        $"没有获取到申请信息 isNeedLisGet={isNeedLisGet} isMatchingBarcode={isMatchingBarcode} tr={tr.Id} barcode={tr.Barcode} testNum={tr.TestNum}"
                    );
                }
            });
        }

        private void RefreshApplyTest(ApplyTest applyTest)
        {
            homeService.UpdateApplyTestCompleted(applyTest);
            WeakReferenceMessenger.Default.Send(
                new EventMsg<DataChangeMsg>(new DataChangeMsg() { ID = applyTest.Id })
                {
                    What = EventWhat.WHAT_CHANGE_APPLY_TEST,
                }
            );
        }

        /// <summary>
        /// 更新对应ID的检测结果
        /// </summary>
        /// <param name="id"></param>
        /// <param name="func"></param>
        private void UpdateTestResultForId(int id, Func<TestResult, TestResult> func)
        {
            if (id < 0)
            {
                return;
            }
            TestResult testResult = homeService.GetTestResult(id);
            if (testResult == null)
            {
                logService.Info($"检测结果为空 UpdateTestResultForId id={id}");
                return;
            }

            homeService.UpdateTestResult(func(testResult));
        }

        /// <summary>
        /// 更新这排样本管内的检测结果
        /// </summary>
        /// <param name="sampleCurrentPos"></param>
        /// <param name="func"></param>
        private void UpdateTestResultForSamplePos(
            int sampleCurrentPos,
            Func<TestResult, TestResult> func
        )
        {
            if (sampleCurrentPos < 0 || sampleCurrentPos >= TestResults.Count)
            {
                return;
            }
            TestResults[sampleCurrentPos] = func(TestResults[sampleCurrentPos]);

            homeService.UpdateTestResult(TestResults[sampleCurrentPos]);
        }

        /// <summary>
        /// 扫码失败，移动到下一个样本
        /// </summary>
        private void ScanFailed()
        {
            logService.Info($"收到 扫码失败 {SampleShelfPos} {SampleCurrentPos}");
            SampleShelfViewModel.UpdateSampleItems(
                SampleShelfPos,
                SampleCurrentPos,
                (item) =>
                {
                    item.State = SampleState.ScanFailed;
                    UpdateTestResultForId(
                        item.ResultId,
                        (tr) =>
                        {
                            tr.ResultState = ResultState.ScanFailed;
                            return tr;
                        }
                    );
                    RefreshChange(item.ResultId);
                    return item;
                }
            );

            // 通知状态机扫码失败
            detectionStateMachine.FireAsync(DetectionTrigger.ScanFailed);

        
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
            // 更新样本状态
            SampleShelfViewModel.UpdateSampleItems(
                SampleShelfPos,
                SampleCurrentPos,
                (item) =>
                {
                    item.State = SampleState.SamplingCompleted;
                    return item;
                }
            );
            var currentTestResult = detectionStateMachine.Context.CurrentAddingSampleTestResult;
            if (currentTestResult != null)
            {
                UpdateTestResultForId(
                    currentTestResult.Id,
                    (item) =>
                    {
                        item.ResultState = ResultState.SamplingSuccess;
                        return item;
                    }
                );
            }
        }

       
     
        public void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model)
        {
            if (!ContinueTest())
                return;
            logService.Info($"接收到 加样: {JsonConvert.SerializeObject(model)}");
            
            // 更新检测结果状态
            var currentTestResult = detectionStateMachine.Context.CurrentAddingSampleTestResult;
            if (currentTestResult != null)
            {
                UpdateTestResultForId(
                    currentTestResult.Id,
                    (t) =>
                    {
                        t.ResultState = ResultState.AddSampleSuccess;
                        return t;
                    }
                );
            }
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
            logService.Info($"接收到 推卡完成: {JsonConvert.SerializeObject(model)}");

            if (detectionStateMachine.Context.PushCardSuccess)
            {
                // 更新 UI 状态（VM 依然负责 UI 相关的 TestResult 更新）
                var project = detectionStateMachine.Context.CurrentAddingSampleTestResult?.Project;
                if (project != null)
                {
                    UpdateTestResultForSamplePos(
                        SampleCurrentPos,
                        (item) =>
                        {
                            item.ProjectId = project.Id;
                            item.Project = project;
                            item.CardQRCode = model.Data.QrCode;
                            return item;
                        }
                    );
                    // CurrentTestResult 已在 context 中更新，这里同步更新本地副本
                    if (detectionStateMachine.Context.CurrentAddingSampleTestResult != null)
                    {
                        detectionStateMachine.Context.CurrentAddingSampleTestResult.Project = project;
                        detectionStateMachine.Context.CurrentAddingSampleTestResult.ProjectId = project.Id;
                    }
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

     
        public void ReceiveMoveReactionAreaModel(BaseResponseModel<MoveReactionAreaModel> model)
        {
            if (!ContinueTest(isTestAction: true))
                return;
            var movingTestResult = detectionStateMachine.Context.MovingToReactionAreaTestResult;
            logService.Info(
                $"接收到 移动反应区: {movingTestResult.Id}{JsonConvert.SerializeObject(model)} ReactionAreaY={detectionStateMachine.Context.ReactionAreaY} ReactionAreaX={detectionStateMachine.Context.ReactionAreaX} id={movingTestResult?.Id}"
            );
            if (movingTestResult != null)
            {
                UpdateTestResultForId(
                    movingTestResult.Id,
                    (t) =>
                    {
                        t.ResultState = ResultState.Incubation;
                        return t;
                    }
                );
            }
            // 更新反应区状态
            ReactionAreaViewModel.UpdateItem(
                detectionStateMachine.Context.ReactionAreaY,
                detectionStateMachine.Context.ReactionAreaX,
                (item) =>
                {
                    item.State = ReactionAreaItem.STATE_WAIT;
                    item.TestResult = movingTestResult;
                    item.ReactionAreaY = detectionStateMachine.Context.ReactionAreaY;
                    item.ReactionAreaX = detectionStateMachine.Context.ReactionAreaX;
                    // 加入等待检测队列
                    Enqueue(item);
                    logService.Info($"入队= {item.TestResult.Id}{JsonConvert.SerializeObject(item)}");
                    return item;
                }
            );

            // 由状态机处理下一步
            _ = detectionStateMachine.FireAsync(DetectionTrigger.MoveToReactionAreaCompleted);
        }

        /// <summary>
        /// 加入等待检测队列
        /// </summary>
        /// <param name="item"></param>
        private void Enqueue(ReactionAreaItem item)
        {
            homeService.Enqueue(item);
        }

        /// <summary>
        /// 检测卡出队，已经到检测时间
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool OnReactionAreaDequeue(ReactionAreaItem item)
        {

            if (IsTesting)
            {
                //正在检测
                return false;
            }
            //如果检测模块运行错误，则不能检测
            if (!ContinueTest(true))
                return false;

            logService.Info(
                $"OnReactionAreaDequeue IsTesting={IsTesting} {!ContinueTest(true)} {JsonConvert.SerializeObject(item)}"
            );
            if (item.TestResult.Project == null)
            {
                logService.Info("项目为空");
                return false;
            }
            IsTesting = true;
            mailboxService.Post(new TestRequestEvent() { item = item });
            return true;
        }
        public void ReceiveTestModel(BaseResponseModel<TestModel> model)
        {
            //logService.Info($"接收到 检测2: {!ContinueTest(true)} {JsonConvert.SerializeObject(model)}");

            if (!ContinueTest(true))
                return;
            TestFinished = true;

            logService.Info($"接收到 检测: {JsonConvert.SerializeObject(model)}");
            // 处理检测数据
            
            int t = 0;
            int c = 0;
            int.TryParse(model.Data.T, out t);
            int.TryParse(model.Data.C, out c);

            int t2 = 0;
            int c2 = 0;
            int.TryParse(model.Data.T2, out t2);
            int.TryParse(model.Data.C2, out c2);
            TestResult temp = null;
            int[] points = model.Data.Point.ToArray();
            Platform.Model.Point point = new Platform.Model.Point()
            {
                Points = points,
                Location = model.Data.Location,
            };
            t = t / 1000;
            c = c / 1000;
            t2 = t2 / 1000;
            c2 = c2 / 1000;
            point.T = "" + t;
            point.C = "" + c;
            point.T2 = "" + t2;
            point.C2 = "" + c2;
            point.Tc = toolRepository.CalcTC(t, c);
            point.Tc2 = toolRepository.CalcTC(t2, c2);
            int pointId = homeService.InsertPoint(point);
            point.Id = pointId;
            //更新检测结果
            UpdateTestResultForId(
                detectionStateMachine.Context.TestResultId,
                (item) =>
                {
                    item.C = "" + c;
                    item.T = "" + t;
                    item.C2 = "" + c2;
                    item.T2 = "" + t2;
                    item.PointId = pointId;
                    item.Point = point;
                    item.ResultState = ResultState.TestFinish;
                    item = toolRepository.CalcTestResult(item);
                    return temp = item;
                }
            );
            //刷新结果
            RefreshChange(detectionStateMachine.Context.TestResultId);
            //单个样本检测完毕
            SingleSampleTestFinished(temp);
            //更新反应区状态
            ReactionAreaViewModel.UpdateItem(
                detectionStateMachine.Context.ReactionAreaTestY,
                detectionStateMachine.Context.ReactionAreaTestX,
                (item) =>
                {
                    item.State = ReactionAreaItem.STATE_END;
                    item.TestResult = temp;
                    return item;
                }
            );

            //检测完了
            if (homeService.ReactionAreaQueueIsEmpty())
            {
                if (
                    SystemGlobal.MachineStatus == MachineStatus.SamplingFinished
                    || SystemGlobal.MachineStatus == MachineStatus.Testing
                    || SystemGlobal.MachineStatus == MachineStatus.RunningError
                )
                {
                    //只有已经取样完成||运行错误，才代表真正检测结束了
                    SetMachineStatus(MachineStatus.TestingEnd);
                    logService.Info("没有待检测的检测卡，则检测完成");
                }
                else
                {
                    //可能正在取样
                    logService.Info("检测完成，但还有待取样的样本");
                }
            }
            // 检测完成后恢复取样的逻辑已迁移到状态机的 CheckAndResumeIfPausedAsync 中
            IsTesting = false;
        }

        /// <summary>
        /// 单个样本检测完毕
        /// 1、上传
        /// 2、打印
        ///
        /// </summary>
        /// <param name="temp"></param>
        private void SingleSampleTestFinished(TestResult temp)
        {
            AutoUpload(temp);
            AutoPrint(temp);
        }

        /// <summary>
        /// 自动打印检测结果
        /// </summary>
        /// <param name="temp"></param>
        private void AutoPrint(TestResult temp)
        {
            homeService.AutoPrintReport(
                temp,
                configRepository.IsAutoPrintA4Report(),
                false,
                configRepository.IsAutoPrintTicket(),
                configRepository.GetPrinterName()
            );
        }

        /// <summary>
        /// 自动上传检测结果
        /// </summary>
        /// <param name="temp"></param>
        private void AutoUpload(TestResult temp)
        {
            //已连接并且自动上传已开启
            if (homeService.Hl7NeedAutoUpload())
            {
                dispatcherService.InvokeAsync(async () =>
                {
                    logService.Info($"开始上传: {temp.Id}");
                    //上传检测结果
                    UploadResult ur = await homeService.UploadTestResultAsync(temp);
                    if (ur != null && ur.ResultType == UploadResultType.Success)
                    {
                        logService.Info($"上传检测结果成功: {ur?.TestResultId}");
                        dispatcherService.Invoke(() =>
                        {
                            //更新检测结果状态
                            UpdateTestResultForId(
                                ur.TestResultId,
                                (item) =>
                                {
                                    item.IsUploaded = true;
                                    return item;
                                }
                            );
                        });

                        RefreshChange(ur.TestResultId);
                    }
                    else
                    {
                        logService.Info(
                            $"上传检测结果失败: {ur?.TestResultId} {JsonConvert.SerializeObject(temp)}"
                        );
                    }
                });
            }
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
