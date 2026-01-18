using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Model.Events;

namespace FluorescenceFullAutomatic.Platform.Services
{
    /// <summary>
    /// 事件邮箱服务实现 - 单线程事件循环
    /// 消除并发回调导致的竞态条件，确保事件按序处理
    /// </summary>
    public class EventMailboxService : IEventMailboxService
    {
        private readonly BlockingCollection<ITestEvent> _queue;
        private readonly CancellationTokenSource _cts;
        private readonly Task _processingTask;
        private readonly List<Func<ITestEvent, Task>> _handlers;
        private readonly object _handlerLock = new object();
        private readonly ILogService _logService;
        private bool _isDisposed;

        public EventMailboxService(ILogService logService)
        {
            _logService = logService;
            _queue = new BlockingCollection<ITestEvent>(new ConcurrentQueue<ITestEvent>());
            _cts = new CancellationTokenSource();
            _handlers = new List<Func<ITestEvent, Task>>();
            _processingTask = Task.Run(ProcessEventsAsync);
        }

        /// <summary>
        /// 投递事件到邮箱（线程安全）
        /// </summary>
        public void Post(ITestEvent evt)
        {
            if (_isDisposed || _cts.IsCancellationRequested)
            {
                _logService.Info($"[Mailbox] 邮箱已关闭，丢弃事件: {evt.EventName}");
                return;
            }

            try
            {
                _queue.Add(evt);
                _logService.Info($"[Mailbox] 事件入队: {evt.EventName}");
            }
            catch (InvalidOperationException)
            {
                _logService.Info($"[Mailbox] 队列已完成添加，丢弃事件: {evt.EventName}");
            }
        }

        /// <summary>
        /// 启动事件循环
        /// </summary>
        public void Start()
        {
            _logService.Info("[Mailbox] 事件循环已启动");
        }

        /// <summary>
        /// 停止事件循环
        /// </summary>
        public void Stop()
        {
            if (_isDisposed)
                return;

            _logService.Info("[Mailbox] 正在停止事件循环...");
            _cts.Cancel();
            _queue.CompleteAdding();

            try
            {
                if (!_processingTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    _logService.Info("[Mailbox] 事件循环停止超时");
                }
                else
                {
                    _logService.Info("[Mailbox] 事件循环已停止");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"[Mailbox] 停止事件循环异常 {ex}");
            }
        }

        /// <summary>
        /// 订阅事件处理
        /// </summary>
        public IDisposable Subscribe(Func<ITestEvent, Task> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_handlerLock)
            {
                _handlers.Add(handler);
            }

            _logService.Info($"[Mailbox] 添加事件处理器，当前处理器数量: {_handlers.Count}");

            return new Subscription(() =>
            {
                lock (_handlerLock)
                {
                    _handlers.Remove(handler);
                }
                _logService.Info($"[Mailbox] 移除事件处理器，当前处理器数量: {_handlers.Count}");
            });
        }

        /// <summary>
        /// 事件处理循环（单线程）
        /// </summary>
        private async Task ProcessEventsAsync()
        {
            _logService.Info("[Mailbox] 事件处理循环已启动");

            try
            {
                foreach (var evt in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    await ProcessSingleEventAsync(evt);
                }
            }
            catch (OperationCanceledException)
            {
                _logService.Info("[Mailbox] 事件循环已取消");
            }
            catch (Exception ex)
            {
                _logService.Error($"[Mailbox] 事件循环异常退出 {ex}");
            }
            finally
            {
                _logService.Info("[Mailbox] 事件处理循环已退出");
            }
        }

        /// <summary>
        /// 处理单个事件
        /// </summary>
        private async Task ProcessSingleEventAsync(ITestEvent evt)
        {
            try
            {
                _logService.Info($"[Mailbox] 开始处理事件: {evt.EventName}");

                // 获取处理器快照（避免在处理过程中被修改）
                List<Func<ITestEvent, Task>> handlersSnapshot;
                lock (_handlerLock)
                {
                    handlersSnapshot = new List<Func<ITestEvent, Task>>(_handlers);
                }

                // 按序调用所有处理器
                foreach (var handler in handlersSnapshot)
                {
                    try
                    {
                        await handler(evt);
                    }
                    catch (Exception ex)
                    {
                        _logService.Error($"[Mailbox] 事件处理器异常: {evt.EventName} {ex}" );
                        // 继续处理下一个处理器，不中断事件循环
                    }
                }

                _logService.Info($"[Mailbox] 完成处理事件: {evt.EventName}");
            }
            catch (Exception ex)
            {
                _logService.Error($"[Mailbox] 处理事件失败: {evt.EventName} {ex}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();

            _queue?.Dispose();
            _cts?.Dispose();

            _logService.Info("[Mailbox] 资源已释放");
        }

        /// <summary>
        /// 订阅句柄
        /// </summary>
        private class Subscription : IDisposable
        {
            private readonly Action _unsubscribe;
            private bool _isDisposed;

            public Subscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
            }

            public void Dispose()
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
                _unsubscribe();
            }
        }
    }
}
