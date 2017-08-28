using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MvBarCode
{
    /// <summary>
    /// 条形码信息
    /// </summary>
    public class MvCodeInfo
    {
        public MvBarCodeCore.MvSBcCodeInfo _Param;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="info">算法返回参数</param>
        public MvCodeInfo(MvBarCodeCore.MvSBcCodeInfo info)
        {
            _Param = info;
        }

        /// <summary>
        /// 区域点坐标数组
        /// </summary>
        public struct MvPtArray
        {
            public Point[] PtArray;
        }

        public string PrintMvPtArray()
        {
            string str = string.Empty;

            foreach (var item in Region.PtArray)
            {
                str += "point " + item.X + " " + item.Y;
            }

            return str;
        }

        /// <summary>
        /// 当前码是否有效
        /// </summary>
        public Int32 Valid
        {
            get { return _Param.Valid; }
        }

        /// <summary>
        /// 码类型
        /// </summary>
        public Int32 Type
        {
            get { return _Param.Type; }
        }

        /// <summary>
        /// 码数据信息
        /// </summary>
        public char[] Code
        {
            get { return _Param.Code.ToCharArray(); }
        }

        /// <summary>
        /// 一维码数据长度
        /// </summary>
        public int CodeLen
        {
            get { return _Param.Len; }
        }
        public static void ClockwiseSortPoints(ref Point[] vPoints)
        {
            //计算重心
            Point center = new Point();
            double X = 0, Y = 0;
            for (int i = 0; i < vPoints.Length; i++)
            {
                X += vPoints[i].X;
                Y += vPoints[i].Y;
            }
            center.X = (int)X / vPoints.Length;
            center.Y = (int)Y / vPoints.Length;

            //冒泡排序
            for (int i = 0; i < vPoints.Length - 1; i++)
            {
                for (int j = 0; j < vPoints.Length - i - 1; j++)
                {
                    if (PointCmp(vPoints[j], vPoints[j + 1], center))
                    {
                        Point tmp = vPoints[j];
                        vPoints[j] = vPoints[j + 1];
                        vPoints[j + 1] = tmp;
                    }
                }
            }
        }
        //若点a大于点b,即点a在点b顺时针方向,返回true,否则返回false
        static bool PointCmp(Point a, Point b, Point center)
        {
            if (a.X >= 0 && b.X < 0)
                return true;
            if (a.X == 0 && b.X == 0)
                return a.Y > b.Y;
            //向量OA和向量OB的叉积
            int det = Convert.ToInt32((a.X - center.X) * (b.Y - center.Y) - (b.X - center.X) * (a.Y - center.Y));
            if (det < 0)
                return true;
            if (det > 0)
                return false;
            //向量OA和向量OB共线，以距离判断大小
            double d1 = (a.X - center.X) * (a.X - center.X) + (a.Y - center.Y) * (a.Y - center.Y);
            double d2 = (b.X - center.X) * (b.X - center.Y) + (b.Y - center.Y) * (b.Y - center.Y);
            return d1 > d2;
        }

        /// <summary>
        /// 区域信息
        /// </summary>
        public MvPtArray Region
        {
            get
            {
                // 分配区域信息
                MvPtArray result = new MvPtArray();

                // 逐个获取每一个区域结构
                //MvBarCodeCore.MvSPoint ptArr = new MvBarCodeCore.MvSPoint();
                //ptArr = (MvBarCodeCore.MvSPoint)Marshal.PtrToStructure(_Param.pts, typeof(MvBarCodeCore.MvSPoint));
                //result.PtArray = new Point[4];
                //for (int j = 0; j < 4; ++j)
                //{
                //    // 逐个获取区域内每一个点坐标信息
                //    Point p = new Point();
                //    p = (Point)Marshal.PtrToStructure(_Param.pts + Marshal.SizeOf(typeof(Point)) * j, typeof(Point));
                //    result.PtArray[j] = p;
                //}


                result.PtArray = new Point[4];
                for (int j = 0; j < 4; ++j)
                {
                    // 逐个获取区域内每一个点坐标信息
                    Point p = new Point();
                    p.X = _Param.pts[j].x;
                    p.Y = _Param.pts[j].y;
                    result.PtArray[j] = p;
                }

                //ClockwiseSortPoints(ref result.PtArray);
                return result;
            }
        }
    }
}
