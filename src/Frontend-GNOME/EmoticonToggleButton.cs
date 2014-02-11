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
    public class EmoticonToggleButton : Gtk.ToggleButton
    {
        private EmoticonsWindow _EmoticonsWindow;
        private Entry _Entry;
        private MainWindow _MainWindow;

        public EmoticonToggleButton(Entry entry, MainWindow mainWindow)
        {
            this.Relief=Gtk.ReliefStyle.Half;
            var imgTogglebutton = new Gtk.Image();
            imgTogglebutton.Pixbuf = Frontend.LoadIcon("face-smile-symbolic", 16, "face-smile-symbolic.png");
            this.Image = imgTogglebutton;
            this.Clicked += EmoticonToggleButtonClick;
            _Entry = entry;
            _MainWindow = mainWindow;
        }

        private void EmoticonToggleButtonClick(object obj, EventArgs args)
        {
            if (((Gtk.ToggleButton) obj).Active) {
                _EmoticonsWindow = new EmoticonsWindow(_Entry, _MainWindow);
                int alx;
                int aly;
                var alh = this.Allocation.Y - 110;
                this.GdkWindow.GetOrigin(out alx, out aly);
                _EmoticonsWindow.Decorated = false;
                _EmoticonsWindow.KeepAbove = true;
                _EmoticonsWindow.Move(alx, aly + alh);
                _EmoticonsWindow.ShowAll();
                _EmoticonsWindow.Destroyed += new EventHandler(OnDestroy);
            } else {
                _EmoticonsWindow.Destroy();
            }
        }

        private void OnDestroy(object obj, EventArgs args)
        {
            this.Active = false;
        }
    }
}
