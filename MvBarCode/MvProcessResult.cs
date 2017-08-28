using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MvBarCode
{
    /// <summary>
    /// 一维码检测结果
    /// </summary>
    public class MvProcessResult : IDisposable
    {
        // 检测结果原始数据
        private MvBarCodeCore.MvSBcProcessResult _Result;

        /// <summary>
        /// 检测结果参数
        /// </summary>
        public MvBarCodeCore.MvSBcProcessResult Result
        {
            set { _Result = value; }
        }

        /// <summary>
        /// 检测到的一维码信息
        /// </summary>
        public MvCodeInfo[] CodeInfo
        {
            get
            {
                if (_Result.Num <= 0)
                {
                    return null;
                }

                // 根据码的个数申请返回数组                
                MvCodeInfo[] infos = new MvCodeInfo[_Result.Num];

                //for (int i = 0; i < 4; ++i)
                //{
                //    int x = (int)Marshal.PtrToStructure((_Result.Code + 16 + 2*i*Marshal.SizeOf(typeof(int))), typeof(int));

                //    int y = (int)Marshal.PtrToStructure((_Result.Code + 20 + 2*i*Marshal.SizeOf(typeof(int))), typeof(int));

                //    System.Console.WriteLine("{0},{1}", x, y);
                //}

                for (int i = 0; i < _Result.Num; ++i)
                {
                    // 提取每一个一维码信息数据
                    MvBarCodeCore.MvSBcCodeInfo info = new MvBarCodeCore.MvSBcCodeInfo();
                    info = (MvBarCodeCore.MvSBcCodeInfo)Marshal.PtrToStructure(_Result.Code +
                        Marshal.SizeOf(typeof(MvBarCodeCore.MvSBcCodeInfo)) * i, typeof(MvBarCodeCore.MvSBcCodeInfo));
                    infos[i] = new MvCodeInfo(info);
                }
                return infos;
            }
        }

        public void Dispose()
        {
        }
    }
}
