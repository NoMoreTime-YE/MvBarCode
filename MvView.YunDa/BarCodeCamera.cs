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
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MvView.YunDa
{
    /// <summary>
    /// 设备层相机对象类
    /// </summary>
    internal class BarCodeCamera : IDisposable
    {
        // 设备序列号
        private string _DeviceIP;

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

        private bool _isSavePic = false;

        // 缓冲池
        // private BarCodeCache _FrameCache = null;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// 构造相机
        /// </summary>
        /// <param name="key"></param>
        public BarCodeCamera(string ip)
        {
            _DeviceIP = ip;

            StringBuilder builder = new StringBuilder(256);
            GetPrivateProfileString("WeightParam", "IsSavePic", "0", builder, 256, Environment.CurrentDirectory + "\\config.ini");
            int result;
            int.TryParse(builder.ToString(), out result);
            if (result == 0)
            {
                _isSavePic = false;
            }
            else
            {
                _isSavePic = true;
            }
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
            // 搜索设备
            List<IDeviceInfo> li = Enumerator.EnumerateDevices();

            if (li.Count > 0)
            {
                // 根据IP地址获取设备
                if (_DeviceIP != string.Empty)
                {
                    _Camera = Enumerator.GetDeviceByGigeIP(_DeviceIP);
                }
                // 单台相机操作可以不用获取IP地址
                else
                {
                    _Camera = Enumerator.GetDeviceByIndex(0);
                }
            }
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
                if(ThridLibray.Enumerator.GigeInterfaceInfo(_Camera.Index, out mac, out ip, out mask, out gateway))
                {
                    if(mac != string.Empty)
                    {
                        return mac.Replace(":", "").Replace(" ", "").Replace("-", "");
                    }
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 检查一维码长度
        /// </summary>
        /// <param name="len">被检查长度信息</param>
        /// <returns>检查结果</returns>
        private bool CheckCodeLen(int len)
        {
            return len == 0x18 || len == 0x0d || len == 0x12; 
        }

        /// <summary>
        /// 条形码截取
        /// </summary>
        /// <param name="code">被截取的条码数据</param>
        /// <returns>截取结果</returns>
        private string ShortCutBarCode(char[] code)
        {
            return new string(code, 0, 0x0d);
        }

        /// <summary>
        /// 检查条码字符
        /// </summary>
        /// <param name="code">条码信息</param>
        /// <returns>检查结果</returns>
        private bool CheckBarCodeCharacter(string code)
        {
            foreach (var c in code)
            {
                if (c < 0x30 || c > 0x39)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查并捕获条码数据
        /// </summary>
        /// <param name="item">条码信息</param>
        /// <param name="result">条码捕获结果</param>
        /// <returns>条码检测结果</returns>
        private bool CheckAndCatchBarcode(MvCodeInfo item, ref List<string> result)
        {
            // 检查条形码有效性
            if (item.Valid != 0x01ff)
            {
                return false;
            }

            // 检查条形码长度
            if (!CheckCodeLen(item.CodeLen))
            {
                return false;
            }

            // 截取待检查条形码，默认为13位
            string tmp = ShortCutBarCode(item.Code);

            // 校验一维码字符的有效性
            if (!CheckBarCodeCharacter(tmp))
            {
                return false;
            }

            // 捕获条码数据
            if (result.IndexOf(tmp) == -1)
            {
                result.Add(tmp);
                return true;
            }
            return false;
        }

        // 扫码线程
        private Thread _BarCodeThread;

        // 算法执行标识
        private long _bAlgorithmRunning = 0;

        // 相机采集标识
        private long _bCollection = 0;

        // 帧缓冲对象
        private IGrabbedRawData _FrameData = null;

        // 码流捕获线程
        private GrabLoopThread _GrabImageThread = null;

        /// <summary>
        /// 开启码流
        /// </summary>
        public void StartGrab()
        {
            new TaskFactory().StartNew(() => 
            {
                if (_Camera == null)
                {
                    throw new InvalidOperationException("Camera is not opened.");
                }

                // 开启一维码检测线程
                if (_BarCodeThread == null)
                {
                    _BarCodeThread = new Thread(DoBarCode);  
                }

                // 初始化帧数据捕获线程
                if (_GrabImageThread == null)
                {
                    _GrabImageThread = new GrabLoopThread(_Camera);
                }

                if (!_GrabImageThread.IsStart)
                {
                    _GrabImageThread.Start();
                }

                // 若线程未启动，启动线程
                if (!_BarCodeThread.IsAlive)
                {
                    _bBarCodeLooping = true;
                    _BarCodeThread.Priority = ThreadPriority.Highest;
                    _BarCodeThread.Start();
                }

                // Interlocked.Exchange(ref _bAlgorithmRunning, 0);
                Interlocked.CompareExchange(ref _bCollection, 1, 0);
            }).Wait();
        }

        // 线程通知标识
        public event EventHandler<BarCodeEventArgs> BarCodeHandle;

        // 一维码缓冲大小
        private const int _nCodeCache = 4;

        // 一维码检测线程
        private bool _bBarCodeLooping = false;

        // 一唯码图像
        private long _bGrabImagFlag = 0;

        private EventWaitHandle _DetectBarCodeEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        private EventWaitHandle _ExitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        // 一维码缓冲区
        private CircularQueue<String> _LastBarCodeQueue = new CircularQueue<String>(_nCodeCache);

        // 记录最后一个扫描的条形码数据
        private string _LastBarCode = string.Empty;
        private void DoBarCode()
        {
            MvBarCodeCore magic = new MvBarCodeCore();
            try
            {
                // 初始化算法
                if (!magic.InitAlgorithm(this.Width, this.Height))
                {
                    Debug.WriteLine("InitAlgorithm failed");
                    return;
                }

                // 初始化算法配参
                MvBarCodeCore.BcConfigParam param = new MvBarCodeCore.BcConfigParam();
                param.Initialization();
                magic.GetConfig(ref param);
                param.CodeType = Convert.ToInt32(MvCodeType.AutoType);
                param.Scale = 4;
                param.MinFactor = 3;
                param.StopAfterResultNum = 4;
                param.PeakThreshInit = 10;
                param.PeakThreshRatio = 0.1F;
                magic.SetConfig(ref param);

                // 记录最新条形码信息
                List<string> newCodes = new List<string>();

                // 记录CodeInfo信息
                MvCodeInfo mci = null;

                // 算法运行标识
                WaitHandle[] handles = new WaitHandle[] { _DetectBarCodeEvent, _ExitHandle };
                
                // 算法运算
                while (_bBarCodeLooping)
                {
                    // 获取最新帧数据
                    _GrabImageThread.QueryNextBuffer(ref _FrameData);

                    // 校验帧数据
                    if (_FrameData == null)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    // 若当前无检测数据，线程休息1ms
                    //if (Interlocked.Read(ref _bAlgorithmRunning) == 0)
                    //{
                    //    continue;
                    //}

                    // TimeRecord.Begin();


                    // 检测指定图片
                    MvCodeInfo[] ci = null;
                    using (MvsImageParam img = new MvsImageParam())
                    {
                        // 赋值图像样本参数
                        img.Width = _FrameData.Width;
                        img.Height = _FrameData.Height;
                        img.DataType = Convert.ToInt32(MvsImgDataType.MVS_IMGDTP_U8);
                        img.Type = Convert.ToInt32(MvsImgType.MVS_IMGTP_UITL_Y);
                        img.ImageData = _FrameData.Image;

                        // 检测算法
                        magic.Process(img, false, ref ci);
                    }

                    // 返回结果为空，继续算法运算
                    if (ci == null)
                    {
                        // 重置检测状态,重新采样
                        // Interlocked.CompareExchange(ref _bAlgorithmRunning, 0, 1);
                        // TimeRecord.End();
                        continue;
                    }


                    // 重置所有检测标识
                    mci = null;
                    newCodes.Clear();

                    // 捕获条形码数据
                    foreach (var item in ci)
                    {
                        if (CheckAndCatchBarcode(item, ref newCodes))
                        {
                            mci = item;
                        }
                    }

                    // 若检测到新的条形码
                    if (newCodes.Count > 0)
                    {
                        // 检测是否有多个条码数据
                        if (newCodes.Count > 1)
                        {
                            // 回调完成后重新采样，并捕获一张涂片数据
                            // Interlocked.CompareExchange(ref _bGrabImagFlag, 1, 0);

                            // 多个条码数据通知异常
                            if (BarCodeHandle != null)
                            {
                                // 删除最新的元素
                                int idx = _LastBarCodeQueue.EarliestIndex(newCodes.ToArray());
                                if (idx != -1) { newCodes.RemoveAt(idx); }
                                BarCodeHandle(this, new BarCodeEventArgs(new BarCodeDescribe(_FrameData.ToBitmap(false),
                                    DetectExceptionType.MulBarCodeExceptionType, newCodes.ToArray())));
                            }
                        }
                        // 判断重复条码
                        else  if (0 != string.Compare( _LastBarCode, newCodes[0]))
                        {
                            // 更新最后一个条码信息
                            _LastBarCode = newCodes[0];

                            // 加入缓冲
                            if (!_LastBarCodeQueue.IsIn(_LastBarCode))
                            {
                               // 更新条码缓存
                                _LastBarCodeQueue.Push(_LastBarCode);
                            }

                            // 回调完成后重新采样，并捕获一张涂片数据
                            // Interlocked.CompareExchange(ref _bGrabImagFlag, 1, 0);

                            // 通知条码数据
                            if (BarCodeHandle != null)
                            {
                                BarCodeHandle(this, new BarCodeEventArgs(new BarCodeDescribe(_FrameData.ToBitmap(false), mci)));
                            }

                            if (_isSavePic == true)
                            {
                                _FrameData.ToBitmap(false).Save(".\\pic\\" + _LastBarCode + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + DateTime.Now.Millisecond.ToString("D3") + ".jpg", ImageFormat.Jpeg);
                            }
                        }
                    }

                    // 重新采样
                    // Interlocked.CompareExchange(ref _bAlgorithmRunning, 0, 1);
                    Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                MvBarCodeGlobalVar.Log.Error(e);
            }
            finally
            {
                magic.Dispose();
            }
        }

        /// <summary>
        /// 关闭码流
        /// </summary>
        public void StopGrab()
        {
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
        }

        /// <summary>
        /// Io输出互斥变量
        /// </summary>
        private long _bIoOutFlag = 0;

        /// <summary>
        /// Io输出
        /// </summary>
        /// <param name="millSeconds">毫秒数</param>
        public void IoOutput(int millSeconds)
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
                            // 选择输出信号
                            using (IEnumParameter ep = _Camera.ParameterCollection[ParametrizeNameSet.DigitalIoLineSelector])
                            {
                                ep.SetValue("Line0");
                            }

                            // 选择用户输出
                            using (IEnumParameter ep = _Camera.ParameterCollection[ParametrizeNameSet.UserOutputSelector])
                            {
                                ep.SetValue("UserOutPut0");
                            }

                            // 输出信号脉冲
                            using (IBooleanParameter bp = _Camera.ParameterCollection[ParametrizeNameSet.UserOutputValue])
                            {
                                bp.SetValue(false);
                            }

                            Thread.Sleep(100);

                            // 停止输出脉冲
                            using (IBooleanParameter bp = _Camera.ParameterCollection[ParametrizeNameSet.UserOutputValue])
                            {
                                bp.SetValue(true);
                            }

                            // 允许输出
                            Interlocked.CompareExchange(ref _bIoOutFlag, 0, 1);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Digital io output exception: " + e.Message);
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
                return false;
            }

            if (serialNo[8] != 'A' || serialNo[9] != 'K')
            {
                return false;
            }

            for (int i = 10; i < 15; ++i)
            {
                if (serialNo[i] < '0' || serialNo[i] > '9')
                {
                    return false;
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
            var task = new TaskFactory().StartNew(() => 
            {
                // 获取相机的对象
                if (_Camera == null)
                {
                    List<IDeviceInfo> li = Enumerator.EnumerateDevices();

                    if (li.Count > 0)
                    {
                        if (_DeviceIP != string.Empty)
                        {
                            _Camera = Enumerator.GetDeviceByGigeIP(_DeviceIP);
                        }
                        else
                        {
                            // 默认获取第0号索引的相机
                            _Camera = Enumerator.GetDeviceByIndex(0);
                        }
                    }
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

                        /*
                        // 初始化缓冲池
                        if (_FrameCache == null)
                        {
                            _FrameCache = new BarCodeCache(10);
                        }
                        */

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

        /// <summary>
        /// 相机是否打开
        /// </summary>
        public bool IsOpen
        {
            get { return _Camera != null ? _Camera.IsOpen : false; }
        }

        /// <summary>
        /// 关闭相机
        /// </summary>
        public void Close()
        {
            new TaskFactory().StartNew(() =>
            {
                if (_Camera == null)
                {
                    throw new InvalidOperationException("Camera is not opened.");
                }

                if (_Camera.IsOpen)
                {
                    _Camera.Close();
                }

                if (_BarCodeThread != null)
                {
                    _bBarCodeLooping = false;
                    _ExitHandle.Set();
                    _BarCodeThread.Join();
                }
                
            }).Wait();
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
                _FrameData = e.GrabResult.Clone();
                _DetectBarCodeEvent.Set();
            }
            
            // 异步显示图像
            if (Interlocked.CompareExchange(ref _bGrabImagFlag, 0, 1) == 1)
            {
                var data = e.GrabResult;
                // 异步显示
                new TaskFactory().StartNew(() =>
                {
                    if (BarCodeHandle != null)
                    {
                        BarCodeHandle(this, new BarCodeEventArgs(new BarCodeDescribe(data.ToBitmap(false), null)));
                    }
                });
            }
        }

        /// <summary>
        /// 获取帧数据信息
        /// </summary>
        /// <param name="item">帧数据</param>
        public void QueryNextBuffer(ref BarCodePayLoad item)
        {
            /*
            if (_FrameCache != null)
            {
                _FrameCache.QueryNextBuffer(ref item);
            }
            */
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
                Close();
                _Camera.Dispose();
                _Camera = null;
            }

            /*
            // 清理缓冲池
            if (_FrameCache != null)
            {
                _FrameCache.Dispose();
                _FrameCache = null;
            }
            */
        }
    }
}

