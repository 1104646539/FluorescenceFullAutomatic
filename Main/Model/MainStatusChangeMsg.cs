using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    public class MainStatusChangeMsg
    {
        public const int What_ChangeState = 100; //状态改变
        public const int What_ClickTest = 200; //点击测试
        public const int What_ClickQC = 300; //点击QC
        public const int What_NetMsg = 400; //新消息

        public int What { get; set; }

        public string Msg { get; set; } 
    }
}
