using FluorescenceFullAutomatic.Repositorys;
using FluorescenceFullAutomatic.Services;
using FluorescenceFullAutomatic.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMain.Repositorys;
using FluorescenceFullAutomatic.Model;
using System.Collections.ObjectModel;
using TestResult = FluorescenceFullAutomatic.Model.TestResult;
using System.Windows.Markup;
using TestMain.Services;

namespace TestMain.ViewModels
{
    /// <summary>
    /// DataManagerViewModel的单元测试类
    /// </summary>
    [TestClass]
    public class DataManagerViewModelUnitTest
    {
        // 服务和ViewModel实例
        private IDataManagerService _dataManagerService;
        private DataManagerViewModel _viewModel;
        private Mock<IDialogCoordinator> _mockDialogCoordinator;

        // 仓储实例
        private FakeTestResultRepository _testResultRepository;
        private FakeProjectRepository _projectRepository;
        private FakeExportExcelRepository _exportExcelRepository;
        private FakeConfigRepository _configRepository;
        private FakePatientRepository _patientRepository;
        private FakeDialogRepository _dialogRepository;
        private FakePrintRepository _printRepository;

        /// <summary>
        /// 测试初始化
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // 创建所需的Repository实例
            _projectRepository = new FakeProjectRepository();
            _testResultRepository = new FakeTestResultRepository();
            _exportExcelRepository = new FakeExportExcelRepository();
            _configRepository = new FakeConfigRepository();
            _patientRepository = new FakePatientRepository();
            _dialogRepository = new FakeDialogRepository();
            _printRepository = new FakePrintRepository();

            // 创建DataManagerService实例
            _dataManagerService = new DataManagerService(_projectRepository, _testResultRepository, _exportExcelRepository,
                _configRepository, _patientRepository, _dialogRepository, _printRepository);
            _mockDialogCoordinator = new Mock<IDialogCoordinator>();
            // 创建ViewModel实例
            _viewModel = new DataManagerViewModel(_dataManagerService, _mockDialogCoordinator.Object,new FakeDispatcherService());
        }

        /// <summary>
        /// 测试数据加载功能
        /// </summary>
        [TestMethod]
        public async Task LoadData_ShouldLoadTestResults()
        {
            // Arrange - 准备测试数据
            _testResultRepository.InitData();

            // Act - 执行加载
            await _viewModel.LoadData();

            // Assert - 验证结果
            Assert.IsNotNull(_viewModel.TestResults);
            Assert.AreEqual(50, _viewModel.TestResults.Count);
            Assert.AreEqual("张三", _viewModel.TestResults[0].Patient.PatientName);
            Assert.AreEqual("血红蛋白", _viewModel.TestResults[0].Project.ProjectName);
            Assert.AreEqual(120, _viewModel.TestResults[0].Id);

            //下一页
            _viewModel.CurrentPage = 2;
            await _viewModel.LoadData();
            // Assert - 验证结果
            Assert.IsNotNull(_viewModel.TestResults);
            Assert.AreEqual(50, _viewModel.TestResults.Count);
            Assert.AreEqual(70, _viewModel.TestResults[0].Id);

            //下一页
            _viewModel.CurrentPage = 3;
            await _viewModel.LoadData();
            // Assert - 验证结果
            Assert.IsNotNull(_viewModel.TestResults);
            Assert.AreEqual(20, _viewModel.TestResults.Count);
            Assert.AreEqual(20, _viewModel.TestResults[0].Id);

            //下一页
            _viewModel.CurrentPage = 4;
            await _viewModel.LoadData();
            // Assert - 验证结果
            Assert.IsNotNull(_viewModel.TestResults);
            Assert.AreEqual(0, _viewModel.TestResults.Count);
        }

        /// <summary>
        /// 测试全选功能
        /// </summary>
        [TestMethod]
        public async Task SelectAll_ShouldSelectAllItems()
        {
            // Arrange - 准备测试数据
            _testResultRepository.InitData();
            await _viewModel.LoadData();

            // Act - 执行全选
            _viewModel.IsAllSelected = true;
            _viewModel.SelectAllCommand.Execute(null);

            // Assert - 验证结果
            Assert.IsTrue(_viewModel.TestResults.All(tr => tr.IsSelected));
            Assert.AreEqual(50, _viewModel.SelectedItems.Count);
        }

        /// <summary>
        /// 测试取消全选功能
        /// </summary>
        [TestMethod]
        public async Task UnselectAll_ShouldUnselectAllItems()
        {
            // Arrange - 准备已选中的测试数据
            _testResultRepository.InitData();
            await _viewModel.LoadData();
            
            // 先全选
            _viewModel.IsAllSelected = true;
            _viewModel.SelectAllCommand.Execute(null);

            // Act - 执行取消全选
            _viewModel.IsAllSelected = false;
            _viewModel.SelectAllCommand.Execute(null);

            // Assert - 验证结果
            Assert.IsTrue(_viewModel.TestResults.All(tr => !tr.IsSelected));
            Assert.AreEqual(0, _viewModel.SelectedItems.Count);
        }

        /// <summary>
        /// 测试添加新数据功能
        /// </summary>
        [TestMethod]
        public async Task RunAddData_ShouldAddNewTestResult()
        {
            // Arrange - 准备初始数据
            _testResultRepository.InitData();
            await _viewModel.LoadData();
            int originalCount = _viewModel.TestResults.Count;
            string originalFirstPatientName = _viewModel.TestResults[0].Patient.PatientName;

            #region  添加测试信息
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
                for (int j = 0; j < 600; j++)
                {
                    randomPoints[j] = random.Next(0, 5000);
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
                tr.Barcode = "BarCode999" ;
                tr.CardQRCode = "QR100200300";
                tr.TestNum =  "999";
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
            #endregion

            //添加
            int newId = _testResultRepository.InsertTestResult(tr);
            tr.Id = newId;
            // Act - 执行添加消息通知
            _viewModel.GetType().GetMethod("RunAddData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_viewModel, new object[] { newId });

            // Assert - 验证结果
            Assert.AreEqual(newId, _viewModel.TestResults[0].Id);
            Assert.AreEqual("999", _viewModel.TestResults[0].TestNum);
            Assert.AreEqual("BarCode999", _viewModel.TestResults[0].Barcode);
        }

        /// <summary>
        /// 测试更新现有数据功能
        /// </summary>
        [TestMethod]
        public async Task RunChangeData_ShouldUpdateExistingTestResult()
        {
            // Arrange - 准备初始数据
            _testResultRepository.InitData();
            await _viewModel.LoadData();

            // 更新第一条数据
            var firstResult = _viewModel.TestResults[0];
            var updatedTestResult = firstResult.Clone();
            updatedTestResult.Patient.PatientName = "张三已更新";
            _dataManagerService.UpdateTestResult(updatedTestResult);

            // Act - 执行更新消息通知
            _viewModel.GetType().GetMethod("RunChangeData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_viewModel, new object[] { updatedTestResult.Id });

            // Assert - 验证结果
            Assert.AreEqual("张三已更新", _viewModel.TestResults[0].Patient.PatientName);
        }

        /// <summary>
        /// 测试导出Excel功能
        /// </summary>
        [TestMethod]
        public async Task ExportToExcel_ShouldExportSelectedResults()
        {
            // Arrange - 准备导出数据
            _testResultRepository.InitData();
            await _viewModel.LoadData();

            // 选择前5条数据
            foreach (var item in _viewModel.TestResults.Take(5))
            {
                item.IsSelected = true;
                _viewModel.SelectionChangedCommand.Execute(item);
            }

            string filePath = "test.xlsx";

            // Act - 执行导出
            bool ret = await _dataManagerService.ExportTestResultsToExcelAsync(_viewModel.SelectedItems.ToList(), filePath);

            // Assert - 验证结果
            Assert.IsTrue(ret);
            Assert.AreEqual(5, _viewModel.SelectedItems.Count);
        }

        /// <summary>
        /// 测试打印报告功能
        /// </summary>
        [TestMethod]
        public async Task PrintReport_ShouldPrintSelectedResults()
        {
            // Arrange - 准备打印数据
            _testResultRepository.InitData();
            await _viewModel.LoadData();

            // 选择前5条数据
            foreach (var item in _viewModel.TestResults.Take(5))
            {
                item.IsSelected = true;
                _viewModel.SelectionChangedCommand.Execute(item);
            }

            string printerName = "测试打印机";
            _configRepository.SetPrinterName(printerName);

            bool printSuccess = false;
            Action<string> successAction = (msg) => printSuccess = true;

            // Act - 执行打印
            _dataManagerService.PrintReport(_viewModel.SelectedItems.ToList(), printerName, successAction);

            // Assert - 验证结果
            Assert.IsTrue(printSuccess);
            Assert.AreEqual(5, _viewModel.SelectedItems.Count);
        }

        /// <summary>
        /// 测试打印小票功能
        /// </summary>
        [TestMethod]
        public async Task PrintTicket_ShouldPrintTicketForSelectedResults()
        {
            // Arrange - 准备打印数据
            _testResultRepository.InitData();
            await _viewModel.LoadData();

            // 选择前5条数据
            foreach (var item in _viewModel.TestResults.Take(5))
            {
                item.IsSelected = true;
                _viewModel.SelectionChangedCommand.Execute(item);
            }

            bool printSuccess = false;
            Action<string> successAction = (msg) => printSuccess = true;
            Action<string> failedAction = (msg) => printSuccess = false;

            // Act - 执行打印小票
            _dataManagerService.PrintTicket(_viewModel.SelectedItems.ToList(), successAction, failedAction);

            // Assert - 验证结果
            Assert.IsTrue(printSuccess);
            Assert.AreEqual(5, _viewModel.SelectedItems.Count);
        }

        /// <summary>
        /// 测试筛选功能
        /// </summary>
        [TestMethod]
        public async Task FilterData_ShouldFilterTestResults()
        {
            // Arrange - 准备测试数据
            _testResultRepository.InitData();
            await _viewModel.LoadData();

            // 测试项目名筛选
            _viewModel.condition = new ConditionModel
            {
                ProjectName = "血红蛋白"
            };
            await _viewModel.LoadData();
            Assert.IsTrue(_viewModel.TestResults.All(tr => tr.Project.ProjectName == "血红蛋白"));

            // 测试结果筛选
            _viewModel.condition = new ConditionModel
            {
                TestVerdict = "阳性"
            };
            await _viewModel.LoadData();
            Assert.IsTrue(_viewModel.TestResults.All(tr => tr.TestVerdict == "阳性"));

            // 测试姓名筛选
            _viewModel.condition = new ConditionModel
            {
                PatientName = "张三"
            };
            await _viewModel.LoadData();
            Assert.IsTrue(_viewModel.TestResults.All(tr => tr.Patient.PatientName == "张三"));

            // 测试时间范围筛选
            DateTime now = DateTime.Now;
            _viewModel.condition = new ConditionModel
            {
                TestTimeMin = now.AddMonths(-1),
                TestTimeMax = now.AddMonths(1)
            };
            await _viewModel.LoadData();
            Assert.IsTrue(_viewModel.TestResults.All(tr => 
                tr.TestTime >= _viewModel.condition.TestTimeMin && 
                tr.TestTime <= _viewModel.condition.TestTimeMax));

            // 测试浓度范围筛选
            _viewModel.condition = new ConditionModel
            {
                ConcentrationMin = 100,
                ConcentrationMax = 300
            };
            await _viewModel.LoadData();
            Assert.IsTrue(_viewModel.TestResults.All(tr => {
                double concentration;
                return double.TryParse(tr.Con, out concentration) && 
                       concentration >= _viewModel.condition.ConcentrationMin && 
                       concentration <= _viewModel.condition.ConcentrationMax;
            }));

            // 测试条码筛选
            _viewModel.condition = new ConditionModel
            {
                Barcode = "BarCode1"
            };
            await _viewModel.LoadData();
            Assert.IsTrue(_viewModel.TestResults.All(tr => tr.Barcode.Contains("BarCode1")));

            // 测试多条件组合筛选
            _viewModel.condition = new ConditionModel
            {
                ProjectName = "血红蛋白",
                TestVerdict = "阳性",
                PatientName = "张三"
            };
            await _viewModel.LoadData();
            Assert.IsTrue(_viewModel.TestResults.All(tr => 
                tr.Project.ProjectName == "血红蛋白" && 
                tr.TestVerdict == "阳性" && 
                tr.Patient.PatientName == "张三"));
        }
    }
}
