using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MvBarCode
{
    /// <summary>
    /// 图像原始数据参数
    /// </summary>
    public class MvsImageParam : IDisposable
    {
        // 原始图像参数
        private MvsCommon.MvSImage _Param = new MvsCommon.MvSImage();

        // 图像内存是否需要释放
        private bool _bFree = false;

        // 图像大小
        private Int32 _ImageSize;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MvsImageParam()
        {
            _Param = MvsCommon.MvSImage.GetInstance();
        }

        /// <summary>
        /// 返回图片信息参数
        /// </summary>
        internal MvsCommon.MvSImage Param
        {
            get { return _Param; }
        }

        /// <summary>
        /// 图像宽
        /// </summary>
        public Int32 Width
        {
            get { return _Param.Width; }
            set { _Param.Width = value; }
        }

        /// <summary>
        /// 图像高
        /// </summary>
        public Int32 Height
        {
            get { return _Param.Height; }
            set { _Param.Height = value; }
        }

        public Int32 ImageSize
        {
            get { return _ImageSize; }
            set { _ImageSize = value; }
        }

        /// <summary>
        /// 图像数据类型
        /// </summary>
        public Int32 DataType
        {
            get { return _Param.DataType; }
            set { _Param.DataType = value; }
        }

        /// <summary>
        /// 图像类型
        /// </summary>
        public Int32 Type
        {
            get { return _Param.Type; }
            set { _Param.Type = value; }
        }

        /// <summary>
        /// 图像数据，内部分配内存保证图像数据的有效性
        /// </summary>
       public byte[] ImageData
        {
            set
            {
                // 图像数据非空
                if(value == null || value.Length == 0)
                {
                    throw new ArgumentNullException();
                }

                // 图像数据已经设置过，先清理全局内存，防止内存泄露
                /*
                if(_Param.ImageData != IntPtr.Zero)
                {
                    if (_bFree && this.ImageSize != value.Length)
                    {
                        Marshal.FreeHGlobal(_Param.ImageData);
                        _Param.ImageData = IntPtr.Zero;
                    }
                }
                */

                if (_Param.ImageData == IntPtr.Zero)
                {
                    // 分配全局内存，存储图像数据
                    _Param.ImageData = Marshal.AllocHGlobal(value.Length);
                    _bFree = true;
                }
                
                if(_Param.ImageData == IntPtr.Zero)
                {
                    throw new Exception("Alloc image data failed");
                }

                // 拷贝数据到全局内存中
                Marshal.Copy(value, 0, _Param.ImageData, value.Length);
            } 
        }

       /// <summary>
       /// 图像数据指针，需要外部调用方保证指针的有效性
       /// </summary>
       public IntPtr ImagePointer
       {
           set 
           { 
               // 保存外部数据指针
               _Param.ImageData = value;
               _bFree = false;
           }
        }

        /// <summary>
        /// 参数清理接口
        /// </summary>
        public void Dispose()
       {
           if (_Param.ImageData != IntPtr.Zero && _bFree)
           {
               Marshal.FreeHGlobal(_Param.ImageData);
               _Param.ImageData = IntPtr.Zero;
           }
       }
    }
}
