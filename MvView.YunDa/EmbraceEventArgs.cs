using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using MvBarCode;

namespace MvView.YunDa
{
    /// <summary>
    /// 相机揽件信息事件
    /// </summary>
    public class EmbraceEventArgs : EventArgs
    {
        /// <summary>
        /// 条形码
        /// </summary>
        private BarCodeDescribe _BarCode;

        /// <summary>                               
        /// 揽件称重
        /// </summary>
        private double _Weight;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="image">揽件称重图片信息</param>
        /// <param name="result">一维码检测结果</param>
        /// <param name="weight">揽件重量</param>
        internal EmbraceEventArgs(BarCodeDescribe info, Double weight)
        {
            // 一维码信息
            this._BarCode = info;

            // 初始化揽件重量
            _Weight = weight;
        }

        /// <summary>
        /// 图像宽
        /// </summary>
        public Int32 Width
        {
            get 
            {
                if (_BarCode == null)
                {
                    throw new ArgumentNullException();
                }
                if(_BarCode.GrabImage == null)
                {
                    throw new ArgumentNullException();
                }
                return _BarCode.GrabImage.Width;
            }
        }

        /// <summary>
        /// 图像高
        /// </summary>
        public Int32 Height
        {
            get 
            {
                if (_BarCode == null)
                {
                    throw new ArgumentNullException();
                }
                if (_BarCode.GrabImage == null)
                {
                    throw new ArgumentNullException();
                }
                return _BarCode.GrabImage.Height;
            }
        }

        /// <summary>
        /// 揽件称重
        /// </summary>
        public Double Weight
        {
            get { return _Weight; }
        }

        /// <summary>
        /// 一维码信息
        /// </summary>
        public string BarCode
        {
            get 
            {
                if (_BarCode == null)
                {
                    throw new ArgumentNullException();
                }

                return _BarCode.Code;
            }
        }

        /// <summary>
        /// 揽件图片信息
        /// </summary>
        public Bitmap GrabImage
        {
            get
            {
                if (_BarCode == null)
                {
                    throw new ArgumentNullException();
                }

                return _BarCode.GrabImage;
            }
        }
    }
}
