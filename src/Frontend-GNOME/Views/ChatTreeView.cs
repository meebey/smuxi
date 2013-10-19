// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
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

        public ChatView CurrentChatView {
            get {
                Gtk.TreeIter iter;
                if (!Selection.GetSelected(out iter)) {
                    return null;
                }
                return (ChatView) TreeStore.GetValue(iter, 0);
            }
        }

        public ChatTreeView()
        {
            ThemeSettings = new ThemeSettings();
            TreeStore = new Gtk.TreeStore(typeof(ChatView));

            Model = TreeStore;
            HeadersVisible = false;
            BorderWidth = 0;
            Selection.Mode = Gtk.SelectionMode.Browse;

            var cellRenderer = new Gtk.CellRendererText();
            var column = new Gtk.TreeViewColumn(null, cellRenderer);
            column.Spacing = 0;
            column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
            column.SetCellDataFunc(cellRenderer, new Gtk.TreeCellDataFunc(RenderChatView));
            AppendColumn(column);
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
            TreeStore.Remove(ref iter);
        }

        public virtual void Render(ChatView chatView)
        {
            if (chatView == null) {
                throw new ArgumentNullException("chatView");
            }

            var iter = FindChatIter(chatView);
            var path = TreeStore.GetPath(iter);
            TreeStore.EmitRowChanged(path, iter);
        }

        public virtual void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);

            if (config == null) {
                throw new ArgumentNullException("config");
            }

            ThemeSettings = new ThemeSettings(config);
        }

        protected virtual void RenderChatView(Gtk.TreeViewColumn column,
                                              Gtk.CellRenderer cellr,
                                              Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            var chat = (ChatView) model.GetValue(iter, 0);
            var renderer = (Gtk.CellRendererText) cellr;

            Gdk.Color color;
            string text;
            if (chat.HighlightCount > 1) {
                color = ThemeSettings.HighlightColor;
                text = String.Format("{0} ({1})",
                                     chat.Name,
                                     chat.HighlightCount.ToString());
            } else if (chat.HighlightCount == 1) {
                color = ThemeSettings.HighlightColor;
                text = chat.Name;
            } else if (chat.HasActivity) {
                color = ThemeSettings.ActivityColor;
                text = chat.Name;
            } else if (chat.HasEvent) {
                color = ThemeSettings.EventColor;
                text = chat.Name;
            } else {
                // no activity
                color = ThemeSettings.NoActivityColor;
                text = chat.Name;
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
                GLib.Markup.EscapeText(text)
            );
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
    }
}
