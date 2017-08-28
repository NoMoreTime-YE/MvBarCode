using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Scale
{
    /// <summary>
    /// 网络数据编解码类
    /// </summary>
    internal class StreamCoder
    {
        private static char[] HexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 
                                              'A', 'B', 'C', 'D', 'E', 'F', 'a', 'b', 'c', 'd', 'e', 'f' };

        /// <summary>
        /// 校验字符
        /// </summary>
        /// <param name="aChar">被校验字符</param>
        /// <param name="charArray">检验字符集</param>
        /// <returns></returns>
        private static bool CharInArray(char aChar, char[] charArray)
        {
            return (Array.Exists<char>(charArray, delegate(char a) { return a == aChar; }));
        }

        /// <summary>
        /// 二进制字符串转换为byte数组
        /// </summary>
        /// <param name="s">字符串</param>
        /// <returns>byte数组</returns>
        public static byte[] HexStringToByteArray(string s)
        {
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char aChar in s)
            {
                if (CharInArray(aChar, HexDigits))
                    sb.Append(aChar);
            }
            s = sb.ToString();
            int bufferlength;
            if ((s.Length % 2) == 1)
                bufferlength = s.Length / 2 + 1;
            else bufferlength = s.Length / 2;
            byte[] buffer = new byte[bufferlength];
            for (int i = 0; i < bufferlength - 1; i++)
                buffer[i] = (byte)Convert.ToByte(s.Substring(2 * i, 2), 16);
            if (bufferlength > 0)
                buffer[bufferlength - 1] = (byte)Convert.ToByte(s.Substring(2 * (bufferlength - 1), (s.Length % 2 == 1 ? 1 : 2)), 16);
            return buffer;
        }

        /// <summary>
        /// byte数组转换到字符
        /// </summary>
        /// <param name="data">byte数组</param>
        /// <returns>对应的字符串</returns>
        public static string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            return sb.ToString().ToUpper();
        }
    }
}
