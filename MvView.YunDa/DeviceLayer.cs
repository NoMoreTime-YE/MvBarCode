using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvView.Scale;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MvView.YunDa
{
    /// <summary>
    /// 设备层对象类
    /// </summary>
    internal class DeviceLayer : IDisposable
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

        // 扫码相机
        private BarCodeCamera _BarCodeCamera;

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

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// 相机通讯Mac地址
        /// </summary>
        public string MacAddress
        {
            get
            {
                if(_BarCodeCamera == null)
                {
                    throw new InvalidOperationException();
                }

                return _BarCodeCamera.InterfaceMacAddress;
            }
        }

        /// <summary>
        /// 设备序列号
        /// </summary>
        public string DeviceID
        {
            get
            {
                if(_BarCodeCamera == null)
                {
                    throw new InvalidOperationException();
                }

                return _BarCodeCamera.DeviceID;
            }
        }

        /// <summary>
        /// 一维码事件回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnBarCode(object sender, BarCodeEventArgs e)
        {
            // 非有效条码信息，回调图像数据
            if (e.Result.Code == string.Empty)
            {
                if (BarCodeHandle != null)
                {
                    BarCodeHandle(this, e);
                }
                return;
            }

            // 记录绑定标识
            Interlocked.Exchange(ref _bBinding, 1);
            // 记录一维码信息
            _BarCodeInfo = e.Result;

            // 检测到一维码后开始获取称数据
            if (_Scale != null)
            {
                _Scale.Start();
            }

            // 条码事件，直接回调
            if (BarCodeHandle != null)
            {
                BarCodeHandle(this, e);
            }
        }

        /// <summary>
        /// 称重事件回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnWight(object sender, WeightEventArgs e)
        {
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
                /*
                if (_BarCodeCamera != null)
                {
                    // 相机Io输出
                    _BarCodeCamera.IoOutput(100);
                }
                */
                if (_BarCodeInfo != null)
                {
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
        /// <param name="ip">条码检测相机IP地址</param>
        /// <param name="protocol">重量采集协议</param>
        public bool Initialization(string ip, string protocol)
        {
            // 初始化相机
            if (_BarCodeCamera == null)
            {
                _BarCodeCamera = new BarCodeCamera(ip);
            }

            // 初始化电子秤
            if (_Scale == null)
            {
                _Scale = ScaleBase.GetScale(protocol);
                if (_Scale == null)
                {
                    throw new ArgumentException("protocol");
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

            return (_BarCodeCamera != null) && (_Scale != null);
        }

        /// <summary>
        /// 是否打开
        /// </summary>
        public bool IsOpen
        {
            get { return _bOpen; }
        }

        /// <summary>
        /// 启动系统相关模块
        /// </summary>
        /// <param name="ip">扫码相机ip地址</param>
        /// <param name="protocol">电子秤协议</param>
        /// <returns>操作结果</returns>
        public bool Open()
        {
            if(_BarCodeCamera == null || _Scale == null)
            {
                throw new InvalidOperationException();
            }

            // 设备已经打开，直接返回
            if (_bOpen)
            {
                return true;
            }

            // 打开电子秤
            if (!_Scale.IsOpen)
            {
                _Scale.Open(string.Empty); 
            }

            if (!_BarCodeCamera.IsOpen)
            {
                _BarCodeCamera.Open(true);
            }

            // 相机和电子秤都开启成功
            if (_BarCodeCamera.IsOpen && _Scale.IsOpen)
            {               
                // 注册重量回调
                _Scale.ScaleWight += OnWight;

                // 注册条码回调
                _BarCodeCamera.BarCodeHandle += OnBarCode;

                _bOpen = true;
            }
            return (_bOpen == true);
        }

        /// <summary>
        /// 关闭设备层对象
        /// </summary>
        /// <returns>操作结果</returns>
        public bool Close()
        {
            // 未打开的设备直接返回false
            if (!_bOpen)
            {
                return true;
            }

            // 关闭相机
            if (_BarCodeCamera != null)
            {
                if (_BarCodeCamera.IsOpen)
                {
                    _BarCodeCamera.Close();
                }
            }

            // 关闭电子秤
            if (_Scale != null)
            {
                if (_Scale.IsOpen)
                {
                    _Scale.Close();
                }
            }

            // 电子秤和相机都完全关闭
            if (!_BarCodeCamera.IsOpen && !_Scale.IsOpen)
            {
                // 注销重量回调
                _Scale.ScaleWight -= OnWight;

                // 注销条码回调
                _BarCodeCamera.BarCodeHandle -= OnBarCode;

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
        /// 开始捕获
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (_BarCodeCamera == null)
            {
                throw new InvalidOperationException();
            }

            // 已经打开直接返回
            if (!_bStart)
            { 
                // 相机已经打开，开启码流
                if (_BarCodeCamera.IsOpen)
                {
                    _BarCodeCamera.StartGrab();
                    _bStart = true;
                }
            }

            return (_bStart == true);
        }

        public bool Stop()
        {
            if (_BarCodeCamera == null)
            {
                throw new InvalidOperationException();
            }

            // 打开码流
            if (_bStart)
            {
                // 关闭码流
                _BarCodeCamera.StopGrab();
                _bStart = false;
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
                _BarCodeCamera.Dispose();
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
