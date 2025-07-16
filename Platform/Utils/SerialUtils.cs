using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Utils
{
    public class SerialUtils
    {
        /// <summary>
        /// �Ƿ���Ҫ�ط�
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsNeedRetry(string data)
        {
            if (GetDataCMD(data).Equals(SerialGlobal.CMD_ResponseReply))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// ��ȡ��������
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static string GetDataCMD(string data)
        {
            return data.Split(' ')[0].Replace(" ", "").Replace(SerialGlobal.EndStr, "");
        }
        /// <summary>
        /// ���ַ���ת��Ϊ���� ����BaseResponseModel
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static BaseResponseModel<T> TranToBaseT<T>(string data)
        {
            return TranToT<BaseResponseModel<T>>(data);
        }
        /// <summary>
        /// ���ַ���ת��Ϊ����
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T TranToT<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }
        /// <summary>
        /// �Ƚ�����byte�����Ƿ����
        /// </summary>
        /// <param name="array1">��һ������</param>
        /// <param name="array2">�ڶ�������</param>
        /// <returns>�������������ȷ���true�����򷵻�false</returns>
        public static bool AreByteArraysEqual(byte[] array1, byte[] array2)
        {
            if (array1 == null || array2 == null)
                return false;
            
            if (array1.Length != array2.Length)
                return false;
                
            return array1.SequenceEqual(array2);
        }
     
    }
}
