using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThridLibray;
using System.Threading.Tasks;
using System.Threading;
using MvBarCode;
using System.Diagnostics;
using MvView.Scale;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;

namespace MvView.Core
{
    /// <summary>
    /// 设备层相机对象类
    /// </summary>
    public class BarCodeCamera : IDisposable
    {

        // 设备序列号
        private int _DeviceIndex;

        // 相机对象
        private IDevice _Camera;

        // 图像宽
        private Int32 _ImageWidth;

        // 图像高
        private Int32 _ImageHeight;

        // 图像宽上限
        private Int32 _MaxImageWidth;

        // 图像高上限
        private Int32 _MaxImageHeight;


        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);

        private MvOcrMat COcrMatting;

        private MvSharpnessMeasure CSharpnessMeasure;

        private BarcodeRuleFilter _barcodeRuleFilter;

        /// <summary>
        /// 构造相机
        /// </summary>
        /// <param name="index"></param>
        public BarCodeCamera(int index)
        {
            _DeviceIndex = index;

            _barcodeRuleFilter = new BarcodeRuleFilter();
        }

        /// <summary>
        /// 当前相机是否有效
        /// </summary>
        public bool Valid
        {
            get { return _Camera != null; }
        }

        /// <summary>
        /// 初始化相机
        /// </summary>
        private void InitCamera()
        {
            // 根据IP地址获取设备
            _Camera = Enumerator.GetDeviceByIndex(_DeviceIndex);
        }

        /// <summary>
        /// 设备序列号
        /// </summary>
        public string DeviceID
        {
            get
            {
                // 相机为空时初始化相机
                if(_Camera == null)
                {
                    InitCamera();
                }

                // 初始化相机失败抛出异常
                if(_Camera == null)
                {
                    throw new InvalidOperationException();
                }

                // 获取相机设备序列号
                string key = _Camera.DeviceKey;
                if (!CheckDeviceKey(key))
                {
                    return string.Empty;
                }

                // 截取序列号后15位
                key = key.Substring(key.Length - 0x0f);
                StringBuilder sb = new StringBuilder();
                foreach(var c in key)
                {
                    if(c >= 0x30 && c <= 0x39)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append((Convert.ToInt32(c) % 10).ToString());
                    }
                }

                // 韵达系统中大华设备第一个序列号固定为2
                sb[0] = '2';
                return sb.ToString();
            }
        }

        /// <summary>
        /// 接口mac地址信息
        /// </summary>
        public string InterfaceMacAddress
        {
            get
            {
                // 相机为空时初始化相机
                if (_Camera == null)
                {
                    InitCamera();
                }

                // 初始化相机失败抛出异常
                if(_Camera == null)
                {
                    throw new InvalidOperationException();
                }

                // 获取与相机通讯网卡接口信息
                string mac = string.Empty;
                string ip = string.Empty;
                string mask = string.Empty;
                string gateway = string.Empty;
                //if(ThridLibray.Enumerator.GigeInterfaceInfo(_Camera.Index, out mac, out ip, out mask, out gateway))
                //{
                //    if(mac != string.Empty)
                //    {
                //        return mac.Replace(":", "").Replace(" ", "").Replace("-", "");
                //    }
                //}
                return string.Empty;
            }
        }

        // 扫码线程
        private Thread _BarCodeThread;

        //截取面单线程
        private Thread _CutSheetThread;

        // 算法执行标识
        private long _bAlgorithmRunning = 0;

        // 相机采集标识
        private long _bCollection = 0;

        // 新条码标识
        private long _bNewBarcode = 0;

        // 帧缓冲对象
        private IGrabbedRawData _FrameData = null;

        /// <summary>
        /// 开启码流
        /// </summary>
        public void StartGrab()
        {


            new TaskFactory().StartNew(() => 
            {
                
                if ((_Camera == null)&&(!MvBarCode.MvBarCodeGlobalVar.LocalImageMode))
                {
                    MvBarCode.MvBarCodeGlobalVar.Log.Info("Camera is not opened.");
                    throw new InvalidOperationException("Camera is not opened.");
                }

                // 开启一维码检测线程
                if (_BarCodeThread == null)
                {
                    MvBarCode.MvBarCodeGlobalVar.Log.Info("初始化一维码检测线程");
                    _BarCodeThread = new Thread(DoBarCode);  
                }

                // 若线程未启动，启动线程
                if (!_BarCodeThread.IsAlive)
                {
                    MvBarCode.MvBarCodeGlobalVar.Log.Info("开启一维码检测线程");
                    _bAppStatus = true;
                    _BarCodeThread.Priority = ThreadPriority.Highest;
                    _BarCodeThread.Start();
                }

                Interlocked.Exchange(ref _bAlgorithmRunning, 0);


                if (MvBarCodeGlobalVar.OpenGetSheet)
                {
                    //截取面单线程
                    if (_CutSheetThread == null)
                    {
                        MvBarCode.MvBarCodeGlobalVar.Log.Info("开启截取面单线程");
                        _CutSheetThread = new Thread(DoCutSheet);
                        _CutSheetThread.Start();

                    }
                }

                // 打开码流通道
                if (!MvBarCode.MvBarCodeGlobalVar.LocalImageMode)
                {
                    if (!_Camera.IsGrabbing)
                    {
                        MvBarCode.MvBarCodeGlobalVar.Log.Info("开启当前线程相机码流通道");
                        if (!_Camera.StreamGrabber.Start(GrabStrategyEnum.grabStrartegySequential, GrabLoop.ProvidedByStreamGrabber))
                        {
                            MvBarCode.MvBarCodeGlobalVar.Log.ErrorFormat("打开码流通道失败");
                        }
                    }
                    else
                    {
                        MvBarCode.MvBarCodeGlobalVar.Log.Info("当前相机已经在获取码流");
                    }
                }
                else
                {
                    //开启本地线程获取_FrameData帧数据
                    new TaskFactory().StartNew(() => {
                        MvBarCode.MvBarCodeGlobalVar.Log.Info("开始读取本地图片");
                        while (_bAppStatus)
                        {
                            LocalImageGrabbing();
                        }
                    });
                }
                Interlocked.CompareExchange(ref _bCollection, 1, 0);
            }).Wait();
        }



        private void LocalImageGrabbing()
        {
            List<Bitmap> bitmapList = new List<Bitmap>();
            LocalFrameData bitmapData;

            System.Drawing.Imaging.BitmapData bmpData;
            try
            {
                if (MvBarCode.MvBarCodeGlobalVar.LocalImagePath == string.Empty)
                {
                    Console.WriteLine("没有本地目录");
                    return;
                }


                string imgType = "*.bmp|*.png|*.jpg|*.jpeg";
                string[] ImgType = imgType.Split('|');
                List<string> Dirs = new List<string>();
                for (int i = 0; i < ImgType.Length; i++)
                {
                    Dirs.AddRange(Directory.GetFiles(string.Format(MvBarCode.MvBarCodeGlobalVar.LocalImagePath), ImgType[i]));
                }
                //_FrameData
                //string[] dirs = Directory.GetFiles();

                foreach (var d in Dirs)
                {
                    Bitmap img = new Bitmap(d);

                    bitmapList.Add(img);
                }
                
                foreach (var b in bitmapList)
                {
                    Thread.Sleep(1000);

                    if (Interlocked.CompareExchange(ref _bAlgorithmRunning, 1, 0) == 0)
                    {
                       
                        Rectangle rect = new Rectangle(0, 0, b.Width, b.Height);
                        bmpData = b.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, b.PixelFormat);
                        bitmapData = new LocalFrameData(b.Width, b.Height, b, bmpData.Scan0);
                        b.UnlockBits(bmpData);

                        _FrameData = bitmapData;

                        _DetectBarCodeEvent.Set();

                    }
                }

                Thread.Sleep(3000);

                foreach(var b in bitmapList)
                {
                    //b.Dispose();
                }
            }
            catch(Exception e)
            {
                MvBarCode.MvBarCodeGlobalVar.Log.Error(e);
            }

        }
        private  byte[] Bitmap2Byte(Bitmap bmp)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, ImageFormat.Jpeg);
                byte[] data = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(data, 0, Convert.ToInt32(stream.Length));
               
                return data;
            }
        }

        /// <summary>
        /// 一维码事件
        /// </summary>
        public event EventHandler<BarCodeEventArgs> BarCodeHandle;

        public event EventHandler<PicturePathEventArgs> PicturePathHandle;

        // 程序运行状态
        private bool _bAppStatus = false;


        private EventWaitHandle _DetectBarCodeEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        private EventWaitHandle _ExitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private EventWaitHandle _CutSheetHanle = new EventWaitHandle(false,EventResetMode.AutoReset);
        private EventWaitHandle _DoneCutSheetHandle = new EventWaitHandle(true, EventResetMode.AutoReset);

        WaitHandle[] cutSheetHanless;
        WaitHandle[] doneCutSheetHandless;

        // 记录最后一个扫描的条形码数据
        private string _LastBarCode = string.Empty;

        // 记录CodeInfo信息
        List<MvCodeInfo> mci = new List<MvCodeInfo>();

        /// <summary>
        /// 算法初始化锁
        /// </summary>
        public static object locker = new object();


        private string barCodePicLocation = string.Empty;
        private void DoBarCode()
        {
            try
            {
                if(MvBarCode.MvBarCodeGlobalVar.LocalImageMode)
                {
                    _ImageWidth = 4000;
                    _ImageHeight = 3000;
                }

                // 初始化算法

                MvBarCodeGlobalVar.Log.Info("DoBarCode:开始初始化算法");
                MvBarCodeCore magic = new MvBarCodeCore();
                lock (locker)
                {
                    if (!magic.InitAlgorithm(this.Width, this.Height))
                    {
                        MvBarCodeGlobalVar.Log.Error("InitAlgorithm failed");
                        Debug.WriteLine("InitAlgorithm failed");
                        return;
                    }

                    MvBarCodeGlobalVar.Log.Info("DoBarCode:结束初始化一维码算法");

                    if (MvBarCode.MvBarCodeGlobalVar.OpenGetSheet)
                    {
                        COcrMatting = new MvOcrMat();
                        COcrMatting.InitAlgorithm(this.Width, this.Height);
                    }
					
				    CSharpnessMeasure = new MvSharpnessMeasure();
                    CSharpnessMeasure.InitAlgorithm(this.Width, this.Height);
                    CSharpnessMeasure.SetConfig();

                    MvBarCodeGlobalVar.Log.Info("DoBarCode:结束初始化清晰度算法");
                }

                MvBarCodeGlobalVar.Log.Info("DoBarCode:结束初始化算法");


                // 初始化算法配参
                MvBarCodeCore.MvSBcConfigParam param = new MvBarCodeCore.MvSBcConfigParam();
                param.Initialization();
                magic.GetConfig(ref param);
                param.CodeType[0] = 2;
                param.CodeType[1] = 1;

                param.codeNum = MvBarCodeGlobalVar.maxNum;
                param.max1DCodeNum = MvBarCodeGlobalVar.maxNum;
                param.segmentationMethod = MvBarCodeGlobalVar.segmentationMethod;
                param.ElemMaxWidth = MvBarCodeGlobalVar.ElemMaxWidth;
                param.ElemMinWidth = MvBarCodeGlobalVar.ElemMinWidth;

                param.MinHeight = MvBarCodeGlobalVar.MinHeight;
                param.MinWidth = MvBarCodeGlobalVar.MinWidth;
                param.MaxHeight = MvBarCodeGlobalVar.MaxHeight;
                param.MaxWidth = MvBarCodeGlobalVar.MaxWidth;

                magic.SetConfig(ref param);

                // 记录最新条形码信息
                List<string> newCodes = new List<string>();


                // 算法运行标识
                WaitHandle[] handles = new WaitHandle[] { _DetectBarCodeEvent, _ExitHandle };
                //截取面单标识符
                cutSheetHanless = new WaitHandle[] { _CutSheetHanle, _ExitHandle };
                //结束截取面单标识符
                doneCutSheetHandless = new WaitHandle[] { _DoneCutSheetHandle, _ExitHandle };

                MvBarCodeGlobalVar.Log.Info("DoBarCode:结束算法配置参数");
                // 算法运算
                while (_bAppStatus)
                {
                    // 等待采样结束
                    if (WaitHandle.WaitAny(handles) != 0)
                    {
                        break;
                    }

                    // 若当前无检测数据，线程休息1ms
                    if (Interlocked.Read(ref _bAlgorithmRunning) == 0)
                    {
                        continue;
                    }

                    // 检测指定图片
                    MvCodeInfo[] ci = null;
                    using (MvsImageParam img = new MvsImageParam())
                    {
                        // 赋值图像样本参数
                        img.Width = _FrameData.Width;
                        img.Height = _FrameData.Height;
                        img.DataType = Convert.ToInt32(MvsImgDataType.MVS_IMGDTP_U8);
                        img.Type = Convert.ToInt32(MvsImgType.MVS_IMGTP_UITL_Y);

                        if (!MvBarCode.MvBarCodeGlobalVar.LocalImageMode)
                            img.ImageData = _FrameData.Image;
                        else
                            img.ImagePointer = _FrameData.Raw;

                        // 检测算法
                        magic.Process(img, false, ref ci);
                    }

                    // 返回结果为空，继续算法运算
                    if (ci == null)
                    {
                        if (MvBarCodeGlobalVar.OpenLive)
                        {
                            BarCodeHandle(this, new BarCodeEventArgs(new BarCodeDescribe(_FrameData.ToBitmap(false), null, false, _DeviceIndex)));
                        }
                        // 重置检测状态,重新采样
                        Interlocked.CompareExchange(ref _bAlgorithmRunning, 0, 1);
                        continue;
                    }

                    // 重置所有检测标识
                    mci.Clear();
                    newCodes.Clear();

                    // 捕获条形码数据
                    foreach (var item in ci)
                    {
                        if (_barcodeRuleFilter.CheckAndCatchBarcode(item, ref newCodes,MvBarCodeGlobalVar.BarcodeRuleType.common))
                        {
                            mci.Add(item);
                        }
                    }

                    if (newCodes.Count > 0)
                    {
                        try
                        {

                            //开启截取面单才需要等待
                            if (COcrMatting != null)
                            {
                                //_DoneCutSheetHandle.WaitOne();
                                if (WaitHandle.WaitAny(doneCutSheetHandless) != 0)
                                {
                                    break;
                                }
                            }

                            // 检测是否有多个条码数据
                            if (newCodes.Count > 1)
                            {
                                _CutSheetHanle.Set();

                                if (BarCodeHandle != null)
                                {
                                    BarCodeHandle(this, new BarCodeEventArgs(new BarCodeDescribe(_FrameData.ToBitmap(false),
                                        DetectExceptionType.MulBarCodeExceptionType, newCodes.ToArray(), mci, _DeviceIndex)));
                                }
                            }
                            // 判断重复条码(暂时去掉相机缓存)
                            else if (0 != string.Compare(_LastBarCode, newCodes[0]))
                            {

                                //相机是否需要自己的缓存
                                if (MvBarCode.MvBarCodeGlobalVar.CameraBarcodeCache)
                                    _LastBarCode = newCodes[0];

                                Interlocked.Exchange(ref _bNewBarcode, 1);

                                //通知截取面单可以开始
                                _CutSheetHanle.Set();

                                if (BarCodeHandle != null)
                                {
                                    BarCodeHandle(this, new BarCodeEventArgs(new BarCodeDescribe(_FrameData.ToBitmap(false), mci[0], true, _DeviceIndex)));
                            }



                            }
                            //有一个条码，但是是重复条码
                            else
                            {
                                _CutSheetHanle.Set();

                                if (MvBarCodeGlobalVar.OpenLive)
                                {
                                    if (BarCodeHandle != null)
                                    {
                                        BarCodeHandle(this, new BarCodeEventArgs(new BarCodeDescribe(_FrameData.ToBitmap(false), mci[0], false, _DeviceIndex)));
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MvBarCodeGlobalVar.Log.Error(e);
                        }
                    }
                    //实时传图片
                    else
                    {
                        if (MvBarCodeGlobalVar.OpenLive)
                        {
                            BarCodeHandle(this, new BarCodeEventArgs(new BarCodeDescribe(_FrameData.ToBitmap(false), null, false, _DeviceIndex)));
                        }
                    }

                    // 重新采样
                    Interlocked.CompareExchange(ref _bAlgorithmRunning, 0, 1);
                    Thread.Sleep(1);
                }
            }
            catch (Exception e1)
            {
                MvBarCodeGlobalVar.Log.Error(e1);
            }
        }

        private Bitmap _cutSheetBitmap = null;
        private string _cutSheetBarcode = string.Empty;

        //截取面单
        private void DoCutSheet()
        {
            //Thread.Sleep(3000);
            while (_bAppStatus)
            {
                //wait _FrameData & mci & _bNewBarcode
                //_CutSheetHanle.WaitOne();
                if (cutSheetHanless==null)
                {
                    Thread.Sleep(10);
                    continue;
                }
                if (WaitHandle.WaitAny(cutSheetHanless) != 0)
                {
                    break;
                }

                Bitmap _b = _FrameData.Clone().ToBitmap(false);

                List<MvCodeInfo> _m = new List<MvCodeInfo>(mci);

                //开启保存截取面单功能
                if (COcrMatting != null)
                {

                    if (CSharpnessMeasure.Process(_b, "", mci, 0) < 45)
                    {
                        _DoneCutSheetHandle.Set();
                        _b.Dispose();
                        Thread.Sleep(10);

                        continue;
                    }

                    //If New Barcode Is Coming
                    if (Interlocked.CompareExchange(ref _bNewBarcode, 0, 1) == 1)
                    {

                        barCodePicLocation = string.Format(".\\pic\\" + _cutSheetBarcode + "_" +
                            DateTime.Now.ToString("yyyyMMddHHmmss") + "_" +
                            DateTime.Now.Millisecond.ToString("D3") + ".jpg");

                        if (_cutSheetBitmap != null)
                        {
                            MvBarCode.MvBarCodeGlobalVar.Log.InfoFormat("Save Last Barcode:{0}", _cutSheetBarcode);
                            _cutSheetBitmap.Save(barCodePicLocation, ImageFormat.Jpeg);
                            _cutSheetBitmap.Dispose();

                            if (PicturePathHandle != null)
                            {
                                PicturePathHandle(this, new PicturePathEventArgs(new PicturePathDescribe(barCodePicLocation)));
                            }
                        }
                        MvBarCode.MvBarCodeGlobalVar.Log.InfoFormat("Get Newer Barcode:{0}", new string(_m[0].Code, 0, _m[0].CodeLen));
                        COcrMatting.SetConfig();

                        MvBarCode.MvBarCodeGlobalVar.Log.InfoFormat("Process Barcode:{0}", new string(_m[0].Code, 0, _m[0].CodeLen));
                        COcrMatting.MattingProcess(_b, _m);
                        _cutSheetBitmap = COcrMatting.GetBitmap();
                        _cutSheetBarcode = new string(_m[0].Code, 0, _m[0].CodeLen);
                    }
                    
                }
                //开启保存保存原图功能
                else if (MvBarCodeGlobalVar.IsSavePic)
                {
                    if (CSharpnessMeasure.Process(_b, "", mci, 0) < 45)
                    {
                        _DoneCutSheetHandle.Set();
                        _b.Dispose();
                        Thread.Sleep(10);

                        continue;
                    }

                    if (Interlocked.CompareExchange(ref _bNewBarcode, 0, 1) == 1)
                    {
                        barCodePicLocation = string.Format(".\\pic\\" + new string(_m[0].Code, 0, _m[0].CodeLen) + "_" +
                            DateTime.Now.ToString("yyyyMMddHHmmss") + "_" +
                            DateTime.Now.Millisecond.ToString("D3") + ".jpg");

                        _b.Save(barCodePicLocation, ImageFormat.Jpeg);

                        if (PicturePathHandle != null)
                        {
                            PicturePathHandle(this, new PicturePathEventArgs(new PicturePathDescribe(barCodePicLocation)));
                        }
                    }
                }

                _DoneCutSheetHandle.Set();
                _b.Dispose();
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// 检查是否正在捕获数据
        /// </summary>
        public bool IsStart
        {
            get { return (Interlocked.Read(ref _bCollection) == 1); }
        }

        /// <summary>
        /// 关闭码流
        /// </summary>
        public void StopGrab()
        {

            //关闭线程
            if (_BarCodeThread != null)
            {
                _bAppStatus = false;
                _ExitHandle.Set();

                Console.WriteLine("_BarCodeThread Stop Flag Set");

                _BarCodeThread.Join();
                _CutSheetThread.Join();

                Console.WriteLine("_BarCodeThread Ended");
            }


            if (!MvBarCode.MvBarCodeGlobalVar.LocalImageMode)
            {
              

                //停止码流
                new TaskFactory().StartNew(() =>
                {
                    if (_Camera == null)
                    {
                        throw new InvalidOperationException("Camera is not opened.");
                    }

                // 关闭码流通道
                if (_Camera.IsGrabbing)
                    {
                        _Camera.ShutdownGrab();
                    }
                    Interlocked.CompareExchange(ref _bCollection, 0, 1);
                }).Wait();

                MvBarCode.MvBarCodeGlobalVar.Log.Info("关闭相机码流成功");
            }
            else
            {
                //关闭本地线程获取_FrameData帧数据
                //。。。
                MvBarCode.MvBarCodeGlobalVar.Log.Info("关闭读取本地码流线程");
                _bAppStatus = false;
            }
        }

        /// <summary>
        /// Io输出互斥变量
        /// </summary>
        private long _bIoOutFlag = 0;

        /// <summary>
        /// 输出相机开关量
        /// </summary>
        /// <param name="millSeconds">高电平持续毫秒数</param>
        /// <param name="num">单号</param>
        public void IoOutput(int millSeconds,string num)
        {
            // 保证每次只允许一个信号输出
            if (Interlocked.CompareExchange(ref _bIoOutFlag, 1, 0) == 0)
            {
                // 异步输出脉冲信号
                new TaskFactory().StartNew(() =>
                {
                try
                {
                    if (_Camera != null)
                    {
                        //选择输出信号
                        using (IEnumParameter ep = _Camera.ParameterCollection[ParametrizeNameSet.IOLineSelector])
                        {
                            ep.SetValue("Line0");
                        }
                        
                        // 选择用户输出
                        using (IEnumParameter ep = _Camera.ParameterCollection[ParametrizeNameSet.IOUserOutputSelector])
                        {
                            ep.SetValue("UserOutPut0");
                        }
                        
                        // 输出信号脉冲
                        using (IBooleanParameter bp = _Camera.ParameterCollection[ParametrizeNameSet.IOUserOutputValue])
                        {
                            bp.SetValue(false);
                        }
                        
                        Thread.Sleep(millSeconds);

                        // 停止输出脉冲
                        using (IBooleanParameter bp = _Camera.ParameterCollection[ParametrizeNameSet.IOUserOutputValue])
                        {
                            bp.SetValue(true);
                        }
                        
                        // 允许输出
                        Interlocked.CompareExchange(ref _bIoOutFlag, 0, 1);
                    }
                }
                catch (Exception e)
                {
                    MvBarCodeGlobalVar.Log.Error(e);
                }
                });
            }

        }

        /// <summary>
        /// 设备序列号校验
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool CheckDeviceKey(string key)
        {
            if(key.Length < 20)
            {
                return false;
            }

            // 校验厂商
            string strMagic = key.Substring(0, 5);
            if (0 != string.Compare(strMagic, "Dahua", true))
            {
                //return false;
            }

            // 校验序列号
            string serialNo = key.Substring(key.Length - 15);
            if (serialNo[1] < 'A' || serialNo[1] > 'L')
            {
                //return false;
            }

            if (serialNo[8] != 'A' || serialNo[9] != 'K')
            {
                //return false;
            }

            for (int i = 10; i < 15; ++i)
            {
                if (serialNo[i] < '0' || serialNo[i] > '9')
                {
                    //return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 打开相机
        /// </summary>
        /// <param name="isWait">同步异步标识</param>
        public void Open(bool isWait)
        {
            if (!MvBarCode.MvBarCodeGlobalVar.LocalImageMode)
            {
                var task = new TaskFactory().StartNew(() =>
                {
                // 获取相机的对象
                if (_Camera == null)
                    {
                        InitCamera();
                    }

                    if (_Camera != null)
                    {
                    // 相机序列号检查
                    if (!CheckDeviceKey(_Camera.DeviceKey))
                        {
                            _Camera.Dispose();
                            _Camera = null;
                            return;
                        }

                    // 相机已经打开直接返回
                    if (_Camera.IsOpen)
                        {
                            return;
                        }

                    // 打开相机并初始化配参
                    if (_Camera.Open())
                        {
                        // 注册流事件函数
                        _Camera.StreamGrabber.ImageGrabbed += OnImageGrabbing;

                        // 初始化参数
                        SetParameters();
                        }
                    }
                });

                if (isWait)
                {
                    task.Wait();
                }
            }
            else
            {
                localCameraIsOpen = true;
            }
        }

        private bool localCameraIsOpen = false;

        /// <summary>
        /// 相机是否打开
        /// </summary>
        public bool IsOpen
        {
            get {
                if (MvBarCode.MvBarCodeGlobalVar.LocalImageMode)
                    return localCameraIsOpen;
                else
                    return _Camera != null ? _Camera.IsOpen : false;
            }
        }

        /// <summary>
        /// 关闭相机
        /// </summary>
        public void Close()
        {
            if (MvBarCode.MvBarCodeGlobalVar.LocalImageMode)
            {
                localCameraIsOpen = false;
                return;
            }
            new TaskFactory().StartNew(() =>
            {
                if (_Camera == null)
                {
                    throw new InvalidOperationException("Camera is not opened.");
                }

                if (_Camera.IsOpen)
                {
                    _Camera.Close();
                    Console.WriteLine("Close camera;");
                }

            }).Wait();
            MvBarCode.MvBarCodeGlobalVar.Log.Info("关闭相机成功");
        }

        /// <summary>
        /// 码流捕获函数
        /// </summary>
        /// <param name="sender">事件投递方</param>
        /// <param name="e">码流事件参数</param>
        private void OnImageGrabbing(object sender, IGrabbedEventArg e)
        {
            // 停止采集
            if (Interlocked.Read(ref _bCollection) == 0)
            {
                return;
            }

            // 更新样本数据，通知算法线程
            if(Interlocked.CompareExchange(ref _bAlgorithmRunning, 1, 0) == 0)
            {
                _FrameData = null;
                _FrameData = e.GrabResult;
                _DetectBarCodeEvent.Set();
            }
        }




        /// <summary>
        /// 设置相机配参
        /// </summary>
        private void SetParameters()
        {
            if (_Camera == null)
            {
                return;
            }

            // 关闭触发模式
            _Camera.TriggerSet.Close();

            // 设置帧格式
            using (IEnumParameter ep = _Camera.ParameterCollection[ParametrizeNameSet.ImagePixelFormat])
            {
                ep.SetValue("Mono8");
            }

            // 获取图像宽度
            using (IIntegraParameter ip = _Camera.ParameterCollection[ParametrizeNameSet.ImageWidth])
            {
                this.MaxWidth = Convert.ToInt32(ip.GetMaximum());
                this.Width = Convert.ToInt32(ip.GetValue());
            }

            // 获取图像高度
            using (IIntegraParameter ip = _Camera.ParameterCollection[ParametrizeNameSet.ImageHeight])
            {
                this.MaxHeight = Convert.ToInt32(ip.GetMaximum());
                this.Height = Convert.ToInt32(ip.GetValue());
            }
        }

        /// <summary>
        /// 最大分辨率宽
        /// </summary>
        public Int32 MaxWidth
        {
            get { return _MaxImageWidth; }
            set { _MaxImageWidth = value; }
        }

        /// <summary>
        /// 最大分辨率高
        /// </summary>
        public Int32 MaxHeight
        {
            get { return _MaxImageHeight; }
            set { _MaxImageHeight = value; }
        }

        /// <summary>
        /// 图像宽
        /// </summary>
        public Int32 Width
        {
            get { return _ImageWidth; }
            set 
            {
                if (value < 0 || value > this.MaxWidth)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (_ImageWidth == value)
                {
                    return;
                }

                using (IIntegraParameter ip = _Camera.ParameterCollection[ParametrizeNameSet.ImageWidth])
                {
                    if (ip.SetValue(Convert.ToInt64(value)))
                    {
                        _ImageWidth = value;
                    }
                }
            }
        }

        /// <summary>
        /// 图像高
        /// </summary>
        public Int32 Height
        {
            get { return _ImageHeight; }
            set
            {
                if (value < 0 || value > this.MaxHeight)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (_ImageHeight == value)
                {
                    return;
                }

                using (IIntegraParameter ip = _Camera.ParameterCollection[ParametrizeNameSet.ImageHeight])
                {
                    if (ip.SetValue(Convert.ToInt64(value)))
                    {
                        _ImageHeight = value;
                    }
                }
            }
        }

        /// <summary>
        /// 资源释放接口
        /// </summary>
        public void Dispose()
        {
            // 关闭相机
            if (_Camera != null)
            {
                //Close();
                _Camera.Dispose();
                _Camera = null;
            }

            if (COcrMatting != null)
            {
                COcrMatting.Dispose();
            }


            if (CSharpnessMeasure != null)
            {
                CSharpnessMeasure.Dispose();
            }

        }
    }
}

