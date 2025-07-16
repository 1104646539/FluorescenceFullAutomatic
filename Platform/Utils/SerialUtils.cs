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
        /// 是否需要重发
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
        /// 获取发送命令
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static string GetDataCMD(string data)
        {
            return data.Split(' ')[0].Replace(" ", "").Replace(SerialGlobal.EndStr, "");
        }
        /// <summary>
        /// 将字符串转换为对象 外层带BaseResponseModel
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static BaseResponseModel<T> TranToBaseT<T>(string data)
        {
            return TranToT<BaseResponseModel<T>>(data);
        }
        /// <summary>
        /// 将字符串转换为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T TranToT<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }
        /// <summary>
        /// 比较两个byte数组是否相等
        /// </summary>
        /// <param name="array1">第一个数组</param>
        /// <param name="array2">第二个数组</param>
        /// <returns>如果两个数组相等返回true，否则返回false</returns>
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
