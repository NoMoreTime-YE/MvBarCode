using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MvBarCode
{
    /// <summary>
    /// 一维码检测类
    /// </summary>
    public class MvBarCodeCore : IDisposable
    {

        /// <summary>
        /// 初始化算法
        /// </summary>
        /// <param name="width">分辨率宽</param>
        /// <param name="height">分辨率高</param>
        /// <returns></returns>
        public bool InitAlgorithm(Int32 width, Int32 height)
        {
            if (this.IsValid)
            {
                return false;
            }

            bool retVal = false;
            try
            {
                // 初始化算法配参
                MvSBcChannelParam param = new MvSBcChannelParam();
                //param.Width = width;
                //param.Height = height;
                param.Width = width / 2 + 1;
                param.Height = height / 2 + 1;

                int sz = bcCalMemSize(ref param);
                MvBarCodeGlobalVar.Log.InfoFormat("bcCalMemSize:{0}", sz.ToString());
                _hHandle = Marshal.AllocHGlobal(sz);
                if (_hHandle != IntPtr.Zero)
                {
                    // 初始化参数算法参数
                    int re=bcInit(_hHandle, ref param);
                    MvBarCodeGlobalVar.Log.InfoFormat("bcInit return:{0}", re.ToString());
                    retVal = true;
                    _bValid = true;
                }
            }
            catch (Exception e)
            {
                MvBarCodeGlobalVar.Log.Error("初始化算法失败 Marshal.AllocHGlobal(sz)", e);
            }
            return retVal;
        }

        /// <summary>
        /// 获取算法配参
        /// </summary>
        /// <param name="param">配参对象</param>
        /// <returns></returns>
        public Int32 GetConfig(ref MvSBcConfigParam param)
        {
            if (_hHandle == IntPtr.Zero)
            {
                return 0;
            }

            Int32 retVal = 0;
            try
            {
                int sz = Marshal.SizeOf(typeof(MvSBcConfigParam));
                // 获取算法配参
                retVal = bcGetConfig(_hHandle, ref param);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e.Message + "Error");
            }
            return retVal;
        }

        /// <summary>
        /// 设置算法参数
        /// </summary>
        /// <param name="param">算法配参</param>
        /// <returns></returns>
        public Int32 SetConfig(ref MvSBcConfigParam param)
        {
            if (_hHandle == IntPtr.Zero)
            {
                return 0;
            }

            Int32 retVal = 0;
            try
            {
                // 设置算法参数
                retVal = bcSetConfig(_hHandle, ref param);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e.Message + "Error");
            }
            return retVal;
        }

        /// <summary>
        /// 执行算法检测
        /// </summary>
        /// <param name="param">检测参数</param>
        /// <param name="result">检测结果</param>
        /// <returns></returns>
        public Int32 Process(ref MvSBcProcessParam param, ref MvSBcProcessResult result)
        {
            if (_hHandle == IntPtr.Zero)
            {
                return 0;
            }

            Int32 retVal = 0;
            try
            {
                //log.InfoFormat("执行算法bcProcess前 BcProcessParam param.DoTrainFlag: {0}  param.Image: {1}", retVal, param.DoTrainFlag, param.Image);
                // 执行算法检测
                retVal = bcProcess(_hHandle, ref param, ref result);
                //log.InfoFormat("执行算法bcProcess后 返回值: {0} BcProcessResult result.Num: {1}  result.Code: {2}", retVal,result.Num,result.Code);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e.Message + "Error");
            }
            return retVal;
        }

        public Int32 Process(MvsImageParam image, bool isTrain, ref MvCodeInfo[] codes)
        {
            Int32 retVal = 0;

            // 构造检测入参
            using (MvProcessParam pp = new MvProcessParam())
            {
                pp.Image = image;
                pp.TrainFlag = isTrain;

                // 申明检测出差
                MvSBcProcessResult pr = new MvSBcProcessResult();
                pr.Initialization();

                MvSBcProcessParam tmp = pp.Param;
                //log.InfoFormat("pp.Param.Image: {0}", pp.Param.Image);
                // 执行检测
                retVal = Process(ref tmp, ref pr);
                // 存储执行结构
                MvProcessResult result = new MvProcessResult();
                result.Result = pr;

                // 输出一维码信息，并返回结果
                codes = result.CodeInfo;
            }
            return retVal;
        }

        /// <summary>
        /// 销毁对象
        /// </summary>
        public void Dispose()
        {
            // 若初始化内存非空
            if (_hHandle != IntPtr.Zero)
            {
                // 执行内存清理
                Marshal.FreeHGlobal(_hHandle);
                _hHandle = IntPtr.Zero;
            }
        }


        /// <summary>
        /// 码信息
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct MvSBcCodeInfo
        {
            /// <summary>
            /// 码是否有效
            /// </summary>
            public int Valid;

            /// <summary>
            /// 码类型
            /// </summary>
            public int Type;

            /// <summary>
            /// 码数据信息
            /// </summary>
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
            //public char[] Code;
            public string Code;

            /// <summary>
            /// 码数量信息
            /// </summary>
            public int Len;

            /// <summary>
            /// 每个码的区域信息，MvsPtArray2D*  MvSPoint pts[4]
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public MvsCommon.MvSPoint[] pts;
            //public IntPtr pts;

            /// <summary>
            /// 构造函数
            /// </summary>
            //public void Initialization()
            //{
            //    Valid = 0;
            //    Type = 0;
            //    //Code = new char[100];
            //    Code = string.Empty;
            //    Len = 0;
            //    pts = new MvSPoint[4];
            //}
        }

       

        /// <summary>
        /// 通道相关参数
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        internal struct MvSBcChannelParam
        {
            /// <summary>
            /// 图像宽
            /// </summary>
            public int Width;

            /// <summary>
            /// 图像高
            /// </summary>
            public int Height;
        }

        /// <summary>
        /// 检测输入参数
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct MvSBcProcessParam
        {
            /// <summary>
            /// 图像裸数据，MvsImage*
            /// </summary>
            public IntPtr img;

            /// <summary>
            /// 训练标识
            /// </summary>
            public int doTrainFlag;

            /// <summary>
            /// 构造函数
            /// </summary>
            public void Initialization()
            {
                img = IntPtr.Zero;
                doTrainFlag = 0;
            }
        }

        /// <summary>
        /// 码识别结果
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct MvSBcProcessResult
        {
            /// <summary>
            /// 检测码数量
            /// </summary>
            public int Num;

            /// <summary>
            /// 码数据指针，BcCodeInfo*
            /// </summary>
            public IntPtr Code;


            /// <summary>
            /// 是否存在解码失败的区域
            /// </summary>
            public int decodeFailFlag;

            /// <summary>
            /// 构造函数
            /// </summary>
            public void Initialization()
            {
                Num = 0;
                Code = IntPtr.Zero;
                decodeFailFlag = 0;
            }
        }

        /// <summary>
        /// 算法配参结构体
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct MvSBcConfigParam
        {
            /// <summary>
            /// 码类型
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public int[] CodeType;

            /// <summary>
            /// ROI区域
            /// </summary>
            MvsCommon.MvSROI roiRect;


            /// <summary>
            /// 最小尺寸
            /// </summary>
            public int ElemMinWidth;

            /// <summary>
            /// 最大尺寸
            /// </summary>
            public int ElemMaxWidth;

            /// <summary>
            /// 扫描线条数
            /// </summary>
            public int ScanLines;


            /// <summary>
            /// ITF25是否进行条码校验,CHECKSUMTRUE：进行条码校验，CHECKSUMFALSE：不校验，默认不进行校验
            /// </summary>
            public int checkSumITF25;


            /// <summary>
            /// CODE39是否进行条码校验，CHECKSUMTRUE：进行条码校验，CHECKSUMFALSE：不校验，默认不进行校验
            /// </summary>
            public int checkSumCode39;			


            /// <summary>
            /// 最小单元高度
            /// </summary>
            public int MinHeight;

            /// <summary>
            /// 最小条码宽度
            /// </summary>
            public int MinWidth;

            /// <summary>
            /// 最大条码宽度
            /// </summary>
            public int MaxWidth;

            /// <summary>
            /// 最大条码宽度
            /// </summary>
            public int MaxHeight;

            /// <summary>
            /// 分割方法, LOCAL_SEGMENTATION:局部分割，GLOBAL_SEGMENTATION：全局分割
            /// </summary>
            public int segmentationMethod;

            /// <summary>
            /// 条码框显示方式，ACCURATE_BOX:精确显示，RAW_BOX：非精确显示
            /// </summary>
            public int boxDisplayMode;

            /// <summary>
            ///SAVE_DECODE_FAILED_IMAGE_VALID:打开保存解码失败图片功能
            //SAVE_DECODE_FAILED_IMAGE_INVALID：关闭保存解码失败图片功能
            /// </summary>
            public int saveDecodeFailImageFlag;    


            /*用于过滤不符合要求的条码的函数指针入口，具体过滤条件由外部定义，算法只输出符合要求的条码
              返回为TRUE(1)则为有效条码，返回为FALSE(0)则为无效条码，
              该参数的默认值为NULL（不定义过滤条件，表示解出的码都符合要求）*/
            /// <summary>
            /// 
            /// </summary>
            public IntPtr isValidBarcode;

            /// <summary>
            /// 需要识别的符合过滤条件的条码数量
            /// </summary>
            public int codeNum;

            /// <summary>
            /// 视野中最多可能出现的一维码的数量
            /// </summary>
            public int max1DCodeNum;

            /// <summary>
            /// 视野中最多可能出现的PDF417码的数量
            /// </summary>
            public int maxPDF417CodeNum;			


            /// <summary>
            /// 构造函数
            /// </summary>
            public void Initialization()
            {
                CodeType = new int[50];
                ElemMinWidth = 0;
                ElemMaxWidth = 0;
                ScanLines = 0;
                checkSumITF25 = 0;
                checkSumCode39 = 0;
                MinHeight = 0;
                MinWidth = 0;
                MaxWidth = 0;
                MaxHeight = 0;
                segmentationMethod = 0;
                boxDisplayMode = 0;
                saveDecodeFailImageFlag = 0;
                isValidBarcode = IntPtr.Zero;

                codeNum = 0;
                max1DCodeNum = 0;
                maxPDF417CodeNum = 0;
            }
        }


        /// <summary>
        /// 初始化算法
        /// </summary>
        /// <param name="hHandle">内存句柄</param>
        /// <param name="pParam">通道参数，BcChannelParam*</param>
        /// <returns></returns>
        [DllImport("BarCode.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int bcInit(IntPtr hHandle, ref MvSBcChannelParam pParam);

        /// <summary>
        /// 设置算法参数
        /// </summary>
        /// <param name="hHandle">内存句柄</param>
        /// <param name="pParam">配置参数，BcConfigParam*</param>
        /// <returns></returns>
        [DllImport("BarCode.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int bcSetConfig(IntPtr hHandle, ref MvSBcConfigParam pParam);

        /// <summary>
        /// 获取算法参数
        /// </summary>
        /// <param name="hHandle">内存句柄</param>
        /// <param name="pParam">配置参数，BcConfigParam*</param>
        /// <returns></returns>
        [DllImport("BarCode.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int bcGetConfig(IntPtr hHandle, ref MvSBcConfigParam pParam);

        /// <summary>
        /// 计算内存长度
        /// </summary>
        /// <param name="param">通道参数，BcChannelParam*</param>
        /// <returns></returns>
        [DllImport("BarCode.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int bcCalMemSize(ref MvSBcChannelParam param);

        /// <summary>
        /// 执行算法检测
        /// </summary>
        /// <param name="hHandle">内存句柄</param>
        /// <param name="pParam">检测相关参数，BcProcessParam*</param>
        /// <param name="pResult">检测结果，BcProcessResult*</param>
        /// <returns></returns>
        [DllImport("BarCode.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int bcProcess(IntPtr hHandle, ref MvSBcProcessParam pParam, ref MvSBcProcessResult pResult);

        // 内存句柄
        private IntPtr _hHandle = IntPtr.Zero;

        // 算法可用标识
        private bool _bValid = false;

        private log4net.ILog log = log4net.LogManager.GetLogger("MV Log");

        /// <summary>
        /// 算法是否可用
        /// </summary>
        public bool IsValid
        {
            get { return _bValid; }
        }

    }
}
