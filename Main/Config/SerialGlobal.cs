using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Config
{
    public class SerialGlobal
    {
        public static Encoding Encoding = Encoding.GetEncoding("gb2312");

        public const string PortName = "COM3";
        public const byte EndChar = 0x0D;
        public const string EndStr = "\n";
        /// <summary>
        /// ��Ӧ��λ���Ļظ�
        /// </summary>
        public const string CMD_ResponseReply = "0";
        /// <summary>
        /// �Լ�����
        /// </summary>
        public const string CMD_GetSelfInspectionState = "1";
        /// <summary>
        /// ����״̬
        /// </summary>
        public const string CMD_GetMachineState = "2";
        /// <summary>
        /// ��ϴҺ״̬
        /// </summary>
        public const string CMD_GetCleanoutFluid = "3";
        /// <summary>
        /// ������״̬
        /// </summary>
        public const string CMD_GetSampleShelf = "4";
        /// <summary>
        /// �ƶ�������
        /// </summary>
        public const string CMD_MoveSampleShelf = "5";
        /// <summary>
        /// �ƶ�����
        /// </summary>
        public const string CMD_MoveSample = "6";
        /// <summary>
        /// ȡ��
        /// </summary>
        public const string CMD_Sampling = "7";
        /// <summary>
        /// ��ϴȡ����
        /// </summary>
        public const string CMD_CleanoutSamplingProbe = "8";
        /// <summary>
        /// ����
        /// </summary>
        public const string CMD_AddingSample = "9";
        /// <summary>
        /// ��ˮ
        /// </summary>
        public const string CMD_Drainage = "10";
        /// <summary>
        /// �ƿ�
        /// </summary>
        public const string CMD_PushCard = "11";
        /// <summary>
        /// �ƶ���⿨����Ӧ��
        /// </summary>
        public const string CMD_MoveReactionArea = "12";
        /// <summary>
        /// ���
        /// </summary>
        public const string CMD_Test = "13";
        /// <summary>
        /// ��Ӧ���¶�
        /// </summary>
        public const string CMD_GetReactionTemp = "14";
        /// <summary>
        /// ��շ�Ӧ��
        /// </summary>
        public const string CMD_ClearReactionArea = "15";
        /// <summary>
        /// ���Ƶ��
        /// </summary>
        public const string CMD_Motor = "16";
        /// <summary>
        /// ���ز���
        /// </summary>
        public const string CMD_ResetParams = "17";
        /// <summary>
        /// ����
        /// </summary>
        public const string CMD_Update = "18";
        /// <summary>
        /// ��ѹ
        /// </summary>
        public const string CMD_Squeezing = "19";
        /// <summary>
        /// ����
        /// </summary>
        public const string CMD_Pierced = "20";
        /// <summary>
        /// ��ȡ�汾��
        /// </summary>
        public const string CMD_Version = "21";
        /// <summary>
        /// �ػ�
        /// </summary>
        public const string CMD_Shutdown= "22";
    }
}
