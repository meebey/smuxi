/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006, 2009-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Threading;
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
        public static Gdk.Pixbuf   IconPixbuf { get; private set; }
        private bool               NickColors { get; set; }
        private GroupChatModel     _GroupChatModel;
        private Gtk.ScrolledWindow _PersonScrolledWindow;
        private Gtk.TreeView       _PersonTreeView;
        private Gtk.ListStore      _PersonListStore;
        private Gtk.Menu           _PersonMenu;
        private Gtk.VBox           _OutputVBox;
        private Gtk.Frame          _PersonTreeViewFrame;
        private Gtk.HPaned         _OutputHPaned;
        private Gtk.ScrolledWindow _TopicScrolledWindow;
        private MessageTextView    _TopicTextView;
        private MessageModel       _Topic;
        private Gtk.TreeViewColumn _IdentityNameColumn;
        IDictionary<string, PersonModel> SyncedPersons { get; set; }
        MessageModel                     SyncedTopic  { get; set; }

        public override bool HasSelection {
            get {
                return base.HasSelection ||
                       _PersonTreeView.Selection.CountSelectedRows() > 0 ||
                       _TopicTextView.HasTextViewSelection;
            }
        }

        public override bool HasFocus {
            get {
                return base.HasFocus ||
                       _PersonTreeView.HasFocus ||
                       _TopicTextView.HasFocus;
            }
        }

        public MessageModel Topic {
            get {
                return _Topic;
            }
            set {
                _Topic = value;
                _TopicTextView.Clear();
                if (value != null) {
                    _TopicTextView.AddMessage(value, false);
                }
                _TopicScrolledWindow.Visible = !_TopicTextView.IsEmpty;
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

        protected override Gtk.Image DefaultTabImage {
            get {
                return new Gtk.Image(IconPixbuf);
            }
        }

        static GroupChatView()
        {
            IconPixbuf = Frontend.LoadIcon(
                "smuxi-group-chat", 16, "group-chat_256x256.png"
            );
        }

        public GroupChatView(GroupChatModel groupChat) : base(groupChat)
        {
            Trace.Call(groupChat);
            
            _GroupChatModel = groupChat;
            
            // person list
            _OutputHPaned = new Gtk.HPaned();
            
            Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
            _PersonScrolledWindow = sw;
            //sw.WidthRequest = 150;
            sw.HscrollbarPolicy = Gtk.PolicyType.Never;
            
            Gtk.TreeView tv = new Gtk.TreeView();
            _PersonTreeView = tv;
            //tv.CanFocus = false;
            tv.BorderWidth = 0;
            tv.Selection.Mode = Gtk.SelectionMode.Multiple;
            sw.Add(tv);
            
            Gtk.TreeViewColumn column;
            Gtk.CellRendererText cellr = new Gtk.CellRendererText();
            cellr.WidthChars = 12;
            column = new Gtk.TreeViewColumn(String.Empty, cellr);
            column.SortColumnId = 0;
            column.Spacing = 0;
            column.SortIndicator = false;
            column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
            column.SetCellDataFunc(cellr, new Gtk.TreeCellDataFunc(RenderPersonIdentityName));
            tv.AppendColumn(column);
            _IdentityNameColumn = column;
            
            Gtk.ListStore liststore = new Gtk.ListStore(typeof(PersonModel));
            liststore.SetSortColumnId(0, Gtk.SortType.Ascending);
            liststore.SetSortFunc(0, new Gtk.TreeIterCompareFunc(SortPersonListStore));
            _PersonListStore = liststore;
            
            tv.Model = liststore;
            tv.RowActivated += new Gtk.RowActivatedHandler(OnPersonsRowActivated);
            tv.FocusOutEvent += OnPersonTreeViewFocusOutEvent;
            
            // popup menu
            _PersonMenu = new Gtk.Menu();
            // don't loose the focus else we lose the selection too!
            // see OnPersonTreeViewFocusOutEvent()
            _PersonMenu.TakeFocus = false;
            _PersonMenu.Shown += OnPersonMenuShown;
            
            _PersonTreeView.ButtonPressEvent += _OnPersonTreeViewButtonPressEvent;
            _PersonTreeView.KeyPressEvent += OnPersonTreeViewKeyPressEvent;
            // frame needed for events when selecting something in the treeview
            _PersonTreeViewFrame = new Gtk.Frame();
            _PersonTreeViewFrame.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler(_OnUserListButtonReleaseEvent);
            _PersonTreeViewFrame.Add(sw);
            
            // topic
            // don't worry, ApplyConfig() will add us to the OutputVBox!
            _OutputVBox = new Gtk.VBox();

            _TopicTextView = new MessageTextView();
            _TopicTextView.Editable = false;
            _TopicTextView.WrapMode = Gtk.WrapMode.WordChar;
            _TopicScrolledWindow = new Gtk.ScrolledWindow();
            _TopicScrolledWindow.ShadowType = Gtk.ShadowType.In;
            // when using PolicyType.Never, it will try to grow but never shrinks!
            _TopicScrolledWindow.HscrollbarPolicy = Gtk.PolicyType.Automatic;
            _TopicScrolledWindow.VscrollbarPolicy = Gtk.PolicyType.Automatic;
            _TopicScrolledWindow.Add(_TopicTextView);
            // make sure the topic is invisible and remains by default and
            // visible when a topic gets set
            _TopicScrolledWindow.ShowAll();
            _TopicScrolledWindow.Visible = false;
            _TopicScrolledWindow.NoShowAll = true;

            Add(_OutputHPaned);
            
            //ApplyConfig(Frontend.UserConfig);
            
            ShowAll();
        }
        
        public override void Disable()
        {
            Trace.Call();
            
            base.Disable();
            
            _TopicTextView.Buffer.Text = String.Empty;
            _PersonListStore.Clear();
            UpdatePersonCount();
        }
        
        public override void Sync()
        {
            Trace.Call();

            GLib.Idle.Add(delegate {
                TabImage.SetFromStock(Gtk.Stock.Refresh, Gtk.IconSize.Menu);
                return false;
            });

#if LOG4NET
            _Logger.Debug("Sync() syncing persons");
#endif
            // REMOTING CALL 1
            SyncedPersons = _GroupChatModel.Persons;
            if (SyncedPersons == null) {
                SyncedPersons = new Dictionary<string, PersonModel>(0);
            }

#if LOG4NET
            _Logger.Debug("Sync() syncing topic");
#endif
            // REMOTING CALL 2
            SyncedTopic = _GroupChatModel.Topic;

            base.Sync();
        }

        public override void Populate()
        {
            Trace.Call();

            // sync persons
            if (_PersonTreeView != null) {
                // HACK: out of scope
                string status = String.Format(
                                    _("Retrieving user list for {0}..."),
                                    SyncedName);
                Frontend.MainWindow.Statusbar.Push(0, status);
    
                Gtk.ListStore ls = (Gtk.ListStore) _PersonTreeView.Model;
                // cleanup, be sure the list is empty
                ls.Clear();
                // detach the model (less CPU load)
                _PersonTreeView.Model = new Gtk.ListStore(typeof(PersonModel));
                string longestName = String.Empty;
                foreach (PersonModel person in SyncedPersons.Values) {
                    ls.AppendValues(person);
                    
                    if (person.IdentityName.Length > longestName.Length) {
                        longestName = person.IdentityName;
                    }
                }
                // attach the model again
                // BUG? TreeView doesn't seem to recognize existing values in the model?!?
                // see: http://www.smuxi.org/issues/show/132
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
               
                // TRANSLATOR: this string will be appended to the one above
                status += String.Format(" {0}", _("done."));
                Frontend.MainWindow.Statusbar.Push(0, status);
            }

            Topic = SyncedTopic;

            base.Populate();
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

            // topic
            _TopicTextView.ApplyConfig(config);
            // predict and set useful topic heigth
            Pango.Layout layout = _TopicTextView.CreatePangoLayout("Test Topic");
            int lineWidth, lineHeigth;
            layout.GetPixelSize(out lineWidth, out lineHeigth);
            // use 2 lines + a bit extra as the topic heigth
            int bestHeigth = (lineHeigth * 2) + 5;
            _TopicTextView.HeightRequest = bestHeigth;
            _TopicScrolledWindow.HeightRequest = bestHeigth;

            string topic_pos = (string) config["Interface/Notebook/Channel/TopicPosition"];
            if (_TopicScrolledWindow.IsAncestor(_OutputVBox)) {
                _OutputVBox.Remove(_TopicScrolledWindow);
            }
            if (OutputScrolledWindow.IsAncestor(_OutputVBox)) {
                _OutputVBox.Remove(OutputScrolledWindow);
            }
            if (topic_pos == "top") {
                _OutputVBox.PackStart(_TopicScrolledWindow, false, false, 2);
                _OutputVBox.PackStart(OutputScrolledWindow, true, true, 0);
            } else if  (topic_pos == "bottom") {
                _OutputVBox.PackStart(OutputScrolledWindow, true, true, 0);
                _OutputVBox.PackStart(_TopicScrolledWindow, false, false, 2);
            } else if (topic_pos == "none") {
                _OutputVBox.PackStart(OutputScrolledWindow, true, true, 0);
            } else {
#if LOG4NET
                _Logger.Error("ApplyConfig(): unsupported value in Interface/Notebook/Channel/TopicPosition: " + topic_pos);
#endif
            }
            _OutputVBox.ShowAll();

            // person list
            if (ThemeSettings.BackgroundColor == null) {
                _PersonTreeView.ModifyBase(Gtk.StateType.Normal);
            } else {
                _PersonTreeView.ModifyBase(Gtk.StateType.Normal, ThemeSettings.BackgroundColor.Value);
            }
            if (ThemeSettings.ForegroundColor == null) {
                _PersonTreeView.ModifyText(Gtk.StateType.Normal);
            } else {
                _PersonTreeView.ModifyText(Gtk.StateType.Normal, ThemeSettings.ForegroundColor.Value);
            }
            _PersonTreeView.ModifyFont(ThemeSettings.FontDescription);
            
            string userlist_pos = (string) config["Interface/Notebook/Channel/UserListPosition"];
            if (_PersonTreeViewFrame.IsAncestor(_OutputHPaned)) {
                _OutputHPaned.Remove(_PersonTreeViewFrame);
            }
            if (_OutputVBox.IsAncestor(_OutputHPaned)) {
                _OutputHPaned.Remove(_OutputVBox);
            }
            if (userlist_pos == "left") {
                _OutputHPaned.Pack1(_PersonTreeViewFrame, false, false);
                _OutputHPaned.Pack2(_OutputVBox, true, true);
            } else if (userlist_pos == "right") {
                _OutputHPaned.Pack1(_OutputVBox, true, true);
                _OutputHPaned.Pack2(_PersonTreeViewFrame, false, false);
            } else if (userlist_pos == "none") {
                _OutputHPaned.Pack1(_OutputVBox, true, true);
            } else {
#if LOG4NET
                _Logger.Error("ApplyConfig(): unsupported value in Interface/Notebook/Channel/UserListPosition: " + userlist_pos);
#endif
            }
            _OutputHPaned.ShowAll();

            NickColors = (bool) config["Interface/Notebook/Channel/NickColors"];
        }

        public virtual void RenderPersonIdentityName(Gtk.TreeViewColumn column,
                                                     Gtk.CellRenderer cellr,
                                                     Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            PersonModel person = (PersonModel) model.GetValue(iter, 0);
            var renderer = (Gtk.CellRendererText) cellr;
            if (NickColors) {
                // TODO: do we need to optimize this? it's called very often...
                Gdk.Color bgColor = _PersonTreeView.Style.Base(Gtk.StateType.Normal);
                var builder = new MessageBuilder();
                builder.NickColors = true;
                builder.AppendNick(person);
                renderer.Markup = PangoTools.ToMarkup(builder.ToMessage(),
                                                      bgColor);
            } else {
                renderer.Text = person.IdentityName;
            }
        }
       
        protected virtual int SortPersonListStore(Gtk.TreeModel model,
                                                  Gtk.TreeIter iter1,
                                                  Gtk.TreeIter iter2)
        {
            Gtk.ListStore liststore = (Gtk.ListStore) model;
            
            PersonModel person1 = (PersonModel) liststore.GetValue(iter1, 0); 
            PersonModel person2 = (PersonModel) liststore.GetValue(iter2, 0); 

            return person1.CompareTo(person2);
        }

        protected virtual void OnPersonsRowActivated(object sender, Gtk.RowActivatedArgs e)
        {
            Trace.Call(sender, e);

            IList<PersonModel> persons = GetSelectedPersons();
            if (persons == null) {
                return;
            }

            // this is a generic implemention that should be able to open/create
            // a private chat in most cases, as it depends what OpenChat()
            // of the specific protocol actually expects/needs
            foreach (PersonModel person in persons) {
                PersonChatModel personChat = new PersonChatModel(
                    person,
                    person.ID,
                    person.IdentityName,
                    null
                );

                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        ChatModel.ProtocolManager.OpenChat(
                            Frontend.FrontendManager,
                            personChat
                        );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        protected virtual void OnPersonTreeViewFocusOutEvent(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            // clear the selection when we loose the focus
            _PersonTreeView.Selection.UnselectAll();
        }
        
        protected virtual void OnPersonTreeViewKeyPressEvent(object sender, Gtk.KeyPressEventArgs e)
        {
            Trace.Call(sender, e);

            if ((e.Event.State & Gdk.ModifierType.Mod1Mask) != 0 ||
                (e.Event.State & Gdk.ModifierType.ControlMask) != 0 ||
                (e.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                // alt, ctrl or shift pushed, returning
                return;
            }

            if (e.Event.Key == Gdk.Key.Menu &&
                _PersonTreeView.Selection.CountSelectedRows() > 0) {
                _PersonMenu.Popup(null, null, null, 0, e.Event.Time);
            }
        }

        protected virtual void OnPersonMenuShown(object sender, EventArgs e)
        {
        }

        private void _OnUserListButtonReleaseEvent(object sender, Gtk.ButtonReleaseEventArgs e)
        {
            Trace.Call(sender, e);

            if (e.Event.Button == 3 && _PersonTreeView.Selection.CountSelectedRows() > 0) {
                // HACK: don't pass the real mouse button that was used to
                // initiate the menu, as sub-menus will only respond to that
                // button for some reason! As workaround we always pass
                // 0 == left mouse button here
                //_PersonMenu.Popup(null, null, null, e.Event.Button, e.Event.Time);
                _PersonMenu.Popup(null, null, null, 0, e.Event.Time);
            }
        }
        
        [GLib.ConnectBefore]
        private void _OnPersonTreeViewButtonPressEvent(object sender, Gtk.ButtonPressEventArgs e)
        {
            Trace.Call(sender, e);
            
            // If there is an existing selection prevent making a new one using
            // the right mouse button.
            // We have to check > 1 though, because you can't undo a single row selection!
            if (e.Event.Button == 3 && _PersonTreeView.Selection.CountSelectedRows() > 1) {
                e.RetVal = true;
            }
        }
        
        protected IList<PersonModel> GetSelectedPersons()
        {
            Gtk.TreeIter iter;
            Gtk.TreeModel model;
            List<PersonModel> persons = new List<PersonModel>();
            Gtk.TreePath[] paths = _PersonTreeView.Selection.GetSelectedRows(out model);
            foreach (Gtk.TreePath path in paths) {
                model.GetIter(out iter, path);
                persons.Add((PersonModel) model.GetValue(iter, 0));
            }
            
            return persons;
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}

