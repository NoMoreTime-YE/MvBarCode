using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MvView.Scale;

namespace MvView.YunDa
{
    /// <summary>
    /// 设备管理类
    /// </summary>
    public class DeviceManager : IDisposable
    {
        // 设备层对象
        private DeviceLayer _dl = new DeviceLayer();

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
        
        // 单件对象
        static DeviceManager _Instance = null;

        /// <summary>
        /// 获取设备管理单件
        /// </summary>
        /// <returns>设备管理对象</returns>
        public static DeviceManager Instance
        {
            get
            {
                // 初始化单件对象
                if (_Instance == null)
                {
                    _Instance = new DeviceManager();
                }
                return _Instance;
            }
        }

        // 初始化标识
        private bool _bInitialization = false;

        /// <summary>
        /// 初始化设备层
        /// </summary>
        /// <param name="ip">扫码设备IP</param>
        /// <param name="protocol">电子秤协议</param>
        /// <returns>初始化结果</returns>
        public bool Initialization(string ip, string protocol)
        {
            bool retVal = false;
            try
            {
                if (_dl == null)
                {
                    // 防止设备dispose后重新Initialization
                    _dl = new DeviceLayer();
                }

                if(!_bInitialization)
                {
                    // 初始化设备
                    if(retVal = _dl.Initialization(ip, protocol))
                    {
                        _bInitialization = true;
                        _dl.EmbraceHandle += this.EmbraceDelegate;
                        _dl.WeightHandle += this.WeightDelegate;
                        _dl.BarCodeHandle += this.BarCodeDelegate;
                    }
                }
                else
                {
                    retVal = true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Initialization device exception, " + e.Message);
            }
            return retVal;
        }

        /// <summary>
        /// 取消设备层初始化
        /// </summary>
        /// <returns>取消结果</returns>
        public bool UnInitialization()
        {
            // 判断是否已经初始化
            if(_bInitialization)
            {
                // 清理对外事件
                this.EmbraceHandle = null;
                this.BarCodeHandle = null;
                this.WeightHandle = null;

                if (_dl != null)
                {
                    // 清理内部事件
                    _dl.EmbraceHandle -= this.EmbraceDelegate;
                    _dl.WeightHandle -= this.WeightDelegate;
                    _dl.BarCodeHandle -= this.BarCodeDelegate;
                }
                _bInitialization = false;
            }
            return true;
        }

        /// <summary>
        /// 获取相机直连网卡Mac地址
        /// </summary>
        public string MacAddress
        {
            get
            {
                if (_dl == null)
                {
                    throw new InvalidOperationException();
                }

                string retVal = string.Empty;
                try
                {
                    retVal = _dl.MacAddress;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Get MacAddress exception, " + e.Message);
                }
                return retVal;
            }
        }

        /// <summary>
        /// 获取设备ID
        /// </summary>
        public string DeviceID
        {
            get
            {
                if (_dl == null)
                {
                    throw new InvalidOperationException();
                }

                string retVal = string.Empty;
                try
                {
                    retVal = _dl.DeviceID;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Get deviceId exception, " + e.Message);
                }
                return retVal;
            }
        }

        /// <summary>
        /// 设备层是否启动
        /// </summary>
        public bool IsStart
        {
            get { return _dl != null ? _dl.IsStart : false; }
        }

        /// <summary>
        /// 设备层是否启动
        /// </summary>
        /// <returns>操作结果</returns>
        public bool Start()
        {
            if (_dl == null)
            {
                throw new InvalidOperationException();
            }

            bool retVal = false;
            try
            {
                retVal = _dl.Start();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Start device exception, " + e.Message);
            }
            return retVal;
        }

        /// <summary>
        /// 关闭设备层
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (_dl == null)
            {
                throw new InvalidOperationException();
            }

            bool retVal = false;
            try
            {
                retVal = _dl.Stop();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Stop device exception, " + e.Message);
            }
            return retVal;
        }

        /// <summary>
        /// 设备层是否打开
        /// </summary>
        public bool IsOpen
        {
            get { return _dl != null ? _dl.IsOpen : false; }
        }

        /// <summary>
        /// 打开设备层
        /// </summary>
        /// <returns>操作状态</returns>
        public bool Open()
        {
            if (_dl == null)
            {
                throw new InvalidOperationException();
            }

            bool retVal = false;
            try
            {
                retVal = _dl.Open();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Open device exception, " + e.Message);
            }
            return retVal;
        }

        /// <summary>
        /// 关闭设备层
        /// </summary>
        /// <returns>操作状态</returns>
        public bool Close()
        {
            if (_dl == null)
            {
                throw new InvalidOperationException();
            }

            bool retVal = false;
            try
            {
                retVal = _dl.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Close device exception, " + e.Message);
            }
            return retVal;
        }

        /// <summary>
        /// 手动称重
        /// </summary>
        public void ManualWeighed()
        {
            if (_dl == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                _dl.ManualWeighed();
            }
            catch (Exception e)
            {
                Debug.WriteLine("ManualWeighed exception, " + e.Message);
            }
        }

        /// <summary>
        /// 尝试获取本地时间
        /// </summary>
        /// <returns>本地时间</returns>
        public DateTime TryGetLocalTime()
        {
            DateTime dt = new DateTime();
            try
            {
                dt = MvView.Time.SystemTime.GetSystemTime();
            }
            catch (Exception e)
            {
                Debug.WriteLine("GetlocalTime exception, " + e.Message);
            }
            return dt;
        }

        /// <summary>
        /// 设置系统时间
        /// </summary>
        /// <param name="dt">系统时间</param>
        public void TrySetLocalTime(DateTime dt)
        {
            try
            {
               MvView.Time.SystemTime.SetSystemTime(dt);
            }
            catch (Exception e)
            {
                Debug.WriteLine("SetLocalTime exception, " + e.Message);
            }
        }

        /// <summary>
        /// 设置系统时间
        /// </summary>
        /// <param name="strDate">系统时间字符串，yyyy-MM-dd HH:mm:ss</param>
        public void TrySetLocalTime(string strDate)
        {
            try
            {
                MvView.Time.SystemTime.SetSystemTime(strDate);
            }
            catch (Exception e)
            {
                Debug.WriteLine("SetLocalTime exception, " + e.Message);
            }
        }

        /// <summary>
        /// 条形码事件委托
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件内容</param>
        void BarCodeDelegate(object sender, BarCodeEventArgs e)
        {
            if (BarCodeHandle != null)
            {
                BarCodeHandle(sender, e);
            }
        }

        /// <summary>
        /// 电子秤称重委托
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件内容</param>
        void WeightDelegate(object sender, WeightEventArgs e)
        {
            if (WeightHandle != null)
            {
                WeightHandle(sender, e);
            }
        }

        /// <summary>
        /// 揽件委托
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件内容</param>
        void EmbraceDelegate(object sender, EmbraceEventArgs e)
        {
            if(EmbraceHandle != null)
            {
                EmbraceHandle(sender, e);
            }
        }

        /// <summary>
        /// 销毁设备层对象
        /// </summary>
        public void Dispose()
        {
            if (_dl != null)
            {
                this.Stop();
                this.Close();
                _dl.Dispose();
            }
        }
    }
}
