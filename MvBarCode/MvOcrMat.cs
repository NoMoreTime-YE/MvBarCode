using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MvBarCode
{
    /// <summary>
    /// 截取面单类
    /// </summary>
    public class MvOcrMat:IDisposable
    {

        private IntPtr m_handle = IntPtr.Zero;

        private MvSMattingProcessResult _omResult = MvSMattingProcessResult.GetInstance();


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="width">图像宽</param>
        /// <param name="height">图像高</param>
        /// <returns></returns>
        public bool InitAlgorithm(Int32 width,Int32 height)
        {
            MvSMattingChannelParam omChannelParam = MvSMattingChannelParam.GetInstance();

            omChannelParam.imgWidth = width;
            omChannelParam.imgHeight = height;

            //计算所需内存大小
            int memChannelSize = MVAPI.mattingCalMemSize(ref omChannelParam);

            m_handle = Marshal.AllocHGlobal(memChannelSize);

            //初始化
            int nResult = MVAPI.mattingInit(m_handle, ref omChannelParam);

            if (nResult > memChannelSize)
            {
                MVAPI.mattingExit(m_handle);
                return false;
            }

            return true;

        }

        /// <summary>
        /// 第一帧条码图像需要setconfig
        /// </summary>
        public void SetConfig()
        {
            MvSMattingConfigParam omConfigParam = MvSMattingConfigParam.GetInstance();
            MVAPI.mattingGetConfig(m_handle, ref omConfigParam);

            omConfigParam.isEmphasis = 2;//参数可自定义
            omConfigParam.isNewPackage = 1;//参数可自定义
            omConfigParam.offsetHorizon = 0;//参数可自定义
            omConfigParam.offsetVertical = 0;//参数可自定义

            MVAPI.mattingSetConfig(m_handle, ref omConfigParam);
        }

        /// <summary>
        /// 主处理函数
        /// </summary>
        /// <param name="bmp">传入图片</param>
        /// <param name="mcList">条码信息</param>
        /// <returns>传出图片</returns>
        public void MattingProcess(System.Drawing.Bitmap bmp, List<MvBarCode.MvCodeInfo> mcList)
        {

            MvSMattingProcessParam omParam = MvSMattingProcessParam.GetInstance();
            _omResult = MvSMattingProcessResult.GetInstance();

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            var ptrImage = bmpData.Scan0;

            MvsCommon.MvSImage img =  MvsCommon.MvSImage.GetInstance();
            img.Height = bmp.Height;
            img.Width = bmp.Width;
            img.ImageData = ptrImage;
            img.DataType = Convert.ToInt32(MvsImgDataType.MVS_IMGDTP_U8);
            img.Type = Convert.ToInt32(MvsImgType.MVS_IMGTP_UITL_Y);

            omParam.pSrcImg = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MvsCommon.MvSImage)));
            Marshal.StructureToPtr(img, omParam.pSrcImg, false);

            var ptrPoint = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MvsCommon.MvSPoint)) * 12);
            for (int j = 0; j < mcList.Count; j++)
            {
                for (int i = 0; i < mcList[j].Region.PtArray.Length; i++)
                {
                    MvsCommon.MvSPoint pt;

                    pt.x = mcList[j].Region.PtArray[i].X;
                    pt.y = mcList[j].Region.PtArray[i].Y;

                    Marshal.StructureToPtr(pt, ptrPoint + Marshal.SizeOf(typeof(MvsCommon.MvSPoint)) * i, false);
                }
            }

            omParam.pCodePos = ptrPoint;
            omParam.nCode = 1;


            MVAPI.mattingProcess(m_handle, ref omParam, ref _omResult);


            bmp.UnlockBits(bmpData);
        }

        public void SaveMattingPicture(string path, int ResizeRate)
        {
            MVAPI.mattingCompress(m_handle, _omResult.pSheetImg, ResizeRate, path);
        }

        public System.Drawing.Bitmap GetBitmap()
        {

            MvsCommon.MvSImage imageinfo =
              (MvsCommon.MvSImage)Marshal.PtrToStructure(_omResult.pSheetImg, typeof(MvsCommon.MvSImage));

            int width = imageinfo.Width, height = imageinfo.Height;

            var fmt = PixelFormat.Format8bppIndexed;
            Bitmap returnBmp = new Bitmap(width, height, fmt);
            var palette = returnBmp.Palette;
            for (var ii = 0; ii < 256; ii++)
            {
                palette.Entries[ii] = Color.FromArgb(ii, ii, ii);
            }
            returnBmp.Palette = palette;

            // 使用ptr指针指向的数据构造位图
            var d = returnBmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly | ImageLockMode.UserInputBuffer, fmt,
                new BitmapData
                {
                    PixelFormat = fmt,
                    Height = height,
                    Width = width,
                    Stride = width,
                    Scan0 = imageinfo.ImageData
                });
            returnBmp.UnlockBits(d);


            return returnBmp;
        }

        /// <summary>
        /// dispose
        /// </summary>
        public void Dispose()
        {
            MVAPI.mattingExit(m_handle);
            Marshal.FreeHGlobal(m_handle);
        }


        /// <summary>
        /// 图像宽高参数
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MvSMattingChannelParam
        {
            public int imgWidth;                           ///>图像宽高
            public int imgHeight;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MVConst.Len_30)]
            public IntPtr[] reserve;

            /// <summary>
            /// 参数初始化
            /// </summary>
            /// <returns></returns>
            public static MvSMattingChannelParam GetInstance()
            {
                MvSMattingChannelParam param = new MvSMattingChannelParam();
                param.imgWidth = 0;
                param.imgHeight = 0;
                param.reserve = new IntPtr[MVConst.Len_30];

                return param;
            }
        }

        /// <summary>
        /// 处理结果参数
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MvSMattingProcessResult
        {
            /// <summary>
            /// 快递单图像
            /// </summary>
            //public MvsImage imgSrc;
            public IntPtr pSheetImg;

            /// <summary>
            /// 包裹图像
            /// </summary>
            public IntPtr pPackageImg;

            /// <summary>
            /// 最好截单图像中码的个数
            /// </summary>
            public int nCode;

            /// <summary>
            /// 最好截单图像中码的坐标
            /// </summary>
            //public MvsImage imgSrc;
            public IntPtr pCodePos;

            /// <summary>
            /// 保留参数
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MVConst.Len_28)]
            public IntPtr[] reserved;

            /// <summary>
            /// 初始化
            /// </summary>
            /// <returns></returns>
            public static MvSMattingProcessResult GetInstance()
            {
                MvSMattingProcessResult param = new MvSMattingProcessResult();

                param.pSheetImg = IntPtr.Zero;
                param.pPackageImg = IntPtr.Zero;
                param.pCodePos = IntPtr.Zero;
                param.reserved = new IntPtr[MVConst.Len_29];

                return param;
            }
        }

        /// <summary>
        /// 算法配置参数
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MvSMattingConfigParam
        {
            /// <summary>
            /// 是否增强图像
            /// </summary>
            public int isEmphasis;

            /// <summary>
            /// 是否是新包裹
            /// </summary>
            public int isNewPackage;

            /// <summary>
            /// 水平边缘扩充
            /// </summary>
            public int offsetHorizon;

            /// <summary>
            /// 垂直边缘扩充
            /// </summary>
            public int offsetVertical;

            /// <summary>
            /// 垂直边缘扩充
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MVConst.Len_28)]
            public IntPtr[] reserve;

            /// <summary>
            /// 初始化
            /// </summary>
            /// <returns></returns>
            public static MvSMattingConfigParam GetInstance()
            {
                MvSMattingConfigParam param = new MvSMattingConfigParam();
                param.reserve = new IntPtr[MVConst.Len_28];

                return param;
            }
        }


        /// <summary>
        /// 处理函数入参
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MvSMattingProcessParam
        {
            /// <summary>
            /// 输入图像
            /// </summary>
            //public MvsImage imgSrc;
            public IntPtr pSrcImg;

            /// <summary>
            /// 码个数
            /// </summary>
            public int nCode;

            /// <summary>
            /// 码坐标
            /// </summary>
            //public MvsImage imgSrc;
            public IntPtr pCodePos;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MVConst.Len_29)]
            public IntPtr[] reserved;

            /// <summary>
            /// 初始化
            /// </summary>
            /// <returns></returns>
            public static MvSMattingProcessParam GetInstance()
            {
                MvSMattingProcessParam param = new MvSMattingProcessParam();

                param.pSrcImg = IntPtr.Zero;
                param.pCodePos = IntPtr.Zero;
                param.reserved = new IntPtr[MVConst.Len_29];

                return param;
            }
        }

    

        /// <summary>
        /// 扣面单接口类
        /// </summary>
        public static class MVAPI
        {
            //private const string SdkDllFilepath = @".\Matting32.dll";
            private const string SdkDllFilepath = @".\Matting64.dll";

            /// <summary>
            /// 模块内存大小计算
            /// </summary>
            /// <param name="param"></param>
            /// <returns></returns>
            [DllImport(SdkDllFilepath, CallingConvention = CallingConvention.Cdecl)]
            public static extern int mattingCalMemSize(ref MvSMattingChannelParam param);

            /// <summary>
            /// 模块内存分配及初始化
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="param"></param>
            /// <returns></returns>
            [DllImport(SdkDllFilepath, CallingConvention = CallingConvention.Cdecl)]
            public static extern int mattingInit(IntPtr handle, ref MvSMattingChannelParam param);


            /// <summary>
            /// 模块配置参数设置
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="param"></param>
            /// <returns></returns>
            [DllImport(SdkDllFilepath, CallingConvention = CallingConvention.Cdecl)]
            public static extern int mattingGetConfig(IntPtr handle, ref MvSMattingConfigParam param);

            /// <summary>
            /// 模块当前配置参数获取
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="param"></param>
            /// <returns></returns>
            [DllImport(SdkDllFilepath, CallingConvention = CallingConvention.Cdecl)]
            public static extern int mattingSetConfig(IntPtr handle, ref MvSMattingConfigParam param);

            /// <summary>
            /// 模块主处理函数
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="param"></param>
            /// <param name="result"></param>
            /// <returns></returns>
            [DllImport(SdkDllFilepath, CallingConvention = CallingConvention.Cdecl)]
            public static extern int mattingProcess(IntPtr handle, ref MvSMattingProcessParam param, ref MvSMattingProcessResult result);


            /// <summary>
            /// 图像压缩
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="pImage">MvSImage*</param>
            /// <param name="compressQuality">修改图像压缩质量，值为0-100，数值越大压缩图像越好，推荐值20</param>
            /// <param name="pathFile"></param>
            /// <returns></returns>
            [DllImport(SdkDllFilepath, CallingConvention = CallingConvention.Cdecl)]
            public static extern int mattingCompress(IntPtr handle, IntPtr pImage, int compressQuality, string pathFile);

            /// <summary>
            /// 模块退出
            /// </summary>
            /// <param name="handle"></param>
            /// <returns></returns>
            [DllImport(SdkDllFilepath, CallingConvention = CallingConvention.Cdecl)]
            public static extern int mattingExit(IntPtr handle);

            

        }
    }
}
