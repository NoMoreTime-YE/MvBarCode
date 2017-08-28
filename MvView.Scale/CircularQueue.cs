using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Scale
{
    /// <summary>
    /// 缓冲队列，所有操作针对 len < _capacity
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CircularQueue<T>
    {
        /// <summary>
        /// 队列数组
        /// </summary>
        private T[] _queue;

        /// <summary>
        /// 队列内存大小
        /// </summary>
        private int _capacity;

        /// <summary>
        /// 队尾
        /// </summary>
        private int _back;

        /// <summary>
        /// 队首
        /// </summary>
        private int _front;

        /// <summary>
        /// 队列大小
        /// </summary>
        private int _size;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxSize">队列大小</param>
        public CircularQueue(int maxSize)
        {
            // 队列初始化大小大于1
            if (maxSize < 1)
            {
                throw new IndexOutOfRangeException("Queue size is too small.");
            }

            // 队列容量
            _capacity = maxSize;

            // 创建队列数组
            _queue = new T[maxSize];

            // 初始化写索引
            _front = _back = 0;

            // 队列大小
            _size = 0;
        }

        /// <summary>
        /// 队列存储元素能力个数
        /// </summary>
        public int Capacity
        {
            get
            {
                return _capacity;
            }
        }

        public T[] Buffer
        {
            get { return _queue; }
        }

        /// <summary>
        /// 写数据到环形缓冲
        /// </summary>
        /// <param name="item"></param>
        public void Push(T item)
        {
            // 更新覆盖
            if (_size > 0 && _front == _back)
            {
                _front = (_front + 1) % _capacity;
            }

            // 存储节点数据
            _queue[_back] = item;
            _back = (_back + 1) % _capacity;
            _size = Math.Min(++_size, _capacity);
        }

        /// <summary>
        /// 将过个元素到缓冲区
        /// </summary>
        /// <param name="item">入队元素</param>
        public void Push(T[] item)
        {
            if (item.Length + _back <= _capacity)
            {
                Array.Copy(item, 0, _queue, _back, item.Length);

                if (_back < _front)
                {
                    _front = Math.Max(_front, _back + item.Length);
                }
            }
            else
            {
                int nTail = _capacity - _back;
                Array.Copy(item, 0, _queue, _back, nTail);
                Array.Copy(item, nTail, _queue, 0, item.Length - nTail);

                if (_back > _front)
                {
                    _front = Math.Max(_front, item.Length - nTail);
                }
                else
                {
                    _front = Math.Min(_front, item.Length - nTail);
                }
            }
            _size = Math.Min(_size + item.Length, _capacity);
            _back = (_back + item.Length) % _capacity;
        }

        /// <summary>
        /// 校验队列中是否有元素
        /// </summary>
        /// <param name="item">被校验元素</param>
        /// <returns></returns>
        public bool IsIn(T item)
        {
            if (_queue == null || _size <= 0)
            {
                return false;
            }

            int idx = _front;
            do
            {
                if (_queue[idx].Equals(item))
                {
                    return true;
                }
                idx = ((++idx) % _capacity);
            } while (idx != _back);
            return false;
        }

        /// <summary>
        /// 队列是否满
        /// </summary>
        public bool IsFull
        {
            get
            {
                return _size == _capacity;
            }
        }

        public int Size
        {
            get
            {
                return _size;
            }
        }

        /// <summary>
        /// 获取队列数据，获取最后的n个元素
        /// </summary>
        public T[] Pop(int n)
        {
            // 检查读取个数
            if (n > _size)
            {
                throw new ArgumentOutOfRangeException();
            } 

            // 初始化输出数组
            T[] val = new T[n];

            // 直接拷贝
            if (_back >= n)
            {
                Array.Copy(_queue, _back - n, val, 0, n);
            }
            // 环形拷贝
            else
            {
                int nFront = _back;
                Array.Copy(_queue, 0, val, 0, nFront);
                Array.Copy(_queue, _capacity - n + nFront, val, nFront, n - nFront);
            }
            // _back = (_back + _capacity - n) % _capacity;
            // _size -= n;
            return val;
        }

        /// <summary>
        /// 获取最早单号
        /// </summary>
        /// <param name="items">查询记录集</param>
        /// <returns>查询结果</returns>
        public int EarliestIndex(T[] items)
        {
            if (_size <= 0)
            {
                return -1;
            }

            int idx = _front;
            do
            {
                int n = Array.IndexOf(items, _queue[idx]);
                if (n != -1)
                {
                    return n;
                }
                idx = ((++idx) % _capacity);
            } while (idx != _back);
            return -1;
        }

        /// <summary>
        /// 重置环形队列
        /// </summary>
        public void Reset()
        {
            _front = 0;
            _back = 0;
            _size = 0;
            Array.Clear(_queue, 0, _queue.Length);
        }
    }
}
