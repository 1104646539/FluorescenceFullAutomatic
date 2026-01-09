
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
    }
}
