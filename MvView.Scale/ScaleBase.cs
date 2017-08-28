using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace MvView.Scale
{
    /// <summary>
    /// 根据电子秤协议获取电子秤对象
    /// </summary>
    public class ScaleBase
    {
        /// <summary>
        /// 通过反射获取对象
        /// </summary>
        /// <param name="id">协议名称</param>
        /// <returns>电子秤对象</returns>
        public static IScale GetScale(string id)
        {
            object retVal = null;
            try
            {
                PropertyInfo property = typeof(ScaleBase).GetProperty(id, BindingFlags.Static | BindingFlags.NonPublic);
                if (property != null)
                {
                    retVal = property.GetValue(null, null);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Getscale exception, " + e.Message);
            }
            return retVal as IScale;
        }

        /// <summary>
        /// 耀华A7
        /// </summary>
        private static IScale YaoHuaA7
        {
            get { return new YaoHuaA7(); }
        }

        /// <summary>
        /// 耀华A9
        /// </summary>
        private static IScale YaoHuaA9
        {
            get { return new YaoHuaA9(); }
        }

        /// <summary>
        /// 宝羽
        /// </summary>
        private static IScale BaoYu
        {
            get { return new BaoYu(); }
        }

        /// <summary>
        /// 默认不称重模式
        /// </summary>
        private static IScale NoScale
        {
            get { return new NoScale(); }
        }

        private static IScale HaiMing
        {
            get { return new HaiMing(); }
        }

        private static IScale RuiTai
        {
            get { return new RuiTai(); }
        }

        private static IScale YaoHuaA8
        {
            get { return new YaoHuaA8(); }
        }

    }
}
