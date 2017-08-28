using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using MvBarCode;

namespace MvView.YunDa
{
    public class BarCodeDescribe
    {
        /// <summary>
        /// 原始帧数据图片
        /// </summary>
        private Bitmap _GrabImage;

        /// <summary>
        /// 一维码
        /// </summary>
        private string _Code;

        /// <summary>
        /// 区域
        /// </summary>
        private Point[] _Regions;

        /// <summary>
        /// 类型
        /// </summary>
        private int _Type;

        /// <summary>
        /// 条码扩展信息
        /// </summary>
        private string[] _ExtMessageInfo;

        /// <summary>
        /// 有效性
        /// </summary>
        private bool _Valid;

        /// <summary>
        /// 条码有效性
        /// </summary>
        DetectExceptionType _Exception;

        /// <summary>
        /// 空构造
        /// </summary>
        internal BarCodeDescribe()
        {
            _GrabImage = null;
            _Code = string.Empty;
            _Regions = null;
            _Type = 0;
            _ExtMessageInfo = null;
            _Valid = false;
            _Exception = DetectExceptionType.NoExceptionType;
        }

        /// <summary>
        /// 构造事件对象
        /// </summary>
        /// <param name="info"></param>
        internal BarCodeDescribe(Bitmap image, MvCodeInfo info)
        {
            _GrabImage = image;
            if (info != null)
            {
                _Code = new String(info.Code, 0, 0x0d);
                _Regions = info.Region.PtArray;
                _Type = info.Type;
                this._Valid = true;
                _Exception = DetectExceptionType.NoExceptionType;
            }
            else
            {
                _Code = String.Empty;
                _Regions = null;
                _Type = 0;
                this._Valid = false;
                _Exception = DetectExceptionType.NULLCodeExceptionType;
            }
            _ExtMessageInfo = null;
        }

        /// <summary>
        /// 异常构造
        /// </summary>
        /// <param name="e">异常类型</param>
        /// <param name="message">异常数据扩展消息</param>
        internal BarCodeDescribe(Bitmap image, DetectExceptionType e, string[] message)
        {
            _GrabImage = image;
            _Code = string.Empty;
            _Regions = null;
            _Type = 0;
            this._Valid = false;
            _Exception = e;
            _ExtMessageInfo = message;
        }

        /// <summary>
        /// 判断当前条码是否有效
        /// </summary>
        public bool Valid
        {
            get { return _Valid; }
            private set { _Valid = value; }
        }

        /// <summary>
        /// 获取内部异常
        /// </summary>
        public DetectExceptionType InternalException
        {
            get { return _Exception; }
        }

        /// <summary>
        /// 获取异常信息扩展数据
        /// </summary>
        public string[] ExceptionMessage
        {
            get { return _ExtMessageInfo; }
        }

        /// <summary>
        /// 获取条码
        /// </summary>
        public String Code
        {
            get { return _Code; }
            private set { _Code = value; }
        }

        /// <summary>
        /// 条码类型
        /// </summary>
        public int Type
        {
            get { return _Type; }
            private set { _Type = value; }
        }

        /// <summary>
        /// 条码区域
        /// </summary>
        public Point[] Regions
        {
            get { return _Regions; }
            private set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                _Regions = value.Clone() as Point[];
            }
        }

        /// <summary>
        /// 条码数据对应图像
        /// </summary>
        public Bitmap GrabImage
        {
            get { return _GrabImage; }
            private set { _GrabImage = value; }
        }

        /// <summary>
        /// 事件克隆
        /// </summary>
        /// <returns>克隆对象结果</returns>
        public BarCodeDescribe Clone()
        {
            BarCodeDescribe newObj = new BarCodeDescribe();
            newObj.Code = this.Code;
            newObj.Type = this.Type;
            newObj.Regions = this.Regions;
            newObj.GrabImage = this.GrabImage;
            return newObj;
        }
    }
}
