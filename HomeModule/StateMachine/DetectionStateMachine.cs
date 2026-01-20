using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stateless;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Model.Events;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.StateMachine;
using FluorescenceFullAutomatic.HomeModule.Services;
using FluorescenceFullAutomatic.UploadModule.Upload;
using FluorescenceFullAutomatic.ViewModels;

namespace FluorescenceFullAutomatic.HomeModule.StateMachine
{
    /// <summary>
    /// 检测流程状态机 - 负责检测流程控制和硬件指令下发
    /// </summary>
    public class DetectionStateMachine
    {
        private readonly StateMachine<DetectionState, DetectionTrigger> _machine;
        private readonly StateContext _context;

        // 依赖服务
        private readonly ILogService _logService;
        private readonly IEventMailboxService _mailboxService;
        private readonly ISerialPortCommandFacade _commandFacade;
        private readonly ISerialPortService _serialPortService;
        private readonly IHomeService _homeService;
        private readonly IConfigService _configService;
        private readonly IProjectService _projectService;

        /// <summary>
        /// 当前状态
        /// </summary>
        public DetectionState CurrentState => _machine.State;

        /// <summary>
        /// 状态上下文（只读访问）
        /// </summary>
        public StateContext Context => _context;

        public DetectionStateMachine(
            ILogService logService,
            IEventMailboxService mailboxService,
            ISerialPortCommandFacade commandFacade,
            ISerialPortService serialPortService,
            IHomeService homeService,
            IConfigService configService,
            IProjectService projectService)
        {
            _logService = logService;
            _mailboxService = mailboxService;
            _commandFacade = commandFacade;
            _serialPortService = serialPortService;
            _homeService = homeService;
            _configService = configService;
            _projectService = projectService;

            _context = new StateContext(_configService);

            // 订阅扫码回调
            _serialPortService.AddScanSuccessListener(async barcode => await HandleBarcodeReceived(barcode, true));
            _serialPortService.AddScanFailedListener(async error => await HandleBarcodeReceived(error, false));

            // 创建状态机
            _machine = new StateMachine<DetectionState, DetectionTrigger>(
                () => _context.CurrentState,
                s => _context.CurrentState = s
            );

            // 配置状态机
            ConfigureStateMachine();
        }

        /// <summary>
        /// 配置状态机（定义所有状态和迁移规则）
        /// </summary>
        private void ConfigureStateMachine()
        {
            // ====== Idle 空闲状态 ======
            _machine.Configure(DetectionState.Idle)
                .Permit(DetectionTrigger.RequestSelfInspection, DetectionState.SelfInspecting);

            // ====== SelfInspecting 自检状态 ======
            _machine.Configure(DetectionState.SelfInspecting)
                .OnEntryAsync(OnEnterSelfInspectingAsync)
                .Permit(DetectionTrigger.SelfInspectionCompleted, DetectionState.SelfInspectingAfterGetMachineStatus)
                .Permit(DetectionTrigger.SelfInspectionFailed, DetectionState.Idle);
            // ====== SelfInspectingAfterGetMachineStatus 状态后获取仪器状态 ======
            _machine.Configure(DetectionState.SelfInspectingAfterGetMachineStatus)
                .OnEntryAsync(OnEnterSelfInspectingAfterGetMachineStatusAsync)
                .Permit(DetectionTrigger.MachineStatusReceived, DetectionState.Ready);

            // ====== Ready 就绪状态 ======
            _machine.Configure(DetectionState.Ready)
                .PermitDynamic(DetectionTrigger.StartDetection, () =>
                {
                    if (ValidateStartDetection(out var errorKey))
                    {
                        return DetectionState.PreparingDetection;
                    }

                    // 校验失败，停留在当前状态，并通知 UI 错误原因
                    _mailboxService.Post(new DetectionValidationErrorEvent { ErrorKey = errorKey });
                    return DetectionState.Ready;
                })
                .Permit(DetectionTrigger.RequestSelfInspection, DetectionState.SelfInspecting);

            // ====== PreparingDetection 准备检测 ======
            _machine.Configure(DetectionState.PreparingDetection)
                .OnEntryAsync(OnEnterPreparingDetectionAsync)
                .Permit(DetectionTrigger.MachineStatusReceived, DetectionState.WaitingForFirstClean)
                .Permit(DetectionTrigger.CancelDetection, DetectionState.Completed);

            // ====== WaitingForFirstClean 等待首次清洗 ======
            _machine.Configure(DetectionState.WaitingForFirstClean)
                .OnEntryAsync(OnEnterWaitingForFirstCleanAsync)
                .Permit(DetectionTrigger.FirstCleanCompleted, DetectionState.MovingToShelf);

            // ====== MovingToShelf 移动到样本架 ======
            _machine.Configure(DetectionState.MovingToShelf)
                .OnEntryAsync(OnEnterMovingToShelfAsync)
                .Permit(DetectionTrigger.ShelfMoveCompleted, DetectionState.PositioningAtSample);

            // ====== PositioningAtSample 定位样本位 ======
            _machine.Configure(DetectionState.PositioningAtSample)
                .OnEntryAsync(OnEnterPositioningAtSampleAsync)
                .Permit(DetectionTrigger.SampleFound, DetectionState.Scanning)          // 发现样本管且需扫码
                .Permit(DetectionTrigger.DirectSampling, DetectionState.SamplingAndPushingCard) // 发现样本杯或无需扫码
                .PermitReentry(DetectionTrigger.MoveToNextSample)                       // 1.2 同一排下一个样本位
                .Permit(DetectionTrigger.MoveToNextShelf, DetectionState.MovingToShelf) // 1.3 移动到下一排
                .Permit(DetectionTrigger.NoMoreSamples, DetectionState.Finishing)       // 1.1 最后一排最后一个
                .Permit(DetectionTrigger.ErrorOccurred, DetectionState.Error);

            // ====== Scanning 扫码状态 ======
            _machine.Configure(DetectionState.Scanning)
                .OnEntryAsync(OnEnterScanningAsync)
                .Permit(DetectionTrigger.ScanSuccess, DetectionState.SamplingAndPushingCard)
                // 扫码失败后的三分支跳转（由 HandleBarcodeReceived -> MoveToNextOrFinishAsync 触发）
                .Permit(DetectionTrigger.MoveToNextSample, DetectionState.PositioningAtSample)  // 1.2 同排下一个
                .Permit(DetectionTrigger.MoveToNextShelf, DetectionState.MovingToShelf)         // 1.3 下一排
                .Permit(DetectionTrigger.NoMoreSamples, DetectionState.Finishing);              // 1.1 结束

            // ====== SamplingAndPushingCard 取样+推卡并行 ======
            // 推卡前需要先获取仪器状态，检测到有检测卡才去，否则VM提示添加卡
            // 取样前需要等待清洗完成，如果清洗未完成则等待，清洗完成后通过 CleaningCompletedForSampling 触发继续
            _machine.Configure(DetectionState.SamplingAndPushingCard)
                .OnEntryAsync(OnEnterSamplingAndPushingCardAsync)
                .Permit(DetectionTrigger.AllConditionsMet, DetectionState.AddingSample)
                .Permit(DetectionTrigger.PushCardFailed, DetectionState.Finishing)
                .Permit(DetectionTrigger.NoCardAvailable, DetectionState.WaitingForCard)
                .PermitReentry(DetectionTrigger.CleaningCompletedForSampling); // 清洗完成后重新进入执行取样

            // ====== WaitingForCard 等待添加检测卡 ======
            _machine.Configure(DetectionState.WaitingForCard)
                .OnEntry(() => _logService.Info("[状态机] 检测卡不足，等待用户添加"))
                .Permit(DetectionTrigger.CardAvailable, DetectionState.SamplingAndPushingCard); // 添加卡后重试

            // ====== AddingSample 加样状态 ======
            _machine.Configure(DetectionState.AddingSample)
                .OnEntryAsync(OnEnterAddingSampleAsync)
                .Permit(DetectionTrigger.AddingSampleCompleted, DetectionState.MovingToReactionArea);

            // ====== MovingToReactionArea 移动到反应区 ======
            // 移动反应区的同时并行启动下一个样本的检测流程
            _machine.Configure(DetectionState.MovingToReactionArea)
                .OnEntryAsync(OnEnterMovingToReactionAreaAsync)
                .Permit(DetectionTrigger.ReactionAreaFull, DetectionState.PausedDueToFullQueue)
                .Permit(DetectionTrigger.MoveToNextSample, DetectionState.PositioningAtSample)  // 1.2 同排下一个
                .Permit(DetectionTrigger.MoveToNextShelf, DetectionState.MovingToShelf)         // 1.3 下一排
                .Permit(DetectionTrigger.NoMoreSamples, DetectionState.Finishing);              // 1.1 结束

            // ====== PausedDueToFullQueue 暂停（反应区满） ======
            _machine.Configure(DetectionState.PausedDueToFullQueue)
                .OnEntry(() => _logService.Info("[状态机] 反应区满，暂停取样"))
                .Permit(DetectionTrigger.ReactionAreaSpaceAvailable, DetectionState.PositioningAtSample);

            // ====== Finishing 取样结束收尾 ======
            _machine.Configure(DetectionState.Finishing)
                .OnEntryAsync(OnEnterFinishingAsync)
                .Permit(DetectionTrigger.ReactionAreaSpaceAvailable, DetectionState.WaitingForTests);

            // ====== WaitingForTests 等待反应区检测完成 ======
            _machine.Configure(DetectionState.WaitingForTests)
                .OnEntry(() => _logService.Info("[状态机] 等待反应区检测完成"))
                .Permit(DetectionTrigger.StartDetection, DetectionState.PreparingDetection)
                .Permit(DetectionTrigger.TestQueueEmpty, DetectionState.Completed);

            // ====== Completed 完成状态 ======
            _machine.Configure(DetectionState.Completed)
                .OnEntry(() =>
                {
                    _logService.Info("[状态机] 全部检测完成");
                    SystemGlobal.MachineStatus = MachineStatus.TestingEnd;
                })
                .PermitDynamic(DetectionTrigger.StartDetection, () =>
                {
                    if (ValidateStartDetection(out var errorKey))
                    {
                        return DetectionState.PreparingDetection;
                    }

                    // 校验失败，通知 UI
                    _mailboxService.Post(new DetectionValidationErrorEvent { ErrorKey = errorKey });
                    return DetectionState.Completed;
                })
                .Permit(DetectionTrigger.RequestSelfInspection, DetectionState.SelfInspecting);

            // ====== Error 错误状态 ======
            _machine.Configure(DetectionState.Error)
                .OnEntry(() =>
                {
                    _logService.Error("[状态机] 进入错误状态");
                    SystemGlobal.MachineStatus = MachineStatus.RunningError;
                })
                .Permit(DetectionTrigger.Retry, DetectionState.PreparingDetection)
                .Permit(DetectionTrigger.ForceComplete, DetectionState.Completed);

            // ====== 全局状态迁移回调 ======
            _machine.OnTransitioned(OnTransitioned);
        }

        private async Task OnEnterSelfInspectingAfterGetMachineStatusAsync()
        {
            _logService.Info("[状态机] 进入 SelfInspectingAfterGetMachineStatus 状态，自检后获取仪器状态");

            // 执行自检指令
              await SafeSerialPortCallAsync(
                () => _commandFacade.GetMachineStateAsync(),
                async (ret) =>
                {
                    _mailboxService.Post(new MachineStatusReceivedEvent() { Result = ret , Reason = MachineStatusRequestReason.AfterSelfInspection}); // 通知 UI
                    await FireAsync(DetectionTrigger.MachineStatusReceived);
                });
        }

        // ========================================
        // 逻辑处理（Handle Methods）
        // ========================================

        public async Task HandleSelfInspectionReceived(BaseResponseModel<List<string>> response)
        {
            bool success = response?.Data == null || response.Data.Count == 0;
            _context.IsSelfInspectionFailed = !success;

            _logService.Info($"[状态机] 收到自检反馈，成功: {success}");
            await FireAsync(success ? DetectionTrigger.SelfInspectionCompleted : DetectionTrigger.SelfInspectionFailed);
        }

        /// <summary>
        /// 处理清洗反馈
        /// - WaitingForFirstClean 状态：触发 FirstCleanCompleted
        /// - 其他状态：检查是否有等待取样的任务，有则触发 CleaningCompletedForSampling
        /// </summary>
        public async Task HandleCleanoutSamplingProbeReceived(BaseResponseModel<CleanoutSamplingProbeModel> response)
        {
            _logService.Info("[状态机] 收到清洗反馈");
            _context.CleaningCompleted = true;

            if (CurrentState == DetectionState.WaitingForFirstClean)
            {
                // 首次清洗完成
                await FireAsync(DetectionTrigger.FirstCleanCompleted);
            }
            else if (_context.IsWaitingForCleaningToSample)
            {
                // 有等待取样的任务，触发继续取样
                _logService.Info("[状态机] 清洗完成，继续执行取样");
                _context.IsWaitingForCleaningToSample = false;
                await FireAsync(DetectionTrigger.CleaningCompletedForSampling);
            }
        }

        public async Task HandleMoveSampleShelfReceived(BaseResponseModel<MoveSampleShelfModel> response)
        {
            _logService.Info("[状态机] 收到移动样本架反馈");
            await FireAsync(DetectionTrigger.ShelfMoveCompleted);
        }

        /// <summary>
        /// 处理移动样本响应
        /// 
        /// 业务流程需求：
        /// 1、样本不存在
        ///    1.1、判断是否是最后一排最后一个，如果是，需要结束取样。
        ///    1.2、判断不是最后一个，继续移动。
        ///    1.3、判断不是最后一排，移动到下一排
        /// 2、样本存在 
        ///    2.1、样本管，判断是否需要扫码
        ///       2.1.1、需要扫码，去扫码
        ///       2.1.2、不需要扫码，去取样、推卡(但推卡前需要获取仪器状态，检测到有检测卡才去，否则VM提示添加卡)，实时获取信息，发送给VM执行RealTimeGetApplyTest
        ///          2.1.1.1、扫码成功，去取样、推卡(但推卡前需要获取仪器状态，检测到有检测卡才去，否则VM提示添加卡)
        ///          2.1.1.2、扫码失败，走1.1 1.2 1.3的判断
        ///    2.2、样本存在 样本杯
        ///       2.2.1、走2.1.2的判断
        /// </summary>
        public async Task HandleMoveSampleReceived(BaseResponseModel<MoveSampleModel> response)
        {
            if (response?.Data == null) return;

            bool found = response.Data.SampleType != MoveSampleModel.None;
            _logService.Info($"[状态机] 收到移动样本反馈，存在样本: {found}");

            if (!found)
            {
                // 1、样本不存在 -> 调用 MoveToNextOrFinishAsync 处理 1.1/1.2/1.3 三分支逻辑
                await MoveToNextOrFinishAsync();
            }
            else
            {
                // 2、样本存在 
                _context.CurrentSampleType = response.Data.SampleType switch
                {
                    MoveSampleModel.SampleTube => SampleType.SampleTube,
                    MoveSampleModel.SampleCup => SampleType.SampleCup,
                    _ => SampleType.None
                };

                if (_context.CurrentSampleType == SampleType.SampleTube)
                {
                    // 2.1、样本管，判断是否需要扫码
                    if (_context.RequireBarcode)
                    {
                        // 2.1.1、需要扫码，去扫码
                        _logService.Info("[状态机] 发现样本管，准备扫码");
                        await FireAsync(DetectionTrigger.SampleFound);
                    }
                    else
                    {
                        // 2.1.2、不需要扫码，去取样、推卡，实时获取信息
                        _logService.Info("[状态机] 发现样本管，配置为跳过扫码，直接加样");
                        await IdentifyAndFetchApplyTestAsync(null);
                        await FireAsync(DetectionTrigger.DirectSampling);
                    }
                }
                else
                {
                    // 2.2、样本存在 样本杯
                    // 2.2.1、走2.1.2的判断，直接去取样、推卡
                    _logService.Info("[状态机] 发现样本杯，准备加样");
                    await IdentifyAndFetchApplyTestAsync(null);
                    await FireAsync(DetectionTrigger.DirectSampling);
                }
            }
        }

        /// <summary>
        /// 处理扫码结果（逻辑下沉）
        /// 
        /// 2.1.1.1、扫码成功，去取样、推卡
        /// 2.1.1.2、扫码失败，走1.1 1.2 1.3的判断
        /// </summary>
        public async Task HandleBarcodeReceived(string barcode, bool success)
        {
            _logService.Info($"[状态机] 收到扫码结果: {barcode}, 成功: {success}");
            
            if (success)
            {
                // 2.1.1.1、扫码成功，去取样、推卡
                _context.CurrentBarcode = barcode;
                await IdentifyAndFetchApplyTestAsync(barcode);
                await FireAsync(DetectionTrigger.ScanSuccess);
            }
            else
            {
                // 2.1.1.2、扫码失败，走1.1 1.2 1.3的判断
                _logService.Warning($"[状态机] 样本位 {_context.CurrentSamplePos} 扫码失败");
                await MoveToNextOrFinishAsync();
            }
        }

        /// <summary>
        /// 识别并获取申请信息（逻辑下沉自 VM）
        /// </summary>
        private async Task IdentifyAndFetchApplyTestAsync(string barcode)
        {
            try
            {
                _context.CurrentBarcode = barcode;
                bool isNeedLisGet = _homeService.isNeedLisGet();
                bool isMatchingBarcode = _homeService.isMatchingBarcode();

                _logService.Info($"[状态机] 正在查询申请信息: Barcode={barcode}, Position={_context.CurrentSamplePos}");

                var qr = await _homeService.QueryApplyTestAsync(
                    isNeedLisGet,
                    isMatchingBarcode,
                    barcode ?? "",
                    "" // TestNum 暂时为空
                );

                if (qr?.ResultType == Hl7Result.QueryResultType.Success && qr.ApplyTests?.Count > 0)
                {
                    var applyTest = qr.ApplyTests[0];
                    _logService.Info($"[状态机] 获取到申请信息: {applyTest.Patient?.PatientName}");

                    // 执行数据库持久化逻辑（原 VM 中的逻辑）
                    applyTest.Patient.InspectDate = DateTime.Now;
                    int patientId = _homeService.InsertPatient(applyTest.Patient);
                    applyTest.Patient.Id = patientId;
                    applyTest.PatientId = patientId;
                    applyTest.ApplyTestType = ApplyTestType.TestEnd;
                    _homeService.InsertApplyTest(applyTest);
                    _homeService.UpdateApplyTestCompleted(applyTest);

                    // 更新上下文中的结果对象
                    if (_context.CurrentAddingSampleTestResult != null)
                    {
                        _context.CurrentAddingSampleTestResult.Patient = applyTest.Patient;
                        _context.CurrentAddingSampleTestResult.PatientId = patientId;
                        _context.CurrentAddingSampleTestResult.Barcode = barcode;
                    }

                    // 通知 UI 数据已更新
                    _mailboxService.Post(new ApplyTestIdentifiedEvent { ApplyTest = applyTest });
                }
                else
                {
                    _logService.Warning("[状态机] 未查询到有效的申请信息");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"[状态机] 获取申请信息异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 统一处理流程推进或结束逻辑（1.1 - 1.3）
        /// 1.1、判断是否是最后一排最后一个，如果是，需要结束取样。
        /// 1.2、判断不是最后一个，继续移动。
        /// 1.3、判断不是最后一排，移动到下一排
        /// </summary>
        private async Task MoveToNextOrFinishAsync()
        {
            if (!_context.IsLastPositionInCurrentRow)
            {
                // 1.2 不是当前排的最后一个，移动到同一排的下一个样本位
                _logService.Info($"[状态机] 1.2 当前位无样本，移动到同排下一个位置 (Pos: {_context.CurrentSamplePos} -> {_context.CurrentSamplePos + 1})");
                _context.MoveToNextSampleInRow();
                await FireAsync(DetectionTrigger.MoveToNextSample);
            }
            else if (_context.HasNextRow)
            {
                // 1.3 是当前排的最后一个，但还有下一排，移动到下一排
                _logService.Info($"[状态机] 1.3 当前排已遍历完，移动到下一排样本架 (Shelf: {_context.CurrentShelfPos} -> next)");
                _context.MoveToNextRow();
                await FireAsync(DetectionTrigger.MoveToNextShelf);
            }
            else
            {
                // 1.1 最后一排最后一个，结束取样
                _logService.Info("[状态机] 1.1 已到达最后一排最后一个位置，准备结束取样流程");
                await FireAsync(DetectionTrigger.NoMoreSamples);
            }
        }

        /// <summary>
        /// 处理加样完成反馈
        /// 加样完成后启动清洗取样针（为下一次取样做准备）
        /// </summary>
        public async Task HandleAddingSampleReceived(BaseResponseModel<AddingSampleModel> response)
        {
            _logService.Info("[状态机] 收到加样反馈");

            // 加样完成后启动清洗取样针（为下一次取样做准备，不阻塞后续流程）
            _context.CleaningCompleted = false;
            _ = SafeSerialPortCallAsync(
                () => _commandFacade.CleanoutSamplingProbeAsync(_context.GetCleanoutDuration()),
                (cleanRet) =>
                {
                    _logService.Info("[状态机] 清洗完成（为下一次取样做准备）");
                    _context.CleaningCompleted = true;
                    _mailboxService.Post(new CleanoutSamplingProbeCompletedEvent() { Result = cleanRet });
                });

            await FireAsync(DetectionTrigger.AddingSampleCompleted);
        }

        private async Task OnEnterScanningAsync()
        {
            _logService.Info("[状态机] 进入 Scanning 状态，开始扫码");
            _serialPortService.ScanBarcode();
        }


        private async Task OnEnterSelfInspectingAsync()
        {
            _logService.Info("[状态机] 进入 SelfInspecting 状态，开始自检");

            // 通知 UI 显示自检对话框
            _mailboxService.Post(new SelfInspectionStartedEvent());

            // 执行自检指令
            await SafeSerialPortCallAsync(
                () => _commandFacade.GetSelfInspectionStateAsync(_context.GetRetainReactionArea()),
                async (ret) =>
                {
                    _mailboxService.Post(new SelfInspectionCompletedEvent() { Result = ret }); // 通知 UI
                    await HandleSelfInspectionReceived(ret); // 直接处理逻辑
                });
        }

        private async Task OnEnterPreparingDetectionAsync()
        {
            _logService.Info("[状态机] 进入 PreparingDetection 状态，获取仪器状态");
            _context.Reset();
            SystemGlobal.TestType = TestType.Test;

            await SafeSerialPortCallAsync(
                () => _commandFacade.GetMachineStateAsync(),
                async (ret) =>
                {
                    _mailboxService.Post(new MachineStatusReceivedEvent() { Result = ret , Reason = MachineStatusRequestReason.BeforeTest}); // 通知 UI
                    await HandleMachineStatusReceived(ret); // 直接处理逻辑
                });
        }

        private async Task OnEnterWaitingForFirstCleanAsync()
        {
            _logService.Info("[状态机] 进入 WaitingForFirstClean 状态，首次清洗取样针");
            _context.InitSampleShelfState();
            _context.CleaningCompleted = false;
            await SafeSerialPortCallAsync(
                () => _commandFacade.CleanoutSamplingProbeAsync(_context.GetCleanoutDuration()),
                async (ret) =>
                {
                    _context.CleaningCompleted = true;
                    _mailboxService.Post(new CleanoutSamplingProbeCompletedEvent() { Result = ret }); // 通知 UI
                    await HandleCleanoutSamplingProbeReceived(ret); // 直接处理逻辑
                });
        }

        private async Task OnEnterMovingToShelfAsync()
        {
            _logService.Info($"[状态机] 进入 MovingToShelf 状态，移动到样本架 {_context.CurrentShelfPos + 1}");

            await SafeSerialPortCallAsync(
                () => _commandFacade.MoveSampleShelfAsync(_context.CurrentShelfPos + 1),
                async (ret) =>
                {
                    // 移动样本架完成后，重置当前样本位为 0
                    _context.CurrentSamplePos = 0;
                    
                    // 发送位置同步事件，让 VM 同步样本架位置
                    _mailboxService.Post(new SamplePositionChangedEvent
                    {
                        ShelfPos = _context.CurrentShelfPos,
                        SamplePos = _context.CurrentSamplePos,
                        SampleType = SampleType.None,
                        TestResultId = -1
                    });
                    
                    _mailboxService.Post(new MoveSampleShelfCompletedEvent() { Result = ret }); // 通知 UI
                    await HandleMoveSampleShelfReceived(ret); // 直接处理逻辑
                });
        }

        private async Task OnEnterPositioningAtSampleAsync()
        {
            _logService.Info($"[状态机] 进入 PositioningAtSample 状态，定位样本位 {_context.CurrentSamplePos}");

            await SafeSerialPortCallAsync(
                () => _commandFacade.MoveSampleAsync(_context.CurrentSamplePos + 1),
                async (ret) =>
                {
                    // 先处理逻辑（更新 context 中的样本类型等信息）
                    await HandleMoveSampleReceived(ret);
                    
                    // 发送位置同步事件，让 VM 同步位置信息
                    _mailboxService.Post(new SamplePositionChangedEvent
                    {
                        ShelfPos = _context.CurrentShelfPos,
                        SamplePos = _context.CurrentSamplePos,
                        SampleType = _context.CurrentSampleType,
                        TestResultId = _context.CurrentAddingSampleTestResult?.Id ?? -1
                    });
                    
                    // 最后通知 UI 移动完成
                    _mailboxService.Post(new MoveSampleCompletedEvent() { Result = ret });
                });
        }

        /// <summary>
        /// 进入取样+推卡状态
        /// 推卡前需要先获取仪器状态，检测到有检测卡才去，否则VM提示添加卡
        /// </summary>
        private async Task OnEnterSamplingAndPushingCardAsync()
        {
            _logService.Info("[状态机] 进入 SamplingAndPushingCard 状态");
            _context.ResetConditionFlags();

            // 先获取仪器状态，检查检测卡数量
            await SafeSerialPortCallAsync(
                () => _commandFacade.GetMachineStateAsync(),
                async (ret) =>
                {
                    if (ret?.Data == null)
                    {
                        _logService.Error("[状态机] 获取仪器状态失败");
                        return;
                    }

                    // 更新上下文中的卡数量
                    _context.CardNum = int.TryParse(ret.Data.CardNum, out var cn) ? cn : 0;
                    _logService.Info($"[状态机] 当前检测卡数量: {_context.CardNum}");
                    _mailboxService.Post(new MachineStatusReceivedEvent() { Result = ret , Reason = MachineStatusRequestReason.BeforePushCard}); // 通知 UI

                    // 通知 VM 更新检测卡数量
                    _mailboxService.Post(new CardNumberUpdatedEvent { CurrentCardNum = _context.CardNum });
                    if (_context.CardNum <= 0)
                    {
                        // 无卡，通知 VM 提示用户添加卡
                        _logService.Warning("[状态机] 检测卡不足，通知 VM 提示用户");
                        _mailboxService.Post(new NoCardAvailableEvent { CurrentCardNum = _context.CardNum });
                        await FireAsync(DetectionTrigger.NoCardAvailable);
                    }
                    else
                    {
                        // 有卡，并行执行取样+推卡
                        _logService.Info("[状态机] 检测卡充足，开始并行取样+推卡");
                        await ExecuteSamplingAndPushingCardAsync();
                    }
                });
        }

        /// <summary>
        /// 执行取样+推卡并行操作
        /// 
        /// 取样前置条件：
        /// - 上一次清洗完成 (CleaningCompleted) - 因为取样针可能还在清洗中
        /// - 如果清洗未完成则设置标志位等待，清洗完成后通过 CleaningCompletedForSampling 触发继续
        /// 
        /// 加样前置条件：
        /// 1. 取样完成 (SamplingCompleted)
        /// 2. 推卡完成且成功 (PushCardCompleted && PushCardSuccess)
        /// 
        /// 流程：等待清洗完成 -> 并行执行取样+推卡 -> 加样完成后清洗取样针
        /// 清洗与后续操作（移动反应区等）并行进行
        /// </summary>
        private async Task ExecuteSamplingAndPushingCardAsync()
        {
            // 取样前需要等待上一次清洗完成
            if (!_context.CleaningCompleted)
            {
                _logService.Info("[状态机] 清洗未完成，设置等待标志，等待清洗完成后继续");
                _context.IsWaitingForCleaningToSample = true;
                return; // 等待清洗完成后通过 CleaningCompletedForSampling 触发重新进入
            }

            // 取样任务
            var samplingTask = SafeSerialPortCallAsync(
                () => _commandFacade.SamplingAsync(_context.GetSampleTypeString(), _context.GetSampleVolume()),
                async (ret) =>
                {
                    _logService.Info("[状态机] 收到取样反馈");
                    _context.SamplingCompleted = true;
                    _mailboxService.Post(new SamplingCompletedEvent() { Result = ret });
                    await TryFireAllConditionsMetAsync();
                });

            // 推卡任务
            var pushCardTask = SafeSerialPortCallAsync(
                () => _commandFacade.PushCardAsync(),
                async (ret) =>
                {
                    await HandlePushCardReceivedInternal(ret);
                    _mailboxService.Post(new PushCardCompletedEvent() { Result = ret });
                    await TryFireAllConditionsMetAsync();
                });

            await Task.WhenAll(samplingTask, pushCardTask);
        }

        /// <summary>
        /// 统一检查加样前置条件是否满足
        /// 加样条件：取样完成 + 推卡完成且成功（不需要等待清洗）
        /// </summary>
        private async Task TryFireAllConditionsMetAsync()
        {
            if (_context.AllConditionsMetForAddingSample)
            {
                _logService.Info("[状态机] 所有条件已满足，准备加样");
                await FireAsync(DetectionTrigger.AllConditionsMet);
            }
            else
            {
                _logService.Info($"[状态机] 等待条件: 取样={_context.SamplingCompleted}, 推卡={_context.PushCardCompleted}, 推卡成功={_context.PushCardSuccess}");
            }
        }

        /// <summary>
        /// 内部处理推卡反馈（仅设置标志位）
        /// </summary>
        private async Task HandlePushCardReceivedInternal(BaseResponseModel<PushCardModel> response)
        {
            if (response?.Data == null) return;

            _logService.Info($"[状态机] 收到推卡反馈，二维码: {response.Data.QrCode}");

            if (IsPushCardSuccess(response.Data))
            {
                var project = _projectService.GetProjectForQrcode(response.Data.QrCode);
                if (project == null)
                {
                    _logService.Error($"[状态机] 推卡失败：无法识别二维码 {response.Data.QrCode}");
                    _context.PushCardSuccess = false;
                    _context.PushCardCompleted = true;
                    await FireAsync(DetectionTrigger.PushCardFailed);
                    return;
                }

                _logService.Info($"[状态机] 推卡成功，识别项目: {project.ProjectName}");
                _context.PushCardSuccess = true;
                _context.PushCardCompleted = true;

                if (_context.CurrentAddingSampleTestResult != null)
                {
                    _context.CurrentAddingSampleTestResult.Project = project;
                    _context.CurrentAddingSampleTestResult.ProjectId = project.Id;
                }
            }
            else
            {
                _logService.Warning("[状态机] 硬件反馈推卡失败");
                _context.PushCardSuccess = false;
                _context.PushCardCompleted = true;
                await FireAsync(DetectionTrigger.PushCardFailed);
            }
        }

        private async Task OnEnterAddingSampleAsync()
        {
            _logService.Info("[状态机] 进入 AddingSample 状态，开始加样");

            await SafeSerialPortCallAsync(
                () => _commandFacade.AddingSampleAsync(_context.GetSampleVolume(), "1"),
                async (ret) =>
                {
                    _mailboxService.Post(new AddingSampleCompletedEvent() { Result = ret }); // 通知 UI
                    await HandleAddingSampleReceived(ret); // 直接处理逻辑
                });
        }

        /// <summary>
        /// 进入移动反应区状态
        /// 
        /// 流程：
        /// 1. 判断反应区是否满，满则暂停
        /// 2. 保存当前 TestResult 到 MovingToReactionAreaTestResult（因为后续会并行处理下一个样本）
        /// 3. 异步启动移动反应区操作（不阻塞）
        /// 4. 同时判断是否有更多样本，有则并行启动下一个样本的检测流程
        /// 移动反应区和下一个样本检测并行进行
        /// </summary>
        private async Task OnEnterMovingToReactionAreaAsync()
        {
            _logService.Info($"[状态机] 进入 MovingToReactionArea 状态");

            // 判断反应区是否满
            if (_homeService.ReactionAreaQueueIsFull())
            {
                _logService.Warning("[状态机] 反应区已满，进入暂停状态");
                await _machine.FireAsync(DetectionTrigger.ReactionAreaFull);
                return;
            }

            // 保存当前 TestResult 到 MovingToReactionAreaTestResult
            // 因为后续会并行处理下一个样本，CurrentTestResult 会被覆盖
            _context.MovingToReactionAreaTestResult = _context.CurrentAddingSampleTestResult;

            // 获取反应区下一个位置
            int y = 0,x = 0;
            ReactionAreaViewModel.Instance.GetReactionAreaNext(out y, out x);
            _context.ReactionAreaY = y;
            _context.ReactionAreaX = x;
            
            _logService.Info($"[状态机] 移动到反应区位置 ({_context.ReactionAreaX}, {_context.ReactionAreaY}), TestResultId={_context.MovingToReactionAreaTestResult?.Id}");

            // 异步启动移动反应区操作（不等待完成，与下一个样本检测并行）
            _ = SafeSerialPortCallAsync(
                () => _commandFacade.MoveReactionAreaAsync(_context.ReactionAreaX, _context.ReactionAreaY),
                (ret) => _mailboxService.Post(new MoveReactionAreaCompletedEvent
                {
                    Result = ret,
                    TestResultId = _context.MovingToReactionAreaTestResult?.Id ?? -1,
                    ReactionAreaX = _context.ReactionAreaX,
                    ReactionAreaY = _context.ReactionAreaY
                }));

            // 同时判断是否有更多样本，启动下一个样本的检测流程
            await MoveToNextOrFinishAsync();
        }

        private async Task OnEnterFinishingAsync()
        {
            _logService.Info("[状态机] 进入 Finishing 状态，取样结束收尾");
            SystemGlobal.MachineStatus = MachineStatus.SamplingFinished;
            // 如果已经在清洗了，就不清洗
            var cleanTask = Task.CompletedTask;
            if(_context.CleaningCompleted){
                // 清洗取样针
                   cleanTask = SafeSerialPortCallAsync(
                    () => {
                        _context.CleaningCompleted = false;
                        return _commandFacade.CleanoutSamplingProbeAsync(_context.GetCleanoutDuration());
                    },
                    (ret) => {
                        _context.CleaningCompleted = true;
                        _mailboxService.Post(new CleanoutSamplingProbeCompletedEvent() { Result = ret });
                    });
            }
            // 样本架复位
            var resetTask = SafeSerialPortCallAsync(
                () => _commandFacade.MoveSampleShelfAsync(0), // 0 表示复位
                (ret) => _mailboxService.Post(new MoveSampleShelfCompletedEvent() { Result = ret }));
            
            await Task.WhenAll(cleanTask, resetTask);

            // 两个动作都结束后，通知 VM 取样结束
            _logService.Info("[状态机] 清洗和复位都完成，通知 VM 取样结束");
            _mailboxService.Post(new SamplingFinishedEvent { HintMessage = "取样结束" });
            if(_homeService.ReactionAreaQueueIsFull()){
                await FireAsync(DetectionTrigger.ReactionAreaSpaceAvailable);
            }
        }

        // ========================================
        // 状态迁移回调
        // ========================================

        private void OnTransitioned(StateMachine<DetectionState, DetectionTrigger>.Transition transition)
        {
            _logService.Info($"[状态机] 状态迁移: {transition.Source} --[{transition.Trigger}]--> {transition.Destination}");

            // 更新全局 MachineStatus
            SystemGlobal.MachineStatus = MapToMachineStatus(transition.Destination);

            // 发送消息通知其他 UI 变更（如主窗口图标、状态显示等）
            WeakReferenceMessenger.Default.Send(
                new MainStatusChangeMsg() { What = MainStatusChangeMsg.What_ChangeState }
            );

            // 发出状态变更事件（用于某些需要订阅具体状态迁移的组件）
            //_mailboxService.Post(new StateTransitionEvent
            //{
            //    FromState = transition.Source,
            //    ToState = transition.Destination,
            //    Trigger = transition.Trigger,
            //    Reason = $"Trigger: {transition.Trigger}"
            //});
        }

        // ========================================
        // 辅助方法
        // ========================================

        /// <summary>
        /// 将内部状态映射到 MachineStatus
        /// </summary>
        private MachineStatus MapToMachineStatus(DetectionState state)
        {
            return state switch
            {
                DetectionState.Idle => MachineStatus.None,
                DetectionState.SelfInspecting => MachineStatus.SelfInspection,
                DetectionState.Ready => MachineStatus.SelfInspectionSuccess,
                DetectionState.PreparingDetection => MachineStatus.Sampling,
                DetectionState.WaitingForFirstClean => MachineStatus.Sampling,
                DetectionState.MovingToShelf => MachineStatus.Sampling,
                DetectionState.PositioningAtSample => MachineStatus.Sampling,
                DetectionState.Scanning => MachineStatus.Sampling,
                DetectionState.SamplingAndPushingCard => MachineStatus.Sampling,
                DetectionState.WaitingToAddSample => MachineStatus.Sampling,
                DetectionState.AddingSample => MachineStatus.Sampling,
                DetectionState.MovingToReactionArea => MachineStatus.Sampling,
                DetectionState.EnqueuedForTest => MachineStatus.Sampling,
                DetectionState.PausedDueToFullQueue => MachineStatus.Sampling,
                DetectionState.Finishing => MachineStatus.SamplingFinished,
                DetectionState.WaitingForTests => MachineStatus.Testing,
                DetectionState.Completed => MachineStatus.TestingEnd,
                DetectionState.Error => MachineStatus.RunningError,
                _ => MachineStatus.None
            };
        }

        // ========================================
        // 公开方法
        // ========================================

        /// <summary>
        /// 校验是否可以开始检测
        /// </summary>
        /// <param name="errorMsg">错误信息</param>
        /// <returns>是否允许开始</returns>
        public bool ValidateStartDetection(out string errorMsg)
        {
            errorMsg = string.Empty;

            if (_context.IsSelfInspectionFailed)
            {
                errorMsg = "SelfInspectionFailed"; // 对应 UI 的自检失败对话框
                return false;
            }

            if (_context.CurrentState == DetectionState.Error)
            {
                errorMsg = "RunningError";
                return false;
            }

            if (_context.CurrentState == DetectionState.Idle)
            {
                errorMsg = "NotSelfInspected";
                return false;
            }

            if (!_context.IsIdleOrReady)
            {
                errorMsg = "AlreadyTesting";
                return false;
            }

            if (_homeService.ReactionAreaQueueIsFull())
            {
                errorMsg = "ReactionAreaFull";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 处理从串口收到的仪器状态数据
        /// </summary>
        /// <param name="response">串口响应数据</param>
        public async Task HandleMachineStatusReceived(BaseResponseModel<MachineStatusModel> response)
        {
            if (response?.Data == null) return;

            // 1. 更新上下文数据
            _context.UpdateFromModel(response.Data);

            _logService.Info($"[状态机] 收到仪器状态响应，当前状态: {CurrentState}, 校验通过: {_context.IsMachineStatusValid}");

            // 2. 如果正在准备检测阶段，则执行业务逻辑判定

            if (_context.IsMachineStatusValid)
            {
                // 状态正常，触发迁移到 WaitingForFirstClean
                await FireAsync(DetectionTrigger.MachineStatusReceived);
            }
            else
            {
                // 状态异常，通知 UI 显示错误对话框
                _mailboxService.Post(new MachineStatusValidationErrorEvent
                {
                    ErrorMessage = _context.GetMachineStatusErrorMessage()
                });
            }

        }

        private bool IsPushCardSuccess(PushCardModel data)
        {
            return data.Success == "1" && !string.IsNullOrEmpty(data.QrCode);
        }

        /// <summary>
        /// 触发状态机（核心方法）
        /// </summary>
        public async Task FireAsync(DetectionTrigger trigger)
        {
            try
            {
                if (_machine.CanFire(trigger))
                {
                    await _machine.FireAsync(trigger);
                }
                else
                {
                    _logService.Warning($"[状态机] 当前状态 {_machine.State} 不接受触发器 {trigger}");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"[状态机] 触发失败: {ex.Message}\n{ex.StackTrace}");
                if (_machine.CanFire(DetectionTrigger.ErrorOccurred))
                {
                    await _machine.FireAsync(DetectionTrigger.ErrorOccurred);
                }
            }
        }

        /// <summary>
        /// 检查是否可以触发某个触发器
        /// </summary>
        public bool CanFire(DetectionTrigger trigger)
        {
            return _machine.CanFire(trigger);
        }

        /// <summary>
        /// 导出状态机图（Graphviz DOT 格式）
        /// </summary>
        public string ExportDotGraph()
        {
            return Stateless.Graph.UmlDotGraph.Format(_machine.GetInfo());
        }

        /// <summary>
        /// 获取当前可用的触发器列表
        /// </summary>
        public string[] GetPermittedTriggers()
        {
            return _machine.PermittedTriggers.Select(t => t.ToString()).ToArray();
        }

        // ========================================
        // 检测操作（独立于状态机流转，但统一由状态机管理）
        // ========================================

        /// <summary>
        /// 执行检测操作
        /// 检测操作独立于取样状态机流转，由反应区队列时间驱动
        /// 只需检查仪器是否异常，不依赖当前取样状态
        /// 
        /// 检测完成后要检查是否有样本因反应区满而暂停，如果有则继续取样
        /// </summary>
        /// <param name="item">反应区项目</param>
        public async Task ExecuteTestAsync(ReactionAreaItem item)
        {
            if (item?.TestResult?.Project == null)
            {
                _logService.Warning("[检测] 项目为空，无法检测");
                return;
            }

            // 检查仪器是否异常
            if (CurrentState == DetectionState.Error)
            {
                _logService.Warning("[检测] 仪器异常，无法检测");
                return;
            }

            Project project = item.TestResult.Project;
            string cardType = project.ProjectType + ""; // 项目类型 0：单联卡 1：双联卡
            string testType = project.TestType + "";     // 测试类型 0：普通卡 1：质控卡

            _logService.Info($"[检测] 开始检测 ID={item.TestResult.Id}, 位置=({item.ReactionAreaX},{item.ReactionAreaY}), 卡类型={cardType}, 测试类型={testType}");

            _context.ReactionAreaTestX = item.ReactionAreaX;
            _context.ReactionAreaTestY = item.ReactionAreaY;
            _context.TestResultId = item.TestResult.Id;
            // 执行检测指令
            await SafeSerialPortCallAsync(
                () => _commandFacade.TestAsync(
                    item.ReactionAreaX,
                    item.ReactionAreaY,
                    cardType,
                    testType,
                    project.ScanStart ?? "",
                    project.ScanEnd ?? "",
                    project.PeakWidth ?? "",
                    project.PeakDistance ?? ""
                ),
                async (ret) =>
                {
                    _logService.Info($"[检测] 检测完成 ID={item.TestResult.Id}");
                    _mailboxService.Post(new TestCompletedEvent { Result = ret });

                    // 检测完成后检查是否有样本因反应区满而暂停，如果有则继续取样
                    await CheckAndResumeIfPausedAsync();
                });
        }

        /// <summary>
        /// 检查并恢复取样（如果因反应区满而暂停）
        /// 检测完成后调用，如果当前状态是 PausedDueToFullQueue，则触发继续取样
        /// </summary>
        private async Task CheckAndResumeIfPausedAsync()
        {
            if (CurrentState == DetectionState.PausedDueToFullQueue)
            {
                _logService.Info("[状态机] 检测完成，反应区有空位，恢复取样");
                await FireAsync(DetectionTrigger.ReactionAreaSpaceAvailable);
            }
        }

        // ========================================
        // 硬件指令安全调用封装
        // ========================================

        /// <summary>
        /// 安全执行串口指令，统一处理异常
        /// </summary>
        private async Task SafeSerialPortCallAsync<T>(
            Func<Task<BaseResponseModel<T>>> command,
            Action<BaseResponseModel<T>> onSuccess)
        {
            try
            {
                var ret = await command();
                onSuccess?.Invoke(ret);
            }
            catch (SerialCommandException ex)
            {
                _logService.Error($"[状态机] 串口指令异常: {ex.Code} {ex.Message}");
                await HandleSerialError();
            }
            catch (Exception ex)
            {
                _logService.Error($"[状态机] 串口未知异常: {ex.Message}");
                await HandleSerialError();
            }
        }

        /// <summary>
        /// 处理串口错误
        /// </summary>
        private async Task HandleSerialError()
        {
            if (_machine.CanFire(DetectionTrigger.ErrorOccurred))
            {
                await _machine.FireAsync(DetectionTrigger.ErrorOccurred);
            }
        }
    }
}
