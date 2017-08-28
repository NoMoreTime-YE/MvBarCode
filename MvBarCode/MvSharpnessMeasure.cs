using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MvBarCode
{
    public class MvSharpnessMeasure
    {

        //清晰度用
        private IntPtr m_handle = IntPtr.Zero;
        private int Width = 0;
        private int Height = 0;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="width">图像宽</param>
        /// <param name="height">图像高</param>
        /// <returns></returns>
        public bool InitAlgorithm(Int32 width, Int32 height)
        {
            this.Width = 4000;
            this.Height = 3000;

            //初始化清晰度的算法
            MvSSharpnessMeasureChannelParam smChannelParam = MvSSharpnessMeasureChannelParam.GetInstance();
            smChannelParam.imgWidth = Width;
            smChannelParam.imgHeight = Height;

            int memSize = MVSHARP.sharpnessMeasureCalMemSize(ref smChannelParam);

            MvBarCodeGlobalVar.Log.InfoFormat("sharpnessMeasureCalMemSize :{0}",memSize.ToString());
            m_handle = Marshal.AllocHGlobal(memSize);

            int result = MVSHARP.sharpnessMeasureInit(m_handle, ref smChannelParam);


            MvBarCodeGlobalVar.Log.InfoFormat("sharpnessMeasureInit return :{0}", result.ToString());

            if (result > memSize)
            {
                MVSHARP.sharpnessMeasureExit(m_handle);
                //记下错误日志
                //log.ErrorFormat("算法需要的内存{0}比最大支持的内存{1}大，此图片不进行清晰度", result, memSize);
            }

            return true;

        }

        /// <summary>
        /// 第一帧条码图像需要setconfig
        /// </summary>
        public void SetConfig()
        {

            MvSSharpnessMeasureConfigParam smConfigParam = MvSSharpnessMeasureConfigParam.GetInstance();
            MVSHARP.sharpnessMeasureGetConfig(m_handle, ref smConfigParam);

            smConfigParam.roi.left = 0;
            smConfigParam.roi.top = 0;
            smConfigParam.roi.right = Width - 1;
            smConfigParam.roi.bottom= Height - 1;
            smConfigParam.thScore = 30;

            MVSHARP.sharpnessMeasureSetConfig(m_handle, ref smConfigParam);

        }

        /// <summary>
        /// 主处理函数
        /// </summary>
        /// <param name="bmp">传入图片</param>
        /// <param name="mcList">条码信息</param>
        /// <returns>传出图片</returns>
        public int Process(Bitmap bmp, string code, List<MvBarCode.MvCodeInfo> mcList, int flag)
        {
            MvSSharpnessMeasureProcessParam processParam = MvSSharpnessMeasureProcessParam.GetInstance();
            MvSSharpnessMeasureProcessResult processResult = MvSSharpnessMeasureProcessResult.GetInstance();

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            var ptrImage = bmpData.Scan0;

            MvsCommon.MvSImage img = MvsCommon.MvSImage.GetInstance();
            img.Height = bmpData.Height;
            img.Width = bmpData.Width;
            img.ImageData = ptrImage;
            img.DataType = Convert.ToInt32(MvsImgDataType.MVS_IMGDTP_U8);
            img.Type = Convert.ToInt32(MvsImgType.MVS_IMGTP_UITL_Y);

            processParam.pSrcImg = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MvsCommon.MvSImage)));
            Marshal.StructureToPtr(img, processParam.pSrcImg, false);

            processParam.nRegion = 1;
            processParam.bCmpFlag = flag;



            var ptrPoint = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MvsCommon.MvSPoint)) * 12);
            IntPtr ptrContent = IntPtr.Zero;
            ptrContent = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MvSCodeContent)) * 10);
            for (int j = 0; j < mcList.Count; j++)
            {
                for (int i = 0; i < mcList[j].Region.PtArray.Length; i++)
                {
                    MvsCommon.MvSPoint pt;

                    pt.x = mcList[j].Region.PtArray[i].X;
                    pt.y = mcList[j].Region.PtArray[i].Y;

                    Marshal.StructureToPtr(pt, ptrPoint + Marshal.SizeOf(typeof(MvsCommon.MvSPoint)) * i, false);
                }

                MvSCodeContent codeContent = MvSCodeContent.GetInstance();
                Array.Copy(Encoding.Default.GetBytes(mcList[j].Code), codeContent.content, mcList[j].CodeLen);
                //codeContent.content = Encoding.Default.GetBytes(mcList[j].Code);
                codeContent.len = mcList[j].CodeLen;

                Marshal.StructureToPtr(codeContent, ptrContent + Marshal.SizeOf(typeof(MvSCodeContent)) * j, false);
                //Marshal.StructureToPtr(codeContent, ptrContent, false);
            }

            processParam.Pts = ptrPoint;

            processParam.CodeContent = ptrContent;

            try
            {
                int result = MVSHARP.sharpnessMeasureProcess(m_handle, ref processParam, ref processResult);
            }



            catch (Exception ex)
            {
               MvBarCodeGlobalVar.Log.Error(ex);
            }

            bmp.UnlockBits(bmpData);

            return processResult.score;
        }

        public void Dispose()
        {
            MVSHARP.sharpnessMeasureExit(m_handle);
            Marshal.FreeHGlobal(m_handle);
        }

        public struct MvSCodeContent
        {
            public int len;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MVConst.Len_100)]
            public byte[] content;

            public static MvSCodeContent GetInstance()
            {
                MvSCodeContent param = new MvSCodeContent();
                param.content = new byte[MVConst.Len_100];
                return param;
            }
        }


        public struct MvSSharpnessMeasureChannelParam
        {
            public int imgWidth;

            public int imgHeight;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MVConst.Len_30)]
            public IntPtr[] reserve;

            public static MvSSharpnessMeasureChannelParam GetInstance()
            {
                MvSSharpnessMeasureChannelParam param = new MvSSharpnessMeasureChannelParam();
                param.imgWidth = 0;
                param.imgHeight = 0;
                param.reserve = new IntPtr[MVConst.Len_30];

                return param;
            }
        }


        public struct MvSSharpnessMeasureConfigParam
        {
            /// <summary>
            /// 有效数据区域
            /// </summary>
            public MvsCommon.MvSRect roi;

            /// <summary>
            /// 阈值分数
            /// </summary>
            public int thScore;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MVConst.Len_24)]
            public IntPtr[] reserve;

            public static MvSSharpnessMeasureConfigParam GetInstance()
            {
                MvSSharpnessMeasureConfigParam param = new MvSSharpnessMeasureConfigParam();
                param.roi = new MvsCommon.MvSRect();
                param.reserve = new IntPtr[MVConst.Len_24];

                return param;
            }
        }


        public struct MvSSharpnessMeasureProcessParam
        {
            /// <summary>
            /// 输入图像
            /// </summary>
            //public MvSImage imgSrc;
            public IntPtr pSrcImg;

            /// <summary>
            /// 比较标志位
            /// </summary>
            public int bCmpFlag;

            /// <summary>
            /// 码区域个数
            /// </summary>
            public int nRegion;

            /// <summary>
            /// 点坐标信息，MvSPoint*
            /// </summary>
            public IntPtr Pts;

            /// <summary>
            /// 码内容 MvSCodeContent
            /// </summary>
            public IntPtr CodeContent;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MVConst.Len_27)]
            public IntPtr[] reserve;


            public static MvSSharpnessMeasureProcessParam GetInstance()
            {
                MvSSharpnessMeasureProcessParam param = new MvSSharpnessMeasureProcessParam();

                param.pSrcImg = IntPtr.Zero;
                param.bCmpFlag = 0;
                param.nRegion = 0;
                param.Pts = IntPtr.Zero;
                param.CodeContent = IntPtr.Zero;
                param.reserve = new IntPtr[MVConst.Len_27];

                return param;
            }
        }

       

        public struct MvSSharpnessMeasureProcessResult
        {


            public int score;

            /// <summary>
            /// 清晰原图
            /// </summary>
            //public MvSImage* imgSrc;
            public IntPtr pImage;

            /// <summary>
            /// 码个数
            /// </summary>
            public int nCode;

            /// <summary>
            /// 点坐标信息，MvSPoint*
            /// </summary>
            public IntPtr Pts;

            /// <summary>
            /// 码内容，step = 100;
            /// </summary>
            public IntPtr pCodeContent;

            /// <summary>
            /// 
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MVConst.Len_27)]
            public IntPtr[] reserve;

            public static MvSSharpnessMeasureProcessResult GetInstance()
            {
                MvSSharpnessMeasureProcessResult param = new MvSSharpnessMeasureProcessResult();
                param.score = 0;
                param.pImage = IntPtr.Zero;
                param.nCode = 1;
                param.Pts = IntPtr.Zero;
                param.pCodeContent = IntPtr.Zero;
                param.reserve = new IntPtr[MVConst.Len_27];

                return param;
            }
        }

        public static class MVSHARP
        {
            //private const string SdkDllFilepathSharpness = @".\SharpnessMeasure32.dll";
            private const string SdkDllFilepathSharpness = @".\SharpnessMeasure64.dll";
            /// <summary>
            /// 模块内存分配及初始化
            /// </summary>
            /// <returns></returns>
            [DllImport(SdkDllFilepathSharpness, CallingConvention = CallingConvention.Cdecl)]
            public static extern int sharpnessMeasureInit(IntPtr handle, ref MvSSharpnessMeasureChannelParam param);


            /// <summary>
            /// 模块当前配置参数获取
            /// </summary>
            /// <returns></returns>
            [DllImport(SdkDllFilepathSharpness, CallingConvention = CallingConvention.Cdecl)]
            public static extern int sharpnessMeasureSetConfig(IntPtr handle, ref MvSSharpnessMeasureConfigParam param);


            /// <summary>
            /// 模块配置参数设置
            /// </summary>
            /// <returns></returns>
            [DllImport(SdkDllFilepathSharpness, CallingConvention = CallingConvention.Cdecl)]
            public static extern int sharpnessMeasureGetConfig(IntPtr handle, ref MvSSharpnessMeasureConfigParam param);


            /// <summary>
            /// 
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="param"></param>
            /// <param name="result"></param>
            /// <returns></returns>
            [DllImport(SdkDllFilepathSharpness, CallingConvention = CallingConvention.Cdecl)]
            public static extern int sharpnessMeasureProcess(IntPtr handle, ref MvSSharpnessMeasureProcessParam param, ref MvSSharpnessMeasureProcessResult result);

            /// <summary>
            /// 模块内存大小计算
            /// </summary>
            /// <returns></returns>
            [DllImport(SdkDllFilepathSharpness, CallingConvention = CallingConvention.Cdecl)]
            public static extern int sharpnessMeasureCalMemSize(ref MvSSharpnessMeasureChannelParam param);

            /// <summary>
            /// 模块退出
            /// </summary>
            /// <returns></returns>
            [DllImport(SdkDllFilepathSharpness, CallingConvention = CallingConvention.Cdecl)]
            public static extern int sharpnessMeasureExit(IntPtr handle);
        }
    }


}

