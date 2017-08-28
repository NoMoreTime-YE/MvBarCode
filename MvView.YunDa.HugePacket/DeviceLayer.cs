using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvView.Scale;
using System.Threading;
using System.Threading.Tasks;
using ThridLibray;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MvView.Core
{
    /// <summary>
    /// 设备层对象类
    /// </summary>
     public class DeviceLayer : IDisposable
    {
        /// <summary>
        /// 揽件数据相关事件
        /// </summary>
        public event EventHandler<EmbraceEventArgs> EmbraceHandle;

        /// <summary>
        /// 实时一维码事件
        /// </summary>
        public event EventHandler<BarCodeEventArgs> BarCodeHandle;

        /// <summary>
        /// 实时电子秤数据事件，电子秤稳定后数据不再更新
        /// </summary>
        public event EventHandler<WeightEventArgs> WeightHandle;

        /// <summary>
        /// 图片保存路径上报事件
        /// </summary>
        public event EventHandler<PicturePathEventArgs> PicturePathHandle;

        /// <summary>
        /// 相机实例数组
        /// </summary>
        public BarCodeCamera[] _BarCodeCamera = null;

        // 电子秤对象
        private IScale _Scale;

        // 相机打开标识
        private bool _bOpen = false;

        // 相机开始标识
        private bool _bStart = false;

        // 绑定标识
        private long _bBinding = 0;

        // 一维码信息
        private BarCodeDescribe _BarCodeInfo = null;

        // 条码缓冲
        private Scale.CircularQueue<string> _BarcodeCache = new CircularQueue<string>(MvBarCode.MvBarCodeGlobalVar.BarcodeCacheNum);

        //条码保留时间
        private Stopwatch BarcodeSustaining = new Stopwatch();

         [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);


        /// <summary>
        /// 相机通讯Mac地址
        /// </summary>
        public string MacAddress
        {
            get
            {
                if(_BarCodeCamera == null || _BarCodeCamera[0] == null)
                {
                    throw new InvalidOperationException();
                }

                return _BarCodeCamera[0].InterfaceMacAddress;
            }
        }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string DeviceID
        {
            get
            {
                if(_BarCodeCamera == null || _BarCodeCamera[0] == null)
                {
                    throw new InvalidOperationException();
                }

                return _BarCodeCamera[0].DeviceID;
            }
        }

        /// <summary>
        /// 检查相机是否为Null
        /// </summary>
        /// <returns>返回值校验</returns>
        private bool IsCameraValid()
        {
            if(_BarCodeCamera == null)
            {
                return false;
            }

            foreach (var c in _BarCodeCamera)
            {
                if (c == null)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 条码通知标识
        /// </summary>
        private long _bBarCodeNotifyFlag = 0;

        void OnPicturePath(object sender, PicturePathEventArgs e)
        {
            if(PicturePathHandle!=null)
            {
                PicturePathHandle(this, e);
            }
        }

        /// <summary>
        /// 一维码事件回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnBarCode(object sender, BarCodeEventArgs e)
        {
            // 1.非有效条码信息，回调图像数据
            // 2.条码数量大于1
            if (e.Result.Code == string.Empty)
            {
                if (BarCodeHandle != null)
                {
                    BarCodeHandle(this, e);
                }
                return;
            }

            if (Interlocked.Read(ref _bBarCodeNotifyFlag) == 1)
            {
                return;
            }

            // 判断是否为重复条码数据
            if (_BarcodeCache.IsIn(e.Result.Code))
            {
                BarcodeSustaining.Stop();
                long t = BarcodeSustaining.ElapsedMilliseconds;


                if (t <= MvBarCode.MvBarCodeGlobalVar.BarcodeValidTime)
                {
                    BarcodeSustaining.Start();
                    return;
                }

            }

            BarcodeSustaining.Reset();
            BarcodeSustaining.Start();

            // 条码事件，直接回调
            if (BarCodeHandle != null)
            {
                //MvBarCode.MvBarCodeGlobalVar.Log.InfoFormat("DeviceLayer读取到条码：{0}",e.Result.Code);
                //读条形码
                BarCodeHandle(this, e);
            }

            // 记录条码数据信息
            if (Interlocked.CompareExchange(ref _bBarCodeNotifyFlag, 1, 0) == 0)
            {
                // 添加条码到缓冲中
                _BarcodeCache.Push(e.Result.Code);

                // 记录绑定标识
                Interlocked.Exchange(ref _bBinding, 1);

                // 记录一维码信息
                _BarCodeInfo = e.Result;
                
                // 检测到一维码后开始获取称数据
                if (_Scale != null)
                {
                    _Scale.AsyncPost(e.Result.Code);

                    // 启动重量检测超时定时器
                }
                else
                {
                    Interlocked.CompareExchange(ref _bBarCodeNotifyFlag, 0, 1);
                }
            }
        }

        /// <summary>
        /// 称重事件回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnWight(object sender, WeightEventArgs e)
        {
            if ( e._bNeedOutPut )
            {
                //称的触发 移动 到客户端 否则由底层控制不合适
                //_BarCodeCamera[0].IoOutput( 800 );
                //_BarCodeCamera[1].IoOutput(20);
                //_BarCodeCamera[0].IoOutput(20);
            }
            
            if (Interlocked.Read(ref _bBinding) == 1)
            {
                if (WeightHandle != null)
                {
                    WeightHandle(this, e);
                }
            }

            if (e.RealWeight)
            {
                // 实时数据绑定 
                Interlocked.CompareExchange(ref _bBinding, 0, 1);

                //条码通知
                Interlocked.CompareExchange(ref _bBarCodeNotifyFlag, 0, 1);
                
                if (_BarCodeInfo != null)
                {
                    /*
                    if (_BarCodeCamera != null)
                    {
                        // 相机Io输出
                        _BarCodeCamera.IoOutput(100);
                    }
                    */
                    // 绑定数据信息
                    if (EmbraceHandle != null)
                    {
                        EmbraceHandle(this, new EmbraceEventArgs(_BarCodeInfo, e.Weight));
                    }

                    // 清理一维码数据
                    _BarCodeInfo = null;
                }
                // 兼容手动获取重量
                else
                {
                    if (WeightHandle != null)
                    {
                        WeightHandle(sender, e);
                    }
                }
            }
        }

        /// <summary>
        /// 初始化设备层
        /// </summary>
        /// <param name="num">初始化相机数量</param>
        /// <param name="protocol">重量采集协议</param>
        public bool Initialization(int num, string protocol)
        {
            MvBarCode.MvBarCodeGlobalVar.Log.Info("抵达设备层初始化");

            //不是离线模式才去发现设备
            if (!MvBarCode.MvBarCodeGlobalVar.LocalImageMode)
            {
                // 搜索设备
                List<IDeviceInfo> li = Enumerator.EnumerateDevices();
                if (li.Count < num)
                {
                    MvBarCode.MvBarCodeGlobalVar.Log.ErrorFormat("初始化失败，实际发现设备:{0}，计划发现设备:{1}", li.Count.ToString(), num.ToString());
                    return false;
                }
            }
            else
            {
                MvBarCode.MvBarCodeGlobalVar.Log.Info("离线模式：初始化设备");
            }

            // 初始化相机
            if (_BarCodeCamera == null)
            {
                // 按照索引号初始化相机
                _BarCodeCamera = new BarCodeCamera[num];
            }

            // 初始化相机组中的相机对象
            for (var i = 0; i < num; ++i)
            {
                if (_BarCodeCamera[i] == null)
                {
                    _BarCodeCamera[i] = new BarCodeCamera(i);
                }
            }

            // 初始化电子秤
            if (_Scale == null)
            {
                _Scale = ScaleBase.GetScale(protocol);
                if (_Scale == null)
                {
                    MvBarCode.MvBarCodeGlobalVar.Log.ErrorFormat("scale为null，初始化失败");
                    return false;
                }

                // 控制采样数量
                _Scale.SampleNum = 4;

                // 控制采样精度
                _Scale.MaxDeviation = 0.03;
				
				  StringBuilder builder = new StringBuilder(128);
                GetPrivateProfileString("WeightParam", "SampleNum", "4", builder, 128, Environment.CurrentDirectory + "\\config.ini");
                int result;
                int.TryParse(builder.ToString(), out result);
                if (result > 4)
                {
                    _Scale.SampleNum = result;
                }

                GetPrivateProfileString("WeightParam", "MaxDeviation", "0.03", builder, 128, Environment.CurrentDirectory + "\\config.ini");
                double result2;
                double.TryParse(builder.ToString(), out result2);
                if (result2 > 0)
                {
                    _Scale.MaxDeviation = result2;
                }
            }

            return true;
        }

        /// <summary>
        /// 是否打开
        /// </summary>
        public bool IsOpen
        {
            get { return _bOpen; }
        }

        /// <summary>
        /// 打开所有相机
        /// </summary>
        /// <param name="isWait">是否同步打开</param>
        /// <returns>打开结果</returns>
        private bool OpenAllCamera(bool isWait)
        {

            if (!IsCameraValid())
            {
                MvBarCode.MvBarCodeGlobalVar.Log.ErrorFormat("!IsCameraValid()");
                return false;
            }

            foreach (var c in _BarCodeCamera)
            {
                if (!c.IsOpen)
                {
                    c.Open(isWait);
                }
            }

            foreach(var c in _BarCodeCamera)
            {
                if(!c.IsOpen)
                {
                    MvBarCode.MvBarCodeGlobalVar.Log.ErrorFormat("!c.IsOpen");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 关闭所有相机
        /// </summary>
        /// <returns>操作结果</returns>
        private bool CloseAllCamera()
        {
            if(!IsCameraValid())
            {
                return false;
            }

            foreach(var c in _BarCodeCamera)
            {
                if(c.IsOpen)
                {
                    c.Close();
                }
            }

            foreach(var c in _BarCodeCamera)
            {
                if(c.IsOpen)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 打开设备（注册相机事件，启动电子秤）
        /// </summary>
        /// <returns>操作结果</returns>
        public bool Open()
        {
            // 校验操作对象的
            if(!IsCameraValid() || _Scale == null)
            {
                // throw new InvalidOperationException();
                MvBarCode.MvBarCodeGlobalVar.Log.ErrorFormat("!IsCameraValid() || _Scale == null");
                return false;
            }

            // 设备已经打开，直接返回
            if (_bOpen)
            {
                return true;
            }

            // 打开电子秤
            if (!_Scale.IsOpen)
            {
                // 打开电子秤
                if (!_Scale.Open(""))
                {
                    MvBarCode.MvBarCodeGlobalVar.Log.ErrorFormat("!_Scale.Open()");
                    return false;
                }
                _Scale.ScaleWight += OnWight;
            }

            // 打开所有相机对象
            if(!OpenAllCamera(true))
            {
                MvBarCode.MvBarCodeGlobalVar.Log.ErrorFormat("!OpenAllCamera(true)");
                return false;
            }

            // 注册条码回调
            foreach (var c in _BarCodeCamera)
            {
                c.BarCodeHandle += OnBarCode;
                c.PicturePathHandle += OnPicturePath;
            }
            // 修改打开标识
            _bOpen = true;
            return (_bOpen == true);
        }

        /// <summary>
        /// 关闭设备层对象
        /// </summary>
        /// <returns>操作结果</returns>
        public bool Close()
        {
            // 检查相机是否有效
            if (!IsCameraValid() || _Scale == null)
            {
                return false;
            }

            // 未打开的设备直接返回false
            if (!_bOpen)
            {
                return true;
            }

            // 关闭相机
            if (!CloseAllCamera())
            {
                return false;
            }

            // 注销事件回调
            foreach(var c in _BarCodeCamera)
            {
                if(c != null)
                {
                    c.BarCodeHandle -= OnBarCode;
                    c.PicturePathHandle -= OnPicturePath; 
                }
            }

            // 关闭电子秤
            if (_Scale.IsOpen)
            {
                _Scale.Close();
            }

            // 电子秤和相机都完全关闭
            if (!_Scale.IsOpen)
            {
                // 注销重量回调
                _Scale.ScaleWight -= OnWight;

                // 更新打开标识
                _bOpen = false;
            }
            return (_bOpen == false);
        }

        /// <summary>
        /// 设备是否开始
        /// </summary>
        public bool IsStart
        {
            get { return _bStart; }
        }

        /// <summary>
        /// 开始捕获码流
        /// </summary>
        /// <returns>操作结果</returns>
        private bool StartAllCamera()
        {
            // 检查相机是否有效
            if (!IsCameraValid())
            {
                MvBarCode.MvBarCodeGlobalVar.Log.ErrorFormat("未将部分相机引用到实例");
                return false;
            }

            // 打开相机码流
            foreach (var c in _BarCodeCamera)
            {
                if (!c.IsStart)
                {
                    c.StartGrab();
                }
            }
            return true;
        }

        private bool StopAllCamera()
        {
            // 检查相机是否有效
            if (!IsCameraValid())
            {
                return false;
            }

            // 打开相机码流
            foreach (var c in _BarCodeCamera)
            {
                if (c.IsStart)
                {
                    c.StopGrab();
                }
            }
            return true;
        }

        /// <summary>
        /// 开启相机码流
        /// </summary>
        /// <returns>操作结果</returns>
        public bool Start()
        {
            // 已经打开直接返回
            if (!_bStart)
            { 
                // 相机已经打开，开启码流
                if (StartAllCamera())
                {
                    _bStart = true;
                }
            }

            return (_bStart == true);
        }
        /// <summary>
        /// 关闭相机码流
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            // 打开码流
            if (_bStart)
            {
                // 关闭码流
                if (StopAllCamera())
                {
                    _bStart = false;
                }
            }

            return (_bStart == false);
        }

        /// <summary>
        /// 手动称重
        /// </summary>
        public void ManualWeighed()
        {
            if (_Scale != null)
            {
                _Scale.Start();
            }
        }

        /// <summary>
        /// 资源清理
        /// </summary>
        public void Dispose()
        {
            if (_BarCodeCamera != null)
            {
                foreach (var c in _BarCodeCamera)
                {
                    if (c != null)
                    {
                        c.Dispose();
                    }
                }
                _BarCodeCamera = null;
            }
            if (_Scale != null)
            {
                _Scale.Dispose();
                _Scale = null;
            }
        }
    }
}
