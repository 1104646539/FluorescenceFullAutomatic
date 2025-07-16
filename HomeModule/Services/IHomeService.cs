using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.Sql;
using FluorescenceFullAutomatic.Platform.Utils;
using FluorescenceFullAutomatic.Platform.ViewModels;
using FluorescenceFullAutomatic.UploadModule.Upload;
using MahApps.Metro.Controls.Dialogs;

namespace FluorescenceFullAutomatic.HomeModule.Services
{
    public interface IHomeService
    {
        public event Func<ReactionAreaItem, bool> _dequeueCallback;

        bool GetIsDebug();
        int InsertTestResult(TestResult testResult);
        bool UpdateTestResult(TestResult testResult);
        TestResult GetTestResultAndPoint(int id);
        TestResult GetTestResult(int id);
        int InsertPoint(Point point);

        void UpdateApplyTestCompleted(ApplyTest applyTest);
  
        void Enqueue(ReactionAreaItem item);

        void SetDequeueDuration(int durationSeconds);

        void ReactionAreaQueueClear();

        int ReactionAreaQueueCount();

        bool ReactionAreaQueueIsEmpty();
        bool ReactionAreaQueueIsFull();

        public void ShowHiltDialog(
            object context,
      string title,
      string msg,
      string confirmText,
      Action<HintDialogViewModel, CustomDialog> actionConfirm,
      string cancelText = null,
      Action<HintDialogViewModel, CustomDialog> actionCancel = null,
      string closeText = null,
      Action<HintDialogViewModel, CustomDialog> actionClose = null,
      bool autoCloseDialog = true
  );
        public Task HideMetroDialogAsync(object context, BaseMetroDialog dialog, MetroDialogSettings settings = null);

        Task<ProgressDialogController> ShowProgressAsync(object context, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null);


        Task<Hl7Result.QueryResult> QueryApplyTestAsync(bool isNeedLisGet, bool isMatchingBarcode, string barcode, string testNum);

        bool isNeedLisGet();
        bool isMatchingBarcode();

        int InsertApplyTest(ApplyTest applyTest);

        int InsertPatient(Patient patient);

        Task<Hl7Result.UploadResult> UploadTestResultAsync(TestResult testResult);

        bool Hl7IsRunning();

        bool Hl7NeedAutoUpload();

       void AutoPrintReport(TestResult tr, bool autoPrint, bool autoUploadFtp, bool autoPrintTicket, string printerName);
    }

    public class HomeService : IHomeService
    {
        private readonly IApplyTestService _testRepository;
        private readonly IConfigService _configRepository;
        private readonly IProjectService _projectRepository;
        private readonly ILisService _lisRepository;
        private readonly IReactionAreaQueueService _reactionAreaQueueRepository;
        private readonly IDialogService _dialogRepository;
        private readonly IApplyTestService _applyTestRepository;
        private readonly IPatientService _patientRepository;
        private readonly IPrintService _printRepository;
        public event Func<ReactionAreaItem, bool> _dequeueCallback
        {
            add { _reactionAreaQueueRepository._dequeueCallback += value; }
            remove { _reactionAreaQueueRepository._dequeueCallback -= value; }
        }

        public HomeService(IApplyTestService testRepository, IConfigService configRepository
            , IProjectService projectRepository, ILisService lisRepository,IReactionAreaQueueService reactionAreaQueueRepository
            ,IDialogService dialogRepository,IApplyTestService applyTestRepository
            ,IPatientService patientRepository, IPrintService printRepository)
        {
            _testRepository = testRepository;
            _configRepository = configRepository;
            _projectRepository = projectRepository;
            _lisRepository = lisRepository;
            _reactionAreaQueueRepository = reactionAreaQueueRepository;
            _dialogRepository = dialogRepository;
            _applyTestRepository = applyTestRepository;
            _patientRepository = patientRepository;
            _printRepository = printRepository;
        }

        public bool GetIsDebug()
        {
            return true;
        }

        public int InsertTestResult(TestResult testResult)
        {
            return SqlHelper.getInstance().InsertTestResult(testResult);
        }

        public bool UpdateTestResult(TestResult testResult)
        {
            return SqlHelper.getInstance().UpdateTestResult(testResult);
        }

        public TestResult GetTestResult(int id)
        {
            return SqlHelper.getInstance().GetTestResultForID(id);
        }

        public TestResult GetTestResultAndPoint(int id)
        {
            return SqlHelper.getInstance().GetTestResultAndPoint(id);
        }

        public int InsertPoint(Point point)
        {
            return SqlHelper.getInstance().InsertPoint(point);
        }


        public void UpdateApplyTestCompleted(ApplyTest applyTest)
        {
            applyTest.ApplyTestType = ApplyTestType.TestEnd;
            SqlHelper.getInstance().UpdateApplyTest(applyTest);
        }

        public void Enqueue(ReactionAreaItem item)
        {
            _reactionAreaQueueRepository.Enqueue(item);
        }

        public void SetDequeueDuration(int durationSeconds)
        {
            _reactionAreaQueueRepository.SetDequeueDuration(durationSeconds);
        }

    
        public void ReactionAreaQueueClear()
        {
            _reactionAreaQueueRepository.Clear();
        }

        public int ReactionAreaQueueCount()
        {
            return _reactionAreaQueueRepository.Count();
        }

        public bool ReactionAreaQueueIsEmpty()
        {
            return _reactionAreaQueueRepository.IsEmpty();
        }

        public bool ReactionAreaQueueIsFull()
        {
            return _reactionAreaQueueRepository.IsFull();
        }

        public void ShowHiltDialog(object context, string title, string msg, string confirmText, Action<HintDialogViewModel, CustomDialog> actionConfirm, string cancelText = null, Action<HintDialogViewModel, CustomDialog> actionCancel = null, string closeText = null, Action<HintDialogViewModel, CustomDialog> actionClose = null, bool autoCloseDialog = true)
        {
            _dialogRepository.ShowHiltDialog(context,title, msg, confirmText, actionConfirm, cancelText, actionCancel, closeText, actionClose, autoCloseDialog);
        }

        public Task<Hl7Result.QueryResult> QueryApplyTestAsync(bool isNeedLisGet, bool isMatchingBarcode, string barcode, string testNum)
        {
          return  _lisRepository.QueryApplyTestAsync(isNeedLisGet,isMatchingBarcode, barcode, testNum);
        }

        public bool isNeedLisGet()
        {
            return _lisRepository.GetOpenUpload() && _lisRepository.GetTwoWay() && _lisRepository.GetAutoGetApplyTest();
        }

        public bool isMatchingBarcode()
        {
            return _lisRepository.GetMatchBarcode();
        }

        public int InsertApplyTest(ApplyTest applyTest)
        {
            return _applyTestRepository.InsertApplyTest(applyTest);
        }

        public int InsertPatient(Patient patient)
        {
            return _patientRepository.InsertPatient(patient);
        }

        public Task<ProgressDialogController> ShowProgressAsync(object context, string title, string message, bool isCancelable = false, MetroDialogSettings settings = null)
        {
            return _dialogRepository.ShowProgressAsync(context,title,message,isCancelable,settings);
        }

        public Task<Hl7Result.UploadResult> UploadTestResultAsync(TestResult testResult)
        {
           return _lisRepository.UploadTestResultAsync(testResult);
        }

        public bool Hl7IsRunning()
        {
           return _lisRepository.IsConnected();
        }

      
        public bool Hl7NeedAutoUpload()
        {
            return _lisRepository.IsConnected() && UploadConfig.Instance.AutoUpload;
        }

       

        public void AutoPrintReport(TestResult tr, bool autoPrint, bool autoUploadFtp, bool autoPrintTicket, string printerName)
        {
            _printRepository.AutoPrintReport(tr,autoPrint,autoUploadFtp,autoPrintTicket,printerName);
        }

        public Task HideMetroDialogAsync(object context, BaseMetroDialog dialog, MetroDialogSettings settings = null)
        {
            return _dialogRepository.HideMetroDialogAsync(context, dialog, settings);
        }
    }
}
