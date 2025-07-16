using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluorescenceFullAutomatic.Core.Model
{
    /// <summary>
    /// 筛选条件
    /// </summary>
    public partial class ConditionModel : ObservableObject
    {
        /// <summary>
        /// 项目名
        /// </summary>
        [ObservableProperty]
        public string projectName;


        /// <summary>
        /// 结果
        /// </summary>
        [ObservableProperty]
        public string testVerdict;
        
        /// <summary>
        /// 姓名
        /// </summary>
        [ObservableProperty]
        public string patientName;
        
        /// <summary>
        /// 检测时间 最小
        /// </summary>
        [ObservableProperty]
        public DateTime? testTimeMin = null;
        
        /// <summary>
        /// 检测时间 最大   
        /// </summary>
        [ObservableProperty]
        public DateTime? testTimeMax = null;
        
        /// <summary>   
        /// 浓度范围 最小
        /// </summary>
        [ObservableProperty]
        public int concentrationMin;
        
        /// <summary>
        /// 浓度范围 最大
        /// </summary>
        [ObservableProperty]
        public int concentrationMax;
         /// <summary>   
        /// 浓度范围 最小 项目2
        /// </summary>
        [ObservableProperty]
        public int concentrationMin2;
        
        /// <summary>
        /// 浓度范围 最大 项目2
        /// </summary>
        [ObservableProperty]
        public int concentrationMax2;

        /// <summary>
        /// 条码
        /// </summary>  
        [ObservableProperty]
        public string barcode;
    }   
}
