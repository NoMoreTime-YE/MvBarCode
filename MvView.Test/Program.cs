using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvView.YunDa;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MvView.Test
{
    class Program
    {
        static void OnBarCode(object sender, BarCodeEventArgs e)
        {
            if (e.Result.Valid)
            {
                Console.WriteLine(e.Result.Code);
            }
            if (e.Result.InternalException == DetectExceptionType.MulBarCodeExceptionType)
            { 
                foreach (var exception in e.Result.ExceptionMessage)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        [DllImport("CLIDelegate.dll")]
        static extern Int64 SysClock_Now();

        [DllImport("CLIDelegate.dll")]
        static extern Int64 SysClock_MillisecSubtract(Int64 t1, Int64 t2);

        [DllImport("CLIDelegate.dll")]
        static extern Int64 SysClock_MicrosecSubtract(Int64 t1, Int64 t2);

        static void OnWeight(object sender, Scale.WeightEventArgs e)
        {
            Console.WriteLine(e.Weight);
        }

        static int nTag = 0;
        public static void BmpConvert(Bitmap src)
        {
            long t1 = SysClock_Now();
            Bitmap dst = ZoomImage(src, 0.2f);
            long t2 = SysClock_Now();
            dst.Save("./pic/dst/" + ++nTag + ".bmp", ImageFormat.Bmp);

            long t = SysClock_MicrosecSubtract(t1, t2);
            Console.WriteLine("receive a new image, " + t + "ns");
        }

        /// <summary>
        /// 按照图像尺寸等比缩放
        /// </summary>
        /// <param name="src">图像源</param>
        /// <param name="rate">图像缩放倍率，0 ~ 1 缩小， 1 ~ 放大</param>
        /// <returns>缩放后的图像</returns>
        public static Bitmap ZoomImage(Bitmap src, double rate)
        {
            // 参数校验
            if (src == null || rate <= 0)
            {
                return null;
            }

            // 初始化目标图像的宽和高
            int nWidth = Convert.ToInt32(src.Width * rate);
            int nHeght = Convert.ToInt32(src.Height * rate);
            Rectangle fromRect = new Rectangle(0, 0, src.Width, src.Height);
            Rectangle toRect = new Rectangle(0, 0, nWidth, nHeght);

            try
            {
                // 产生一个新的bitmap图像
                Bitmap bmp = new Bitmap(nWidth, nHeght);
                using ( Graphics g = Graphics.FromImage(bmp))
                {
                    // 用黑色清空
                    //g.Clear(Color.Black);

                    // 指定高质量的双三次插值法，执行预筛选以确保高质量的收缩
                    //g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    // 指定高质量低速度呈现
                    //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                    // 根据原区域绘制目标区域
                    g.DrawImage(src, toRect, fromRect, GraphicsUnit.Pixel);
                }
                return bmp;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;
        }

        static void OnEmbrace(object sender, EmbraceEventArgs e)
        {
            var img = e.GrabImage;
            if (img != null)
            {

            }
        }

        static void Main(string[] args)
        {
            //System.Media.SoundPlayer sp = new System.Media.SoundPlayer();
            //sp.SoundLocation = @"错误提示音.wav";
            //sp.PlayLooping();
            //sp.Play();

            DeviceManager dm = DeviceManager.Instance;
            dm.Initialization(string.Empty, "YaoHuaA7");

            
            string deviceId = dm.DeviceID;
            string mac = dm.MacAddress;

            if (!dm.IsOpen)
            {
                dm.Open();
                dm.BarCodeHandle += OnBarCode;
                dm.WeightHandle += OnWeight;
                dm.EmbraceHandle += OnEmbrace;
            }

            dm.Start();

            EventWaitHandle exit = new EventWaitHandle(false, EventResetMode.ManualReset);
            exit.WaitOne();
        }
    }
}
