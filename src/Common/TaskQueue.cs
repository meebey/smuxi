// This file is part of Smuxi and is licensed under the terms of MIT/X11
//
// Copyright (c) 2008 Alan McGovern <alan.mcgovern@gmail.com>
// Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Smuxi.Common
{
    public delegate object TaskQueueJobHandler();
    public delegate void   TaskQueueTaskHandler();
    public delegate bool   TaskQueueTimeoutHandler();
    
    public delegate void   TaskQueueExceptionEventHandler(object sender, TaskQueueExceptionEventArgs e);
    
    public class TaskQueueExceptionEventArgs : EventArgs
    {
        Exception f_Exception;
        
        public Exception Exception {
            get {
                return f_Exception;
            }
        }
        
        public TaskQueueExceptionEventArgs(Exception ex)
        {
            f_Exception = ex;
        }
    }
                                                          
    public class TaskQueue : IDisposable
    {
        private class DelegateTask
        {
            private ManualResetEvent handle;
            private object result;
            private TaskQueueJobHandler task;
            private Exception exception;

            public ManualResetEvent Handle {
                get {
                    return handle;
                }
                set {
                    handle = value;
                }
            }
            
            public object Result
            {
                get { return result; }
            }
            
            public Exception Exception {
                get {
                    return exception;
                }
            }
            
            public DelegateTask(TaskQueueJobHandler task)
            {
                this.task = task;
            }

            public void Execute()
            {
                try {
                    result = task();
                } catch (Exception ex) {
                    exception = ex;
                }
                
                if (handle != null)
                    handle.Set();
            }
        }

        bool disposed;
        AutoResetEvent handle = new AutoResetEvent(false);
        Queue<DelegateTask> tasks = new Queue<DelegateTask>();
        Thread thread;
        string name;
        
        public TaskQueueExceptionEventHandler ExceptionEvent;
        public EventHandler                   AbortedEvent;

        public bool Disposed
        {
            get { return disposed; }
        }

        public TaskQueue(string name)
        {
            this.name = name;
            InitThread();
        }

        ~TaskQueue()
        {
            Dispose(false);
        }
        
        void InitThread()
        {
            thread = new Thread(new ThreadStart(Loop));
            thread.Name = name;
            thread.IsBackground = true;
            thread.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;
            
            disposed = true;

            // make sure the thread will notice the disposed state when
            // disposing or finalizing this object
            handle.Set();
        }
        
        void Loop()
        {
            while (true)
            {
                DelegateTask task = null;
                
                lock (tasks)
                {
                    if (tasks.Count > 0)
                        task = tasks.Dequeue();
                }

                if (task == null)
                {
                    if (disposed)
                        break;

                    #if DISABLED
                    // WARNING: if we are being disposed at _this_ point, there
                    // are no new tasks added and we are waiting forever, then
                    // we would leak this thread! The dispose check + break will
                    // never happen, so we have to use a sane timeout here.
                    // Checking every 60 seconds if we are disposed should be
                    // reasonable.
                    handle.WaitOne(60 * 1000);
                    #endif
                    // OPT: let Dispose() trigger the handle, but is this safe?
                    handle.WaitOne();
                }
                else
                {
                    task.Execute();
                    
                    if (task.Exception != null) {
                        if (ExceptionEvent != null) {
                            ExceptionEvent(this, new TaskQueueExceptionEventArgs(task.Exception));
                        }
                        break;
                    }
                }
            }
            
            if (AbortedEvent != null) {
                AbortedEvent(this, EventArgs.Empty);
            }
        }

        private void Queue(DelegateTask task)
        {
            lock (tasks)
            {
                tasks.Enqueue(task);
                handle.Set();
            }
        }

        public void Queue(TaskQueueTaskHandler task)
        {
            Queue(new DelegateTask(delegate { 
                task();
                return null;
            }));
        }

        public void QueueWait(TaskQueueTaskHandler task)
        {
            QueueWait(delegate {
                task();
                return null;
            });
        }

        public object QueueWait(TaskQueueJobHandler task)
        {
            return QueueWait(new DelegateTask(task));
        }

        private object QueueWait(DelegateTask t)
        {
            if (t.Handle != null)
                t.Handle.Reset();
            else
                t.Handle = new ManualResetEvent(false);
            
            if (Thread.CurrentThread == thread)
                t.Execute();
            else
                Queue(t);

            t.Handle.WaitOne();
            t.Handle.Close();

            return t.Result;
        }
        
        public void Reset(bool abortActiveTask)
        {
            if (abortActiveTask) {
                thread.Abort();
                InitThread();
            }
            lock (tasks) {
                tasks.Clear();
            }
        }
        
        public void Reset()
        {
            Reset(false);
        }
    }
}
