using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log4YunDa
{
    public static class LogClass
    {
        
        //表示向txt写入文本
        static StreamWriter sw;
        static string logAddress = @"D:\MVBarCode.log";

        public static void Info(string s)
        {
            AddLog("提示", ref s);
        }

        public static void Error(string s)
        {
            AddLog("警告", ref s);
        }

        private static void AddLog(string infoType, ref string info)
        {
            using (sw = File.AppendText(logAddress))
            {
                string message = "时间： " + DateTime.Now + "\r\n" +
                    infoType + "： " + info + "\r\n" +
                    "位置： " + GetCurSourceFileName() + "\r\n" +
                    "行数： " + GetLineNum().ToString() + "\r\n\r\n";

                sw.Write(message);
                sw.Close();
            }
        }

        public static int GetLineNum()
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(3, true);
            return st.GetFrame(0).GetFileLineNumber();
        }

        public static string GetCurSourceFileName()
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(3, true);
            return st.GetFrame(0).GetFileName();
        }
    




    }
}
