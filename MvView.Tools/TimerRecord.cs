using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MvView.Tools
{
    /// <summary>
    /// 记录节点
    /// </summary>
    internal class RecordItem
    {
        /// <summary>
        /// 记录节点名称
        /// </summary>
        string _Msg;

        /// <summary>
        /// 记录节点事件值
        /// </summary>
        Int64 _time;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="msg">消息</param>
        /// <param name="t">时间值</param>
        public RecordItem(string msg, Int64 t)
        {
            this.Message = msg;
            this.Time = t;
        }

        /// <summary>
        /// 节点消息
        /// </summary>
        public string Message
        {
            private set { _Msg = value; }
            get { return _Msg; }
        }

        /// <summary>
        /// 节点时间
        /// </summary>
        public Int64 Time
        {
            private set { _time = value; }
            get { return _time; }
        }
    }

    /// <summary>
    /// 时间节点管理类
    /// </summary>
    internal class RecordItemImpl
    {
        /// <summary>
        /// 时间消息链
        /// </summary>
        private List<RecordItem> _ItemList = new List<RecordItem>();

        /// <summary>
        /// 开始计时
        /// </summary>
        public void Begin()
        {
            Int64 t = SystemClock.Now();
            this.Reset();
            _ItemList.Add(new RecordItem(@"One record", t));
        }

        /// <summary>
        /// 计时节点
        /// </summary>
        /// <param name="msg">节点消息</param>
        public void Tag(string msg)
        {
            Int64 t = SystemClock.Now();
            _ItemList.Add(new RecordItem(msg, t));
        }

        /// <summary>
        /// 结束计时
        /// </summary>
        public void End()
        {
            Int64 t = SystemClock.Now();
            _ItemList.Add(new RecordItem(@"end", t));
        }

        /// <summary>
        /// 重置计时节点
        /// </summary>
        public void Reset()
        {
            _ItemList.Clear();
        }

        /// <summary>
        /// 将计时节点转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_ItemList.Count < 2)
            {
                return string.Empty;
            }

            string result = _ItemList[0].Message;
            for (var i = 1; i < _ItemList.Count; ++i)
            {
                var cost = SystemClock.MicrosecSubtract(_ItemList[i].Time, _ItemList[i - 1].Time);
                var record = _ItemList[i].Message + ": " + cost.ToString();
                result += ", " + record;
            }
            var totalCost = ", total time pay: " +
                SystemClock.MicrosecSubtract(_ItemList[_ItemList.Count - 1].Time, _ItemList[0].Time).ToString();
            result += totalCost;
            return result;
        }
    }

    /// <summary>
    /// 时间记录对象
    /// </summary>
    public class TimeRecordImpl
    {
        /// <summary>
        /// 记录集名称
        /// </summary>
        private string _RecordName = string.Empty;

        /// <summary>
        /// 记录集数据
        /// </summary>
        private List<string> _Records = new List<string>();

        /// <summary>
        /// 当前记录值
        /// </summary>
        private RecordItemImpl _CurrentRecord = new RecordItemImpl();

        /// <summary>
        /// 同步对象
        /// </summary>
        private object _LockObj = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">记录名称</param>
        public TimeRecordImpl(string name)
        {
            _RecordName = name;
        }

        /// <summary>
        /// 开始记录一条记录
        /// </summary>
        public void Begin()
        {
            lock (_LockObj)
            {
                _CurrentRecord.Begin();
            }
        }

        /// <summary>
        /// 记录节点
        /// </summary>
        /// <param name="msg">节点消息</param>
        public void Tag(string msg)
        {
            lock (_LockObj)
            {
                _CurrentRecord.Tag(msg);
            }
        }

        /// <summary>
        /// 记录中间节点
        /// </summary>
        public void End()
        {
            lock (_LockObj)
            {
                _CurrentRecord.End();
                var record = _CurrentRecord.ToString();
                if (record != string.Empty)
                {
                    _Records.Add(record);
                }
            }
        }

        /// <summary>
        /// dump记录数据到指定路径
        /// </summary>
        private void InternalDump(string path)
        {
            var name = (path == string.Empty) ? @"~tmp.txt" : path;
            using (FileStream fs = new FileStream(name, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    foreach (var s in _Records)
                    {
                        sw.WriteLine(s);
                        sw.Flush();
                    }
                    sw.Close();
                }
                fs.Close();
            }
        }

        /// <summary>
        /// Dump记录
        /// </summary>
        /// <param name="path">记录路径</param>
        public void Dump(string path)
        {
            lock (_LockObj)
            {
                InternalDump(path);
            }
        }
    }

    /// <summary>
    /// 时间记录集
    /// </summary>
    public class TimeRecord
    {
        /// <summary>
        /// 记录集单件
        /// </summary>
        private static TimeRecordImpl _RecordInstance = new TimeRecordImpl(@"InstanceRecord");

        /// <summary>
        /// 同步对象
        /// </summary>
        private static object _LockObj = new object();

        /// <summary>
        /// begin标识
        /// </summary>
        private static bool _bFlag = false;

        /// <summary>
        /// 开始一条记录
        /// </summary>
        public static void Begin()
        {
            lock (_LockObj)
            {
                if (!_bFlag)
                {
                    _RecordInstance.Begin();
                    _bFlag = true;
                }
            }
        }

        /// <summary>
        /// 中间记录节点
        /// </summary>
        /// <param name="msg">节点消息</param>
        public static void Tag(string msg)
        {
            lock (_LockObj)
            {
                if (_bFlag) { _RecordInstance.Tag(msg); }
            }
        }

        /// <summary>
        /// 结束一条记录
        /// </summary>
        public static void End()
        {
            lock (_LockObj)
            {
                if (_bFlag)
                {
                    _RecordInstance.End();
                    _bFlag = false;
                }
            }
        }

        /// <summary>
        /// dump记录集
        /// </summary>
        /// <param name="path">记录集存储路径</param>
        public static void Dump(string path)
        {
            lock (_LockObj)
            {
                _RecordInstance.Dump(path);
            }
        }
    }
}
