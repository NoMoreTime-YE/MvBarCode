using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Scale
{
    internal class YaoHuaA9 : IScale
    {
        public virtual bool Open(string info)
        {
            return true;
        }

        public virtual bool IsOpen 
        {
            get { return true; }
        }

        public virtual bool Close()
        {
            return true;
        }

        public virtual bool Post(string barCode, Int32 timeout, ref Double weight)
        {
            return true;
        }

        public virtual bool AsyncPost(string barCode)
        {

            return Start();
        }

        public virtual bool Start()
        {
            if (ScaleWight != null)
            {
                ScaleWight(this, new WeightEventArgs(0.0d));
            }
            return true;
        }

        public virtual bool Stop()
        {
            return true;
        }


        public virtual Double MaxDeviation
        {
            set;
            get;
        }

        public virtual Int32 SampleNum
        {
            set;
            get;
        }

        public event EventHandler<WeightEventArgs> ScaleWight;

        public virtual void Dispose()
        {
            if (ScaleWight != null)
            {
                ScaleWight = null;
            }
        }
    }
}
