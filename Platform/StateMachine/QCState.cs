using System.ComponentModel;

namespace FluorescenceFullAutomatic.Platform.StateMachine
{
    /// <summary>
    /// QC 质控流程状态
    /// </summary>
    public enum QCState
    {
        /// <summary>
        /// 空闲
        /// </summary>
        [Description("空闲")]
        Idle,

        /// <summary>
        /// 准备质控（获取仪器状态）
        /// </summary>
        [Description("准备质控")]
        PreparingQC,

        /// <summary>
        /// 等待添加检测卡
        /// </summary>
        [Description("等待添加检测卡")]
        WaitingForCard,

        /// <summary>
        /// 推卡中
        /// </summary>
        [Description("推卡中")]
        PushingCard,

        /// <summary>
        /// 等待推卡确认（项目验证）
        /// </summary>
        [Description("验证项目")]
        WaitingCardConfirm,

        /// <summary>
        /// 移动到反应区
        /// </summary>
        [Description("移动反应区")]
        MovingToReaction,

        /// <summary>
        /// 检测中
        /// </summary>
        [Description("检测中")]
        Testing,

        /// <summary>
        /// 等待下一次检测
        /// </summary>
        [Description("等待下次检测")]
        WaitingNextTest,

        /// <summary>
        /// 完成
        /// </summary>
        [Description("完成")]
        Completed,

        /// <summary>
        /// 错误
        /// </summary>
        [Description("错误")]
        Error
    }
}
