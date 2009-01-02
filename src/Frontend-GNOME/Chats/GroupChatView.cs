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
        private Gtk.VBox           _OutputVBox;
        private Gtk.Frame          _PersonTreeViewFrame;
        private Gtk.HPaned         _OutputHPaned;
        private Gtk.ScrolledWindow _TopicScrolledWindow;
        private Gtk.TextView       _TopicTextView;
        private Gtk.TextTagTable   _TopicTextTagTable;
        private Gtk.TreeViewColumn _IdentityNameColumn;
        private Gtk.Image          _TabImage;
        
        public Gtk.ScrolledWindow TopicScrolledWindow {
            get {
                return _TopicScrolledWindow;
            }
        }

        public Gtk.TextView TopicTextView {
            get {
                return _TopicTextView;
            }
        }

        protected Gtk.TextTagTable TopicTextTagTable {
            get {
                return _TopicTextTagTable;
            }
        }

        public override bool HasSelection {
            get {
                return base.HasSelection || _PersonTreeView.Selection.CountSelectedRows() > 0;
            }
        }
        
        public override bool HasFocus {
            get {
                return base.HasFocus || _PersonTreeView.HasFocus;
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
            tv.FocusOutEvent += OnPersonTreeViewFocusOutEvent;
            
            // popup menu
            _PersonMenu = new Gtk.Menu();
            // don't loose the focus else we lose the selection too!
            // see OnPersonTreeViewFocusOutEvent()
            _PersonMenu.TakeFocus = false;
            
            _PersonTreeView.ButtonPressEvent += _OnPersonTreeViewButtonPressEvent;
            // frame needed for events when selecting something in the treeview
            _PersonTreeViewFrame = new Gtk.Frame();
            _PersonTreeViewFrame.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler(_OnUserListButtonReleaseEvent);
            _PersonTreeViewFrame.Add(sw);
            
            // topic
            _OutputVBox = new Gtk.VBox();

            _TopicTextView = new Gtk.TextView();
            _TopicTextView.Editable = false;
            _TopicTextView.WrapMode = Gtk.WrapMode.WordChar;
            
            _TopicScrolledWindow = new Gtk.ScrolledWindow();
            _TopicScrolledWindow.HscrollbarPolicy = Gtk.PolicyType.Never;
            _TopicScrolledWindow.VscrollbarPolicy = Gtk.PolicyType.Automatic;
            _TopicScrolledWindow.Add(_TopicTextView);
            
            _TopicTextTagTable = new Gtk.TextTagTable();
            _TopicTextTagTable = base.OutputTextTagTable;
            
            Add(_OutputHPaned);
            
            ApplyConfig(Frontend.UserConfig);
            
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
            
            _TopicTextView.Buffer.Text = String.Empty;
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
            MessageModel topic = _GroupChatModel.Topic;
            if ((_TopicTextView.Buffer != null) &&
               (topic != null)) {
                // XXX
                SetTopic(topic);
                _TopicTextView.Buffer.Text = topic.ToString();
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
        
        public void SetTopic(MessageModel topic)
        {
            Trace.Call(topic);
            _TopicTextView.Buffer = new Gtk.TextBuffer(IntPtr.Zero);
            Gtk.TextIter iter = _TopicTextView.Buffer.EndIter;

            foreach (MessagePartModel topicPart in topic.MessageParts) {
#if LOG4NET
                _Logger.Debug("SetTopic(): topicPart.GetType(): " + topicPart.GetType());
#endif
                Gdk.Color bgColor = _TopicTextView.DefaultAttributes.Appearance.BgColor;
//                if (_BackgroundColor != null) {
//                    bgColor = _BackgroundColor.Value;
//                }
                TextColor bgTextColor = ColorTools.GetTextColor(bgColor);
                // TODO: implement all types
                if (topicPart is UrlMessagePartModel) {
                    UrlMessagePartModel fmsgui = (UrlMessagePartModel) topicPart;
                    // HACK: the engine should set a color for us!
                    Gtk.TextTag urlTag = _TopicTextTagTable.Lookup("url");
                    Gdk.Color urlColor = urlTag.ForegroundGdk;
                    //Console.WriteLine("urlColor: " + urlColor);
                    TextColor urlTextColor = ColorTools.GetTextColor(urlColor);
                    urlTextColor = ColorTools.GetBestTextColor(urlTextColor, bgTextColor);
                    //Console.WriteLine("GetBestTextColor({0}, {1}): {2}",  urlColor, bgTextColor, urlTextColor);
                    urlTag.ForegroundGdk = ColorTools.GetGdkColor(urlTextColor);
                    _TopicTextView.Buffer.InsertWithTagsByName(ref iter, fmsgui.Url, "url");
                } else if (topicPart is TextMessagePartModel) {
                    TextMessagePartModel fmsgti = (TextMessagePartModel) topicPart;
#if LOG4NET
                    _Logger.Debug("SetTopic(): fmsgti.Text: '" + fmsgti.Text + "'");
#endif
                    List<string> tags = new List<string>();
                    if (fmsgti.ForegroundColor != TextColor.None) {
                        TextColor color = ColorTools.GetBestTextColor(fmsgti.ForegroundColor, bgTextColor);
                        //Console.WriteLine("GetBestTextColor({0}, {1}): {2}",  fmsgti.ForegroundColor, bgTextColor, color);
                        string tagname = _GetTextTagName(color, null);
                        //string tagname = _GetTextTagName(fmsgti.ForegroundColor, null);
                        tags.Add(tagname);
                    }
                    if (fmsgti.BackgroundColor != TextColor.None) {
                        string tagname = _GetTextTagName(null, fmsgti.BackgroundColor);
                        tags.Add(tagname);
                    }
                    if (fmsgti.Underline) {
#if LOG4NET
                        _Logger.Debug("SetTopic(): fmsgti.Underline is true");
#endif
                        tags.Add("underline");
                    }
                    if (fmsgti.Bold) {
#if LOG4NET
                        _Logger.Debug("SetTopic(): fmsgti.Bold is true");
#endif
                        tags.Add("bold");
                    }
                    if (fmsgti.Italic) {
#if LOG4NET
                        _Logger.Debug("SetTopic(): fmsgti.Italic is true");
#endif
                        tags.Add("italic");
                    }
                    
                    _TopicTextView.Buffer.InsertWithTagsByName(ref iter,
                                                               fmsgti.Text,
                                                               tags.ToArray());
                } 
            }
            _TopicTextView.Buffer.Insert(ref iter, "\n");
        }

        public override void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);
            
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            
            base.ApplyConfig(config);
            
            if (BackgroundColor != null) {
                _PersonTreeView.ModifyBase(Gtk.StateType.Normal, BackgroundColor.Value);
                _TopicTextView.ModifyBase(Gtk.StateType.Normal, BackgroundColor.Value);
            } else {
                _PersonTreeView.ModifyBase(Gtk.StateType.Normal);
                _TopicTextView.ModifyBase(Gtk.StateType.Normal);
            }
            
            if (ForegroundColor != null) {
                _PersonTreeView.ModifyText(Gtk.StateType.Normal, ForegroundColor.Value);
                _TopicTextView.ModifyText(Gtk.StateType.Normal, ForegroundColor.Value);
            } else {
                _PersonTreeView.ModifyText(Gtk.StateType.Normal);
                _TopicTextView.ModifyText(Gtk.StateType.Normal);
            }
            
            _PersonTreeView.ModifyFont(FontDescription);
            _TopicTextView.ModifyFont(FontDescription);
            
            // topic
            string topic_pos = (string) config["Interface/Notebook/Channel/TopicPosition"];
            if (_TopicTextView.IsAncestor(_OutputVBox)) {
                _OutputVBox.Remove(_TopicTextView);
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
        }

        private void _RenderPersonIdentityName(Gtk.TreeViewColumn column,
                                               Gtk.CellRenderer cellr,
                                               Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            PersonModel person = (PersonModel) model.GetValue(iter, 0);
            (cellr as Gtk.CellRendererText).Text = person.IdentityName;
        }
       
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
        
        protected virtual void OnPersonTreeViewFocusOutEvent(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            // clear the selection when we loose the focus
            _PersonTreeView.Selection.UnselectAll();
        }
        
        private void _OnUserListButtonReleaseEvent(object sender, Gtk.ButtonReleaseEventArgs e)
        {
            Trace.Call(sender, e);

            if (e.Event.Button == 3 && _PersonTreeView.Selection.CountSelectedRows() > 0) {
                _PersonMenu.Popup(null, null, null, e.Event.Button, e.Event.Time);
                _PersonMenu.ShowAll();
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

