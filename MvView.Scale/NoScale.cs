using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvView.Scale
{
    internal class NoScale : IScale
    {
        public virtual bool Open(string info)
        {
            _bOpen = true;
            return true;
        }

        private bool _bOpen = false;
        public virtual bool IsOpen
        {
            get { return (_bOpen == true); }
        }

        public virtual bool Close()
        {
            _bOpen = false;
            return true;
        }

        public virtual bool Post(string barCode, Int32 timeout, ref Double weight)
        {
            return true;
        }

        public virtual bool AsyncPost(string barCode)
        {
            return this.Start();
        }

        public virtual bool Start()
        {
            if (this.ScaleWight != null)
            {
                var e = new WeightEventArgs(0.0d);
				
				
                e.RealWeight = true;
                this.ScaleWight(this, e);
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
