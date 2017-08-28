using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MvView.Tools
{
    /// <summary>
    /// 封装系统时间统计
    /// </summary>
    public class SystemClock
    {
        [DllImport("CLIDelegate.dll")]
        static extern Int64 SysClock_Now();

        [DllImport("CLIDelegate.dll")]
        static extern Int64 SysClock_MillisecSubtract(Int64 t1, Int64 t2);

        [DllImport("CLIDelegate.dll")]
        static extern Int64 SysClock_MicrosecSubtract(Int64 t1, Int64 t2);
        
        /// <summary>
        /// 返回当前CPU纳秒数
        /// </summary>
        /// <returns>纳秒时间</returns>
        public static Int64 Now()
        {
            return SysClock_Now();
        }

        /// <summary>
        /// 计算时间差值
        /// </summary>
        /// <param name="t1">Time1</param>
        /// <param name="t2">Time2</param>
        /// <returns>毫秒级的时间差</returns>
        public static Int64 MillisecSubtract(Int64 t1, Int64 t2)
        {
            return SysClock_MillisecSubtract(t1, t2);
        }

        /// <summary>
        /// 计算时间差值
        /// </summary>
        /// <param name="t1">Time1</param>
        /// <param name="t2">Time2</param>
        /// <returns>微秒级的时间差</returns>
        public static Int64 MicrosecSubtract(Int64 t1, Int64 t2)
        {
            return SysClock_MicrosecSubtract(t1, t2);
        }
    }
}
