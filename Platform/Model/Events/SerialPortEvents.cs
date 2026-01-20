using System.Collections.Generic;
using FluorescenceFullAutomatic.Core.Model;

namespace FluorescenceFullAutomatic.Platform.Model.Events
{
    /// <summary>
    /// 仪器状态查询请求原因
    /// </summary>
    public enum MachineStatusRequestReason
    {
        /// <summary>
        /// 自检后查询
        /// </summary>
        AfterSelfInspection,

        /// <summary>
        /// 开始检测前查询
        /// </summary>
        BeforeTest,

        /// <summary>
        /// 推卡前查询
        /// </summary>
        BeforePushCard,

        /// <summary>
        /// 移动样本前查询
        /// </summary>
        BeforeMoveSample
    }

    #region 请求事件（发送串口命令前）

    /// <summary>
    /// 自检请求事件（CMD_GetSelfInspectionState = "1"）
    /// </summary>
    public class SelfInspectionRequestEvent : TestEventBase
    {
        public bool RetainReactionArea { get; set; }
    }

    /// <summary>
    /// 获取仪器状态请求事件（CMD_GetMachineState = "2"）
    /// </summary>
    public class MachineStatusRequestEvent : TestEventBase
    {
        public MachineStatusRequestReason Reason { get; set; }
    }

    /// <summary>
    /// 获取清洗液状态请求事件（CMD_GetCleanoutFluid = "3"）
    /// </summary>
    public class GetCleanoutFluidRequestEvent : TestEventBase
    {
    }
    /// <summary>
    /// 扫描条码请求事件
    /// </summary>
    public class ScanBarcodeRequestEvent : TestEventBase
    {
    }
   
    /// <summary>
    /// 移动样本架请求事件（CMD_MoveSampleShelf = "5"）
    /// </summary>
    public class MoveSampleShelfRequestEvent : TestEventBase
    {
        public int Position { get; set; }
    }

    /// <summary>
    /// 移动样本请求事件（CMD_MoveSample = "6"）
    /// </summary>
    public class MoveSampleRequestEvent : TestEventBase
    {
        public int Position { get; set; }
    }

    /// <summary>
    /// 取样请求事件（CMD_Sampling = "7"）
    /// </summary>
    public class SamplingRequestEvent : TestEventBase
    {
        public string Type { get; set; }
        public int Volume { get; set; }
    }

    /// <summary>
    /// 清洗取样针请求事件（CMD_CleanoutSamplingProbe = "8"）
    /// </summary>
    public class CleanoutSamplingProbeRequestEvent : TestEventBase
    {
        public int Duration { get; set; }
    }

    /// <summary>
    /// 加样请求事件（CMD_AddingSample = "9"）
    /// </summary>
    public class AddingSampleRequestEvent : TestEventBase
    {
        public int Volume { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// 排水请求事件（CMD_Drainage = "10"）
    /// </summary>
    public class DrainageRequestEvent : TestEventBase
    {
    }

    /// <summary>
    /// 推卡请求事件（CMD_PushCard = "11"）
    /// </summary>
    public class PushCardRequestEvent : TestEventBase
    {
    }

    /// <summary>
    /// 移动到反应区请求事件（CMD_MoveReactionArea = "12"）
    /// </summary>
    public class MoveReactionAreaRequestEvent : TestEventBase
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>
    /// 检测请求事件（CMD_Test = "13"）
    /// </summary>
    public class TestRequestEvent : TestEventBase
    {
        public ReactionAreaItem item { get; set; }
    }

    /// <summary>
    /// 获取反应区温度请求事件（CMD_GetReactionTemp = "14"）
    /// </summary>
    public class GetReactionTempRequestEvent : TestEventBase
    {
        public string Temp { get; set; }
    }

    /// <summary>
    /// 清空反应区请求事件（CMD_ClearReactionArea = "15"）
    /// </summary>
    public class ClearReactionAreaRequestEvent : TestEventBase
    {
    }

    /// <summary>
    /// 控制电机请求事件（CMD_Motor = "16"）
    /// </summary>
    public class MotorRequestEvent : TestEventBase
    {
        public string Motor { get; set; }
        public string Direction { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// 重载参数请求事件（CMD_ResetParams = "17"）
    /// </summary>
    public class ResetParamsRequestEvent : TestEventBase
    {
    }

    /// <summary>
    /// 升级请求事件（CMD_Update = "18"）
    /// </summary>
    public class UpdateRequestEvent : TestEventBase
    {
    }

    /// <summary>
    /// 挤压请求事件（CMD_Squeezing = "19"）
    /// </summary>
    public class SqueezingRequestEvent : TestEventBase
    {
        public string Type { get; set; }
    }

    /// <summary>
    /// 刺破请求事件（CMD_Pierced = "20"）
    /// </summary>
    public class PiercedRequestEvent : TestEventBase
    {
        public string Type { get; set; }
    }

    /// <summary>
    /// 获取版本号请求事件（CMD_Version = "21"）
    /// </summary>
    public class GetVersionRequestEvent : TestEventBase
    {
    }

    /// <summary>
    /// 关机请求事件（CMD_Shutdown = "22"）
    /// </summary>
    public class ShutdownRequestEvent : TestEventBase
    {
    }

    #endregion

    #region 完成事件（接收到串口响应后）

    /// <summary>
    /// 自检完成事件（收到CMD_GetSelfInspectionState响应）
    /// </summary>
    public class SelfInspectionCompletedEvent : TestEventBase
    {
        public BaseResponseModel<List<string>> Result { get; set; }
    }

    /// <summary>
    /// 仪器状态接收完成事件（收到CMD_GetMachineState响应）
    /// </summary>
    public class MachineStatusReceivedEvent : TestEventBase
    {
        public BaseResponseModel<MachineStatusModel> Result { get; set; }
        public MachineStatusRequestReason Reason { get; set; }
    }

    /// <summary>
    /// 清洗液状态接收完成事件（收到CMD_GetCleanoutFluid响应）
    /// </summary>
    public class GetCleanoutFluidCompletedEvent : TestEventBase
    {
        public BaseResponseModel<CleanoutFluidModel> Result { get; set; }
    }

    /// <summary>
    /// 样本架状态接收完成事件（收到CMD_GetSampleShelf响应）
    /// </summary>
    public class GetSampleShelfCompletedEvent : TestEventBase
    {
        public BaseResponseModel<SamleShelfModel> Result { get; set; }
    }

    /// <summary>
    /// 样本架移动完成事件（收到CMD_MoveSampleShelf响应）
    /// </summary>
    public class MoveSampleShelfCompletedEvent : TestEventBase
    {
        public BaseResponseModel<MoveSampleShelfModel> Result { get; set; }
    }

    /// <summary>
    /// 样本移动完成事件（收到CMD_MoveSample响应）
    /// </summary>
    public class MoveSampleCompletedEvent : TestEventBase
    {
        public BaseResponseModel<MoveSampleModel> Result { get; set; }
    }

    /// <summary>
    /// 取样完成事件（收到CMD_Sampling响应）
    /// </summary>
    public class SamplingCompletedEvent : TestEventBase
    {
        public BaseResponseModel<SamplingModel> Result { get; set; }
    }

    /// <summary>
    /// 清洗取样针完成事件（收到CMD_CleanoutSamplingProbe响应）
    /// </summary>
    public class CleanoutSamplingProbeCompletedEvent : TestEventBase
    {
        public BaseResponseModel<CleanoutSamplingProbeModel> Result { get; set; }
    }

    /// <summary>
    /// 加样完成事件（收到CMD_AddingSample响应）
    /// </summary>
    public class AddingSampleCompletedEvent : TestEventBase
    {
        public BaseResponseModel<AddingSampleModel> Result { get; set; }
    }

    /// <summary>
    /// 排水完成事件（收到CMD_Drainage响应）
    /// </summary>
    public class DrainageCompletedEvent : TestEventBase
    {
        public BaseResponseModel<DrainageModel> Result { get; set; }
    }

    /// <summary>
    /// 推卡完成事件（收到CMD_PushCard响应）
    /// </summary>
    public class PushCardCompletedEvent : TestEventBase
    {
        public BaseResponseModel<PushCardModel> Result { get; set; }
    }

    /// <summary>
    /// 移动到反应区完成事件（收到CMD_MoveReactionArea响应）
    /// </summary>
    public class MoveReactionAreaCompletedEvent : TestEventBase
    {
        public BaseResponseModel<MoveReactionAreaModel> Result { get; set; }
        
        /// <summary>
        /// 当前移动的检测结果 ID（从状态机 context 传递）
        /// </summary>
        public int TestResultId { get; set; } = -1;
        
        /// <summary>
        /// 反应区 X 坐标
        /// </summary>
        public int ReactionAreaX { get; set; }
        
        /// <summary>
        /// 反应区 Y 坐标
        /// </summary>
        public int ReactionAreaY { get; set; }
    }

    /// <summary>
    /// 检测完成事件（收到CMD_Test响应）
    /// </summary>
    public class TestCompletedEvent : TestEventBase
    {
        public BaseResponseModel<TestModel> Result { get; set; }
    }

    /// <summary>
    /// 反应区温度获取完成事件（收到CMD_GetReactionTemp响应）
    /// </summary>
    public class GetReactionTempCompletedEvent : TestEventBase
    {
        public BaseResponseModel<ReactionTempModel> Result { get; set; }
    }

    /// <summary>
    /// 清空反应区完成事件（收到CMD_ClearReactionArea响应）
    /// </summary>
    public class ClearReactionAreaCompletedEvent : TestEventBase
    {
        public BaseResponseModel<ClearReactionAreaModel> Result { get; set; }
    }

    /// <summary>
    /// 控制电机完成事件（收到CMD_Motor响应）
    /// </summary>
    public class MotorCompletedEvent : TestEventBase
    {
        public BaseResponseModel<MotorModel> Result { get; set; }
    }

    /// <summary>
    /// 重载参数完成事件（收到CMD_ResetParams响应）
    /// </summary>
    public class ResetParamsCompletedEvent : TestEventBase
    {
        public BaseResponseModel<ResetParamsModel> Result { get; set; }
    }

    /// <summary>
    /// 升级完成事件（收到CMD_Update响应）
    /// </summary>
    public class UpdateCompletedEvent : TestEventBase
    {
        public BaseResponseModel<UpdateModel> Result { get; set; }
    }

    /// <summary>
    /// 挤压完成事件（收到CMD_Squeezing响应）
    /// </summary>
    public class SqueezingCompletedEvent : TestEventBase
    {
        public BaseResponseModel<SqueezingModel> Result { get; set; }
    }

    /// <summary>
    /// 刺破完成事件（收到CMD_Pierced响应）
    /// </summary>
    public class PiercedCompletedEvent : TestEventBase
    {
        public BaseResponseModel<PiercedModel> Result { get; set; }
    }

    /// <summary>
    /// 获取版本号完成事件（收到CMD_Version响应）
    /// </summary>
    public class GetVersionCompletedEvent : TestEventBase
    {
        public BaseResponseModel<VersionModel> Result { get; set; }
    }

    /// <summary>
    /// 关机完成事件（收到CMD_Shutdown响应）
    /// </summary>
    public class ShutdownCompletedEvent : TestEventBase
    {
        public BaseResponseModel<ShutdownModel> Result { get; set; }
    }

    #endregion

    /// <summary>
    /// 串口命令错误事件
    /// </summary>
    public class SerialCommandErrorEvent : TestEventBase
    {
        public string CommandCode { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
