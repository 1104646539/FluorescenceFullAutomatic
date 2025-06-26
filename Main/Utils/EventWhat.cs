using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Utils
{
    public class EventWhat
    {
        /// <summary>
        /// 刷新数据
        /// </summary>
        public const int WHAT_REFRESH_DATA = 1000;
        /// <summary>
        /// 添加数据
        /// </summary>
        public const int WHAT_ADD_DATA = 1001;
        /// <summary>
        /// 更新数据
        /// </summary>
        public const int WHAT_CHANGE_DATA = 1002;

        /// <summary>
        /// 更新申请状态
        /// </summary>
        public const int WHAT_CHANGE_APPLY_TEST = 2000;

        /// <summary>
        /// 更新配置，检测配置
        /// </summary>
        public const int WHAT_CHANGE_TEST_SETTINGS = 3000;

        /// <summary>
        /// 点击首页
        /// </summary>
        public const int WHAT_CLICK_HOME = 4000;
        /// <summary>
        /// 点击申请信息
        /// </summary>
        public const int WHAT_CLICK_APPLY_TEST = 4001;
        /// <summary>
        /// 点击数据管理
        /// </summary>
        public const int WHAT_CLICK_DATA_MANAGER = 4002;
        /// <summary>
        /// 点击QC
        /// </summary>
        public const int WHAT_CLICK_QC = 4003;
        /// <summary>
        /// 点击设置
        /// </summary>
        public const int WHAT_CLICK_SETTINGS = 4004;
        /// <summary>
        /// 调试模式变更
        /// </summary>
        public const int WHAT_CLICK_DEBUG_MODE = 5000;
    }
}
