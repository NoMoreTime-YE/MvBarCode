using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MvBarCode
{
    /// <summary>
    /// 全局变量
    /// </summary>
    public static class MvBarCodeGlobalVar
    {
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// log实例
        /// </summary>
        public static log4net.ILog Log = log4net.LogManager.GetLogger("MV Log");

        /// <summary>
        /// 从扫码到客户端的显示时间
        /// </summary>
        public static Stopwatch ClientElapsed = new Stopwatch();

        /// <summary>
        /// 从将单号发送给服务器到最后的驱动时间
        /// </summary>
        public static Stopwatch PortElapsed = new Stopwatch();

        /// <summary>
        /// 是否读取本地图片作为条形码来源
        /// </summary>
        public static bool LocalImageMode = false;

        /// <summary>
        /// 本地图片路径
        /// </summary>
        public static string LocalImagePath = string.Empty;

        /// <summary>
        /// 初始化相机个数
        /// </summary>
        public static int InitCarmeraNum = 1;

        /// <summary>
        /// http接收地址
        /// </summary>
        public static string HttpEndpoint = string.Empty;

        /// <summary>
        /// 称的协议以及模式
        /// </summary>
        public static string ScaleMode = string.Empty;

        /// <summary>
        /// 条码缓冲区个数
        /// </summary>
        public static int BarcodeCacheNum = 0;

        /// <summary>
        /// 条码有效时间（毫秒）
        /// </summary>
        public static long BarcodeValidTime = 2000;

        /// <summary>
        /// 相机是否有自己的缓存
        /// </summary>
        public static bool CameraBarcodeCache = true;

        /// <summary>
        /// 大提高解码能力，降低解码速度（次要因素）
        /// </summary>
        //public static int scale = 4;

        /// <summary>
        /// 小提高解码能力，降低解码速度
        /// </summary>
        //public static int minFactor = 3;

        /// <summary>
        /// 一次最多解几个码
        /// </summary>
        public static int maxNum = 5;

        public static int segmentationMethod = 1;
        public static int ElemMaxWidth = 32;
        public static int ElemMinWidth = 2;

        public static int MinHeight = 10;
        public static int MinWidth = 50;
        public static int MaxHeight = 5000;
        public static int MaxWidth = 5000;

        /// <summary>
        /// 是否开启实时传图功能
        /// </summary>
        public static bool OpenLive = false;

        /// <summary>
        /// 是否开启扣面单功能
        /// </summary>
        public static bool OpenGetSheet = false;

        /// <summary>
        /// 是否开启保存图片功能
        /// </summary>
        public static bool IsSavePic = false;

        /// <summary>
        /// 过滤规则选择
        /// </summary>
        public enum BarcodeRuleType
        {
            /// <summary>
            /// 通用规则，包含字母数字和'-'
            /// </summary>
            common,

            /// <summary>
            /// 用于捕获上一站和下一站的条码
            /// </summary>
            yundaSiteCode,

            /// <summary>
            /// 韵达包裹条码
            /// </summary>
            yundaCode
        }

        /// <summary>
        /// 判断是否是64位的版本
        /// </summary>
        /// <returns></returns>
        public static bool Is64bit
        {
            get
            {
                return IntPtr.Size == 8;
            }
        }

        static MvBarCodeGlobalVar()
        {
          

            StringBuilder builder = new StringBuilder(256);
            GetPrivateProfileString("NormalParam", "LocalImageMode", "0", builder, 256, Environment.CurrentDirectory + "\\config.ini");
            int result;
            int.TryParse(builder.ToString(), out result);
            if (result == 0)
            {
                LocalImageMode = false;
            }
            else
            {
                LocalImageMode = true;
            }


            builder.Clear();
            GetPrivateProfileString("ViewParam", "InitCarmeraNum", "1", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!int.TryParse(builder.ToString().Trim(), out InitCarmeraNum))
            {
                InitCarmeraNum = 1;
            }

            builder.Clear();
            GetPrivateProfileString("ViewParam", "HttpEndpoint", "", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            HttpEndpoint = builder.ToString();

            builder.Clear();
            GetPrivateProfileString("WeightParam", "ScaleMode", "NoScale", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            ScaleMode = builder.ToString();
            if (ScaleMode == string.Empty)
                ScaleMode = "NoScale";


            builder.Clear();
            GetPrivateProfileString("NormalParam", "BarcodeCacheNum", "2", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if(!int.TryParse(builder.ToString().Trim(), out BarcodeCacheNum))
            {
                BarcodeCacheNum = 2;
            }

            builder.Clear();
            GetPrivateProfileString("NormalParam", "BarcodeValidTime", "2000", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!long.TryParse(builder.ToString().Trim(), out BarcodeValidTime))
            {
                BarcodeValidTime = 2000;
            }


            builder.Clear();
            GetPrivateProfileString("NormalParam", "CameraBarcodeCache", "1", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            int flag;
            if (!int.TryParse(builder.ToString().Trim(), out flag))
            {
                CameraBarcodeCache = true;
            }
            else
            {
                if (flag != 0)
                    CameraBarcodeCache = true;
                else
                    CameraBarcodeCache = false;
            }


            builder.Clear();
            GetPrivateProfileString("NormalParam", "LocalImagePath", "", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            LocalImagePath = builder.ToString();


            //builder.Clear();
            //GetPrivateProfileString("AlgorithmParam", "scale", "4", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            //if (!int.TryParse(builder.ToString().Trim(), out scale))
            //{
            //    scale = 4;
            //}

            //builder.Clear();
            //GetPrivateProfileString("AlgorithmParam", "minFactor", "3", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            //if (!int.TryParse(builder.ToString().Trim(), out minFactor))
            //{
            //    minFactor = 4;
            //}

            builder.Clear();
            GetPrivateProfileString("AlgorithmParam", "maxNum", "5", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!int.TryParse(builder.ToString().Trim(), out maxNum))
            {
                maxNum = 4;
            }

            builder.Clear();
            GetPrivateProfileString("AlgorithmParam", "segmentationMethod", "1", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!int.TryParse(builder.ToString().Trim(), out segmentationMethod))
            {
                segmentationMethod = 1;
            }

            builder.Clear();
            GetPrivateProfileString("AlgorithmParam", "ElemMaxWidth", "32", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!int.TryParse(builder.ToString().Trim(), out ElemMaxWidth))
            {
                ElemMaxWidth = 32;
            }

            builder.Clear();
            GetPrivateProfileString("AlgorithmParam", "ElemMinWidth", "2", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!int.TryParse(builder.ToString().Trim(), out ElemMinWidth))
            {
                ElemMinWidth = 2;
            }

            builder.Clear();
            GetPrivateProfileString("AlgorithmParam", "MinHeight", "10", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!int.TryParse(builder.ToString().Trim(), out MinHeight))
            {
                MinHeight = 10;
            }

            builder.Clear();
            GetPrivateProfileString("AlgorithmParam", "MinWidth", "50", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!int.TryParse(builder.ToString().Trim(), out MinWidth))
            {
                MinWidth = 50;
            }

            builder.Clear();
            GetPrivateProfileString("AlgorithmParam", "MaxHeight", "5000", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!int.TryParse(builder.ToString().Trim(), out MaxHeight))
            {
                MaxHeight = 5000;
            }

            builder.Clear();
            GetPrivateProfileString("AlgorithmParam", "MaxWidth", "5000", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            if (!int.TryParse(builder.ToString().Trim(), out MaxWidth))
            {
                MaxWidth = 5000;
            }

            builder.Clear();
            GetPrivateProfileString("NormalParam", "OpenLive", "0", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            int b;
            if (!int.TryParse(builder.ToString().Trim(), out b))
            {
                OpenLive = false;
            }
            else
            {
                if (b != 0)
                    OpenLive = true;
                else
                    OpenLive = false;
            }



            builder.Clear();
            GetPrivateProfileString("NormalParam", "OpenGetSheet", "0", builder, 1024, Environment.CurrentDirectory + "\\config.ini");
            int matting;
            if (!int.TryParse(builder.ToString().Trim(), out matting))
            {
                OpenGetSheet = false;
            }
            else
            {
                if (matting != 0)
                    OpenGetSheet = true;
                else
                    OpenGetSheet = false;
            }

            builder.Clear();
            GetPrivateProfileString("NormalParam", "IsSavePic", "0", builder, 256, Environment.CurrentDirectory + "\\config.ini");
            int.TryParse(builder.ToString(), out result);
            if (result == 0)
            {
                IsSavePic = false;
            }
            else
            {
                IsSavePic = true;
            }
        }
    }
}
