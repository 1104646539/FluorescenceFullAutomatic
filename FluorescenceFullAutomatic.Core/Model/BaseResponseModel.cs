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
        /// Type 字段取值 1 下位机响应 2 下位机回复
        /// </summary>
        public const string Type_Response = "1";
        public const string Type_Reply = "2";
        /// <summary>
        /// State 字段取值 1 成功 2 下位机回复
        /// </summary>
        public const string State_Success = "1";
		public const string State_Failed = "2";

        /// <summary>
        /// Error 字段取值 0 成功 其他，不成功
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
