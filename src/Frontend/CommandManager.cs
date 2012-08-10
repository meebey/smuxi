// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
// 
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

using System;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend
{
    public delegate void CommandExceptionEventHandler(object sender,
                                                      CommandExceptionEventArgs e);

    public class CommandExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; private set;}

        public CommandExceptionEventArgs(Exception ex)
        {
            Exception = ex;
        }
    }

    public class CommandManager : IDisposable
    {
#if LOG4NET
        static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        static readonly string f_LibraryTextDomain = "smuxi-frontend";
        Session         f_Session;
        TaskQueue       f_TaskQueue;
        TimeSpan        f_LastCommandTimeSpan;

        public TimeSpan LastCommandTimeSpan {
            get {
                return f_LastCommandTimeSpan;
            }
        }

        public event CommandExceptionEventHandler ExceptionEvent;

        public CommandManager(Session session)
        {
            if (session == null) {
                throw new ArgumentNullException("session");
            }

            f_Session = session;

            f_TaskQueue = new TaskQueue("CommandManager");
            f_TaskQueue.ExceptionEvent += OnTaskQueueExceptionEvent;
            f_TaskQueue.AbortedEvent   += OnTaskQueueAbortedEvent;
        }

        ~CommandManager()
        {
            Trace.Call();

            Dispose(false);
        }

        public void Dispose()
        {
            Trace.Call();

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            Trace.Call(disposing);

            if (disposing) {
                f_TaskQueue.Dispose();
            }
        }

        public void Execute(CommandModel cmd)
        {
            Trace.Call(cmd);

            if (cmd == null) {
                throw new ArgumentNullException("cmd");
            }

            f_TaskQueue.Queue(delegate {
                DoExecute(cmd);
            });
        }

        private void DoExecute(CommandModel cmd)
        {
            Trace.Call(cmd);

            DateTime start, stop;
            start = DateTime.UtcNow;

            bool handled;
            handled = f_Session.Command(cmd);
            if (!handled) {
                // we maybe have no network manager yet
                IProtocolManager pm = cmd.Chat.ProtocolManager;
                if (pm != null) {
                    handled = pm.Command(cmd);
                } else {
                    handled = false;
                }
            }
            if (!handled) {
               Unknown(cmd);
            }

            stop = DateTime.UtcNow;
            f_LastCommandTimeSpan = (stop - start);
        }

        private void Unknown(CommandModel cmd)
        {
            var msg = new MessageBuilder().
                AppendEventPrefix().
                AppendText(_("Unknown Command: {0}"), cmd.Command).
                ToMessage();
            cmd.FrontendManager.AddMessageToChat(cmd.Chat, msg);
        }

        protected virtual void OnTaskQueueExceptionEvent(object sender, TaskQueueExceptionEventArgs e)
        {
            Trace.Call(sender, e);

#if LOG4NET
            f_Logger.Error("Exception in TaskQueue: ", e.Exception);
            f_Logger.Error("Inner-Exception: ", e.Exception.InnerException);
#endif
            if (ExceptionEvent != null) {
                ExceptionEvent(this, new CommandExceptionEventArgs(e.Exception));
            }
        }

        protected virtual void OnTaskQueueAbortedEvent(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

#if LOG4NET
            f_Logger.Debug("OnTaskQueueAbortedEvent(): task queue aborted!");
#endif
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }
    }
}
