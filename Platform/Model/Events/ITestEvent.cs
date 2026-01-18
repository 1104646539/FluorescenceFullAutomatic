using System;

namespace FluorescenceFullAutomatic.Platform.Model.Events
{
    /// <summary>
    /// 检测流程事件接口
    /// </summary>
    public interface ITestEvent
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        string EventName { get; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        DateTime Timestamp { get; }
    }

    /// <summary>
    /// 事件基类
    /// </summary>
    public abstract class TestEventBase : ITestEvent
    {
        public string EventName => this.GetType().Name;
        public DateTime Timestamp { get; } = DateTime.Now;

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] {EventName}";
        }
    }
}
