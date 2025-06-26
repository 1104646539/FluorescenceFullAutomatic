using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    /// <summary>
    /// 清洗液
    /// </summary>
    public class CleanoutFluidModel
    {
        /// <summary>
        /// 清洗液是否存在 0不存在，1存在
        /// </summary>
        private string cleanoutFluidVolumn;

		public string CleanoutFluidVolumn
        {
			get { return cleanoutFluidVolumn; }
			set { cleanoutFluidVolumn = value; }
		}

	}
}
