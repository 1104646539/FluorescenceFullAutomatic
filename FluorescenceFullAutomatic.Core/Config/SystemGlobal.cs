
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Config
{
    public class SystemGlobal
    {
        /// <summary>
        /// 仪器当前运行状态
        /// </summary>
        public static MachineStatus MachineStatus = MachineStatus.None;
        /// <summary>
        /// 是否是在结束检测过程中
        /// </summary>
        public static bool IsRunningtStop = false;

        /// <summary>
        /// 当前温度是否合格
        /// </summary>
        public static bool TempStandard = false;

        /// <summary>
        /// 当前检测类型 分析 ，质控，调试
        /// </summary>
        public static TestType TestType = TestType.None;

        /// <summary>
        /// 当错误后是否要继续检测
        /// </summary>
        public static bool ErrorContinueTest = false;
        /// <summary>
        /// 是否是调试代码模式。不使用真实串口
        /// </summary>
        public const bool IsCodeDebug = true;
     
        /// <summary>
        /// 升级文件的盘符别名
        /// </summary>
        public const string UpdateFlashName = "STM32";
        /// <summary>
        /// 升级文件名
        /// </summary>
        public const string UpdateFileName = "appup.bin";
        /// <summary>
        /// 下位机版本
        /// </summary>
        public static string McuVersion = "";
        /// <summary>
        /// 默认的模板文件路径
        /// </summary>
        public static string Template_Path = GetCurrentProjectPath + @"/template/A4.xlsx";
        /// <summary>
        /// 默认的双联卡模板文件路径
        /// </summary>
        public static string DoubleTemplate_Path = GetCurrentProjectPath + @"/template/A4-双联卡.xlsx";
        /// <summary>
        /// 默认的缓存路径
        /// </summary>
        public static string Cache_Path = GetCurrentProjectPath + @"/cache";

        public static string GetCurrentProjectPath
        {

            get
            {
                //return Environment.CurrentDirectory.Replace(@"\bin\Debug\net6.0-windows", "");//获取具体路径
                return Environment.CurrentDirectory;//获取具体路径
            }
        }
    }
   
}
