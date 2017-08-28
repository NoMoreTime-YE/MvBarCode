using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;

namespace MvView.Scale
{
    /// <summary>
    /// 网络通讯类
    /// </summary>
    internal class MvTcp
    {
        /// <summary>
        /// 数据读写状态对象
        /// </summary>
        internal class StateObject
        {
            public TcpClient client = null;
            public int totalBytesRead = 0;
            public const int BufferSize = 1024;
            public string readType = null;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder messageBuffer = new StringBuilder();
        }

        // TCP对象
        private TcpClient _Client;

        // 网络流对象，用于数据收发
        private NetworkStream _Stream;

        /// <summary>
        /// TCP客户端数据获取事件
        /// </summary>
        public event EventHandler<StreamEventArgs> StreamBuffer;

        /// <summary>
        /// 连接远程对象
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port">端口号</param>
        /// <returns>连接状态</returns>
        public bool Connect(string ip, int port)
        {
            // 初始化客户端
            if(_Client == null)
            {
                _Client = new TcpClient();
                _Client.ReceiveTimeout = 10;
            }

            // 检查客户端连接状态
            if(_Client.Connected)
            {
                return true;
            }

            bool retVal = false;
            try
            {
                // 同步连接远程服务端
                _Client.Connect(ip, port);
                if (_Client.Connected)
                {
                    _Stream = _Client.GetStream();
                    retVal = true;
                    AsyncRead();
                } 
            }
            catch(SocketException se)
            {
                // 捕获异常
                Debug.WriteLine("MvTcp connect exception, " + se.Message);
            }
            return retVal;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public void Disconnect()
        {
            if (_Client != null)
            {
                if(_Client.Connected)
                {
                    _Stream.Close();
                }

                _Client.Close();
            }
        }

        /// <summary>
        /// 客户端是否建立连接
        /// </summary>
        public bool IsConnected
        {
            get { return _Client != null ? _Client.Connected : false; }
        }

        /// <summary>
        /// 发送字节数组
        /// </summary>
        /// <param name="data">字节</param>
        public void Send(byte[] data)
        {
            try
            {
                if (_Stream != null)
                {
                    _Stream.Write(data, 0, data.Length);
                }
            }
            catch (SocketException se)
            {
                Debug.WriteLine("MvTcp send data exception, " + se.Message);
            }
        }

        /// <summary>
        /// 发送字符串
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="isHex">是否为hex字符串</param>
        public void Send(string str, bool isHex)
        {
            byte[] data = null;
            if (isHex)
            {
                data = StreamCoder.HexStringToByteArray(str);
            }
            else
            {
                data = Encoding.ASCII.GetBytes(str);
            }

            this.Send(data);
        }

        /// <summary>
        /// 异步监听读时间
        /// </summary>
        private void AsyncRead()
        {
            StateObject st = new StateObject();
            st.client = _Client;
            if(_Stream.CanRead)
            {
                try
                {
                    IAsyncResult ar = _Stream.BeginRead(st.buffer, 0, StateObject.BufferSize,
                        new AsyncCallback(TCPReadCallBack), st);
                }
                catch(SocketException se)
                {
                    Debug.WriteLine("MvTcp async read exception, " + se.Message);
                }
            }
        }

        /// <summary>
        /// 异步数据读取
        /// </summary>
        /// <param name="ar"></param>
        private void TCPReadCallBack(IAsyncResult ar)
        {
            StateObject st = (StateObject)ar.AsyncState;
            if (st.client == null || !st.client.Connected)
            {
                return;
            }

            int nBytes;
            NetworkStream ns = st.client.GetStream();
            nBytes = ns.EndRead(ar);

            st.totalBytesRead += nBytes;
            if (nBytes > 0)
            {
                // 通过事件的方式通知数据
                if (StreamBuffer != null)
                {
                    byte[] data = new byte[nBytes];
                    Array.Copy(st.buffer, 0, data, 0, nBytes);
                    StreamBuffer(this, new StreamEventArgs(data));
                }
                
                // 监听数据
                ns.BeginRead(st.buffer, 0, StateObject.BufferSize,
                    new AsyncCallback(TCPReadCallBack), st);
            }
            else
            {
                ns.Close();
                st.client.Close();
                ns = null;
                st = null;
            }
        }
    }
}
