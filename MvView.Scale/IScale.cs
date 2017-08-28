using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Scale
{
    /// <summary>
    /// 称重接口类
    /// </summary>
    public interface IScale : IDisposable
    {
        /// <summary>
        /// 打开电子秤通道
        /// </summary>
        /// <param name="info">打开信息</param>
        /// <returns>操作结果</returns>
        bool Open(string info);

        /// <summary>
        /// 是否打开
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// 关闭电子秤通道
        /// </summary>
        /// <returns>操作结果</returns>
        bool Close();

        /// <summary>
        /// 同步获取重量信息
        /// </summary>
        /// <param name="barCode">一维码信息</param>
        /// <param name="weight">称重信息</param>
        /// <returns>操作结果</returns>
        bool Post(string barCode, Int32 timeout, ref Double weight);

        /// <summary>
        /// 称重事件
        /// </summary>
        event EventHandler<WeightEventArgs> ScaleWight;

        /// <summary>
        /// 异步获取称重信息
        /// </summary>
        /// <param name="barCode">一维码信息</param>
        /// <returns>操作结果</returns>
        bool AsyncPost(string barCode);

        /// <summary>
        /// 开始捕获重量数据
        /// </summary>
        /// <returns>操作结果</returns>
        bool Start();

        /// <summary>
        /// 停止捕获重量数据
        /// </summary>
        /// <returns>操作结果</returns>
        bool Stop();

        /// <summary>
        /// 最大误差范围
        /// </summary>
        Double MaxDeviation { set; }

        /// <summary>
        /// 样本数量
        /// </summary>
        Int32 SampleNum { set; }
    }
}
