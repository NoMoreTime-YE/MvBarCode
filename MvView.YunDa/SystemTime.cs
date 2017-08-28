using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MvView.Time
{
    /// <summary>
    /// 系统时间结构体
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SystemDateTime
    {
        /// <summary>
        /// 年
        /// </summary>
        public ushort Year;

        /// <summary>
        /// 月
        /// </summary>
        public ushort Month;

        /// <summary>
        /// 每周的事件
        /// </summary>
        public ushort DayOfWeek;

        /// <summary>
        /// 日
        /// </summary>
        public ushort Day;

        /// <summary>
        /// 时
        /// </summary>
        public ushort Hour;

        /// <summary>
        /// 分
        /// </summary>
        public ushort Minute;

        /// <summary>
        /// 秒
        /// </summary>
        public ushort Second;

        /// <summary>
        /// 毫秒
        /// </summary>
        public ushort MilliSeconds;
    }

    /// <summary>
    /// 系统时间管理
    /// </summary>
    internal class SystemTime
    {
        /// <summary>
        /// 获取当前系统时间
        /// </summary>
        /// <param name="st">系统时间结构</param>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetLocalTime(ref SystemDateTime st);

        /// <summary>
        /// 设置当前系统时间
        /// </summary>
        /// <param name="st">系统时间结构</param>
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void SetLocalTime(ref SystemDateTime st);

        /// <summary>
        /// DateTime格式转换为SystemTime
        /// </summary>
        /// <param name="dt">DateTime格式时间</param>
        /// <returns>SystemTime格式时间</returns>
        public static SystemDateTime DataTimeToSystemTime(DateTime dt)
        {
            SystemDateTime st = new SystemDateTime();
            st.Year = Convert.ToUInt16(dt.Year);
            st.Month = Convert.ToUInt16(dt.Month);
            st.DayOfWeek = Convert.ToUInt16(dt.DayOfWeek);
            st.Day = Convert.ToUInt16(dt.Day);
            st.Hour = Convert.ToUInt16(dt.Hour);
            st.Minute = Convert.ToUInt16(dt.Minute);
            st.Second = Convert.ToUInt16(dt.Second);
            st.MilliSeconds = Convert.ToUInt16(dt.Millisecond);
            return st;
        }

        /// <summary>
        /// 同步本地事件
        /// </summary>
        /// <param name="dt">DateTime时间</param>
        public static void SetSystemTime(DateTime dt)
        {
            SystemDateTime st = SystemTime.DataTimeToSystemTime(dt);
            SetLocalTime(ref st);
        }

        /// <summary>
        /// 同步本地时间
        /// </summary>
        /// <param name="strTime">时间字符串，字符串格式为：</param>
        public static void SetSystemTime(SystemDateTime st)
        {
            SetLocalTime(ref st);
        }

        /// <summary>
        /// 同步本地时间
        /// </summary>
        /// <param name="strTime">时间字符串</param>
        public static void SetSystemTime(string strTime)
        {
            try
            {
                System.Globalization.DateTimeFormatInfo format = new System.Globalization.DateTimeFormatInfo();
                format.ShortDatePattern = "yyyy-MM-dd HH:mm:ss";
                DateTime dt = Convert.ToDateTime(strTime, format);
                SetSystemTime(dt);
            }
            catch (Exception e)
            {
                Debug.WriteLine("SetSystemTime exception, " + e.Message);
            }
        }

        /// <summary>
        /// 获取系统时间
        /// </summary>
        /// <returns>系统当前时间</returns>
        public static DateTime GetSystemTime()
        {
            SystemDateTime st = new SystemDateTime();
            GetLocalTime(ref st);

            return new DateTime(st.Year, st.Month, st.Day, st.Hour, st.Minute, st.Second, st.MilliSeconds);
        }
    }
}
