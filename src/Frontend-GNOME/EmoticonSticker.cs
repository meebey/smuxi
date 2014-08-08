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

namespace Smuxi.Frontend.Gnome
{
	public class EmoticonSticker : Gtk.EventBox
    {
        private EmoticonsWindow EmoticonsWindow { get; set; }
        private Entry Entry { get; set; }
        private MainWindow MainWindow { get; set; }
        private EmoticonStore EmoticonStore { get; set; }
        public bool OpenedEmoticonWindow { get; set; }

		public EmoticonSticker(Entry entry, MainWindow mainWindow, EmoticonStore emoticonStore)
        {
            Entry = entry;
            MainWindow = mainWindow;
            EmoticonStore = emoticonStore;
            if (Entry == null) {
                throw new ArgumentNullException("Entry");
            }
            if (MainWindow == null) {
                throw new ArgumentNullException("MainWindow");
            }
            if (EmoticonStore == null) {
                throw new ArgumentNullException("EmoticonStore");
            }
            OpenedEmoticonWindow = false;
            var emoticonSticker = new Gtk.Image();
            emoticonSticker.Pixbuf = emoticonStore.GetEmoticonImage("smile", 20);
            Add(emoticonSticker);
            ButtonPressEvent += OnClicked;
            EmoticonsWindow = new EmoticonsWindow(Entry, MainWindow, EmoticonStore, this);
        }

        private void OnClicked(object obj, EventArgs args)
        {
            if (!OpenedEmoticonWindow) {
                int alx;
                int aly;
                var alh = Allocation.Y - 160;
                GdkWindow.GetOrigin(out alx, out aly);
                EmoticonsWindow.Decorated = false;
                EmoticonsWindow.KeepAbove = true;
                EmoticonsWindow.Move(alx, aly + alh);
                EmoticonsWindow.ShowAll();
                OpenedEmoticonWindow = true;
            } else {
                EmoticonsWindow.Hide();
                OpenedEmoticonWindow = false;
            }
        }
    }
}
