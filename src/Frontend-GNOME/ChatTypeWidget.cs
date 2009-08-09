// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2009 Mirco Bauer <meebey@meebey.net>
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
    [System.ComponentModel.ToolboxItem(true)]
    public partial class ChatTypeWidget : Gtk.Bin
    {
        private Gtk.ListStore f_ListStore;

        public ChatType ChatType {
            get {
                Gtk.TreeIter iter;
                f_ComboBox.GetActiveIter(out iter);
                return (ChatType) f_ListStore.GetValue(iter, 0);
            }
        }

        public ChatTypeWidget()
        {
            Build();
            Init();
        }

        private void Init()
        {
            f_ListStore = new Gtk.ListStore(
                typeof(ChatType),
                typeof(string)
            );
            f_ListStore.AppendValues(ChatType.Person, _("Person / Private"));
            f_ListStore.AppendValues(ChatType.Group,  _("Group / Public"));
            f_ListStore.SetSortColumnId(1, Gtk.SortType.Ascending);
            
            f_ComboBox.Clear();
            Gtk.CellRenderer cell = new Gtk.CellRendererText();
            f_ComboBox.PackStart(cell, false);
            f_ComboBox.AddAttribute(cell, "text", 1);
            f_ComboBox.Model = f_ListStore;
            f_ComboBox.Active = 0;
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
