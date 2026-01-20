using System.ComponentModel;

namespace FluorescenceFullAutomatic.Platform.StateMachine
{
    /// <summary>
    /// 检测流程状态
    /// </summary>
    public enum DetectionState
    {
        /// <summary>
        /// 空闲（刚启动）
        /// </summary>
        [Description("空闲")]
        Idle,

        /// <summary>
        /// 正在自检
        /// </summary>
        [Description("正在自检")]
        SelfInspecting,

        /// <summary>
        /// 自检完成后获取一次状态
        /// </summary>
        [Description("正在自检")]
        SelfInspectingAfterGetMachineStatus,
        /// <summary>
        /// 就绪（自检完成）
        /// </summary>
        [Description("已就绪")]
        Ready,

        /// <summary>
        /// 准备检测（首次获取仪器状态）
        /// </summary>
        [Description("准备检测")]
        PreparingDetection,

        /// <summary>
        /// 等待首次清洗取样针
        /// </summary>
        [Description("准备检测")]
        WaitingForFirstClean,

        /// <summary>
        /// 移动到样本架
        /// </summary>
        [Description("检测中")]
        MovingToShelf,

        /// <summary>
        /// 定位到样本位
        /// </summary>
        [Description("检测中")]
        PositioningAtSample,

        /// <summary>
        /// 正在扫码
        /// </summary>
        [Description("检测中")]
        Scanning,

        /// <summary>
        /// 取样+推卡并行
        /// </summary>
        [Description("检测中")]
        SamplingAndPushingCard,

        /// <summary>
        /// 等待添加检测卡
        /// </summary>
        [Description("等待添加检测卡")]
        WaitingForCard,

        /// <summary>
        /// 等待加样条件满足
        /// </summary>
        [Description("检测中")]
        WaitingToAddSample,

        /// <summary>
        /// 正在加样
        /// </summary>
        [Description("检测中")]
        AddingSample,

        /// <summary>
        /// 移动到反应区
        /// </summary>
        [Description("检测中")]
        MovingToReactionArea,

        /// <summary>
        /// 已入队待检测
        /// </summary>
        [Description("检测中")]
        EnqueuedForTest,

        /// <summary>
        /// 反应区满暂停取样
        /// </summary>
        [Description("检测中")]
        PausedDueToFullQueue,

        /// <summary>
        /// 取样结束收尾（清洗+复位）
        /// </summary>
        [Description("取样完成")]
        Finishing,

        /// <summary>
        /// 等待反应区检测完成
        /// </summary>
        [Description("检测中")]
        WaitingForTests,

        /// <summary>
        /// 全部完成
        /// </summary>
        [Description("已就绪")]
        Completed,

        /// <summary>
        /// 运行错误
        /// </summary>
        [Description("运行错误")]
        Error
    }
}
