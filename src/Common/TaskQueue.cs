/*
 * $Id: FrontendManager.cs 378 2008-08-24 00:26:35Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/FrontendManager.cs $
 * $Rev: 378 $
 * $Author: meebey $
 * $Date: 2008-08-24 02:26:35 +0200 (Sun, 24 Aug 2008) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008 Alan McGovern <alan.mcgovern@gmail.com>
 * Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

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
            tasks.Clear();
        }
        
        public void Reset()
        {
            Reset(false);
        }
    }
}
