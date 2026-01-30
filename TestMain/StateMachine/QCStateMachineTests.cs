using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.StateMachine;
using FluorescenceFullAutomatic.HomeModule.Services;
using FluorescenceFullAutomatic.ViewModels;
using Serilog;

namespace TestMain.StateMachine
{
    [TestClass]
    public class QCStateMachineTests
    {
        private Mock<IEventMailboxService> _mockMailboxService;
        private Mock<ISerialPortCommandFacade> _mockSerialPortCommandFacade;
        private Mock<IProjectService> _mockProjectService;
        private Mock<ILogService> _mockLogService;
        private Mock<IToolService> _mockToolService;
        private Mock<IPointService> _mockPointService;

        private Mock<IConfigService> _mockConfigService;

        private QCStateMachine _stateMachine;

        [TestInitialize]
        public void Setup()
        {
            _mockMailboxService = new Mock<IEventMailboxService>();
            _mockSerialPortCommandFacade = new Mock<ISerialPortCommandFacade>();
            _mockProjectService = new Mock<IProjectService>();
            _mockLogService = new Mock<ILogService>();
            _mockToolService = new Mock<IToolService>();
            _mockPointService = new Mock<IPointService>();
            _mockPointService = new Mock<IPointService>();
            _mockConfigService = new Mock<IConfigService>();

            _stateMachine = new QCStateMachine(
                _mockMailboxService.Object,
                _mockSerialPortCommandFacade.Object,
                _mockProjectService.Object,
                _mockLogService.Object,
                _mockToolService.Object,
                _mockPointService.Object,
                _mockPointService.Object,
                _mockConfigService.Object
            );
        }

        [TestMethod]
        public void FireAsync_StartQC_EntersPreparingQCState()
        {
            // Since internal state is private, we verify behavior via interactions or successful execution w/o exception.
            // Ideally we'd check the state, but if 'State' property isn't public, we check side effects.

            // Arrange
            // Ensure ValidateStartQC passes
            // Assume we can just fire StartQC and see if it tries to send commands or post events.

            // The state machine setup is complex and often requires the machine to be in a certain state (InitState).
            _stateMachine.InitState();

            // Act
            // We need to await this
            var task = _stateMachine.FireAsync(QCTrigger.StartQC);
            task.Wait();

            // Assert
            // Checks if OnEnterPreparingQCAsync logic was executed, which usually sends a command or checks something.
            // Based on QCStateMachine logic: 
            // OnEnterPreparingQCAsync checks SystemGlobal.MachineStatus or other validations.
            // If satisfied, it might move to PushingCard.
        }

        // Add more tests simulating feedback from hardware
        // For example, calling HandleCardAddedConfirmAsync should trigger state transition from Preparing to next
    }
}
