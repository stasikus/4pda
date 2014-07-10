using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace WhitePrideWorldWide 
{
    abstract class BaseHttp
    {
      
        protected Thread workThread ;
        protected int howmuch;
        
        public bool UseProxy = false;
        public bool Ready ;
        public readonly int Ident ;

        protected string errorMsg;
        protected bool bError;
        protected bool terminated;

        public string ErrorMsg
        {
            get
            {
                return errorMsg;
            }
        }

        public bool WasErrors
        {
            get
            {
                return bError;
            }
        }
        
       protected WhitePrideWorldWide.HttpClient httpClient = new WhitePrideWorldWide.HttpClient();

       #region Need To be realized in derrived classes!
       protected abstract void Execute ();
     #endregion


        #region ClassEvents
        public class LogEventArgs : EventArgs
        {
            public readonly string what;
            public readonly int fromId;
            public LogEventArgs(string w, int from)
            {
                what = w;
                fromId = from;
            }
        }

        public delegate void LogEventHandler(object sender, LogEventArgs e);
        public event LogEventHandler LogEvent;
        protected virtual void OnLog(object sender, LogEventArgs e)
        {
            if (LogEvent != null)
            {
                LogEvent(sender, e);
            }
        }

        protected void do_Log(string what)
        {
            OnLog(this, new LogEventArgs(what, Ident));
        }

        public class DebugEventArgs : EventArgs
        {
            public readonly string what;
            public readonly string where;
            public readonly DateTime when;
            public readonly int sender;
            public DebugEventArgs(DateTime t1, string t2, string t3, int from)
            {
                when = t1;
                where = t2;
                what = t3;
                this.sender = from;
            }                    
        }

        public delegate void DebugEventHandler(object sender, DebugEventArgs e);
        public event DebugEventHandler DebugEvent;
        protected virtual void OnDebug(object sender, DebugEventArgs e)
        {
            if (DebugEvent != null)
            {
                DebugEvent(sender, e);
            }
        }

        protected void do_Debug(string what, string where, int from)
        {
            OnDebug(this, new DebugEventArgs(DateTime.Now, where, what, from));
        }

        public class ProgressEventArgs : EventArgs
        {
            public readonly int current;
            public readonly int all;
            public readonly string somedata;

            public ProgressEventArgs (int cur , int all , string some)
            {
                current = cur;
                this.all = all;
                somedata = some;
            }
        }
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        public event ProgressEventHandler ProgressEvent;
        protected virtual void OnProgress(object sender, ProgressEventArgs e)
        {
            if (ProgressEvent != null)
            {
                ProgressEvent(sender, e);
            }
          
        }

        protected virtual void do_Progress (int cur, int all , string some )
        {
            OnProgress (this, new ProgressEventArgs(cur, all, some));
        }

        public event EventHandler Finish;
        protected virtual void OnFinish(object sender, EventArgs e)
        {
            if (Finish != null)
            {
                Finish(this, e);
            }
        }

        protected virtual void do_Finish(object sender)
        {
              OnFinish(this, null);
        } 
#endregion 

        public BaseHttp ( int number , int howmuch )
        {
            
            this.howmuch = howmuch;
            workThread = null;
            httpClient.UseCookies = true;
            this.Ident = number;
        }


#region WorkThreadMethods

        public void Abort ()
        {
            try
            {
                workThread.Abort();
            }
            finally
            {
               workThread = null;
            }
        }

        public void Terminate()
        {
            lock (this)
            {
                terminated = true;
            }
        }

        public void Suspend()
        {
            if (workThread != null) workThread.Suspend(); 
        }

        public void Resume()
        {
            if (workThread != null) workThread.Resume();
        }

        public void WaitTillEnd()
        {
            workThread.Join();
        }
        
        public virtual  void Start()
        {

            Ready = false;
            terminated = false;
            if (workThread != null)
            {
                Abort ();
            }
            ThreadStart start = new ThreadStart(Execute);
            workThread = new Thread(start);
            workThread.Priority = ThreadPriority.Normal;
            workThread.Start();
        }
#endregion
    }
    
}
