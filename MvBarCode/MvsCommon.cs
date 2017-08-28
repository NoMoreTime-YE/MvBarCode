using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MvBarCode
{
    public static  class MvsCommon
    {
        /// <summary>
        /// 图像数据结构
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
        internal struct MvSImage
        {
            /// <summary>
            /// 图像格式
            /// </summary>
            public int Type;

            /// <summary>
            /// 存储类型
            /// </summary>
            public int DataType;

            /// <summary>
            /// 图像宽
            /// </summary>
            public int Width;

            /// <summary>
            /// 图像高
            /// </summary>
            public int Height;

            /// <summary>
            /// 有效数据区域
            /// </summary>
            public MvSRect2Di32 ROI;

            /// <summary>
            /// 图像所在坐标系, void*
            /// </summary>
            //public IntPtr Coordinate;

            /// <summary>
            /// 图片数据指针，uint8*
            /// </summary>
            public IntPtr ImageData;

            /// <summary>
            /// 图像掩码，uint8*
            /// </summary>
            public IntPtr Mask;

            /// <summary>
            /// 保留字段，int32_t*
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
            public IntPtr[] Reserved;

            /// <summary>
            /// 构造函数
            /// </summary>
            public static MvSImage GetInstance()
            {
                MvSImage m = new MvSImage();
                m.Type = 0;
                m.DataType = 0;
                m.Width = 0;
                m.Height = 0;
                m.ROI = new MvSRect2Di32();
                //Coordinate = IntPtr.Zero;
                m.ImageData = IntPtr.Zero;
                m.Mask = IntPtr.Zero;
                m.Reserved = new IntPtr[22];
                return m;
            }
        }

        /// <summary>
        /// 图像数据结构
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
        public struct MvSRect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
        public struct MvSPoint
        {
            /// <summary>
            /// X坐标
            /// </summary>
            public int x;

            /// <summary>
            /// Y坐标
            /// </summary>
            public int y;
        }

        /// <summary>
        /// Regtangle区域坐标
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
        public struct MvSRect2Di32
        {
            /// <summary>
            /// 左上
            /// </summary>
            public MvSPoint2Di32 UL;

            /// <summary>
            /// 右下
            /// </summary>
            public MvSPoint2Di32 LR;

            /// <summary>
            /// 构造函数
            /// </summary>
            public static MvSRect2Di32 GetInstance()
            {
                MvSRect2Di32 m = new MvSRect2Di32();
                m.UL = new MvSPoint2Di32();
                m.LR = new MvSPoint2Di32();

                return m;
            }
        }

        /// <summary>
        /// 二维点坐标
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
        public struct MvSPoint2Di32
        {
            /// <summary>
            /// X坐标
            /// </summary>
            public int x;

            /// <summary>
            /// Y坐标
            /// </summary>
            public int y;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
        public struct MvSROI
        {
            int num;                            //ROI个数

            //MvSRect2Di32*
            IntPtr rect;                 //ROI外接矩形

            //MvSImage*
            IntPtr img;                         //ROI图像
        }
    }

    /// <summary>
    /// 常量
    /// </summary>
    public static class MVConst
    {
        /// <summary>
        /// Len_10
        /// </summary>
        public const int Len_10 = 10;
        /// <summary>
        /// Len_22
        /// </summary>
        public const int Len_22 = 22;

        public const int Len_24 = 24;

        public const int Len_27 = 27;

        /// <summary>
        /// Len_28
        /// </summary>
        public const int Len_28 = 28;

        /// <summary>
        /// Len_29
        /// </summary>
        public const int Len_29 = 29;

        /// <summary>
        /// Len_30
        /// </summary>
        public const int Len_30 = 30;

        /// <summary>
        /// Len_31
        /// </summary>
        public const int Len_31 = 31;

        public const int Len_100 = 100;






    }
}
