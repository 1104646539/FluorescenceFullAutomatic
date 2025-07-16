using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Model
{
    public class BaseResponseModel<T>
    {
        /// <summary>
        /// Type �ֶ�ȡֵ 1 ��λ����Ӧ 2 ��λ���ظ�
        /// </summary>
        public const string Type_Response = "1";
        public const string Type_Reply = "2";
        /// <summary>
        /// State �ֶ�ȡֵ 1 �ɹ� 2 ��λ���ظ�
        /// </summary>
        public const string State_Success = "1";
		public const string State_Failed = "2";

        /// <summary>
        /// Error �ֶ�ȡֵ 0 �ɹ� ���������ɹ�
        /// </summary>
        public const string Error_Success = "0";


        private string code;

		public string Code
		{
			get { return code; }
			set { code = value; }
		}


		private string type;

		public string Type
		{
			get { return type; }
			set { type = value; }
		}

		private string state;

		public string State
		{
			get { return state; }
			set { state = value; }
		}

        private string error;

        public string Error
        {
            get { return error; }
            set { error = value; }
        }
        private T data;

		public T Data
		{
			get { return data; }
			set { data = value; }
		}

	}
}
