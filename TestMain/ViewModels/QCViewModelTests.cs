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
using FluorescenceFullAutomatic.HomeModule.Services;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using System.Linq;
using TestResult = FluorescenceFullAutomatic.Platform.Model.TestResult;

namespace TestMain.ViewModels
{
    [TestClass]
    public class QCViewModelTests
    {
        private Mock<IToolService> _mockToolService;
        private Mock<ISerialPortService> _mockSerialPortService;
        private Mock<ISerialPortCommandFacade> _mockSerialPortCommandFacade;
        private Mock<IConfigService> _mockConfigService;
        private Mock<IProjectService> _mockProjectService;
        private Mock<IReactionAreaQueueService> _mockReactionAreaQueueService;
        private Mock<IDialogService> _mockDialogService;
        private Mock<IPointService> _mockPointService;
        private Mock<IPrintService> _mockPrintService;
        private Mock<IEventMailboxService> _mockMailboxService;
        private Mock<IDispatcherService> _mockDispatcherService;
        private Mock<ILogService> _mockLogService;

        private QCViewModel _viewModel;

        [TestInitialize]
        public void Setup()
        {
            _mockToolService = new Mock<IToolService>();
            _mockSerialPortService = new Mock<ISerialPortService>();
            _mockSerialPortCommandFacade = new Mock<ISerialPortCommandFacade>();
            _mockConfigService = new Mock<IConfigService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockReactionAreaQueueService = new Mock<IReactionAreaQueueService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockPointService = new Mock<IPointService>();
            _mockPrintService = new Mock<IPrintService>();
            _mockMailboxService = new Mock<IEventMailboxService>();
            _mockDispatcherService = new Mock<IDispatcherService>();
            _mockLogService = new Mock<ILogService>();

            // Mock dispatcher to execute actions immediately
            _mockDispatcherService.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

            _viewModel = new QCViewModel(
                _mockToolService.Object,
                _mockSerialPortService.Object,
                _mockSerialPortCommandFacade.Object,
                _mockConfigService.Object,
                _mockProjectService.Object,
                _mockReactionAreaQueueService.Object,
                _mockDialogService.Object,
                _mockPointService.Object,
                _mockPrintService.Object,
                _mockMailboxService.Object,
                _mockDispatcherService.Object,
                _mockLogService.Object
            );
        }

        [TestMethod]
        public void Constructor_InitializesCorrectly()
        {
            Assert.IsNotNull(_viewModel);
            Assert.IsNotNull(_viewModel.ResultPoints);
            Assert.AreEqual(10, _viewModel.ResultPoints.Count);
        }

        [TestMethod]
        public void ClickStartQC_WhenMachineIsSelfInspectionFailed_ShowsError()
        {
            // Arrange
            SystemGlobal.MachineStatus = MachineStatus.SelfInspectionFailed;

            // Act
            _viewModel.ClickStartQC();

            // Assert
            _mockDialogService.Verify(d => d.ShowHiltDialog(It.IsAny<object>(), "提示", "自检失败，请先自检。", "好的", It.IsAny<Action<object, DialogCoordinator>>(), null, null), Times.Once);
            _mockMailboxService.Verify(m => m.Post(It.IsAny<StartQCEvent>()), Times.Never);
        }

        [TestMethod]
        public void ClickStartQC_WhenReactionAreaIsNotEmpty_ShowsError()
        {
            // Arrange
            SystemGlobal.MachineStatus = MachineStatus.Standby;
            _mockReactionAreaQueueService.Setup(r => r.Count()).Returns(1);

            // Act
            _viewModel.ClickStartQC();

            // Assert
            _mockDialogService.Verify(d => d.ShowHiltDialog(It.IsAny<object>(), "提示", "反应区不为空，请等待检测结束。", "好的", It.IsAny<Action<object, DialogCoordinator>>(), null, null), Times.Once);
            _mockMailboxService.Verify(m => m.Post(It.IsAny<StartQCEvent>()), Times.Never);
        }

        [TestMethod]
        public void ClickStartQC_WhenMachineIsStandbyAndReactionAreaEmpty_StartsQC()
        {
            // Arrange
            SystemGlobal.MachineStatus = MachineStatus.Standby;
            _mockReactionAreaQueueService.Setup(r => r.Count()).Returns(0);

            // Act
            _viewModel.ClickStartQC();

            // Assert
            _mockMailboxService.Verify(m => m.Post(It.IsAny<StartQCEvent>()), Times.Once);
            // Verify ResultPoints are cleared/reset
            Assert.AreEqual(10, _viewModel.ResultPoints.Count);
        }

        [TestMethod]
        public void HandlerQCEventAsync_QCFinishEvent_CalculatesVariance()
        {
            // This test requires invoking the private event handler or simulating the event if using Messenger.
            // Since mailboxService.Subscribe calls a private method, we can't easily invoke it directly from here
            // without reflection or if the Messenger sends it.
            // However, the ViewModel subscribes to the mailbox service.
            // The mailbox service mock needs to be setup to trigger the callback.

            // For the sake of this test, we might use reflection to invoke HandlerQCEventAsync 
            // OR better, we can test the CalculateVariance logic if it was public or via the QCFinishEvent effect on properties.

            // Let's use Reflection to invoke the private handler for testing purposes
            var methodInfo = typeof(QCViewModel).GetMethod("HandlerQCEventAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Setup some result points
            for (int i = 0; i < 5; i++)
            {
                _viewModel.ResultPoints[i] = new Point { Tc = "100", Tc2 = "200" };
            }

            // Mock CurrentTestItem project type to Single
            var project = new Project { ProjectType = Project.Project_Type_Single };
            var testResult = new TestResult { Project = project };
            _viewModel.CurrentTestItem = new ReactionAreaItem { TestResult = testResult };


            // Act
            methodInfo.Invoke(_viewModel, new object[] { new QCFinishEvent() });

            // Assert
            Assert.IsNotNull(_viewModel.QcTime);
            Assert.AreEqual(0, _viewModel.Variance); // 100, 100, 100, 100, 100 => variance 0
            Assert.AreEqual("合格", _viewModel.QcResult);
        }

        [TestMethod]
        public void CalculateVariance_LogicCheck()
        {
            // Using reflection to test private Check logic or verify through public properties after setting points.
            // Let's set points that have variance.

            _viewModel.ResultPoints[0] = new Point { Tc = "100" };
            _viewModel.ResultPoints[1] = new Point { Tc = "102" };

            // Mock CurrentTestItem project type to Single
            var project = new Project { ProjectType = Project.Project_Type_Single };
            var testResult = new TestResult { Project = project };
            _viewModel.CurrentTestItem = new ReactionAreaItem { TestResult = testResult };

            var methodInfo = typeof(QCViewModel).GetMethod("HandlerQCEventAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            methodInfo.Invoke(_viewModel, new object[] { new QCFinishEvent() });

            // Mean = 101. Variance = Sqrt( ((100-101)^2 + (102-101)^2) / 2 ) / 101 * 100
            // = Sqrt( (1 + 1) / 2 ) / 101 * 100
            // = 1 / 101 * 100 = 0.990099...

            Assert.IsTrue(_viewModel.Variance > 0.99 && _viewModel.Variance < 1.0);
        }
    }
}
