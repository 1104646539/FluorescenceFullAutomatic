using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Core.Config;

namespace FluorescenceFullAutomatic.Platform.Model.Events
{
    /// <summary>
    /// QC 验证错误类型
    /// </summary>
    public enum QCValidationErrorType
    {
        /// <summary>
        /// 无错误
        /// </summary>
        None,

        /// <summary>
        /// 未自检
        /// </summary>
        NotSelfInspected,

        /// <summary>
        /// 自检失败
        /// </summary>
        SelfInspectionFailed,

        /// <summary>
        /// 正在检测中
        /// </summary>
        AlreadyTesting,

        /// <summary>
        /// 其他类型检测进行中
        /// </summary>
        OtherTestInProgress,

        /// <summary>
        /// 反应区非空
        /// </summary>
        ReactionAreaNotEmpty,

        /// <summary>
        /// 运行错误
        /// </summary>
        RunningError
    }

    #region 请求事件

    /// <summary>
    /// 开始质控请求事件
    /// </summary>
    public class StartQCEvent : TestEventBase { }

    /// <summary>
    /// 取消质控请求事件
    /// </summary>
    public class CancelQCEvent : TestEventBase { }

    /// <summary>
    /// 卡已添加确认事件（用户确认已添加检测卡）
    /// </summary>
    public class QCCardAddedConfirmEvent : TestEventBase { }

    /// <summary>
    /// 重试推卡事件
    /// </summary>
    public class QCRetryPushCardEvent : TestEventBase { }

    #endregion

    #region UI 事件

    /// <summary>
    /// QC 验证错误事件（通知 UI 显示错误提示）
    /// </summary>
    public class QCValidationErrorEvent : TestEventBase
    {
        public QCValidationErrorType ErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// QC 卡不可用事件（通知 UI 提示添加检测卡）
    /// </summary>
    public class QCNoCardAvailableEvent : TestEventBase
    {
        public string ErrorMessage { get; set; }
    }
    public class MachineStateChangeEvent : TestEventBase
    {
        public MachineStatus NewState { get; set; }
    }
    /// <summary>
    /// QC 项目无效事件（通知 UI 提示项目问题）
    /// </summary>
    public class QCProjectInvalidEvent : TestEventBase
    {
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// QC 完成事件，只是完成，比如不添加检测卡等
    /// </summary>
    public class QCCompletedEvent : TestEventBase
    {
        public string Message { get; set; }
    }
    /// <summary>
    /// QC 结束事件，成功或者失败
    /// </summary>
    public class QCFinishEvent : TestEventBase
    {

    }
    #endregion

    #region 硬件反馈事件（QC 专用）

    /// <summary>
    /// QC 仪器状态接收事件
    /// </summary>
    public class QCMachineStatusReceivedEvent : TestEventBase
    {
        public BaseResponseModel<MachineStatusModel> Result { get; set; }
    }
    /// <summary>
    /// QC 出队事件，开始检测了
    /// </summary>
    public class QCDequeueEvent : TestEventBase
    {
        public ReactionAreaItem CurrentTestItem { get; set; }
    }
    /// <summary>
    /// QC 检测完成事件
    /// </summary>
    public class QCTestCompletedEvent : TestEventBase
    {
        public BaseResponseModel<TestModel> Result { get; set; }
    }

    /// <summary>
    /// QC 结果处理完成事件
    /// </summary>
    public class QCResultProcessedEvent : TestEventBase
    {
        public Point Point { get; set; }
        public int Index { get; set; }
    }
  
    
    #endregion
}
