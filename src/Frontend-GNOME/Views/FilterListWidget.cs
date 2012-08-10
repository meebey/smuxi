// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
// 
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

using System;
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class FilterListWidget : Gtk.Bin
    {
        Gtk.Window           f_Parent { get; set; }
        Gtk.ListStore        f_ListStore { get; set; }
        FilterListController f_Controller { get; set; }
        Gtk.ListStore        f_ChatTypeListStore { get; set; }
        Gtk.ListStore        f_MessageTypeListStore { get; set; }
        Gtk.ListStore        f_ProtocolListStore { get; set; }

        public event EventHandler Changed;

        public FilterListWidget(Gtk.Window parent, UserConfig userConfig)
        {
            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }

            Build();
            Init();

            f_Parent = parent;
            f_Controller = new FilterListController(userConfig);
        }

        public void InitProtocols(IList<string> protocols)
        {
            Trace.Call(protocols);

            if (protocols == null) {
                throw new ArgumentNullException("protocols");
            }

            f_ProtocolListStore.Clear();

            f_ProtocolListStore.AppendValues(String.Empty);
            foreach (string protocol in protocols) {
                f_ProtocolListStore.AppendValues(protocol);
            }
            f_ProtocolListStore.SetSortColumnId(0, Gtk.SortType.Ascending);
        }

        public void Load()
        {
            Trace.Call();

            f_ListStore.Clear();

            var filters = f_Controller.GetFilterList();
            foreach (var filter in filters) {
                f_ListStore.AppendValues(filter.Value, filter.Key);
            }

            f_TreeView.ColumnsAutosize();
        }

        public void Save()
        {
            Trace.Call();

            // search for removed filters
            foreach (var filterPair in f_Controller.GetFilterList()) {
                bool removed = true;
                foreach (object[] row in f_ListStore) {
                    if ((int) row[1] == filterPair.Key) {
                        removed = false;
                        break;
                    }
                }
                if (removed) {
                    f_Controller.RemoveFilter(filterPair.Key);
                }
            }

            Gtk.TreeIter iter;
            if (!f_ListStore.GetIterFirst(out iter)) {
                // empty list, nothing to do
                return;
            }
            do {
                var filter = (FilterModel) f_ListStore.GetValue(iter, 0);
                var key = (int) f_ListStore.GetValue(iter, 1);

                // test patterns
                try {
                    Pattern.IsMatch(String.Empty, filter.ChatID);
                } catch (ArgumentException ex) {
                    throw new ApplicationException(
                        String.Format(
                            _("Invalid filter regex: '{0}'. Reason: {1}"),
                            filter.ChatID, ex.Message
                        )
                    );
                }
                try {
                    Pattern.IsMatch(String.Empty, filter.MessagePattern);
                } catch (ArgumentException ex) {
                    throw new ApplicationException(
                        String.Format(
                            _("Invalid filter regex: '{0}'. Reason: {1}"),
                            filter.MessagePattern, ex.Message
                        )
                    );
                }

                if (key == -1) {
                    // new filter
                    if (String.IsNullOrEmpty(filter.Protocol) &&
                        filter.ChatType == null &&
                        String.IsNullOrEmpty(filter.ChatID) &&
                        filter.MessageType == null &&
                        String.IsNullOrEmpty(filter.MessagePattern)) {
                        // drop empty filters
                        f_ListStore.Remove(ref iter);
                        continue;
                    }
                    key = f_Controller.AddFilter(filter);
                    // write generated key back
                    f_ListStore.SetValue(iter, 1, key);
                } else {
                    // update filter
                    f_Controller.SetFilter(key, filter);
                }
            } while (f_ListStore.IterNext(ref iter));
        }

        protected virtual void OnChanged(EventArgs e)
        {
            if (Changed != null) {
                Changed(this, e);
            }
        }

        protected virtual void OnAddButtonClicked(object sender, System.EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                var filter = new FilterModel();
                filter.Protocol       = String.Empty;
                filter.ChatID         = String.Empty;
                filter.MessagePattern = String.Empty;
                Gtk.TreeIter iter = f_ListStore.AppendValues(filter, -1);
                f_TreeView.Selection.SelectIter(iter);

                OnChanged(EventArgs.Empty);
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        protected virtual void OnRemoveButtonClicked(object sender, System.EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                Gtk.TreeIter iter;
                if (!f_TreeView.Selection.GetSelected(out iter)) {
                    return;
                }

                Gtk.MessageDialog md = new Gtk.MessageDialog(
                     f_Parent,
                     Gtk.DialogFlags.Modal,
                     Gtk.MessageType.Warning,
                     Gtk.ButtonsType.YesNo,
                     _("Are you sure you want to delete the selected filter?")
                );
                int result = md.Run();
                md.Destroy();
                if (result != (int) Gtk.ResponseType.Yes) {
                    return;
                }

                f_ListStore.Remove(ref iter);

                OnChanged(EventArgs.Empty);
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        void Init()
        {
            f_ProtocolListStore = new Gtk.ListStore(typeof(string));

            f_ListStore = new Gtk.ListStore(
                typeof(FilterModel),
                typeof(int), // filter key
                typeof(string) // tool tip
            );
            f_TreeView.Model = f_ListStore;
            Gtk.TreeViewColumn column;
            Gtk.CellRendererText textCellr;
            Gtk.CellRendererCombo comboCellr;

            comboCellr = new Gtk.CellRendererCombo();
            comboCellr.Model = f_ProtocolListStore;
            comboCellr.TextColumn = 0;
            comboCellr.HasEntry = false;
            comboCellr.Editable = true;
            comboCellr.Edited += OnProtocolEdited;
            column = f_TreeView.AppendColumn(_("Protocol"), comboCellr);
            column.SetCellDataFunc(comboCellr, RenderProtocol);

            f_ChatTypeListStore = new Gtk.ListStore(typeof(string),
                                                    typeof(ChatType?));
            f_ChatTypeListStore.AppendValues(String.Empty, null);
            f_ChatTypeListStore.AppendValues(_("Person / Private"),  ChatType.Person);
            f_ChatTypeListStore.AppendValues(_("Group / Public"),    ChatType.Group);
            f_ChatTypeListStore.AppendValues(_("Protocol / Server"), ChatType.Protocol);
            comboCellr = new Gtk.CellRendererCombo();
            comboCellr.Model = f_ChatTypeListStore;
            comboCellr.TextColumn = 0;
            comboCellr.HasEntry = false;
            comboCellr.Editable = true;
            comboCellr.Edited += OnChatTypeEdited;
            column = f_TreeView.AppendColumn(_("Chat Type"), comboCellr);
            column.Resizable = true;
            column.Sizing = Gtk.TreeViewColumnSizing.GrowOnly;
            column.SetCellDataFunc(comboCellr, RenderChatType);

            textCellr = new Gtk.CellRendererText();
            textCellr.Editable = true;
            textCellr.Edited += delegate(object sender, Gtk.EditedArgs e) {
                Gtk.TreeIter iter;
                if (!f_ListStore.GetIterFromString(out iter, e.Path)) {
                    return;
                }
                FilterModel filter = (FilterModel) f_ListStore.GetValue(iter, 0);
                filter.ChatID = e.NewText;
                f_ListStore.EmitRowChanged(new Gtk.TreePath(e.Path), iter);
                OnChanged(EventArgs.Empty);
            };
            column = f_TreeView.AppendColumn(_("Name"), textCellr);
            column.MinWidth = 80;
            column.Resizable = true;
            column.Sizing = Gtk.TreeViewColumnSizing.GrowOnly;
            column.SetCellDataFunc(textCellr,
                delegate(Gtk.TreeViewColumn col,
                         Gtk.CellRenderer cellr,
                         Gtk.TreeModel model, Gtk.TreeIter iter ) {
                    FilterModel filter = (FilterModel) model.GetValue(iter, 0);
                    (cellr as Gtk.CellRendererText).Text = filter.ChatID;
                }
            );

            f_MessageTypeListStore = new Gtk.ListStore(typeof(string),
                                                       typeof(MessageType?));
            f_MessageTypeListStore.AppendValues(String.Empty, null);
            f_MessageTypeListStore.AppendValues(_("Normal"), MessageType.Normal);
            f_MessageTypeListStore.AppendValues(_("Event"),  MessageType.Event);
            comboCellr = new Gtk.CellRendererCombo();
            comboCellr.Model = f_MessageTypeListStore;
            comboCellr.TextColumn = 0;
            comboCellr.HasEntry = false;
            comboCellr.Editable = true;
            comboCellr.Edited += OnMessageTypeEdited;
            column = f_TreeView.AppendColumn(_("Type"), comboCellr);
            column.Resizable = true;
            column.Sizing = Gtk.TreeViewColumnSizing.GrowOnly;
            column.SetCellDataFunc(comboCellr, RenderMessageType);
            /*
            f_TreeView.HasTooltip = true;
            f_TreeView.QueryTooltip += delegate(object sender, Gtk.QueryTooltipArgs e) {
                e.Tooltip.Text = "Message Type";
                f_TreeView.SetTooltipCell(e.Tooltip, null, column, null);
                e.RetVal = true;
            };
            */

            textCellr = new Gtk.CellRendererText();
            textCellr.Editable = true;
            textCellr.Edited += delegate(object sender, Gtk.EditedArgs e) {
                Gtk.TreeIter iter;
                if (!f_ListStore.GetIterFromString(out iter, e.Path)) {
                    return;
                }
                FilterModel filter = (FilterModel) f_ListStore.GetValue(iter, 0);
                filter.MessagePattern = e.NewText;
                f_ListStore.EmitRowChanged(new Gtk.TreePath(e.Path), iter);
                OnChanged(EventArgs.Empty);
            };
            column = f_TreeView.AppendColumn(_("Pattern"), textCellr);
            column.Resizable = true;
            column.MinWidth = 80;
            column.Sizing = Gtk.TreeViewColumnSizing.GrowOnly;
            column.SetCellDataFunc(textCellr,
                delegate(Gtk.TreeViewColumn col,
                         Gtk.CellRenderer cellr,
                         Gtk.TreeModel model, Gtk.TreeIter iter) {
                    FilterModel filter = (FilterModel) model.GetValue(iter, 0);
                    (cellr as Gtk.CellRendererText).Text = filter.MessagePattern;
                }
            );
        }

        void RenderProtocol(Gtk.TreeViewColumn column, Gtk.CellRenderer cellr,
                            Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            FilterModel filter = (FilterModel) model.GetValue(iter, 0);
            (cellr as Gtk.CellRendererCombo).Text = filter.Protocol;
        }

        void OnProtocolEdited(object sender, Gtk.EditedArgs e)
        {
            Trace.Call(sender, e);

            Gtk.TreeIter iter;
            if (!f_ListStore.GetIterFromString(out iter, e.Path)) {
                return;
            }
            FilterModel filter = (FilterModel) f_ListStore.GetValue(iter, 0);
            filter.Protocol = e.NewText;

            f_ListStore.EmitRowChanged(new Gtk.TreePath(e.Path), iter);
            OnChanged(EventArgs.Empty);
        }

        void RenderChatType(Gtk.TreeViewColumn column, Gtk.CellRenderer cellr,
                            Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            FilterModel filter = (FilterModel) model.GetValue(iter, 0);
            foreach (object[] row in f_ChatTypeListStore) {
                if ((ChatType?) row[1] == filter.ChatType) {
                    (cellr as Gtk.CellRendererCombo).Text = (string) row[0];
                    break;
                }
            }
        }

        void OnChatTypeEdited(object sender, Gtk.EditedArgs e)
        {
            Trace.Call(sender, e);

            Gtk.TreeIter iter;
            if (!f_ListStore.GetIterFromString(out iter, e.Path)) {
                return;
            }
            FilterModel filter = (FilterModel) f_ListStore.GetValue(iter, 0);
            // HACK: lame GTK+ 2.12 is not exposing the combo box neither
            // the iterator of the selected row inside the combo box thus
            // we have lookup the value in the list store using the text :/
            // TODO: starting with GTK+ 2.14 the Changed event can be used
            // see http://git.gnome.org/browse/gtk+/tree/gtk/gtkcellrenderercombo.c#n178
            ChatType? newChatType = null;
            foreach (object[] row in f_ChatTypeListStore) {
                if ((string) row[0] == e.NewText) {
                    newChatType = (ChatType?) row[1];
                    break;
                }
            }
            filter.ChatType = newChatType;

            f_ListStore.EmitRowChanged(new Gtk.TreePath(e.Path), iter);
            OnChanged(EventArgs.Empty);
        }

        void RenderMessageType(Gtk.TreeViewColumn column, Gtk.CellRenderer cellr,
                               Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            FilterModel filter = (FilterModel) model.GetValue(iter, 0);
            foreach (object[] row in f_MessageTypeListStore) {
                if ((MessageType?) row[1] == filter.MessageType) {
                    (cellr as Gtk.CellRendererCombo).Text = (string) row[0];
                    break;
                }
            }
        }

        void OnMessageTypeEdited(object sender, Gtk.EditedArgs e)
        {
            Trace.Call(sender, e);

            Gtk.TreeIter iter;
            if (!f_ListStore.GetIterFromString(out iter, e.Path)) {
                return;
            }
            FilterModel filter = (FilterModel) f_ListStore.GetValue(iter, 0);
            // HACK: lame GTK+ 2.12 is not exposing the combo box neither
            // the iterator of the selected row inside the combo box thus
            // we have lookup the value in the list store using the text :/
            // TODO: starting with GTK+ 2.14 the Changed event can be used
            // see http://git.gnome.org/browse/gtk+/tree/gtk/gtkcellrenderercombo.c#n178
            MessageType? newMsgType = null;
            foreach (object[] row in f_MessageTypeListStore) {
                if ((string) row[0] == e.NewText) {
                    newMsgType = (MessageType?) row[1];
                    break;
                }
            }
            filter.MessageType = newMsgType;

            f_ListStore.EmitRowChanged(new Gtk.TreePath(e.Path), iter);
            OnChanged(EventArgs.Empty);
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
