using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using MvBarCode;

namespace MvView.Core
{
    /// <summary>
    /// 条码信息类
    /// </summary>
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
        private List<Point[]> _Regions = new List<Point[]>();

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
        /// 相机序列
        /// </summary>
        private int cameraIndex;

        /// <summary>
        /// 空构造
        /// </summary>
        internal BarCodeDescribe()
        {
            _GrabImage = null;
            _Code = string.Empty;
            _Regions.Clear();
            _Type = 0;
            _ExtMessageInfo = null;
            _Valid = false;
            _Exception = DetectExceptionType.NoExceptionType;
        }

        /// <summary>
        /// 条码信息类构造函数
        /// </summary>
        /// <param name="image">识别出条码的图片</param>
        /// <param name="info">条形码信息</param>
        /// <param name="hasBarcode">构造函数是否含有条码信息</param>
        /// <param name="cameraIndex">相机序列</param>
        internal BarCodeDescribe(Bitmap image, MvCodeInfo info, bool hasBarcode, int cameraIndex)
        {
            this.cameraIndex = cameraIndex;
            _GrabImage = image;
            if (info != null)
            {
                if (hasBarcode)
                {
                    _Code = new String(info.Code, 0, info.CodeLen);
                }
                else
                {
                    _Code = string.Empty;
                }
                _Regions.Add(info.Region.PtArray);
                _Type = info.Type;
                this._Valid = true;
                _Exception = DetectExceptionType.NoExceptionType;
            }
            else
            {
                _Code = String.Empty;
                _Regions.Clear();
                _Type = 0;
                this._Valid = false;
                _Exception = DetectExceptionType.NULLCodeExceptionType;
            }
            _ExtMessageInfo = null;
        }

        /// <summary>
        /// 条码信息类构造函数（异常信息）
        /// </summary>
        /// <param name="image">条码图片</param>
        /// <param name="e">异常信息类型</param>
        /// <param name="message">异常信息（条码数组）</param>
        /// <param name="infoList">条码类：用于取条码坐标</param>
        /// <param name="cameraIndex">相机序列</param>
        internal BarCodeDescribe(Bitmap image, DetectExceptionType e, string[] message,List<MvCodeInfo> infoList, int cameraIndex)
        {
            this.cameraIndex = cameraIndex;
            _GrabImage = image;
            _Code = string.Empty;
            foreach (var i in infoList)
            { _Regions.Add(i.Region.PtArray); }
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
        public List<Point[]> Regions
        {
            get { return _Regions; }
            private set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                _Regions = value;
            }
        }

        /// <summary>
        /// 相机序列
        /// </summary>
        public int CameraIndex
        {
            get { return this.cameraIndex; }
        }

        /// <summary>
        /// 条码数据对应图像（已经画出条码框）
        /// </summary>
        public Bitmap GrabImage
        {
            get
            {
                if (_Regions.Count == 0 || _Regions == null)
                    return _GrabImage;
                else
                {
                    Bitmap b = new Bitmap(_GrabImage.Width, _GrabImage.Height);
                    using (System.Drawing.Pen pen = new Pen(Brushes.Green, 20))
                    {
                        using (Graphics g = Graphics.FromImage(b))
                        {
                            g.DrawImage(_GrabImage, new Rectangle(0, 0, _GrabImage.Width, _GrabImage.Height), new Rectangle(0, 0, _GrabImage.Width, _GrabImage.Height), GraphicsUnit.Pixel);
                            for (int c = 0; c < _Regions.Count(); c++)
                            { g.DrawLines(pen, _Regions[c]); }
                        }
                    }
                    return b;
                }
            }
            private set { _GrabImage = value; }
        }

        /// <summary>
        /// 原始图像
        /// </summary>
        public Bitmap OriginalGrabImage
        {
            get
            {
                return _GrabImage;
            }
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

    /// <summary>
    /// 原始帧数据图片路径
    /// </summary>
    public class PicturePathDescribe
    {
        private string  _picPath;

        public PicturePathDescribe(string p)
        {
            _picPath = p;
        }

        /// <summary>
        /// 实时图片
        /// </summary>
        public string PicPath
        {
            get
            {
                return _picPath;
            }
        }
    }

}
