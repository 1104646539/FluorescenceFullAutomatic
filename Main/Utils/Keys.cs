using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FluorescenceFullAutomatic.Utils
{
    public static class Keys
    {
        /// <summary>
        /// 应用程序标题
        /// </summary>
        public const string Title = "Title";

        /// <summary>
        /// 首页标题
        /// </summary>
        public const string HomeTitle = "HomeTitle";
        /// <summary>
        /// 申请测试
        /// </summary>
        public const string ApplyTestTitle = "ApplyTestTitle";
        /// <summary>
        /// 项目列表
        /// </summary>
        public const string ProjectListTitle = "ProjectListTitle";
        /// <summary>
        /// 数据管理标题
        /// </summary>
        public const string DataManagerTitle = "DataManagerTitle";
        /// <summary>
        /// 质控
        /// </summary>
        public const string QCTitle = "QCTitle";

        /// <summary>
        /// 设置标题
        /// </summary>
        public const string SettingsTitle = "SettingsTitle";

        /// <summary>
        /// 阳性结果
        /// </summary>
        public const string ResultPositive = "result_positive";

        /// <summary>
        /// 阴性结果
        /// </summary>
        public const string ResultNegative = "result_negative";

        /// <summary>
        /// 无效结果
        /// </summary>
        public const string ResultInvalid = "result_invalid";

        /// <summary>
        /// 不合格结果
        /// </summary>
        public const string ResultFailed = "result_failed";

        /// <summary>
        /// 合格结果
        /// </summary>
        public const string ResultPass = "result_pass";


        /// <summary>
        /// 检测项目 FOB2
        /// </summary>
        public const string ProjectFOB = "project_fob";
        /// <summary>
        /// 检测项目 TRF2
        /// </summary>
        public const string ProjectTRF = "project_trf";
        /// <summary>
        /// 检测项目 CAL2
        /// </summary>
        public const string ProjectCAL = "project_cal";
        /// <summary>
        /// 检测项目 LF2
        /// </summary>
        public const string ProjectLF = "project_lf";
        /// <summary>
        /// 检测项目 QC2
        /// </summary>
        public const string ProjectQC = "project_qc";
        /// <summary>
        /// 检测项目 DQC 
        /// </summary>
        public const string ProjectDQC = "project_dqc";
        /// <summary>
        /// 检测项目 DFT
        /// </summary>
        public const string ProjectDFT = "project_dft";
        /// <summary>
        /// 检测项目 FOB/TRF
        /// </summary>
        public const string ProjectFOBTRF = "project_fob_trf";
        /// <summary>
        /// 检测项目 QC FOB/TRF 
        /// </summary>
        public const string ProjectQCFOBTRF = "project_qc_fob_trf";


        /// <summary>
        /// 获取资源字典中的字符串值
        /// </summary>
        /// <param name="key">资源键</param>
        /// <returns>资源值</returns>
        public static string GetString(string key)
        {
            if (Application.Current != null && Application.Current.Resources.Contains(key))
            {
                return Application.Current.Resources[key] as string;
            }
            return key; // 如果找不到资源，返回键名
        }
    }
}
