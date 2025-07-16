using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Model
{
    /// <summary>
    /// 样本架状态
    /// </summary>
    public class SamleShelfModel
    {
        /// <summary>
        /// 样本架状态 0不存在，1存在
        /// </summary>
        private List<int> samleShelf;

		public List<int> SamleShelf
        {
			get { return samleShelf; }
			set { samleShelf = value; }
		}

	}
}
