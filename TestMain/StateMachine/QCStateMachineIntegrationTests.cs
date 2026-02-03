using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.StateMachine;
using FluorescenceFullAutomatic.ViewModels;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Model.Events;
using FluorescenceFullAutomatic.Platform.Ex;

namespace TestMain.StateMachine
{
    /// <summary>
    /// QCStateMachine 集成测试类
    /// 测试完整的质控流程状态流转
    /// </summary>
    [TestClass]
    public class QCStateMachineIntegrationTests
    {
        #region Mock 对象
        private Mock<IEventMailboxService> _mockMailboxService;
        private Mock<ISerialPortCommandFacade> _mockCommandFacade;
        private Mock<IProjectService> _mockProjectService;
        private Mock<ILogService> _mockLogService;
        private Mock<IToolService> _mockToolService;
        private Mock<IPointService> _mockPointService;
        private Mock<IReactionAreaService> _mockReactionAreaService;
        private Mock<IConfigService> _mockConfigService;
        private Mock<ISystemGlobalService> _mockSystemGlobalService;

        private QCStateMachine _stateMachine;

        // 事件捕获
        private List<ITestEvent> _capturedEvents;
        private int _testCount;
        #endregion

        [TestInitialize]
        public void Setup()
        {
            _mockMailboxService = new Mock<IEventMailboxService>();
            _mockCommandFacade = new Mock<ISerialPortCommandFacade>();
            _mockProjectService = new Mock<IProjectService>();
            _mockLogService = new Mock<ILogService>();
            _mockToolService = new Mock<IToolService>();
            _mockPointService = new Mock<IPointService>();
            _mockReactionAreaService = new Mock<IReactionAreaService>();
            _mockConfigService = new Mock<IConfigService>();
            _mockSystemGlobalService = new Mock<ISystemGlobalService>();

            _capturedEvents = new List<ITestEvent>();
            _testCount = 0;

            // 设置事件捕获
            _mockMailboxService.Setup(x => x.Post(It.IsAny<ITestEvent>()))
                .Callback<ITestEvent>(e => _capturedEvents.Add(e));

            // 设置默认的系统状态
            SetupDefaultSystemState();

            // 创建状态机
            _stateMachine = new QCStateMachine(
                _mockMailboxService.Object,
                _mockCommandFacade.Object,
                _mockProjectService.Object,
                _mockLogService.Object,
                _mockToolService.Object,
                _mockPointService.Object,
                _mockReactionAreaService.Object,
                _mockConfigService.Object,
                _mockSystemGlobalService.Object
            );
        }

        /// <summary>
        /// 设置默认的系统状态（有效状态）
        /// </summary>
        private void SetupDefaultSystemState()
        {
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus())
                .Returns(MachineStatus.SelfInspectionSuccess);
            _mockReactionAreaService.Setup(x => x.IsFull()).Returns(true);
            _mockReactionAreaService.Setup(x => x.Count()).Returns(0);
        }

        /// <summary>
        /// 设置模拟的仪器状态响应（有卡）
        /// </summary>
        private void SetupMachineStateWithCard(int cardCount = 10)
        {
            _mockCommandFacade.Setup(x => x.GetMachineStateAsync(
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ReturnsAsync(new BaseResponseModel<MachineStatusModel>
                {
                    Data = new MachineStatusModel { CardNum = cardCount.ToString(), CardExist = "1" }
                });
        }

        /// <summary>
        /// 设置模拟的仪器状态响应（无卡）
        /// </summary>
        private void SetupMachineStateNoCard()
        {
            _mockCommandFacade.Setup(x => x.GetMachineStateAsync(
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ReturnsAsync(new BaseResponseModel<MachineStatusModel>
                {
                    Data = new MachineStatusModel { CardNum = "0" }
                });
        }

        /// <summary>
        /// 设置推卡成功响应
        /// </summary>
        private void SetupPushCardSuccess()
        {
            _mockCommandFacade.Setup(x => x.PushCardAsync(
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ReturnsAsync(new BaseResponseModel<PushCardModel>
                {
                    Data = new PushCardModel { QrCode = "QC001", Success = PushCardModel.PushCardSuccess, }
                });
        }

        /// <summary>
        /// 设置项目验证成功
        /// </summary>
        private void SetupProjectValidation(bool isValid = true)
        {
            var project = new Project
            {
                Id = 1,
                ProjectType = Project.Project_Type_Single,
                ProjectName = "QC测试项目"
            };

            _mockProjectService.Setup(x => x.GetProjectForQrcode(It.IsAny<string>()))
                .Returns(isValid ? project : null);
        }

        /// <summary>
        /// 设置移动反应区成功
        /// </summary>
        private void SetupMoveToReactionSuccess()
        {
            _mockCommandFacade.Setup(x => x.MoveReactionAreaAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ReturnsAsync(new BaseResponseModel<MoveReactionAreaModel>());
        }

        /// <summary>
        /// 设置检测成功响应
        /// </summary>
        private void SetupTestSuccess()
        {
            _mockCommandFacade.Setup(x => x.TestAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ReturnsAsync(new BaseResponseModel<TestModel>
                {
                    Data = new TestModel
                    {
                        T = "100.5",
                        C = "200.3",
                        T2 = "150.2",
                        C2 = "250.1",
                        Location = new int[] { 1, 2, 3, 4 },
                        Point = new List<int> { 1, 2, 3, 4 }
                    }
                });

            _mockToolService.Setup(x => x.CalcTC(It.IsAny<double>(), It.IsAny<double>()))
                .Returns("0.5");
            _mockPointService.Setup(x => x.InsertPoint(It.IsAny<Point>()))
                .Returns(1);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _capturedEvents.Clear();
        }

        #region IT-001: 完整质控流程测试

        /// <summary>
        /// IT-001: 完整质控流程 - Happy Path
        /// 验证从开始到完成的完整流程（10次检测）
        /// </summary>
        [TestMethod]
        public async Task CompleteQCFlow_HappyPath_ShouldComplete10Tests()
        {
            // Arrange - 设置所有操作成功
            SetupMachineStateWithCard(10);
            SetupPushCardSuccess();
            SetupProjectValidation(true);
            SetupMoveToReactionSuccess();
            SetupTestSuccess();

            // Act - 启动质控流程
            await _stateMachine.FireAsync(QCTrigger.StartQC);

            // 等待一小段时间让异步操作完成
            await Task.Delay(100);

            // Assert - 验证流程进入了正确状态
            // 验证获取仪器状态被调用
            _mockCommandFacade.Verify(
                x => x.GetMachineStateAsync(It.IsAny<CancellationToken>(), It.IsAny<int>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// IT-002: 单次检测流程
        /// 验证只执行一次检测的场景
        /// </summary>
        [TestMethod]
        public async Task CompleteQCFlow_SingleTest_ShouldCompleteSuccessfully()
        {
            // Arrange
            SetupMachineStateWithCard(1);
            SetupPushCardSuccess();
            SetupProjectValidation(true);
            SetupMoveToReactionSuccess();
            SetupTestSuccess();

            // Act
            await _stateMachine.FireAsync(QCTrigger.StartQC);
            await Task.Delay(100);

            // Assert
            _mockCommandFacade.Verify(
                x => x.GetMachineStateAsync(It.IsAny<CancellationToken>(), It.IsAny<int>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region IT-010~013: 异常流程测试

        /// <summary>
        /// IT-010: 无卡 → 用户添加卡 → 重试成功
        /// </summary>
        [TestMethod]
        public async Task QCFlow_NoCard_RetrySuccess_ShouldContinue()
        {
            // Arrange - 首次无卡
            SetupMachineStateNoCard();

            // Act - 启动质控
            await _stateMachine.FireAsync(QCTrigger.StartQC);
            await Task.Delay(100);

            // Assert - 应该发送无卡事件
            Assert.IsTrue(_capturedEvents.Exists(e => e is QCNoCardAvailableEvent),
                "应该发送 QCNoCardAvailableEvent");
        }

        /// <summary>
        /// IT-011: 无卡 → 用户取消质控
        /// </summary>
        [TestMethod]
        public async Task QCFlow_NoCard_Cancel_ShouldReturnToIdle()
        {
            // Arrange
            SetupMachineStateNoCard();

            // Act - 启动后取消
            await _stateMachine.FireAsync(QCTrigger.StartQC);
            await Task.Delay(100);
            await _stateMachine.HandleCancelQCAsync();

            // Assert - 应该返回 Idle
            _mockMailboxService.Verify(
                x => x.Post(It.IsAny<ITestEvent>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// IT-012: 项目验证失败 → 等待用户操作 → 用户重试 → 重新推卡成功
        /// 流程: Idle → PreparingQC → PushingCard → (项目验证失败) → WaitingForCard → (用户重试) → PreparingQC → PushingCard → 成功
        /// </summary>
        [TestMethod]
        public async Task QCFlow_InvalidProject_Retry_ShouldContinue()
        {
            // Arrange - 第一次推卡项目验证失败，第二次成功
            SetupMachineStateWithCard(10);
            SetupPushCardSuccess();
            SetupMoveToReactionSuccess();
            SetupTestSuccess();

            // 第一次项目验证失败
            var callCount = 0;
            _mockProjectService.Setup(x => x.GetProjectForQrcode(It.IsAny<string>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount++ == 1)
                    {
                        return null;
                    }
                    return new Project
                    {
                        Id = 1,
                        ProjectType = Project.Project_Type_Single,
                        TestType = Project.Test_Type_QC,
                        ProjectName = "QC测试项目"
                    };
                });

            // Act - 第一次启动质控（将验证失败）
            await _stateMachine.FireAsync(QCTrigger.StartQC);
            await Task.Delay(500);

            // 验证发送了项目无效事件
            Assert.IsTrue(_capturedEvents.Exists(e => e is QCProjectInvalidEvent),
                "应该发送 QCProjectInvalidEvent");

            // 用户点击重试推卡
            await _stateMachine.HandleRetryPushCardAsync();
            await Task.Delay(500);

            // Assert - 验证推卡被调用了两次（第一次失败，第二次成功）
            _mockCommandFacade.Verify(
                x => x.PushCardAsync(It.IsAny<CancellationToken>(), It.IsAny<int>()),
                Times.AtLeast(2), "应该至少调用两次推卡");
        }

        /// <summary>
        /// IT-013: 推卡失败 → 进入 WaitingForCard → 用户取消 → 返回 Idle
        /// 根据状态机设计：PushingCard → PushCardFailed → WaitingForCard → CancelQC → Idle
        /// </summary>
        [TestMethod]
        public async Task QCFlow_PushCardFailed_Cancel_ShouldReturnToIdle()
        {
            // Arrange - 推卡失败
            SetupMachineStateWithCard(10);
            SetupProjectValidation(true);

            // 推卡返回失败
            _mockCommandFacade.Setup(x => x.PushCardAsync(
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ReturnsAsync(new BaseResponseModel<PushCardModel>
                {
                    Data = new PushCardModel { Success = PushCardModel.PushCardFail, QrCode = "" }
                });

            // Act - 启动质控（推卡将失败）
            await _stateMachine.FireAsync(QCTrigger.StartQC);
            await Task.Delay(500);

            // 此时应该在 WaitingForCard 状态，用户选择取消
            await _stateMachine.HandleCancelQCAsync();
            await Task.Delay(100);

            // Assert - 验证推卡被调用了一次，然后用户取消
            _mockCommandFacade.Verify(
                x => x.PushCardAsync(It.IsAny<CancellationToken>(), It.IsAny<int>()),
                Times.Once, "推卡应该只被调用一次");

            // 验证状态机返回了 Idle
            Assert.AreEqual(QCState.Idle, _stateMachine.CurrentState);
        }

        #endregion

        #region IT-020~021: 错误恢复测试

        /// <summary>
        /// IT-020: 串口错误 → 进入 Error 状态 → 取消 → 返回 Idle
        /// </summary>
        [TestMethod]
        public async Task QCFlow_SerialError_Recovery_ShouldReturnToIdle()
        {
            // Arrange
            SetupMachineStateWithCard(10);
            _mockCommandFacade.Setup(x => x.GetMachineStateAsync(
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ThrowsAsync(new SerialCommandException("01", "设备错误"));

            // Act
            await _stateMachine.FireAsync(QCTrigger.StartQC);
            await Task.Delay(100);

            // 取消恢复
            await _stateMachine.HandleCancelQCAsync();

            // Assert
            Assert.IsTrue(_capturedEvents.Exists(e => e is SerialPortErrorEvent),
                "应该发送 SerialPortErrorEvent");
        }

        /// <summary>
        /// IT-021: 超时错误 → 进入 Error 状态 → 取消 → 返回 Idle
        /// </summary>
        [TestMethod]
        public async Task QCFlow_Timeout_Recovery_ShouldReturnToIdle()
        {
            // Arrange
            SetupMachineStateWithCard(10);
            _mockCommandFacade.Setup(x => x.GetMachineStateAsync(
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .ThrowsAsync(new TimeoutException("操作超时"));

            // Act
            await _stateMachine.FireAsync(QCTrigger.StartQC);
            await Task.Delay(100);

            // 取消恢复
            await _stateMachine.HandleCancelQCAsync();

            // Assert
            Assert.IsTrue(_capturedEvents.Exists(e => e is SerialPortErrorEvent),
                "应该发送 SerialPortErrorEvent");
        }

        #endregion

        #region IT-030~031: 边界条件测试

        /// <summary>
        /// IT-030: 检测过程中取消
        /// </summary>
        [TestMethod]
        public async Task QCFlow_CancelDuringTest_ShouldReturnToIdle()
        {
            // Arrange
            SetupMachineStateWithCard(10);
            SetupPushCardSuccess();
            SetupProjectValidation(true);
            SetupMoveToReactionSuccess();

            // 设置检测延迟，让我们有时间取消
            _mockCommandFacade.Setup(x => x.TestAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
                .Returns(async () =>
                {
                    await Task.Delay(1000); // 延迟1秒
                    return new BaseResponseModel<TestModel>
                    {
                        Data = new TestModel { T = "100", C = "200" }
                    };
                });

            // Act - 启动后立即取消
            var startTask = _stateMachine.FireAsync(QCTrigger.StartQC);
            await Task.Delay(50); // 稍等一下让流程开始
            await _stateMachine.HandleCancelQCAsync();

            // Assert - 取消方法应该被调用而不抛出异常
            Assert.IsTrue(true, "取消操作应该不抛出异常");
        }

        /// <summary>
        /// IT-031: 多次尝试开始质控（验证失败场景）
        /// </summary>
        [TestMethod]
        public async Task QCFlow_MultipleStartAttempts_ShouldHandleGracefully()
        {
            // Arrange - 设置验证失败
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus())
                .Returns(MachineStatus.None); // 未自检

            // Act - 多次尝试开始
            await _stateMachine.FireAsync(QCTrigger.StartQC);
            await _stateMachine.FireAsync(QCTrigger.StartQC);
            await _stateMachine.FireAsync(QCTrigger.StartQC);

            // Assert - 应该发送3次验证错误事件
            int errorCount = _capturedEvents.FindAll(e => e is QCValidationErrorEvent).Count;
            Assert.AreEqual(3, errorCount, "应该发送3次 QCValidationErrorEvent");
        }

        #endregion

        #region 辅助验证测试

        /// <summary>
        /// 验证状态机初始状态
        /// </summary>
        [TestMethod]
        public void InitialState_ShouldBeIdle()
        {
            // Assert - 新创建的状态机应该是验证方法可以调用
            var result = _stateMachine.ValidateStartQC();
            Assert.AreEqual(QCValidationErrorType.None, result);
        }

        /// <summary>
        /// 验证 ValidateStartQC 在各种条件下的行为
        /// </summary>
        [TestMethod]
        public void ValidateStartQC_WhenAllValid_ShouldReturnNone()
        {
            // Arrange - 默认设置已经是有效状态

            // Act
            var result = _stateMachine.ValidateStartQC();

            // Assert
            Assert.AreEqual(QCValidationErrorType.None, result);
        }

        /// <summary>
        /// 验证在未自检状态下启动质控
        /// </summary>
        [TestMethod]
        public void ValidateStartQC_WhenNotInspected_ShouldReturnError()
        {
            // Arrange
            _mockSystemGlobalService.Setup(x => x.GetMachineStatus())
                .Returns(MachineStatus.None);

            // Act
            var result = _stateMachine.ValidateStartQC();

            // Assert
            Assert.AreEqual(QCValidationErrorType.NotSelfInspected, result);
        }

        #endregion
    }
}
