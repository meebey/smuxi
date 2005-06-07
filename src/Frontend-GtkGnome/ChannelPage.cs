/**
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
using System.Globalization;
using Meebey.Smuxi.Engine;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class ChannelPage : Page
    {
        private Gtk.TreeView _UserListTreeView;
        private Gtk.Entry    _TopicEntry;
        private Gtk.Menu     _TabMenu;
        private Gtk.Menu     _UserListMenu;
        
        public Gtk.TreeView UserListTreeView
        {
            get {
                return _UserListTreeView;
            }
        }

        public Gtk.Entry TopicEntry
        {
            get {
                return _TopicEntry;
            }
        }
        
        public Gtk.Menu TabMenu
        {
            get {
                return _TabMenu;
            }
        }
        
        public ChannelPage(Engine.Page epage) : base(epage)
        {
            _Label = new Gtk.Label(epage.Name);
            _LabelEventBox.Add(_Label);
            _Label.Show();
            
            // userlist
            Gtk.ScrolledWindow sw = null;
            string userlist_pos = (string)Frontend.UserConfig["Interface/Notebook/Channel/UserListPosition"];
            if ((userlist_pos == "left") ||
                (userlist_pos == "right")) {
               sw = new Gtk.ScrolledWindow();
               sw.WidthRequest = 120;
               
               Gtk.TreeView tv = new Gtk.TreeView();
               tv.CanFocus = false;
               tv.BorderWidth = 0;
               sw.Add(tv);
               _UserListTreeView = tv;
               
               Gtk.TreeViewColumn statuscolumn;
               statuscolumn = new Gtk.TreeViewColumn("", new Gtk.CellRendererText(), "text", 0);
               statuscolumn.SortColumnId = 0;
               statuscolumn.Spacing = 0;
               statuscolumn.SortIndicator = false;
               statuscolumn.Sizing = Gtk.TreeViewColumnSizing.Autosize;
               
               Gtk.TreeViewColumn usercolumn;
               usercolumn = new Gtk.TreeViewColumn("Users (0)", new Gtk.CellRendererText(), "text", 1);
               usercolumn.SortColumnId = 1;
               usercolumn.Spacing = 0;
               usercolumn.SortIndicator = false;
               usercolumn.Sizing = Gtk.TreeViewColumnSizing.Autosize;
               
               Gtk.ListStore liststore = new Gtk.ListStore(typeof(string), typeof(string));
               liststore.SetSortColumnId(0, Gtk.SortType.Ascending);
#if GTK_1
               liststore.SetSortFunc(0, new Gtk.TreeIterCompareFunc(_OnStatusSort), IntPtr.Zero, new Gtk.DestroyNotify(_OnDestroyNotify));
               liststore.SetSortFunc(1, new Gtk.TreeIterCompareFunc(_OnUsersListSort), IntPtr.Zero, new Gtk.DestroyNotify(_OnDestroyNotify));
#elif GTK_2
               liststore.SetSortFunc(0, new Gtk.TreeIterCompareFunc(_OnStatusSort));
               liststore.SetSortFunc(1, new Gtk.TreeIterCompareFunc(_OnUsersListSort));
#endif
               
               tv.Model = liststore;
               tv.AppendColumn(statuscolumn);
               tv.AppendColumn(usercolumn);
            } else if (userlist_pos == "none") {
            } else {
#if LOG4NET
                Logger.Main.Error("ChannelPage() unknown value in Interface/Notebook/Channel/UserListPosition: "+userlist_pos);
#endif
            }
            
            // right box
            Gtk.VBox vbox = new Gtk.VBox();
            
            string topic_pos = (string)Frontend.UserConfig["Interface/Notebook/Channel/TopicPosition"];
            if (topic_pos == "top" || topic_pos == "bottom") {
                Gtk.Entry topic = new Gtk.Entry();
                topic.Editable = false;
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
                Logger.Main.Error("ChannelPage() unknown value in Interface/Notebook/Channel/TopicPosition: "+topic_pos);
#endif
            }
            
            if (userlist_pos == "left" || userlist_pos == "right") { 
                Gtk.HPaned hpaned = new Gtk.HPaned();
                if (userlist_pos == "left") {
                    hpaned.Pack1(sw, false, false);
                    hpaned.Pack2(vbox, true, true);
                } else {
                    hpaned.Pack1(vbox, true, true);
                    hpaned.Pack2(sw, false, false);
                }
                Add(hpaned);
            } else {
                Add(vbox);
            }
            
            // popup menu
            Gtk.AccelGroup agrp = new Gtk.AccelGroup();
            Frontend.MainWindow.AddAccelGroup(agrp);
            _TabMenu = new Gtk.Menu();
            Gtk.ImageMenuItem image_item = new Gtk.ImageMenuItem(Gtk.Stock.Close, agrp);
            image_item.Activated += new EventHandler(_OnTabMenuCloseActivated);  
            _TabMenu.Append(image_item);
            
            _LabelEventBox.ButtonPressEvent += new Gtk.ButtonPressEventHandler(_OnTabButtonPress);
        }
        
        static private int _OnStatusSort(Gtk.TreeModel model, Gtk.TreeIter itera, Gtk.TreeIter iterb)
        {
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
    
        static private int _OnUsersListSort(Gtk.TreeModel model, Gtk.TreeIter itera, Gtk.TreeIter iterb)
        {
            Gtk.ListStore liststore = (Gtk.ListStore)model;
            // nickname
            string column2a = (string)liststore.GetValue(itera, 1);
            string column2b = (string)liststore.GetValue(iterb, 1);
            
            return String.Compare(column2a, column2b, true, CultureInfo.InvariantCulture);
        }
        
        private void _OnTabButtonPress(object obj, Gtk.ButtonPressEventArgs args)
        {
#if LOG4NET
            Logger.UI.Debug("_OnTabButtonPress triggered");
#endif

            if (args.Event.Button == 3) {
                _TabMenu.Popup(null, null, null, IntPtr.Zero, args.Event.Button, args.Event.Time);
                _TabMenu.ShowAll();
            }
        }
        
        private void _OnTabMenuCloseActivated(object sender, EventArgs e)
        {
            if (EnginePage.NetworkManager is IrcManager) {
                IrcManager imanager = (IrcManager)EnginePage.NetworkManager;
                imanager.CommandPart(new CommandData(Frontend.FrontendManager,
                                            EnginePage.Name));
            }
        }
        
        private void _OnUserListButtonPress(object obj, Gtk.ButtonPressEventArgs args)
        {
#if LOG4NET
            Logger.UI.Debug("_OnUserListButtonPress triggered");
#endif

            if (args.Event.Button == 3) {
                _UserListMenu.Popup(null, null, null, IntPtr.Zero, args.Event.Button, args.Event.Time);
                _UserListMenu.ShowAll();
            }
        }
        
#if GTK_1
        static private void _OnDestroyNotify()
        {
            // noop
        }
#endif
    }
}
