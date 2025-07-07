using CommunityToolkit.Mvvm.ComponentModel;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Services;
using FluorescenceFullAutomatic.Utils;
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
using FluorescenceFullAutomatic.Ex;
using FluorescenceFullAutomatic.ViewModels;
using System.Collections.ObjectModel;
using System.Web.ApplicationServices;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Repositorys;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class QCViewModel : ObservableObject, IReceiveData
    {
        #region 字段
        private readonly ISerialPortService serialPortService;

        private readonly IConfigService configService;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IHomeService homeService;
        private readonly IReactionAreaQueueRepository reactionAreaQueueRepository;
        // 命令执行 状态跟踪
        /// <summary>
        /// 取样命令是否完成
        /// </summary>
        private bool SamplingFinished { get; set; }

        /// <summary>
        /// 自检命令是否完成
        /// </summary>
        private bool SelfInspectionFinished { get; set; }

        /// <summary>
        /// 获取卡仓状态命令是否完成
        /// </summary>
        private bool MachineStateFinished { get; set; }

        /// <summary>
        /// 获取清洗液状态命令是否完成
        /// </summary>
        private bool CleanoutFluidFinished { get; set; }

        /// <summary>
        /// 获取样本架状态命令是否完成
        /// </summary>
        private bool SampleShelfFinished { get; set; }

        /// <summary>
        /// 移动样本架命令是否完成
        /// </summary>
        private bool MoveSampleShelfFinished { get; set; }

        /// <summary>
        /// 移动样本命令是否完成
        /// </summary>
        private bool MoveSampleFinished { get; set; }

        /// <summary>
        /// 清洗取样针命令是否完成
        /// </summary>
        private bool CleanoutSamplingProbeFinished { get; set; }

        /// <summary>
        /// 加样命令是否完成
        /// </summary>
        private bool AddingSampleFinished { get; set; }

        /// <summary>
        /// 排水命令是否完成
        /// </summary>
        private bool DrainageFinished { get; set; }

        /// <summary>
        /// 推卡命令是否完成
        /// </summary>
        private bool PushCardFinished { get; set; }

        /// <summary>
        /// 移动到反应区命令是否完成
        /// </summary>
        private bool MoveReactionAreaFinished { get; set; }

        /// <summary>
        /// 检测命令是否完成
        /// </summary>
        private bool TestFinished { get; set; }

        /// <summary>
        /// 获取/设置反应区温度命令是否完成
        /// </summary>
        private bool ReactionTempFinished { get; set; }

        /// <summary>
        /// 清空反应区命令是否完成
        /// </summary>
        private bool ClearReactionAreaFinished { get; set; }

        /// <summary>
        /// 电机控制命令是否完成
        /// </summary>
        private bool MotorFinished { get; set; }

        /// <summary>
        /// 重载参数命令是否完成
        /// </summary>
        private bool ResetParamsFinished { get; set; }

        /// <summary>
        /// 升级命令是否完成
        /// </summary>
        private bool UpdateFinished { get; set; }

        /// <summary>
        /// 挤压命令是否完成
        /// </summary>
        private bool SqueezingFinished { get; set; }

        /// <summary>
        /// 刺破命令是否完成
        /// </summary>
        private bool PiercedFinished { get; set; }

        /// <summary>
        /// 是否开始后第一次获取仪器状态
        /// </summary>
        private bool IsFirstGetMachineState = false;

        /// <summary>
        /// 第一次加载
        /// </summary>

        /// <summary>
        /// 卡仓是否存在
        /// </summary>
        private bool CardExist = false;

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
        /// 样本架是否存在
        /// </summary>
        private bool[] SampleShelf = new bool[6];

        /// <summary>
        /// 样本架第一个位置
        /// </summary>
        private int SampleShelfFirstPos = -1;

        /// <summary>
        /// 样本架最后一个位置
        /// </summary>
        private int SampleShelfLastPos = -1;

        /// <summary>
        /// 样本架当前位置
        /// </summary>
        private int SampleShelfPos = -1;

        /// <summary>
        /// 当前样本位置
        /// </summary>
        private int SampleCurrentPos = 0;

        /// <summary>
        /// 样本最大位置
        /// </summary>
        private const int SampleMaxPos = 4;

        ///
        /// 是否恢复取样
        /// </summary>
        private bool IsRestoreSampling = false;

        /// <summary>
        /// 恢复取样的取样类型
        /// </summary>
        private string RestoreSamplingType = "";

        /// <summary>
        /// 是否是第一次加载
        /// </summary>
        private bool FirstLoad = true;

        /// <summary>
        /// 是否是检测前想要自检的
        /// </summary>
        private bool IsTestGetSelfMachineState = true;

        /// <summary>
        /// 第一次清洗取样针
        /// </summary>
        private bool IsFirstCleanoutSamplingProbe = true;

        /// <summary>
        /// 检测结束要显示的提示
        /// </summary>
        string TestFinishedHiltMsg = "";

        /// <summary>
        /// 是否因为移动样本而获取仪器状态
        /// </summary>
        private bool IsMoveSampleGetMachineState = false;

        /// <summary>
        /// 当前卡需要放置的 反应区X坐标
        /// </summary>
        private int ReactionAreaX = -1;

        /// <summary>
        /// 当前卡需要放置的 反应区Y坐标
        /// </summary>
        private int ReactionAreaY = 0;

        /// <summary>
        /// 当前加样的结果
        /// </summary>
        private TestResult AddingSampleTestResult;

        /// <summary>
        /// 因为推卡获取仪器状态
        /// </summary>
        private bool IsPushCardGetMachineState = false;

        /// <summary>
        /// 是否恢复推卡
        /// </summary>
        private bool IsRestorePushCard = false;

        /// <summary>
        /// 是否正在检测
        /// </summary>
        private bool IsTesting = false;

        /// <summary>
        /// 正在检测的检测结果ID
        /// </summary>
        private int TestResultId = -1;

        /// <summary>
        /// 检测时检测卡所在坐标 X
        /// </summary>
        private int ReactionAreaTestX = 0;

        /// <summary>
        /// 检测时检测卡所在坐标 Y
        /// </summary>
        private int ReactionAreaTestY = 0;

        /// <summary>
        /// 最大检测次数    
        /// </summary>
        private const int QC_TEST_COUNT = 10;

        /// <summary>
        /// 当前检测次数
        /// </summary>
        private int CurrentTestCount = 0;

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


        #endregion
        public QCViewModel(IHomeService homeService, ISerialPortService serialService, IConfigService configService
            , IDialogCoordinator dialogCoordinator,IReactionAreaQueueRepository reactionAreaQueueRepository)
        {
            this.homeService = homeService;
            this.serialPortService = serialService;
            this.configService = configService;
            this.dialogCoordinator = dialogCoordinator;
            this.reactionAreaQueueRepository = reactionAreaQueueRepository;
            ReactionAreaViewModel = ReactionAreaViewModel.Instance;
            serialPortService.AddReceiveData(this);
            //serialPortService.OnAddDequeue(OnReactionAreaDequeue);

            ClearResultPoints();
            RegisterMsg();
        }
        [RelayCommand]
        private void Print() {
        }
        private void RegisterMsg()
        {
            WeakReferenceMessenger.Default.Register<MainStatusChangeMsg>(this, (r, m) => {
                if (m.What == MainStatusChangeMsg.What_ClickQC)
                {
                    ClickStartQC();
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
            InitState();
            GetMachineState();
        }

        private void SetMachineStatus(MachineStatus state)
        {
            SystemGlobal.MachineStatus = state;
            UpdateMainState();
            StateMsg = SystemGlobal.MachineStatus.GetDescription();
        }
        private void UpdateMainState()
        {
            WeakReferenceMessenger.Default.Send(
                new MainStatusChangeMsg() { What = MainStatusChangeMsg.What_ChangeState }
            );
        }
        private void InitState()
        {
            SetMachineStatus(MachineStatus.Sampling);
            SystemGlobal.TestType = TestType.QC;
            ClearResultPoints();
            CurrentTestCount = 0;
            IsFirstGetMachineState = true;
            SampleShelfFirstPos = -1;
            SampleShelfLastPos = -1;
            SampleCurrentPos = 0;
            // ReactionAreaX = -1;这两个不能初始化，因为可能正在检测
            // ReactionAreaY = 0;
            TestFinishedHiltMsg = "";
            IsFirstCleanoutSamplingProbe = false;
            RestoreSamplingType = "";
            IsRestoreSampling = false;
            CleanoutFluidExist = false;
            CardExist = false;
            CardNum = 0;
            SampleShelf = new bool[6];
            SampleShelfPos = -1;
            Variance = 0;
            Variance2 = 0;
            QcTime = "";

            // 重置所有状态标志为 false
            ResetAllStatusFlags();
        }
        /// <summary>
        /// 验证仪器状态是否可以开始检测
        /// </summary>
        /// <returns></returns>
        private bool VerifyMachineState()
        {
            string errorMsg = "";
            if (
                SystemGlobal.MachineStatus == MachineStatus.Sampling
                || SystemGlobal.MachineStatus == MachineStatus.SamplingFinished
            )
            {
                Log.Information("正在检测，请等待检测结束。");
                errorMsg = "正在检测，请等待检测结束。";
            }
            else if (SystemGlobal.MachineStatus == MachineStatus.SelfInspectionFailed)
            {
                Log.Information("自检失败，请先自检。");
                errorMsg = "自检失败，请先自检。";
            }
            else if (SystemGlobal.MachineStatus == MachineStatus.None)
            {
                Log.Information("仪器未自检，请先自检。");
                errorMsg = "仪器未自检，请先自检。";
            }
            else if (ReactionAreaIsEmpty())
            {
                Log.Information("反应区不为空，请等待检测结束。");
                errorMsg = "反应区不为空，请等待检测结束。";
            }
            else if (SystemGlobal.MachineStatus.IsRunningError())
            {
                Log.Information("仪器运行异常");
                errorMsg = RunningErrorMsg;
            }
            if (!string.IsNullOrEmpty(errorMsg))
            {
                Log.Information("仪器状态异常，请检查仪器状态。");
                GlobalUtil.ShowHiltDialog(
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

        public void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model)
        {
            if (!ContinueTest()) return;
            AddingSampleFinished = true;
            Log.Information($"接收到 加样: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveCleanoutSamplingProbeModel(BaseResponseModel<CleanoutSamplingProbeModel> model)
        {
            if (!ContinueTest()) return;
            CleanoutSamplingProbeFinished = true;
            Log.Information($"接收到 清洗取样针: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveClearReactionAreaModel(BaseResponseModel<ClearReactionAreaModel> model)
        {
            if (!ContinueTest()) return;
            ClearReactionAreaFinished = true;
            Log.Information($"接收到 清空反应区: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveDrainageModel(BaseResponseModel<DrainageModel> model)
        {
            if (!ContinueTest()) return;
            DrainageFinished = true;
            Log.Information($"接收到 排水: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveGetSelfMachineStatusModel(BaseResponseModel<List<string>> model)
        {
            if (!ContinueTest()) return;
            SelfInspectionFinished = true;
            Log.Information($"接收到 自检: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveMachineStatusModel(BaseResponseModel<MachineStatusModel> model)
        {
            if (!ContinueTest()) return;
            MachineStateFinished = true;
            Log.Information($"接收到 仪器状态: {JsonConvert.SerializeObject(model)}");

            if (IsFirstGetMachineState)
            {
                ParseMachineStatus(model.Data);
                //如果仪器状态正常（卡仓存在，卡仓有卡，清洗液存在，样本架存在）,
                //则初始化样本架状态，并清洗取样针，准备开始检测
                IsFirstGetMachineState = false;
                if (
                    CardExist
                    && CardNum > 0
                )
                {
                    PushCard();
                }
                else
                {
                    //如果仪器状态异常，则提示
                    string msg =
                        $"仪器状态异常,{(CardExist == false ? "卡仓不存在," : "")}{(CardNum <= 0 ? "检测卡不足," : "")}";
                    msg = msg.TrimEnd(',');
                    GlobalUtil.ShowHiltDialog(
                        "提示",
                        msg,
                        "重新获取",
                        (d, dialog) =>
                        {
                            IsFirstGetMachineState = true;
                            //重新获取状态
                            GetMachineState();
                        },
                        "结束检测",
                        (d, dialog) =>
                        {
                            //结束检测
                            Log.Information("结束检测1");
                            TestFinishedHiltMsg = "检测结束," + msg;
                            TestFinishedAction();
                        }
                    );
                }
            }
        }

        /// <summary>
        /// 检测卡出队，已经到检测时间
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool OnReactionAreaDequeue(ReactionAreaItem item)
        {
            Log.Information(
                $"OnReactionAreaDequeue IsTesting={IsTesting} {!ContinueTest(true)} {JsonConvert.SerializeObject(item)}"
            );
            if (IsTesting)
            {
                //正在检测
                return false;
            }
            //如果检测模块运行错误，则不能检测
            if (!ContinueTest(true))
                return false;

            CurrentTestItem = item;

            return QCTest(item);
        }
        private bool QCTest(ReactionAreaItem item)
        {
            Project project = item.TestResult.Project;
            if (project == null)
            {
                Log.Information("项目为空");
                return false;
            }
            string cardType = project.ProjectType + ""; //项目类型 0：单联卡 1：双联卡
            string testType = project.TestType + ""; //测试类型 0：普通卡 1：质控卡

            if (CurrentTestCount++ == QC_TEST_COUNT - 1)
            {
                testType = "" + Project.Test_Type_Stadard;
            }

            //记录检测卡所在坐标
            ReactionAreaTestY = item.ReactionAreaY;
            ReactionAreaTestX = item.ReactionAreaX;
            Test(
                item.ReactionAreaX,
                item.ReactionAreaY,
                cardType,
                testType,
                project.ScanStart,
                project.ScanEnd,
                project.PeakWidth,
                project.PeakDistance
            );

            IsTesting = true;
            return true;
        }
        /// <summary>
        /// 检测结束动作开始
        /// </summary>
        private void TestFinishedAction()
        {
            if (SystemGlobal.MachineStatus == MachineStatus.SamplingFinished)
            {
                return;
            }
            SetMachineStatus(MachineStatus.TestingEnd);
            SystemGlobal.TestType = TestType.None;
        }


        /// <summary>
        /// 解析仪器状态数据
        /// </summary>
        /// <param name="data"></param>
        private void ParseMachineStatus(MachineStatusModel data)
        {
            ParseMachineStatusCard(data);
            CleanoutFluidExist = data.CleanoutFluid == "1";
            for (int i = 0; i < 6; i++)
            {
                SampleShelf[i] = data.SamleShelf[i] == 1;
            }
        }

        /// <summary>
        /// 解析卡仓和清洗液状态
        /// </summary>
        /// <param name="data"></param>
        private void ParseMachineStatusCard(MachineStatusModel data)
        {
            CardExist = data.CardExist == "1";
            int tempNum = 0;
            int.TryParse(data.CardNum, out tempNum);
            CardNum = tempNum;
        }
        public void ReceiveMotorModel(BaseResponseModel<MotorModel> model)
        {
            if (!ContinueTest()) return;
            MotorFinished = true;
            Log.Information($"接收到 电机控制: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveMoveReactionAreaModel(BaseResponseModel<MoveReactionAreaModel> model)
        {
            if (!ContinueTest()) return;
            MoveReactionAreaFinished = true;
            Log.Information($"接收到 移动反应区: {JsonConvert.SerializeObject(model)}");
            //更新反应区状态
            ReactionAreaViewModel.UpdateItem(
                ReactionAreaY,
                ReactionAreaX,
                (item) =>
                {
                    item.State = ReactionAreaItem.STATE_WAIT;
                    item.TestResult = AddingSampleTestResult;
                    item.ReactionAreaY = ReactionAreaY;
                    item.ReactionAreaX = ReactionAreaX;
                    //加入等待检测队列
                    //Enqueue(item);
                    return item;
                }
            );
            //直接去检测
            CurrentTestItem = ReactionAreaViewModel.GetItem(ReactionAreaY,ReactionAreaX);
            QCTest(CurrentTestItem);
        }

        /// <summary>
        /// 加入等待检测队列
        /// </summary>
        /// <param name="item"></param>
        private void Enqueue(ReactionAreaItem item)
        {
            reactionAreaQueueRepository.Enqueue(item);
        }

        public void ReceiveMoveSampleModel(BaseResponseModel<MoveSampleModel> model)
        {
            if (!ContinueTest()) return;
            MoveSampleFinished = true;
            Log.Information($"接收到 移动样本: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveMoveSampleShelfModel(BaseResponseModel<MoveSampleShelfModel> model)
        {
            if (!ContinueTest()) return;
            MoveSampleShelfFinished = true;
            Log.Information($"接收到 移动样本架: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceivePiercedModel(BaseResponseModel<PiercedModel> model)
        {
            if (!ContinueTest()) return;
            PiercedFinished = true;
            Log.Information($"接收到 刺破: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceivePushCardModel(BaseResponseModel<PushCardModel> model)
        {
            if (!ContinueTest()) return;
            PushCardFinished = true;
            Log.Information($"接收到 推卡: {JsonConvert.SerializeObject(model)}");

            if (IsPushCardSuccess(model.Data))
            {
                Project project = configService.GetProject(model.Data.QrCode);
                Log.Information($"检测卡项目 {JsonConvert.SerializeObject(project)}" );
                if (project == null)
                {
                    Log.Information("项目为空");
                    //项目为空，暂时走推卡失败流程
                    //重新推卡
                    PushCardGetMachineState();
                    return;
                }
                if (project.TestType != Project.Test_Type_QC)
                {
                    Log.Information("此项目不是质控项目");
                    //此项目不是质控项目
                    GlobalUtil.ShowHiltDialog(
                        "提示",
                        "此项目不是质控项目",
                        "再次推卡",
                        (d, dialog) => { 
                            PushCardGetMachineState();
                        },
                        "结束检测",
                        (d, dialog) => {
                            //结束检测
                            TestFinishedHiltMsg = "检测结束,此项目不是质控项目";
                            TestFinishedAction();
                        }
                    );
                    return;
                }
                AddingSampleTestResult = new TestResult() { Project = project };
                ReactionAreaY = 0;
                ReactionAreaX = 0;
                MoveReactionArea(ReactionAreaX, ReactionAreaY);

            }
            else
            {
                // 推卡失败
                Log.Information("推卡失败");
                // 重新推卡
                PushCardGetMachineState();
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

        /// <summary>
        /// 因为推卡获取仪器状态
        /// </summary>
        private void PushCardGetMachineState()
        {
            IsFirstGetMachineState = true;
            //PushCardFinished = false;
            GetMachineState();
        }
        public void ReceiveReactionTempModel(BaseResponseModel<ReactionTempModel> model)
        {
            if (!ContinueTest()) return;
            ReactionTempFinished = true;
            Log.Information($"接收到 反应区温度: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveResetParamsModel(BaseResponseModel<ResetParamsModel> model)
        {
            if (!ContinueTest()) return;
            ResetParamsFinished = true;
            Log.Information($"接收到 重置参数: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveSamplingModel(BaseResponseModel<SamplingModel> model)
        {
            if (!ContinueTest()) return;
            SamplingFinished = true;
            Log.Information($"接收到 取样: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveSqueezingModel(BaseResponseModel<SqueezingModel> model)
        {
            if (!ContinueTest()) return;
            SqueezingFinished = true;
            Log.Information($"接收到 挤压: {JsonConvert.SerializeObject(model)}");
        }
        [ObservableProperty]
        ObservableCollection<Point> resultPoints = new ObservableCollection<Point>();
        public void ReceiveTestModel(BaseResponseModel<TestModel> model)
        {
            if (!ContinueTest()) return;
            TestFinished = true;
            Log.Information($"接收到 检测: {CurrentTestCount} {JsonConvert.SerializeObject(model)}");

            // 处理检测数据
            double t = 0;
            double c = 0;
            double.TryParse(model.Data.T, out t);
            double.TryParse(model.Data.C, out c);

            double t2 = 0;
            double c2 = 0;
            double.TryParse(model.Data.T2, out t2);
            double.TryParse(model.Data.C2, out c2);
            int[] points = model.Data.Point.ToArray();
            Point point = new Point() { Points = points, Location = model.Data.Location };
            point.T = "" + t;
            point.C = "" + c;
            point.T2 = "" + t2;
            point.C2 = "" + c2;
            point.Tc = configService.CalcTC(t, c);
            point.Tc2 = configService.CalcTC(t2, c2);
            int pointId = homeService.InsertPoint(point);
            point.Id = pointId;
            ResultPoints[CurrentTestCount-1] = point;
            if (CurrentTestCount == QC_TEST_COUNT)
            {
                  //更新反应区状态
            ReactionAreaViewModel.UpdateItem(
                ReactionAreaTestY,
                ReactionAreaTestX,
                (item) =>
                {
                    item.State = ReactionAreaItem.STATE_END;
                    return item;
                }
            );

                //检测结束,计算结果
                TestFinishedAction();
                CalcQcResult();
            }
            else
            {
                //检测未结束，继续检测
                DelayAndExecute();
            }
            IsTesting = false;
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
        string varianceScope = "0%-8%";
        /// <summary>
        /// 质控时间
        /// </summary>
        [ObservableProperty]
        string qcTime;
        
        [ObservableProperty ]
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
            bool isQualified = Variance >= 0 && Variance <= 8;
            if (CurrentTestItem.TestResult.Project.ProjectType == Project.Project_Type_Double)
            {
                isQualified = isQualified && (Variance2 >= 0 && Variance2 <= 8);
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

        private async Task DelayAndExecute()
        {
            await Task.Delay(Test_Interval); // 延时
            QCTest(CurrentTestItem); // 执行任务
        }
      
        public void ReceiveUpdateModel(BaseResponseModel<UpdateModel> model)
        {
            if (!ContinueTest()) return;
            UpdateFinished = true;
            Log.Information($"接收到 升级: {JsonConvert.SerializeObject(model)}");
        }
        public void ReceiveStateError(BaseResponseModel<dynamic> model)
        {
            if (!IsQCType())
                return;
            SystemGlobal.MachineStatus = MachineStatus.RunningError;
            SystemGlobal.ErrorContinueTest = true;
            //如果本来就是检测或移动反应区错误，则运行错误后不继续检测
            if (
                model.Code == SerialGlobal.CMD_Test
                || model.Code == SerialGlobal.CMD_MoveReactionArea
            )
            {
                SystemGlobal.ErrorContinueTest = false;
            }
            RunningErrorMsg = $"运行错误，请联系经销商人员维护。\n错误信息: {model.Error}";
            // 显示错误信息
            GlobalUtil.ShowHiltDialog("提示", RunningErrorMsg, "确定", (d, dialog) => { });
        }
        public void ReceiveVersionModel(BaseResponseModel<VersionModel> model)
        {
            if (!IsQCType())
                return;
        }

        public void ReceiveShutdownModel(BaseResponseModel<ShutdownModel> model)
        {
            if (!IsQCType())
                return;
        }
        /// <summary>
        /// 重置所有命令状态标志 false
        /// </summary>
        private void ResetAllStatusFlags()
        {
            SamplingFinished = false;
            SelfInspectionFinished = false;
            MachineStateFinished = false;
            CleanoutFluidFinished = false;
            SampleShelfFinished = false;
            MoveSampleShelfFinished = false;
            MoveSampleFinished = false;
            CleanoutSamplingProbeFinished = false;
            AddingSampleFinished = false;
            DrainageFinished = false;
            PushCardFinished = false;
            MoveReactionAreaFinished = true;
            // TestFinished = false;//这个不能重置，因为可能正在检测
            ReactionTempFinished = false;
            ClearReactionAreaFinished = false;
            MotorFinished = false;
            ResetParamsFinished = false;
            UpdateFinished = false;
            SqueezingFinished = false;
            PiercedFinished = false;
            IsRestorePushCard = false;
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
            if (!IsQCType())
                return false;

            return true;
        }
        /// <summary>
        /// 检查是否为正常检测类型
        /// </summary>
        /// <returns>如果是正常检测类型返回true，否则返回false</returns>
        private bool IsQCType()
        {
            if (SystemGlobal.TestType != TestType.QC)
            {
                // Log.Information($"非正常检测类型，当前类型：{SystemGlobal.TestType}");
                return false;
            }
            return true;
        }
        // 实现 ISerialPortService 的方法
        public void GetSelfInspectionState()
        {
            SelfInspectionFinished = false;
            Log.Information("执行 自检");
            serialPortService.GetSelfInspectionState();
        }

        public void GetMachineState()
        {
            MachineStateFinished = false;
            Log.Information("执行 仪器状态");
            serialPortService.GetMachineState();
        }

        public void MoveSampleShelf(int pos)
        {
            SampleShelfPos = pos;
            MoveSampleShelfFinished = false;
            Log.Information($"执行 移动样本架 {pos + 1}");
            serialPortService.MoveSampleShelf(pos + 1);
        }

        public void MoveSample(int pos)
        {
            MoveSampleFinished = false;
            //SampleCurrentPos = pos;
            Log.Information($"执行 移动样本 {pos}");
            serialPortService.MoveSample(pos + 1);
        }

        public void Sampling(string type, int volume)
        {
            SamplingFinished = false;
            Log.Information($"执行 取样，类型: {type}，体积: {volume}");
            serialPortService.Sampling(type, volume);
        }

        public void CleanoutSamplingProbe()
        {
            CleanoutSamplingProbeFinished = false;
            Log.Information("执行 清洗取样针");
            serialPortService.CleanoutSamplingProbe(configService.CleanoutDuration());
        }

        public void AddingSample(int volume, string type)
        {
            AddingSampleFinished = false;
            Log.Information($"执行 加样，体积: {volume}，类型: {type}");
            serialPortService.AddingSample(volume, type);
        }

        public void Drainage()
        {
            DrainageFinished = false;
            Log.Information("执行 排水");
            serialPortService.Drainage();
        }

        public void PushCard()
        {
            if (!MoveReactionAreaFinished)
            {
                //还未将上一张卡移动到反应区,先等待
                IsRestorePushCard = true;
                Log.Information("等待 移动反应区 结束");
            }
            else
            {
                //推卡
                PushCardFinished = false;
                Log.Information("执行 推卡");
                serialPortService.PushCard();
            }
        }

        public void MoveReactionArea(int x, int y)
        {
            MoveReactionAreaFinished = false;
            Log.Information($"执行 移动反应区 ({x}, {y})");
            serialPortService.MoveReactionArea(x, y);
        }

        public void Test(
            int x,
            int y,
            string cardType,
            string testType,
            string scanStart,
            string scanEnd,
            string peakWidth,
            string peakDistance
        )
        {
            TestFinished = false;
            Log.Information(
                $"执行 检测，坐标: ({x}, {y})，卡片类型: {cardType}，检测类型: {testType}，"
                    + $"扫描起始: {scanStart}，扫描结束: {scanEnd}，峰值宽度: {peakWidth}，峰值距离: {peakDistance}"
            );
            serialPortService.Test(
                x,
                y,
                cardType,
                testType,
                scanStart,
                scanEnd,
                peakWidth,
                peakDistance
            );
        }

        public void GetReactionTemp(string temp = "0")
        {
            ReactionTempFinished = false;
            Log.Information($"执行 反应区温度，温度: {temp}");
            serialPortService.GetReactionTemp(temp);
        }

        public void ClearReactionArea()
        {
            ClearReactionAreaFinished = false;
            Log.Information("执行 清空反应区");
            serialPortService.ClearReactionArea();
        }

        public void Motor(string motor, string direction, string value)
        {
            MotorFinished = false;
            Log.Information($"执行 电机控制，电机: {motor}，方向: {direction}，值: {value}");
            serialPortService.Motor(motor, direction, value);
        }

        public void ResetParams()
        {
            ResetParamsFinished = false;
            Log.Information("执行 重置参数");
            serialPortService.ResetParams();
        }

        public void Update()
        {
            UpdateFinished = false;
            Log.Information($"执行 升级");
            serialPortService.Update();
        }

        public void Squeezing(string type)
        {
            SqueezingFinished = false;
            Log.Information($"执行 挤压，类型: {type}");
            serialPortService.Squeezing(type);
        }

        public void Pierced(string type)
        {
            PiercedFinished = false;
            Log.Information($"执行 刺破，类型: {type}");
            serialPortService.Pierced(type);
        }

        string RunningErrorMsg = "";

        [RelayCommand]
        public void ClickShowDetails(Point point)
        {
            if (point != null)
            {
                ShowResultDetails(new TestResult() { Point = point, T = point.T, C = point.C });
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
                MainWindow.Instance.HideMetroDialogAsync( customDialog);
            };
            ResultDetailsControl resultDetailsControl = new ResultDetailsControl();
            resultDetailsControl.DataContext = resultDetailsViewModel;
            customDialog.Content = resultDetailsControl;

            MainWindow.Instance.ShowMetroDialogAsync(customDialog);
        }
        private CustomDialog customDialog;

    }
}
