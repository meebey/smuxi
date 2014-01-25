// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 jamesaxl
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
using System.Collections;
using System.Collections.Generic;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class EmoticonsWindow : Gtk.Window
    {
        private Entry _Entry;

        public EmoticonsWindow(Entry entry ,MainWindow mainWindow) : base(Gtk.WindowType.Popup)
        {
            var emoticonStore = new EmoticonStore();
            var emoticonsTable = new Gtk.Table(3,8,true);
            var emoticonButtonlist = new List<EmoticonButton>();
            _Entry = entry;
            foreach (var emoticon in emoticonStore)
                emoticonButtonlist.Add(new EmoticonButton(this, _Entry, emoticon.Key as String, emoticon.Value as String));

            for (uint i = 0; i <= 7; i++)
                emoticonsTable.Attach(emoticonButtonlist[(int)i], i, i + 1, 0, 1);          

            for (uint i = 8, y = 0; i <= 15; i++, y++)
                emoticonsTable.Attach(emoticonButtonlist[(int)i], y, y + 1, 1, 2);          

            for (uint i = 16, y = 0; i <= 23; i++, y++)
                emoticonsTable.Attach(emoticonButtonlist[(int)i], y, y + 1, 2, 3);          

            this.Add(emoticonsTable);
            mainWindow.FocusOutEvent += DestroyEmoticonsWindow;
            mainWindow.FocusChildSet += DestroyEmoticonsWindow;
        }

        private void DestroyEmoticonsWindow(object obj, EventArgs args)
        {
            this.Destroy();
        }
    }
}
