using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MvView.Core
{
    public delegate void DoHandler();

    public class TaskHost
    {
        private ManualResetEvent _TimeoutObject = null;

        private bool _bTimeout = false;

        private DoHandler Do = null;

        public TaskHost()               
        {
            this._TimeoutObject = new ManualResetEvent(true);
        }

        public bool DoWithTimeout(TimeSpan ts)
        {
            if (this.Do == null)
            {
                return false;
            }
            this._TimeoutObject.Reset();
            this._bTimeout = true;
            this.Do.BeginInvoke(DoAsyncCallback, null);

            if(this._TimeoutObject.WaitOne(ts, false))
            {
                this._bTimeout = true;
            }
            return this._bTimeout;
        }

        private void DoAsyncCallback(IAsyncResult result)
        {
            try
            {
                this.Do.EndInvoke(result);
            }
            catch
            {
                this._bTimeout = true;
            }
            finally
            {
                this._TimeoutObject.Set();
            }
        }
    }
}
