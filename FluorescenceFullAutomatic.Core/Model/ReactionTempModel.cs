using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Model
{
    /// <summary>
    /// 反应区温度
    /// </summary>
    public class ReactionTempModel
    {
        /// <summary>
        /// 反应区温度 单位为℃*10,如305表示30.5℃
        /// </summary>
        private string temp;

        public string Temp
        {
            get { return temp; }
            set { temp = value; }
        }

    }
}
