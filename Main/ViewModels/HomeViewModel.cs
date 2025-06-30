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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Ex;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Services;
using FluorescenceFullAutomatic.Sql;
using FluorescenceFullAutomatic.Upload;
using FluorescenceFullAutomatic.Utils;
using FluorescenceFullAutomatic.Views;
using FluorescenceFullAutomatic.Views.Ctr;
using FluorescenceFullAutomatic.ViewModels;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Main.Upload;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using Serilog;
using Spire.Pdf;
using SqlSugar;
using static Main.Upload.Hl7Result;
using Point = FluorescenceFullAutomatic.Model.Point;

namespace FluorescenceFullAutomatic.ViewModels
{
    /// <summary>
    /// 首页
    /// </summary>
    public partial class HomeViewModel : ObservableRecipient, IReceiveData
    {
        #region 属性
        [ObservableProperty]
        public string title;

        private readonly IHomeService homeService;
        private readonly IConfigService configService;
        private readonly ISerialPortService serialPortService;
        private readonly ILisService lisService;
        private readonly IUploadService uploadService;
        private readonly IApplyTestService applyTestService;

        [ObservableProperty]
        public ReactionAreaViewModel reactionAreaViewModel;

        [ObservableProperty]
        public SampleShelfViewModel sampleShelfViewModel;
        public IDialogCoordinator dialogCoordinator;

        /// <summary>
        /// 获取温度间隔
        /// </summary>
        private const int GetReactionTempInterval = 1000 * 30; // 30秒

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
        /// 是否因为自检结束而获取仪器状态
        /// </summary>
        private bool IsSelfMachineGetMachineState = false;
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
        /// 当前正在移动反应区的结果
        /// </summary>
        private TestResult MoveReactionAreaTestResult;

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
        /// 获取反应区温度定时器
        /// </summary>
        private Timer _getReactionTempTimer;

        [ObservableProperty]
        private string imgCard;

        [ObservableProperty]
        private string imgCleanout;

        #endregion
        public HomeViewModel(
            ISerialPortService serialPortService,
            IHomeService homeService,
            IConfigService configService,
            ILisService lisService,
            IUploadService uploadService,
            IApplyTestService applyTestService,
            IDialogCoordinator dialogCoordinator
        )
        {
            this.serialPortService = serialPortService;
            this.homeService = homeService;
            this.configService = configService;
            this.dialogCoordinator = dialogCoordinator;
            this.uploadService = uploadService;
            this.lisService = lisService;
            this.applyTestService = applyTestService;
            this.serialPortService.OnAddDequeue(OnReactionAreaDequeue);
            this.serialPortService.AddReceiveData(this);
            SampleShelfViewModel = new SampleShelfViewModel();
            ReactionAreaViewModel = ReactionAreaViewModel.Instance;
            this.serialPortService.AddScanSuccessListener(OnScanSuccess);
            this.serialPortService.AddScanFailedListener(OnScanFailed);
            ChangeTestSettings();
            this.SampleShelfViewModel.onSelectedSampleItem += OnSampleItemSelected;
            OnSampleItemSelected(null);
            RegisterMsg();
            UpdateState();

            // 移除HL7连接回调代码
            HL7Helper.Instance.IsRunning();
            Test();
        }

        private void Test()
        {
            //for (int i = 0; i < 50; i++)
            //{
            //    TestResult tr = SqlHelper.getInstance().GetTestResult(1);
            //}

            //Task.Run(() => {
            //    for (int i = 0; i < 50; i++)
            //    {
            //        TestResult tr = SqlHelper.getInstance().GetTestResult(1);
            //    }
            //});
        }

        [RelayCommand]
        public void ClickTest1()
        {
            Task.Run(async () =>
            {
                TestResult tr = new TestResult();
                // 病人信息
                tr.Patient = new Patient
                {
                    PatientName = "张三",
                    PatientGender = "男", 
                    PatientAge = "35",
                    InspectDate = DateTime.Now.AddDays(1),
                    InspectDepartment = "内科",
                    InspectDoctor = "李医生",
                    TestDoctor = "王医生",
                    CheckDoctor = "赵医生"
                };
                
                // 项目信息
                tr.Project = new Project
                {
                    ProjectCode = "FOB",
                    ProjectName = "血红蛋白",
                    ProjectUnit = "mg/L",
                    ProjectLjz = 100,
                    BatchNum = "20240401",
                    IdentifierCode = "FBHB001",
                    A1 = 1.2,
                    A2 = 0.8,
                    X0 = 10,
                    P = 1.5,
                    ProjectUnit2 = "ng/mL",
                    ProjectLjz2 = 80,
                    ConMax = 200,
                    A12 = 1.0,
                    A22 = 0.6,
                    X02 = 8,
                    P2 = 1.2,
                    ConMax2 = 150,
                    ProjectType = Project.Project_Type_Single,
                    TestType = Project.Test_Type_Stadard,
                    IsDefault = 0,
                    ScanStart = "100",
                    ScanEnd = "500",
                    PeakWidth = "2.5",
                    PeakDistance = "5"
                };
                
                // 检测信息
                Random random = new Random();
                int[] randomPoints = new int[600];
                for (int i = 0; i < 600; i++)
                {
                    randomPoints[i] = random.Next(0, 5000);
                }
                
                tr.Point = new Point
                {
                    Points = randomPoints,
                    Location = new int[] { 1, 2 },
                    T = "10.5",
                    C = "5.2",
                    Tc = "2",
                    T2 = "8.7",
                    C2 = "4.3",
                    Tc2 = "1.5"
                };
                
                // 设置基本检测信息
                tr.Barcode = "BarCode001";
                tr.CardQRCode = "QR100200300";
                tr.TestNum = "10000";
                tr.FrameNum = "1";
                tr.TestTime = DateTime.Now.AddMonths(5);
                
                // 设置检测结果
                tr.T = "10.5";
                tr.C = "5.2";
                tr.Tc = "2";
                tr.Con = "10.5";
                tr.Result = "正常";
                tr.T2 = "8.7";
                tr.C2 = "4.3";
                tr.Tc2 = "1.5";
                tr.Con2 = "8.7";
                tr.Result2 = "正常";
                tr.TestVerdict = "合格";

                // 上传结果
                Hl7Result.UploadResult ur = await HL7Helper.Instance.UploadTestResultAsync(tr);
                Log.Information($"上传结果={ur.ResultType}");
            });
        }

        [RelayCommand]
        public void ClickTest2()
        {
            Task.Run(async () =>
            {
                QueryType queryType = QueryType.BC;
                string condition1 = "100";
                string condition2 = "";
                Hl7Result.QueryResult qr = await HL7Helper.Instance.QueryApplyTestAsync(
                    queryType,
                    condition1,
                    condition2
                );
                Log.Information(
                    $"查询结果={qr.ResultType} {JsonConvert.SerializeObject(qr.ApplyTests)}"
                );
            });
            
        }

        [RelayCommand]
        public void ClickTest3()
        {
 
        }

        [RelayCommand]
        public void ClickTest4()
        {
           
        }


        public void GetDeviceInfos()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (d.DriveType == DriveType.Removable) // 检测是否为可移动驱动器，如U盘
                {
                    //MessageBox.Show($"Found removable drive: {d.Name}");
                    Log.Information($"Found removable drive: {d.Name} {d.VolumeLabel}");
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
            this.serialPortService.SetEnqueueDuration(configService.ReactionDuration());
        }

        [RelayCommand]
        public void Loaded()
        {
            Log.Information($"Loaded FirstLoad={FirstLoad}");
            if (FirstLoad)
            {
                FirstLoad = false;
                if (SystemGlobal.IsCodeDebug)
                {
                    GoGetSelfMachineStatus();
                }
                else
                {
                    IsTestGetSelfMachineState = false;
                }
            }
        }

        [RelayCommand]
        public void ClickSelfMachineStatus()
        {
            FirstLoad = true;
            Log.Information($"Loaded FirstLoad={FirstLoad}");
            IsTestGetSelfMachineState = true;
            GoGetSelfMachineStatus();
        }

        private void GoGetSelfMachineStatus()
        {
            Log.Information("自检失败，点击重新自检 GoGetSelfMachineStatus");
            IsTestGetSelfMachineState = true;
            IsSelfMachineGetMachineState = true;
            SetMachineStatus(MachineStatus.None);

            ShowSelfMachineDialog();
            GetSelfInspectionState();
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
            SystemGlobal.TestType = TestType.Test;
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
            //CleanoutFluidExist = false;
            //CardExist = false;
            //CardNum = 0;
            SampleShelf = new bool[6];
            SampleShelfPos = -1;
            SampleShelfViewModel.Clear();
            MoveReactionAreaTestResult = null;
            AddingSampleTestResult = null;
            PushCardFailedCount = 0;

            // 重置所有状态标志为 false
            ResetAllStatusFlags();
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

        [RelayCommand]
        public void ClickStart()
        {
            if (VerifyMachineState())
            {
                GoMachineStatus();
            }
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
            else if (ReactionAreaIsFull())
            {
                Log.Information("反应区已满，请等待检测结束。");
                errorMsg = "反应区已满，请等待检测结束。";
            }
            else if (SystemGlobal.MachineStatus.IsRunningError())
            {
                Log.Information("仪器");
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

        private void GoMachineStatus()
        {
            InitState();
            GetMachineState();
        }

        [RelayCommand]
        public void Insert()
        {
     
        }

        private void RefreshAdd(int id)
        {
            Log.Information($"data 发出添加={id}");
            WeakReferenceMessenger.Default.Send(
                new EventMsg<DataChangeMsg>(new DataChangeMsg() { ID = id })
                {
                    What = EventWhat.WHAT_ADD_DATA,
                }
            );
        }

        private void RefreshChange(int id)
        {
            Log.Information($"data 发出更新={id}");
            WeakReferenceMessenger.Default.Send(
                new EventMsg<DataChangeMsg>(new DataChangeMsg() { ID = id })
                {
                    What = EventWhat.WHAT_CHANGE_DATA,
                }
            );
        }

        [RelayCommand]
        public void ShowDialog()
        {
          
        }

        /// <summary>
        /// 检查是否为正常检测类型
        /// </summary>
        /// <returns>如果是正常检测类型返回true，否则返回false</returns>
        private bool IsNormalTestType()
        {
            if (SystemGlobal.TestType != TestType.Test)
            {
                // Log.Information($"非正常检测类型，当前类型：{SystemGlobal.TestType}");
                return false;
            }
            return true;
        }

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
            Log.Information($"接收到 自检: {JsonConvert.SerializeObject(model)}");
            await CloseSelfMachineDialog();

            StartGetReactionTempTask();
            string error = JoinSelfMachineError(model.Data);
            if (string.IsNullOrEmpty(error))
            {
                SetMachineStatus(MachineStatus.SelfInspectionSuccess);
                GetMachineState();
                GlobalUtil.ShowHiltDialog(
                    "提示",
                    "自检完成",
                    "好的",
                    (d, dialog) =>
                    {
                        Log.Information("自检完成，点击好的");
                    }
                );
            }
            else
            {
                GlobalUtil.ShowHiltDialog(
                    "提示",
                    $"自检失败{error}",
                    "重新自检",
                    async (d, dialog) =>
                    {
                        await HideHintDialog(dialog);
                        Log.Information("自检失败，点击重新自检");
                        GoGetSelfMachineStatus();
                    },
                    "暂不自检",
                    (d, dialog) =>
                    {
                        Log.Information("自检失败，点击暂不自检");
                    }
                );
                SetMachineStatus(MachineStatus.SelfInspectionFailed);
            }
            GetVersion();
        }

        /// <summary>
        /// 清空待检测的检测卡列表
        /// </summary>
        private void ClearWaitTestCard()
        {
            ReactionAreaQueue.getInstance().Clear();
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
                    $"错误代码：{item}，错误信息：{configService.GetString($"error_{item}")}"
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
                (state) => {
                    //GetReactionTemp();
                },
                null,
                0,
                GetReactionTempInterval
            );
        }

        public void ReceiveMachineStatusModel(BaseResponseModel<MachineStatusModel> model)
        {
            if (IsSelfMachineGetMachineState)
            {
                //自检后获取仪器状态
                IsSelfMachineGetMachineState = false;
                ParseMachineStatusCard(model.Data);
            }
            if (!ContinueTest())
                return;
            MachineStateFinished = true;
            Log.Information(
                $"接收到 仪器状态: {JsonConvert.SerializeObject(model)} SampleCurrentPos={SampleCurrentPos} IsFirstGetMachineState={IsFirstGetMachineState} IsPushCardGetMachineState={IsPushCardGetMachineState} IsMoveSampleGetMachineState={IsMoveSampleGetMachineState}"
            );
            
            if (IsFirstGetMachineState)  // 点击开始后第一次获取仪器状态
            {
                ParseMachineStatus(model.Data);
                //如果仪器状态正常（卡仓存在，卡仓有卡，清洗液存在，样本架存在）,
                //则初始化样本架状态，并清洗取样针，准备开始检测
                IsFirstGetMachineState = false;
                if (
                    CardExist
                    && CardNum > 0
                    && CleanoutFluidExist
                    && SampleShelf.Any(x => x == true)
                )
                {
                    InitSampleShelfState();
                    IsFirstCleanoutSamplingProbe = true;
                    GoCleanoutSamplingProbe();
                }
                else
                {
                    //如果仪器状态异常，则提示
                    string msg =
                        $"仪器状态异常,{(CardExist == false ? "卡仓不存在," : "")}{(CardNum <= 0 ? "检测卡不足," : "")}{(CleanoutFluidExist == false ? "清洗液不存在," : "")}{(SampleShelf.Any(x => x == true) ? "" : "样本架不存在")}";
                    msg = msg.TrimEnd(',');
                    GlobalUtil.ShowHiltDialog(
                        "提示",
                        msg,
                        "重新获取",
                        (d, dialog) =>
                        {
                            //重新获取状态
                            GoMachineStatus();
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
            else if (IsPushCardGetMachineState)
            {
                // 推卡而获取的仪器状态
                IsPushCardGetMachineState = false;
                ParseMachineStatusCard(model.Data);
                if (CardExist && CardNum > 0)
                {
                    //有卡，推卡
                    PushCard();
                }
                else
                {
                    //没卡或没卡仓，提示
                    string msg = !CardExist
                        ? "卡仓不存在，请检测卡仓是否存在"
                        : "未检测到检测卡，请添加检测卡";
                    string confirmText = !CardExist ? "重新检查" : "已添加";
                    msg = msg.TrimEnd(',');
                    Log.Information($"推卡失败，没卡");
                    GlobalUtil.ShowHiltDialog(
                        "提示",
                        msg,
                        confirmText,
                        (d, dialog) =>
                        {
                            //重新获取状态
                            PushCardGetMachineState();
                        },
                        "结束检测",
                        (d, dialog) =>
                        {
                            //结束检测
                            Log.Information("结束检测2");
                            TestFinishedHiltMsg = "取样结束";
                            TestFinishedAction();
                        }
                    );
                }
            }
            else if (IsMoveSampleGetMachineState)
            {
                //因为要移动到下一个样本而获取的仪器状态
                IsMoveSampleGetMachineState = false;
                ParseMachineStatusCard(model.Data);
                if (CardExist && CardNum > 0 && CleanoutFluidExist)
                {
                    //有卡和清洗液，移动到下一个样本
                    MoveSampleNext();
                }
                else
                {
                    //没卡或没清洗液，提示
                    string msg =
                        (!CardExist || CardNum <= 0) ? "未检测到检测卡，请添加检测卡," : "";
                    msg += !CleanoutFluidExist ? "未检测到清洗液，请添加清洗液" : "";
                    string confirmText = "已添加";
                    msg = msg.TrimEnd(',');
                    Log.Information(msg);

                    GlobalUtil.ShowHiltDialog(
                        "提示",
                        msg,
                        confirmText,
                        async (d, dialog) =>
                        {
                            MoveSampleNextGetMachineState();
                        },
                        "结束检测",
                        (d, dialog) =>
                        {
                            //结束检测
                            Log.Information("结束检测3");
                            TestFinishedHiltMsg = "取样结束";
                            TestFinishedAction();
                        }
                    );
                }
            }
        }

        /// <summary>
        /// 隐藏提示对话框
        /// </summary>
        /// <returns></returns>
        public Task HideHintDialog(CustomDialog dialog)
        {
            return MainWindow.Instance.HideMetroDialogAsync(dialog);
        }

        /// <summary>
        /// 移动到第一个存在的样本架处
        /// </summary>
        private void MoveSampleShelfFirst()
        {
            MoveSampleShelf(SampleShelfFirstPos);
        }

        /// <summary>
        /// 初始化样本架状态
        /// </summary>
        private void InitSampleShelfState()
        {
            for (int i = 0; i < 6; i++)
            {
                if (SampleShelf[i])
                {
                    if (SampleShelfFirstPos == -1)
                    {
                        SampleShelfFirstPos = i;
                    }
                    SampleShelfLastPos = i;
                }
            }
            this.SampleShelfViewModel.UpdateShelfState(SampleShelf);
        }

        /// <summary>
        /// 移动到下一个存在的样本架处
        /// </summary>
        private void MoveSampleShelfNext()
        {
            if (!SampleShelfIsLast())
            {
                for (int i = SampleShelfPos + 1; i <= SampleShelfLastPos; i++)
                {
                    if (SampleShelf[i])
                    {
                        MoveSampleShelf(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 样本架是否最后一个
        /// </summary>
        private bool SampleShelfIsLast()
        {
            return SampleShelfPos == SampleShelfLastPos;
        }

        /// <summary>
        /// 解析仪器状态数据
        /// </summary>
        /// <param name="data"></param>
        private void ParseMachineStatus(MachineStatusModel data)
        {
            ParseMachineStatusCard(data);
            //CleanoutFluidExist = data.CleanoutFluid == "1";
            for (int i = 0; i < 6; i++)
            {
                SampleShelf[i] = data.SamleShelf[i] == 1;
            }
        }

        /// <summary>
        /// 解析卡仓状态
        /// </summary>
        /// <param name="data"></param>
        private void ParseMachineStatusCard(MachineStatusModel data)
        {
            CardExist = data.CardExist == "1";
            CleanoutFluidExist = data.CleanoutFluid == "1";
            int tempNum = 0;
            int.TryParse(data.CardNum, out tempNum);
            CardNum = tempNum;
        }

        public void ReceiveMoveSampleShelfModel(BaseResponseModel<MoveSampleShelfModel> model)
        {
            if (!ContinueTest())
                return;
            MoveSampleShelfFinished = true;
            Log.Information($"接收到 移动样本架: {JsonConvert.SerializeObject(model)}");
            if (SystemGlobal.MachineStatus == MachineStatus.SamplingFinished)
            {
                //检测结束动作，显示完成对话框
                ShowFinishedDialog();
            }
            else
            {
                //处理移动样本架数据
                InitCurrentSampleShelfState();
                MoveSampleNext();
            }
        }

        /// <summary>
        /// 初始化当前样本架状态
        /// </summary>
        private void InitCurrentSampleShelfState()
        {
            SampleCurrentPos = -1;
            TestResults.Clear();
        }

        /// <summary>
        /// 移动到下一个样本处
        /// 如果当前样本位置大于等于样本最大位置，则移动到下一个样本架
        /// </summary>
        private void MoveSampleNext()
        {
            if (SampleIsLast())
            {
                //移动到下一排
                MoveSampleShelfNext();
            }
            else
            {
                //移动到下一个
                MoveSample(++SampleCurrentPos);
            }
        }

        /// <summary>
        /// 样本是否是最后一个
        /// </summary>
        private bool SampleIsLast()
        {
            return SampleCurrentPos >= SampleMaxPos;
        }

        /// <summary>
        /// 是最后一排最后一个样本了
        /// </summary>
        /// <returns></returns>
        private bool IsLastSample()
        {
            return SampleIsLast() && SampleShelfIsLast();
        }

        private List<TestResult> TestResults = new List<TestResult>();

        public void ReceiveMoveSampleModel(BaseResponseModel<MoveSampleModel> model)
        {
            if (!ContinueTest())
                return;
            MoveSampleFinished = true;
            Log.Information(
                $"接收到 移动样本: {JsonConvert.SerializeObject(model)} SampleShelfPos={SampleShelfPos} SampleCurrentPos={SampleCurrentPos}"
            );

            // 处理移动样本数据
            if (model.Data.SampleType == MoveSampleModel.None)
            {
                InsertTestResult(null);
                SampleShelfViewModel.UpdateSampleItems(
                    SampleShelfPos,
                    SampleCurrentPos,
                    (item) =>
                    {
                        item.State = SampleState.NotExist;
                        return item;
                    }
                );
                AddingSampleTestResult = TestResults[SampleCurrentPos];
                //不存在
                if (IsLastSample())
                {
                    //已经移动到最后一个样本架，则检测结束
                    Log.Information("检测结束,没有样本");
                    GlobalUtil.ShowHiltDialog(
                        "提示",
                        "检测结束,没有样本",
                        "好的",
                        (d, dialog) => { }
                    );
                }
                else
                {
                    //移动到下一个样本
                    MoveSampleNext();
                }
            }
            else if (model.Data.SampleType == MoveSampleModel.SampleTube)
            {
                string testNum = configService.TestNumIncrement() + "";
                TestResult tr = InsertTestResult(new TestResult() { TestNum = testNum });
                AddingSampleTestResult = TestResults[SampleCurrentPos];
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
                //添加结果
                RefreshAdd(tr.Id);
                //样本管
                if (IsNeedScanBarcode())
                {
                    //需要扫描条码
                    Log.Information("需要扫描条码,去扫码");
                    ScanBarcode();
                }
                else
                {
                    //不需要扫描条码
                    Log.Information("不需要扫描条码,去取样");
                    VerifyStateSamplingPushCard(model.Data.SampleType);
                    //实时获取申请信息
                    RealTimeGetApplyTest(AddingSampleTestResult);
                }
            }
            else if (model.Data.SampleType == MoveSampleModel.SampleCup)
            {
                string testNum = configService.TestNumIncrement() + "";
                TestResult tr = InsertTestResult(new TestResult() { TestNum = testNum });
                AddingSampleTestResult = TestResults[SampleCurrentPos];
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
                //添加结果
                RefreshAdd(tr.Id);
                //样本杯
                //不需要扫描条码
                Log.Information("不需要扫描条码,去取样");
                VerifyStateSamplingPushCard(model.Data.SampleType);
                //实时获取申请信息
                RealTimeGetApplyTest(AddingSampleTestResult);
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

        /// <summary>
        /// 验证状态
        /// 取样
        /// 推卡
        /// </summary>
        /// <param name="samplingType"></param>
        private void VerifyStateSamplingPushCard(string samplingType)
        {
            if (CleanoutSamplingProbeFinished)
            {
                //取样
                Sampling(samplingType, configService.SamplingVolumn());
            }
            else
            {
                //正在清洗取样针
                Log.Information("正在清洗取样针");
                IsRestoreSampling = true;
                RestoreSamplingType = samplingType;
            }
            PushCard();
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
            if (SystemGlobal.MachineStatus.IsRunning())
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    ScanFailed();
                });
            }
        }

        private void OnScanSuccess(string barcode)
        {
            if (SystemGlobal.MachineStatus.IsRunning())
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    ScanSuccess(barcode);
                });
            }
        }

        /// <summary>
        /// 扫码成功，取样推卡
        /// </summary>
        /// <param name="barcode"></param>
        private void ScanSuccess(string barcode)
        {
            Log.Information($"收到 扫码成功:{barcode}");
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
            //取样，推卡
            VerifyStateSamplingPushCard(MoveSampleModel.SampleTube);
        }

        /// <summary>
        /// 实时获取申请信息
        /// </summary>
        /// <param name="tr"></param>
        private void RealTimeGetApplyTest(TestResult tr)
        {
            Task.Run(async () =>
            {
                bool isNeedLisGet = uploadService.GetOpenUpload() && uploadService.GetTwoWay() && uploadService.GetAutoGetApplyTest();
                bool isMatchingBarcode = uploadService.GetMatchBarcode();
                QueryResult qr = await lisService.QueryApplyTestAsync(
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
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            applyTest.Patient.InspectDate = DateTime.Now;
                            Patient patientTemp = applyTest.Patient;
                            int patientId =  applyTestService.InsertPatient(patientTemp);
                            patientTemp.Id = patientId;
                            applyTest.PatientId = patientId;
                            applyTest.ApplyTestType = ApplyTestType.TestEnd;
                            int applyTestId = applyTestService.InsertApplyTest(applyTest);
                            Log.Information($"插入了 patient={patientId} applyTestId={applyTestId}");
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
                            Log.Information(
                                $"申请信息={applyTest.Id} tr={tr.Id} {JsonConvert.SerializeObject(applyTest)} {JsonConvert.SerializeObject(tr)}"
                            );

                            //刷新结果
                            RefreshChange(tr.Id);
                        });
                    }
                    else
                    {
                        Log.Information(
                            $"没有获取到申请信息 isNeedLisGet={isNeedLisGet} isMatchingBarcode={isMatchingBarcode} tr={tr.Id} barcode={tr.Barcode} testNum={tr.TestNum}"
                        );
                    }
                }
                else
                {
                    Log.Information(
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
                Log.Information($"检测结果为空 UpdateTestResultForId id={id}");
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
            Log.Information($"收到 扫码失败 {SampleShelfPos} {SampleCurrentPos}");
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

            //不存在
            if (IsLastSample())
            {
                //已经移动到最后一个样本架，则检测结束
                Log.Information("检测结束,没有样本");
                TestFinishedHiltMsg = "没有样本";
                TestFinishedAction();
            }
            else
            {
                //移动到下一个样本
                MoveSampleNext();
            }
        }

        /// <summary>
        /// 是否需要扫码
        /// </summary>
        /// <returns></returns>
        private bool IsNeedScanBarcode()
        {
            return configService.IsScanBarcode();
        }

        bool restoreMoveSampleNextGetMachineState = false;

        /// <summary>
        /// 移动到下一个样本前先 获取仪器状态
        /// </summary>
        private void MoveSampleNextGetMachineState()
        {
            if (ReactionAreaIsFull())
            {
                Log.Information("反应区已满，暂时结束检测");
                restoreMoveSampleNextGetMachineState = true;
                //TestFinishedHiltMsg = "反应区已满";
                //TestFinishedAction();
            }
            else
            {
                Log.Information(
                    $"还不是最后一个，准备继续检测，获取状态 SampleShelfPos={SampleShelfPos} SampleCurrentPos={SampleCurrentPos} SampleMaxPos={SampleMaxPos} SampleShelfLastPos={SampleShelfLastPos}"
                );
                //获取仪器状态，移动到下一个样本
                IsMoveSampleGetMachineState = true;
                MoveSampleFinished = false;
                GetMachineState();
            }
        }

        /// <summary>
        /// 反应区是否已满
        /// </summary>
        /// <returns></returns>
        private bool ReactionAreaIsFull()
        {
            return serialPortService.DequeueCount() >= 30;
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
            SetMachineStatus(MachineStatus.SamplingFinished);
            //清洗取样针
            GoCleanoutSamplingProbe();

            //样本架复位
            MoveSampleShelfReset();
        }

        /// <summary>
        /// 样本架复位
        /// </summary>
        private void MoveSampleShelfReset()
        {
            //样本架复位
            MoveSampleShelf(-1);
        }

        public void ReceiveSamplingModel(BaseResponseModel<SamplingModel> model)
        {
            if (!ContinueTest())
                return;
            SamplingFinished = true;
            Log.Information($"接收到 取样: {JsonConvert.SerializeObject(model)}");
            ///更新样本状态
            SampleShelfViewModel.UpdateSampleItems(
                SampleShelfPos,
                SampleCurrentPos,
                (item) =>
                {
                    item.State = SampleState.SamplingCompleted;
                    return item;
                }
            );
            UpdateTestResultForId(
                AddingSampleTestResult.Id,
                (item) =>
                {
                    item.ResultState = ResultState.SamplingSuccess;
                    return item;
                }
            );
            //获取当前样本的测试结果
            //AddingSampleTestResult = TestResults[SampleCurrentPos];

            //加样
            GoAddingSample();
        }

        /// <summary>
        /// 去加样
        /// </summary>
        private void GoAddingSample()
        {
            //推卡完成 并且 取样完成 并且 清洗取样针完成 并且 不需要恢复推卡（已推卡）
            if (
                PushCardFinished
                && SamplingFinished
                && CleanoutSamplingProbeFinished
                && !IsRestorePushCard
            )
            {
                AddingSample(configService.SamplingVolumn(), "1");
            }
        }

        public void ReceiveCleanoutSamplingProbeModel(
            BaseResponseModel<CleanoutSamplingProbeModel> model
        )
        {
            if (!ContinueTest())
                return;
            CleanoutSamplingProbeFinished = true;
            Log.Information($"接收到 清洗取样针: {JsonConvert.SerializeObject(model)}");

            if (IsRestoreSampling)
            {
                //恢复取样
                IsRestoreSampling = false;
                Sampling(RestoreSamplingType, configService.SamplingVolumn());
            }
            else if (IsFirstCleanoutSamplingProbe)
            {
                //开始检测，清洗取样针后才开始检测
                IsFirstCleanoutSamplingProbe = false;
                MoveSampleShelfFirst();
            }
            else if (SystemGlobal.MachineStatus == MachineStatus.SamplingFinished)
            {
                //检测结束动作，显示完成对话框
                ShowFinishedDialog();
            }
        }

        /// <summary>
        /// 检测结束动作完成，则显示完成对话框
        /// 1、清洗取样针
        /// 2、样本架复位
        /// </summary>
        private void ShowFinishedDialog()
        {
            if (CleanoutSamplingProbeFinished && MoveSampleShelfFinished)
            {
                GlobalUtil.ShowHiltDialog(
                    "提示",
                    "" + TestFinishedHiltMsg,
                    "好的",
                    (d, dialog) => { }
                );

                if (serialPortService.DequeueCount() == 0)
                {
                    //没有待检测的检测卡，则直接结束检测
                    SetMachineStatus(MachineStatus.TestingEnd);
                    Log.Information("没有待检测的检测卡，则直接结束检测");
                }
                else
                {
                    //继续等待检测
                    SetMachineStatus(MachineStatus.Testing);
                    Log.Information("继续等待检测");
                }
            }
        }

        public void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model)
        {
            if (!ContinueTest())
                return;
            AddingSampleFinished = true;
            Log.Information($"接收到 加样: {JsonConvert.SerializeObject(model)}");
            // 处理加样数据
            //移动检测卡到反应区
            GoMoveReactionArea();

            UpdateTestResultForId(
                AddingSampleTestResult.Id,
                (t) =>
                {
                    t.ResultState = ResultState.AddSampleSuccess;
                    return t;
                }
            );
            if (IsLastSample())
            {
                //已经移动到最后一个样本架，则检测结束
                Log.Information("检测结束,取样结束。");
                TestFinishedHiltMsg = "取样结束。";
                TestFinishedAction();
            }
            else if (serialPortService.DequeueCount() == 29) {//反应区是否已经满了,29+当前这个
                Log.Information("反应区已满，暂时结束检测");
                restoreMoveSampleNextGetMachineState = true;
            }
            else
            {
                //不是最后一个样本，清洗取样针，继续取样
                GoCleanoutSamplingProbe();
                //获取状态，移动下一个
                MoveSampleNextGetMachineState();
            }
        }

        /// <summary>
        /// 清洗取样针
        /// </summary>
        private void GoCleanoutSamplingProbe()
        {
            CleanoutSamplingProbe();
        }

        /// <summary>
        /// 移动检测卡到反应区等待
        /// </summary>
        private void GoMoveReactionArea()
        {
            if (GetReactionAreaNext())
            {
                Log.Information($"检测卡移动反应区 {ReactionAreaX} {ReactionAreaY}");
                //反应区还有位置，移动到反应区
                MoveReactionArea(ReactionAreaX, ReactionAreaY);
                MoveReactionAreaTestResult = AddingSampleTestResult;
            }
            else
            {
                //反应区已满
                Log.Information("反应区已满");
            }
        }

        /// <summary>
        /// 获取反应区下一个可用位置
        /// </summary>
        private bool GetReactionAreaNext()
        {
            if (ReactionAreaIsFull())
            {
                Log.Information($"反应区已满 {serialPortService.DequeueCount()}");
                return false;
            }

            return ReactionAreaViewModel.GetReactionAreaNext(out ReactionAreaY, out ReactionAreaX);
        }

        public void ReceiveDrainageModel(BaseResponseModel<DrainageModel> model)
        {
            if (!ContinueTest())
                return;
            DrainageFinished = true;
            Log.Information($"接收到 排水完成: {JsonConvert.SerializeObject(model)}");
            // 处理排水数据
        }

        private int PushCardFailedCount = 0;
        private int PushCardFailedCountMax = 3;

        public void ReceivePushCardModel(BaseResponseModel<PushCardModel> model)
        {
            if (!ContinueTest())
                return;
            PushCardFinished = true;
            Log.Information($"接收到 推卡完成: {JsonConvert.SerializeObject(model)}");
            // 处理推卡数据
            if (IsPushCardSuccess(model.Data))
            {
                Project project = configService.GetProject(model.Data.QrCode);
                Log.Information($"检测卡项目 @{JsonConvert.SerializeObject(project)}");
                if (project == null)
                {
                    Log.Information("项目为空");
                    //项目为空，暂时走推卡失败流程
                    //重新推卡
                    PuchCardFailed();
                    //PushCardGetMachineState();
                    return;
                }
                //更新项目
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
                AddingSampleTestResult.Project = project;
                AddingSampleTestResult.ProjectId = project.Id;
                // 加样
                GoAddingSample();
            }
            else
            {
                // 推卡失败
                Log.Information("推卡失败");
                // 重新推卡
                PuchCardFailed();
                //PushCardGetMachineState();
            }
        }

        private void PuchCardFailed()
        {
            PushCardFailedCount++;

            if (PushCardFailedCount >= PushCardFailedCountMax)
            {
                //超过最大推卡失败次数
                TestFinishedHiltMsg = "检测结束。推卡错误，请检查检测卡或卡仓。";
                TestFinishedAction();
                return;
            }
            else
            {
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
            IsPushCardGetMachineState = true;
            //PushCardFinished = false;
            GetMachineState();
        }

        public void ReceiveMoveReactionAreaModel(BaseResponseModel<MoveReactionAreaModel> model)
        {
            if (!ContinueTest(isTestAction: true))
                return;
            MoveReactionAreaFinished = true;
            Log.Information(
                $"接收到 移动反应区: {JsonConvert.SerializeObject(model)} ReactionAreaY={ReactionAreaY} ReactionAreaX={ReactionAreaX} id={MoveReactionAreaTestResult?.Id}"
            );
            UpdateTestResultForId(
                MoveReactionAreaTestResult.Id,
                (t) =>
                {
                    t.ResultState = ResultState.Incubation;
                    return t;
                }
            );
            //更新反应区状态
            ReactionAreaViewModel.UpdateItem(
                ReactionAreaY,
                ReactionAreaX,
                (item) =>
                {
                    item.State = ReactionAreaItem.STATE_WAIT;
                    item.TestResult = MoveReactionAreaTestResult;
                    item.ReactionAreaY = ReactionAreaY;
                    item.ReactionAreaX = ReactionAreaX;
                    //加入等待检测队列
                    Enqueue(item);
                    Log.Information($"入队={JsonConvert.SerializeObject(item)}");
                    return item;
                }
            );

            if (IsRestorePushCard)
            {
                //因为还未移动完毕导致的推卡暂停，移动完了，恢复推卡
                IsRestorePushCard = false;
                PushCard();
                Log.Information("完成 移动反应区 结束");
            }
        }

        /// <summary>
        /// 加入等待检测队列
        /// </summary>
        /// <param name="item"></param>
        private void Enqueue(ReactionAreaItem item)
        {
            serialPortService.Enqueue(item);
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

            Log.Information(
                $"OnReactionAreaDequeue IsTesting={IsTesting} {!ContinueTest(true)} {JsonConvert.SerializeObject(item)}"
            );
            Project project = item.TestResult.Project;
            if (project == null)
            {
                Log.Information("项目为空");
                return false;
            }
            string cardType = project.ProjectType + ""; //项目类型 0：单联卡 1：双联卡
            string testType = project.TestType + ""; //测试类型 0：普通卡 1：质控卡
            TestResultId = item.TestResult.Id;
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

        public void ReceiveTestModel(BaseResponseModel<TestModel> model)
        {
            //Log.Information($"接收到 检测2: {!ContinueTest(true)} {JsonConvert.SerializeObject(model)}");

            if (!ContinueTest(true))
                return;
            TestFinished = true;

            Log.Information($"接收到 检测: {JsonConvert.SerializeObject(model)}");
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
            Point point = new Point() { Points = points, Location = model.Data.Location };
            t = t / 1000;
            c = c / 1000;
            t2 = t2 / 1000;
            c2 = c2 / 1000;
            point.T = "" + t;
            point.C = "" + c;
            point.T2 = "" + t2;
            point.C2 = "" + c2;
            point.Tc = configService.CalcTC(t, c);
            point.Tc2 = configService.CalcTC(t2, c2);
            int pointId = homeService.InsertPoint(point);
            point.Id = pointId;
            //更新检测结果
            UpdateTestResultForId(
                TestResultId,
                (item) =>
                {
                    item.C = "" + c;
                    item.T = "" + t;
                    item.C2 = "" + c2;
                    item.T2 = "" + t2;
                    item.PointId = pointId;
                    item.Point = point;
                    item.ResultState = ResultState.TestFinish;
                    item = configService.CalcTestResult(item);
                    return temp = item;
                }
            );
            //刷新结果
            RefreshChange(TestResultId);
            //单个样本检测完毕
            SingleSampleTestFinished(temp);
            ////检测完更新
            //RefreshAdd(TestResultId);
            //更新反应区状态
            ReactionAreaViewModel.UpdateItem(
                ReactionAreaTestY,
                ReactionAreaTestX,
                (item) =>
                {
                    item.State = ReactionAreaItem.STATE_END;
                    item.TestResult = temp;
                    return item;
                }
            );

            //检测完了
            if (serialPortService.DequeueCount() == 0)
            {
                if (
                    SystemGlobal.MachineStatus == MachineStatus.SamplingFinished
                    || SystemGlobal.MachineStatus == MachineStatus.Testing
                    || SystemGlobal.MachineStatus == MachineStatus.RunningError
                )
                {
                    //只有已经取样完成||运行错误，才代表真正检测结束了
                    SetMachineStatus(MachineStatus.TestingEnd);
                    Log.Information("没有待检测的检测卡，则检测完成");
                }
                else
                {
                    //可能正在取样
                    Log.Information("检测完成，但还有待取样的样本");
                }
            }
            //检测完要恢复取样
            if (restoreMoveSampleNextGetMachineState)
            {
                restoreMoveSampleNextGetMachineState = false;
                MoveSampleNextGetMachineState();
            }
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
            A4ReportUtil.AutoExecReport(temp,configService.IsAutoPrintA4Report(),false,configService.GetPrinterName());
            //打印小票
            if (configService.IsAutoPrintTicket()) {
                TicketReportUtil.Instance.PrintTicket(temp, (msg) => { }, (err) => { });
            }
        }
        /// <summary>
        /// 自动上传检测结果
        /// </summary>
        /// <param name="temp"></param>
        private void AutoUpload(TestResult temp)
        {
            //已连接并且自动上传已开启
            if (HL7Helper.Instance.IsConnected() && UploadConfig.Instance.AutoUpload) {
                Task.Run(async() =>
                {
                    Log.Information($"开始上传: {temp.Id}");
                    //上传检测结果
                    UploadResult ur =  await HL7Helper.Instance.UploadTestResultAsync(temp);
                    if (ur != null && ur.ResultType == UploadResultType.Success)
                    {
                        Log.Information($"上传检测结果成功: {ur?.TestResultId}");
                        App.Current.Dispatcher.Invoke(() =>
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
                        Log.Information($"上传检测结果失败: {ur?.TestResultId} {JsonConvert.SerializeObject(temp)}");
                    }
                });
            }
        }

        public void ReceiveReactionTempModel(BaseResponseModel<ReactionTempModel> model)
        {
            if (!ContinueTest())
                return;
            ReactionTempFinished = true;
            Log.Information($"接收到 反应区温度: {JsonConvert.SerializeObject(model)}");
            // 处理反应区温度数据

            ReactionTemp = model.Data.Temp;
        }

        public void ReceiveClearReactionAreaModel(BaseResponseModel<ClearReactionAreaModel> model)
        {
            if (!ContinueTest())
                return;
            ClearReactionAreaFinished = true;
            Log.Information($"接收到 清空反应区: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveMotorModel(BaseResponseModel<MotorModel> model)
        {
            if (!ContinueTest())
                return;
            MotorFinished = true;
            Log.Information($"接收到 电机控制: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveResetParamsModel(BaseResponseModel<ResetParamsModel> model)
        {
            if (!ContinueTest())
                return;
            ResetParamsFinished = true;
            Log.Information($"接收到 重置参数: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveUpdateModel(BaseResponseModel<UpdateModel> model)
        {
            if (!ContinueTest())
                return;
            UpdateFinished = true;
            Log.Information($"接收到 升级: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveSqueezingModel(BaseResponseModel<SqueezingModel> model)
        {
            if (!ContinueTest())
                return;
            SqueezingFinished = true;
            Log.Information($"接收到 挤压: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceivePiercedModel(BaseResponseModel<PiercedModel> model)
        {
            if (ContinueTest())
                return;
            PiercedFinished = true;
            Log.Information($"接收到 刺破: {JsonConvert.SerializeObject(model)}");
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
            Log.Information("显示自检对话框");
            showSelfController = await dialogCoordinator.ShowProgressAsync(
                this,
                "提示",
                "正在自检……"
            );
            showSelfController.SetIndeterminate();
        }

        private async Task CloseSelfMachineDialog()
        {
            Log.Information("关闭自检对话框");
            if (showSelfController != null)
            {
                await showSelfController?.CloseAsync();
            }
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

        private void GetVersion()
        {
            serialPortService.GetVersion();
        }

        string RunningErrorMsg = "";

        public void ReceiveStateError(BaseResponseModel<dynamic> model)
        {
            if (!IsNormalTestType())
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
            //if (!IsNormalTestType())
            //return;
            SystemGlobal.McuVersion = model.Data.Ver;
        }

        public void ReceiveShutdownModel(BaseResponseModel<ShutdownModel> model)
        {
            if (!IsNormalTestType())
                return;
        }
    }
}
