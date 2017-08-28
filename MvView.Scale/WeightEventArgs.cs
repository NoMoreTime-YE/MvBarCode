using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Scale
{
    /// <summary>
    /// 揽件称重事件
    /// </summary>
    public class WeightEventArgs : EventArgs
    {
        // 重量
        private Double _Weight;

        // 一维码信息
        private String _BarCode;

        // 稳定称重
        private bool _bReal;

        // 相机输出
        public bool _bNeedOutPut = false;

        /// <summary>
        /// 揽件称重事件构造
        /// </summary>
        /// <param name="arg">揽件称重</param>
        internal WeightEventArgs(Double arg)
        {
            _Weight = arg;
        }

        /// <summary>
        /// 截取数值到指定精度位
        /// </summary>
        /// <param name="val">原数值</param>
        /// <param name="s">精度位数</param>
        /// <returns>截取后的数值</returns>
        private static double ToFixed(double val, int s)
        {
            double sp = Math.Pow(10, s);
            return Math.Truncate(val) + Math.Floor((val - Math.Truncate(val)) * sp) / sp;
        }

        /// <summary>
        /// 重量
        /// </summary>
        public Double Weight
        {
            get { return ToFixed(_Weight, 2); }
        }

        /// <summary>
        /// 一维码信息
        /// </summary>
        public string BarCode
        {
            get { return _BarCode; }
            set { _BarCode = value; }
        }

        /// <summary>
        /// 稳定称重标识
        /// </summary>
        public bool RealWeight
        {
            get { return _bReal; }
            internal set { _bReal = value; }
        }

        /// <summary>
        /// 克隆对象
        /// </summary>
        /// <returns>称重事件</returns>
        public WeightEventArgs Clone()
        {
            return new WeightEventArgs(this.Weight);
        }
    }
}
