using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Ex
{
    public static class DateTimeEx
    {
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string DateTimeFormat2 = "yyyy-MM-dd HH:mm:ss.fff";
        public const string DateTimeFormat3 = "yyyyMMddHHmmss";
        public const string DateTimeFormat4 = "yyyy-MM-dd HH:mm";
        /// <summary>
        /// 年月日时分秒
        /// </summary>
        /// <returns></returns>
        public static string GetDateTimeString(this DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormat);
        }

        /// <summary>
        /// 年月日时分秒毫秒
        /// </summary>
        /// <returns></returns>
        public static string GetDateTimeString2(this DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormat2);
        }

        /// <summary>
        /// 年月日时分秒 
        /// </summary>
        /// <returns></returns>
        public static string GetDateTimeString3(this DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormat3);
        }
        /// <summary>
        /// 年月日时分
        /// </summary>
        /// <returns></returns>
        public static string GetDateTimeString4(this DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormat4);
        }
        public static DateTime GetDateTime(this string str)
        {
            return DateTime.Parse(str);
        }
    }
}
