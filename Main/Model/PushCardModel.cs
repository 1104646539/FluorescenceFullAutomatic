using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    /// <summary>
    /// �ƿ�
    /// </summary>
    public class PushCardModel
    {
        public const string PushCardSuccess = "1";
        public const string PushCardFail = "0";
        /// <summary>
        /// �ƿ��Ƿ�ɹ� 0���ɹ���1�ɹ�
        /// </summary>
        private string success;

        public string Success
        {
            get { return success; }
            set { success = value; }
        }
        /// <summary>
        /// ��ά����Ϣ
        /// </summary>
        private string qrCode;

        public string QrCode
        {
            get { return qrCode; }
            set { qrCode = value; }
        }

    }
}
