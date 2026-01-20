namespace FluorescenceFullAutomatic.Platform.StateMachine
{
    /// <summary>
    /// 检测流程触发器（对应事件）
    /// </summary>
    public enum DetectionTrigger
    {
        // ============ 用户操作 ============
        
        /// <summary>
        /// 点击开始检测
        /// </summary>
        StartDetection,

        /// <summary>
        /// 请求自检
        /// </summary>
        RequestSelfInspection,

        /// <summary>
        /// 取消检测
        /// </summary>
        CancelDetection,

        // ============ 自检阶段 ============
        
        /// <summary>
        /// 自检完成
        /// </summary>
        SelfInspectionCompleted,

        /// <summary>
        /// 自检失败
        /// </summary>
        SelfInspectionFailed,

        // ============ 准备阶段 ============
        
        /// <summary>
        /// 收到仪器状态
        /// </summary>
        MachineStatusReceived,

        /// <summary>
        /// 首次清洗完成
        /// </summary>
        FirstCleanCompleted,

        /// <summary>
        /// 清洗完成，可以继续取样
        /// </summary>
        CleaningCompletedForSampling,

        // ============ 样本架/样本移动 ============
        
        /// <summary>
        /// 样本架移动完成
        /// </summary>
        ShelfMoveCompleted,

        /// <summary>
        /// 样本定位完成
        /// </summary>
        SamplePositioned,

        /// <summary>
        /// 样本不存在
        /// </summary>
        SampleNotFound,

        /// <summary>
        /// 样本存在
        /// </summary>
        SampleFound,

        /// <summary>
        /// 跳过扫码直接取样
        /// </summary>
        DirectSampling,

        /// <summary>
        /// 移动到同一排的下一个样本位
        /// </summary>
        MoveToNextSample,

        /// <summary>
        /// 移动到下一排样本架
        /// </summary>
        MoveToNextShelf,

        // ============ 扫码 ============
        
        /// <summary>
        /// 扫码成功
        /// </summary>
        ScanSuccess,

        /// <summary>
        /// 扫码失败
        /// </summary>
        ScanFailed,

        // ============ 取样+推卡 ============
        
        /// <summary>
        /// 取样完成
        /// </summary>
        SamplingCompleted,

        /// <summary>
        /// 推卡完成
        /// </summary>
        PushCardCompleted,

        /// <summary>
        /// 推卡失败
        /// </summary>
        PushCardFailed,

        /// <summary>
        /// 检测卡不足，等待添加
        /// </summary>
        NoCardAvailable,

        /// <summary>
        /// 检测卡已添加，可以继续
        /// </summary>
        CardAvailable,

        /// <summary>
        /// 所有加样条件满足（取样+推卡+清洗都完成）
        /// </summary>
        AllConditionsMet,

        // ============ 加样+移动反应区 ============
        
        /// <summary>
        /// 加样完成
        /// </summary>
        AddingSampleCompleted,

        /// <summary>
        /// 移动到反应区完成
        /// </summary>
        MoveToReactionAreaCompleted,

        // ============ 反应区管理 ============
        
        /// <summary>
        /// 反应区满
        /// </summary>
        ReactionAreaFull,

        /// <summary>
        /// 反应区有空位
        /// </summary>
        ReactionAreaSpaceAvailable,

        // ============ 继续/结束 ============
        
        /// <summary>
        /// 还有更多样本
        /// </summary>
        HasMoreSamples,

        /// <summary>
        /// 没有更多样本了
        /// </summary>
        NoMoreSamples,

        /// <summary>
        /// 待测队列为空
        /// </summary>
        TestQueueEmpty,

        /// <summary>
        /// 待测队列非空
        /// </summary>
        TestQueueNotEmpty,

        // ============ 错误处理 ============
        
        /// <summary>
        /// 发生错误
        /// </summary>
        ErrorOccurred,

        /// <summary>
        /// 重试
        /// </summary>
        Retry,

        /// <summary>
        /// 强制完成
        /// </summary>
        ForceComplete
    }
}
