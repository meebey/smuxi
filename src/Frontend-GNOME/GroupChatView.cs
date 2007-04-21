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
using System.Globalization;
using Mono.Unix;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    public class GroupChatView : ChatView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Gtk.TreeView  _UserListTreeView;
        private Gtk.ListStore _UserListStore;
        private Gtk.Entry     _TopicEntry;
        private Gtk.Menu      _TabMenu;
        private Gtk.Menu      _UserListMenu;
        
        public Gtk.TreeView UserListTreeView {
            get {
                return _UserListTreeView;
            }
        }

        public Gtk.Entry TopicEntry {
            get {
                return _TopicEntry;
            }
        }
        
        public Gtk.Menu TabMenu {
            get {
                return _TabMenu;
            }
        }
        
        public GroupChatView(GroupChatModel groupChat) : base(groupChat)
        {
            _Label = new Gtk.Label(groupChat.Name);
            _LabelEventBox.Add(_Label);
            _Label.Show();
            
            // userlist
            Gtk.Frame frame = null;
            string userlist_pos = (string)Frontend.UserConfig["Interface/Notebook/Channel/UserListPosition"];
            if ((userlist_pos == "left") ||
                (userlist_pos == "right")) {
               Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
               sw.WidthRequest = 120;
               
               Gtk.TreeView tv = new Gtk.TreeView();
               tv.CanFocus = false;
               tv.BorderWidth = 0;
               sw.Add(tv);
               _UserListTreeView = tv;
               
               Gtk.TreeViewColumn statuscolumn;
               statuscolumn = new Gtk.TreeViewColumn(String.Empty, new Gtk.CellRendererText(), "text", 0);
               statuscolumn.SortColumnId = 0;
               statuscolumn.Spacing = 0;
               statuscolumn.SortIndicator = false;
               statuscolumn.Sizing = Gtk.TreeViewColumnSizing.Autosize;
               
               Gtk.TreeViewColumn usercolumn;
               usercolumn = new Gtk.TreeViewColumn(String.Empty, new Gtk.CellRendererText(), "text", 1);
               usercolumn.SortColumnId = 1;
               usercolumn.Spacing = 0;
               usercolumn.SortIndicator = false;
               usercolumn.Sizing = Gtk.TreeViewColumnSizing.Autosize;
               
               Gtk.ListStore liststore = new Gtk.ListStore(typeof(string), typeof(string));
               liststore.SetSortColumnId(0, Gtk.SortType.Ascending);
               liststore.SetSortFunc(0, new Gtk.TreeIterCompareFunc(_OnStatusSort));
               liststore.SetSortFunc(1, new Gtk.TreeIterCompareFunc(_OnUsersListSort));
               _UserListStore = liststore;
               
               tv.Model = liststore;
               tv.AppendColumn(statuscolumn);
               tv.AppendColumn(usercolumn);
               tv.RowActivated += new Gtk.RowActivatedHandler(_OnUserListRowActivated);
               
                // popup menu
                _UserListMenu = new Gtk.Menu();
                
                Gtk.ImageMenuItem op_item = new Gtk.ImageMenuItem(Catalog.GetString("Op"));
                op_item.Activated += new EventHandler(_OnUserListMenuOpActivated);
                _UserListMenu.Append(op_item);
                
                Gtk.ImageMenuItem deop_item = new Gtk.ImageMenuItem(Catalog.GetString("Deop"));
                deop_item.Activated += new EventHandler(_OnUserListMenuDeopActivated);
                _UserListMenu.Append(deop_item);
                
                Gtk.ImageMenuItem voice_item = new Gtk.ImageMenuItem(Catalog.GetString("Voice"));
                voice_item.Activated += new EventHandler(_OnUserListMenuVoiceActivated);
                _UserListMenu.Append(voice_item);
                
                Gtk.ImageMenuItem devoice_item = new Gtk.ImageMenuItem(Catalog.GetString("Devoice"));
                devoice_item.Activated += new EventHandler(_OnUserListMenuDevoiceActivated);
                _UserListMenu.Append(devoice_item);
                
                Gtk.ImageMenuItem kick_item = new Gtk.ImageMenuItem(Catalog.GetString("Kick"));
                kick_item.Activated += new EventHandler(_OnUserListMenuKickActivated);
                _UserListMenu.Append(kick_item);
                
                // frame needed for events when selecting something in the treeview
                frame = new Gtk.Frame();
                frame.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler(_OnUserListButtonReleaseEvent);
                frame.Add(sw);
            } else if (userlist_pos == "none") {
            } else {
#if LOG4NET
                _Logger.Error("ChannelPage..ctor(): unknown value in Interface/Notebook/Channel/UserListPosition: "+userlist_pos);
#endif
            }
            
            // right box
            Gtk.VBox vbox = new Gtk.VBox();
            
            string topic_pos = (string)Frontend.UserConfig["Interface/Notebook/Channel/TopicPosition"];
            if (topic_pos == "top" || topic_pos == "bottom") {
                Gtk.Entry topic = new Gtk.Entry();
                topic.IsEditable = false;
                _TopicEntry = topic;
                if (topic_pos == "top") {
                    vbox.PackStart(topic, false, false, 2);
                    vbox.PackStart(_OutputScrolledWindow, true, true, 0);
                } else {
                    vbox.PackStart(_OutputScrolledWindow, true, true, 0);
                    vbox.PackStart(topic, false, false, 2);
                }
            } else if (topic_pos == "none") {
                vbox.PackStart(_OutputScrolledWindow, true, true, 0);
            } else {
#if LOG4NET
                _Logger.Error("ChannelPage..ctor(): unknown value in Interface/Notebook/Channel/TopicPosition: "+topic_pos);
#endif
            }
            
            if (userlist_pos == "left" || userlist_pos == "right") { 
                Gtk.HPaned hpaned = new Gtk.HPaned();
                if (userlist_pos == "left") {
                    hpaned.Pack1(frame, false, false);
                    hpaned.Pack2(vbox, true, true);
                } else {
                    hpaned.Pack1(vbox, true, true);
                    hpaned.Pack2(frame, false, false);
                }
                Add(hpaned);
            } else {
                Add(vbox);
            }
            
            // popup menu
            Gtk.AccelGroup agrp = new Gtk.AccelGroup();
            Frontend.MainWindow.AddAccelGroup(agrp);
            _TabMenu = new Gtk.Menu();
            Gtk.ImageMenuItem close_item = new Gtk.ImageMenuItem(Gtk.Stock.Close, agrp);
            close_item.Activated += new EventHandler(_OnTabMenuCloseActivated);  
            _TabMenu.Append(close_item);
            
            _LabelEventBox.ButtonPressEvent += new Gtk.ButtonPressEventHandler(_OnTabButtonPress);
        }
        
        public override void Disable()
        {
            base.Disable();
            
            _TopicEntry.Text = String.Empty;
            
            _UserListStore.Clear();
            UpdateUsersCount();
        }
        
        public void UpdateUsersCount()
        {
            _UserListTreeView.GetColumn(1).Title = String.Format(
                                                    _("Users") + " ({0})",
                                                    _UserListStore.IterNChildren());
        }
        
        private static int _OnStatusSort(Gtk.TreeModel model, Gtk.TreeIter itera, Gtk.TreeIter iterb)
        {
            //Trace.Call(model, itera, iterb);
            
            Gtk.ListStore liststore = (Gtk.ListStore)model;
            // status
            int    status1a   = 0;
            string column1a = (string)liststore.GetValue(itera, 0);
            int    status1b   = 0;
            string column1b = (string)liststore.GetValue(iterb, 0);
            // nickname
            string column2a = (string)liststore.GetValue(itera, 1);
            string column2b = (string)liststore.GetValue(iterb, 1);
        
            if (column1a.IndexOf("@") != -1) {
                status1a += 1;
            }
            if (column1a.IndexOf("+") != -1) {
                status1a += 2;
            }
            if (status1a == 0) {
                status1a = 4;
            }
            column2a = status1a+column2a;
        
            if (column1b.IndexOf("@") != -1) {
                status1b += 1;
            }
            if (column1b.IndexOf("+") != -1) {
                status1b += 2;
            }
            if (status1b == 0) {
                status1b = 4;
            }
            column2b = status1b+column2b;
            
            return String.Compare(column2a, column2b, true, CultureInfo.InvariantCulture);
        }
    
        private static int _OnUsersListSort(Gtk.TreeModel model, Gtk.TreeIter itera, Gtk.TreeIter iterb)
        {
            //Trace.Call(model, itera, iterb);

            Gtk.ListStore liststore = (Gtk.ListStore)model;
            // nickname
            string column2a = (string)liststore.GetValue(itera, 1);
            string column2b = (string)liststore.GetValue(iterb, 1);
            
            return String.Compare(column2a, column2b, true, CultureInfo.InvariantCulture);
        }
        
        private void _OnTabButtonPress(object sender, Gtk.ButtonPressEventArgs e)
        {
            Trace.Call(sender, e);

            if (e.Event.Button == 3) {
                _TabMenu.Popup(null, null, null, e.Event.Button, e.Event.Time);
                _TabMenu.ShowAll();
            }
        }
        
        private void _OnTabMenuCloseActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandPart(new CommandModel(Frontend.FrontendManager, ChatModel,
                                                      ChatModel.Name));
            }
        }
        
        private void _OnUserListRowActivated(object sender, Gtk.RowActivatedArgs e)
        {
            Trace.Call(sender, e);
            
            string user = _GetSelectedNode();
            if (user == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandMessageQuery(new CommandModel(Frontend.FrontendManager,
                                                              ChatModel, user));
            }
        }            
        
        private void _OnUserListButtonReleaseEvent(object sender, Gtk.ButtonReleaseEventArgs e)
        {
            Trace.Call(sender, e);

            if (e.Event.Button == 3) {
                _UserListMenu.Popup(null, null, null, e.Event.Button, e.Event.Time);
                _UserListMenu.ShowAll();
            }
        }
        
        private void _OnUserListMenuOpActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            string whom = _GetSelectedNode();
            if (whom == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandOp(new CommandModel(Frontend.FrontendManager, ChatModel,
                    whom));
            }
        } 
        
        private void _OnUserListMenuDeopActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            string whom = _GetSelectedNode();
            if (whom == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandDeop(new CommandModel(Frontend.FrontendManager, ChatModel,
                    whom));
            }
        }
         
        private void _OnUserListMenuVoiceActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            string whom = _GetSelectedNode();
            if (whom == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandVoice(new CommandModel(Frontend.FrontendManager, ChatModel,
                    whom));
            }
        }
        
        private void _OnUserListMenuDevoiceActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            string whom = _GetSelectedNode();
            if (whom == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandDevoice(new CommandModel(Frontend.FrontendManager, ChatModel,
                    whom));
            }
        } 
        
        private void _OnUserListMenuKickActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            string victim = _GetSelectedNode();
            if (victim == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandKick(new CommandModel(Frontend.FrontendManager, ChatModel,
                    victim));
            }
        } 
        
        private string _GetSelectedNode()
        {
            Gtk.TreeIter iter;
            Gtk.TreeModel model;
            if (_UserListTreeView.Selection.GetSelected(out model, out iter)) {
                return (string)model.GetValue(iter, 1);
            }
            
            return null;
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
