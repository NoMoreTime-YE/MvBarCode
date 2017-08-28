using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace MvView.Scale
{
    internal class HaiMing : IScale
    {
        private SerialPort _Serial;
    
        private bool _bOpen = false;

        private float _lastWeight = 0;

        private float weight = 0;

        private UInt32 count = 0;

        private UInt32 _lastCount = 10000;

        bool flag;

        /// <summary>
        /// 异步事件
        /// </summary>
        public event EventHandler<WeightEventArgs> ScaleWight;



        public virtual bool IsOpen
        {
            get { return _bOpen; }
        }

        /// <summary>
        /// 打开串口设备
        /// </summary>
        /// <param name="info">打开信息</param>
        /// <returns>操作结果</returns>
        public virtual bool Open(string info)
        {
            StreamWriter sw = File.AppendText("D:\\HaiMing.txt");
            string w = "  open in HaiMing";
            sw.Write(w);
            sw.Close();

            try {
                if (_Serial == null)
                {
                    String port = "COM6";

                    // 初始化串口
                    _Serial = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);
                    _Serial.Open();
                }

                if (this._Serial != null)
                {
                    StreamWriter sw2 = File.AppendText("D:\\HaiMing.txt");
                    string w2 = "   串口已经打开\n  ";
                    sw2.Write(w2);
                    sw2.Close();


                    _bOpen = true;
                    byte[] clearSerial = new byte[8] { 0X1, 0X6, 0X0, 0X31, 0X0, 0X8, 0Xd9, 0xc3 };
                    _Serial.Write(clearSerial, 0, 8);

                    while ( _Serial.BytesToRead < 8 )
                        Thread.Sleep(10);
                    byte[] x = new byte[8];
                    _Serial.Read(x, 0, 8);

                    StreamWriter sw3 = File.AppendText("D:\\HaiMing.txt");
                    string w3 = string.Format("  读取串口清零返回  {0} {1} {2}  {3} {4} {5} {6} {7}",x[0],x[1],x[2], x[3], x[4], x[5], x[6], x[7] );
                    sw3.Write(w3);
                    sw3.Close();

                    //表示第一次进入的标示
                    flag = false;
                    _lastCount = 10000;

                    Console.WriteLine(x.ToString());
                }
            }
            catch(Exception e)
            {
                StreamWriter sw4 = File.AppendText("D:\\HaiMing.txt");
                string w4 = e.ToString();
                sw4.Write(w4);
                sw4.Close();

                Console.WriteLine(e.ToString());
            }

            return (this._Serial != null);
        }

        /// <summary>
        /// 关闭串口通道
        /// </summary>
        /// <returns>操作结果</returns>
        public virtual bool Close()
        {
            try
            {
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
        /// 异步Post数据
        /// </summary>
        /// <param name="barCode">一维码信息</param>
        /// <returns></returns>
        public virtual bool AsyncPost(string barCode)
        {
            return GetWight();
        }

        /// <summary>
        /// 实时获取当前揽件重量
        /// </summary>
        private bool GetWight()
        {

            StreamWriter sw4 = File.AppendText("D:\\HaiMing.txt");
            string w4 = "  已经进入AsyncPost \n";
            sw4.Write(w4);
            sw4.Close();

            try
            {
                int cnt = 10;
                while ( cnt-- > 0 )
                {
                    //重量发送指令
                    _Serial.DiscardInBuffer();

                    byte[] buffer = new byte[8] { 0X1, 0X3, 0X0, 0X1, 0X0, 0X2, 0X95, 0XCB };
                    _Serial.Write(buffer, 0, 8);

                    byte[] recvBuffer = new byte[16];

                    while (_Serial.BytesToRead < 9)
                        Thread.Sleep(10);
                    int i = _Serial.Read(recvBuffer, 0, 9);

                    if (recvBuffer[0] != 1 || recvBuffer[1] != 3)
                    {

                        StreamWriter sw8 = File.AppendText("D:\\HaiMing.txt");
                        string w8 = string.Format("  读取串口重量错误返回  {0} {1} {2}  {3} {4} {5} {6} {7} {8}", recvBuffer[0], recvBuffer[1], recvBuffer[2], recvBuffer[3], recvBuffer[4], recvBuffer[5], recvBuffer[6], recvBuffer[7], recvBuffer[8]);
                        sw8.Write(w8);
                        sw8.Close();
                        continue;
                    }

                    StreamWriter sw3 = File.AppendText("D:\\HaiMing.txt");
                    string w3 = string.Format("  读取串口重量返回  {0} {1} {2}  {3} {4} {5} {6} {7} {8} {9}", recvBuffer[0], recvBuffer[1], recvBuffer[2], recvBuffer[3], recvBuffer[4], recvBuffer[5], recvBuffer[6], recvBuffer[7], recvBuffer[8], recvBuffer[9]);
                    sw3.Write(w3);
                    sw3.Close();

                    weight = ((recvBuffer[3] << 8) | recvBuffer[4]);

                    StreamWriter sw9 = File.AppendText("D:\\HaiMing.txt");
                    string w9 = "转化完成重量：：："+weight.ToString();
                    sw9.Write(w9);
                    sw9.Close();

                    // 计算重量
                    weight = weight * (float)0.01;
                    count = (UInt32)((recvBuffer[5] << 8) | recvBuffer[6]);

                    StreamWriter swa = File.AppendText("D:\\HaiMing.txt");
                    string wa = "当前count : " + count.ToString() + "前一次count(last count)" + _lastCount.ToString();
                    swa.Write(wa);
                    swa.Close();

                    if (flag == false)
                    {
                        _lastCount = count;
                        flag = true;
                        continue;
                    }

                    if (count == _lastCount)
                    {
                        Thread.Sleep(50);
                        continue;
                    }
                    else
                    {
                        _lastCount = count;
                        _lastWeight = weight;
                        break;
                    }
                }
                
                // 最终数据回调
                if (ScaleWight != null)
                {
                    StreamWriter sw9 = File.AppendText("D:\\HaiMing.txt");
                    string w9 = "   \n 最终数据回调 \n ";
                    sw9.Write(w9);
                    sw9.Close();

                    //不能置ling
                    //if (cnt <= 0)
                    //{
                    //    weight = 0;
                    //}

                    WeightEventArgs e = new WeightEventArgs(weight);
                    e.RealWeight = true;
                    e._bNeedOutPut = true;
                    ScaleWight(this, e);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("GetWight serial exception, " + e.Message);
            }
            return false;
        }

        /// <summary>
        /// 对象释放接口
        /// </summary>
        public virtual void Dispose()
        {
            Close();
        }

        private Double _MaxDeviation = 0.003;

        public virtual Double MaxDeviation
        {
            private get { return _MaxDeviation; }
            set { _MaxDeviation = value; }
        }

        public virtual bool Post(string barCode, Int32 timeout, ref Double weight)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 开始捕获重量数据
        /// </summary>
        /// <returns>操作结果</returns>
        public virtual bool Start()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 停止捕获
        /// </summary>
        /// <returns>操作结果</returns>
        public virtual bool Stop()
        {
            throw new NotImplementedException();
        }


        private int _nSampleCountPerGroup = 4;
        /// <summary>
        /// 样本数量
        /// </summary>
        public virtual Int32 SampleNum
        {
            set { _nSampleCountPerGroup = value; }
        }

       
    }

}
