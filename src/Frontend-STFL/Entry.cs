/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007, 2010-2013 Mirco Bauer <meebey@meebey.net>
 * Copyright (c) 2011 Andrius Bentkus <andrius.bentkus@gmail.com>
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
using System.IO;
using System.Reflection;
using Mono.Unix;
using Stfl;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Stfl
{
    public class Entry
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        MainWindow      f_MainWindow;
        ChatViewManager f_ChatViewManager;
        CommandManager CommandManager { get; set; }
        NickCompleter NickCompleter { get; set; }
        EntrySettings Settings { get; set; }

        event EventHandler Activated;

        public string Text {
            get {
                return f_MainWindow["input_text"];
            }
            set {
                f_MainWindow["input_text"] = value;
            }
        }

        public int Position {
            get {
                return Int32.Parse(f_MainWindow["input_pos"]);
            }
            set {
                f_MainWindow["input_pos"] = value.ToString();
            }
        }
        
        public Entry(MainWindow mainWindow, ChatViewManager chatViewManager)
        {
            if (mainWindow == null) {
                throw new ArgumentNullException("mainWindow");
            }
            if (chatViewManager == null) {
                throw new ArgumentNullException("chatViewManager");
            }

            f_MainWindow = mainWindow;
            f_MainWindow.KeyPressed += OnKeyPressed;
            
            f_ChatViewManager = chatViewManager;
            f_ChatViewManager.CurrentChatSwitched += OnChatSwitched;

            Frontend.SessionPropertyChanged += delegate {
                InitCommandManager();
            };

            Settings = new EntrySettings();
            NickCompleter = new TabCycleNickCompleter();
        }
        
        public virtual void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);

            if (config == null) {
                throw new ArgumentNullException("config");
            }

            Settings.ApplyConfig(config);

            // replace nick completer if needed
            if (Settings.BashStyleCompletion && !(NickCompleter is LongestPrefixNickCompleter)) {
                NickCompleter = new LongestPrefixNickCompleter();
            } else if (!Settings.BashStyleCompletion && !(NickCompleter is TabCycleNickCompleter)) {
                NickCompleter = new TabCycleNickCompleter();
            }

            // set the completion character
            NickCompleter.CompletionChar = Settings.CompletionCharacter;
        }

        void InitCommandManager()
        {
            Trace.Call();

            if (CommandManager != null) {
                CommandManager.Dispose();
            }

            if (Frontend.Session == null) {
                CommandManager = null;
            } else {
                CommandManager = new CommandManager(Frontend.Session);
            }
        }

        private void OnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            Trace.Call(sender, e);
            
#if LOG4NET
            _Logger.Debug("_OnKeyPressed(): e.Key: '" + e.Key + "' e.Focus: '" + e.Focus + "'");
#endif
            switch (e.Key) {
                case "ENTER":
                    OnActivated(EventArgs.Empty);
                    break;
                case "PPAGE":
                    if (f_ChatViewManager.ActiveChat != null) {
                        f_ChatViewManager.ActiveChat.ScrollUp();
                    }
                    break;
                case "NPAGE":
                    if (f_ChatViewManager.ActiveChat != null) {
                        f_ChatViewManager.ActiveChat.ScrollDown();
                    }
                    break;
                case "TAB":
                    CompleteNick();
                    break;
                case "kPRV5": // CTRL + PAGE UP
                case "^P":
                    f_ChatViewManager.CurrentChatNumber--;
                    break;
                case "kNXT5": // CTRL + PAGE DOWN
                case "^N":
                    f_ChatViewManager.CurrentChatNumber++;
                    break;
                case "^W":
                    DeleteUntilSpace();
                    break;
                case "kRIT5":
                    JumpWord(false);
                    break;
                case "kLFT5":
                    JumpWord(true);
                    break;
                case "^D":
                    DeleteChar();
                    break;
            }
        }

        private void OnChatSwitched(object sender, ChatSwitchedEventArgs e)
        {
            Trace.Call(sender, e);

            f_MainWindow.InputLabel = String.Format("[{0}]",
                                                    e.ChatView.ChatModel.Name);
        }

        public virtual void OnActivated(EventArgs e)
        {
            var text = Text;
            if (String.IsNullOrEmpty(text)) {
                return;
            }

            ExecuteCommand(text);
            
            if (Activated != null) {
                Activated(this, EventArgs.Empty);
            }

            Text = String.Empty;
        }
        
        public void ExecuteCommand(string cmd)
        {
            if (cmd == null) {
                throw new ArgumentNullException("cmd");
            }

            ChatModel chat = null;
            var currentChat = f_ChatViewManager.CurrentChat;
            if (currentChat != null) {
                chat = currentChat.ChatModel;
            }
            CommandModel cd = new CommandModel(
                Frontend.FrontendManager,
                chat,
                (string) Frontend.UserConfig["Interface/Entry/CommandCharacter"],
                cmd
            );

            var handled = Command(cd);
            if (handled) {
                return;
            }

            CommandManager.Execute(cd);
        }

        private bool Command(CommandModel cmd)
        {
            bool handled = false;
            if (cmd.IsCommand) {
                switch (cmd.Command.ToLower()) {
                    case "help":
                        CommandHelp(cmd);
                        break;
                    case "window":
                        CommandWindow(cmd);
                        handled = true;
                        break;
                    case "exit":
                        Frontend.Quit();
                        handled = true;
                        break;
                    case "gc":
#if LOG4NET
                        _Logger.Debug("GC.Collect()");
#endif
                        var msg = new MessageBuilder().
                            AppendEventPrefix().
                            AppendText("GCing...").
                            ToMessage();
                        cmd.FrontendManager.AddMessageToChat(cmd.Chat, msg);
                        GC.Collect();
                        handled = true;
                        break;
                }
            }
            return handled;
        }

        void CommandHelp(CommandModel cmd)
        {
            var chatView = f_MainWindow.ChatViewManager.GetChat(cmd.Chat);
            var builder = new MessageBuilder();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            builder.AppendHeader(_("Frontend Commands"));
            chatView.AddMessage(builder.ToMessage());

            string[] help = {
                "help",
                "window (number|close)",
                "exit",
            };

            foreach (string line in help) {
                builder = new MessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                chatView.AddMessage(builder.ToMessage());
            }
        }

        private void CommandWindow(CommandModel cmd)
        {
            if (cmd.Parameter == "close") {
                f_ChatViewManager.CurrentChat.Close();
                return;
            }

            int window;
            if (!Int32.TryParse(cmd.Parameter, out window)) {
                return;
            }
            ChatView chat = f_ChatViewManager.GetChat(window - 1);
            if (chat == null) {
                return;
            }
            f_ChatViewManager.CurrentChat = chat;
        }

        private void CommandUnknown(CommandModel cmd)
        {
            var msg = new MessageBuilder().
                AppendEventPrefix().
                AppendText(_("Unknown Command: {0}"), cmd.Command).
                ToMessage();
            cmd.FrontendManager.AddMessageToChat(cmd.Chat, msg);
        }

        // gets the position of the first space left
        private int GetLeftSpace(int end)
        {
            // we are already at the very beginning
            if (end == 0) {
                return 0;
            }

            int start;

            // are the first characters spaces?
            bool firstSpace = true;

            for (start = end; start > 0; start--) {
                if (start >= Text.Length) {
                    continue;
                } else if (Text[start] == ' ') {
                    if (firstSpace) {
                        continue;
                    } else {
                        start++; // don't cut the last char
                        break;
                    }
                } else {
                    firstSpace = false;
                }
            }

            return start;
        }

        private int GetRightSpace(int start)
        {
            bool firstSpace = true;

            int end;
            for (end = start; end < Text.Length; end++) {
                if (Text[end] == ' ') {
                    if (firstSpace) {
                        continue;
                    } else {
                        break;
                    }
                } else {
                    firstSpace = false;
                }
            }

            return end;
        }

        private void DeleteUntilSpace()
        {
            int end = Position;

            // nothing to delete, if we are at the very beginning
            if (end == 0) {
                return;
            }

            int start = GetLeftSpace(end);

            Text = Text.Substring(0, start) + Text.Substring(end);

            Position = start;
        }

        private void JumpWord(bool left)
        {
            if (left) {
                int pos = GetLeftSpace(Position);
                if (pos > 0) {
                    pos--;
                }
                Position = pos;
            } else {
                Position = GetRightSpace(Position);
            }
        }

        private void DeleteChar()
        {
            Text = Text.Substring(0, Position) +
                   Text.Substring(Math.Min(Position + 1, Text.Length));
        }

        void CompleteNick()
        {
            // perform completion
            string text = Text;
            int position = Position;
            NickCompleter.Complete(ref text, ref position, f_ChatViewManager.CurrentChat);
            Text = text;
            Position = position;
        }

        static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
