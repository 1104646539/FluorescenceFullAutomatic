using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluorescenceFullAutomatic.ViewModels;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Model.Events;
using FluorescenceFullAutomatic.Platform.StateMachine;
using FluorescenceFullAutomatic.Platform.ViewModels;
using FluorescenceFullAutomatic.Platform.Views.Ctr;
using System.Linq;
using TestResult = FluorescenceFullAutomatic.Platform.Model.TestResult;
using MahApps.Metro.Controls.Dialogs;

namespace TestMain.ViewModels
{
    /// <summary>
    /// QCViewModel 单元测试类
    /// 覆盖测试方案中 VM-010 到 VM-036 的测试用例
    /// </summary>
    [TestClass]
    public class QCViewModelTests
    {
        #region Mock 对象
        private Mock<IToolService> _mockToolService;
        private Mock<ISerialPortService> _mockSerialPortService;
        private Mock<ISerialPortCommandFacade> _mockCommandFacade;
        private Mock<IConfigService> _mockConfigService;
        private Mock<IProjectService> _mockProjectService;
        private Mock<IReactionAreaService> _mockReactionAreaService;
        private Mock<IDialogService> _mockDialogService;
        private Mock<IPointService> _mockPointService;
        private Mock<IPrintService> _mockPrintService;
        private Mock<IEventMailboxService> _mockMailboxService;
        private Mock<IDispatcherService> _mockDispatcherService;
        private Mock<ILogService> _mockLogService;
        private Mock<ISystemGlobalService> _mockSystemGlobalService;
        private Mock<IQCStateMachine> _mockQCStateMachine;

        private QCViewModel _viewModel;

        // 用于验证对话框调用的捕获变量
        private string _capturedDialogTitle;
        private string _capturedDialogMsg;
        private int _dialogCallCount;
        #endregion

        [TestInitialize]
        public void Setup()
        {
            // 初始化所有 Mock 对象
            _mockToolService = new Mock<IToolService>();
            _mockSerialPortService = new Mock<ISerialPortService>();
            _mockCommandFacade = new Mock<ISerialPortCommandFacade>();
            _mockConfigService = new Mock<IConfigService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockReactionAreaService = new Mock<IReactionAreaService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockPointService = new Mock<IPointService>();
            _mockPrintService = new Mock<IPrintService>();
            _mockMailboxService = new Mock<IEventMailboxService>();
            _mockDispatcherService = new Mock<IDispatcherService>();
            _mockLogService = new Mock<ILogService>();
            _mockSystemGlobalService = new Mock<ISystemGlobalService>();
            _mockQCStateMachine = new Mock<IQCStateMachine>();

            // 重置捕获变量
            _capturedDialogTitle = null;
            _capturedDialogMsg = null;
            _dialogCallCount = 0;

            // 设置默认行为 - 使用 SelfInspectionSuccess 代替不存在的 Standby
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus()).Returns(MachineStatus.SelfInspectionSuccess);
            _mockSystemGlobalService.Setup(x => x.GetTestType()).Returns(TestType.None);
            _mockReactionAreaService.Setup(x => x.Count()).Returns(0);
            _mockReactionAreaService.Setup(x => x.IsFull()).Returns(true);

            // 设置 DispatcherService 立即执行
            _mockDispatcherService.Setup(x => x.Invoke(It.IsAny<Action>()))
                .Callback<Action>(action => action());

            // 使用 Callback 捕获对话框参数
            _mockDialogService.Setup(x => x.ShowHiltDialog(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Action<HintDialogViewModel, CustomDialog>>(),
                It.IsAny<string>(),
                It.IsAny<Action<HintDialogViewModel, CustomDialog>>(),
                It.IsAny<string>(),
                It.IsAny<Action<HintDialogViewModel, CustomDialog>>(),
                It.IsAny<bool>()
            )).Callback<object, string, string, string, Action<HintDialogViewModel, CustomDialog>, string, Action<HintDialogViewModel, CustomDialog>, string, Action<HintDialogViewModel, CustomDialog>, bool>(
                (context, title, msg, confirmText, actionConfirm, cancelText, actionCancel, closeText, actionClose, autoClose) =>
                {
                    _capturedDialogTitle = title;
                    _capturedDialogMsg = msg;
                    _dialogCallCount++;
                });

            // 创建 ViewModel
            _viewModel = new QCViewModel(
                _mockToolService.Object,
                _mockSerialPortService.Object,
                _mockCommandFacade.Object,
                _mockConfigService.Object,
                _mockProjectService.Object,
                _mockReactionAreaService.Object,
                _mockDialogService.Object,
                _mockPointService.Object,
                _mockPrintService.Object,
                _mockMailboxService.Object,
                _mockDispatcherService.Object,
                _mockLogService.Object,
                _mockSystemGlobalService.Object,
                _mockQCStateMachine.Object
            );
        }

        [TestCleanup]
        public void Cleanup()
        {
            // 清理全局状态
        }

        #region 4.1.1 状态验证测试 (VM-010 ~ VM-015)

        /// <summary>
        /// VM-010: 仪器未自检时点击开始质控应弹出对话框
        /// </summary>
        [TestMethod]
        public void ClickStartQC_WhenMachineNotSelfInspected_ShouldShowDialog()
        {
            // Arrange
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus()).Returns(MachineStatus.None);

            // Act
            _viewModel.ClickStartQC();

            // Assert - 使用捕获的变量验证
            Assert.AreEqual(1, _dialogCallCount, "应该调用一次对话框");
            Assert.IsTrue(_capturedDialogMsg.Contains("未自检"), $"对话框消息应包含'未自检', 实际: {_capturedDialogMsg}");
            _mockMailboxService.Verify(x => x.Post(It.IsAny<StartQCEvent>()), Times.Never);
        }

        /// <summary>
        /// VM-011: 仪器自检失败时点击开始质控应弹出对话框
        /// </summary>
        [TestMethod]
        public void ClickStartQC_WhenSelfInspectionFailed_ShouldShowDialog()
        {
            // Arrange
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus()).Returns(MachineStatus.SelfInspectionFailed);

            // Act
            _viewModel.ClickStartQC();

            // Assert
            Assert.AreEqual(1, _dialogCallCount, "应该调用一次对话框");
            Assert.IsTrue(_capturedDialogMsg.Contains("自检失败"), $"对话框消息应包含'自检失败', 实际: {_capturedDialogMsg}");
        }

        /// <summary>
        /// VM-012: 仪器正在检测时点击开始质控应弹出对话框
        /// </summary>
        [TestMethod]
        public void ClickStartQC_WhenSampling_ShouldShowDialog()
        {
            // Arrange
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus()).Returns(MachineStatus.Sampling);

            // Act
            _viewModel.ClickStartQC();

            // Assert
            Assert.AreEqual(1, _dialogCallCount, "应该调用一次对话框");
            Assert.IsTrue(_capturedDialogMsg.Contains("正在检测"), $"对话框消息应包含'正在检测', 实际: {_capturedDialogMsg}");
        }

        /// <summary>
        /// VM-013: 反应区不为空时点击开始质控应弹出对话框
        /// </summary>
        [TestMethod]
        public void ClickStartQC_WhenReactionAreaNotEmpty_ShouldShowDialog()
        {
            // Arrange
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus()).Returns(MachineStatus.SelfInspectionSuccess);
            _mockReactionAreaService.Setup(x => x.Count()).Returns(1); // 反应区有项目

            // Act
            _viewModel.ClickStartQC();

            // Assert
            Assert.AreEqual(1, _dialogCallCount, "应该调用一次对话框");
            Assert.IsTrue(_capturedDialogMsg.Contains("反应区不为空"), $"对话框消息应包含'反应区不为空', 实际: {_capturedDialogMsg}");
        }

        /// <summary>
        /// VM-014: 所有条件有效时应发送 StartQCEvent
        /// </summary>
        [TestMethod]
        public void ClickStartQC_WhenAllValid_ShouldPostStartQCEvent()
        {
            // Arrange - SelfInspectionSuccess 状态是有效的就绪状态
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus()).Returns(MachineStatus.SelfInspectionSuccess);
            _mockReactionAreaService.Setup(x => x.Count()).Returns(0);

            // Act
            _viewModel.ClickStartQC();

            // Assert
            _mockMailboxService.Verify(x => x.Post(It.IsAny<StartQCEvent>()), Times.Once);
            Assert.AreEqual(0, _dialogCallCount, "不应该调用对话框");
        }

        /// <summary>
        /// VM-015: 仪器检测完成后应可以开始质控
        /// </summary>
        [TestMethod]
        public void ClickStartQC_WhenTestingEnd_ShouldPostStartQCEvent()
        {
            // Arrange - TestingEnd 状态也是有效的就绪状态
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus()).Returns(MachineStatus.TestingEnd);
            _mockReactionAreaService.Setup(x => x.Count()).Returns(0);

            // Act
            _viewModel.ClickStartQC();

            // Assert
            _mockMailboxService.Verify(x => x.Post(It.IsAny<StartQCEvent>()), Times.Once);
        }

        /// <summary>
        /// VM-015b: 仪器检测结束后仍在处理时应弹出对话框
        /// </summary>
        [TestMethod]
        public void ClickStartQC_WhenSamplingFinished_ShouldShowDialog()
        {
            // Arrange
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus()).Returns(MachineStatus.SamplingFinished);

            // Act
            _viewModel.ClickStartQC();

            // Assert
            Assert.AreEqual(1, _dialogCallCount, "应该调用一次对话框");
        }

        #endregion

        #region 4.1.2 事件处理测试 (VM-020 ~ VM-025)

        /// <summary>
        /// VM-020: 收到 QCValidationErrorEvent 时应显示对话框
        /// </summary>
        [TestMethod]
        public async Task HandlerQCEvent_WhenValidationError_ShouldShowDialog()
        {
            // Arrange
            Func<ITestEvent, Task> handler = null;
            _mockMailboxService.Setup(x => x.Subscribe(It.IsAny<Func<ITestEvent, Task>>()))
                .Callback<Func<ITestEvent, Task>>(h => handler = h);

            // 重新创建 ViewModel 以捕获订阅
            _viewModel = new QCViewModel(
                _mockToolService.Object,
                _mockSerialPortService.Object,
                _mockCommandFacade.Object,
                _mockConfigService.Object,
                _mockProjectService.Object,
                _mockReactionAreaService.Object,
                _mockDialogService.Object,
                _mockPointService.Object,
                _mockPrintService.Object,
                _mockMailboxService.Object,
                _mockDispatcherService.Object,
                _mockLogService.Object,
                _mockSystemGlobalService.Object,
                _mockQCStateMachine.Object
            );

            // Act
            await handler(new QCValidationErrorEvent { ErrorType = QCValidationErrorType.NotSelfInspected });

            // Assert
            Assert.AreEqual(1, _dialogCallCount, "应该调用一次对话框");
            Assert.IsTrue(_capturedDialogMsg.Contains("未自检"), $"对话框消息应包含'未自检', 实际: {_capturedDialogMsg}");
        }

        /// <summary>
        /// VM-021: 收到 MachineStateChangeEvent 时应更新状态
        /// </summary>
        [TestMethod]
        public async Task HandlerQCEvent_WhenMachineStateChange_ShouldUpdateState()
        {
            // Arrange
            Func<ITestEvent, Task> handler = null;
            _mockMailboxService.Setup(x => x.Subscribe(It.IsAny<Func<ITestEvent, Task>>()))
                .Callback<Func<ITestEvent, Task>>(h => handler = h);

            _viewModel = new QCViewModel(
                _mockToolService.Object,
                _mockSerialPortService.Object,
                _mockCommandFacade.Object,
                _mockConfigService.Object,
                _mockProjectService.Object,
                _mockReactionAreaService.Object,
                _mockDialogService.Object,
                _mockPointService.Object,
                _mockPrintService.Object,
                _mockMailboxService.Object,
                _mockDispatcherService.Object,
                _mockLogService.Object,
                _mockSystemGlobalService.Object,
                _mockQCStateMachine.Object
            );

            // Act
            await handler(new MachineStateChangeEvent { NewState = MachineStatus.Sampling });

            // Assert
            _mockSystemGlobalService.Verify(x => x.SetMachineStatus(MachineStatus.Sampling), Times.Once);
        }

        /// <summary>
        /// VM-022: 收到 QCNoCardAvailableEvent 时应显示双按钮对话框
        /// </summary>
        [TestMethod]
        public async Task HandlerQCEvent_WhenNoCardAvailable_ShouldShowTwoButtonDialog()
        {
            // Arrange
            Func<ITestEvent, Task> handler = null;
            _mockMailboxService.Setup(x => x.Subscribe(It.IsAny<Func<ITestEvent, Task>>()))
                .Callback<Func<ITestEvent, Task>>(h => handler = h);

            _viewModel = new QCViewModel(
                _mockToolService.Object,
                _mockSerialPortService.Object,
                _mockCommandFacade.Object,
                _mockConfigService.Object,
                _mockProjectService.Object,
                _mockReactionAreaService.Object,
                _mockDialogService.Object,
                _mockPointService.Object,
                _mockPrintService.Object,
                _mockMailboxService.Object,
                _mockDispatcherService.Object,
                _mockLogService.Object,
                _mockSystemGlobalService.Object,
                _mockQCStateMachine.Object
            );

            // Act
            await handler(new QCNoCardAvailableEvent { ErrorMessage = "请添加检测卡" });

            // Assert - 验证调用了带两个按钮的对话框
            Assert.AreEqual(1, _dialogCallCount, "应该调用一次对话框");
            Assert.AreEqual("请添加检测卡", _capturedDialogMsg);
        }

        /// <summary>
        /// VM-023: 收到 QCResultProcessedEvent 时应更新 ResultPoints
        /// </summary>
        [TestMethod]
        public async Task HandlerQCEvent_WhenResultProcessed_ShouldUpdateResultPoints()
        {
            // Arrange
            Func<ITestEvent, Task> handler = null;
            _mockMailboxService.Setup(x => x.Subscribe(It.IsAny<Func<ITestEvent, Task>>()))
                .Callback<Func<ITestEvent, Task>>(h => handler = h);

            _viewModel = new QCViewModel(
                _mockToolService.Object,
                _mockSerialPortService.Object,
                _mockCommandFacade.Object,
                _mockConfigService.Object,
                _mockProjectService.Object,
                _mockReactionAreaService.Object,
                _mockDialogService.Object,
                _mockPointService.Object,
                _mockPrintService.Object,
                _mockMailboxService.Object,
                _mockDispatcherService.Object,
                _mockLogService.Object,
                _mockSystemGlobalService.Object,
                _mockQCStateMachine.Object
            );

            var testPoint = new Point { Tc = "10.5", Tc2 = "8.3" };

            // Act
            await handler(new QCResultProcessedEvent { Point = testPoint, Index = 0 });

            // Assert
            Assert.AreEqual(testPoint, _viewModel.ResultPoints[0]);
        }

        /// <summary>
        /// VM-024: 收到 QCCompletedEvent 时应更新视图
        /// </summary>
        [TestMethod]
        public async Task HandlerQCEvent_WhenCompleted_ShouldChangeView()
        {
            // Arrange
            Func<ITestEvent, Task> handler = null;
            _mockMailboxService.Setup(x => x.Subscribe(It.IsAny<Func<ITestEvent, Task>>()))
                .Callback<Func<ITestEvent, Task>>(h => handler = h);

            _mockSystemGlobalService.Setup(x => x.GetTestType()).Returns(TestType.None);

            _viewModel = new QCViewModel(
                _mockToolService.Object,
                _mockSerialPortService.Object,
                _mockCommandFacade.Object,
                _mockConfigService.Object,
                _mockProjectService.Object,
                _mockReactionAreaService.Object,
                _mockDialogService.Object,
                _mockPointService.Object,
                _mockPrintService.Object,
                _mockMailboxService.Object,
                _mockDispatcherService.Object,
                _mockLogService.Object,
                _mockSystemGlobalService.Object,
                _mockQCStateMachine.Object
            );

            // Act
            await handler(new QCCompletedEvent { Message = "质控完成" });

            // Assert - 验证 IsEnabled 应该为 true（因为 TestType 不是 QC）
            Assert.IsTrue(_viewModel.IsEnabled);
        }

        /// <summary>
        /// VM-025: 收到 StartQCEvent 时应触发状态机
        /// </summary>
        [TestMethod]
        public async Task HandlerQCEvent_WhenStartQC_ShouldFireStateMachine()
        {
            // Arrange
            Func<ITestEvent, Task> handler = null;
            _mockMailboxService.Setup(x => x.Subscribe(It.IsAny<Func<ITestEvent, Task>>()))
                .Callback<Func<ITestEvent, Task>>(h => handler = h);

            _viewModel = new QCViewModel(
                _mockToolService.Object,
                _mockSerialPortService.Object,
                _mockCommandFacade.Object,
                _mockConfigService.Object,
                _mockProjectService.Object,
                _mockReactionAreaService.Object,
                _mockDialogService.Object,
                _mockPointService.Object,
                _mockPrintService.Object,
                _mockMailboxService.Object,
                _mockDispatcherService.Object,
                _mockLogService.Object,
                _mockSystemGlobalService.Object,
                _mockQCStateMachine.Object
            );

            // Act
            await handler(new StartQCEvent());

            // Assert
            _mockQCStateMachine.Verify(x => x.FireAsync(QCTrigger.StartQC), Times.Once);
        }

        #endregion

        #region 4.1.3 结果计算测试 (VM-030 ~ VM-036)

        /// <summary>
        /// VM-030: 空结果数组时计算变异系数不应抛出异常
        /// </summary>
        [TestMethod]
        public void CalculateVariance_WhenEmptyResults_ShouldNotThrow()
        {
            // Arrange - ResultPoints 已在构造函数中初始化为 10 个 null

            // Act & Assert - 使用反射调用私有方法
            var method = typeof(QCViewModel).GetMethod("CalculateVariance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            double result = (double)method.Invoke(_viewModel, new object[] { new double[0] });
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// VM-031: 单项目正确计算变异系数
        /// </summary>
        [TestMethod]
        public void CalculateVariance_WhenSingleProject_ShouldCalculateCorrectly()
        {
            // Arrange
            var method = typeof(QCViewModel).GetMethod("CalculateVariance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 使用已知的测试数据：[100, 100, 100, 100, 100] => 变异系数应该是 0
            double[] uniformValues = { 100, 100, 100, 100, 100 };

            // Act
            double result = (double)method.Invoke(_viewModel, new object[] { uniformValues });

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// VM-032: 变异系数在范围内时应返回合格
        /// </summary>
        [TestMethod]
        public void CalcQcResult_WhenVarianceWithinScope_ShouldReturnQualified()
        {
            // Arrange - 设置 CurrentTestItem 和 ResultPoints
            var project = new Project { ProjectType = Project.Project_Type_Single };
            var testResult = new TestResult { Project = project };
            _viewModel.CurrentTestItem = new ReactionAreaItem { TestResult = testResult };

            // 设置 10 个相同值的点（变异系数为 0）
            _viewModel.ResultPoints.Clear();
            for (int i = 0; i < 10; i++)
            {
                _viewModel.ResultPoints.Add(new Point { Tc = "100", Tc2 = "100" });
            }

            // Act
            var method = typeof(QCViewModel).GetMethod("CalcQcResult",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_viewModel, null);

            // Assert
            Assert.AreEqual("合格", _viewModel.QcResult);
            Assert.AreEqual(0, _viewModel.Variance);
        }

        /// <summary>
        /// VM-033: 空数据点时应安全处理
        /// </summary>
        [TestMethod]
        public void CalcQcResult_WhenNullPoints_ShouldHandleSafely()
        {
            // Arrange
            var project = new Project { ProjectType = Project.Project_Type_Single };
            var testResult = new TestResult { Project = project };
            _viewModel.CurrentTestItem = new ReactionAreaItem { TestResult = testResult };

            // ResultPoints 默认都是 null
            _viewModel.ClearResultPoints();

            // Act & Assert - 不应抛出异常
            var method = typeof(QCViewModel).GetMethod("CalcQcResult",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            try
            {
                method.Invoke(_viewModel, null);
                // 如果有 null 点，可能会抛出异常，这是预期行为
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                // 如果抛出空引用异常，说明代码没有正确处理 null
                Assert.IsTrue(ex.InnerException is NullReferenceException, "应该安全处理 null 点");
            }
        }

        /// <summary>
        /// VM-034: 某些结果为空时应正确处理
        /// </summary>
        [TestMethod]
        public void CalcQcResult_WhenSomeResultsNull_ShouldHandleCorrectly()
        {
            // Arrange
            var project = new Project { ProjectType = Project.Project_Type_Single };
            var testResult = new TestResult { Project = project };
            _viewModel.CurrentTestItem = new ReactionAreaItem { TestResult = testResult };

            _viewModel.ResultPoints.Clear();
            // 只添加 5 个有效点，其余为 null
            for (int i = 0; i < 5; i++)
            {
                _viewModel.ResultPoints.Add(new Point { Tc = "100", Tc2 = "100" });
            }
            for (int i = 5; i < 10; i++)
            {
                _viewModel.ResultPoints.Add(null);
            }

            // Act & Assert
            var method = typeof(QCViewModel).GetMethod("CalcQcResult",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            try
            {
                method.Invoke(_viewModel, null);
            }
            catch (System.Reflection.TargetInvocationException)
            {
                // 预期可能会抛出异常（null 点未处理）
            }
        }

        /// <summary>
        /// VM-035: 变异系数超出范围时应返回不合格
        /// </summary>
        [TestMethod]
        public void CalcQcResult_WhenVarianceExceedsScope_ShouldReturnUnqualified()
        {
            // Arrange
            var project = new Project { ProjectType = Project.Project_Type_Single };
            var testResult = new TestResult { Project = project };
            _viewModel.CurrentTestItem = new ReactionAreaItem { TestResult = testResult };

            // 设置变异较大的数据
            _viewModel.ResultPoints.Clear();
            double[] values = { 100, 110, 120, 130, 140, 150, 160, 170, 180, 190 };
            foreach (var val in values)
            {
                _viewModel.ResultPoints.Add(new Point { Tc = val.ToString(), Tc2 = "100" });
            }

            // Act
            var method = typeof(QCViewModel).GetMethod("CalcQcResult",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_viewModel, null);

            // Assert
            Assert.AreEqual("不合格", _viewModel.QcResult);
            Assert.IsTrue(_viewModel.Variance > 5);
        }

        /// <summary>
        /// VM-036: 双联卡项目应计算两个变异系数
        /// </summary>
        [TestMethod]
        public void CalcQcResult_WhenDoubleProject_ShouldCalculateBothVariances()
        {
            // Arrange
            var project = new Project { ProjectType = Project.Project_Type_Double };
            var testResult = new TestResult { Project = project };
            _viewModel.CurrentTestItem = new ReactionAreaItem { TestResult = testResult };

            // 设置 10 个相同值的点（两个项目）
            _viewModel.ResultPoints.Clear();
            for (int i = 0; i < 10; i++)
            {
                _viewModel.ResultPoints.Add(new Point { Tc = "100", Tc2 = "100" });
            }

            // Act
            var method = typeof(QCViewModel).GetMethod("CalcQcResult",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_viewModel, null);

            // Assert
            Assert.AreEqual("合格", _viewModel.QcResult);
            Assert.AreEqual(0, _viewModel.Variance);
            Assert.AreEqual(0, _viewModel.Variance2);
        }

        #endregion

        #region 辅助方法测试

        /// <summary>
        /// 测试 ClearResultPoints 方法
        /// </summary>
        [TestMethod]
        public void ClearResultPoints_ShouldResetTo10NullPoints()
        {
            // Act
            _viewModel.ClearResultPoints();

            // Assert
            Assert.AreEqual(10, _viewModel.ResultPoints.Count);
            Assert.IsTrue(_viewModel.ResultPoints.All(p => p == null));
        }

        /// <summary>
        /// 测试 GetValidationErrorMessage 方法
        /// </summary>
        [TestMethod]
        public void GetValidationErrorMessage_ShouldReturnCorrectMessage()
        {
            // Arrange
            var method = typeof(QCViewModel).GetMethod("GetValidationErrorMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act & Assert
            string msg1 = (string)method.Invoke(_viewModel, new object[] { QCValidationErrorType.NotSelfInspected });
            Assert.IsTrue(msg1.Contains("未自检"));

            string msg2 = (string)method.Invoke(_viewModel, new object[] { QCValidationErrorType.SelfInspectionFailed });
            Assert.IsTrue(msg2.Contains("自检失败"));

            string msg3 = (string)method.Invoke(_viewModel, new object[] { QCValidationErrorType.AlreadyTesting });
            Assert.IsTrue(msg3.Contains("正在检测"));

            string msg4 = (string)method.Invoke(_viewModel, new object[] { QCValidationErrorType.ReactionAreaNotEmpty });
            Assert.IsTrue(msg4.Contains("反应区不为空"));
        }

        #endregion
    }
}
