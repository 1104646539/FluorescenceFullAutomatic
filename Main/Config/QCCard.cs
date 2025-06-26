using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Config
{
    public enum QCCard
    {
        /// <summary>
        /// 质控卡
        /// </summary>
        [Description("质控卡")]
        QCCard = 0,
        /// <summary>
        /// 标准品
        /// </summary>
        [Description("标准品")]
        Standard = 0
    }
}
