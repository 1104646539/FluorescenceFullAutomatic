
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Moq;
using MahApps.Metro.Controls.Dialogs;
using FluorescenceFullAutomatic.ViewModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using TestMain.Repositorys;
using FluorescenceFullAutomatic.Platform.Services;
using System.Collections.Concurrent;
using FluorescenceFullAutomatic.Platform.Utils;
using System.Threading;
using FluorescenceFullAutomatic.Core.Model;

namespace TestMain
{
    [TestClass]
    public class UnitTest1
    {
      
        [TestInitialize]
        public void Setup()
        {
            SerialPortHelper.Configure(() => new FakeSerialPortImpl(), resetInstance: true);
        }
      
        [TestMethod]
        public async Task TestViewModel_ShouldLoadData()
        {
          
        }

        #region 原有测试用例

        [TestMethod]
        public async Task SerialPortCommandFacade_GetMachineStateAsync_ShouldReturnData()
        {
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);
            var model = await facade.GetMachineStateAsync(timeoutMs: 5000);
            Assert.IsNotNull(model);
            Assert.IsNotNull(model.Data);
        }

        [TestMethod]
        public async Task SerialPortCommandFacade_GetMachineStateAsync_ShouldSupportQueue()
        {
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);
            var t1 = facade.GetMachineStateAsync(timeoutMs: 5000);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () =>
                {
                    await facade.GetMachineStateAsync(timeoutMs: 5000);
                }
            );
            var result = await t1;
            Assert.IsNotNull(result?.Data);
        }

        [TestMethod]
        public async Task SerialPortCommandFacade_ShouldAckReplyAfterResult()
        {
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);

            var sent = new ConcurrentQueue<string>();
            var received = new ConcurrentQueue<string>();

            serialPortService.AddOriginSendDataListener((s) => sent.Enqueue(s ?? ""));
            serialPortService.AddOriginReceiveDataListener((s) => received.Enqueue(s ?? ""));

            var facade = new SerialPortCommandFacade(serialPortService);
            await facade.GetMachineStateAsync(timeoutMs: 5000);

            var sentList = sent.ToArray();
            var receivedList = received.ToArray();

            Assert.IsTrue(sentList.Any(x => x.StartsWith("2 ")), "未发现上位机发送 GetMachineState(2) 命令");
            Assert.IsTrue(receivedList.Any(x => x.Contains("\"Code\":\"2\"") && x.Contains("\"Type\":\"1\"")), "未发现下位机返回响应(Type=1, Code=2)");
            Assert.IsTrue(receivedList.Any(x => x.Contains("\"Code\":\"2\"") && x.Contains("\"Type\":\"2\"")), "未发现下位机返回结果(Type=2, Code=2)");
            Assert.IsTrue(sentList.Any(x => x.StartsWith("0 2 ")), "未发现上位机对结果回执 ResponseReply(0 2)");
        }

        #endregion

        #region 新增测试用例 - 单个命令测试

        [TestMethod]
        public async Task MoveSampleAsync_ShouldReturnSuccess()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act
            var result = await facade.MoveSampleAsync(1, timeoutMs: 5000);

            // Assert
            Assert.IsNotNull(result, "返回结果不应为空");
            Assert.IsNotNull(result.Data, "返回数据不应为空");
            Assert.AreEqual("1", result.State, "移动样本应该成功");
        }

        [TestMethod]
        public async Task PushCardAsync_ShouldReturnCardInfo()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act
            var result = await facade.PushCardAsync(timeoutMs: 5000);

            // Assert
            Assert.IsNotNull(result, "返回结果不应为空");
            Assert.IsNotNull(result.Data, "返回数据不应为空");
            Assert.AreEqual("1", result.Data.Success, "推卡应该成功");
            Assert.IsFalse(string.IsNullOrEmpty(result.Data.QrCode), "二维码不应为空");
        }

        [TestMethod]
        public async Task SamplingAsync_ShouldReturnSuccess()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act
            var result = await facade.SamplingAsync("0", 150, timeoutMs: 5000);

            // Assert
            Assert.IsNotNull(result, "返回结果不应为空");
            Assert.IsNotNull(result.Data, "返回数据不应为空");
            Assert.AreEqual("1", result.Data.Result, "取样应该成功");
        }

        [TestMethod]
        public async Task TestAsync_ShouldReturnTestResult()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act
            var result = await facade.TestAsync(0, 0, "0", "0", "200", "800", "10", "15", timeoutMs: 5000);

            // Assert
            Assert.IsNotNull(result, "返回结果不应为空");
            Assert.IsNotNull(result.Data, "返回数据不应为空");
            Assert.IsTrue(result.Data.Point.Count > 0, "检测点位数据不应为空");
        }

        #endregion

        #region 新增测试用例 - 连续命令测试

        [TestMethod]
        public async Task SamplingAndAddingSample_ShouldCompleteSequentially()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act - 取样
            var samplingResult = await facade.SamplingAsync("0", 150, timeoutMs: 5000);
            Assert.AreEqual("1", samplingResult.Data.Result, "取样应该成功");

            // Act - 加样
            var addingResult = await facade.AddingSampleAsync(150, "1", timeoutMs: 5000);
            Assert.AreEqual("1", addingResult.State, "加样应该成功");
        }

        [TestMethod]
        public async Task PushCardAndMoveReactionArea_ShouldCompleteSequentially()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act - 推卡
            var pushResult = await facade.PushCardAsync(timeoutMs: 5000);
            Assert.AreEqual("1", pushResult.Data.Success, "推卡应该成功");

            // Act - 移动反应区
            var moveResult = await facade.MoveReactionAreaAsync(0, 0, timeoutMs: 8000);
            Assert.AreEqual("1", moveResult.State, "移动反应区应该成功");
        }

        [TestMethod]
        public async Task CompleteWorkflow_MoveSample_Sampling_PushCard_AddingSample_MoveReactionArea()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act & Assert - 完整流程
            var moveResult = await facade.MoveSampleAsync(1, timeoutMs: 5000);
            Assert.AreEqual("1", moveResult.State, "步骤1: 移动样本失败");

            var samplingResult = await facade.SamplingAsync("0", 150, timeoutMs: 5000);
            Assert.AreEqual("1", samplingResult.Data.Result, "步骤2: 取样失败");

            var pushResult = await facade.PushCardAsync(timeoutMs: 5000);
            Assert.AreEqual("1", pushResult.Data.Success, "步骤3: 推卡失败");

            var addingResult = await facade.AddingSampleAsync(150, "1", timeoutMs: 5000);
            Assert.AreEqual("1", addingResult.State, "步骤4: 加样失败");

            var reactionResult = await facade.MoveReactionAreaAsync(0, 0, timeoutMs: 8000);
            Assert.AreEqual("1", reactionResult.State, "步骤5: 移动反应区失败");
        }

        #endregion

        #region 新增测试用例 - 超时场景

        [TestMethod]
        public async Task CommandTimeout_ShouldThrowTimeoutException()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act & Assert - 设置极短超时，应该抛出 TimeoutException
            await Assert.ThrowsExceptionAsync<TimeoutException>(
                async () => await facade.GetMachineStateAsync(timeoutMs: 1),
                "应该抛出 TimeoutException"
            );
        }

        [TestMethod]
        public async Task LongRunningCommand_ShouldCompleteWithinTimeout()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act - 移动反应区需要3秒，设置5秒超时应该成功
            var result = await facade.MoveReactionAreaAsync(0, 0, timeoutMs: 5000);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("1", result.State);
        }

        #endregion

        #region 新增测试用例 - 取消令牌

        [TestMethod]
        public async Task CancellationToken_ShouldCancelCommand()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);
            var cts = new CancellationTokenSource();

            // Act - 立即取消
            cts.Cancel();

            // Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await facade.MoveReactionAreaAsync(0, 0, cts.Token, timeoutMs: 5000),
                "应该抛出 OperationCanceledException"
            );
        }

        [TestMethod]
        public async Task CancellationToken_DelayedCancel_ShouldCancelInProgress()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);
            var cts = new CancellationTokenSource();

            // Act - 1秒后取消（移动反应区需要3秒）
            cts.CancelAfter(1000);

            // Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await facade.MoveReactionAreaAsync(0, 0, cts.Token, timeoutMs: 5000),
                "应该在执行过程中被取消"
            );
        }

        #endregion

        #region 新增测试用例 - 并发控制

        [TestMethod]
        public async Task ConcurrentSameCommand_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act - 同时发起两个相同命令
            var task1 = facade.MoveSampleAsync(1, timeoutMs: 5000);
            
            // Assert - 第二个命令应该抛出异常
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await facade.MoveSampleAsync(2, timeoutMs: 5000),
                "应该抛出 InvalidOperationException"
            );

            // 等待第一个命令完成
            var result = await task1;
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task ConcurrentDifferentCommands_ShouldExecuteSequentially()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act - 同时发起不同命令（应该都能成功）
            var task1 = facade.MoveSampleAsync(1, timeoutMs: 5000);
            var task2 = facade.GetMachineStateAsync(timeoutMs: 5000);
            var task3 = facade.SamplingAsync("0", 150, timeoutMs: 5000);

            // Assert - 等待所有命令完成
            await task1;
            await task2;
            await task3;

            Assert.IsNotNull(await task1, "移动样本命令应该成功");
            Assert.IsNotNull(await task2, "获取机器状态命令应该成功");
            Assert.IsNotNull(await task3, "取样命令应该成功");
        }

        #endregion

        #region 新增测试用例 - 自检流程

        [TestMethod]
        public async Task SelfInspection_ShouldReturnEmptyErrorList()
        {
            // Arrange
            var serialPortService = new SerialPortService();
            serialPortService.Connect("COM3", 9600);
            var facade = new SerialPortCommandFacade(serialPortService);

            // Act
            var result = await facade.GetSelfInspectionStateAsync(false, timeoutMs: 10000);

            // Assert
            Assert.IsNotNull(result, "返回结果不应为空");
            Assert.IsNotNull(result.Data, "返回数据不应为空");
            Assert.AreEqual(0, result.Data.Count, "自检成功时错误列表应该为空");
        }

        #endregion
    }
}
