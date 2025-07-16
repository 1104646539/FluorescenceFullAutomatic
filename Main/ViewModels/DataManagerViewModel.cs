using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Views.Ctr;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FluorescenceFullAutomatic.Platform.Utils;
using System.Windows;
using FluorescenceFullAutomatic.ViewModels;
using System.Threading;
using SqlSugar;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Views;
using FluorescenceFullAutomatic.UploadModule.Upload;
using FluorescenceFullAutomatic.Core.Config;
using Microsoft.Win32;
using System.IO;
using OpenTK.Graphics.ES10;
using System.Web.ApplicationServices;
using FluorescenceFullAutomatic.Core.Model;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class DataManagerViewModel:ObservableObject
    {
        private readonly IDataManagerService _dataManagerService;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IDispatcherService _dispatcherService;
        [ObservableProperty]
        ObservableCollection<TestResult> testResults;  
        private bool _isFirstLoad = true;
        [ObservableProperty]
        private bool isVisible;
        [ObservableProperty]
        private Visibility visibility;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private double controlWidth;

        [ObservableProperty]
        private double controlHeight;

        [ObservableProperty]
        private int currentPage;
        [ObservableProperty]
        private int totalPage;
        /// <summary>
        /// 一页数量
        /// </summary>
        private const int PageSize = 50;
        /// <summary>
        /// 一次最大导出数量
        /// </summary>
        private const int Max_Export_Count = 5000;
        [ObservableProperty]
        PagingControlViewModel pagingControlViewModel;

        [ObservableProperty]
        private ObservableCollection<TestResult> selectedItems;

        [ObservableProperty]
        private bool isAllSelected;

        [ObservableProperty]
        private bool isExportMenuOpen;

        [ObservableProperty]
        private ObservableCollection<MenuItemViewModel> exportMenuItems;
        [ObservableProperty]
        private bool isPrintMenuOpen;

        [ObservableProperty]
        private ObservableCollection<MenuItemViewModel> printMenuItems;

        
        /// <summary>
        /// 全选
        /// </summary>
        [RelayCommand]
        public void SelectAll()
        {
            if (TestResults == null) return;
            
            foreach (var item in TestResults)
            {
                item.IsSelected = IsAllSelected;
                if (IsAllSelected)
                {
                    if (!SelectedItems.Contains(item))
                    {
                        SelectedItems.Add(item);
                    }
                }
                else
                {
                    SelectedItems.Remove(item);
                }
            }
        }

        [RelayCommand]
        public void SelectionChanged(TestResult testResult)
        {
            if (testResult != null)
            {
                if (testResult.IsSelected)
                {
                    if (!SelectedItems.Contains(testResult))
                    {
                        SelectedItems.Add(testResult);
                    }
                }
                else
                {
                    SelectedItems.Remove(testResult);
                }
            }
        }

        [RelayCommand]
        public void GetSelectedItems()
        {
            Log.Information($"选中的数据:{SelectedItems?.Count}");

        }
        [RelayCommand]
        public async void DeleteSelected() {
            if (VerifySelectedAndRunning())
            {
                return;
            }
            dialogService.ShowHiltDialog(this,"提示", "确定删除数据吗？删除后不可恢复", "确定", async (vm, d) => {
                await MainWindow.Instance.HideMetroDialogAsync(d);
                DeleteData();
            }, "取消", (vm, d) => {});
        }

        private async void DeleteData()
        {
            int retCount = _dataManagerService.DeleteTestResult(SelectedItems.ToList());

            dialogService.ShowHiltDialog(this,"提示", $"删除成功", "确定", (vm, d) => { });
            if (retCount > 0)
            {
                await LoadData();
            }
        }
        private  bool VerifySelectedAndRunning()
        {
            if (SelectedItems == null || SelectedItems.Count <= 0)
            {
                dialogService.ShowHiltDialog(this,"提示", "未选择数据", "确定", (vm, d) => { });
                return true;
            }
            return VerifyRunning();

        }
        private bool VerifyRunning() {
            if (SystemGlobal.MachineStatus.IsRunning())
            {
                dialogService.ShowHiltDialog(this,"提示", "正在检测中，请等待检测结束", "确定", (vm, d) => { });
                return true;
            }
            return false;
        }
        /// <summary>
        /// 上传
        /// </summary>
        [RelayCommand]
        public async void UploadItems() {
            if (VerifySelectedAndRunning())
            {
                return;
            }
            var controller = await MainWindow.Instance.ShowProgressAsync("提示", "正在上传，请稍候...");
            int count = SelectedItems.Count;
            lisService.UploadTestResult(SelectedItems.ToList(), async (call) =>
            {
                string hilt = "";
                await controller.CloseAsync();
                if (call == null || call.Count <= 0)
                {
                    hilt = $"上传完成，本次上传成功{count}条数据";
                }
                else
                {
                    int success = count - call.Count;
                    string detail = string.Join(",", call.Select(c => $"\n数据{c.TestResultId},{c.ResultType}"));
                    hilt = $"上传完成，本次上传成功{success}条数据,失败{call.Count}条。具体为{detail}";
                }
                dialogService.ShowHiltDialog(this, "提示", hilt, "确定", (_, d) => { });
            });

        }
        private readonly IDialogService dialogService;
        private readonly ILisService lisService;
        public DataManagerViewModel(IDataManagerService dataManagerService, 
            IDialogCoordinator dialogCoordinator, IDispatcherService dispatcherService,IDialogService dialogService,ILisService lisService)
        {
            this.lisService = lisService;
            this.dialogService =  dialogService;
            _dataManagerService = dataManagerService;
            _dialogCoordinator = dialogCoordinator;
            _dispatcherService = dispatcherService;

            PagingControlViewModel = new PagingControlViewModel();
            PagingControlViewModel.PageChanged += OnPageChanged;
            PagingControlViewModel.SetPageSize(PageSize);
            RegisterMsg();

            CurrentPage = 1;

            TestResults = new ObservableCollection<TestResult>();
            SelectedItems = new ObservableCollection<TestResult>();

            // 初始化导出菜单项
            ExportMenuItems = new ObservableCollection<MenuItemViewModel>
            {
                new MenuItemViewModel
                {
                    Header = "导出所选数据",
                    Command = ExportSelectedToExcelCommand
                },
                new MenuItemViewModel
                {
                    Header = "导出所有数据",
                    Command = ExportAllToExcelCommand
                }
            };
            // 初始化导出菜单项
            PrintMenuItems = new ObservableCollection<MenuItemViewModel>
            {
                new MenuItemViewModel
                {
                    Header = "打印A4报告",
                    Command = PrintA4Command
                },
                new MenuItemViewModel
                {
                    Header = "打印小票",
                    Command = PrintTicketCommand
                }
            };
        }
     
        private void RegisterMsg()
        {
            WeakReferenceMessenger.Default.Register<EventMsg<DataChangeMsg>>(this, (r, m) => {
                if (m.What == EventWhat.WHAT_REFRESH_DATA)
                {

                }
                else if (m.What == EventWhat.WHAT_ADD_DATA)
                {
                    Log.Information($"data 添加={m.Value.ID}");
                    RunAddData(m.Value.ID);
                }
                else if (m.What == EventWhat.WHAT_CHANGE_DATA)
                {
                    Log.Information($"data 更新={m.Value.ID}");
                    RunChangeData(m.Value.ID);
                }
            });
        }

        private void RunChangeData(int id)
        {
            TestResult tr = _dataManagerService.GetTestResult(id);
            if (TestResults != null && tr != null)
            {
                for (int i = 0; i < TestResults.Count; i++)
                {
                    if (TestResults[i].Id == id) {
                        _dispatcherService.Invoke(() =>
                        {
                            TestResults[i] = tr;
                        });
                      
                    }
                }
            }
        }

        private void RunAddData(int id)
        {
            TestResult tr = _dataManagerService.GetTestResult(id);
            if (TestResults != null && tr != null && CurrentPage == 1) {
                if (TestResults.Count >= PageSize)
                {
                    TestResults.RemoveAt(TestResults.Count - 1);
                }
                TestResults.Insert(0,tr);
            }
        }

        [RelayCommand]
        public void Loaded()
        {
            if(_isFirstLoad)
            {
                CurrentPage = 1;
                LoadData();
                _isFirstLoad = false;
            }
        }
        [RelayCommand]
        public async Task LoadData()
        {
            try
            {
                IsLoading = true;
                await GetAllTestResult();
            }
            finally
            {
                IsLoading = false;
            }
        }
        public async Task GetAllTestResult()
        {
            
            Log.Information($"GetAllTestResult {DateTime.Now.GetDateTimeString2()}");
            TotalPage = await _dataManagerService.GetAllTestResultCountPage(condition,PageSize);
            PagingControlViewModel.SetTotalPages(TotalPage);
            Log.Information($"GetAllTestResult2 {DateTime.Now.GetDateTimeString2()}");

            var result = await _dataManagerService.GetAllTestResult(condition,CurrentPage, PageSize);
            TestResults = new ObservableCollection<TestResult>(result);
            Log.Information($"GetAllTestResult2 {DateTime.Now.GetDateTimeString2()}");

        }

        CustomDialog customDialog = new CustomDialog();
        [RelayCommand]
        public void ClickResultDetails(TestResult testResult){
            Log.Information($"点击详情: {JsonConvert.SerializeObject(testResult)}");
            testResult = _dataManagerService.GetTestResultAndPoint(testResult.Id);
            ResultDetailsViewModel resultDetailsViewModel = new ResultDetailsViewModel();
            resultDetailsViewModel.Result = testResult;
            resultDetailsViewModel.CloseAction = () =>
            {
                _dialogCoordinator.HideMetroDialogAsync(this, customDialog);
            };
            resultDetailsViewModel.ConfirmAction = async (tr) =>
            {
                await _dialogCoordinator.HideMetroDialogAsync(this, customDialog);
                SaveEditInfo(tr);
                Log.Information($"{JsonConvert.SerializeObject(tr)}");

            };
            ResultDetailsControl resultDetailsControl = new ResultDetailsControl();
            resultDetailsControl.Update(resultDetailsViewModel);
            customDialog.Content = resultDetailsControl;
            
            _dialogCoordinator.ShowMetroDialogAsync(this, customDialog);
        }
        public ConditionModel condition = new ConditionModel();
        /// <summary>
        /// 筛选数据
        /// </summary>
        [RelayCommand]
        public void FilterCondition() { 
            //显示筛选对话框
            FilterConditionViewModel filterConditionViewModel = new FilterConditionViewModel();
            filterConditionViewModel.Update(condition);
            filterConditionViewModel.confirmAction = (cnd) => {
                this.condition = cnd;
                MainWindow.Instance.HideMetroDialogAsync(customDialog);
                LoadData();
            };
            filterConditionViewModel.cancelAction = () => { 
                MainWindow.Instance.HideMetroDialogAsync(customDialog);
            };
            FilterConditionControl filterConditionControl = new FilterConditionControl();
            filterConditionControl.DataContext = filterConditionViewModel;
            customDialog.Content = filterConditionControl;
             _dialogCoordinator.ShowMetroDialogAsync(this, customDialog);
        }
        private void SaveEditInfo(TestResult tr)
        {
            if (tr == null) return;
            if (tr.Patient != null)
            {
                Patient tempPatient = tr.Patient;
                if (tempPatient.Id == 0)
                {
                    //保存
                   int id =  _dataManagerService.InsertPatient(tempPatient);
                   tempPatient.Id = id;
                }
                else {
                    //更新
                    _dataManagerService.UpdatePatient(tempPatient);
                }
                tr.Patient = tempPatient;
                tr.PatientId = tempPatient.Id;
            }
            _dataManagerService.UpdateTestResult(tr);
            RunChangeData(tr.Id);
        }

        [RelayCommand]
        public void SizeChanged()
        {
            Log.Information($"窗口大小变更 {ControlWidth} {ControlHeight}");
        }
        
      
        private void OnPageChanged(int page)
        {
            CurrentPage = page;
            Log.Information($"Page {page}");
            LoadPagedData();
        }

        private async void LoadPagedData()
        {
           await LoadData();
        }

        [RelayCommand]
        public async Task ExportToExcel()
        {
            // 默认导出所选数据
            await ExportSelectedToExcel();
        }
        [RelayCommand]
        public void PrintA4()
        {
            if (SelectedItems == null || SelectedItems.Count <= 0)
            {
                dialogService.ShowHiltDialog(this,"提示", "请选择要打印的数据", "确定", (vm, d) => { });
                return;
            }
            _dataManagerService.PrintReport(SelectedItems.ToList(), _dataManagerService.GetPrinterName(), (msg) => {
                _dispatcherService.Invoke(() =>
                {
                    dialogService.ShowHiltDialog(this,"提示", $"打印完成,{msg}", "确定", (vm, d) => { });
                });
            }, (err) => {
                _dispatcherService.Invoke(() =>
                {
                    dialogService.ShowHiltDialog(this,"提示", $"打印失败,{err}", "确定", (vm, d) => { });
                });
            });
             
        }
        [RelayCommand]
        public void PrintTicket()
        {
            if (SelectedItems == null || SelectedItems.Count <= 0)
            {
                dialogService.ShowHiltDialog(this,"提示", "请选择要打印的数据", "确定", (vm, d) => { });
                return;
            }
            _dataManagerService.PrintTicket(SelectedItems.ToList(), (msg)=> {
                _dispatcherService.Invoke(() =>
                {
                    dialogService.ShowHiltDialog(this,"提示", $"打印完成", "确定", (vm, d) => { });
                });
            },(err) => {
                _dispatcherService.Invoke(() =>
                {
                    dialogService.ShowHiltDialog(this,"提示", $"打印失败：{err}", "确定", (vm, d) => { });
                });
            });
        }
        [RelayCommand]
        public async Task ExportSelectedToExcel()
        {
            if (SelectedItems == null || SelectedItems.Count <= 0)
            {
                dialogService.ShowHiltDialog(this,"提示", "请选择要导出的数据", "确定", (vm, d) => { });
                return;
            }

            await ExportDataToExcel(SelectedItems.ToList());
        }
       
        [RelayCommand]
        public async Task ExportAllToExcel()
        {
            int count = await _dataManagerService.GetAllTestResultCount(condition);
            if (count > Max_Export_Count) {
                dialogService.ShowHiltDialog(this,"提示", $"导出失败，一次导出数量不能超过{Max_Export_Count}条，当前选择了{count}条", "确定", (vm, d) => { });
                return;
            }
            List<TestResult> tempTr = await _dataManagerService.GetAllTestResult(condition);
            await ExportDataToExcel(tempTr);
        }
        /// <summary>
        /// 导出数据到Excel
        /// </summary>
        /// <param name="dataToExport"></param>
        /// <returns></returns>
        private async Task ExportDataToExcel(List<TestResult> dataToExport)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel文件|*.xlsx",
                Title = "导出Excel",
                FileName = $"检测结果_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var controller = await _dialogCoordinator.ShowProgressAsync(this, "提示", "正在导出数据，请稍候...");
                controller.SetIndeterminate();

                try
                {
                    var success = await _dataManagerService.ExportTestResultsToExcelAsync(dataToExport, saveFileDialog.FileName);
                    await controller.CloseAsync();

                    if (success)
                    {
                        dialogService.ShowHiltDialog(this,"提示", "导出成功", "确定", (vm, d) => { });
                    }
                    else
                    {
                        dialogService.ShowHiltDialog(this,"提示", "导出失败", "确定", (vm, d) => { });
                    }
                }
                catch (Exception ex)
                {
                    await controller.CloseAsync();
                    Log.Error(ex, "导出Excel时发生错误");
                    dialogService.ShowHiltDialog(this,"错误", "导出过程中发生错误", "确定", (vm, d) => { });
                }
            }
        }

        [RelayCommand]
        private void ShowExportMenu()
        {
            IsExportMenuOpen = true;
        }

        [RelayCommand]
        private void HideExportMenu()
        {
            IsExportMenuOpen = false;
        }
        [RelayCommand]
        private void ShowPrintMenu()
        {
            IsPrintMenuOpen = true;
        }

        [RelayCommand]
        private void HidePrintMenu()
        {
            IsPrintMenuOpen = false;
        }
    }
}
