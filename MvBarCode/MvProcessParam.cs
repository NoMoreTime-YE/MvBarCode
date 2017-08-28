using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MvBarCode
{
    /// <summary>
    /// 检测算法参数对象
    /// </summary>
    public class MvProcessParam : IDisposable
    {
        // 参数对象
        private MvBarCodeCore.MvSBcProcessParam _Param = new MvBarCodeCore.MvSBcProcessParam();

        /// <summary>
        /// 构造函数
        /// </summary>
        public MvProcessParam()
        {
            _Param.Initialization();
        }

        /// <summary>
        /// 获取检测参数
        /// </summary>
        public MvBarCodeCore.MvSBcProcessParam Param
        {
            get { return _Param; }
        }

        /// <summary>
        /// 训练标识
        /// </summary>
        public bool TrainFlag
        {
            get { return _Param.doTrainFlag == 0x01; }
            set { _Param.doTrainFlag = (value ? 1 : 0); }
        }

        /// <summary>
        /// 图像数据参数
        /// </summary>
        public MvsImageParam Image
        {
            set
            {
                // 参数设置非空检测
                if(value == null)
                {
                    throw new ArgumentNullException();
                }

                // 若参数内存已经申请内存，先释放防止内存泄露
                /*
                if(_Param.Image != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_Param.Image);
                    _Param.Image = IntPtr.Zero;
                }
                */

                // 分配参数内存
                if (_Param.img == IntPtr.Zero)
                {
                    _Param.img = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MvsCommon.MvSImage)));
                }
                
                // 拷贝数据
                if(_Param.img != IntPtr.Zero)
                {
                    // 映射参数到指定内存
                    Marshal.StructureToPtr(value.Param, _Param.img, false);
                }
            }
        }

        /// <summary>
        /// 参数清理
        /// </summary>
        public void Dispose()
        {
            // 清理参数，释放分配的全局内存
            if(_Param.img != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_Param.img);
                _Param.img = IntPtr.Zero;
            }
        }
    }
}
