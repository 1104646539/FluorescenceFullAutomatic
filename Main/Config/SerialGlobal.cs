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
        /// 响应下位机的回复
        /// </summary>
        public const string CMD_ResponseReply = "0";
        /// <summary>
        /// 自检命令
        /// </summary>
        public const string CMD_GetSelfInspectionState = "1";
        /// <summary>
        /// 仪器状态
        /// </summary>
        public const string CMD_GetMachineState = "2";
        /// <summary>
        /// 清洗液状态
        /// </summary>
        public const string CMD_GetCleanoutFluid = "3";
        /// <summary>
        /// 样本架状态
        /// </summary>
        public const string CMD_GetSampleShelf = "4";
        /// <summary>
        /// 移动样本架
        /// </summary>
        public const string CMD_MoveSampleShelf = "5";
        /// <summary>
        /// 移动样本
        /// </summary>
        public const string CMD_MoveSample = "6";
        /// <summary>
        /// 取样
        /// </summary>
        public const string CMD_Sampling = "7";
        /// <summary>
        /// 清洗取样针
        /// </summary>
        public const string CMD_CleanoutSamplingProbe = "8";
        /// <summary>
        /// 加样
        /// </summary>
        public const string CMD_AddingSample = "9";
        /// <summary>
        /// 排水
        /// </summary>
        public const string CMD_Drainage = "10";
        /// <summary>
        /// 推卡
        /// </summary>
        public const string CMD_PushCard = "11";
        /// <summary>
        /// 移动检测卡到反应区
        /// </summary>
        public const string CMD_MoveReactionArea = "12";
        /// <summary>
        /// 检测
        /// </summary>
        public const string CMD_Test = "13";
        /// <summary>
        /// 反应区温度
        /// </summary>
        public const string CMD_GetReactionTemp = "14";
        /// <summary>
        /// 清空反应区
        /// </summary>
        public const string CMD_ClearReactionArea = "15";
        /// <summary>
        /// 控制电机
        /// </summary>
        public const string CMD_Motor = "16";
        /// <summary>
        /// 重载参数
        /// </summary>
        public const string CMD_ResetParams = "17";
        /// <summary>
        /// 升级
        /// </summary>
        public const string CMD_Update = "18";
        /// <summary>
        /// 挤压
        /// </summary>
        public const string CMD_Squeezing = "19";
        /// <summary>
        /// 刺破
        /// </summary>
        public const string CMD_Pierced = "20";
        /// <summary>
        /// 获取版本号
        /// </summary>
        public const string CMD_Version = "21";
        /// <summary>
        /// 关机
        /// </summary>
        public const string CMD_Shutdown= "22";
    }
}
