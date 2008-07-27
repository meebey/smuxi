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
using System.Collections.Generic;
using System.Globalization;
using Mono.Unix;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Group)]
    public class GroupChatView : ChatView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private GroupChatModel     _GroupChatModel;
        private Gtk.ScrolledWindow _PersonScrolledWindow;
        private Gtk.TreeView       _PersonTreeView;
        private Gtk.ListStore      _PersonListStore;
        private Gtk.Menu           _PersonMenu;
        private Gtk.Entry          _TopicEntry;
        private Gtk.TreeViewColumn _IdentityNameColumn;
        private Gtk.Image          _TabImage;
                
        public Gtk.Entry TopicEntry {
            get {
                return _TopicEntry;
            }
        }
        
        protected Gtk.TreeView PersonTreeView {
            get {
                return _PersonTreeView;
            }
        }
        
        protected Gtk.Menu PersonMenu {
            get {
                return _PersonMenu;
            }
        }
        
        protected Gtk.TreeViewColumn IdentityNameColumn {
            get {
                return _IdentityNameColumn;
            }
        }
        
        public GroupChatView(GroupChatModel groupChat) : base(groupChat)
        {
            Trace.Call(groupChat);
            
            _GroupChatModel = groupChat;
            
            // userlist
            Gtk.Frame frame = null;
            string userlist_pos = (string)Frontend.UserConfig["Interface/Notebook/Channel/UserListPosition"];
            if ((userlist_pos == "left") ||
                (userlist_pos == "right")) {
                Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
                //sw.WidthRequest = 150;
                sw.HscrollbarPolicy = Gtk.PolicyType.Never;
                _PersonScrolledWindow = sw;
                
                Gtk.TreeView tv = new Gtk.TreeView();
                tv.CanFocus = false;
                tv.BorderWidth = 0;
                sw.Add(tv);
                _PersonTreeView = tv;
                
                Gtk.TreeViewColumn column;
                Gtk.CellRendererText cellr = new Gtk.CellRendererText();
                cellr.WidthChars = 15;
                column = new Gtk.TreeViewColumn(String.Empty, cellr);
                column.SortColumnId = 0;
                column.Spacing = 0;
                column.SortIndicator = false;
                column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
                column.SetCellDataFunc(cellr, new Gtk.TreeCellDataFunc(_RenderPersonIdentityName));
                tv.AppendColumn(column);
                _IdentityNameColumn = column;
                
                Gtk.ListStore liststore = new Gtk.ListStore(typeof(PersonModel));
                liststore.SetSortColumnId(0, Gtk.SortType.Ascending);
                liststore.SetSortFunc(0, new Gtk.TreeIterCompareFunc(SortPersonListStore));
                _PersonListStore = liststore;
                
                tv.Model = liststore;
                tv.RowActivated += new Gtk.RowActivatedHandler(OnPersonsRowActivated);
               
                // popup menu
                _PersonMenu = new Gtk.Menu();
                
                // frame needed for events when selecting something in the treeview
                frame = new Gtk.Frame();
                frame.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler(_OnUserListButtonReleaseEvent);
                frame.Add(sw);
            } else if (userlist_pos == "none") {
            } else {
#if LOG4NET
                _Logger.Error("GroupChatView..ctor(): unknown value in Interface/Notebook/Channel/UserListPosition: "+userlist_pos);
#endif
            }
            
            // topic
            Gtk.VBox vbox = new Gtk.VBox();
            string topic_pos = (string)Frontend.UserConfig["Interface/Notebook/Channel/TopicPosition"];
            if (topic_pos == "top" || topic_pos == "bottom") {
                Gtk.Entry topic = new Gtk.Entry();
                topic.IsEditable = false;
                _TopicEntry = topic;
                if (topic_pos == "top") {
                    vbox.PackStart(topic, false, false, 2);
                    vbox.PackStart(OutputScrolledWindow, true, true, 0);
                } else {
                    vbox.PackStart(OutputScrolledWindow, true, true, 0);
                    vbox.PackStart(topic, false, false, 2);
                }
            } else if (topic_pos == "none") {
                vbox.PackStart(OutputScrolledWindow, true, true, 0);
            } else {
#if LOG4NET
                _Logger.Error("GroupChatView..ctor(): unknown value in Interface/Notebook/Channel/TopicPosition: "+topic_pos);
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
            
            _TabImage = new Gtk.Image(
                new Gdk.Pixbuf(
                    null,
                    "group-chat.svg",
                    16,
                    16
                )
            );

            TabHBox.PackStart(_TabImage, false, false, 2);
            TabHBox.ShowAll();
            
            ShowAll();
        }
        
        public override void Disable()
        {
            Trace.Call();
            
            base.Disable();
            
            _TopicEntry.Text = String.Empty;
            _PersonListStore.Clear();
            UpdatePersonCount();
        }
        
        public override void Sync()
        {
            Trace.Call();

            IDictionary<string, PersonModel> persons = _GroupChatModel.Persons; 
#if LOG4NET
            _Logger.Debug("Sync() syncing persons");
#endif
            // sync persons
            if (_PersonTreeView != null) {
                int count = persons.Count;
                /*
                if (count > 1) {
                    Frontend.MainWindow.ProgressBar.DiscreteBlocks = (uint)count;
                } else {
                    Frontend.MainWindow.ProgressBar.DiscreteBlocks = 2;
                }
                Frontend.MainWindow.ProgressBar.BarStyle = Gtk.ProgressBarStyle.Continuous;
                */
                
                // HACK: out of scope
                string status = String.Format(
                                    _("Syncing chat persons of {0}..."),
                                    ChatModel.Name);
#if UI_GNOME
                Frontend.MainWindow.Statusbar.Push(status);
#elif UI_GTK
                Frontend.MainWindow.Statusbar.Push(0, status);
#endif
    
                Gtk.ListStore ls = (Gtk.ListStore) _PersonTreeView.Model;
                // cleanup, be sure the list is empty
                ls.Clear();
                // detach the model (less CPU load)
                _PersonTreeView.Model = new Gtk.ListStore(typeof(PersonModel));
                int i = 1;
                string longestName = String.Empty;
                foreach (PersonModel person in persons.Values) {
                    ls.AppendValues(person);
                    
                    if (person.IdentityName.Length > longestName.Length) {
                        longestName = person.IdentityName;
                    }
                    
                    //Frontend.MainWindow.ProgressBar.Fraction = (double)i++ / count;
                    /*
                    // this seems to break the sync when it's remote engine is used,
                    // guess it does some other GUI processing, like removing users from
                    // the userlist....
                    while (Gtk.Application.EventsPending()) {
                        Gtk.Application.RunIteration(false);
                    }
                    */
                }
                // attach the model again
                _PersonTreeView.Model = ls;
                
                /*
                // predict and set useful width
                Console.WriteLine("longestNickname: " + longestName);
                Pango.Layout layout = _PersonScrolledWindow.CreatePangoLayout(longestName);
                //_PersonScrolledWindow.WidthRequest = layout.Width;
                Console.WriteLine("layout.Width: " + layout.Width);
                _PersonScrolledWindow.SetSizeRequest(layout.Width, 0);
                */
                
                UpdatePersonCount(); 
               
                // HACK: out of scope
                Frontend.MainWindow.ProgressBar.Fraction = 0;
                status += _(" done.");
#if UI_GNOME
                Frontend.MainWindow.Statusbar.Push(status);
#elif UI_GTK
                Frontend.MainWindow.Statusbar.Push(0, status);
#endif
            }
           
#if LOG4NET
            _Logger.Debug("Sync() syncing topic");
#endif
            // sync topic
            string topic = _GroupChatModel.Topic;
            if ((_TopicEntry != null) &&
               (topic != null)) {
                _TopicEntry.Text = topic;
            }
            
            base.Sync();
        }
        
        protected void UpdatePersonCount()
        {
            _IdentityNameColumn.Title = String.Format(_("Person") + " ({0})",
                                                      _PersonListStore.IterNChildren());
        }
        
        public void AddPerson(PersonModel person)
        {
            Trace.Call(person);
            
            if (_PersonListStore == null) {
                // no liststore, nothing todo
                return;
            }
            
            _PersonListStore.AppendValues(person);
            UpdatePersonCount();
        }
        
        public void UpdatePerson(PersonModel oldPerson, PersonModel newPerson)
        {
            Trace.Call(oldPerson, newPerson);
            
            if (_PersonListStore == null) {
                // no liststore, nothing todo
                return;
            }
            
            Gtk.TreeIter iter;
            bool res = _PersonListStore.GetIterFirst(out iter);
            if (!res) {
#if LOG4NET
                _Logger.Error("UpdatePersonModelInChannel(): _PersonsStore.GetIterFirst() returned false, ignoring update...");
#endif
                return;
            }
            
            do {
                PersonModel person = (PersonModel) _PersonListStore.GetValue(iter, 0);
                if (person.ID  == oldPerson.ID) {
                     _PersonListStore.SetValue(iter, 0, newPerson);
                    break;
                }
            } while (_PersonListStore.IterNext(ref iter));
            _PersonTreeView.CheckResize();
            //_PersonListStore.Reorder();
        }
        
        public void RemovePerson(PersonModel person)
        {
            Trace.Call(person);
            
            if (_PersonListStore == null) {
                // no liststore, nothing todo
                return;
            }
            
            Gtk.TreeIter iter;
            bool res = _PersonListStore.GetIterFirst(out iter);
            if (!res) {
#if LOG4NET
                _Logger.Error("RemovePerson(): GetIterFirst() returned false!");
#endif
                return;
            }
            
            do {
                PersonModel currentPerson = (PersonModel) _PersonListStore.GetValue(iter, 0);
                if (currentPerson.ID == person.ID) {
                    _PersonListStore.Remove(ref iter);
                    break;
                }
            } while (_PersonListStore.IterNext(ref iter));
            UpdatePersonCount();
        }
        
        public override void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);
            
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            
            base.ApplyConfig(config);
            
            // HACK: using .Equals() as Gdk.Color doesn't implement != and ==
            if (!BackgroundColor.Equals(Gdk.Color.Zero)) {
                _PersonTreeView.ModifyBase(Gtk.StateType.Normal, BackgroundColor);
                _TopicEntry.ModifyBase(Gtk.StateType.Normal, BackgroundColor);
            } else {
                _PersonTreeView.ModifyBase(Gtk.StateType.Normal);
                _TopicEntry.ModifyBase(Gtk.StateType.Normal);
            }
            
            // HACK: using .Equals() as Gdk.Color doesn't implement != and ==
            if (!ForegroundColor.Equals(Gdk.Color.Zero)) {
                _PersonTreeView.ModifyText(Gtk.StateType.Normal, ForegroundColor);
                _TopicEntry.ModifyText(Gtk.StateType.Normal, ForegroundColor);
            } else {
                _PersonTreeView.ModifyText(Gtk.StateType.Normal);
                _TopicEntry.ModifyText(Gtk.StateType.Normal);
            }
            
            _PersonTreeView.ModifyFont(FontDescription);
            _TopicEntry.ModifyFont(FontDescription);
        }

        private void _RenderPersonIdentityName(Gtk.TreeViewColumn column,
                                               Gtk.CellRenderer cellr,
                                               Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            PersonModel person = (PersonModel) model.GetValue(iter, 0);
            (cellr as Gtk.CellRendererText).Text = person.IdentityName;
        }
       
        /*
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
        */
        
        protected virtual int SortPersonListStore(Gtk.TreeModel model,
                                                  Gtk.TreeIter iter1,
                                                  Gtk.TreeIter iter2)
        {
            Gtk.ListStore liststore = (Gtk.ListStore) model;
            
            PersonModel person1 = (PersonModel) liststore.GetValue(iter1, 0); 
            PersonModel person2 = (PersonModel) liststore.GetValue(iter2, 0); 
            
            return String.Compare(person1.IdentityName, person2.IdentityName,
                                  true, CultureInfo.InvariantCulture);
        }
        
        protected virtual void OnPersonsRowActivated(object sender, Gtk.RowActivatedArgs e)
        {
            Trace.Call(sender, e);
        }
        
        private void _OnUserListButtonReleaseEvent(object sender, Gtk.ButtonReleaseEventArgs e)
        {
            Trace.Call(sender, e);

            if (e.Event.Button == 3) {
                _PersonMenu.Popup(null, null, null, e.Event.Button, e.Event.Time);
                _PersonMenu.ShowAll();
            }
        }
        
        protected PersonModel GetSelectedPerson()
        {
            Gtk.TreeIter iter;
            Gtk.TreeModel model;
            if (_PersonTreeView.Selection.GetSelected(out model, out iter)) {
                return (PersonModel) model.GetValue(iter, 0);
            }
            
            return null;
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}

