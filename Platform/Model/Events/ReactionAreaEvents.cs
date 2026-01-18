namespace FluorescenceFullAutomatic.Platform.Model.Events
{
    /// <summary>
    /// 反应区出队事件
    /// </summary>
    public class ReactionAreaDequeueEvent : TestEventBase
    {
        /// <summary>
        /// 出队的反应区项目
        /// </summary>
        public ReactionAreaItem Item { get; set; }
    }
}
