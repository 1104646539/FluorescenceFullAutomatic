using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Model
{
    /// <summary>
    /// 移动样本
    /// </summary>
    public class MoveSampleModel
    {
        /// <summary>
        /// 不存在
        /// </summary>
        public const string None = "0";
        /// <summary>
        /// 样本管
        /// </summary>
        public const string SampleTube = "1";
        /// <summary>
        /// 样本杯
        /// </summary>
        public const string SampleCup = "2";
        /// <summary>
        /// 样本类型 0不存在，1样本管，2样本杯
        /// </summary>
        private string sampleType;

		public string SampleType
        {
			get { return sampleType; }
			set { sampleType = value; }
		}

	}
}
