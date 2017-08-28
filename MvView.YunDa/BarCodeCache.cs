using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThridLibray;
using System.Drawing;
using MvBarCode;

namespace MvView.YunDa
{
    /// <summary>
    /// 数据节点
    /// </summary>
    internal class BarCodePayLoad
    {
        // 条码信息 
        private MvCodeInfo[] _CodeInfo;

        /// <summary>
        /// 条码数据构造
        /// </summary>
        /// <param name="info">条码信息</param>
        public BarCodePayLoad(MvCodeInfo[] info)
        {
            _CodeInfo = info;
        }

        // 获取条码信息
        public MvCodeInfo[] CodeInfo
        {
            get { return _CodeInfo; }
        }

        /// <summary>
        /// 获取索引为idx的条码数据
        /// </summary>
        /// <param name="idx">索引</param>
        /// <returns>条码结果</returns>
        public MvCodeInfo this[int idx]
        {
            get
            {
                if (_CodeInfo == null)
                {
                    throw new InvalidOperationException();
                }

                if (idx < 0 || idx >= _CodeInfo.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return _CodeInfo[idx];
            }
        }
    }

    /// <summary>
    /// 数据缓冲池
    /// </summary>
    internal class BarCodeCache : IDisposable
    {
        // 缓冲池
        private BarCodePayLoad[] _PayloadQueue;

        // 缓冲池大小
        private Int32 _Capacity;

        // 缓冲节点元素个数
        private Int32 _Size;

        // 缓冲读索引
        private Int32 _ReadIndex;

        // 缓冲写索引
        private Int32 _WriteIndex;

        /// <summary>
        /// 缓冲构造
        /// </summary>
        /// <param name="maxSize">缓冲池大小</param>
        public BarCodeCache(int maxSize)
        {
            if (maxSize < 1)
            {
                throw new IndexOutOfRangeException();
            }

            // 记录缓冲池大小
            _Capacity = maxSize;

            // 创建缓冲池
            _PayloadQueue = new BarCodePayLoad[maxSize];

            // 记录读写索引
            _Size = _ReadIndex = _WriteIndex = 0;
        }

        /// <summary>
        /// 缓冲池是否为空
        /// </summary>
        public bool IsEmpty
        {
            get { return _Size == 0; }
        }

        /// <summary>
        /// 缓冲池是否满
        /// </summary>
        public bool IsFull
        {
            get { return _Size == _Capacity; }
        }

        /// <summary>
        /// 缓冲池当前大小
        /// </summary>
        public Int32 Size
        {
            get { return _Size; }
        }

        // 互斥对象
        private object _LockObj = new object();

        /// <summary>
        /// 将数据放入缓冲池
        /// </summary>
        /// <param name="info">一维码信息数据</param>
        public void PutBuffer(MvCodeInfo[] info)
        {
            // 初始化一个节点
            BarCodePayLoad item = new BarCodePayLoad(info);
            
            // 将数据放入缓冲池
            lock (_LockObj)
            {
                // 缓冲池满
                if (_ReadIndex == (_WriteIndex + 1) % _Capacity)
                {
                    // 重新分配缓冲池空间
                    BarCodePayLoad[] newQueue = new BarCodePayLoad[2 * _Capacity];

                    // 分配失败，抛出异常
                    if (newQueue == null)
                    {
                        throw new ArgumentOutOfRangeException("Out of alloc size.");
                    }

                    // 若读索引在写索引的后面
                    if (_WriteIndex > _ReadIndex)
                    {
                        // 直接拷贝整个缓冲区
                        Array.Copy(_PayloadQueue, _ReadIndex, newQueue, _ReadIndex, _Size);
                    }
                    else
                    {
                        // 计算读索引到缓冲尾的节点数
                        int tailNum = _Capacity - _ReadIndex;
                        // 拷贝尾部的节点
                        Array.Copy(_PayloadQueue, _ReadIndex, newQueue, _ReadIndex, tailNum);

                        // 拷贝头部的索引
                        Array.Copy(_PayloadQueue, 0, newQueue, _Capacity, _Size - tailNum);
                        // 更新写索引
                        _WriteIndex += _Capacity;
                    }

                    // 更新缓冲池
                    _PayloadQueue = newQueue;

                    //更新缓冲池的大小
                    _Capacity = (_Capacity << 1);
                }

                // 放入写索引
                _PayloadQueue[_WriteIndex] = item;

                // 更新写索引
                _WriteIndex = (_WriteIndex + 1) % _Capacity;

                // 更新缓冲池节点数
                ++_Size;
            }
        }

        /// <summary>
        /// 获取下一帧数据信息
        /// </summary>
        /// <param name="item">帧数据</param>
        public void QueryNextBuffer(ref BarCodePayLoad item)
        {
            lock (_LockObj)
            {
                if (this.IsEmpty)
                {
                    throw new ArgumentOutOfRangeException();
                }

                // 获取读索引
                Int32 idx = _ReadIndex;

                // 更新读索引
                _ReadIndex = (_ReadIndex + 1) % _Capacity;

                // 更新缓冲池节点数
                --_Size;

                // 获取帧数据
                item = _PayloadQueue[idx];
            }
        }

        public void Dispose()
        {
        }
    }
}
