using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Repositorys;
using FluorescenceFullAutomatic.Sql;
using FluorescenceFullAutomatic.Upload;
using FluorescenceFullAutomatic.Utils;
using FluorescenceFullAutomatic.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Main.Upload;

namespace FluorescenceFullAutomatic.Services
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
        private readonly IApplyTestRepository _testRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ILisRepository _lisRepository;
        private readonly IReactionAreaQueueRepository _reactionAreaQueueRepository;
        private readonly IDialogRepository _dialogRepository;
        private readonly IUploadConfigRepository _uploadConfigRepository;
        private readonly IApplyTestRepository _applyTestRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IPrintRepository _printRepository;
        public event Func<ReactionAreaItem, bool> _dequeueCallback
        {
            add { _reactionAreaQueueRepository._dequeueCallback += value; }
            remove { _reactionAreaQueueRepository._dequeueCallback -= value; }
        }

        public HomeService(IApplyTestRepository testRepository, IConfigRepository configRepository
            , IProjectRepository projectRepository, ILisRepository lisRepository,IReactionAreaQueueRepository reactionAreaQueueRepository
            ,IDialogRepository dialogRepository,IUploadConfigRepository uploadConfigRepository,IApplyTestRepository applyTestRepository
            ,IPatientRepository patientRepository, IPrintRepository printRepository)
        {
            _testRepository = testRepository;
            _configRepository = configRepository;
            _projectRepository = projectRepository;
            _lisRepository = lisRepository;
            _reactionAreaQueueRepository = reactionAreaQueueRepository;
            _dialogRepository = dialogRepository;
            _uploadConfigRepository = uploadConfigRepository;
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

        public void ShowHiltDialog(string title, string msg, string confirmText, Action<HintDialogViewModel, CustomDialog> actionConfirm, string cancelText = null, Action<HintDialogViewModel, CustomDialog> actionCancel = null, string closeText = null, Action<HintDialogViewModel, CustomDialog> actionClose = null, bool autoCloseDialog = true)
        {
            _dialogRepository.ShowHiltDialog(title, msg, confirmText, actionConfirm, cancelText, actionCancel, closeText, actionClose, autoCloseDialog);
        }

        public Task<Hl7Result.QueryResult> QueryApplyTestAsync(bool isNeedLisGet, bool isMatchingBarcode, string barcode, string testNum)
        {
          return  _lisRepository.QueryApplyTestAsync(isNeedLisGet,isMatchingBarcode, barcode, testNum);
        }

        public bool isNeedLisGet()
        {
            return _uploadConfigRepository.GetOpenUpload() && _uploadConfigRepository.GetTwoWay() && _uploadConfigRepository.GetAutoGetApplyTest();
        }

        public bool isMatchingBarcode()
        {
            return _uploadConfigRepository.GetMatchBarcode();
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
           return HL7Helper.Instance.UploadTestResultAsync(testResult);
        }

        public bool Hl7IsRunning()
        {
           return HL7Helper.Instance.IsRunning();
        }

      
        public bool Hl7NeedAutoUpload()
        {
            return HL7Helper.Instance.IsConnected() && UploadConfig.Instance.AutoUpload;
        }

       

        public void AutoPrintReport(TestResult tr, bool autoPrint, bool autoUploadFtp, bool autoPrintTicket, string printerName)
        {
            _printRepository.AutoPrintReport(tr,autoPrint,autoUploadFtp,autoPrintTicket,printerName);
        }
    }
}
