using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    /// <summary>
    /// 推卡
    /// </summary>
    public class PushCardModel
    {
        public const string PushCardSuccess = "1";
        public const string PushCardFail = "0";
        /// <summary>
        /// 推卡是否成功 0不成功，1成功
        /// </summary>
        private string success;

        public string Success
        {
            get { return success; }
            set { success = value; }
        }
        /// <summary>
        /// 二维码信息
        /// </summary>
        private string qrCode;

        public string QrCode
        {
            get { return qrCode; }
            set { qrCode = value; }
        }

    }
}
