using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Scale
{
    /// <summary>
    /// 宝羽电子秤封装类
    /// </summary>
    internal class BaoYu : IScale
    {
        private class Protocol 
        {
            private byte[] _Cmd = new byte[20];

            public Protocol()
            {
                _Cmd[0] = 0x13; // 长度
                _Cmd[1] = 0x01; // 起始
                _Cmd[2] = 0x02; // 起始字节
                _Cmd[3] = 0x01; // 命令字节
                _Cmd[4] = 0x01; // 台号 ？？
                _Cmd[5] = 0x0d; // 单号长度，默认为13位
                _Cmd[0x13] = 0x0a; // 结束符
            }

            public static bool CheckResponse(byte[] buffer)
            {
                /*
                return buffer[0x00] == 0x1c
                    && buffer[0x01] == 0x01
                    && buffer[0x02] == 0x02
                    && buffer[0x03] == 0x01;
                */
                return true;
            }

            public byte[] Encode(int nIdx, string code)
            {
                _Cmd[4] = Convert.ToByte(nIdx);
                byte[] t = Encoding.ASCII.GetBytes(code);
                Array.Copy(t, 0, _Cmd, 0x06, Math.Min(t.Length, 13));
                return _Cmd;
            }
        }

        // 网络通讯类
        private MvTcp _TcpStream = new MvTcp();

        private bool _bOpen = false;

        /// <summary>
        /// 打开网络通道
        /// </summary>
        /// <param name="info">ip:port</param>
        /// <returns></returns>
        public virtual bool Open(string info)
        {
            if (_TcpStream == null)
            {
                _TcpStream = new MvTcp();
            }

            if (!_TcpStream.IsConnected)
            {
                string[] items = info.Split(':');
                if (items.Length < 2)
                {
                    return false;
                }

                if (!_TcpStream.Connect(items[0], Convert.ToInt32(items[1])))
                {
                    return false;
                }

                _TcpStream.StreamBuffer += OmStreamData;

                _bOpen = true;
            }
            return true;
        }

        public virtual bool IsOpen
        {
            get { return _bOpen; }
        }

        /// <summary>
        /// 关闭通道
        /// </summary>
        /// <returns>操作结果</returns>
        public virtual bool Close()
        {
            if (_TcpStream != null)
            {
                _TcpStream.Disconnect();
                _bOpen = false;
            }
            return true;
        }

        /// <summary>
        /// 同步获取称重信息
        /// </summary>
        /// <param name="barCode">一维码信息</param>
        /// <param name="weight">称重数据</param>
        /// <returns>操作结果</returns>
        public virtual bool Post(string barCode, Int32 timeout, ref Double weight)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 称重信息返回事件
        /// </summary>
        public event EventHandler<WeightEventArgs> ScaleWight;

        /// <summary>
        /// 异步获取称重信息
        /// </summary>
        /// <param name="barCode">一维码信息</param>
        /// <returns>操作结果</returns>
        public virtual bool AsyncPost(string barCode)
        {
            if (_TcpStream == null)
            {
                throw new InvalidOperationException();
            }

            if (_TcpStream.IsConnected)
            {
                Protocol p = new Protocol();
                _TcpStream.Send(p.Encode(0x01, barCode));
                return true;
            }
            return false;
        }

        /// <summary>
        /// TCP数据流回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OmStreamData(object sender, StreamEventArgs e)
        {
            byte[] data = e.Data;
            if(!Protocol.CheckResponse(data))
            {
               return;
            }

            /*
            StringBuilder sb = new StringBuilder();
            foreach (var b in data)
            {
                sb.AppendFormat("{0:x2} ", b);
            }
            Console.WriteLine(sb.ToString());
            */

            byte[] weight = null;
            byte[] code = null;
            int weightLenIdx = 0x05;
            if (weightLenIdx + 2 < data.Length)
            {
                // 解析重量
                int nSymbol = (data[weightLenIdx + 1] == 0x2d) ? 1 : 0;
                int weightLen = Convert.ToInt32(data[weightLenIdx]) + nSymbol;
                if (weightLenIdx + weightLen + 1 < data.Length)
                {
                    weight = new byte[weightLen];
                    Array.Copy(data, weightLenIdx + 1, weight, 0, weightLen); 
                    
                    // 解析条码
                    int codeLenIndex = weightLenIdx + weightLen + 2;
                    if (codeLenIndex < data.Length)
                    {
                        int codeLen = Convert.ToInt32(data[codeLenIndex]);
                        if (codeLenIndex + codeLen + 1 < data.Length)
                        {
                            code = new byte[codeLen];
                            Array.Copy(data, codeLenIndex + 1, code, 0, codeLen);
                        }
                    }
                } 
            }

            if (ScaleWight != null)
            {
                double val = 0.0d;
                if(weight != null)
                {
                    val = Convert.ToDouble(
                        System.Text.Encoding.Default.GetString(weight));
                }
                WeightEventArgs we = new WeightEventArgs(val);   
                if (code != null)
                {
                    we.BarCode = System.Text.Encoding.Default.GetString(code);
                }
                we.RealWeight = true;

                ScaleWight(this, we);
            }
        }

        /// <summary>
        /// 开始捕获
        /// </summary>
        /// <returns></returns>
        public virtual bool Start()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 停止捕获
        /// </summary>
        /// <returns></returns>
        public virtual bool Stop()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public virtual void Dispose()
        {
            Close();
        }

        public virtual Double MaxDeviation
        {
            get;
            set;
        }

        public virtual Int32 SampleNum
        {
            get;
            set;
        }
    }
}
