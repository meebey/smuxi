/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2013 Mirco Bauer <meebey@meebey.net>
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Mono.Unix;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    public class Entry : Gtk.TextView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private StringCollection _History = new StringCollection();
        private int              _HistoryPosition;
        private bool             _HistoryChangedLine;
        private CommandManager   _CommandManager;
        private new EntrySettings Settings { get; set; }

        private NickCompleter NickCompleter { get; set; }

        ChatViewManager ChatViewManager;
        event EventHandler<EventArgs> Activated;

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

        public string Text {
            get {
                return Buffer.Text;
            }
            set {
                Buffer.Text = value;
            }
        }

        public int Position {
            get {
                return Buffer.CursorPosition;
            }
            set {
                Gtk.TextIter position;
                if (value < 0) {
                    position = Buffer.EndIter;
                } else {
                    position = Buffer.GetIterAtOffset(value);
                }
                Buffer.PlaceCursor(position);
            }
        }

        public Entry(ChatViewManager chatViewManager)
        {
            Trace.Call(chatViewManager);

            if (chatViewManager == null) {
                throw new ArgumentNullException("chatViewManager");
            }

            _History.Add(String.Empty);
            
            ChatViewManager = chatViewManager;
            Settings = new EntrySettings();
            WrapMode = Gtk.WrapMode.WordChar;

            InitSpellCheck();
            InitCommandManager();
            Frontend.SessionPropertyChanged += delegate {
                InitCommandManager();
            };

            Activated += _OnActivated;
            KeyPressEvent += new Gtk.KeyPressEventHandler(_OnKeyPress);
            PasteClipboard += _OnClipboardPasted;
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
            if (Frontend.IsWindows && String.IsNullOrEmpty(Text)) {
                // HACK: workaround rendering issue on Windows where the text
                // cursor and first typed character are not showing up until
                // a 2nd character is typed, see #810
                QueueDraw();
            }

            if (ChatViewManager.CurrentChatView == null) {
                // we are during a sync, so don't process keys yet
                return;
            }

            int keynumber = (int)e.Event.KeyValue;
            Gdk.Key key = e.Event.Key;
            if ((e.Event.State & Gdk.ModifierType.ControlMask) != 0 ||
                (Frontend.IsMacOSX && (e.Event.State & Gdk.ModifierType.MetaMask) != 0)) {
                // ctrl / cmd is pressed
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
                    case Gdk.Key.c:
                    case Gdk.Key.C:
                        // only use copy if something is selected in the entry
                        if (Buffer.HasSelection) {
                            e.RetVal = false;
                            break;
                        }
                        // copy selection from main chat window
                        var buf = ChatViewManager.CurrentChatView.OutputMessageTextView.Buffer;
                        buf.CopyClipboard(Gtk.Clipboard.Get(Gdk.Selection.Clipboard));
                        break;
                    // don't break unicode input
                    case Gdk.Key.U:
                    // don't break paste
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
                    // anything else we let GTK+ handle
                    default:
                        e.RetVal = false;
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
                switch (key) {
                    case Gdk.Key.h:
                    case Gdk.Key.H:
                        if (Frontend.IsMacOSX) {
                            Frontend.MainWindow.Iconify();
                            e.RetVal = true;
                        }
                        break;
                    case Gdk.Key.braceleft:
                    case Gdk.Key.Up:
                        if (Frontend.IsMacOSX) {
                            ChatViewManager.CurrentChatNumber--;
                            e.RetVal = true;
                        }
                        break;
                    case Gdk.Key.braceright:
                    case Gdk.Key.Down:
                        if (Frontend.IsMacOSX) {
                            ChatViewManager.CurrentChatNumber++;
                            e.RetVal = true;
                        }
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
                    // supress scrolling
                    ChatViewManager.CurrentChatView.ScrollUp();
                    e.RetVal = true;
                    break;
                case Gdk.Key.Page_Down:
                    // supress scrolling
                    ChatViewManager.CurrentChatView.ScrollDown();
                    e.RetVal = true;
                    break;
                case Gdk.Key.Return:
                case Gdk.Key.KP_Enter:
                case Gdk.Key.ISO_Enter:
                case Gdk.Key.Key_3270_Enter:
                    // supress adding a newline
                    e.RetVal = true;
                    if (Activated != null) {
                        Activated(this, EventArgs.Empty);
                    }
                    break;
            }
        }

        private void _OnActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                if (!(Text.Length > 0)) {
                    return;
                }
                if (ChatViewManager.CurrentChatView == null) {
                    return;
                }
                
                if (Text.IndexOf("\n") != -1) {
                    var text = Text.TrimEnd('\n');
                    // seems to be a paste, so let's break it apart
                    string[] msgParts = text.Split(new char[] {'\n'});
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
                    if (Frontend.EngineVersion < new Version(0,8,11)) {
                        foreach (string msg in msgParts) {
                            ExecuteCommand(msg);
                        }
                    } else {
                        // new engines know how to handle messages containing \n
                        ExecuteCommand(text);
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
                    case "sync":
                        _CommandSync(cd);
                        handled = true;
                        break;
                    case "sort":
                        CommandSort(cd);
                        handled = true;
                        break;
                    case "gc":
                        GC.Collect();
                        handled = true;
                        break;
                    case "generate_messages":
                        CommandGenerateMessages(cd);
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
            "window (number|channelname|queryname|close)",
            "sync",
            "sort",
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
            Frontend.OpenFindGroupChatWindow(cd.Parameter);
        }

        private void _CommandDetach(CommandModel cd)
        {
            Frontend.Quit();
        }

        private void _CommandWindow(CommandModel cd)
        {
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

                        if (chatView == currentChat) {
                            // we don't want to switch to ourselves
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
    
        private void _CommandSync(CommandModel cmd)
        {
            if (Frontend.IsLocalEngine) {
                return;
            }

            var msgCount = 0;
            Int32.TryParse(cmd.Parameter, out msgCount);

            var chatView = ChatViewManager.CurrentChatView;
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    var oldValue = Frontend.UseLowBandwidthMode;
                    // HACK: force a full sync
                    Frontend.UseLowBandwidthMode = false;
                    chatView.Sync(msgCount);
                    Frontend.UseLowBandwidthMode = oldValue;

                    Gtk.Application.Invoke(delegate {
                        Frontend.UseLowBandwidthMode = false;
                        chatView.Populate();
                        Frontend.UseLowBandwidthMode = oldValue;
                        chatView.ScrollToEnd();
                    });
                } catch (Exception ex) {
                    Frontend.ShowError(null, ex);
                }
            });
        }

        void CommandSort(CommandModel cmd)
        {
            var chats = new List<ChatView>(ChatViewManager.Chats);
            // as the sorting does 2 remoting calls, we use a background thread
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    chats.Sort((x, y) => {
                        if (x.ProtocolManager != null &&
                            y.ProtocolManager != null &&
                            x.ProtocolManager != y.ProtocolManager) {
                            // REMOTING CALL 1
                            var xprot = x.ProtocolManager.Protocol;
                            // REMOTING CALL 2
                            var yprot = y.ProtocolManager.Protocol;
                            var prot = xprot.CompareTo(yprot);
                            if (prot != 0) {
                                return prot;
                            }

                            // theirs protocols are equal, so their network decide
                            // REMOTING CALL 3
                            var xnet = x.ProtocolManager.NetworkID;
                            // REMOTING CALL 4
                            var ynet = y.ProtocolManager.NetworkID;
                            var net = xnet.CompareTo(ynet);
                            if (net != 0) {
                               return net;
                            }
                        }

                        // their networks are equal, so their type decide
                        var type = GetChatSortValue(y).CompareTo(GetChatSortValue(x));
                        if (type != 0) {
                            return type;
                        }

                        // their types are equal, so their name decides
                        return x.ID.CompareTo(y.ID);
                    });

                    Gtk.Application.Invoke(delegate {
                        for (int i = 0; i < chats.Count; i++) {
                            Frontend.MainWindow.Notebook.ReorderChild(chats[i], i);
                        }
                    });
                } catch (Exception ex) {
#if LOG4NET
                    _Logger.Error("CommandSort(): Exception", ex);
#endif
                }
            });
        }

        int GetChatSortValue(ChatView chat)
        {
            int status = 0;
            if (chat is SessionChatView) {
                status += 100;
            }
            if (chat is ProtocolChatView) {
                status += 50;
            }
            if (chat is GroupChatView) {
                status += 10;
            }
            return status;
        }

        private void _CommandClear(CommandModel cd)
        {
            ChatViewManager.CurrentChatView.Clear();
        }

        void CommandGenerateMessages(CommandModel cmd)
        {
            var chat = ChatViewManager.CurrentChatView;
            _CommandManager.CommandGenerateMessages(cmd, chat);

            var builder = new MessageBuilder();
            builder.AppendText(
                "ChatView.AddMessage(): MessageTextTagTable.Size: {0}",
                chat.OutputMessageTextView.MessageTextTagTable.Size
            );
            chat.AddMessage(builder.ToMessage());
        }

        private void _NickCompletion()
        {
            // perform completion
            string text = Text;
            int position = Position;
            NickCompleter.Complete(ref text, ref position, ChatViewManager.CurrentChatView);
            Text = text;
            Position = position;
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

            // replace nick completer if needed
            if (Settings.BashStyleCompletion && !(NickCompleter is LongestPrefixNickCompleter)) {
                NickCompleter = new LongestPrefixNickCompleter();
            } else if (!Settings.BashStyleCompletion && !(NickCompleter is TabCycleNickCompleter)) {
                NickCompleter = new TabCycleNickCompleter();
            }

            // set the completion character
            NickCompleter.CompletionChar = Settings.CompletionCharacter;
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
                _CommandManager.EngineVersion = Frontend.EngineVersion;
                _CommandManager.ExceptionEvent +=
                delegate(object sender, CommandExceptionEventArgs e) {
                    Gtk.Application.Invoke(delegate {
                        Frontend.ShowException(e.Exception);
                    });
                };
            }
        }

        private void InitSpellCheck()
        {
#if GTKSPELL
            try {
                gtkspell_new_attach(Handle, null, IntPtr.Zero);
            } catch (Exception ex) {
                _Logger.Error("InitSpellCheck(): gtkspell_new_attach() "+
                              "threw exception", ex);
            }
#endif
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }

#if GTKSPELL
        [DllImport("gtkspell.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr gtkspell_new_attach(IntPtr text_view,
                                                 string locale,
                                                 IntPtr error);

        [DllImport("gtkspell.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void gtkspell_detach(IntPtr obj);
#endif
    }
}
