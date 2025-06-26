using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Config
{
    /// <summary>
    /// 仪器状态
    /// </summary>
    public enum MachineStatus
    {
        /// <summary>
        /// 空闲
        /// </summary>
        [Description("等待自检")]
        None = 0,
        /// <summary>
        /// 自检中
        /// </summary>
        [Description("正在自检")]
        SelfInspection = 1,
        /// <summary>
        /// 自检失败
        /// </summary>
        [Description("自检失败")]
        SelfInspectionFailed = 2,
        /// <summary>
        /// 运行错误
        /// </summary>
        [Description("运行错误")]
        RunningError = 3,
        /// <summary>
        /// 自检完成
        /// </summary>
        [Description("已就绪")]
        SelfInspectionSuccess = 4,
        /// <summary>
        /// 取样中
        /// </summary>
        [Description("检测中")]
        Sampling = 5,
        /// <summary>
        /// 取样结束
        /// </summary>
        [Description("取样完成")]
        SamplingFinished = 6,
        /// <summary>
        /// 检测中
        /// </summary>
        [Description("检测中")]
        Testing = 7,
        /// <summary>
        /// 结束检测
        /// </summary>
        [Description("已就绪")]
        TestingEnd = 8

    }
    public static class MachineStatusEx
    {

        /// <summary>
        /// 是否正在检测状态
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        public static bool IsRunning(this MachineStatus machine)
        {
            return machine >= MachineStatus.Sampling && machine != MachineStatus.TestingEnd;
        }
        /// <summary>
        /// 是否准备好了
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        public static bool IsPrepare(this MachineStatus machine)
        {
            return machine == MachineStatus.SelfInspectionSuccess || machine == MachineStatus.Testing || machine == MachineStatus.TestingEnd;
        }

        /// <summary>
        /// 是否运行错误
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        public static bool IsRunningError(this MachineStatus machine)
        {
            return machine == MachineStatus.RunningError;
        }
    }
}
