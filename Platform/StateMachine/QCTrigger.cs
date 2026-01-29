namespace FluorescenceFullAutomatic.Platform.StateMachine
{
    /// <summary>
    /// QC 质控流程触发器
    /// </summary>
    public enum QCTrigger
    {
        // ============ 用户操作 ============

        /// <summary>
        /// 开始质控
        /// </summary>
        StartQC,

        /// <summary>
        /// 取消质控
        /// </summary>
        CancelQC,

        // ============ 仪器状态 ============

        /// <summary>
        /// 收到仪器状态
        /// </summary>
        MachineStatusReceived,

        /// <summary>
        /// 卡不可用（卡仓不存在或检测卡不足）
        /// </summary>
        CardNotAvailable,

        /// <summary>
        /// 卡可用
        /// </summary>
        CardAvailable,

        // ============ 推卡 ============

        /// <summary>
        /// 推卡完成
        /// </summary>
        PushCardCompleted,

        /// <summary>
        /// 推卡失败
        /// </summary>
        PushCardFailed,

        /// <summary>
        /// 重试推卡
        /// </summary>
        RetryPushCard,

        // ============ 项目验证 ============

        /// <summary>
        /// 项目有效（QC项目）
        /// </summary>
        ProjectValid,

        /// <summary>
        /// 项目无效（非QC项目或项目为空）
        /// </summary>
        ProjectInvalid,

        // ============ 反应区 ============

        /// <summary>
        /// 移动反应区完成
        /// </summary>
        MoveReactionCompleted,

        // ============ 检测 ============

        /// <summary>
        /// 检测完成（单次）
        /// </summary>
        TestSingleCompleted,

        /// <summary>
        /// 所有检测完成（10次）
        /// </summary>
        AllTestsCompleted,

        // ============ 错误 ============

        /// <summary>
        /// 发生错误
        /// </summary>
        ErrorOccurred
    }
}
