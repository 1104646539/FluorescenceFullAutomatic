using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    /// <summary>
    /// ��ϴҺ
    /// </summary>
    public class CleanoutFluidModel
    {
        /// <summary>
        /// ��ϴҺ�Ƿ���� 0�����ڣ�1����
        /// </summary>
        private string cleanoutFluidVolumn;

		public string CleanoutFluidVolumn
        {
			get { return cleanoutFluidVolumn; }
			set { cleanoutFluidVolumn = value; }
		}

	}
}
