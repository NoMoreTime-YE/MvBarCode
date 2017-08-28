using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Scale
{
    /// <summary>
    /// TCP流数据事件
    /// </summary>
    internal class StreamEventArgs : EventArgs
    {
        // 事件参数
        private byte[] _Arg;

        /// <summary>
        /// 事件构造
        /// </summary>
        /// <param name="hex">事件数据</param>
        internal StreamEventArgs(byte[] hex)
        {
            _Arg = hex;
        }

        /// <summary>
        /// 返回数据
        /// </summary>
        public byte[] Data
        {
            get { return _Arg; }
        }

        /// <summary>
        /// 克隆事件
        /// </summary>
        /// <returns>克隆出的事件对象</returns>
        public StreamEventArgs Clone()
        {
            return new StreamEventArgs(_Arg.Clone() as byte[]);
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns>转换后的字符串</returns>
        public override string ToString()
        {
            return new string(Encoding.UTF8.GetChars(_Arg));
        }

        /// <summary>
        /// 转换为16进制形式的字符串
        /// </summary>
        /// <returns>转换后的字符串</returns>
        public string ToHexString()
        {
            return StreamCoder.ByteArrayToHexString(_Arg);
        }
    }
}
