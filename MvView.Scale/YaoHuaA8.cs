using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace MvView.Scale
{
    /// <summary>
    /// 兼容XK3190-A7，常用于中通，申通等快递，波特率选9600
    /// </summary>
    internal class YaoHuaA8 : IScale
    {  
        // 串口对象
        private SerialPort _Serial;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);

        string comName = "COM1";

        public YaoHuaA8()
        {
            StringBuilder builder = new StringBuilder(128);
            GetPrivateProfileString("WeightParam", "COMName", "COM1", builder, 128, Environment.CurrentDirectory + "\\config.ini");
            comName = builder.ToString();
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        private void ProbeDev()
        {
            // 获取所有串口名称
            var portList = SerialPort.GetPortNames();

            foreach (string port in portList)
            {  
                if(port != comName)
                {
                    continue;
                }

                // 初始化串口
                SerialPort ser = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);
                try
                {
                    // 设置串口超时时间为100ms
                    //ser.ReadTimeout = 1000;
                    ser.ReadTimeout = Timeout.Infinite;
                    ser.Open();

                    // 打开串口读通道
                    ser.RtsEnable = true;
                    if(ser.IsOpen)
                    {
                        // 若不是电子秤对应的串口对象会抛出超时异常
                        //ser.ReadTo("=");

                        // 若不抛出异常，记录串口对象
                        _Serial = ser;
                        

                        break;
                    }
                }
                catch(Exception e)
                {

              
                    // 串口读异常时关闭串口对象
                    if (ser.IsOpen)
                    {
                        ser.Close();
                    }

                }
            }
        }

        // 串口线程loop标识
        private bool _bSerialLooping = false;

        // 采样标识
        private long _SampleFlag = 0;

        // 每组的采样个数
        private static int _nSampleCountPerGroup = 1;

        // 称重缓冲
        private double[] _SampleArray;

        // 采样缓冲样本
        private object _LockSampleArrObj = new object();

        // 实时称重样本标识
        private long _RealWightFlag = 0;

        // 实时采样标识
        private long _SampleWeightFlag = 0;

        // 实时称重信息
        private double _RealWight = 0.0;

        // 串口采样线程
        private Thread _SerialThread;

        /// <summary>
        /// 解析串口数据,解析时去除第一个字符，去除第二个字符
        /// </summary>
        /// <param name="buffer">串口数据</param>
        /// <param name="len">串口长度</param>
        /// <returns></returns>
        private double[] ParseStreamData(char[] buffer, int len)
        {
            string strBufferTmp = new string(buffer, 0, len);
            string[] strBufferArr = strBufferTmp.Split('=');
            if(strBufferArr.Length <= 2)
            {
                return null;
            }
            double[] retVal = new double[strBufferArr.Length - 2];
            for (int i = 1; i < strBufferArr.Length - 1; ++i)
            {
                char[] tmp = strBufferArr[i].ToCharArray();
                Array.Reverse(tmp);
                retVal[i - 1] = Convert.ToDouble(new string(tmp));
            }
            return retVal;
        }

        /// <summary>
        /// 解析一个协议帧
        /// </summary>
        /// <param name="str">一帧数据</param>
        /// <returns>解析结果</returns>
        private double ParseStrData(string str)
        {
            if(string.IsNullOrEmpty(str) == true)
            {
                return 0.0;
            }
            char[] tmp = str.ToCharArray();
            Array.Reverse(tmp);
            return Convert.ToDouble(new string(tmp));
        }

        /// <summary>
        /// 从串口获取重量信息
        /// </summary>
        /// <returns></returns>
        private void ReadFromSerial()
        {
            if (_Serial == null)
            {
                throw new InvalidOperationException();
            }

            if (!_Serial.IsOpen)
            {
                return;
            }

            try
            {
                // 实时采集串口数据
                while (_bSerialLooping)
                {
                    // 读到第一个等号
                    //_Serial.ReadTo("=");

                    // 线程通知标识
                    bool bNotify = false;
                    try
                    {
                        

                        //double weight = 1.1;
                        _Serial.ReadTo("=");
                        double weight = ParseStrData(_Serial.ReadExisting());
                        //string str =_Serial.ReadLine();

                        // 保护flag
                        lock(_LockFlagObj)
                        { 
                            // 刷新缓冲
                            _WightCache.Push(weight);

                            // 周期称重采样
                            if (Interlocked.Read(ref _SampleFlag) == 1)
                            {
                                if (_WightCache.Size >= _nSampleCountPerGroup)
                                {
                                    if (Interlocked.CompareExchange(ref _SampleWeightFlag, 1, 0) == 0)
                                    {
                                        _SampleArray = _WightCache.Pop(_nSampleCountPerGroup);
                                        bNotify = true;
                                    }
                                }
                            }
                        }

                        // 实时称重采样
                        if (Interlocked.Read(ref _RealWightFlag) == 1)
                        {
                            Interlocked.Exchange(ref _RealWight, weight);
                            bNotify = true;
                        }   
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Serial thread exception, " + e.Message);
                    }

                    if (bNotify)
                    {
                        _WightWaitHandle.Set();
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Read from serial exception, " + e.Message);
            }
        }

        /// <summary>
        /// 异步事件
        /// </summary>
        public event EventHandler<WeightEventArgs> ScaleWight;

        private bool _bOpen = false;

        /// <summary>
        /// 打开串口设备
        /// </summary>
        /// <param name="info">打开信息</param>
        /// <returns>操作结果</returns>
        public virtual bool Open(string info)
        {
            if (_Serial == null)
            {
                // 初始化串口设备
                ProbeDev();

                // 开启串口数据捕获线程
                if (_Serial != null)
                {
                    // 串口监听线程
                    if (_SerialThread == null)
                    {
                        _SerialThread = new Thread(ReadFromSerial);
                        _bSerialLooping = true;
                        _SerialThread.Start();
                    }

                    // 样本分析线程
                    if (_WightCatchThread == null)
                    {
                        _WightCatchThread = new Thread(GetWight);
                        _bLooping = true;
                        _WightCatchThread.Start();
                    }

                    // 初始化样本缓冲区
                    if (_WightCache == null)
                    {
                        _WightCache = new CircularQueue<double>(_nSampleCountPerGroup);
                    }

                    // 样本对象
                    if (_SampleArray == null)
                    {
                        _SampleArray = new double[_nSampleCountPerGroup];
                    }
                    // 默认开启取样标识
                    Interlocked.Exchange(ref _SampleFlag, 0);
                    Interlocked.Exchange(ref _SampleWeightFlag, 0);
                    _bOpen = true;
                }
            }
            return (this._Serial != null);
        }

        public virtual bool IsOpen
        {
            get { return _bOpen; }
        }

        /// <summary>
        /// 关闭串口通道
        /// </summary>
        /// <returns>操作结果</returns>
        public virtual bool Close()
        {
            try
            {
                // 停止缓冲数据
                Interlocked.CompareExchange(ref _SampleFlag, 0, 1);

                // 关闭线程
                if (_WightCatchThread != null)
                {
                    _bLooping = false;
                    _bSerialLooping = false;
                    _ExitHandle.Set();
                    _WightCatchThread.Join();
                    _WightCatchThread = null;
                    _SerialThread.Join();
                    _SerialThread = null;
                }

                // 关闭串口
                if (_Serial != null)
                {
                    if (_Serial.IsOpen)
                    {
                        _Serial.Close();
                        _bOpen = false;
                    }
                    _Serial = null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Close serial exception, " + e.Message);
            }
            return true;
        }

        /// <summary>
        /// Post数据
        /// </summary>
        /// <param name="barCode">一维码</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="weight">重量信息</param>
        /// <returns></returns>
        public virtual bool Post(string barCode, Int32 timeout, ref Double weight)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 异步Post数据
        /// </summary>
        /// <param name="barCode">一维码信息</param>
        /// <returns></returns>
        public virtual bool AsyncPost(string barCode)
        {
            throw new NotImplementedException();
        }

        private Double _MaxDeviation = 0.003;

        public virtual Double MaxDeviation
        {
            private get { return _MaxDeviation; }
            set { _MaxDeviation = value; }
        }

        /// <summary>
        /// 校验w和v是否在有效误差范围之内
        /// </summary>
        /// <param name="w">被校验值</param>
        /// <param name="v">平均值</param>
        /// <returns></returns>
        private bool WightCheck(Double w, Double v)
        {
            return (w + _MaxDeviation > v) && (w - _MaxDeviation < v);
        }

        /// <summary>
        /// 重量捕获数据
        /// </summary>
        private EventWaitHandle _WightWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private EventWaitHandle _ExitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        /// <summary>
        /// 线程运行标识
        /// </summary>
        private bool _bLooping = false;

        /// <summary>
        /// 重量捕获线程
        private Thread _WightCatchThread;

        /// <summary>
        /// 重量缓冲
        /// </summary>
        private CircularQueue<Double> _WightCache;

        /// <summary>
        /// flag对象锁
        /// </summary>
        private object _LockFlagObj = new object(); 

        /// <summary>
        /// 实时获取当前揽件重量
        /// </summary>
        private void GetWight()
        {
            // 串口触发事件和线程退出事件
            WaitHandle[] handles = new WaitHandle[] { _WightWaitHandle, _ExitHandle };
            while (_bLooping)
            {
                int bWait = WaitHandle.WaitAny(handles);
                // 退出线程
                if (bWait == 1)
                {
                    break;
                }

                // 实时称重数据回调
                if (Interlocked.Read(ref _RealWightFlag) == 1)
                {
                    if (ScaleWight != null)
                    {
                        double tmp = Interlocked.CompareExchange(ref _RealWight, 0, 0);

                        WeightEventArgs e = new WeightEventArgs(tmp);
                        e.RealWeight = false;
                        ScaleWight(this, e);
                    }
                }

                if (Interlocked.Read(ref _SampleWeightFlag) == 1)
                {
                    // 计算样本平均值
                    Double avg = _SampleArray.Min();

                    // 排除最小电子称精度误差一下的重量
                    if (avg < 0.02 || Array.IndexOf(_SampleArray, 0.0) != -1)
                    {
                        // 重新取样运算
                        lock (_LockFlagObj)
                        {
                            Interlocked.CompareExchange(ref _SampleWeightFlag, 0, 1);
                            Array.Clear(_SampleArray, 0, _SampleArray.Length);
                        }
                        continue;
                    }

                    // 计算样本方差大小
                    Double sum = 0.0;
                    foreach (var x in _SampleArray)
                    {
                        sum += (x - avg) * (x - avg);
                    }

                    // 计算当前样本集合的方差值
                    Double variance = sum / _nSampleCountPerGroup;

                    // 校验样本的方差值和最大误差之间的大小关系
                    if (avg >= 0.03/*kg*/ && variance <= _MaxDeviation)
                    {
                        lock(_LockFlagObj)
                        {
                            Interlocked.CompareExchange(ref _SampleFlag, 0, 1);
                            Interlocked.CompareExchange(ref _RealWightFlag, 0, 1);
                            Interlocked.CompareExchange(ref _SampleWeightFlag, 0, 1);
                            Array.Clear(_SampleArray, 0, _SampleArray.Length);
                        }
                        
                        // 最终数据回调
                        if (ScaleWight != null)
                        {
                            WeightEventArgs e = new WeightEventArgs(avg);
                            e.RealWeight = true;
                            ScaleWight(this, e);
                        }
                    }
                    // 获取采样样本
                    else
                    {
                        lock(_LockFlagObj)
                        {   
                            Array.Clear(_SampleArray, 0, _SampleArray.Length);
                            Interlocked.CompareExchange(ref _SampleWeightFlag, 0, 1); 
                        }
                    }                    
                }
                
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 开始捕获重量数据
        /// </summary>
        /// <returns>操作结果</returns>
        public virtual bool Start()
        {
            lock(_LockFlagObj)
            {
                Array.Clear(_SampleArray, 0, _SampleArray.Length);
                // _WightCache.Reset();
                // Interlocked.CompareExchange(ref _RealWightFlag, 1, 0);
                Interlocked.CompareExchange(ref _SampleFlag, 1, 0);
                Interlocked.CompareExchange(ref _SampleWeightFlag, 0, 1);
            }
            return true;
        }

        /// <summary>
        /// 停止捕获
        /// </summary>
        /// <returns>操作结果</returns>
        public virtual bool Stop()
        {
            lock(_LockFlagObj)
            {
                Array.Clear(_SampleArray, 0, _SampleArray.Length);
                // _WightCache.Reset();
                Interlocked.Exchange(ref _SampleWeightFlag, 0);
                Interlocked.Exchange(ref _SampleFlag, 0);
            }
            return true;
        }

        /// <summary>
        /// 对象释放接口
        /// </summary>
        public virtual void Dispose()
        {
            Close();
        }

        /// <summary>
        /// 样本数量
        /// </summary>
        public virtual Int32 SampleNum
        {
            set
            {
                //_nSampleCountPerGroup = value;
            }
        }
    }
}
