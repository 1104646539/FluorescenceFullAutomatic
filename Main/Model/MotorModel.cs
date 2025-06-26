using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
	/// <summary>
	/// 控制电机
	/// </summary>
    public class MotorModel
    {
        /// <summary>
        /// 复位状态
        /// </summary>
        private string restState;

		public string RestState
        {
			get { return restState; }
			set { restState = value; }
		}

	}
}
