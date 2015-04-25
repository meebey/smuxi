/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006, 2009-2014 Mirco Bauer <meebey@meebey.net>
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
using System.Linq;
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
        public override IList<PersonModel> Participants { get; protected set; }
        protected Gtk.CellRendererText IdentityNameCellRenderer { get; set; }
        Gtk.ScrolledWindow PersonScrolledWindow { get; set; }

        public event EventHandler ParticipantsChanged;

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

        public Gtk.HPaned OutputHPaned {
            get {
                return _OutputHPaned;
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
            Participants = new List<PersonModel>();
            _OutputHPaned = new Gtk.HPaned();

            Gtk.TreeView tv = new Gtk.TreeView();
            _PersonTreeView = tv;
            Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
            PersonScrolledWindow = sw;
            sw.ShadowType = Gtk.ShadowType.None;
            sw.HscrollbarPolicy = Gtk.PolicyType.Never;

            //tv.CanFocus = false;
            tv.BorderWidth = 0;
            tv.Selection.Mode = Gtk.SelectionMode.Multiple;
            sw.Add(tv);
            
            Gtk.TreeViewColumn column;
            var cellr = new Gtk.CellRendererText() {
                Ellipsize = Pango.EllipsizeMode.End
            };
            IdentityNameCellRenderer = cellr;
            column = new Gtk.TreeViewColumn(String.Empty, cellr);
            column.SortColumnId = 0;
            column.Spacing = 0;
            column.SortIndicator = false;
            column.Expand = true;
            column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
            // FIXME: this callback leaks memory
            column.SetCellDataFunc(cellr, new Gtk.TreeCellDataFunc(RenderPersonIdentityName));
            tv.AppendColumn(column);
            _IdentityNameColumn = column;
            
            Gtk.ListStore liststore = new Gtk.ListStore(typeof(PersonModel));
            liststore.SetSortColumnId(0, Gtk.SortType.Ascending);
            liststore.SetSortFunc(0, new Gtk.TreeIterCompareFunc(SortPersonListStore));
            _PersonListStore = liststore;
            
            tv.Model = liststore;
            tv.SearchColumn = 0;
            tv.SearchEqualFunc = (model, col, key, iter) => {
                var person = (PersonModel) model.GetValue(iter, col);
                // Ladies and gentlemen welcome to C
                // 0 means it matched but 0 as bool is false. So if it matches
                // we have to return false. Still not clear? true is false and
                // false is true, weirdo! If you think this is retarded,
                // yes it is.
                return !person.IdentityName.StartsWith(key, StringComparison.InvariantCultureIgnoreCase);
            };
            tv.EnableSearch = true;
            tv.HeadersVisible = false;
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
            _PersonTreeViewFrame = new Gtk.Frame() {
                ShadowType = Gtk.ShadowType.In
            };
            _PersonTreeViewFrame.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler(_OnUserListButtonReleaseEvent);
            _PersonTreeViewFrame.Add(sw);
            
            // topic
            // don't worry, ApplyConfig() will add us to the OutputVBox!
            _OutputVBox = new Gtk.VBox() {
                Spacing = 1
            };

            _TopicTextView = new MessageTextView();
            _TopicTextView.Editable = false;
            _TopicTextView.WrapMode = Gtk.WrapMode.WordChar;
            _TopicScrolledWindow = new Gtk.ScrolledWindow();
            _TopicScrolledWindow.ShadowType = Gtk.ShadowType.In;
            _TopicScrolledWindow.HscrollbarPolicy = Gtk.PolicyType.Never;
            _TopicScrolledWindow.Add(_TopicTextView);
            // make sure the topic is invisible and remains by default and
            // visible when a topic gets set
            _TopicScrolledWindow.ShowAll();
            _TopicScrolledWindow.Visible = false;
            _TopicScrolledWindow.NoShowAll = true;
            _TopicScrolledWindow.SizeRequested += delegate(object o, Gtk.SizeRequestedArgs args) {
                // predict and set useful topic heigth
                int lineWidth, lineHeight;
                using (var layout = _TopicTextView.CreatePangoLayout("Test Topic")) {
                    layout.GetPixelSize(out lineWidth, out lineHeight);
                }
                var lineSpacing = _TopicTextView.PixelsAboveLines +
                                  _TopicTextView.PixelsBelowLines;
                var it = _TopicTextView.Buffer.StartIter;
                int newLines = 1;
                // move to end of next visual line
                while (_TopicTextView.ForwardDisplayLineEnd(ref it)) {
                    newLines++;
                    // calling ForwardDisplayLineEnd repeatedly stays on the same position
                    // therefor we move one cursor position further
                    it.ForwardCursorPosition();
                }
                newLines = Math.Min(newLines, 3);
                var bestSize = new Gtk.Requisition() {
                    Height = ((lineHeight + lineSpacing) * newLines) + 4
                };
                args.Requisition = bestSize;
            };

            Add(_OutputHPaned);
            
            //ApplyConfig(Frontend.UserConfig);
            
            ShowAll();
        }

        protected GroupChatView(IntPtr handle) : base(handle)
        {
        }

        public override void Dispose()
        {
            Trace.Call();

            // HACK: this shouldn't be needed but GTK# keeps GC handles
            // these callbacks for some reason and thus leaks :(
            // release ListStore.SetSortFunc() callback
            // gtk_list_store_finalize() -> _gtk_tree_data_list_header_free() -> destroy(user_data);
            _TopicTextView.Dispose();
            _PersonListStore.Dispose();
            // release TreeViewColumn.SetCellDataFunc() callback
            // gtk_tree_view_column_finalize -> GtkTreeViewColumnCellInfo -> info->destroy(info->func_data)
            _IdentityNameColumn.Dispose();

            base.Dispose();
        }

        public override void Disable()
        {
            Trace.Call();
            
            base.Disable();
            
            _TopicTextView.Buffer.Text = String.Empty;
            _PersonListStore.Clear();
            OnParticipantsChanged(EventArgs.Empty);
        }
        
        public override void Sync(int msgCount)
        {
            Trace.Call(msgCount);

            GLib.Idle.Add(delegate {
                TabImage.SetFromStock(Gtk.Stock.Refresh, Gtk.IconSize.Menu);
                OnStatusChanged(EventArgs.Empty);
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

            base.Sync(msgCount);
        }

        public override void Populate()
        {
            Trace.Call();

            // sync persons
            var persons = SyncedPersons;
            if (_PersonTreeView != null && persons != null) {
                // HACK: out of scope
                string status = String.Format(
                                    _("Retrieving user list for {0}..."),
                                    SyncedName);
                Frontend.MainWindow.Status = status;
    
                Gtk.ListStore ls = (Gtk.ListStore) _PersonTreeView.Model;
                // cleanup, be sure the list is empty
                ls.Clear();
                // detach the model (less CPU load)
                _PersonTreeView.Model = new Gtk.ListStore(typeof(PersonModel));
                Participants = new List<PersonModel>();
                string longestName = String.Empty;
                foreach (var person in persons.Values.OrderBy(x => x)) {
                    ls.AppendValues(person);
                    
                    if (person.IdentityName.Length > longestName.Length) {
                        longestName = person.IdentityName;
                    }
                    Participants.Add(person);
                }
                // attach the model again
                // BUG? TreeView doesn't seem to recognize existing values in the model?!?
                // see: http://www.smuxi.org/issues/show/132
                _PersonTreeView.Model = ls;
                _PersonTreeView.SearchColumn = 0;

                OnParticipantsChanged(EventArgs.Empty);

                // TRANSLATOR: this string will be appended to the one above
                status += String.Format(" {0}", _("done."));
                Frontend.MainWindow.Status = status;
            }
            SyncedPersons = null;

            Topic = SyncedTopic;

            base.Populate();
        }

        public override void AddMessage(MessageModel msg)
        {
            base.AddMessage(msg);

            var nick = msg.GetNick();
            if (nick == null) {
                return;
            }

            // update who spoke last
            for (int i = 0; i < Participants.Count; ++i) {
                var speaker = Participants[i];
                if (speaker.IdentityName == nick) {
                    Participants.RemoveAt(i);
                    Participants.Insert(0, speaker);
                    break;
                }
            }
        }
        
        public void AddPerson(PersonModel person)
        {
            Trace.Call(person);
            
            if (_PersonListStore == null) {
                // no liststore, nothing todo
                return;
            }
            
            _PersonListStore.AppendValues(person);
            Participants.Add(person);
            OnParticipantsChanged(EventArgs.Empty);
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

            for (int i = 0; i < Participants.Count; ++i) {
                if (Participants[i].ID == oldPerson.ID) {
                    Participants[i] = newPerson;
                    break;
                }
            }
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

            for (int i = 0; i < Participants.Count; ++i) {
                if (Participants[i].ID == person.ID) {
                    Participants.RemoveAt(i);
                    break;
                }
            }

            OnParticipantsChanged(EventArgs.Empty);
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
            string topic_pos = (string) config["Interface/Notebook/Channel/TopicPosition"];
            if (_TopicScrolledWindow.IsAncestor(_OutputVBox)) {
                _OutputVBox.Remove(_TopicScrolledWindow);
            }
            if (OutputScrolledWindow.IsAncestor(_OutputVBox)) {
                _OutputVBox.Remove(OutputScrolledWindow);
            }
            if (topic_pos == "top") {
                _OutputVBox.PackStart(_TopicScrolledWindow, false, false, 0);
                _OutputVBox.PackStart(OutputScrolledWindow, true, true, 0);
            } else if  (topic_pos == "bottom") {
                _OutputVBox.PackStart(OutputScrolledWindow, true, true, 0);
                _OutputVBox.PackStart(_TopicScrolledWindow, false, false, 0);
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
            if (userlist_pos == "left") {
                userlist_pos = "right";
            }
            if (_PersonTreeViewFrame.IsAncestor(_OutputHPaned)) {
                _OutputHPaned.Remove(_PersonTreeViewFrame);
            }
            if (_OutputVBox.IsAncestor(_OutputHPaned)) {
                _OutputHPaned.Remove(_OutputVBox);
            }
            if (userlist_pos == "left") {
                _OutputHPaned.Pack1(_PersonTreeViewFrame, false, true);
                _OutputHPaned.Pack2(_OutputVBox, true, true);
            } else if (userlist_pos == "right") {
                _OutputHPaned.Pack1(_OutputVBox, true, false);
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
                builder.Settings.NickColors = true;
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

        protected virtual void OnParticipantsChanged(EventArgs e)
        {
            if (ParticipantsChanged != null) {
                ParticipantsChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnPersonsRowActivated(object sender, Gtk.RowActivatedArgs e)
        {
            Trace.Call(sender, e);

            IList<PersonModel> persons = GetSelectedPersons();
            if (persons == null || persons.Count == 0) {
                return;
            }

            var protocolManager = ProtocolManager;
            if (protocolManager == null) {
#if LOG4NET
                _Logger.WarnFormat(
                    "{0}.OnPersonsRowActivated(): ProtocolManager is null, " +
                    "bailing out!", this
                );
#endif
                return;
            }

            // jump to person chat if available
            foreach (var chatView in Frontend.MainWindow.ChatViewManager.Chats) {
                if (!(chatView is PersonChatView)) {
                    continue;
                }
                var personChatView = (PersonChatView) chatView;
                if (personChatView.PersonModel == persons[0]) {
                    Frontend.MainWindow.ChatViewManager.CurrentChatView = personChatView;
                    return;
                }
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
                        protocolManager.OpenChat(
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
                if (model.GetIter(out iter, path)) {
                    persons.Add((PersonModel) model.GetValue(iter, 0));
                }
            }
            
            return persons;
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}

