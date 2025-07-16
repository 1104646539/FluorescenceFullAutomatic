using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Model
{
    /// <summary>
    /// �ƶ�����
    /// </summary>
    public class MoveSampleModel
    {
        /// <summary>
        /// ������
        /// </summary>
        public const string None = "0";
        /// <summary>
        /// ������
        /// </summary>
        public const string SampleTube = "1";
        /// <summary>
        /// ������
        /// </summary>
        public const string SampleCup = "2";
        /// <summary>
        /// �������� 0�����ڣ�1�����ܣ�2������
        /// </summary>
        private string sampleType;

		public string SampleType
        {
			get { return sampleType; }
			set { sampleType = value; }
		}

	}
}
