using System;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Model.Events;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.StateMachine;
using Serilog;
using Stateless;

namespace FluorescenceFullAutomatic.ViewModels
{
    /// <summary>
    /// QC 质控流程状态机 - 负责质控流程控制和硬件指令下发
    /// </summary>
    public class QCStateMachine
    {
        #region 常量
        /// <summary>
        /// 最大检测次数
        /// </summary>
        private const int QC_TEST_COUNT = 10;

        /// <summary>
        /// 检测间隔（毫秒）
        /// </summary>
        private const int TEST_INTERVAL = 2000;
        #endregion

        #region 私有字段
        private readonly IEventMailboxService _mailboxService;
        private readonly ISerialPortCommandFacade _commandFacade;
        private readonly IProjectService _projectService;
        private readonly ILogService _logService;
        private readonly IToolService _toolService;
        private readonly IPointService _pointService;
        private readonly IReactionAreaService _reactionAreaService;
        private readonly IConfigService _configService;
        private readonly StateMachine<QCState, QCTrigger> _machine;

        // 状态上下文
        private QCState _currentState = QCState.Idle;
        private int _currentTestCount = 0;
        private bool _cardExist = false;
        private int _cardNum = 0;
        private Project _currentProject = null;
        private TestResult _currentTestResult = null;
        private ReactionAreaItem _currentTestItem = null;
        private string _lastPushCardQrCode = "";
        #endregion

        #region 属性
        /// <summary>
        /// 当前状态
        /// </summary>
        public QCState CurrentState => _currentState;

        /// <summary>
        /// 当前检测次数
        /// </summary>
        public int CurrentTestCount => _currentTestCount;

        /// <summary>
        /// 当前项目
        /// </summary>
        public Project CurrentProject => _currentProject;

        /// <summary>
        /// 当前检测结果
        /// </summary>
        public TestResult CurrentTestResult => _currentTestResult;

        /// <summary>
        /// 当前检测项
        /// </summary>
        public ReactionAreaItem CurrentTestItem => _currentTestItem;

        /// <summary>
        /// 状态上下文
        /// </summary>
        public StateContext Context => _context;
        private StateContext _context;
        #endregion

        #region 构造函数
        public QCStateMachine(
            IEventMailboxService mailboxService,
            ISerialPortCommandFacade commandFacade,
            IProjectService projectService,
            ILogService logService,
            IToolService toolService,
            IPointService pointService,
            IReactionAreaService reactionAreaService,
            IConfigService configService
        )
        {
            _mailboxService = mailboxService;
            _commandFacade = commandFacade;
            _projectService = projectService;
            _logService = logService;
            _toolService = toolService;
            _pointService = pointService;
            _configService = configService;
            _reactionAreaService = reactionAreaService;
            _context = new StateContext(_configService);
            // 创建状态机
            _machine = new StateMachine<QCState, QCTrigger>(
                () => _currentState,
                s => _currentState = s
            );

            // 配置状态机
            ConfigureStateMachine();
        }
        #endregion

        #region 状态机配置
        /// <summary>
        /// 配置状态机（定义所有状态和迁移规则）
        /// </summary>
        private void ConfigureStateMachine()
        {
            // ====== Idle 空闲状态 ======
            _machine
                .Configure(QCState.Idle)
                .PermitDynamic(
                    QCTrigger.StartQC,
                    () =>
                    {
                        var errorType = ValidateStartQC();
                        if (errorType == QCValidationErrorType.None)
                        {
                            return QCState.PreparingQC;
                        }
                        // 校验失败，通知 UI 错误原因
                        _mailboxService.Post(new QCValidationErrorEvent { ErrorType = errorType });
                        return QCState.Idle;
                    }
                );

            // ====== PreparingQC 准备质控 ======
            _machine
                .Configure(QCState.PreparingQC)
                .OnEntryAsync(OnEnterPreparingQCAsync)
                .Permit(QCTrigger.CardAvailable, QCState.PushingCard)
                .Permit(QCTrigger.CardNotAvailable, QCState.WaitingForCard)
                .Permit(QCTrigger.CancelQC, QCState.Idle)
                .Permit(QCTrigger.ErrorOccurred, QCState.Error);

            // ====== WaitingForCard 等待添加检测卡 ======
            _machine
                .Configure(QCState.WaitingForCard)
                .Permit(QCTrigger.RetryPushCard, QCState.PreparingQC) //重试推卡
                .Permit(QCTrigger.ProjectInvalid, QCState.PreparingQC) //无效重试推卡
                .Permit(QCTrigger.CancelQC, QCState.Idle);

            // ====== PushingCard 推卡中 ======
            _machine
                .Configure(QCState.PushingCard)
                .OnEntryAsync(OnEnterPushingCardAsync)
                .Permit(QCTrigger.ProjectValid, QCState.MovingToReaction)
                .Permit(QCTrigger.ProjectInvalid, QCState.WaitingForCard)
                .Permit(QCTrigger.PushCardFailed, QCState.WaitingForCard)
                .Permit(QCTrigger.ErrorOccurred, QCState.Error);

            // ====== WaitingCardConfirm 等待项目确认 ======
            //_machine.Configure(QCState.WaitingCardConfirm)
            //    .Permit(QCTrigger.ProjectValid, QCState.MovingToReaction)
            //    .Permit(QCTrigger.ProjectInvalid, QCState.PreparingQC)
            //    .Permit(QCTrigger.CancelQC, QCState.Idle);

            // ====== MovingToReaction 移动到反应区 ======
            _machine
                .Configure(QCState.MovingToReaction)
                .OnEntryAsync(OnEnterMovingToReactionAsync)
                .Permit(QCTrigger.MoveReactionCompleted, QCState.Testing)
                .Permit(QCTrigger.ErrorOccurred, QCState.Error);

            // ====== Testing 检测中 ======
            _machine
                .Configure(QCState.Testing)
                .OnEntryAsync(OnEnterTestingAsync)
                .Permit(QCTrigger.TestSingleCompleted, QCState.WaitingNextTest)
                .Permit(QCTrigger.AllTestsCompleted, QCState.Completed)
                .Permit(QCTrigger.ErrorOccurred, QCState.Error);

            // ====== WaitingNextTest 等待下一次检测 ======
            _machine
                .Configure(QCState.WaitingNextTest)
                .OnEntryAsync(OnEnterWaitingNextTestAsync)
                .Permit(QCTrigger.RetryPushCard, QCState.Testing)
                .Permit(QCTrigger.ErrorOccurred, QCState.Error);

            // ====== Completed 完成 ======
            _machine
                .Configure(QCState.Completed)
                .OnEntry(OnEnterCompleted)
                .Permit(QCTrigger.StartQC, QCState.PreparingQC);

            // ====== Error 错误状态 ======
            _machine.Configure(QCState.Error).Permit(QCTrigger.CancelQC, QCState.Idle);
        }
        #endregion

        #region 公共方法


        /// <summary>
        /// 安全执行串口指令，统一处理异常
        /// </summary>
        private async Task SafeSerialPortCallAsync<T>(
            Func<Task<BaseResponseModel<T>>> command,
            Action<BaseResponseModel<T>> onSuccess
        )
        {
            try
            {
                var ret = await command();
                onSuccess?.Invoke(ret);
            }
            catch (SerialCommandException ex)
            {
                _logService.Error($"[状态机] 串口指令异常: {ex.Code} {ex.Message}");
                _mailboxService.Post(
                    new SerialPortErrorEvent
                    {
                        ErrorType = SerialPortErrorType.DeviceError,
                        CommandCode = ex.Code,
                        ErrorMessage = ex.DeviceError,
                    }
                );
                await HandleSerialError();
            }
            catch (TimeoutException ex)
            {
                _logService.Error($"[状态机] 串口超时: {ex.Message}");
                _mailboxService.Post(
                    new SerialPortErrorEvent
                    {
                        ErrorType = SerialPortErrorType.Timeout,
                        CommandCode = "",
                        ErrorMessage = ex.Message,
                    }
                );
                await HandleSerialError();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("串口重发"))
            {
                _logService.Error($"[状态机] 串口命令重发: {ex.Message}");
                _mailboxService.Post(
                    new SerialPortErrorEvent
                    {
                        ErrorType = SerialPortErrorType.CommandDuplicate,
                        CommandCode = "",
                        ErrorMessage = ex.Message,
                    }
                );
                await HandleSerialError();
            }
            catch (Exception ex)
            {
                _logService.Error($"[状态机] 串口异常: {ex.Message}");
                _mailboxService.Post(
                    new SerialPortErrorEvent
                    {
                        ErrorType = SerialPortErrorType.Other,
                        CommandCode = "",
                        ErrorMessage = ex.Message,
                    }
                );
                await HandleSerialError();
            }
        }

        /// <summary>
        /// 处理串口错误
        /// </summary>
        private async Task HandleSerialError()
        {
            if (_machine.CanFire(QCTrigger.ErrorOccurred))
            {
                await _machine.FireAsync(QCTrigger.ErrorOccurred);
            }
        }

        /// <summary>
        /// 触发状态迁移
        /// </summary>
        public async Task FireAsync(QCTrigger trigger)
        {
            try
            {
                Log.Information($"[QCStateMachine] 触发: {trigger}, 当前状态: {_currentState}");
                await _machine.FireAsync(trigger);
                Log.Information($"[QCStateMachine] 迁移后状态: {_currentState}");
            }
            catch (Exception ex)
            {
                Log.Error($"[QCStateMachine] 状态迁移失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 校验是否可以开始质控
        /// </summary>
        public QCValidationErrorType ValidateStartQC()
        {
            // 检查是否有其他类型检测正在进行
            //if (SystemGlobal.TestType != TestType.None && SystemGlobal.TestType != TestType.QC)
            //{
            //    return QCValidationErrorType.OtherTestInProgress;
            //}

            // 检查仪器状态
            if (SystemGlobal.MachineStatus == MachineStatus.None)
            {
                return QCValidationErrorType.NotSelfInspected;
            }

            if (SystemGlobal.MachineStatus == MachineStatus.SelfInspectionFailed)
            {
                return QCValidationErrorType.SelfInspectionFailed;
            }

            if (
                SystemGlobal.MachineStatus == MachineStatus.Sampling
                || SystemGlobal.MachineStatus == MachineStatus.SamplingFinished
            )
            {
                return QCValidationErrorType.AlreadyTesting;
            }

            if (SystemGlobal.MachineStatus.IsRunningError())
            {
                return QCValidationErrorType.RunningError;
            }

            // 检查反应区是否为空
            if (!_reactionAreaService.IsFull())
            {
                return QCValidationErrorType.ReactionAreaNotEmpty;
            }

            return QCValidationErrorType.None;
        }

        string hintMsg = "";

        /// <summary>
        /// 处理仪器状态反馈
        /// </summary>
        public async Task HandleMachineStatusReceivedAsync(
            BaseResponseModel<MachineStatusModel> model
        )
        {
            if (_currentState != QCState.PreparingQC)
            {
                return;
            }

            var data = model.Data;
            _cardExist = data.CardExist == "1";
            int.TryParse(data.CardNum, out _cardNum);

            if (_cardExist && _cardNum > 0)
            {
                await FireAsync(QCTrigger.CardAvailable);
            }
            else
            {
                string errorMsg =
                    $"仪器状态异常,{(_cardExist == false ? "卡仓不存在," : "")}{(_cardNum <= 0 ? "检测卡不足," : "")}";
                hintMsg = errorMsg.TrimEnd(',');
                _mailboxService.Post(new QCNoCardAvailableEvent { ErrorMessage = hintMsg });
                await FireAsync(QCTrigger.CardNotAvailable);
            }
        }

        /// <summary>
        /// 处理推卡反馈
        /// </summary>
        public async Task HandlePushCardReceivedAsync(BaseResponseModel<PushCardModel> model)
        {
            if (_currentState != QCState.PushingCard)
            {
                return;
            }

            var data = model.Data;
            bool success =
                data.Success == PushCardModel.PushCardSuccess && !string.IsNullOrEmpty(data.QrCode);

            if (success)
            {
                _lastPushCardQrCode = data.QrCode;
                //await FireAsync(QCTrigger.PushCardCompleted);

                // 验证项目
                await ValidateProjectAsync(data.QrCode);
            }
            else
            {
                Log.Information("[QCStateMachine] 推卡失败");
                await FireAsync(QCTrigger.PushCardFailed);
            }
        }

        /// <summary>
        /// 验证项目是否为 QC 项目
        /// </summary>
        private async Task ValidateProjectAsync(string qrCode)
        {
            _currentProject = _projectService.GetProjectForQrcode(qrCode);

            if (_currentProject == null)
            {
                Log.Information("[QCStateMachine] 项目为空");
                _mailboxService.Post(new QCProjectInvalidEvent { ErrorMessage = "项目为空" });
                await FireAsync(QCTrigger.ProjectInvalid);
                return;
            }

            if (_currentProject.TestType != Project.Test_Type_QC)
            {
                Log.Information("[QCStateMachine] 此项目不是质控项目");
                _mailboxService.Post(
                    new QCProjectInvalidEvent { ErrorMessage = "此项目不是质控项目" }
                );
                await FireAsync(QCTrigger.ProjectInvalid);
                return;
            }

            _context.CurrentAddingSampleTestResult = new TestResult { Project = _currentProject };
            _context.MovingToReactionAreaTestResult = _context.CurrentAddingSampleTestResult;
            await FireAsync(QCTrigger.ProjectValid);
        }

        /// <summary>
        /// 处理移动反应区反馈
        /// </summary>
        public async Task HandleMoveReactionAreaReceivedAsync(
            BaseResponseModel<MoveReactionAreaModel> model
        )
        {
            if (_currentState != QCState.MovingToReaction)
            {
                return;
            }
            //更新反应区状态
            _reactionAreaService.UpdateItem(
                _context.ReactionAreaY,
                _context.ReactionAreaX,
                (item) =>
                {
                    item.State = ReactionAreaItem.STATE_WAIT;
                    item.TestResult = _context.MovingToReactionAreaTestResult;
                    item.ReactionAreaY = _context.ReactionAreaY;
                    item.ReactionAreaX = _context.ReactionAreaX;
                    _mailboxService.Post(new QCDequeueEvent { CurrentTestItem = item });
                    return item;
                }
            );
            await FireAsync(QCTrigger.MoveReactionCompleted);
        }

        /// <summary>
        /// 测试结果返回
        /// </summary>
        public async Task HandleTestReceivedAsync(BaseResponseModel<TestModel> model)
        {
            if (_currentState != QCState.Testing)
            {
                return;
            }

            // 增加计数
            _currentTestCount++;

            // 处理测试结果逻辑
            ProcessTestResult(model);

            if (_currentTestCount >= QC_TEST_COUNT)
            {
                _reactionAreaService.UpdateItem(
                    _context.ReactionAreaY,
                    _context.ReactionAreaX,
                    (item) =>
                    {
                        item.State = ReactionAreaItem.STATE_END;
                        item.TestResult = _context.MovingToReactionAreaTestResult;
                        item.ReactionAreaY = _context.ReactionAreaY;
                        item.ReactionAreaX = _context.ReactionAreaX;
                        return item;
                    }
                );

                qualityFinish();

                await FireAsync(QCTrigger.AllTestsCompleted);
            }
            else
            {
                await FireAsync(QCTrigger.TestSingleCompleted);
            }
        }

        private void qualityFinish()
        {
            _mailboxService.Post(new QCFinishEvent());
            // // 获取当前时间作为质控时间
            // QcTime = DateTime.Now.GetDateTimeString();

            // // 计算变异系数
            // if (ResultPoints.Count > 0)
            // {
            //     // 计算项目1的变异系数
            //     double[] tcValues = ResultPoints.Select(p => double.Parse(p.Tc)).ToArray();
            //     Variance = CalculateVariance(tcValues);

            //     // 如果是双联卡，计算项目2的变异系数
            //     if (CurrentTestItem.TestResult.Project.ProjectType == Project.Project_Type_Double)
            //     {
            //         double[] tc2Values = ResultPoints.Select(p => double.Parse(p.Tc2)).ToArray();
            //         Variance2 = CalculateVariance(tc2Values);
            //     }
            // }

            // // 判断变异系数是否在合格范围内
            // bool isQualified = Variance >= 0 && Variance <= maxVarianceScope;
            // if (CurrentTestItem.TestResult.Project.ProjectType == Project.Project_Type_Double)
            // {
            //     isQualified = isQualified && (Variance2 >= 0 && Variance2 <= maxVarianceScope);
            // }
            // QcResult = isQualified ? "合格" : "不合格";
            // // 显示结果
            // string resultMsg = $"质控时间：{QcTime}\n" +
            //                  $"项目1变异系数：{Variance}%\n";
            // if (CurrentTestItem.TestResult.Project.ProjectType == Project.Project_Type_Double)
            // {
            //     resultMsg += $"项目2变异系数：{Variance2}%\n";
            // }

            // resultMsg += $"标准方差范围：{VarianceScope}\n" +
            //             $"质控结果：{QcResult}";
        }

        /// <summary>
        /// 用户确认已添加检测卡
        /// </summary>
        public async Task HandleCardAddedConfirmAsync()
        {
            if (_currentState == QCState.WaitingForCard)
            {
                await FireAsync(QCTrigger.RetryPushCard);
            }
        }

        /// <summary>
        /// 用户重试推卡
        /// </summary>
        public async Task HandleRetryPushCardAsync()
        {
            if (_currentState == QCState.WaitingForCard)
            {
                await FireAsync(QCTrigger.ProjectInvalid);
            }
        }

        /// <summary>
        /// 取消质控
        /// </summary>
        public async Task HandleCancelQCAsync()
        {
            await FireAsync(QCTrigger.CancelQC);
        }

        /// <summary>
        /// 初始化状态（开始新的质控流程前调用）
        /// </summary>
        public void InitState()
        {
            _currentTestCount = 0;
            _cardExist = false;
            _cardNum = 0;
            _currentProject = null;
            _currentTestResult = null;
            _currentTestItem = null;
            _lastPushCardQrCode = "";

            // 设置全局状态
            SystemGlobal.TestType = TestType.QC;
            SetMachineState(MachineStatus.Sampling);
        }

        /// <summary>
        /// 设置当前检测项
        /// </summary>
        public void SetCurrentTestItem(ReactionAreaItem item)
        {
            _currentTestItem = item;
        }
        #endregion

        #region 状态进入动作
        /// <summary>
        /// 进入准备质控状态
        /// </summary>
        private async Task OnEnterPreparingQCAsync()
        {
            Log.Information("[QCStateMachine] 进入 PreparingQC 状态，获取仪器状态");
            InitState();
            try
            {
                await SafeSerialPortCallAsync(
                    () => _commandFacade.GetMachineStateAsync(),
                    async (ret) =>
                    {
                        await HandleMachineStatusReceivedAsync(ret);
                    }
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[QCStateMachine] 获取仪器状态失败: {ex.Message}");
                await FireAsync(QCTrigger.ErrorOccurred);
            }
        }

        /// <summary>
        /// 进入推卡状态
        /// </summary>
        private async Task OnEnterPushingCardAsync()
        {
            Log.Information("[QCStateMachine] 进入 PushingCard 状态，执行推卡");
            try
            {
                await SafeSerialPortCallAsync(
                    () => _commandFacade.PushCardAsync(),
                    async (ret) =>
                    {
                        await HandlePushCardReceivedAsync(ret);
                    }
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[QCStateMachine] 推卡失败: {ex.Message}");
                await FireAsync(QCTrigger.ErrorOccurred);
            }
        }

        /// <summary>
        /// 进入移动反应区状态
        /// </summary>
        private async Task OnEnterMovingToReactionAsync()
        {
            Log.Information("[QCStateMachine] 进入 MovingToReaction 状态，移动到反应区 (0, 0)");
            try
            {
                await SafeSerialPortCallAsync(
                    () =>
                    {
                        _context.ReactionAreaX = 0;
                        _context.ReactionAreaY = 0;
                        return _commandFacade.MoveReactionAreaAsync(
                            _context.ReactionAreaX,
                            _context.ReactionAreaY
                        );
                    },
                    async (ret) =>
                    {
                        await HandleMoveReactionAreaReceivedAsync(ret);
                    }
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[QCStateMachine] 移动反应区失败: {ex.Message}");
                await FireAsync(QCTrigger.ErrorOccurred);
            }
        }

        /// <summary>
        /// 检测状态
        /// </summary>
        private async Task OnEnterTestingAsync()
        {
            Log.Information(
                $"[QCStateMachine] 进入 Testing 状态，开始第 {_currentTestCount + 1} 次检测"
            );

            if (_currentProject == null)
            {
                Log.Error("[QCStateMachine] 当前项目为空，无法检测");
                await FireAsync(QCTrigger.ErrorOccurred);
                return;
            }

            string cardType = _currentProject.ProjectType + "";
            string testType = _currentProject.TestType + "";

            // 第 10 次检测使用标准卡
            if (_currentTestCount == QC_TEST_COUNT - 1)
            {
                testType = "" + Project.Test_Type_Stadard;
            }

            await SafeSerialPortCallAsync(
                async () =>
                {
                    return await _commandFacade.TestAsync(
                        0,
                        0,
                        cardType,
                        testType,
                        _currentProject.ScanStart,
                        _currentProject.ScanEnd,
                        _currentProject.PeakWidth,
                        _currentProject.PeakDistance
                    );
                },
                async (result) =>
                {
                    await HandleTestReceivedAsync(result);
                }
            );
        }

        /// <summary>
        /// 进入等待下一次检测状态
        /// </summary>
        private async Task OnEnterWaitingNextTestAsync()
        {
            Log.Information(
                $"[QCStateMachine] 进入 WaitingNextTest 状态，延时 {TEST_INTERVAL}ms 后继续"
            );
            await Task.Delay(TEST_INTERVAL);
            await FireAsync(QCTrigger.RetryPushCard);
        }

        /// <summary>
        /// 进入完成状态
        /// </summary>
        private void OnEnterCompleted()
        {
            Log.Information("[QCStateMachine] 进入 Completed 状态，质控完成");
            // 重置全局状态
            SystemGlobal.TestType = TestType.None;
            SetMachineState(MachineStatus.TestingEnd);
            _mailboxService.Post(new QCCompletedEvent { Message = "质控完成" });
        }

        private void SetMachineState(MachineStatus status)
        {
            _mailboxService.Post(new MachineStateChangeEvent { NewState = status });
        }
        #endregion
        #region 业务逻辑方法

        /// <summary>
        /// 处理测试结果
        /// </summary>
        private void ProcessTestResult(BaseResponseModel<TestModel> model)
        {
            if (model.Data == null)
                return;

            double t = 0,
                c = 0,
                t2 = 0,
                c2 = 0;
            double.TryParse(model.Data.T, out t);
            double.TryParse(model.Data.C, out c);
            double.TryParse(model.Data.T2, out t2);
            double.TryParse(model.Data.C2, out c2);

            var points = model.Data.Point.ToArray();
            var point = new Platform.Model.Point
            {
                Points = points,
                Location = model.Data.Location,
                T = "" + t,
                C = "" + c,
                T2 = "" + t2,
                C2 = "" + c2,
                Tc = _toolService.CalcTC(t, c),
                Tc2 = _toolService.CalcTC(t2, c2),
            };
            int pointId = _pointService.InsertPoint(point);
            point.Id = pointId;

            _mailboxService.Post(
                new QCResultProcessedEvent { Point = point, Index = _currentTestCount - 1 }
            );
        }

        #endregion
    }
}
