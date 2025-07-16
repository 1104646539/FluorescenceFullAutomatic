using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Model
{
    /// <summary>
    /// ����״̬
    /// </summary>
    public class MachineStatusModel
    {
        /// <summary>
        /// �����Ƿ����
        /// </summary>
        private string cardExist;

		public string CardExist
        {
			get { return cardExist; }
			set { cardExist = value; }
		}
        /// <summary>
        /// ��⿨���� 0-60
        /// </summary>
        private string cardNum;

		public string CardNum
        {
			get { return cardNum; }
			set { cardNum = value; }
		}

        /// <summary>
        /// ��ϴҺ�Ƿ���ڣ�0�����ڣ�1����
        /// </summary>
        private string cleanoutFluid;

		public string CleanoutFluid
        {
			get { return cleanoutFluid; }
			set { cleanoutFluid = value; }
		}
     /// <summary>
        /// ������״̬ 0�����ڣ�1����
        /// </summary>
        private List<int> samleShelf;

		public List<int> SamleShelf
        {
			get { return samleShelf; }
			set { samleShelf = value; }
		}

	}
}
