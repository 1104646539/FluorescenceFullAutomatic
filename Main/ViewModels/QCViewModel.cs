using CommunityToolkit.Mvvm.ComponentModel;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Views.Ctr;
using FluorescenceFullAutomatic.Views;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.ViewModels;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Utils;
using FluorescenceFullAutomatic.Platform.Model.Events;
using FluorescenceFullAutomatic.Platform.StateMachine;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class QCViewModel : ObservableObject
    {
        #region 字段
        private readonly ISerialPortService serialPortService;
        private readonly ISerialPortCommandFacade serialPortCommandFacade;
        private readonly IPointService pointService;
        private readonly IToolService toolRepository;
        private readonly IProjectService projectRepository;
        private readonly IConfigService configRepository;
        private readonly IReactionAreaService reactionAreaQueueRepository;
        private readonly IPrintService printService;
        private readonly IDialogService dialogRepository;
        private readonly IEventMailboxService mailboxService;
        private readonly IDispatcherService dispatcherService;
        private readonly IQCStateMachine qcStateMachine;

        private readonly ISystemGlobalService systemGlobalService;

        /// <summary>
        /// 卡仓数量
        /// </summary>
        [ObservableProperty]
        private int cardNum;

        /// <summary>
        /// 清洗液是否存在
        /// </summary>
        [ObservableProperty]
        private bool cleanoutFluidExist;

        /// <summary>
        /// 反应区温度
        /// </summary>
        [ObservableProperty]
        private string reactionTemp;

        [ObservableProperty]
        private string stateMsg;

        /// <summary>
        /// 检测结束要显示的提示
        /// </summary>
        string TestFinishedHiltMsg = "";

        /// <summary>
        /// 最大检测次数    
        /// </summary>
        private const int QC_TEST_COUNT = 10;

        /// <summary>
        /// 检测间隔
        /// </summary>
        private const int Test_Interval = 1000;
        /// <summary>
        /// 反应区视图模型
        /// </summary>
        public ReactionAreaViewModel ReactionAreaViewModel { get; set; }
        [ObservableProperty]
        private ReactionAreaItem currentTestItem;
        [ObservableProperty]
        private string showQCCardQCText;

        [ObservableProperty]
        private bool isEnabled;
        /// <summary>
        /// 显示在界面的检测结果
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Point> resultPoints = new ObservableCollection<Point>();
        #endregion
        public QCViewModel(IToolService toolRepository, ISerialPortService serialService, ISerialPortCommandFacade serialPortCommandFacade
        , IConfigService configRepository
            , IProjectService projectRepository, IReactionAreaService reactionAreaService
            , IDialogService dialogRepository, IPointService pointService, IPrintService printService
            , IEventMailboxService mailboxService, IDispatcherService dispatcherService, ILogService logService, ISystemGlobalService systemGlobalService, IQCStateMachine qcStateMachine)
        {
            this.printService = printService;
            this.pointService = pointService;
            this.toolRepository = toolRepository;
            this.projectRepository = projectRepository;
            this.serialPortService = serialService;
            this.serialPortCommandFacade = serialPortCommandFacade;
            this.configRepository = configRepository;
            this.reactionAreaQueueRepository = reactionAreaService;
            this.dialogRepository = dialogRepository;
            this.mailboxService = mailboxService;
            this.dispatcherService = dispatcherService;
            this.systemGlobalService = systemGlobalService;
            this.qcStateMachine = qcStateMachine;
            // 订阅事件邮箱
            mailboxService.Subscribe(HandlerQCEventAsync);

            ClearResultPoints();
            ChangesView();
            RegisterMsg();
        }

        private void RegisterMsg()
        {
            WeakReferenceMessenger.Default.Register<MainStatusChangeMsg>(this, (r, m) =>
            {
                if (m.What == MainStatusChangeMsg.What_ClickQC)
                {
                    //ClickStartQC();
                }
            });
        }
        public void ClearResultPoints()
        {
            ResultPoints.Clear();
            for (int i = 0; i < QC_TEST_COUNT; i++)
            {
                ResultPoints.Add(null);
            }
        }
        [RelayCommand]
        public void ClickStartQC()
        {
            if (VerifyMachineState())
            {
                Log.Information("仪器状态正常，开始质控");
                StartQC();
            }

        }

        private void StartQC()
        {
            ClearResultPoints();
            // QC 状态机
            mailboxService.Post(new StartQCEvent());
        }

        private void SetMachineStatus(MachineStatus state)
        {
            systemGlobalService.SetMachineStatus(state);
            UpdateMainState();
            StateMsg = systemGlobalService.GetMachineStatus().GetDescription();
        }
        private void UpdateMainState()
        {
            WeakReferenceMessenger.Default.Send(
                new MainStatusChangeMsg() { What = MainStatusChangeMsg.What_ChangeState }
            );
        }

        /// <summary>
        /// 验证仪器状态是否可以开始检测
        /// </summary>
        /// <returns></returns>
        private bool VerifyMachineState()
        {
            string errorMsg = "";
            if (
                systemGlobalService.GetMachineStatus() == MachineStatus.Sampling
                || systemGlobalService.GetMachineStatus() == MachineStatus.SamplingFinished
            )
            {
                Log.Information("正在检测，请等待检测结束。");
                errorMsg = "正在检测，请等待检测结束。";
            }
            else if (systemGlobalService.GetMachineStatus() == MachineStatus.SelfInspectionFailed)
            {
                Log.Information("自检失败，请先自检。");
                errorMsg = "自检失败，请先自检。";
            }
            else if (systemGlobalService.GetMachineStatus() == MachineStatus.None)
            {
                Log.Information("仪器未自检，请先自检。");
                errorMsg = "仪器未自检，请先自检。";
            }
            else if (ReactionAreaIsEmpty())
            {
                Log.Information("反应区不为空，请等待检测结束。");
                errorMsg = "反应区不为空，请等待检测结束。";
            }
            else if (systemGlobalService.GetMachineStatus().IsRunningError())
            {
                Log.Information("仪器运行异常");
                errorMsg = RunningErrorMsg;
            }
            if (!string.IsNullOrEmpty(errorMsg))
            {
                Log.Information("仪器状态异常，请检查仪器状态。");
                dialogRepository.ShowHiltDialog(this,
                    "提示",
                    errorMsg,
                    "好的",
                    (d, dialog) =>
                    {
                        Log.Information("仪器状态异常，请检查仪器状态。");
                    }
                );
            }
            return string.IsNullOrEmpty(errorMsg);
        }

        private bool ReactionAreaIsEmpty()
        {
            return reactionAreaQueueRepository.Count() > 0;
        }

        /// <summary>
        /// 标准方差 项目1
        /// </summary>
        [ObservableProperty]
        double variance;
        /// <summary>
        /// 标准方差 项目2
        /// </summary>
        [ObservableProperty]
        double variance2;
        /// <summary>
        /// 标准方差范围
        /// </summary>
        [ObservableProperty]
        string varianceScope = "0%-5%";
        private const int maxVarianceScope = 5;
        /// <summary>
        /// 质控时间
        /// </summary>
        [ObservableProperty]
        string qcTime;

        [ObservableProperty]
        string qcResult;

        /// <summary>
        /// 计算变异系数
        /// </summary>
        /// <param name="values">需要计算的数据数组</param>
        /// <returns>变异系数（百分比）</returns>
        private double CalculateVariance(double[] values)
        {
            if (values == null || values.Length == 0)
                return 0;

            double mean = values.Average();
            double sumSquares = values.Sum(x => Math.Pow(x - mean, 2));
            double variance = Math.Sqrt(sumSquares / values.Length) / mean * 100;
            // 向下取整到5位小数
            return Math.Floor(variance * 100000) / 100000;
        }
        [RelayCommand]
        private void Print()
        {
            if (ResultPoints.Count == 0)
            {
                //dialogRepository.ShowHiltDialog(this, "提示", "没有检测结果，无法打印", "确定", (d, dialog) => { });
                return;
            }
            printService.PrintTicketQC(QcTime, Variance + "%", Variance2 + "%", VarianceScope, QcResult,
                TranTestResults(ResultPoints)
                , CurrentTestItem.TestResult.Project.ProjectType == Project.Project_Type_Double,
                (f) =>
                {

                }, (e) =>
                {
                    dialogRepository.ShowHiltDialog(this, "打印失败", e, "确定", (d, dialog) => { });
                });
        }

        private List<TestResult> TranTestResults(ObservableCollection<Point> resultPoints)
        {
            List<TestResult> trs = new List<TestResult>();
            for (int i = 0; i < resultPoints.Count; i++)
            {
                trs.Add(new TestResult()
                {
                    Tc = resultPoints[i].Tc,
                    Tc2 = resultPoints[i].Tc2,
                });
            }
            return trs;
        }

        /// <summary>
        /// 计算质控结果
        /// </summary>
        private void CalcQcResult()
        {
            // 获取当前时间作为质控时间
            QcTime = DateTime.Now.GetDateTimeString();

            // 计算变异系数
            if (ResultPoints.Count > 0)
            {
                // 计算项目1的变异系数
                double[] tcValues = ResultPoints.Select(p => double.Parse(p.Tc)).ToArray();
                Variance = CalculateVariance(tcValues);

                // 如果是双联卡，计算项目2的变异系数
                if (CurrentTestItem.TestResult.Project.ProjectType == Project.Project_Type_Double)
                {
                    double[] tc2Values = ResultPoints.Select(p => double.Parse(p.Tc2)).ToArray();
                    Variance2 = CalculateVariance(tc2Values);
                }
            }

            // 判断变异系数是否在合格范围内
            bool isQualified = Variance >= 0 && Variance <= maxVarianceScope;
            if (CurrentTestItem.TestResult.Project.ProjectType == Project.Project_Type_Double)
            {
                isQualified = isQualified && (Variance2 >= 0 && Variance2 <= maxVarianceScope);
            }
            QcResult = isQualified ? "合格" : "不合格";
            // 显示结果
            string resultMsg = $"质控时间：{QcTime}\n" +
                             $"项目1变异系数：{Variance}%\n";
            if (CurrentTestItem.TestResult.Project.ProjectType == Project.Project_Type_Double)
            {
                resultMsg += $"项目2变异系数：{Variance2}%\n";
            }

            resultMsg += $"标准方差范围：{VarianceScope}\n" +
                        $"质控结果：{QcResult}";

            //ShowHiltDialog(
            //    dialogCoordinator,
            //    "质控结果",
            //    resultMsg,
            //    "确定",
            //    (d, dialog) => { }
            //);

        }

        private void ChangesView()
        {
            ShowQCCardQCText = systemGlobalService.GetTestType() == TestType.QC ? "质控中" : "质控卡质控";
            IsEnabled = systemGlobalService.GetTestType() != TestType.QC;
        }

        string RunningErrorMsg = "";

        [RelayCommand]
        public void ClickShowDetails(Point point)
        {
            if (point != null)
            {
                ShowResultDetails(new TestResult() { Point = point, T = point.T, C = point.C, T2 = point.T2, C2 = point.C2 });
            }
        }

        public void ShowResultDetails(TestResult testResult)
        {
            customDialog = new CustomDialog();
            Log.Information($"收到检测结果: {JsonConvert.SerializeObject(testResult)}");
            //testResult = _homeService.GetTestResultAndPoint(testResult.Id);
            ResultDetailsViewModel resultDetailsViewModel = new ResultDetailsViewModel();
            resultDetailsViewModel.Result = testResult;
            resultDetailsViewModel.CloseAction = () =>
            {
                dialogRepository.HideMetroDialogAsync(this, customDialog);
            };
            ResultDetailsControl resultDetailsControl = new ResultDetailsControl();
            resultDetailsControl.Update(resultDetailsViewModel);
            customDialog.Content = resultDetailsControl;

            dialogRepository.ShowMetroDialogAsync(this, customDialog);
        }
        private CustomDialog customDialog;

        #region 事件处理
        /// <summary>
        /// 处理 QC 事件
        /// </summary>
        private async Task HandlerQCEventAsync(ITestEvent evt)
        {
            switch (evt)
            {
                case StartQCEvent e:
                    _ = qcStateMachine.FireAsync(QCTrigger.StartQC);
                    break;

                case QCValidationErrorEvent e:
                    dispatcherService.Invoke(() =>
                    {
                        string msg = GetValidationErrorMessage(e.ErrorType);
                        dialogRepository.ShowHiltDialog(this, "提示", msg, "好的", (d, dialog) => { });
                    });
                    break;

                case QCNoCardAvailableEvent e:
                    dispatcherService.Invoke(() =>
                    {
                        dialogRepository.ShowHiltDialog(this, "提示", e.ErrorMessage, "我已添加", async (d, dialog) =>
                        {
                            await qcStateMachine.HandleCardAddedConfirmAsync();
                        }, "结束检测", async (d, dialog) =>
                        {
                            await qcStateMachine.HandleCancelQCAsync();
                        });
                    });
                    break;
                case MachineStateChangeEvent e:
                    SetMachineStatus(e.NewState);
                    break;
                case QCProjectInvalidEvent e:
                    dispatcherService.Invoke(() =>
                    {
                        dialogRepository.ShowHiltDialog(this, "提示", e.ErrorMessage, "再次推卡", async (d, dialog) =>
                        {
                            await qcStateMachine.HandleRetryPushCardAsync();
                        }, "结束检测", async (d, dialog) =>
                        {
                            TestFinishedHiltMsg = "检测结束," + e.ErrorMessage;
                            await qcStateMachine.HandleCancelQCAsync();
                        });
                    });
                    break;
                case QCDequeueEvent e:
                    CurrentTestItem = e.CurrentTestItem;
                    break;
                case QCResultProcessedEvent e:
                    Log.Information($"QCResultProcessedEvent {e}");
                    dispatcherService.Invoke(() =>
                    {
                        ResultPoints[e.Index] = e.Point;
                    });
                    break;
                case QCCompletedEvent e:
                    ChangesView();
                    break;
                case QCFinishEvent e:
                    CalcQcResult();
                    break;
            }
        }

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        private string GetValidationErrorMessage(QCValidationErrorType errorType)
        {
            return errorType switch
            {
                QCValidationErrorType.NotSelfInspected => "仪器未自检，请先自检。",
                QCValidationErrorType.SelfInspectionFailed => "自检失败，请先自检。",
                QCValidationErrorType.AlreadyTesting => "正在检测，请等待检测结束。",
                QCValidationErrorType.OtherTestInProgress => "其他检测正在进行中，请等待。",
                QCValidationErrorType.ReactionAreaNotEmpty => "反应区不为空，请等待检测结束。",
                QCValidationErrorType.RunningError => RunningErrorMsg,
                _ => "仪器状态异常，请检查仪器状态。"
            };
        }


        #endregion

    }
}
