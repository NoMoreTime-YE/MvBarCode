using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ThridLibray;

namespace MvView.YunDa
{
    /// <summary>
    /// 手动获取最新帧数据
    /// </summary>
    internal class GrabLoopThread : IDisposable
    {
        /// <summary>
        /// 帧获取对象
        /// </summary>
        private Thread _GrabThread = null;

        /// <summary>
        /// 相机设备对象
        /// </summary>
        private IDevice _GrabDevice = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dev">相机对象</param>
        public GrabLoopThread(IDevice dev)
        {
            this._GrabDevice = dev;
        }

        /// <summary>
        /// 相机打开标识
        /// </summary>
        private bool _bStart = false;

        /// <summary>
        /// 相机是否打开
        /// </summary>
        public bool IsStart
        {
            get { return _bStart; }
        }

        /// <summary>
        /// 开始捕获
        /// </summary>
        public void Start()
        {
            // 校验
            if (_GrabDevice == null || _bStart)
            {
                return;
            }

            // 打开相机
            if (_GrabDevice.IsOpen)
            {
                if (!_GrabDevice.IsGrabbing)
                {
                    _GrabDevice.StreamGrabber.Start(
                        GrabStrategyEnum.grabStrartegySequential, GrabLoop.ProvidedByUser);
                }
            }
            
            // 初始化线程
            if (_GrabThread == null)
            {
                _GrabThread = new Thread(this.GetFrame);
                _GrabThread.Priority = ThreadPriority.Highest;
            }

            // 开始捕获
            if (_GrabThread != null && _GrabDevice.IsGrabbing)
            {
                _GrabThread.Start(); 
                _GrabEvent.Set();
                _bStart = true;
            }
        }

        /// <summary>
        /// 停止捕获
        /// </summary>
        /// <param name="dev"></param>
        public void Stop()
        {
            // 关闭码流
            if (_GrabDevice != null && _GrabDevice.IsGrabbing)
            { 
                if (_GrabDevice.ShutdownGrab())
                { 
                    _GrabEvent.Reset();
                    _bStart = false;
                }
            }
        }

        public void Dispose()
        {
            // 关闭线程
            if (_GrabThread != null)
            {
                _bGrabLooping = false;
                _ExitEvent.Set();
                _GrabThread.Join();
                _GrabThread = null;
            }

            // 关闭码流
            if (_GrabDevice.IsGrabbing)
            {
                _GrabDevice.ShutdownGrab();
            }
        }

        /// <summary>
        /// 获取下一帧数据
        /// </summary>
        /// <returns></returns>
        public void QueryNextBuffer(ref IGrabbedRawData data)
        {
            lock (_LockObj)
            {
                data = _FrameBuffer;
            }
        }

        /// <summary>
        /// 线程循环标识
        /// </summary>
        private bool _bGrabLooping = true;

        /// <summary>
        /// 帧缓冲
        /// </summary>
        private IGrabbedRawData _FrameBuffer = null;

        /// <summary>
        /// 帧对象保护锁
        /// </summary>
        private object _LockObj = new object();

        /// <summary>
        /// 线程信号量
        /// </summary>
        private EventWaitHandle _GrabEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        private EventWaitHandle _ExitEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

        /// <summary>
        /// 码流捕获接口
        /// </summary>
        private void GetFrame()
        {
            EventWaitHandle[] handles = new EventWaitHandle[2] { _ExitEvent, _GrabEvent };
            while (_bGrabLooping)                            
            {
                // 等待取流信号
                if (EventWaitHandle.WaitAny(handles) == 0)
                {
                    break;
                }

				// 获取最新帧数据
                IGrabbedRawData data = null;
                if (!_GrabDevice.WaitForFrameTriggerReady(out data, 200))
                {
                    continue;
                }

                // 获取最新帧数据
                lock (_LockObj)
                {
                    _FrameBuffer = data;
                }

                Thread.Sleep(1);
            }
        }
    }
}
