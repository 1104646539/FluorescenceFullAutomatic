namespace FluorescenceFullAutomatic.Platform.Model.Events
{
    /// <summary>
    /// 用户点击开始检测
    /// </summary>
    public class StartTestEvent : TestEventBase
    {
    }

    /// <summary>
    /// 用户点击停止检测
    /// </summary>
    public class StopTestEvent : TestEventBase
    {
    }

    /// <summary>
    /// 自检开始事件（用于通知 UI 显示自检对话框）
    /// </summary>
    public class SelfInspectionStartedEvent : TestEventBase
    {
    }

    /// <summary>
    /// 自检结束事件（用于通知 UI 关闭自检对话框）
    /// </summary>
    public class SelfInspectionEndedEvent : TestEventBase
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 检测启动校验失败事件（用于通知 UI 显示错误提示）
    /// </summary>
    public class DetectionValidationErrorEvent : TestEventBase
    {
        public string ErrorKey { get; set; }
    }

    /// <summary>
    /// 仪器状态异常校验失败事件
    /// </summary>
    public class MachineStatusValidationErrorEvent : TestEventBase
    {
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 识别并获取到申请信息事件
    /// </summary>
    public class ApplyTestIdentifiedEvent : TestEventBase
    {
        public ApplyTest ApplyTest { get; set; }
    }

    /// <summary>
    /// 检测卡不足事件（用于通知 VM 提示用户添加卡）
    /// </summary>
    public class NoCardAvailableEvent : TestEventBase
    {
        public int CurrentCardNum { get; set; }
    }
    /// <summary>
    /// 检测卡不足事件（用于通知 VM 提示用户添加卡）
    /// </summary>
    public class CardNumberUpdatedEvent : TestEventBase
    {
        public int CurrentCardNum { get; set; }
    }


    /// <summary>
    /// 取样结束事件（清洗取样针和样本架复位都完成后通知 VM）
    /// </summary>
    public class SamplingFinishedEvent : TestEventBase
    {
        public string HintMessage { get; set; }
    }

    /// <summary>
    /// 样本位置变更事件（通知 VM 从 StateContext 同步位置信息）
    /// 在 MoveSampleCompletedEvent 发送前触发，让 VM 及时获取最新位置
    /// </summary>
    public class SamplePositionChangedEvent : TestEventBase
    {
        /// <summary>
        /// 当前样本架位置（0-5）
        /// </summary>
        public int ShelfPos { get; set; }

        /// <summary>
        /// 当前样本位（0-4）
        /// </summary>
        public int SamplePos { get; set; }

        /// <summary>
        /// 样本类型
        /// </summary>
        public SampleType SampleType { get; set; }

        /// <summary>
        /// 当前检测结果 ID（用于 VM 关联）
        /// </summary>
        public int TestResultId { get; set; }
    }
}
