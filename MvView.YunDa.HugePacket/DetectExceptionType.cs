using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Core
{
    /// <summary>
    /// 条码检测异常类型
    /// </summary>
    public enum DetectExceptionType
    {
        /// <summary>
        /// 正常条码，无异常
        /// </summary>
        NoExceptionType = 0,

        /// <summary>
        /// 空条码数据
        /// </summary>
        NULLCodeExceptionType,

        /// <summary>
        /// 一帧数据中检测到多个条码数据
        /// </summary>
        MulBarCodeExceptionType,

        /// <summary>
        /// 条码长度异常
        /// </summary>
        OutOfLengthRangeExceptionType,

        /// <summary>
        /// 不在有效条码规则范围
        /// </summary>
        OutOfRulesExceptionType
    }
}
