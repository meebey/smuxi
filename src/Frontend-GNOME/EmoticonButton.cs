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
        private Entry _Entry;
        private EmoticonsWindow _EmoticonsWindow;

        public EmoticonButton(EmoticonsWindow emoticonsWindow, Entry entry, string tooltip, string image)
        {
            _Entry = entry;
            _EmoticonsWindow = emoticonsWindow;
            this.Relief = Gtk.ReliefStyle.None;
            this.TooltipText = tooltip;
            this.Clicked += EmoticonButtonCallback;
            var emoticonImage = new Gtk.Image();
            var imgPixbuf = "face-" + image + "-symbolic";
            emoticonImage.Pixbuf = Frontend.LoadIcon(imgPixbuf, 24, imgPixbuf + ".png");
            this.Image = emoticonImage;
        }

        private void EmoticonButtonCallback(object obj, EventArgs args)
        {
            var action = (string)((Gtk.Button)obj).TooltipText;
            if (String.IsNullOrEmpty(_Entry.Text) || _Entry.Text.EndsWith(" ")) {
                _Entry.Text = _Entry.Text + action + " ";
            } else {
                _Entry.Text = _Entry.Text +" " + action + " ";
            }
            _Entry.HasFocus = true;
            _EmoticonsWindow.Destroy();
        }
    }
}
