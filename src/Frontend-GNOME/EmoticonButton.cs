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
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;

namespace Smuxi.Frontend.Gnome
{
    public class EmoticonButton : Gtk.Button
    {
        private Entry Entry { get; set; }
        private string Symbol { get; set; }
        private EmoticonsWindow EmoticonsWindow { get; set; }

        public EmoticonButton (EmoticonsWindow emoticonsWindow, Entry entry, string emoticonSymbol, Gdk.Pixbuf emoticonImage, string emoticonName)
        {
            Entry = entry;
            EmoticonsWindow = emoticonsWindow;
            if (Entry == null) {
                throw new ArgumentNullException("Entry");
            }
            if (EmoticonsWindow == null) {
                throw new ArgumentNullException("EmoticonsWindow");
            }
            Relief = Gtk.ReliefStyle.None;
            TooltipText = emoticonName;
            Symbol = emoticonSymbol;
            var img = new Gtk.Image();
            img.Pixbuf = emoticonImage;
            Image = img;
            Clicked += OnClicked;
        }

        public EmoticonButton (EmoticonsWindow emoticonsWindow, Entry entry, string emoticonSymbol, string emoticonName)
        {
            Entry = entry;
            EmoticonsWindow = emoticonsWindow;
            if (Entry == null) {
                throw new ArgumentNullException("Entry");
            }
            if (EmoticonsWindow == null) {
                throw new ArgumentNullException("EmoticonsWindow");
            }
            Relief = Gtk.ReliefStyle.None;
            TooltipText = emoticonName;
            Symbol = emoticonSymbol;
            var font = Pango.FontDescription.FromString("Serif Bold 24");
            var sybmbolLabel = new Gtk.Label(emoticonSymbol);
            sybmbolLabel.ModifyFont(font);
            Label = emoticonSymbol;
            Clicked += OnClicked;
        }

        private void OnClicked(object obj, EventArgs args)
        {
            var action = Symbol;
            var iterInstert = Entry.Buffer.EndIter;
            Entry.Buffer.Insert(ref iterInstert, action);
            Entry.HasFocus = true;
            EmoticonsWindow.Hide();
        }
    }
}
