/**
 * $Id: AssemblyInfo.cs 34 2004-09-05 14:46:59Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/Gnosmirc/trunk/src/AssemblyInfo.cs $
 * $Rev: 34 $
 * $Author: meebey $
 * $Date: 2004-09-05 16:46:59 +0200 (Sun, 05 Sep 2004) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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
using Meebey.Smuxi.Engine;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    using System.Collections.Specialized;

    public class Entry : Gtk.Entry
    {
        private StringCollection _History = new StringCollection();
        private int              _HistoryPosition = 0;
        private bool             _HistoryChangedLine = false;

        public StringCollection History
        {
            get {
                return _History;
            }
        }

        public int HistoryPosition
        {
            get {
                return _HistoryPosition;
            }
            set {
                _HistoryPosition = value;
            }
        }

        public bool HistoryChangedLine
        {
            get {
                return _HistoryChangedLine;
            }
            set {
                _HistoryChangedLine = value;
            }
        }

        public Entry()
        {
            _History.Add(String.Empty);
            
            Activated += new EventHandler(_OnActivated);
            KeyPressEvent += new Gtk.KeyPressEventHandler(_OnKeyPress);
            FocusOutEvent += new Gtk.FocusOutEventHandler(_OnFocusOut);
        }

        public void UpdateHistoryChangedLine()
        {
            if ((_History.Count > 0) &&
                (Text.Length > 0) &&
                (Text != HistoryCurrent())) {
                // the entry changed and the entry is not empty
                _HistoryChangedLine = true;
#if LOG4NET
                Logger.CommandHistory.Debug("changed = true");
#endif
            } else {
                _HistoryChangedLine = false;
#if LOG4NET
                Logger.CommandHistory.Debug("changed = false");
#endif
            }
        }

        public void AddToHistory(string data, int positiondiff)
        {
            /*
            this code doesnt work well
            // _History.Count-1 is the last entry, which should be always empty
            if ((_History.Count > 1) &&
                (data == _History[_History.Count-2])) {
                // don't add the same value
                return;
            }
            */

            _History.Insert(_History.Count-1, data);
#if LOG4NET
             Logger.CommandHistory.Debug("added: '"+data+"' to history");
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
                Logger.CommandHistory.Debug("entry changed, adding to history");
#endif
                AddToHistory(Text, 0);
                _HistoryChangedLine = false;
            }

            if (_HistoryPosition > 0) {
#if LOG4NET
                Logger.CommandHistory.Debug("showing previous item");
#endif
                _HistoryPosition--;
                Text = HistoryCurrent();
            }
        }

        public void HistoryNext()
        {
            if (_HistoryChangedLine) {
#if LOG4NET
                Logger.CommandHistory.Debug("entry changed, adding to history");
#endif
                AddToHistory(Text, 0);
                _HistoryChangedLine = false;
            }

            if (_HistoryPosition < _History.Count-1) {
#if LOG4NET
                Logger.CommandHistory.Debug("showing next item");
#endif
                _HistoryPosition++;
                Text = HistoryCurrent();
                Position = -1;
            } else if (Text.Length > 0) {
#if LOG4NET
                Logger.CommandHistory.Debug("not empty line, lets add one");
#endif
                // last position and we went further down
                _History.Add("");
                _HistoryPosition++;
                Text = String.Empty;
            }
        }

        [GLib.ConnectBefore]
        private void _OnKeyPress(object obj, Gtk.KeyPressEventArgs args)
        {
#if LOG4NET
            Logger.UI.Debug("Entry.OnKeyPress triggered");
            Logger.UI.Debug("KeyValue: "+args.Event.KeyValue);
#endif

            int keynumber = (int)args.Event.KeyValue;
            if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                // ctrl is pressed
                switch (keynumber) {
                    case 120: // x
                        if (Frontend.FrontendManager.CurrentPage.PageType == PageType.Server) {
                            Frontend.FrontendManager.NextNetworkManager();
                        }
                    break;
                }
            }
            
            int pagenumber = -1;
            if ((args.Event.State & Gdk.ModifierType.Mod1Mask) != 0) {
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

                if ((pagenumber != -1) &&
                    (Frontend.MainWindow.Notebook.NPages >= pagenumber+1)) {
                    Frontend.MainWindow.Notebook.Page = pagenumber;
                }
            }

            if (((args.Event.State & Gdk.ModifierType.Mod1Mask) != 0) ||
                ((args.Event.State & Gdk.ModifierType.ControlMask) != 0) ||
                ((args.Event.State & Gdk.ModifierType.ShiftMask) != 0)) {
                // alt, ctrl or shift pushed, returning
                return;
            }

            UpdateHistoryChangedLine();
            switch (keynumber) {
                case 65289: // TAB
                    args.RetVal = true;
                break;
                case 65362: // Up-Arrow
#if LOG4NET
                    Logger.UI.Debug("Up-Arrow");
#endif
                    HistoryPrevious();
                break;
                case 65364: // Down-Arrow
#if LOG4NET
                    Logger.UI.Debug("Down-Arrow");
#endif
                    HistoryNext();
                break;
            }

            if (Frontend.FrontendManager.CurrentPage is Engine.ChannelPage) {
                switch (keynumber) {
                    case 65289: // TAB
                        if (Text.Length > 0) {
                            _NickCompletion();
                        }
                    break;
                }
            }
        }

        private void _OnFocusOut(object obj, Gtk.FocusOutEventArgs args)
        {
            HasFocus = true;
            Position = -1;
        }

        private void _OnActivated(object obj, EventArgs args)
        {
            if (!(Text.Length > 0)) {
                return;
            } 
            
            bool handled = false;
            handled = _Command(Frontend.FrontendManager, Text);
            if (!handled) {
                handled = Frontend.Session.Command(Frontend.FrontendManager, Text);
            }
            if (!handled) {
                // we may have no network manager yet
                if (Frontend.FrontendManager.CurrentNetworkManager != null) {
                    handled = Frontend.FrontendManager.CurrentNetworkManager.Command(
                                Frontend.FrontendManager, Text);
                }
            }
            if (!handled) {
               _CommandUnknown(Frontend.FrontendManager, Text);
            }
            
            AddToHistory(Text, History.Count - HistoryPosition);
            Text = String.Empty;
        }
        
        private bool _Command(FrontendManager fm, string data)
        {
            bool handled = false;
            if (!(data.Length >= 1)) {
                return false;
            }

            string[] dataex = data.Split(new char[] {' '});
            string parameter = String.Join(" ", dataex, 1, dataex.Length-1);
            string command = (dataex[0].Length > 1) ? dataex[0].Substring(1).ToLower() : "";
            // command that work even without beeing connected 
            if (data[0] == ((string)Frontend.UserConfig["Interface/Entry/CommandCharacter"])[0]) {
                switch (command) {
                    case "quit":
                        _CommandQuit(fm, data, dataex, parameter);
                        handled = true;
                        break;
                    case "echo":
                        _CommandEcho(fm, data, dataex, parameter);
                        handled = true;
                        break;
                    case "exec":
                        _CommandExec(fm, data, dataex, parameter);
                        handled = true;
                        break;
                    case "window":
                        _CommandWindow(fm, data, dataex, parameter);
                        handled = true;
                        break;
                }
            }
            
            return handled;
        }
        
        private void _CommandQuit(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Frontend.Quit();
        }

        private void _CommandEcho(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            fm.AddTextToCurrentPage("-!- "+parameter);
        }
        
        private void _CommandExec(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 2) {
                string output;
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = dataex[1];
                if (dataex.Length >= 3) { 
                    process.StartInfo.Arguments = String.Join(" ", dataex, 2, dataex.Length-2);
                }
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                
                try {
                    process.Start();
                    output = process.StandardOutput.ReadToEnd();
                    fm.AddTextToCurrentPage(output);
                } catch {
                }
            }
        }
    
        private void _CommandWindow(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 2) {
                string name;
                if (dataex[1].ToLower() == "close") {
                    name = fm.CurrentPage.Name;
                    if (fm.CurrentPage.PageType != PageType.Server) {
                        if (fm.CurrentNetworkManager is IrcManager) {
                            IrcManager ircm = (IrcManager)fm.CurrentNetworkManager; 
                            if (fm.CurrentPage.PageType == PageType.Channel) {
                                // HACK: we emulate an user typed command, WTF?!?
                                ircm.Command(fm, "/part "+name);
                            } else {
                                // query
                                Frontend.Session.RemovePage(fm.CurrentPage);
                            }
                        }
                    }
                } else {
                    try {
                        int number = Int32.Parse(dataex[1]);
                        if (number <= Frontend.MainWindow.Notebook.NPages) {
                            Frontend.MainWindow.Notebook.CurrentPage = number - 1;
                        } else {
                            // TODO: search the page when found make it currrent page
#if _0
                            foreach (Page page in Frontend.Session.Pages) {
                                if (page.Name.ToLower() == dataex[1].ToLower()) {
                                    number = Frontend.MainWindow.Notebook.PageNum(page);
                                    Frontend.MainWindow.Notebook.CurrentPage = number;
                                    break;
                                }
                            }
#endif
                        }
                    } catch (FormatException) {
                    }
                }
            }
        }
    
        private void _CommandUnknown(FrontendManager fm, string data)
        {
            string[] dataex = data.Split(new char[] {' '});
            fm.AddTextToCurrentPage("-!- Unknown Command: "+dataex[0].Substring(1).ToLower());
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
            Logger.NickCompletion.Debug("previous_space: "+previous_space);
            Logger.NickCompletion.Debug("next_space: "+next_space);
#endif

            if (previous_space != -1 && next_space != -1) {
                // previous and next space exist
                word = text.Substring(previous_space+1, next_space-previous_space-1);
            } else if (previous_space != -1) {
                // previous space exist
                word = text.Substring(previous_space+1);
            } else if (next_space != -1) {
                // next space exist
                word = text.Substring(0, text.Length-next_space);
            } else {
                // no spaces
                word = text;
            }

            if (word == String.Empty) {
                return;
            }

            // find the possible nickname
            bool found = false;
            string nick = null;
            Engine.ChannelPage cp = (Engine.ChannelPage)Frontend.FrontendManager.CurrentPage;
            if ((bool)Frontend.UserConfig["Interface/Entry/BashStyleCompletion"]) {
                string[] result = cp.NicknameLookupAll(word);
                if (result.Length > 1) {
                    string nicks = String.Join(" ", result);
                    Frontend.FrontendManager.AddTextToCurrentPage("-!- "+nicks);
                } else if (result.Length == 1) {
                    found = true;
                    nick = result[0];
                }
            } else {
                string result = cp.NicknameLookup(word);
                if (result != null) {
                    found = true;
                    nick = result;
                 }
            }

            if (found) {
                // put the found nickname in place
                if (previous_space != -1 && next_space != -1) {
                    // previous and next space exist
                    temp = text.Remove(previous_space+1, word.Length);
                    temp = temp.Insert(previous_space+1, nick);
                    Text = temp;
                    Position = previous_space+2+nick.Length;
                } else if (previous_space != -1) {
                    // previous space exist
                    temp = text.Remove(previous_space+1, word.Length);
                    temp = temp.Insert(previous_space+1, nick);
                    Text = temp+" ";
                    Position = previous_space+2+nick.Length;
                } else if (next_space != -1) {
                    // next space exist
                    Text = nick+(string)Frontend.UserConfig["Interface/Entry/CompletionCharacter"]+" ";
                    Position = -1;
                } else {
                    // no spaces
                    Text = nick+(string)Frontend.UserConfig["Interface/Entry/CompletionCharacter"]+" ";
                    Position = -1;
                }
            }
        }
    }
}
