using System;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Model.Events;

namespace FluorescenceFullAutomatic.Platform.Services
{
    /// <summary>
    /// 事件邮箱服务接口
    /// </summary>
    public interface IEventMailboxService : IDisposable
    {
        /// <summary>
        /// 投递事件到邮箱（线程安全）
        /// </summary>
        void Post(ITestEvent evt);

        /// <summary>
        /// 启动事件循环
        /// </summary>
        void Start();

        /// <summary>
        /// 停止事件循环
        /// </summary>
        void Stop();

        /// <summary>
        /// 订阅事件处理
        /// </summary>
        /// <param name="handler">事件处理器</param>
        /// <returns>订阅句柄，Dispose时取消订阅</returns>
        IDisposable Subscribe(Func<ITestEvent, Task> handler);
    }
}
