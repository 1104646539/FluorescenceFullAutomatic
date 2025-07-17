using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Ex
{
    public static class StringEx
    {
        /// <summary>
        /// 补齐字符串
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <param name="align">对齐方式 1-左对齐 2-居中对齐 3-右对齐</param>
        /// <param name="len">目标长度</param>
        /// <param name="cr">填充字符</param>
        /// <returns></returns>
        public static string ComplementString(this string str, int align, int len, string cr = " ")
        {
            if (string.IsNullOrEmpty(cr))
                cr = " ";
            if (str == null)
                str = string.Empty;
            if (cr.Length == 0)
                cr = " ";
            if (str.Length >= len)
            {
                return str.Substring(0, len);
            }
            int padLen = len - str.Length;
            switch (align)
            {
                case 1: // 左对齐
                    return str + RepeatString(cr, padLen);
                case 2: // 居中对齐
                    int left = padLen / 2;
                    int right = padLen - left;
                    return RepeatString(cr, left) + str + RepeatString(cr, right);
                case 3: // 右对齐
                    return RepeatString(cr, padLen) + str;
                default:
                    return str;
            }
        }

        private static string RepeatString(string cr, int count)
        {
            if (string.IsNullOrEmpty(cr) || count <= 0)
                return string.Empty;
            StringBuilder sb = new StringBuilder();
            while (sb.Length < count)
            {
                int remain = count - sb.Length;
                if (cr.Length <= remain)
                    sb.Append(cr);
                else
                    sb.Append(cr.Substring(0, remain));
            }
            return sb.ToString();
        }
    }
}
