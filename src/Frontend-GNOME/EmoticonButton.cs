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
        private EmoticonsWindow EmoticonsWindow { get; set; }

        public EmoticonButton(EmoticonsWindow emoticonsWindow, Entry entry, string tooltip,
                              string image, bool imageflag = true)
        {
            Entry = entry;
            EmoticonsWindow = emoticonsWindow;
            Relief = Gtk.ReliefStyle.None;
            TooltipText = tooltip;
            Clicked += OnClicked;
            if (imageflag) {
                var emoticonImage = new Gtk.Image();
                var imgPixbuf = "face-" + image + "-symbolic";
                emoticonImage.Pixbuf = Frontend.LoadIcon(imgPixbuf, 24, imgPixbuf + ".png");
                Image = emoticonImage;
            } else {
                Label = TooltipText;
            }
        }

        private void OnClicked(object obj, EventArgs args)
        {
            var action = TooltipText;
            var buffer = Entry.Buffer;
            var iterInstert = buffer.EndIter;
            buffer.Insert(ref iterInstert, action);
            Entry.HasFocus = true;
            EmoticonsWindow.Destroy();
        }
    }
}
