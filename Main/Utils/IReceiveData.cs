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
        /// �Լ���
        /// </summary>
        /// <param name="model"></param>
        void ReceiveGetSelfMachineStatusModel(BaseResponseModel<List<string>> model);
        /// <summary>
        /// ��ȡ����״̬
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMachineStatusModel(BaseResponseModel<MachineStatusModel> model);
       
        /// <summary>
        /// ��ȡ�ƶ�������
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMoveSampleShelfModel(BaseResponseModel<MoveSampleShelfModel> model);
        /// <summary>
        /// ��ȡ�ƶ�����
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMoveSampleModel(BaseResponseModel<MoveSampleModel> model);
        /// <summary>
        /// ��ȡȡ��
        /// </summary>
        /// <param name="model"></param>
        void ReceiveSamplingModel(BaseResponseModel<SamplingModel> model);
        /// <summary>
        /// ��ȡ��ϴȡ����
        /// </summary>
        /// <param name="model"></param>
        void ReceiveCleanoutSamplingProbeModel(BaseResponseModel<CleanoutSamplingProbeModel> model);
        /// <summary>
        /// ��ȡ����
        /// </summary>
        /// <param name="model"></param>
        void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model);
        /// <summary>
        /// ��ȡ��ˮ
        /// </summary>
        /// <param name="model"></param>
        void ReceiveDrainageModel(BaseResponseModel<DrainageModel> model);
        /// <summary>
        /// ��ȡ�ƿ�
        /// </summary>
        /// <param name="model"></param>
        void ReceivePushCardModel(BaseResponseModel<PushCardModel> model);
        /// <summary>
        /// ��ȡ�ƶ���⿨����Ӧ��
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMoveReactionAreaModel(BaseResponseModel<MoveReactionAreaModel> model);
        /// <summary>
        /// ��ȡ���
        /// </summary>
        /// <param name="model"></param>
        void ReceiveTestModel(BaseResponseModel<TestModel> model);
        /// <summary>
        /// ��ȡ��Ӧ���¶�
        /// </summary>
        /// <param name="model"></param>
        void ReceiveReactionTempModel(BaseResponseModel<ReactionTempModel> model);
        /// <summary>
        /// ��ȡ��շ�Ӧ��
        /// </summary>
        /// <param name="model"></param>
        void ReceiveClearReactionAreaModel(BaseResponseModel<ClearReactionAreaModel> model);
        /// <summary>
        /// ��ȡ���Ƶ��
        /// </summary>
        /// <param name="model"></param>
        void ReceiveMotorModel(BaseResponseModel<MotorModel> model);
        /// <summary>
        /// ��ȡ���ز���
        /// </summary>
        /// <param name="model"></param>
        void ReceiveResetParamsModel(BaseResponseModel<ResetParamsModel> model);
        /// <summary>
        /// ��ȡ����
        /// </summary>
        /// <param name="model"></param>
        void ReceiveUpdateModel(BaseResponseModel<UpdateModel> model);
        /// <summary>
        /// ��ȡ��ѹ
        /// </summary>
        /// <param name="model"></param>
        void ReceiveSqueezingModel(BaseResponseModel<SqueezingModel> model);
        /// <summary>
        /// ��ȡ����
        /// </summary>
        /// <param name="model"></param>
        void ReceivePiercedModel(BaseResponseModel<PiercedModel> model);
        /// <summary>
        /// ��ȡ�汾��
        /// </summary>
        /// <param name="model"></param>
        void ReceiveVersionModel(BaseResponseModel<VersionModel> model);
        /// <summary>
        /// �ػ�
        /// </summary>
        /// <param name="model"></param>
        void ReceiveShutdownModel(BaseResponseModel<ShutdownModel> model);
        /// <summary>
        /// ״̬����
        /// </summary>
        /// <param name="model"></param>
        void ReceiveStateError(BaseResponseModel<dynamic> model);
    }
}
