// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010, 2012-2014 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
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
        static string FrontendVersion { get; set; }
        Session         f_Session;
        TaskQueue       f_TaskQueue;
        TimeSpan        f_LastCommandTimeSpan;

        public Version EngineVersion { get; set; }

        public TimeSpan LastCommandTimeSpan {
            get {
                return f_LastCommandTimeSpan;
            }
        }

        public event CommandExceptionEventHandler ExceptionEvent;

        static CommandManager()
        {
            var asm = Assembly.GetAssembly(typeof(CommandManager));
            var asm_name = asm.GetName(false);
            FrontendVersion = asm_name.Version.ToString();
        }

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
                try {
                    DoExecute(cmd);
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error("Execute(): DoExecute() threw exception!", ex);
#endif
                    var msg = CreateMessageBuilder().
                        AppendErrorText("Command '{0}' failed. Reason: {1}",
                                        cmd.Command, ex.Message).
                        ToMessage();
                    AddMessageToFrontend(cmd, msg);
                }
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
                    case "echo":
                        CommandEcho(cmd);
                        handled = true;
                        break;
                    case "benchmark_message_builder":
                        CommandBenchmarkMessageBuilder(cmd);
                        handled = true;
                        break;
                    case "exception":
                        throw new Exception("You asked for it.");
                }
            }
            if (handled) {
                // no need to send the command to the engine
                return;
            }

            DateTime start, stop;
            start = DateTime.UtcNow;

            handled = f_Session.Command(cmd);
            IProtocolManager pm = null;
            if (!handled) {
                if (cmd.Chat is SessionChatModel && cmd.FrontendManager != null) {
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
                var filteredCmd = IOSecurity.GetFilteredPath(cmd.Command);
                var hooks = new HookRunner("frontend", "command-manager",
                                           "command-" + filteredCmd);
                hooks.EnvironmentVariables.Add("FRONTEND_VERSION", FrontendVersion);
                hooks.Environments.Add(new CommandHookEnvironment(cmd));
                hooks.Environments.Add(new ChatHookEnvironment(cmd.Chat));
                if (pm != null) {
                    hooks.Environments.Add(new ProtocolManagerHookEnvironment(pm));
                }

                var cmdChar = (string) f_Session.UserConfig["Interface/Entry/CommandCharacter"];
                hooks.Commands.Add(new SessionHookCommand(f_Session, cmd.Chat, cmdChar));
                if (pm != null) {
                    hooks.Commands.Add(new ProtocolManagerHookCommand(pm, cmd.Chat, cmdChar));
                }

                // show time
                hooks.Init();
                if (hooks.HasHooks) {
                    hooks.Run();
                    handled = true;
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
                    DoExecute(new CommandModel(cmd.FrontendManager,
                                               cmd.Chat,
                                               cmd.CommandCharacter, output));
                } else {
                    var msg = CreateMessageBuilder().AppendText(output).ToMessage();
                    AddMessageToFrontend(cmd, msg);
                }
            };

            string file;
            string args = null;
            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                file = "sh";
                args = String.Format("-c \"{0}\"",
                                     parameter.Replace("\"", @"\"""));
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
                    var msg = CreateMessageBuilder().
                        AppendErrorText("Executing '{0}' failed with: {1}",
                                        command, ex.Message).
                        ToMessage();
                    AddMessageToFrontend(cmd, msg);
                }
            }
        }

        private void CommandEcho(CommandModel cmd)
        {
            Trace.Call(cmd);

            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                    AppendText(cmd.Parameter).
                    ToMessage();
            AddMessageToFrontend(cmd, msg);
        }

        public void CommandGenerateMessages(CommandModel cmd, IChatView chat)
        {
            Trace.Call(cmd, chat);

            var count = 0;
            Int32.TryParse(cmd.Parameter, out count);

            var builder = CreateMessageBuilder();
            var sender = new ContactModel("msg-tester", "msg-tester", "test", "test");
            builder.AppendMessage(sender, "time for a messsage generator command so I can test speed and memory usage");
            var text = builder.CreateText(" *formatted text* ");
            text.Bold = true;
            builder.Append(text);
            builder.AppendUrl("https://www.smuxi.org/");

            var msgs = new List<MessageModel>(count);
            for (var i = 0; i < count; i++) {
                var msg = builder.ToMessage();
                msgs.Add(msg);
            }

            DateTime start, stop;
            start = DateTime.UtcNow;
            foreach (var msg in msgs) {
                chat.AddMessage(msg);
            }
            stop = DateTime.UtcNow;

            builder = CreateMessageBuilder();
            builder.AppendText(
                "IChatView.AddMessage(): count: {0} took: {1:0} ms avg: {2:0.00} ms",
                count,
                (stop - start).TotalMilliseconds,
                (stop - start).TotalMilliseconds / count
            );
            chat.AddMessage(builder.ToMessage());
        }

        public void CommandBenchmarkMessageBuilder(CommandModel cmd)
        {
            Trace.Call(cmd);

            var count = 1000;
            var showHelp = false;
            var appendMessage = false;
            var appendText = false;
            var appendEvent = false;
            var appendFormat = false;
            var toMessage = false;
            try {
                var opts = new NDesk.Options.OptionSet() {
                    { "c|count=", v => count = Int32.Parse(v) },
                    { "m|append-message", v => appendMessage = true },
                    { "t|append-text", v => appendText = true },
                    { "e|append-event", v => appendEvent = true },
                    { "f|append-format", v => appendFormat = true },
                    { "T|to-message", v => toMessage = true },
                };
                opts.Add("h|?|help", x => {
                    showHelp = true;
                    var writer = new StringWriter();
                    opts.WriteOptionDescriptions(writer);
                    AddMessageToFrontend(
                        cmd,
                        CreateMessageBuilder().
                            AppendHeader("{0} usage", cmd.Command).
                            AppendText("\n").
                            AppendText("Parameters:\n").
                            AppendText(writer.ToString()).
                            ToMessage()
                    );
                    return;
                });
                opts.Parse(cmd.Parameter.Split(' '));
                if (showHelp) {
                    return;
                }
            } catch (Exception ex) {
                AddMessageToFrontend(
                    cmd,
                    CreateMessageBuilder().
                        AppendErrorText("Invalid parameter: {0}", ex.Message).
                        ToMessage()
                );
                return;
            }

            DateTime start, stop;
            start = DateTime.UtcNow;
            MessageBuilder builder;
            for (var i = 0; i < count; i++) {
                builder = CreateMessageBuilder();
                if (appendMessage) {
                    builder.AppendMessage("This is message with a link to https://www.smuxi.org/.");
                }
                if (appendText) {
                    builder.AppendText("This is message with just text.");
                }
                if (appendEvent) {
                    builder.AppendEventPrefix();
                }
                if (appendFormat) {
                    builder.AppendFormat("{0} [{1}] has joined {2}",
                                         "meebey3",
                                         "~smuxi@31-18-115-252-dynip.superkabel.de",
                                         "#smuxi-devel");
                }
                if (toMessage) {
                    var msg = builder.ToMessage();
                }
            }
            stop = DateTime.UtcNow;

            builder = CreateMessageBuilder();
            builder.AppendText("MessageBuilder().");
            if (appendMessage) {
                builder.AppendText("AppendMessage().");
            }
            if (appendText) {
                builder.AppendText("AppendText().");
            }
            if (appendEvent) {
                builder.AppendText("AppendEventPrefix().");
            }
            if (appendFormat) {
                builder.AppendText("AppendFormat().");
            }
            if (toMessage) {
                builder.AppendText("ToMessage()");
            }
            builder.AppendText(
                " count: {1} took: {2:0} ms avg: {3:0.00} ms",
                cmd.Data,
                count,
                (stop - start).TotalMilliseconds,
                (stop - start).TotalMilliseconds / count
            );
            AddMessageToFrontend(cmd, builder.ToMessage());
        }

        private void Unknown(CommandModel cmd)
        {
            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                AppendText(_("Unknown Command: {0}"), cmd.Command).
                ToMessage();
            AddMessageToFrontend(cmd, msg);
        }

        void NotEnoughParameters(CommandModel cmd)
        {
            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                AppendText(_("Not enough parameters for {0} command"), cmd.Command).
                ToMessage();
            AddMessageToFrontend(cmd, msg);
        }

        MessageBuilder CreateMessageBuilder()
        {
            return new MessageBuilder();
        }

        void AddMessageToFrontend(CommandModel cmd, MessageModel msg)
        {
            if (cmd == null) {
                throw new ArgumentNullException("cmd");
            }
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            if (EngineVersion != null && EngineVersion >= new Version(0, 10)) {
                f_Session.AddMessageToFrontend(cmd, msg);
            } else {
                f_Session.AddMessageToChat(cmd.Chat, msg);
            }
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
