// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010, 2012-2013 Mirco Bauer <meebey@meebey.net>
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
using System.Linq;
using SysDiag = System.Diagnostics;
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

            var handled = false;
            if (cmd.IsCommand) {
                switch (cmd.Command) {
                    case "exec":
                        CommandExec(cmd);
                        handled = true;
                        break;
                }
            }
            if (handled) {
                // no need to send the command to the engine
                return;
            }

            DateTime start, stop;
            start = DateTime.UtcNow;

            handled = f_Session.Command(cmd);
            if (!handled) {
                IProtocolManager pm;
                if (cmd.Chat is SessionChatModel) {
                    pm = cmd.FrontendManager.CurrentProtocolManager;
                } else {
                    pm = cmd.Chat.ProtocolManager;
                }

                // we maybe have no network manager yet
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

        private void CommandExec(CommandModel cmd)
        {
            Trace.Call(cmd);

            if (cmd.DataArray.Length < 2) {
                NotEnoughParameters(cmd);
                return;
            }
            var parameter = cmd.Parameter;
            var parameters = cmd.Parameter.Split(' ');
            var messageOutput = false;
            var executeOutput = false;
            if (parameters.Length > 0) {
                var shift = false;
                switch (parameters[0]) {
                    case "-c":
                        executeOutput = true;
                        shift = true;
                        break;
                    case "-o":
                        messageOutput = true;
                        shift = true;
                        break;
                }
                if (shift) {
                    parameters = parameters.Skip(1).ToArray();
                    parameter = String.Join(" ", parameters);
                }
            }
            SysDiag.DataReceivedEventHandler handler = (sender, e) => {
                if (String.IsNullOrEmpty(e.Data)) {
                  return;
                }
                // eat trailing newlines
                var output = e.Data.TrimEnd('\r', '\n');
                if (executeOutput || messageOutput) {
                    if (messageOutput && output.StartsWith(cmd.CommandCharacter)) {
                        // escape command character
                        output = String.Format("{0}{1}",
                                               cmd.CommandCharacter, output);
                    }
                    Execute(new CommandModel(cmd.FrontendManager,
                                             cmd.Chat,
                                             cmd.CommandCharacter, output));
                } else {
                    var msg = new MessageBuilder().AppendText(output).ToMessage();
                    cmd.FrontendManager.AddMessageToChat(cmd.Chat, msg);
                }
            };

            string file;
            string args = null;
            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                file = "sh";
                args = String.Format("-c '{0}'",
                                     parameter.Replace("'", @"\'"));
            } else {
                file = parameters[1];
                if (parameters.Length > 1) {
                    args = String.Join(" ", parameters.Skip(1).ToArray());
                }
            }
            var info = new SysDiag.ProcessStartInfo() {
                FileName = file,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using (var process = new SysDiag.Process()) {
                process.StartInfo = info;
                process.OutputDataReceived += handler;
                process.ErrorDataReceived += handler;

                try {
                    process.Start();
                    process.StandardInput.Close();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error(ex);
#endif
                    var command = info.FileName;
                    if (!String.IsNullOrEmpty(info.Arguments)) {
                        command += " " + info.Arguments;
                    }
                    var msg = new MessageBuilder().
                        AppendErrorText("Executing '{0}' failed with: {1}",
                                        command, ex.Message).
                        ToMessage();
                    cmd.FrontendManager.AddMessageToChat(cmd.Chat, msg);
                }
            }
        }

        private void Unknown(CommandModel cmd)
        {
            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                AppendText(_("Unknown Command: {0}"), cmd.Command).
                ToMessage();
            cmd.FrontendManager.AddMessageToChat(cmd.Chat, msg);
        }

        void NotEnoughParameters(CommandModel cmd)
        {
            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                AppendText(_("Not enough parameters for {0} command"), cmd.Command).
                ToMessage();
            cmd.FrontendManager.AddMessageToChat(cmd.Chat, msg);
        }

        MessageBuilder CreateMessageBuilder()
        {
            return new MessageBuilder();
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
