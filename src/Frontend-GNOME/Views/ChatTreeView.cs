// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013-2015 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class ChatTreeView : Gtk.TreeView
    {
        public Gtk.TreeStore TreeStore { get; private set; }
        ThemeSettings ThemeSettings { get; set; }
        Gtk.TreeViewColumn ActivityColumn { get; set; }
        int f_CurrentChatNumber;

        public ChatView CurrentChatView {
            get {
                Gtk.TreeIter iter;
                if (!Selection.GetSelected(out iter)) {
                    return null;
                }
                return (ChatView) TreeStore.GetValue(iter, 0);
            }
            set {
                Gtk.TreeIter iter;
                if (value == null) {
                    TreeStore.GetIterFirst(out iter);
                } else {
                    iter = FindChatIter(value);
                }
                var path = TreeStore.GetPath(iter);
                // we have to ensure we can make the new selection
                ExpandToPath(path);
                Selection.SelectPath(path);
            }
        }

        public int CurrentChatNumber {
            get {
                return f_CurrentChatNumber;
            }
            set {
                var path = GetPath(value);
                if (path == null) {
                    return;
                }
                // we have to ensure we can make the new selection
                ExpandToPath(path);
                Selection.SelectPath(path);
            }
        }

        public ChatTreeView()
        {
            ThemeSettings = new ThemeSettings();
            TreeStore = new Gtk.TreeStore(typeof(ChatView));
            TreeStore.SetSortColumnId(0, Gtk.SortType.Ascending);
            TreeStore.SetSortFunc(0, SortTreeStore);

            Model = TreeStore;
            HeadersVisible = false;
            BorderWidth = 0;
            ShowExpanders = false;
            LevelIndentation = 12;
            Selection.Mode = Gtk.SelectionMode.Browse;
            Selection.Changed += (sender, e) => {
                Gtk.TreeIter iter;
                if (!Selection.GetSelected(out iter) &&
                    TreeStore.GetIterFirst(out iter)) {
                    Selection.SelectIter(iter);
                    return;
                }
                var path = TreeStore.GetPath(iter);
                f_CurrentChatNumber = GetRowNumber(path);
            };

            var iconRenderer = new Gtk.CellRendererPixbuf();
            var column = new Gtk.TreeViewColumn(null, iconRenderer);
            column.Spacing = 0;
            column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
            column.SetCellDataFunc(iconRenderer, new Gtk.TreeCellDataFunc(RenderChatViewIcon));
            AppendColumn(column);

            var cellRenderer = new Gtk.CellRendererText() {
                Ellipsize = Pango.EllipsizeMode.End
            };
            column = new Gtk.TreeViewColumn(null, cellRenderer);
            column.Spacing = 0;
            column.Expand = true;
            column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
            column.SetCellDataFunc(cellRenderer, new Gtk.TreeCellDataFunc(RenderChatViewName));
            AppendColumn(column);

            cellRenderer = new Gtk.CellRendererText();
            column = new Gtk.TreeViewColumn(null, cellRenderer);
            column.Spacing = 0;
            column.Alignment = 1;
            column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
            column.SetCellDataFunc(cellRenderer, new Gtk.TreeCellDataFunc(RenderChatViewActivity));
            AppendColumn(column);
            ActivityColumn = column;
        }

        public virtual void Append(ChatView chatView)
        {
            if (chatView == null) {
                throw new ArgumentNullException("chatView");
            }

            if (chatView is SessionChatView ||
                chatView is ProtocolChatView) {
                // top level chats
                TreeStore.AppendValues(chatView);
                ReparentOrphans();
                if (TreeStore.IterNChildren() == 1) {
                    // first node, usualy Smuxi chat
                    CurrentChatView = chatView;
                }
            } else {
                // childs with parents, hopefully
                var parentIter = FindProtocolChatIter(chatView);
                if (TreeStore.IterIsValid(parentIter)) {
                    TreeStore.AppendValues(parentIter, chatView);
                    var path = TreeStore.GetPath(parentIter);
                    ExpandRow(path, true);
                } else {
                    // parent chat doesn't exist yet, thus it has to become
                    // a top level chat for now and re-parent later
                    TreeStore.AppendValues(chatView);
                }
            }
        }

        public virtual void Remove(ChatView chatView)
        {
            if (chatView == null) {
                throw new ArgumentNullException("chatView");
            }

            var iter = FindChatIter(chatView);
            if (!TreeStore.IterIsValid(iter)) {
                return;
            }
            TreeStore.Remove(ref iter);
        }

        public virtual void Render(ChatView chatView)
        {
            Trace.Call(chatView);

            if (chatView == null) {
                throw new ArgumentNullException("chatView");
            }

            var iter = FindChatIter(chatView);
            //var path = TreeStore.GetPath(iter);
            //TreeStore.EmitRowChanged(path, iter);
            // HACK: this emits row_changed _and_ sort_iter_changed and there is
            // no other public API in GTK+ to trigger a resort of a modified
            // value in the tree view :/
            TreeStore.SetValue(iter, 0, chatView);
        }

        public virtual bool IsVisible(ChatView chatView)
        {
            if (chatView == null) {
                throw new ArgumentNullException("chatView");
            }

            Gtk.TreePath visibleStart, visibleEnd;
            if (!GetVisibleRange(out visibleStart, out visibleEnd)) {
                return false;
            }
            var chatIter = FindChatIter(chatView);
            var chatPath = TreeStore.GetPath(chatIter);
            // we ignore 0 on purpose, say if a few pixels of a row are returned
            // as visible by GetVisibleRange() that is not good enough for us
            return visibleStart.Compare(chatPath) <= 0 &&
                   visibleEnd.Compare(chatPath) >= 0;
        }

        public virtual void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);

            if (config == null) {
                throw new ArgumentNullException("config");
            }

            ThemeSettings = new ThemeSettings(config);
            if (ThemeSettings.BackgroundColor == null) {
                ModifyBase(Gtk.StateType.Normal);
            } else {
                ModifyBase(Gtk.StateType.Normal, ThemeSettings.BackgroundColor.Value);
            }
            if (ThemeSettings.ForegroundColor == null) {
                ModifyText(Gtk.StateType.Normal);
            } else {
                ModifyText(Gtk.StateType.Normal, ThemeSettings.ForegroundColor.Value);
            }
            ModifyFont(ThemeSettings.FontDescription);

            ActivityColumn.Visible = (bool) config["Interface/ShowActivityCounter"];
        }

        protected virtual void RenderChatViewIcon(Gtk.TreeViewColumn column,
                                                  Gtk.CellRenderer cellr,
                                                  Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            if (cellr == null) {
                throw new ArgumentNullException("cellr");
            }
            if (model == null) {
                throw new ArgumentNullException("model");
            }

            var chat = (ChatView) model.GetValue(iter, 0);
            var renderer = (Gtk.CellRendererPixbuf) cellr;

            switch (chat.TabImage.StorageType) {
                case Gtk.ImageType.Pixbuf:
                    renderer.Pixbuf = chat.TabImage.Pixbuf;
                    break;
                case Gtk.ImageType.Stock:
                    renderer.StockId = chat.TabImage.Stock;
                    break;
                default:
                    renderer.Pixbuf = null;
                    break;
            }
        }

        protected virtual void RenderChatViewName(Gtk.TreeViewColumn column,
                                                  Gtk.CellRenderer cellr,
                                                  Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            if (cellr == null) {
                throw new ArgumentNullException("cellr");
            }
            if (model == null) {
                throw new ArgumentNullException("model");
            }

            var chat = (ChatView) model.GetValue(iter, 0);
            var renderer = (Gtk.CellRendererText) cellr;

            Gdk.Color color;
            if (chat.HighlightCount > 1) {
                color = ThemeSettings.HighlightColor;
            } else if (chat.HighlightCount == 1) {
                color = ThemeSettings.HighlightColor;
            } else if (chat.HasActivity) {
                color = ThemeSettings.ActivityColor;
            } else if (chat.HasEvent) {
                color = ThemeSettings.EventColor;
            } else {
                // no activity
                color = ThemeSettings.NoActivityColor;
            }

            var textColor = TextColorTools.GetBestTextColor(
                ColorConverter.GetTextColor(color),
                ColorConverter.GetTextColor(
                    Gtk.Rc.GetStyle(this).Base(Gtk.StateType.Normal)
                ), TextColorContrast.High
            );
            renderer.Markup = String.Format(
                "<span foreground=\"{0}\">{1}</span>",
                GLib.Markup.EscapeText(textColor.ToString()),
                GLib.Markup.EscapeText(chat.Name)
            );
        }

        protected virtual void RenderChatViewActivity(Gtk.TreeViewColumn column,
                                                      Gtk.CellRenderer cellr,
                                                      Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            if (cellr == null) {
                throw new ArgumentNullException("cellr");
            }
            if (model == null) {
                throw new ArgumentNullException("model");
            }

            var chat = (ChatView) model.GetValue(iter, 0);
            var renderer = (Gtk.CellRendererText) cellr;

            Gdk.Color color;
            string text = null;
            if (chat.HighlightCount >= 1) {
                color = ThemeSettings.HighlightColor;
                text = chat.HighlightCount.ToString();
            } else if (chat.ActivityCount >= 1) {
                color = ThemeSettings.ActivityColor;
                text = chat.ActivityCount.ToString();
            } else {
                // no highlight counter
                renderer.Markup = String.Empty;
                return;
            }
            if (text == null) {
                return;
            }

            var textColor = TextColorTools.GetBestTextColor(
                ColorConverter.GetTextColor(color),
                ColorConverter.GetTextColor(
                Gtk.Rc.GetStyle(this).Base(Gtk.StateType.Normal)
                ), TextColorContrast.High
            );
            renderer.Markup = String.Format(
                "<span foreground=\"{0}\">({1})</span>",
                GLib.Markup.EscapeText(textColor.ToString()),
                GLib.Markup.EscapeText(text)
            );
        }

        protected virtual int SortTreeStore(Gtk.TreeModel model,
                                            Gtk.TreeIter iter1,
                                            Gtk.TreeIter iter2)
        {
            var chat1 = (ChatView) model.GetValue(iter1, 0);
            var chat2 = (ChatView) model.GetValue(iter2, 0);
            // Smuxi is always the first item
            if (chat1 is SessionChatView &&
                chat2 is SessionChatView) {
                return 0;
            } else if (chat1 is SessionChatView) {
                return -1;
            } else if (chat2 is SessionChatView) {
                return 1;
            } else if (chat1 is GroupChatView &&
                       chat2 is GroupChatView) {
                // let Name decide
            }  else if (chat1 is GroupChatView) {
                return -1;
            }  else if (chat2 is GroupChatView) {
                return 1;
            }
            return chat1.Name.CompareTo(chat2.Name);
        }

        protected override bool OnKeyPressEvent(Gdk.EventKey @event)
        {
            if ((@event.State & Gdk.ModifierType.Mod1Mask) != 0 ||
                (@event.State & Gdk.ModifierType.ControlMask) != 0 ||
                (@event.State & Gdk.ModifierType.ShiftMask) != 0) {
                // alt, ctrl or shift pushed, returning
                return base.OnKeyPressEvent(@event);
            }

            if (CurrentChatView is SessionChatView) {
                // no menu for Smuxi chat
                return base.OnKeyPressEvent(@event);
            }

            if (@event.Key == Gdk.Key.Menu &&
                Selection.CountSelectedRows() > 0) {
                CurrentChatView.TabMenu.Popup(null, null, null, 0, @event.Time);
                return true;
            }

            return base.OnKeyPressEvent(@event);
        }

        protected override bool OnButtonReleaseEvent(Gdk.EventButton @event)
        {
            Trace.Call(@event);

            if (CurrentChatView is SessionChatView) {
                // no menu for Smuxi chat
                return base.OnButtonReleaseEvent(@event);
            }

            if (@event.Button == 3 && Selection.CountSelectedRows() > 0) {
                CurrentChatView.TabMenu.Popup(null, null, null, 0, @event.Time);
                return true;
            }

            return base.OnButtonReleaseEvent(@event);
        }

        void ReparentOrphans()
        {
            Gtk.TreeIter iter;
            Gtk.TreeIter parentIter = Gtk.TreeIter.Zero;
            TreeStore.GetIterFirst(out iter);
            do {
                var orphan = (ChatView) TreeStore.GetValue(iter, 0);
                if (orphan is SessionChatView ||
                    orphan is ProtocolChatView) {
                    continue;
                }
                if (TreeStore.IterParent(out parentIter, iter)) {
                    // already has an parent
                    continue;
                }
                // no parent, let's find one!
                parentIter = FindProtocolChatIter(orphan);
                if (!TreeStore.IterIsValid(parentIter)) {
                    continue;
                }
                // found a parent \o/
                TreeStore.Remove(ref iter);
                TreeStore.AppendValues(parentIter, orphan);
                var parentPath = TreeStore.GetPath(parentIter);
                ExpandRow(parentPath, true);
                // reset iter to first as we changed the store and thus can't
                // continue the iteration
                TreeStore.GetIterFirst(out iter);
            } while (TreeStore.IterNext(ref iter));
        }

        Gtk.TreeIter FindProtocolChatIter(ChatView child)
        {
            Gtk.TreeIter iter;
            Gtk.TreeIter parentIter = Gtk.TreeIter.Zero;
            TreeStore.GetIterFirst(out iter);
            do {
                var candidate = (ChatView) TreeStore.GetValue(iter, 0);
                if (!(candidate is ProtocolChatView) ||
                    candidate.ProtocolManager == null) {
                    continue;
                }
                if (child.ProtocolManager != candidate.ProtocolManager) {
                    continue;
                }
                parentIter = iter;
                break;
            } while (TreeStore.IterNext(ref iter));
            return parentIter;
        }

        Gtk.TreeIter FindChatIter(ChatView view)
        {
            Gtk.TreeIter chatIter = Gtk.TreeIter.Zero;
            TreeStore.Foreach((model, path, iter) => {
                var candidate = (ChatView) model.GetValue(iter, 0);
                if (candidate == view) {
                    chatIter = iter;
                    return true;
                }
                return false;
            });
            return chatIter;
        }

        int GetRowNumber(Gtk.TreePath path)
        {
            Gtk.TreeIter iter;
            if (!TreeStore.GetIter(out iter, path)) {
                // invalid path
                return -1;
            }

            Gtk.TreeIter walkerIter;
            TreeStore.GetIterFirst(out walkerIter);
            var walker = TreeStore.GetPath(walkerIter);
            for (var i = 0; TreeStore.GetIter(out walkerIter, walker); i++) {
                if (walker.Compare(path) == 0) {
                    return i;
                }

                if (TreeStore.IterHasChild(walkerIter)) {
                    walker.Down();
                } else {
                    walker.Next();

                    if (!TreeStore.GetIter(out walkerIter, walker)) {
                        // invalid path: reached last row
                        walker.Up();
                        walker.Next();
                    }
                }
            }
            return -1;
        }

        Gtk.TreePath GetPath(int rowNumber)
        {
            Gtk.TreeIter iter;
            TreeStore.GetIterFirst(out iter);
            var path = TreeStore.GetPath(iter);
            // TODO: clamp upper limit
            int i;
            for (i = 0; rowNumber >= 0 && i < rowNumber; i++) {
                TreeStore.GetIter(out iter, path);
                if (TreeStore.IterHasChild(iter)) {
                    path.Down();
                } else {
                    path.Next();

                    TreeStore.GetIter(out iter, path);
                    if (!TreeStore.IterIsValid(iter)) {
                        // reached last row
                        path.Up();
                        path.Next();
                    }
                }
            }
            return path;
        }
    }
}
