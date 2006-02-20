/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
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
using System.Collections;
using System.Collections.Specialized;
using Meebey.Smuxi.Engine;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class Entry : Gtk.Entry
    {
        private StringCollection _History = new StringCollection();
        private int              _HistoryPosition = 0;
        private bool             _HistoryChangedLine = false;

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
                _History.Add(String.Empty);
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
                    // don't loose the focus
                    args.RetVal = true;
                    
                    if (Frontend.FrontendManager.CurrentPage is Engine.ChannelPage) {
                        if (Text.Length > 0) {
                            _NickCompletion();
                        }
                    }
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
                case 65365: // Page-Up
#if LOG4NET
                    Logger.UI.Debug("Page-Up");
#endif
                    Frontend.MainWindow.Notebook.GetPage(
                        Frontend.FrontendManager.CurrentPage).ScrollUp();
                    break;
                case 65366: // Page-Down
#if LOG4NET
                    Logger.UI.Debug("Page-Down");
#endif
                    Frontend.MainWindow.Notebook.GetPage(
                        Frontend.FrontendManager.CurrentPage).ScrollDown();
                    break;
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
            
            ExecuteCommand(Text);
            AddToHistory(Text, History.Count - HistoryPosition);
            Text = String.Empty;
        }
        
        public void ExecuteCommand(string cmd)
        {
            bool handled;
            CommandData cd = new CommandData(Frontend.FrontendManager,
                                    (string)Frontend.UserConfig["Interface/Entry/CommandCharacter"],
                                    cmd);
            handled = _Command(cd);
            if (!handled) {
                handled = Frontend.Session.Command(cd);
            }
            if (!handled) {
                // we may have no network manager yet
                if (Frontend.FrontendManager.CurrentNetworkManager != null) {
                    handled = Frontend.FrontendManager.CurrentNetworkManager.Command(cd);
                } else {
                    handled = true;
                }
            }
            if (!handled) {
               _CommandUnknown(cd);
            }
        }
        
        private bool _Command(CommandData cd)
        {
            bool handled = false;
            
            // command that work even without beeing connected 
            if (cd.IsCommand) {
                switch (cd.Command) {
                    case "help":
                        _CommandHelp(cd);
                        break;
                    case "quit":
                        _CommandQuit(cd);
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
                }
            }
            
            return handled;
        }
        
        private void _CommandHelp(CommandData cd)
        {
            string[] help = {
            "[Frontend Commands]",
            "help",
            "window (number|channelname|queryname|close",
            "echo data",
            "exec command",
            "quit [quitmessage]",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToCurrentPage("-!- "+line);
            }
        }
        
        private void _CommandQuit(CommandData cd)
        {
            Frontend.Quit();
        }

        private void _CommandEcho(CommandData cd)
        {
            cd.FrontendManager.AddTextToCurrentPage("-!- "+cd.Parameter);
        }
        
        private void _CommandExec(CommandData cd)
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
                    cd.FrontendManager.AddTextToCurrentPage(output);
                } catch {
                }
            }
        }
    
        private void _CommandWindow(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                string name;
                if (cd.DataArray[1].ToLower() == "close") {
                    name = fm.CurrentPage.Name;
                    if (fm.CurrentPage.PageType != PageType.Server) {
                        if (fm.CurrentNetworkManager is IrcManager) {
                            IrcManager ircm = (IrcManager)fm.CurrentNetworkManager; 
                            if (fm.CurrentPage.PageType == PageType.Channel) {
                                ircm.CommandPart(new CommandData(fm, cd.CommandCharacter, "/part "+name));
                            } else {
                                // query
                                Frontend.Session.RemovePage(fm.CurrentPage);
                            }
                        }
                    }
                } else {
                    bool is_number = false;
                    int pagecount = Frontend.MainWindow.Notebook.NPages;
                    try {
                        int number = Int32.Parse(cd.DataArray[1]);
                        is_number = true;
                        if (number <= pagecount) {
                            Frontend.MainWindow.Notebook.CurrentPage = number - 1;
                        }
                    } catch (FormatException) {
                    }
                    
                    if (!is_number) {
                        // seems to be query- or channelname
                        // let's see if we find something
                        ArrayList candidates = new ArrayList();
                        for (int i = 0; i < pagecount; i++) {
                            Page page = (Page)Frontend.MainWindow.Notebook.GetNthPage(i);
                            Engine.Page epage = page.EnginePage;
                            if (epage.Name.ToLower() == cd.DataArray[1].ToLower()) {
                                if ((epage.PageType == fm.CurrentPage.PageType) &&
                                    (epage.NetworkManager == fm.CurrentPage.NetworkManager)) {
                                    Frontend.MainWindow.Notebook.CurrentPage = i;
                                    break;
                                } else {
                                    // there was no exact match
                                    candidates.Add(i);
                                }
                            }
                        }
                        if (candidates.Count > 0) {
                            Frontend.MainWindow.Notebook.CurrentPage = (int)candidates[0];
                        }
                    }
                }
            }
        }
    
        private void _CommandUnknown(CommandData cd)
        {
            cd.FrontendManager.AddTextToCurrentPage("-!- Unknown Command: "+cd.Command);
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
                    found = true;
                    nick = result[0];
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
