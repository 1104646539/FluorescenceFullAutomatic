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
    /// ��ҳ
    /// </summary>
    public partial class HomeViewModel : ObservableRecipient, IReceiveData
    {
        #region ����
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
        /// ��ȡ�¶ȼ��
        /// </summary>
        private const int GetReactionTempInterval = 1000 * 30; // 30��

        // ����ִ�� ״̬����
        /// <summary>
        /// ȡ�������Ƿ����
        /// </summary>
        private bool SamplingFinished { get; set; }

        /// <summary>
        /// �Լ������Ƿ����
        /// </summary>
        private bool SelfInspectionFinished { get; set; }

        /// <summary>
        /// ��ȡ����״̬�����Ƿ����
        /// </summary>
        private bool MachineStateFinished { get; set; }

        /// <summary>
        /// ��ȡ��ϴҺ״̬�����Ƿ����
        /// </summary>
        private bool CleanoutFluidFinished { get; set; }

        /// <summary>
        /// ��ȡ������״̬�����Ƿ����
        /// </summary>
        private bool SampleShelfFinished { get; set; }

        /// <summary>
        /// �ƶ������������Ƿ����
        /// </summary>
        private bool MoveSampleShelfFinished { get; set; }

        /// <summary>
        /// �ƶ����������Ƿ����
        /// </summary>
        private bool MoveSampleFinished { get; set; }

        /// <summary>
        /// ��ϴȡ���������Ƿ����
        /// </summary>
        private bool CleanoutSamplingProbeFinished { get; set; }

        /// <summary>
        /// ���������Ƿ����
        /// </summary>
        private bool AddingSampleFinished { get; set; }

        /// <summary>
        /// ��ˮ�����Ƿ����
        /// </summary>
        private bool DrainageFinished { get; set; }

        /// <summary>
        /// �ƿ������Ƿ����
        /// </summary>
        private bool PushCardFinished { get; set; }

        /// <summary>
        /// �ƶ�����Ӧ�������Ƿ����
        /// </summary>
        private bool MoveReactionAreaFinished { get; set; }

        /// <summary>
        /// ��������Ƿ����
        /// </summary>
        private bool TestFinished { get; set; }

        /// <summary>
        /// ��ȡ/���÷�Ӧ���¶������Ƿ����
        /// </summary>
        private bool ReactionTempFinished { get; set; }

        /// <summary>
        /// ��շ�Ӧ�������Ƿ����
        /// </summary>
        private bool ClearReactionAreaFinished { get; set; }

        /// <summary>
        /// ������������Ƿ����
        /// </summary>
        private bool MotorFinished { get; set; }

        /// <summary>
        /// ���ز��������Ƿ����
        /// </summary>
        private bool ResetParamsFinished { get; set; }

        /// <summary>
        /// ���������Ƿ����
        /// </summary>
        private bool UpdateFinished { get; set; }

        /// <summary>
        /// ��ѹ�����Ƿ����
        /// </summary>
        private bool SqueezingFinished { get; set; }

        /// <summary>
        /// ���������Ƿ����
        /// </summary>
        private bool PiercedFinished { get; set; }

        /// <summary>
        /// �Ƿ�ʼ���һ�λ�ȡ����״̬
        /// </summary>
        private bool IsFirstGetMachineState = false;

        /// <summary>
        /// ��һ�μ���
        /// </summary>

        /// <summary>
        /// �����Ƿ����
        /// </summary>
        private bool CardExist = false;

        /// <summary>
        /// ��������
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private int cardNum;

        /// <summary>
        /// ��ϴҺ�Ƿ����
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private bool cleanoutFluidExist;

        /// <summary>
        /// ��Ӧ���¶�
        /// </summary>
        [ObservableProperty]
        private string reactionTemp;

        [ObservableProperty]
        private string stateMsg;

        /// <summary>
        /// �������Ƿ����
        /// </summary>
        private bool[] SampleShelf = new bool[6];

        /// <summary>
        /// �����ܵ�һ��λ��
        /// </summary>
        private int SampleShelfFirstPos = -1;

        /// <summary>
        /// ���������һ��λ��
        /// </summary>
        private int SampleShelfLastPos = -1;

        /// <summary>
        /// �����ܵ�ǰλ��
        /// </summary>
        private int SampleShelfPos = -1;

        /// <summary>
        /// ��ǰ����λ��
        /// </summary>
        private int SampleCurrentPos = 0;

        /// <summary>
        /// �������λ��
        /// </summary>
        private const int SampleMaxPos = 4;

        ///
        /// �Ƿ�ָ�ȡ��
        /// </summary>
        private bool IsRestoreSampling = false;

        /// <summary>
        /// �ָ�ȡ����ȡ������
        /// </summary>
        private string RestoreSamplingType = "";

        /// <summary>
        /// �Ƿ��ǵ�һ�μ���
        /// </summary>
        private bool FirstLoad = true;

        /// <summary>
        /// �Ƿ��Ǽ��ǰ��Ҫ�Լ��
        /// </summary>
        private bool IsTestGetSelfMachineState = true;

        /// <summary>
        /// ��һ����ϴȡ����
        /// </summary>
        private bool IsFirstCleanoutSamplingProbe = true;

        /// <summary>
        /// ������Ҫ��ʾ����ʾ
        /// </summary>
        string TestFinishedHiltMsg = "";
        /// <summary>
        /// �Ƿ���Ϊ�Լ��������ȡ����״̬
        /// </summary>
        private bool IsSelfMachineGetMachineState = false;
        /// <summary>
        /// �Ƿ���Ϊ�ƶ���������ȡ����״̬
        /// </summary>
        private bool IsMoveSampleGetMachineState = false;

        /// <summary>
        /// ��ǰ����Ҫ���õ� ��Ӧ��X����
        /// </summary>
        private int ReactionAreaX = -1;

        /// <summary>
        /// ��ǰ����Ҫ���õ� ��Ӧ��Y����
        /// </summary>
        private int ReactionAreaY = 0;

        /// <summary>
        /// ��ǰ�����Ľ��
        /// </summary>
        private TestResult AddingSampleTestResult;

        /// <summary>
        /// ��ǰ�����ƶ���Ӧ���Ľ��
        /// </summary>
        private TestResult MoveReactionAreaTestResult;

        /// <summary>
        /// ��Ϊ�ƿ���ȡ����״̬
        /// </summary>
        private bool IsPushCardGetMachineState = false;

        /// <summary>
        /// �Ƿ�ָ��ƿ�
        /// </summary>
        private bool IsRestorePushCard = false;

        /// <summary>
        /// �Ƿ����ڼ��
        /// </summary>
        private bool IsTesting = false;

        /// <summary>
        /// ���ڼ��ļ����ID
        /// </summary>
        private int TestResultId = -1;

        /// <summary>
        /// ���ʱ��⿨�������� X
        /// </summary>
        private int ReactionAreaTestX = 0;

        /// <summary>
        /// ���ʱ��⿨�������� Y
        /// </summary>
        private int ReactionAreaTestY = 0;

        /// <summary>
        /// ��ȡ��Ӧ���¶ȶ�ʱ��
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

            // �Ƴ�HL7���ӻص�����
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
                // ������Ϣ
                tr.Patient = new Patient
                {
                    PatientName = "����",
                    PatientGender = "��", 
                    PatientAge = "35",
                    InspectDate = DateTime.Now.AddDays(1),
                    InspectDepartment = "�ڿ�",
                    InspectDoctor = "��ҽ��",
                    TestDoctor = "��ҽ��",
                    CheckDoctor = "��ҽ��"
                };
                
                // ��Ŀ��Ϣ
                tr.Project = new Project
                {
                    ProjectCode = "FOB",
                    ProjectName = "Ѫ�쵰��",
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
                
                // �����Ϣ
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
                
                // ���û��������Ϣ
                tr.Barcode = "BarCode001";
                tr.CardQRCode = "QR100200300";
                tr.TestNum = "10000";
                tr.FrameNum = "1";
                tr.TestTime = DateTime.Now.AddMonths(5);
                
                // ���ü����
                tr.T = "10.5";
                tr.C = "5.2";
                tr.Tc = "2";
                tr.Con = "10.5";
                tr.Result = "����";
                tr.T2 = "8.7";
                tr.C2 = "4.3";
                tr.Tc2 = "1.5";
                tr.Con2 = "8.7";
                tr.Result2 = "����";
                tr.TestVerdict = "�ϸ�";

                // �ϴ����
                Hl7Result.UploadResult ur = await HL7Helper.Instance.UploadTestResultAsync(tr);
                Log.Information($"�ϴ����={ur.ResultType}");
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
                    $"��ѯ���={qr.ResultType} {JsonConvert.SerializeObject(qr.ApplyTests)}"
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
                if (d.DriveType == DriveType.Removable) // ����Ƿ�Ϊ���ƶ�����������U��
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
            Log.Information("�Լ�ʧ�ܣ���������Լ� GoGetSelfMachineStatus");
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
            // ReactionAreaX = -1;���������ܳ�ʼ������Ϊ�������ڼ��
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

            // ��������״̬��־Ϊ false
            ResetAllStatusFlags();
        }

        /// <summary>
        /// ������������״̬��־ false
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
            // TestFinished = false;//����������ã���Ϊ�������ڼ��
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
        /// ��֤����״̬�Ƿ���Կ�ʼ���
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
                Log.Information("���ڼ�⣬��ȴ���������");
                errorMsg = "���ڼ�⣬��ȴ���������";
            }
            else if (SystemGlobal.MachineStatus == MachineStatus.SelfInspectionFailed)
            {
                Log.Information("�Լ�ʧ�ܣ������Լ졣");
                errorMsg = "�Լ�ʧ�ܣ������Լ졣";
            }
            else if (SystemGlobal.MachineStatus == MachineStatus.None)
            {
                Log.Information("����δ�Լ죬�����Լ졣");
                errorMsg = "����δ�Լ죬�����Լ졣";
            }
            else if (ReactionAreaIsFull())
            {
                Log.Information("��Ӧ����������ȴ���������");
                errorMsg = "��Ӧ����������ȴ���������";
            }
            else if (SystemGlobal.MachineStatus.IsRunningError())
            {
                Log.Information("����");
                errorMsg = RunningErrorMsg;
            }
            if (!string.IsNullOrEmpty(errorMsg))
            {
                Log.Information("����״̬�쳣����������״̬��");
                GlobalUtil.ShowHiltDialog(
                    "��ʾ",
                    errorMsg,
                    "�õ�",
                    (d, dialog) =>
                    {
                        Log.Information("����״̬�쳣����������״̬��");
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
            Log.Information($"data �������={id}");
            WeakReferenceMessenger.Default.Send(
                new EventMsg<DataChangeMsg>(new DataChangeMsg() { ID = id })
                {
                    What = EventWhat.WHAT_ADD_DATA,
                }
            );
        }

        private void RefreshChange(int id)
        {
            Log.Information($"data ��������={id}");
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
        /// ����Ƿ�Ϊ�����������
        /// </summary>
        /// <returns>���������������ͷ���true�����򷵻�false</returns>
        private bool IsNormalTestType()
        {
            if (SystemGlobal.TestType != TestType.Test)
            {
                // Log.Information($"������������ͣ���ǰ���ͣ�{SystemGlobal.TestType}");
                return false;
            }
            return true;
        }

        public async void ReceiveGetSelfMachineStatusModel(BaseResponseModel<List<string>> model)
        {
            //������Լ���ȡ���Ŵ������򲻴���
            if (!IsTestGetSelfMachineState)
            {
                return;
            }
            ClearReactionAreaState();
            ClearWaitTestCard();
            IsTestGetSelfMachineState = false;
            SelfInspectionFinished = true;
            Log.Information($"���յ� �Լ�: {JsonConvert.SerializeObject(model)}");
            await CloseSelfMachineDialog();

            StartGetReactionTempTask();
            string error = JoinSelfMachineError(model.Data);
            if (string.IsNullOrEmpty(error))
            {
                SetMachineStatus(MachineStatus.SelfInspectionSuccess);
                GetMachineState();
                GlobalUtil.ShowHiltDialog(
                    "��ʾ",
                    "�Լ����",
                    "�õ�",
                    (d, dialog) =>
                    {
                        Log.Information("�Լ���ɣ�����õ�");
                    }
                );
            }
            else
            {
                GlobalUtil.ShowHiltDialog(
                    "��ʾ",
                    $"�Լ�ʧ��{error}",
                    "�����Լ�",
                    async (d, dialog) =>
                    {
                        await HideHintDialog(dialog);
                        Log.Information("�Լ�ʧ�ܣ���������Լ�");
                        GoGetSelfMachineStatus();
                    },
                    "�ݲ��Լ�",
                    (d, dialog) =>
                    {
                        Log.Information("�Լ�ʧ�ܣ�����ݲ��Լ�");
                    }
                );
                SetMachineStatus(MachineStatus.SelfInspectionFailed);
            }
            GetVersion();
        }

        /// <summary>
        /// ��մ����ļ�⿨�б�
        /// </summary>
        private void ClearWaitTestCard()
        {
            ReactionAreaQueue.getInstance().Clear();
        }

        /// <summary>
        /// ��շ�Ӧ��״̬
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
            
            // ������ת��Ϊʵ�ʴ�����Ϣ������ʽ�����
            var errorMessages = data.Select(item =>
                    $"������룺{item}��������Ϣ��{configService.GetString($"error_{item}")}"
                )
                .ToList();
            return "\n" + string.Join("\n", errorMessages);
        }

        /// <summary>
        /// ��ʼ��ȡ��Ӧ���¶�����
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
                //�Լ���ȡ����״̬
                IsSelfMachineGetMachineState = false;
                ParseMachineStatusCard(model.Data);
            }
            if (!ContinueTest())
                return;
            MachineStateFinished = true;
            Log.Information(
                $"���յ� ����״̬: {JsonConvert.SerializeObject(model)} SampleCurrentPos={SampleCurrentPos} IsFirstGetMachineState={IsFirstGetMachineState} IsPushCardGetMachineState={IsPushCardGetMachineState} IsMoveSampleGetMachineState={IsMoveSampleGetMachineState}"
            );
            
            if (IsFirstGetMachineState)  // �����ʼ���һ�λ�ȡ����״̬
            {
                ParseMachineStatus(model.Data);
                //�������״̬���������ִ��ڣ������п�����ϴҺ���ڣ������ܴ��ڣ�,
                //���ʼ��������״̬������ϴȡ���룬׼����ʼ���
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
                    //�������״̬�쳣������ʾ
                    string msg =
                        $"����״̬�쳣,{(CardExist == false ? "���ֲ�����," : "")}{(CardNum <= 0 ? "��⿨����," : "")}{(CleanoutFluidExist == false ? "��ϴҺ������," : "")}{(SampleShelf.Any(x => x == true) ? "" : "�����ܲ�����")}";
                    msg = msg.TrimEnd(',');
                    GlobalUtil.ShowHiltDialog(
                        "��ʾ",
                        msg,
                        "���»�ȡ",
                        (d, dialog) =>
                        {
                            //���»�ȡ״̬
                            GoMachineStatus();
                        },
                        "�������",
                        (d, dialog) =>
                        {
                            //�������
                            Log.Information("�������1");
                            TestFinishedHiltMsg = "������," + msg;
                            TestFinishedAction();
                        }
                    );
                }
            }
            else if (IsPushCardGetMachineState)
            {
                // �ƿ�����ȡ������״̬
                IsPushCardGetMachineState = false;
                ParseMachineStatusCard(model.Data);
                if (CardExist && CardNum > 0)
                {
                    //�п����ƿ�
                    PushCard();
                }
                else
                {
                    //û����û���֣���ʾ
                    string msg = !CardExist
                        ? "���ֲ����ڣ����⿨���Ƿ����"
                        : "δ��⵽��⿨������Ӽ�⿨";
                    string confirmText = !CardExist ? "���¼��" : "�����";
                    msg = msg.TrimEnd(',');
                    Log.Information($"�ƿ�ʧ�ܣ�û��");
                    GlobalUtil.ShowHiltDialog(
                        "��ʾ",
                        msg,
                        confirmText,
                        (d, dialog) =>
                        {
                            //���»�ȡ״̬
                            PushCardGetMachineState();
                        },
                        "�������",
                        (d, dialog) =>
                        {
                            //�������
                            Log.Information("�������2");
                            TestFinishedHiltMsg = "ȡ������";
                            TestFinishedAction();
                        }
                    );
                }
            }
            else if (IsMoveSampleGetMachineState)
            {
                //��ΪҪ�ƶ�����һ����������ȡ������״̬
                IsMoveSampleGetMachineState = false;
                ParseMachineStatusCard(model.Data);
                if (CardExist && CardNum > 0 && CleanoutFluidExist)
                {
                    //�п�����ϴҺ���ƶ�����һ������
                    MoveSampleNext();
                }
                else
                {
                    //û����û��ϴҺ����ʾ
                    string msg =
                        (!CardExist || CardNum <= 0) ? "δ��⵽��⿨������Ӽ�⿨," : "";
                    msg += !CleanoutFluidExist ? "δ��⵽��ϴҺ���������ϴҺ" : "";
                    string confirmText = "�����";
                    msg = msg.TrimEnd(',');
                    Log.Information(msg);

                    GlobalUtil.ShowHiltDialog(
                        "��ʾ",
                        msg,
                        confirmText,
                        async (d, dialog) =>
                        {
                            MoveSampleNextGetMachineState();
                        },
                        "�������",
                        (d, dialog) =>
                        {
                            //�������
                            Log.Information("�������3");
                            TestFinishedHiltMsg = "ȡ������";
                            TestFinishedAction();
                        }
                    );
                }
            }
        }

        /// <summary>
        /// ������ʾ�Ի���
        /// </summary>
        /// <returns></returns>
        public Task HideHintDialog(CustomDialog dialog)
        {
            return MainWindow.Instance.HideMetroDialogAsync(dialog);
        }

        /// <summary>
        /// �ƶ�����һ�����ڵ������ܴ�
        /// </summary>
        private void MoveSampleShelfFirst()
        {
            MoveSampleShelf(SampleShelfFirstPos);
        }

        /// <summary>
        /// ��ʼ��������״̬
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
        /// �ƶ�����һ�����ڵ������ܴ�
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
        /// �������Ƿ����һ��
        /// </summary>
        private bool SampleShelfIsLast()
        {
            return SampleShelfPos == SampleShelfLastPos;
        }

        /// <summary>
        /// ��������״̬����
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
        /// ��������״̬
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
            Log.Information($"���յ� �ƶ�������: {JsonConvert.SerializeObject(model)}");
            if (SystemGlobal.MachineStatus == MachineStatus.SamplingFinished)
            {
                //��������������ʾ��ɶԻ���
                ShowFinishedDialog();
            }
            else
            {
                //�����ƶ�����������
                InitCurrentSampleShelfState();
                MoveSampleNext();
            }
        }

        /// <summary>
        /// ��ʼ����ǰ������״̬
        /// </summary>
        private void InitCurrentSampleShelfState()
        {
            SampleCurrentPos = -1;
            TestResults.Clear();
        }

        /// <summary>
        /// �ƶ�����һ��������
        /// �����ǰ����λ�ô��ڵ����������λ�ã����ƶ�����һ��������
        /// </summary>
        private void MoveSampleNext()
        {
            if (SampleIsLast())
            {
                //�ƶ�����һ��
                MoveSampleShelfNext();
            }
            else
            {
                //�ƶ�����һ��
                MoveSample(++SampleCurrentPos);
            }
        }

        /// <summary>
        /// �����Ƿ������һ��
        /// </summary>
        private bool SampleIsLast()
        {
            return SampleCurrentPos >= SampleMaxPos;
        }

        /// <summary>
        /// �����һ�����һ��������
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
                $"���յ� �ƶ�����: {JsonConvert.SerializeObject(model)} SampleShelfPos={SampleShelfPos} SampleCurrentPos={SampleCurrentPos}"
            );

            // �����ƶ���������
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
                //������
                if (IsLastSample())
                {
                    //�Ѿ��ƶ������һ�������ܣ��������
                    Log.Information("������,û������");
                    GlobalUtil.ShowHiltDialog(
                        "��ʾ",
                        "������,û������",
                        "�õ�",
                        (d, dialog) => { }
                    );
                }
                else
                {
                    //�ƶ�����һ������
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
                //��ӽ��
                RefreshAdd(tr.Id);
                //������
                if (IsNeedScanBarcode())
                {
                    //��Ҫɨ������
                    Log.Information("��Ҫɨ������,ȥɨ��");
                    ScanBarcode();
                }
                else
                {
                    //����Ҫɨ������
                    Log.Information("����Ҫɨ������,ȥȡ��");
                    VerifyStateSamplingPushCard(model.Data.SampleType);
                    //ʵʱ��ȡ������Ϣ
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
                //��ӽ��
                RefreshAdd(tr.Id);
                //������
                //����Ҫɨ������
                Log.Information("����Ҫɨ������,ȥȡ��");
                VerifyStateSamplingPushCard(model.Data.SampleType);
                //ʵʱ��ȡ������Ϣ
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
        /// ��֤״̬
        /// ȡ��
        /// �ƿ�
        /// </summary>
        /// <param name="samplingType"></param>
        private void VerifyStateSamplingPushCard(string samplingType)
        {
            if (CleanoutSamplingProbeFinished)
            {
                //ȡ��
                Sampling(samplingType, configService.SamplingVolumn());
            }
            else
            {
                //������ϴȡ����
                Log.Information("������ϴȡ����");
                IsRestoreSampling = true;
                RestoreSamplingType = samplingType;
            }
            PushCard();
        }

        /// <summary>
        /// ȥɨ��
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
        /// ɨ��ɹ���ȡ���ƿ�
        /// </summary>
        /// <param name="barcode"></param>
        private void ScanSuccess(string barcode)
        {
            Log.Information($"�յ� ɨ��ɹ�:{barcode}");
            //��¼����
            SampleShelfViewModel.UpdateSampleItems(
                SampleShelfPos,
                SampleCurrentPos,
                (item) =>
                {
                    item.State = SampleState.ScanSuccess;
                    return item;
                }
            );
            //���½��
            UpdateTestResultForSamplePos(
                SampleCurrentPos,
                (item) =>
                {
                    item.ResultState = ResultState.ScanSuccess;
                    item.Barcode = barcode;
                    return item;
                }
            );
            //��ȡ��Ϣ
            TestResult tr = TestResults[SampleCurrentPos];

            //ʵʱ��ȡ������Ϣ����������
            RealTimeGetApplyTest(tr);

            TestResults[SampleCurrentPos].Barcode = barcode;
            //ȡ�����ƿ�
            VerifyStateSamplingPushCard(MoveSampleModel.SampleTube);
        }

        /// <summary>
        /// ʵʱ��ȡ������Ϣ
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
                    //���յ�һ�����ݣ����¼����
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
                            Log.Information($"������ patient={patientId} applyTestId={applyTestId}");
                            UpdateTestResultForId(
                                tr.Id,
                                (item) =>
                                {
                                    item.Patient = patientTemp;
                                    item.PatientId = patientTemp.Id;
                                    return item;
                                }
                            );
                            //���������ܵļ����
                            for (int i = 0; i < TestResults.Count; i++)
                            {
                                if (TestResults[i] != null && TestResults[i].Id == tr.Id)
                                {
                                    TestResults[i].Patient = patientTemp;
                                    TestResults[i].PatientId = patientTemp.Id;
                                    break;
                                }
                            }
                            //ˢ��������Ϣ
                            RefreshApplyTest(applyTest);
                            Log.Information(
                                $"������Ϣ={applyTest.Id} tr={tr.Id} {JsonConvert.SerializeObject(applyTest)} {JsonConvert.SerializeObject(tr)}"
                            );

                            //ˢ�½��
                            RefreshChange(tr.Id);
                        });
                    }
                    else
                    {
                        Log.Information(
                            $"û�л�ȡ��������Ϣ isNeedLisGet={isNeedLisGet} isMatchingBarcode={isMatchingBarcode} tr={tr.Id} barcode={tr.Barcode} testNum={tr.TestNum}"
                        );
                    }
                }
                else
                {
                    Log.Information(
                        $"û�л�ȡ��������Ϣ isNeedLisGet={isNeedLisGet} isMatchingBarcode={isMatchingBarcode} tr={tr.Id} barcode={tr.Barcode} testNum={tr.TestNum}"
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
        /// ���¶�ӦID�ļ����
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
                Log.Information($"�����Ϊ�� UpdateTestResultForId id={id}");
                return;
            }

            homeService.UpdateTestResult(func(testResult));
        
        }


        /// <summary>
        /// ���������������ڵļ����
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
        /// ɨ��ʧ�ܣ��ƶ�����һ������
        /// </summary>
        private void ScanFailed()
        {
            Log.Information($"�յ� ɨ��ʧ�� {SampleShelfPos} {SampleCurrentPos}");
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

            //������
            if (IsLastSample())
            {
                //�Ѿ��ƶ������һ�������ܣ��������
                Log.Information("������,û������");
                TestFinishedHiltMsg = "û������";
                TestFinishedAction();
            }
            else
            {
                //�ƶ�����һ������
                MoveSampleNext();
            }
        }

        /// <summary>
        /// �Ƿ���Ҫɨ��
        /// </summary>
        /// <returns></returns>
        private bool IsNeedScanBarcode()
        {
            return configService.IsScanBarcode();
        }

        bool restoreMoveSampleNextGetMachineState = false;

        /// <summary>
        /// �ƶ�����һ������ǰ�� ��ȡ����״̬
        /// </summary>
        private void MoveSampleNextGetMachineState()
        {
            if (ReactionAreaIsFull())
            {
                Log.Information("��Ӧ����������ʱ�������");
                restoreMoveSampleNextGetMachineState = true;
                //TestFinishedHiltMsg = "��Ӧ������";
                //TestFinishedAction();
            }
            else
            {
                Log.Information(
                    $"���������һ����׼��������⣬��ȡ״̬ SampleShelfPos={SampleShelfPos} SampleCurrentPos={SampleCurrentPos} SampleMaxPos={SampleMaxPos} SampleShelfLastPos={SampleShelfLastPos}"
                );
                //��ȡ����״̬���ƶ�����һ������
                IsMoveSampleGetMachineState = true;
                MoveSampleFinished = false;
                GetMachineState();
            }
        }

        /// <summary>
        /// ��Ӧ���Ƿ�����
        /// </summary>
        /// <returns></returns>
        private bool ReactionAreaIsFull()
        {
            return serialPortService.DequeueCount() >= 30;
        }

        /// <summary>
        /// ������������ʼ
        /// </summary>
        private void TestFinishedAction()
        {
            if (SystemGlobal.MachineStatus == MachineStatus.SamplingFinished)
            {
                return;
            }
            SetMachineStatus(MachineStatus.SamplingFinished);
            //��ϴȡ����
            GoCleanoutSamplingProbe();

            //�����ܸ�λ
            MoveSampleShelfReset();
        }

        /// <summary>
        /// �����ܸ�λ
        /// </summary>
        private void MoveSampleShelfReset()
        {
            //�����ܸ�λ
            MoveSampleShelf(-1);
        }

        public void ReceiveSamplingModel(BaseResponseModel<SamplingModel> model)
        {
            if (!ContinueTest())
                return;
            SamplingFinished = true;
            Log.Information($"���յ� ȡ��: {JsonConvert.SerializeObject(model)}");
            ///��������״̬
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
            //��ȡ��ǰ�����Ĳ��Խ��
            //AddingSampleTestResult = TestResults[SampleCurrentPos];

            //����
            GoAddingSample();
        }

        /// <summary>
        /// ȥ����
        /// </summary>
        private void GoAddingSample()
        {
            //�ƿ���� ���� ȡ����� ���� ��ϴȡ������� ���� ����Ҫ�ָ��ƿ������ƿ���
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
            Log.Information($"���յ� ��ϴȡ����: {JsonConvert.SerializeObject(model)}");

            if (IsRestoreSampling)
            {
                //�ָ�ȡ��
                IsRestoreSampling = false;
                Sampling(RestoreSamplingType, configService.SamplingVolumn());
            }
            else if (IsFirstCleanoutSamplingProbe)
            {
                //��ʼ��⣬��ϴȡ�����ſ�ʼ���
                IsFirstCleanoutSamplingProbe = false;
                MoveSampleShelfFirst();
            }
            else if (SystemGlobal.MachineStatus == MachineStatus.SamplingFinished)
            {
                //��������������ʾ��ɶԻ���
                ShowFinishedDialog();
            }
        }

        /// <summary>
        /// ������������ɣ�����ʾ��ɶԻ���
        /// 1����ϴȡ����
        /// 2�������ܸ�λ
        /// </summary>
        private void ShowFinishedDialog()
        {
            if (CleanoutSamplingProbeFinished && MoveSampleShelfFinished)
            {
                GlobalUtil.ShowHiltDialog(
                    "��ʾ",
                    "" + TestFinishedHiltMsg,
                    "�õ�",
                    (d, dialog) => { }
                );

                if (serialPortService.DequeueCount() == 0)
                {
                    //û�д����ļ�⿨����ֱ�ӽ������
                    SetMachineStatus(MachineStatus.TestingEnd);
                    Log.Information("û�д����ļ�⿨����ֱ�ӽ������");
                }
                else
                {
                    //�����ȴ����
                    SetMachineStatus(MachineStatus.Testing);
                    Log.Information("�����ȴ����");
                }
            }
        }

        public void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model)
        {
            if (!ContinueTest())
                return;
            AddingSampleFinished = true;
            Log.Information($"���յ� ����: {JsonConvert.SerializeObject(model)}");
            // �����������
            //�ƶ���⿨����Ӧ��
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
                //�Ѿ��ƶ������һ�������ܣ��������
                Log.Information("������,ȡ��������");
                TestFinishedHiltMsg = "ȡ��������";
                TestFinishedAction();
            }
            else if (serialPortService.DequeueCount() == 29) {//��Ӧ���Ƿ��Ѿ�����,29+��ǰ���
                Log.Information("��Ӧ����������ʱ�������");
                restoreMoveSampleNextGetMachineState = true;
            }
            else
            {
                //�������һ����������ϴȡ���룬����ȡ��
                GoCleanoutSamplingProbe();
                //��ȡ״̬���ƶ���һ��
                MoveSampleNextGetMachineState();
            }
        }

        /// <summary>
        /// ��ϴȡ����
        /// </summary>
        private void GoCleanoutSamplingProbe()
        {
            CleanoutSamplingProbe();
        }

        /// <summary>
        /// �ƶ���⿨����Ӧ���ȴ�
        /// </summary>
        private void GoMoveReactionArea()
        {
            if (GetReactionAreaNext())
            {
                Log.Information($"��⿨�ƶ���Ӧ�� {ReactionAreaX} {ReactionAreaY}");
                //��Ӧ������λ�ã��ƶ�����Ӧ��
                MoveReactionArea(ReactionAreaX, ReactionAreaY);
                MoveReactionAreaTestResult = AddingSampleTestResult;
            }
            else
            {
                //��Ӧ������
                Log.Information("��Ӧ������");
            }
        }

        /// <summary>
        /// ��ȡ��Ӧ����һ������λ��
        /// </summary>
        private bool GetReactionAreaNext()
        {
            if (ReactionAreaIsFull())
            {
                Log.Information($"��Ӧ������ {serialPortService.DequeueCount()}");
                return false;
            }

            return ReactionAreaViewModel.GetReactionAreaNext(out ReactionAreaY, out ReactionAreaX);
        }

        public void ReceiveDrainageModel(BaseResponseModel<DrainageModel> model)
        {
            if (!ContinueTest())
                return;
            DrainageFinished = true;
            Log.Information($"���յ� ��ˮ���: {JsonConvert.SerializeObject(model)}");
            // ������ˮ����
        }

        private int PushCardFailedCount = 0;
        private int PushCardFailedCountMax = 3;

        public void ReceivePushCardModel(BaseResponseModel<PushCardModel> model)
        {
            if (!ContinueTest())
                return;
            PushCardFinished = true;
            Log.Information($"���յ� �ƿ����: {JsonConvert.SerializeObject(model)}");
            // �����ƿ�����
            if (IsPushCardSuccess(model.Data))
            {
                Project project = configService.GetProject(model.Data.QrCode);
                Log.Information($"��⿨��Ŀ @{JsonConvert.SerializeObject(project)}");
                if (project == null)
                {
                    Log.Information("��ĿΪ��");
                    //��ĿΪ�գ���ʱ���ƿ�ʧ������
                    //�����ƿ�
                    PuchCardFailed();
                    //PushCardGetMachineState();
                    return;
                }
                //������Ŀ
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
                // ����
                GoAddingSample();
            }
            else
            {
                // �ƿ�ʧ��
                Log.Information("�ƿ�ʧ��");
                // �����ƿ�
                PuchCardFailed();
                //PushCardGetMachineState();
            }
        }

        private void PuchCardFailed()
        {
            PushCardFailedCount++;

            if (PushCardFailedCount >= PushCardFailedCountMax)
            {
                //��������ƿ�ʧ�ܴ���
                TestFinishedHiltMsg = "���������ƿ����������⿨�򿨲֡�";
                TestFinishedAction();
                return;
            }
            else
            {
                PushCardGetMachineState();
            }
        }

        /// <summary>
        /// �Ƿ��ƿ��ɹ�
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool IsPushCardSuccess(PushCardModel data)
        {
            return data.Success == PushCardModel.PushCardSuccess
                && !string.IsNullOrEmpty(data.QrCode);
        }

        /// <summary>
        /// ��Ϊ�ƿ���ȡ����״̬
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
                $"���յ� �ƶ���Ӧ��: {JsonConvert.SerializeObject(model)} ReactionAreaY={ReactionAreaY} ReactionAreaX={ReactionAreaX} id={MoveReactionAreaTestResult?.Id}"
            );
            UpdateTestResultForId(
                MoveReactionAreaTestResult.Id,
                (t) =>
                {
                    t.ResultState = ResultState.Incubation;
                    return t;
                }
            );
            //���·�Ӧ��״̬
            ReactionAreaViewModel.UpdateItem(
                ReactionAreaY,
                ReactionAreaX,
                (item) =>
                {
                    item.State = ReactionAreaItem.STATE_WAIT;
                    item.TestResult = MoveReactionAreaTestResult;
                    item.ReactionAreaY = ReactionAreaY;
                    item.ReactionAreaX = ReactionAreaX;
                    //����ȴ�������
                    Enqueue(item);
                    Log.Information($"���={JsonConvert.SerializeObject(item)}");
                    return item;
                }
            );

            if (IsRestorePushCard)
            {
                //��Ϊ��δ�ƶ���ϵ��µ��ƿ���ͣ���ƶ����ˣ��ָ��ƿ�
                IsRestorePushCard = false;
                PushCard();
                Log.Information("��� �ƶ���Ӧ�� ����");
            }
        }

        /// <summary>
        /// ����ȴ�������
        /// </summary>
        /// <param name="item"></param>
        private void Enqueue(ReactionAreaItem item)
        {
            serialPortService.Enqueue(item);
        }

        /// <summary>
        /// ��⿨���ӣ��Ѿ������ʱ��
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool OnReactionAreaDequeue(ReactionAreaItem item)
        {
            if (IsTesting)
            {
                //���ڼ��
                return false;
            }
            //������ģ�����д������ܼ��
            if (!ContinueTest(true))
                return false;

            Log.Information(
                $"OnReactionAreaDequeue IsTesting={IsTesting} {!ContinueTest(true)} {JsonConvert.SerializeObject(item)}"
            );
            Project project = item.TestResult.Project;
            if (project == null)
            {
                Log.Information("��ĿΪ��");
                return false;
            }
            string cardType = project.ProjectType + ""; //��Ŀ���� 0�������� 1��˫����
            string testType = project.TestType + ""; //�������� 0����ͨ�� 1���ʿؿ�
            TestResultId = item.TestResult.Id;
            //��¼��⿨��������
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
            //Log.Information($"���յ� ���2: {!ContinueTest(true)} {JsonConvert.SerializeObject(model)}");

            if (!ContinueTest(true))
                return;
            TestFinished = true;

            Log.Information($"���յ� ���: {JsonConvert.SerializeObject(model)}");
            // ����������

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
            //���¼����
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
            //ˢ�½��
            RefreshChange(TestResultId);
            //��������������
            SingleSampleTestFinished(temp);
            ////��������
            //RefreshAdd(TestResultId);
            //���·�Ӧ��״̬
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

            //�������
            if (serialPortService.DequeueCount() == 0)
            {
                if (
                    SystemGlobal.MachineStatus == MachineStatus.SamplingFinished
                    || SystemGlobal.MachineStatus == MachineStatus.Testing
                    || SystemGlobal.MachineStatus == MachineStatus.RunningError
                )
                {
                    //ֻ���Ѿ�ȡ�����||���д��󣬲Ŵ���������������
                    SetMachineStatus(MachineStatus.TestingEnd);
                    Log.Information("û�д����ļ�⿨���������");
                }
                else
                {
                    //��������ȡ��
                    Log.Information("�����ɣ������д�ȡ��������");
                }
            }
            //�����Ҫ�ָ�ȡ��
            if (restoreMoveSampleNextGetMachineState)
            {
                restoreMoveSampleNextGetMachineState = false;
                MoveSampleNextGetMachineState();
            }
            IsTesting = false;
        }
        /// <summary>
        /// ��������������
        /// 1���ϴ�
        /// 2����ӡ
        /// 
        /// </summary>
        /// <param name="temp"></param>
        private void SingleSampleTestFinished(TestResult temp)
        {
            AutoUpload(temp);
            AutoPrint(temp);
        }
        /// <summary>
        /// �Զ���ӡ�����
        /// </summary>
        /// <param name="temp"></param>
        private void AutoPrint(TestResult temp)
        {
            A4ReportUtil.AutoExecReport(temp,configService.IsAutoPrintA4Report(),false,configService.GetPrinterName());
            //��ӡСƱ
            if (configService.IsAutoPrintTicket()) {
                TicketReportUtil.Instance.PrintTicket(temp, (msg) => { }, (err) => { });
            }
        }
        /// <summary>
        /// �Զ��ϴ������
        /// </summary>
        /// <param name="temp"></param>
        private void AutoUpload(TestResult temp)
        {
            //�����Ӳ����Զ��ϴ��ѿ���
            if (HL7Helper.Instance.IsConnected() && UploadConfig.Instance.AutoUpload) {
                Task.Run(async() =>
                {
                    Log.Information($"��ʼ�ϴ�: {temp.Id}");
                    //�ϴ������
                    UploadResult ur =  await HL7Helper.Instance.UploadTestResultAsync(temp);
                    if (ur != null && ur.ResultType == UploadResultType.Success)
                    {
                        Log.Information($"�ϴ�������ɹ�: {ur?.TestResultId}");
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            //���¼����״̬
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
                        Log.Information($"�ϴ������ʧ��: {ur?.TestResultId} {JsonConvert.SerializeObject(temp)}");
                    }
                });
            }
        }

        public void ReceiveReactionTempModel(BaseResponseModel<ReactionTempModel> model)
        {
            if (!ContinueTest())
                return;
            ReactionTempFinished = true;
            Log.Information($"���յ� ��Ӧ���¶�: {JsonConvert.SerializeObject(model)}");
            // ����Ӧ���¶�����

            ReactionTemp = model.Data.Temp;
        }

        public void ReceiveClearReactionAreaModel(BaseResponseModel<ClearReactionAreaModel> model)
        {
            if (!ContinueTest())
                return;
            ClearReactionAreaFinished = true;
            Log.Information($"���յ� ��շ�Ӧ��: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveMotorModel(BaseResponseModel<MotorModel> model)
        {
            if (!ContinueTest())
                return;
            MotorFinished = true;
            Log.Information($"���յ� �������: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveResetParamsModel(BaseResponseModel<ResetParamsModel> model)
        {
            if (!ContinueTest())
                return;
            ResetParamsFinished = true;
            Log.Information($"���յ� ���ò���: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveUpdateModel(BaseResponseModel<UpdateModel> model)
        {
            if (!ContinueTest())
                return;
            UpdateFinished = true;
            Log.Information($"���յ� ����: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceiveSqueezingModel(BaseResponseModel<SqueezingModel> model)
        {
            if (!ContinueTest())
                return;
            SqueezingFinished = true;
            Log.Information($"���յ� ��ѹ: {JsonConvert.SerializeObject(model)}");
        }

        public void ReceivePiercedModel(BaseResponseModel<PiercedModel> model)
        {
            if (ContinueTest())
                return;
            PiercedFinished = true;
            Log.Information($"���յ� ����: {JsonConvert.SerializeObject(model)}");
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
            Log.Information("��ʾ�Լ�Ի���");
            showSelfController = await dialogCoordinator.ShowProgressAsync(
                this,
                "��ʾ",
                "�����Լ졭��"
            );
            showSelfController.SetIndeterminate();
        }

        private async Task CloseSelfMachineDialog()
        {
            Log.Information("�ر��Լ�Ի���");
            if (showSelfController != null)
            {
                await showSelfController?.CloseAsync();
            }
        }

        // ʵ�� ISerialPortService �ķ���
        public void GetSelfInspectionState()
        {
            SelfInspectionFinished = false;
            Log.Information("ִ�� �Լ�");
            serialPortService.GetSelfInspectionState();
        }

        public void GetMachineState()
        {
            MachineStateFinished = false;
            Log.Information("ִ�� ����״̬");
            serialPortService.GetMachineState();
        }

        public void MoveSampleShelf(int pos)
        {
            SampleShelfPos = pos;
            MoveSampleShelfFinished = false;
            Log.Information($"ִ�� �ƶ������� {pos + 1}");
            serialPortService.MoveSampleShelf(pos + 1);
        }

        public void MoveSample(int pos)
        {
            MoveSampleFinished = false;
            //SampleCurrentPos = pos;
            Log.Information($"ִ�� �ƶ����� {pos}");
            serialPortService.MoveSample(pos + 1);
        }

        public void Sampling(string type, int volume)
        {
            SamplingFinished = false;
            Log.Information($"ִ�� ȡ��������: {type}�����: {volume}");
            serialPortService.Sampling(type, volume);
        }

        public void CleanoutSamplingProbe()
        {
            CleanoutSamplingProbeFinished = false;
            Log.Information("ִ�� ��ϴȡ����");
            serialPortService.CleanoutSamplingProbe(configService.CleanoutDuration());
        }

        public void AddingSample(int volume, string type)
        {
            AddingSampleFinished = false;
            Log.Information($"ִ�� ���������: {volume}������: {type}");
            serialPortService.AddingSample(volume, type);
        }

        public void Drainage()
        {
            DrainageFinished = false;
            Log.Information("ִ�� ��ˮ");
            serialPortService.Drainage();
        }

        public void PushCard()
        {
            if (!MoveReactionAreaFinished)
            {
                //��δ����һ�ſ��ƶ�����Ӧ��,�ȵȴ�
                IsRestorePushCard = true;
                Log.Information("�ȴ� �ƶ���Ӧ�� ����");
            }
            else
            {
                //�ƿ�
                PushCardFinished = false;
                Log.Information("ִ�� �ƿ�");
                serialPortService.PushCard();
            }
        }

        public void MoveReactionArea(int x, int y)
        {
            MoveReactionAreaFinished = false;
            Log.Information($"ִ�� �ƶ���Ӧ�� ({x}, {y})");
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
                $"ִ�� ��⣬����: ({x}, {y})����Ƭ����: {cardType}���������: {testType}��"
                    + $"ɨ����ʼ: {scanStart}��ɨ�����: {scanEnd}����ֵ���: {peakWidth}����ֵ����: {peakDistance}"
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
            Log.Information($"ִ�� ��Ӧ���¶ȣ��¶�: {temp}");
            serialPortService.GetReactionTemp(temp);
        }

        public void ClearReactionArea()
        {
            ClearReactionAreaFinished = false;
            Log.Information("ִ�� ��շ�Ӧ��");
            serialPortService.ClearReactionArea();
        }

        public void Motor(string motor, string direction, string value)
        {
            MotorFinished = false;
            Log.Information($"ִ�� ������ƣ����: {motor}������: {direction}��ֵ: {value}");
            serialPortService.Motor(motor, direction, value);
        }

        public void ResetParams()
        {
            ResetParamsFinished = false;
            Log.Information("ִ�� ���ò���");
            serialPortService.ResetParams();
        }

        public void Update()
        {
            UpdateFinished = false;
            Log.Information($"ִ�� ����");
            serialPortService.Update();
        }

        public void Squeezing(string type)
        {
            SqueezingFinished = false;
            Log.Information($"ִ�� ��ѹ������: {type}");
            serialPortService.Squeezing(type);
        }

        public void Pierced(string type)
        {
            PiercedFinished = false;
            Log.Information($"ִ�� ���ƣ�����: {type}");
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
            //����������Ǽ����ƶ���Ӧ�����������д���󲻼������
            if (
                model.Code == SerialGlobal.CMD_Test
                || model.Code == SerialGlobal.CMD_MoveReactionArea
            )
            {
                SystemGlobal.ErrorContinueTest = false;
            }
            RunningErrorMsg = $"���д�������ϵ��������Աά����\n������Ϣ: {model.Error}";
            // ��ʾ������Ϣ
            GlobalUtil.ShowHiltDialog("��ʾ", RunningErrorMsg, "ȷ��", (d, dialog) => { });
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
