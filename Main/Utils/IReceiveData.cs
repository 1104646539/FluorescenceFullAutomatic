using FluorescenceFullAutomatic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Utils
{
    public interface IReceiveData
    {
        /// <summary>
        /// 自检结果
        /// </summary>
        /// <param name="model"></param>
        void ReceiveGetSelfMachineStatusModel(BaseResponseModel<List<string>> model);
        /// <summary>
        /// 获取卡仓状态
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMachineStatusModel(BaseResponseModel<MachineStatusModel> model);
       
        /// <summary>
        /// 获取移动样本架
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMoveSampleShelfModel(BaseResponseModel<MoveSampleShelfModel> model);
        /// <summary>
        /// 获取移动样本
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMoveSampleModel(BaseResponseModel<MoveSampleModel> model);
        /// <summary>
        /// 获取取样
        /// </summary>
        /// <param name="model"></param>
        void ReceiveSamplingModel(BaseResponseModel<SamplingModel> model);
        /// <summary>
        /// 获取清洗取样针
        /// </summary>
        /// <param name="model"></param>
        void ReceiveCleanoutSamplingProbeModel(BaseResponseModel<CleanoutSamplingProbeModel> model);
        /// <summary>
        /// 获取加样
        /// </summary>
        /// <param name="model"></param>
        void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model);
        /// <summary>
        /// 获取排水
        /// </summary>
        /// <param name="model"></param>
        void ReceiveDrainageModel(BaseResponseModel<DrainageModel> model);
        /// <summary>
        /// 获取推卡
        /// </summary>
        /// <param name="model"></param>
        void ReceivePushCardModel(BaseResponseModel<PushCardModel> model);
        /// <summary>
        /// 获取移动检测卡到反应区
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMoveReactionAreaModel(BaseResponseModel<MoveReactionAreaModel> model);
        /// <summary>
        /// 获取检测
        /// </summary>
        /// <param name="model"></param>
        void ReceiveTestModel(BaseResponseModel<TestModel> model);
        /// <summary>
        /// 获取反应区温度
        /// </summary>
        /// <param name="model"></param>
        void ReceiveReactionTempModel(BaseResponseModel<ReactionTempModel> model);
        /// <summary>
        /// 获取清空反应区
        /// </summary>
        /// <param name="model"></param>
        void ReceiveClearReactionAreaModel(BaseResponseModel<ClearReactionAreaModel> model);
        /// <summary>
        /// 获取控制电机
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMotorModel(BaseResponseModel<MotorModel> model);
        /// <summary>
        /// 获取重载参数
        /// </summary>
        /// <param name="model"></param>
        void ReceiveResetParamsModel(BaseResponseModel<ResetParamsModel> model);
        /// <summary>
        /// 获取升级
        /// </summary>
        /// <param name="model"></param>
        void ReceiveUpdateModel(BaseResponseModel<UpdateModel> model);
        /// <summary>
        /// 获取挤压
        /// </summary>
        /// <param name="model"></param>
        void ReceiveSqueezingModel(BaseResponseModel<SqueezingModel> model);
        /// <summary>
        /// 获取刺破
        /// </summary>
        /// <param name="model"></param>
        void ReceivePiercedModel(BaseResponseModel<PiercedModel> model);
        /// <summary>
        /// 获取版本号
        /// </summary>
        /// <param name="model"></param>
        void ReceiveVersionModel(BaseResponseModel<VersionModel> model);
        /// <summary>
        /// 关机
        /// </summary>
        /// <param name="model"></param>
        void ReceiveShutdownModel(BaseResponseModel<ShutdownModel> model);
        /// <summary>
        /// 状态错误
        /// </summary>
        /// <param name="model"></param>
        void ReceiveStateError(BaseResponseModel<dynamic> model);
    }
}
