using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using MvBarCode;

namespace MvView.Core
{
    /// <summary>
    /// 一维码事件类
    /// </summary>
    public class BarCodeEventArgs : EventArgs
    {
        /// <summary>
        /// 一维码结果描述对象
        /// </summary>
        private BarCodeDescribe _Result;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="item">一维码描述信息</param>
        internal BarCodeEventArgs(BarCodeDescribe item)
        {
            _Result = item;
        }

        /// <summary>
        /// 一维码结果
        /// </summary>
        public BarCodeDescribe Result
        {
            get { return _Result; }
        }

        /// <summary>
        /// 对象克隆
        /// </summary>
        /// <returns></returns>
        public BarCodeEventArgs Clone()
        {
            return new BarCodeEventArgs(this.Result.Clone());
        }
    }

    /// <summary>
    /// 图片路径
    /// </summary>
    public class PicturePathEventArgs : EventArgs
    {
        private PicturePathDescribe _Result;

        public PicturePathEventArgs(PicturePathDescribe item)
        {
            _Result = item;
        }

        /// <summary>
        /// 图片结果
        /// </summary>
        public PicturePathDescribe Result
        {
            get
            {
                return _Result;
            }
        }
    }

}
