/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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

namespace Smuxi.FrontendGnome
{
    public class Entry : Gtk.Entry
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private StringCollection _History = new StringCollection();
        private int              _HistoryPosition;
        private bool             _HistoryChangedLine;
        private Notebook         _Notebook;
        
        public StringCollection History {
            get {
                return _History;
            }
        }

        public int HistoryPosition {
            get {
                return _HistoryPosition;
            }
            set {
                _HistoryPosition = value;
            }
        }

        public bool HistoryChangedLine {
            get {
                return _HistoryChangedLine;
            }
            set {
                _HistoryChangedLine = value;
            }
        }

        public Entry(Notebook notebook)
        {
            _History.Add(String.Empty);
            
            _Notebook = notebook;
            
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
                _Logger.Debug("_HistoryChangedLine = true");
#endif
            } else {
                _HistoryChangedLine = false;
#if LOG4NET
                _Logger.Debug("_HistoryChangedLine = false");
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

            if (_History.Count > (int)Frontend.UserConfig["Interface/Entry/CommandHistorySize"]) {
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
            Trace.Call(sender, e);
            
#if LOG4NET
            _Logger.Debug("_OnKeyPress(): Key: " + e.Event.Key.ToString() + 
                          " KeyValue: " + e.Event.KeyValue);
#endif

            int keynumber = (int)e.Event.KeyValue;
            Gdk.Key key = e.Event.Key;
            if ((e.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                // ctrl is pressed
                e.RetVal = true;
                switch (key) {
                    case Gdk.Key.x:
                        //if (Frontend.FrontendManager.CurrentPage.PageType == PageType.Server) {
                        // this does the same with one remoting call less
                        // (the CurrentPage object is not called)
                        if (_Notebook.CurrentChatView.ChatModel.ChatType == ChatType.Network) {
                            Frontend.FrontendManager.NextNetworkManager();
                        }
                        break;
                    case Gdk.Key.Page_Down:
                        if (_Notebook.CurrentPage < _Notebook.NPages) {
                            _Notebook.CurrentPage++;
                        }
                        break;
                    case Gdk.Key.Page_Up:
                        if (_Notebook.CurrentPage > 0) {
                            _Notebook.CurrentPage--;
                        }
                        break;
                    case Gdk.Key.Home:
                        _Notebook.CurrentChatView.ScrollToStart();
                        break;
                    case Gdk.Key.End:
                        _Notebook.CurrentChatView.ScrollToEnd();
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

                if (pagenumber != -1 &&
                    _Notebook.NPages >= pagenumber + 1) {
                    _Notebook.Page = pagenumber;
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
                    e.RetVal = true;
                    if (Frontend.MainWindow.CaretMode) {
                        // when we are in caret-mode change focus to output textview
                        _Notebook.CurrentChatView.OutputTextView.HasFocus = true;
                    } else {
                        // don't loose the focus (if we are not in caret-mode)
                        if (_Notebook.CurrentChatView.ChatModel is GroupChatModel) {
                            if (Text.Length > 0) {
                                _NickCompletion();
                            }
                        }
                    }
                    break;
                case Gdk.Key.Up:
                    HistoryPrevious();
                    break;
                case Gdk.Key.Down:
                    HistoryNext();
                    break;
                case Gdk.Key.Page_Up:
                    _Notebook.CurrentChatView.ScrollUp();
                    break;
                case Gdk.Key.Page_Down:
                    _Notebook.CurrentChatView.ScrollDown();
                    break;
            }
        }

        private void _OnFocusOut(object sender, Gtk.FocusOutEventArgs e)
        {
            Trace.Call(sender, e);
            
            if (!Frontend.MainWindow.CaretMode) {
                HasFocus = true;
                Position = -1;
            }
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
                		string msg = String.Format(_("You are going to paste {0} lines, do you want to continue?"),
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
                    AddToHistory(Text, History.Count - HistoryPosition);
                    // reset history position to last entry
                    _HistoryPosition = _History.Count - 1;
    	        }
                Text = String.Empty;
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
                Frontend.ShowException(ex);
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
            
            bool handled;
            CommandModel cd = new CommandModel(Frontend.FrontendManager, _Notebook.CurrentChatView.ChatModel,
                                    (string)Frontend.UserConfig["Interface/Entry/CommandCharacter"],
                                    cmd);
            handled = _Command(cd);
            if (!handled) {
                handled = Frontend.Session.Command(cd);
            }
            if (!handled) {
                // we may have no network manager yet
                Engine.INetworkManager nm = Frontend.FrontendManager.CurrentNetworkManager;
                if (nm != null) {
                    handled = nm.Command(cd);
                } else {
                    handled = false;
                }
            }
            if (!handled) {
               _CommandUnknown(cd);
            }
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
                }
            }
            
            return handled;
        }
        
        private void _CommandHelp(CommandModel cd)
        {
            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = _("[Frontend Commands]");
            msgPart.Bold = true;
            msg.MessageParts.Add(msgPart);
            
            cd.FrontendManager.AddMessageToChat(cd.Chat, msg);
            
            string[] help = {
            "help",
            "window (number|channelname|queryname|close",
            "clear",
            "echo data",
            "exec command",
            "detach",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToCurrentChat("-!- " + line);
            }
        }
        
        private void _CommandDetach(CommandModel cd)
        {
            Frontend.Quit();
        }

        private void _CommandEcho(CommandModel cd)
        {
            cd.FrontendManager.AddTextToCurrentChat("-!- "+cd.Parameter);
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
                    cd.FrontendManager.AddTextToCurrentChat(output);
                } catch {
                }
            }
        }
    
        private void _CommandWindow(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                ChatModel currentChatModel = _Notebook.CurrentChatView.ChatModel;
                string name;
                if (cd.DataArray[1].ToLower() == "close") {
                    name = currentChatModel.Name;
                    if (currentChatModel.ChatType != ChatType.Network) {
                        if (currentChatModel.NetworkManager is IrcNetworkManager) {
                            IrcNetworkManager ircm = (IrcNetworkManager) currentChatModel.NetworkManager; 
                            if (currentChatModel.ChatType == ChatType.Group) {
                                ircm.CommandPart(new CommandModel(fm, currentChatModel, name));
                            } else {
                                // query
                                Frontend.Session.RemoveChat(currentChatModel);
                            }
                        }
                    }
                } else {
                    bool is_number = false;
                    int pagecount = _Notebook.NPages;
                    try {
                        int number = Int32.Parse(cd.DataArray[1]);
                        is_number = true;
                        if (number <= pagecount) {
                            _Notebook.CurrentPage = number - 1;
                        }
                    } catch (FormatException) {
                    }
                    
                    if (!is_number) {
                        // seems to be query- or channelname
                        // let's see if we find something
                        ArrayList candidates = new ArrayList();
                        for (int i = 0; i < pagecount; i++) {
                            ChatView chatView = _Notebook.GetChat(i);
                            ChatModel chatModel = chatView.ChatModel;
                            
                            if (chatModel.Name.ToLower() == cd.DataArray[1].ToLower()) {
                                // name matches
                                // first let's see if there is an exact match, if so, take it
                                if ((chatModel.ChatType == currentChatModel.ChatType) &&
                                    (chatModel.NetworkManager == currentChatModel.NetworkManager)) {
                                    _Notebook.CurrentPage = i;
                                    break;
                                } else {
                                    // there was no exact match
                                    candidates.Add(i);
                                }
                            }
                        }
                        
                        if (candidates.Count > 0) {
                            _Notebook.CurrentPage = (int)candidates[0];
                        }
                    }
                }
            }
        }
    
        private void _CommandClear(CommandModel cd)
        {
            _Notebook.CurrentChatView.OutputTextView.Buffer.Clear();
        }
        
        private void _CommandUnknown(CommandModel cd)
        {
            cd.FrontendManager.AddTextToCurrentChat("-!- " +
                                String.Format(Catalog.GetString(
                                              "Unknown Command: {0}"),
                                              cd.Command));
        }
        
        private void _NickCompletion()
        {
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

            // find the possible nickname
            bool found = false;
            bool partial_found = false;
            string nick = null;
            //GroupChatModel cp = (GroupChatModel) Frontend.FrontendManager.CurrentChat;
            GroupChatModel cp = (GroupChatModel) _Notebook.CurrentChatView.ChatModel;
            if ((bool)Frontend.UserConfig["Interface/Entry/BashStyleCompletion"]) {
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
                    Frontend.FrontendManager.AddTextToCurrentChat("-!- " + nicks);
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
                        Text = temp+" ";
                    }
                    Position = previous_space + 2 + nick.Length;
                } else if (next_space != -1) {
                    // only next space exist
                    temp = text.Remove(0, next_space + 1);
                    if (partial_found) {
                        Text = nick + " " + temp;
                        Position = nick.Length;
                    } else {
                        Text = nick+(string)Frontend.UserConfig["Interface/Entry/CompletionCharacter"] + " " + temp;
                        Position = nick.Length + 2;
                    }
                } else {
                    // no spaces
                    if (partial_found) {
                        Text = nick;
                    } else {
                        Text = nick+(string)Frontend.UserConfig["Interface/Entry/CompletionCharacter"]+" ";
                    }
                    Position = -1;
                }
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }        
    }
}
