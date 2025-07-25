﻿using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.ViewModels;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface IDataManagerService
    {
        /// <summary>
        /// 获取所有项目
        /// </summary>
        /// <returns></returns>
        List<Project> GetAllProject();
        /// <summary>
        /// 获取所有结果
        /// </summary>
        /// <returns></returns>
        Task<List<TestResult>> GetAllTestResult(ConditionModel conditio);
        Task<List<TestResult>> GetAllTestResult(ConditionModel condition, int page, int pageSize);
        TestResult GetTestResult(int id);
        Task<int> GetAllTestResultCountPage(ConditionModel condition, int pageSize);
        Task<int> GetAllTestResultCount(ConditionModel condition);
        int DeleteTestResult(List<TestResult> testResults);
        TestResult GetTestResultAndPoint(int id);
        bool UpdateTestResult(TestResult testResult);
        Task<bool> ExportTestResultsToExcelAsync(List<TestResult> testResults, string filePath);
        string GetPrinterName();

        int InsertPatient(Patient patient);
        bool UpdatePatient(Patient patient);

        public void ShowHiltDialog(MetroWindow window,
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
        public void PrintReport(List<TestResult> trs, string printerName, Action<string> successAction = null, Action<string> failedAction = null);

        public void PrintTicket(List<TestResult> trs, Action<string> successAction, Action<string> failedAction);
    }
    public class DataManagerService : IDataManagerService
    {

        private readonly IProjectService _projectRepository;
        private readonly ITestResultService _testResultRepository;
        private readonly IExportExcelService _exportExcelRepository;
        private readonly IConfigService _configRepository;
        private readonly IPatientService _patientRepository;
        private readonly IDialogService _dialogRepository;
        private readonly IPrintService _printRepository;

        public DataManagerService(IProjectService projectRepository, ITestResultService testResultRepository,
        IExportExcelService exportExcelRepository, IConfigService configRepository, IPatientService patientRepository,
        IDialogService dialogRepository, IPrintService printRepository)
        {
            this._projectRepository = projectRepository;
            this._testResultRepository = testResultRepository;
            this._exportExcelRepository = exportExcelRepository;
            this._configRepository = configRepository;
            this._patientRepository = patientRepository;
            this._dialogRepository = dialogRepository;
            this._printRepository = printRepository;
        }
        public void PrintReport(List<TestResult> trs, string printerName, Action<string> successAction = null, Action<string> failedAction = null)
        {
            _printRepository.PrintReport(trs, printerName, successAction, failedAction);
        }

        public void PrintTicket(List<TestResult> trs, Action<string> successAction, Action<string> failedAction)
        {
            _printRepository.PrintTicket(trs, successAction, failedAction);
        }
        public List<Project> GetAllProject()
        {
            return _projectRepository.GetAllProject();
        }

        public Task<List<TestResult>> GetAllTestResult(ConditionModel condition)
        {
            return _testResultRepository.GetAllTestResultAsync(condition);
        }

        public Task<List<TestResult>> GetAllTestResult(ConditionModel condition, int page, int pageSize)
        {
            return _testResultRepository.GetAllTestResultAsync(condition, page, pageSize);
        }

        public Task<int> GetAllTestResultCountPage(ConditionModel condition, int pageSize)
        {
            return _testResultRepository.GetAllTestResultCountPageAsync(condition, pageSize);

        }

        public Task<int> GetAllTestResultCount(ConditionModel condition)
        {
            return _testResultRepository.GetAllTestResultCountAsync(condition);
        }

        public TestResult GetTestResult(int id)
        {
            return _testResultRepository.GetTestResultForID(id);
        }

        public int DeleteTestResult(List<TestResult> testResults)
        {
            return _testResultRepository.DeleteTestResult(testResults);
        }

        public TestResult GetTestResultAndPoint(int id)
        {
            return _testResultRepository.GetTestResultPointForID(id);
        }

        public bool UpdateTestResult(TestResult testResult)
        {
            return _testResultRepository.UpdateTestResult(testResult);
        }

        public Task<bool> ExportTestResultsToExcelAsync(List<TestResult> testResults, string filePath)
        {
            return _exportExcelRepository.ExportTestResultsToExcelAsync(testResults, filePath);
        }

        public string GetPrinterName()
        {
            return _configRepository.GetPrinterName();
        }

        public int InsertPatient(Patient patient)
        {
            return _patientRepository.InsertPatient(patient);
        }

        public bool UpdatePatient(Patient patient)
        {
            return _patientRepository.UpdatePatient(patient);
        }

        public void ShowHiltDialog(MetroWindow window,string title, string msg, string confirmText, Action<HintDialogViewModel, CustomDialog> actionConfirm, string cancelText = null, Action<HintDialogViewModel, CustomDialog> actionCancel = null, string closeText = null, Action<HintDialogViewModel, CustomDialog> actionClose = null, bool autoCloseDialog = true)
        {
            _dialogRepository.ShowHiltDialog(window, title, msg, confirmText, actionConfirm, cancelText, actionCancel, closeText, actionClose, autoCloseDialog);
        }

        public Task HideMetroDialogAsync(MetroWindow window, BaseMetroDialog dialog, MetroDialogSettings settings = null)
        {
            return _dialogRepository.HideMetroDialogAsync(window, dialog, settings);
        }
    }
}
