using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    /// <summary>
    /// 取样
    /// </summary>
    public class SamplingModel
    {
        /// <summary>
        /// 取样结果 0取样失败，1取样成功 
        /// </summary>
        private string result;

		public string Result
        {
			get { return result; }
			set { result = value; }
		}

	}
}
