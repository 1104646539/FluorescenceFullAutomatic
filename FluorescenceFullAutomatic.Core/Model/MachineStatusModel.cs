using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Model
{
    /// <summary>
    /// 仪器状态
    /// </summary>
    public class MachineStatusModel
    {
        /// <summary>
        /// 卡仓是否存在
        /// </summary>
        private string cardExist;

		public string CardExist
        {
			get { return cardExist; }
			set { cardExist = value; }
		}
        /// <summary>
        /// 检测卡数量 0-60
        /// </summary>
        private string cardNum;

		public string CardNum
        {
			get { return cardNum; }
			set { cardNum = value; }
		}

        /// <summary>
        /// 清洗液是否存在，0不存在，1存在
        /// </summary>
        private string cleanoutFluid;

		public string CleanoutFluid
        {
			get { return cleanoutFluid; }
			set { cleanoutFluid = value; }
		}
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
