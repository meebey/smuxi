/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2008 Mirco Bauer <meebey@meebey.net>
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Mono.Unix;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    public class Entry : Gtk.Entry
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private StringCollection _History = new StringCollection();
        private int              _HistoryPosition;
        private bool             _HistoryChangedLine;
        private CommandManager   _CommandManager;
        private new EntrySettings Settings { get; set; }

        ChatViewManager ChatViewManager;

        /*
        public StringCollection History {
            get {
                return _History;
            }
        }
        */

        /*
        public int HistoryPosition {
            get {
                return _HistoryPosition;
            }
            set {
                _HistoryPosition = value;
            }
        }
        */
        
        /*
        public bool HistoryChangedLine {
            get {
                return _HistoryChangedLine;
            }
            set {
                _HistoryChangedLine = value;
            }
        }
        */

        public Entry(ChatViewManager chatViewManager)
        {
            Trace.Call(chatViewManager);

            if (chatViewManager == null) {
                throw new ArgumentNullException("chatViewManager");
            }

            _History.Add(String.Empty);
            
            ChatViewManager = chatViewManager;
            Settings = new EntrySettings();

            InitCommandManager();
            Frontend.SessionPropertyChanged += delegate {
                InitCommandManager();
            };

            Activated += new EventHandler(_OnActivated);
            KeyPressEvent += new Gtk.KeyPressEventHandler(_OnKeyPress);
            FocusOutEvent += new Gtk.FocusOutEventHandler(_OnFocusOut);
            ClipboardPasted += new EventHandler(_OnClipboardPasted);
        }

        public void UpdateHistoryChangedLine()
        {
            if ((_History.Count > 0) &&
                (Text.Length > 0) &&
                (Text != HistoryCurrent())) {
                // the entry changed and the entry is not empty
                _HistoryChangedLine = true;
#if LOG4NET
                //_Logger.Debug("_HistoryChangedLine = true");
#endif
            } else {
                _HistoryChangedLine = false;
#if LOG4NET
                //_Logger.Debug("_HistoryChangedLine = false");
#endif
            }
        }

        public void AddToHistory(string data, int positiondiff)
        {
            /*
            // BUG: this code doesnt work well
            // _History.Count-1 is the last entry, which should be always empty
            if ((_History.Count > 1) &&
                (data == _History[_History.Count-2])) {
                // don't add the same value
                return;
            }
            */

            _History.Insert(_History.Count-1, data);
#if LOG4NET
             _Logger.Debug("added: '"+data+"' to history");
#endif

            if (_History.Count > Settings.CommandHistorySize) {
                _History.RemoveAt(0);
            } else {
                _HistoryPosition += positiondiff;
            }
        }

        public string HistoryCurrent()
        {
            return _History[_HistoryPosition];
        }

        public void HistoryPrevious()
        {
            if (_HistoryChangedLine) {
#if LOG4NET
                _Logger.Debug("entry changed, adding to history");
#endif
                AddToHistory(Text, 0);
                _HistoryChangedLine = false;
            }

            if (_HistoryPosition > 0) {
#if LOG4NET
                _Logger.Debug("showing previous item");
#endif
                _HistoryPosition--;
                Text = HistoryCurrent();
                Position = -1;
            }
        }

        public void HistoryNext()
        {
            if (_HistoryChangedLine) {
#if LOG4NET
                _Logger.Debug("entry changed, adding to history");
#endif
                AddToHistory(Text, 0);
                _HistoryChangedLine = false;
            }

            if (_HistoryPosition < _History.Count-1) {
#if LOG4NET
                _Logger.Debug("showing next item");
#endif
                _HistoryPosition++;
                Text = HistoryCurrent();
                Position = -1;
            } else if (Text.Length > 0) {
#if LOG4NET
                _Logger.Debug("not empty line, lets add one");
#endif
                // last position and we went further down
                _History.Add(String.Empty);
                _HistoryPosition++;
                Text = String.Empty;
            }
        }

        [GLib.ConnectBefore]
        private void _OnKeyPress(object sender, Gtk.KeyPressEventArgs e)
        {
            // too much logging noise
            //Trace.Call(sender, e);

#if LOG4NET
            // too much logging noise
            /*
            _Logger.Debug("_OnKeyPress(): Key: " + e.Event.Key.ToString() +
                          " KeyValue: " + e.Event.KeyValue);
            */
#endif

            try {
                ProcessKey(e);
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void ProcessKey(Gtk.KeyPressEventArgs e)
        {
            int keynumber = (int)e.Event.KeyValue;
            Gdk.Key key = e.Event.Key;
            if ((e.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                // ctrl is pressed
                e.RetVal = true;
                switch (key) {
                    case Gdk.Key.x:
                    case Gdk.Key.X:
                        if (ChatViewManager.CurrentChatView is SessionChatView) {
                            Frontend.FrontendManager.NextProtocolManager();
                        } else {
                            // don't break cut
                            e.RetVal = false;
                        }
                        break;
                    case Gdk.Key.p:
                    case Gdk.Key.P:
                        ChatViewManager.CurrentChatNumber--;
                        break;
                    case Gdk.Key.n:
                    case Gdk.Key.N:
                        ChatViewManager.CurrentChatNumber++;
                        break;
                    case Gdk.Key.Tab:
                    case Gdk.Key.ISO_Left_Tab:
                        if ((e.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                            ChatViewManager.CurrentChatNumber--;
                        } else {
                            ChatViewManager.CurrentChatNumber++;
                        }
                        break;
                    // don't break unicode input
                    case Gdk.Key.U:
                    // don't break copy/paste
                    case Gdk.Key.c:
                    case Gdk.Key.C:
                    case Gdk.Key.v:
                    case Gdk.Key.V:
                    // don't break select all
                    case Gdk.Key.a:
                    case Gdk.Key.A:
                    // don't break jump one word left/right
                    case Gdk.Key.Right:
                    case Gdk.Key.Left:
                    // don't break delete last word
                    case Gdk.Key.BackSpace:
                        e.RetVal = false;
                        break;
                    case Gdk.Key.Home:
                        ChatViewManager.CurrentChatView.ScrollToStart();
                        break;
                    case Gdk.Key.End:
                        ChatViewManager.CurrentChatView.ScrollToEnd();
                        break;
                }
            }
            
            int pagenumber = -1;
            if ((e.Event.State & Gdk.ModifierType.Mod1Mask) != 0) {
                // alt is pressed
                switch (keynumber) {
                    case 49: // 1
                    case 50: // 2
                    case 51: // 3
                    case 52: // 4
                    case 53: // 5
                    case 54: // 6
                    case 55: // 7
                    case 56: // 8
                    case 57: // 9
                        pagenumber = keynumber - 49;
                        break;
                    case 48: // 0
                        pagenumber = 9;
                        break;
                    case 113: // q
                        pagenumber = 10;
                        break;
                    case 119: // w
                        pagenumber = 11;
                        break;
                    case 101: // e
                        pagenumber = 12;
                        break;
                    case 114: // r
                        pagenumber = 13;
                        break;
                    case 116: // t
                        pagenumber = 14;
                        break;
                    case 121: // y
                        pagenumber = 15;
                        break;
                    case 117: // u
                        pagenumber = 16;
                        break;
                    case 105: // i
                        pagenumber = 17;
                        break;
                    case 111: // o
                        pagenumber = 18;
                        break;
                    case 112: // p
                        pagenumber = 19;
                        break;
                }

                if (pagenumber != -1) {
                    ChatViewManager.CurrentChatNumber = pagenumber;
                }
            }

            if ((e.Event.State & Gdk.ModifierType.Mod1Mask) != 0 ||
                (e.Event.State & Gdk.ModifierType.ControlMask) != 0 ||
                (e.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                // alt, ctrl or shift pushed, returning
                return;
            }

            UpdateHistoryChangedLine();
            switch (key) {
                case Gdk.Key.Tab:
                    // don't let GTK handle the focus, as we will do it
                    e.RetVal = true;
                    if (Frontend.MainWindow.CaretMode) {
                        // when we are in caret-mode change focus to output textview
                        ChatViewManager.CurrentChatView.HasFocus = true;
                    } else {
                        if (Text.Length > 0) {
                            _NickCompletion();
                        }
                    }
                    break;
                case Gdk.Key.Up:
                    // supress widget navigation/jumping (like tab)
                    e.RetVal = true;
                    
                    HistoryPrevious();
                    break;
                case Gdk.Key.Down:
                    // supress widget navigation/jumping (like tab)
                    e.RetVal = true;
                    
                    HistoryNext();
                    break;
                case Gdk.Key.Page_Up:
                    ChatViewManager.CurrentChatView.ScrollUp();
                    break;
                case Gdk.Key.Page_Down:
                    ChatViewManager.CurrentChatView.ScrollDown();
                    break;
            }
        }

        private void _OnFocusOut(object sender, Gtk.FocusOutEventArgs e)
        {
            Trace.Call(sender, e);
            
            if (Frontend.MainWindow.CaretMode) {
                return;
            }
            
            // we can't just move to focus directly back as that breaks the
            // notebook scrolling, see trac bug#11
            
            // grant the user 250ms to start a selection
            GLib.Timeout.Add(250, new GLib.TimeoutHandler(delegate {
                // TODO: check mouse buttons, if left mouse button is still pressed
                // we should not interrupt either, as the user is going to make a selection!

                // don't interrupt on-going entry selections
                int start, end;
                if (GetSelectionBounds(out start, out end)) {
#if LOG4NET
                    //_Logger.Debug("_OnFocusOut(): Entry has on-going selection, waiting..."); 
#endif
                    return true;
                }

                ChatView chat = ChatViewManager.CurrentChatView;
                if (chat == null) {
                    return false;
                }

                // don't interrupt on-going selections
                if (chat.HasSelection && chat.HasFocus) {
#if LOG4NET
                    //_Logger.Debug("_OnFocusOut(): CurrentChatView has on-going selection, waiting..."); 
#endif
                    return true;
                }
                
                // HACK: for some reason the selection of the TextView gets
                // lost when moving the focus from the TextView to the entry
                // being non-empty. So we empty it, move focus and then set the
                // old value back. GTK+ bug maybe?
                string text = Text;
                Text = String.Empty;
                HasFocus = true;
                Text = text;
                Position = -1;
                
                return false;
            }));
        }
        
        private void _OnActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                if (!(Text.Length > 0)) {
                    return;
                }
                
                if (Text.IndexOf("\n") != -1) {
                    // seems to be a paste, so let's break it apart
                    string[] msgParts = Text.Split(new char[] {'\n'});
                    if (msgParts.Length > 3) {
                        string msg = String.Format(_("You are going to paste {0} lines. Do you want to continue?"),
                                                   msgParts.Length);
                        Gtk.MessageDialog md = new Gtk.MessageDialog(
                                                    Frontend.MainWindow,
                                                    Gtk.DialogFlags.Modal,
                                                    Gtk.MessageType.Warning,
                                                    Gtk.ButtonsType.YesNo,
                                                    msg);
                        Gtk.ResponseType res = (Gtk.ResponseType)md.Run();
                        md.Destroy();
                        if (res != Gtk.ResponseType.Yes) {
                            Text = String.Empty;
                            return;
                        }
                    }
                    foreach (string msg in msgParts) {
                        ExecuteCommand(msg);
                    }
                } else {
                    ExecuteCommand(Text);
                    AddToHistory(Text, _History.Count - _HistoryPosition);
                    // reset history position to last entry
                    _HistoryPosition = _History.Count - 1;
                }
                Text = String.Empty;
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
                Frontend.ShowException(null, ex);
            }
        }
        
        private void _OnClipboardPasted(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
        }
        
        public void ExecuteCommand(string cmd)
        {
            if (!(cmd.Length > 0)) {
                return;
            }

            CommandModel cd = new CommandModel(
                Frontend.FrontendManager,
                ChatViewManager.CurrentChatView.ChatModel,
                Settings.CommandCharacter,
                cmd
            );

            if (_Command(cd)) {
                return;
            }

            _CommandManager.Execute(cd);
        }
        
        private bool _Command(CommandModel cd)
        {
            bool handled = false;
            
            // command that work even without beeing connected 
            if (cd.IsCommand) {
                switch (cd.Command) {
                    case "help":
                        _CommandHelp(cd);
                        break;
                    case "detach":
                        _CommandDetach(cd);
                        handled = true;
                        break;
                    case "echo":
                        _CommandEcho(cd);
                        handled = true;
                        break;
                    case "exec":
                        _CommandExec(cd);
                        handled = true;
                        break;
                    case "window":
                        _CommandWindow(cd);
                        handled = true;
                        break;
                    case "clear":
                        _CommandClear(cd);
                        handled = true;
                        break;
                    case "list":
                        _CommandList(cd);
                        handled = true;
                        break;
                }
            }
            
            return handled;
        }
        
        private void _CommandHelp(CommandModel cd)
        {
            var chatView = ChatViewManager.GetChat(cd.Chat);
            var builder = new MessageBuilder();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            builder.AppendHeader(_("Frontend Commands"));
            chatView.AddMessage(builder.ToMessage());

            string[] help = {
            "help",
            "window (number|channelname|queryname|close)",
            "clear",
            "echo data",
            "exec command",
            "detach",
            "list [search key]",
            };

            foreach (string line in help) {
                builder = new MessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                chatView.AddMessage(builder.ToMessage());
            }
        }

        private void _CommandList(CommandModel cd)
        {
            Frontend.MainWindow.OpenFindGroupChatWindow(cd.Parameter);
        }

        private void _CommandDetach(CommandModel cd)
        {
            Frontend.Quit();
        }

        private void _CommandEcho(CommandModel cd)
        {
            cd.FrontendManager.AddTextToChat(
                cd.Chat,
                String.Format("-!- {0}", cd.Parameter)
            );
        }
        
        private void _CommandExec(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                string output;
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = cd.DataArray[1];
                if (cd.DataArray.Length >= 3) { 
                    process.StartInfo.Arguments = String.Join(" ", cd.DataArray,
                                                    2, cd.DataArray.Length-2);
                }
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                
                try {
                    process.Start();
                    output = process.StandardOutput.ReadToEnd();
                    cd.FrontendManager.AddTextToChat(cd.Chat, output);
                } catch {
                }
            }
        }
    
        private void _CommandWindow(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                var currentChat = ChatViewManager.CurrentChatView;
                if (cd.Parameter.ToLower() == "close") {
                    currentChat.Close();
                } else {
                    try {
                        int number = Int32.Parse(cd.DataArray[1]);
                        if (number > ChatViewManager.Chats.Count) {
                            return;
                        }
                        ChatViewManager.CurrentChatNumber = number - 1;
                        return;
                    } catch (FormatException) {
                    }
                    
                    // seems to be query- or channelname
                    // let's see if we find something
                    var seachKey = cd.Parameter.ToLower();
                    var candidates = new List<ChatView>();
                    foreach (var chatView in ChatViewManager.Chats) {

                        if (chatView.Name.ToLower() != seachKey) {
                            continue;
                        }
                        // name matches
                        // let's see if there is an exact match, if so, take it
                        if ((chatView.GetType() == currentChat.GetType()) &&
                            (chatView.ProtocolManager == currentChat.ProtocolManager)) {
                            candidates.Add(chatView);
                            break;
                        } else {
                            // there was no exact match
                            candidates.Add(chatView);
                        }
                    }

                    if (candidates.Count == 0) {
                        return;
                    }
                    ChatViewManager.CurrentChatView = candidates[0];
                }
            }
        }
    
        private void _CommandClear(CommandModel cd)
        {
            ChatViewManager.CurrentChatView.Clear();
        }

        private void _NickCompletion()
        {
            // return if we don't support the current ChatView
            if (!(ChatViewManager.CurrentChatView is GroupChatView) &&
                !(ChatViewManager.CurrentChatView is PersonChatView)) {
                return;
            }

            int position = CursorPosition;
            string text = Text;
            string word;
            int previous_space;
            int next_space;

            // find the current word
            string temp;
            temp = text.Substring(0, position);
            previous_space = temp.LastIndexOf(' ');
            next_space = text.IndexOf(' ', position);

#if LOG4NET
            _Logger.Debug("previous_space: "+previous_space);
            _Logger.Debug("next_space: "+next_space);
#endif

            if (previous_space != -1 && next_space != -1) {
                // previous and next space exist
                word = text.Substring(previous_space + 1, next_space - previous_space - 1);
            } else if (previous_space != -1) {
                // previous space exist
                word = text.Substring(previous_space + 1);
            } else if (next_space != -1) {
                // next space exist
                word = text.Substring(0, next_space);
            } else {
                // no spaces
                word = text;
            }

            if (word == String.Empty) {
                return;
            }

            bool leadingAt = false;
            // remove leading @ character
            if (word.StartsWith("@")) {
                word = word.Substring(1);
                leadingAt = true;
            }

            // find the possible nickname
            bool found = false;
            bool partial_found = false;
            string nick = null;

            ChatView view = ChatViewManager.CurrentChatView;
            if (view is GroupChatView) {
                GroupChatView cp = (GroupChatView) view;
                if (Settings.BashStyleCompletion) {
                    IList<string> result = cp.PersonLookupAll(word);
                    if (result == null || result.Count == 0) {
                        // no match
                    } else if (result.Count == 1) {
                        found = true;
                        nick = result[0];
                    } else if (result.Count >= 2) {
                        string[] nickArray = new string[result.Count];
                        result.CopyTo(nickArray, 0);
                        string nicks = String.Join(" ", nickArray, 1, nickArray.Length - 1);
                        ChatViewManager.CurrentChatView.AddMessage(
                            new MessageModel(String.Format("-!- {0}", nicks))
                        );
                        found = true;
                        partial_found = true;
                        nick = result[0];
                    }
                } else {
                    PersonModel person = cp.PersonLookup(word);
                    if (person != null) {
                        found = true;
                        nick = person.IdentityName;
                     }
                }
            } else {
                var personChat = (PersonChatView) ChatViewManager.CurrentChatView;
                var personModel = personChat.PersonModel;
                if (personModel.IdentityName.StartsWith(word,
                        StringComparison.InvariantCultureIgnoreCase)) {
                    found = true;
                    nick = personModel.IdentityName;
                }
            }

            string completionChar = Settings.CompletionCharacter;
            // add leading @ back and supress completion character
            if (leadingAt) {
                word = String.Format("@{0}", word);
                nick = String.Format("@{0}", nick);
                completionChar = String.Empty;
            }

            if (found) {
                // put the found nickname in place
                if (previous_space != -1 && next_space != -1) {
                    // previous and next space exist
                    temp = text.Remove(previous_space + 1, word.Length);
                    temp = temp.Insert(previous_space + 1, nick);
                    Text = temp;
                    if (partial_found) {
                        Position = previous_space + 1 + nick.Length;
                    } else {
                        Position = previous_space + 2 + nick.Length;
                    }
                } else if (previous_space != -1) {
                    // only previous space exist
                    temp = text.Remove(previous_space + 1, word.Length);
                    temp = temp.Insert(previous_space + 1, nick);
                    if (partial_found) {
                        Text = temp;
                    } else {
                        Text = temp + " ";
                    }
                    Position = previous_space + 2 + nick.Length;
                } else if (next_space != -1) {
                    // only next space exist
                    temp = text.Remove(0, next_space + 1);
                    if (partial_found) {
                        Text = nick + " " + temp;
                        Position = nick.Length;
                    } else {
                        Text = String.Format("{0}{1} {2}", nick,
                                             completionChar, temp);
                        Position = nick.Length + completionChar.Length + 1;
                    }
                } else {
                    // no spaces
                    if (partial_found) {
                        Text = nick;
                    } else {
                        Text = String.Format("{0}{1} ", nick, completionChar);
                    }
                    Position = -1;
                }
            }
        }
        
        public virtual void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);
            
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            var theme = new ThemeSettings(config);
            if (theme.BackgroundColor == null) {
                ModifyBase(Gtk.StateType.Normal);
            } else {
                ModifyBase(Gtk.StateType.Normal, theme.BackgroundColor.Value);
            }
            if (theme.ForegroundColor == null) {
                ModifyText(Gtk.StateType.Normal);
            } else {
                ModifyText(Gtk.StateType.Normal, theme.ForegroundColor.Value);
            }
            ModifyFont(theme.FontDescription);

            Settings.ApplyConfig(config);
        }

        private void InitCommandManager()
        {
            if (_CommandManager != null) {
                _CommandManager.Dispose();
            }

            if (Frontend.Session == null) {
                _CommandManager = null;
            } else {
                _CommandManager = new CommandManager(Frontend.Session);
                _CommandManager.ExceptionEvent +=
                delegate(object sender, CommandExceptionEventArgs e) {
                    Gtk.Application.Invoke(delegate {
                        Frontend.ShowException(e.Exception);
                    });
                };
            }
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }        
    }
}
