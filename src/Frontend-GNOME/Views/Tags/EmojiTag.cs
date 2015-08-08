// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2015 Carlos Martín Nieto
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
    public class EmojiTag : Gtk.TextTag
    {
        public string Path { get; private set; }
        public Gtk.TextMark Mark { get; private set; }

        public EmojiTag(Gtk.TextMark mark, string path) : base(null)
        {
            if (mark == null) {
                throw new ArgumentNullException("mark");
            }

            if (path == null) {
                throw new ArgumentNullException("path");
            }

            Mark = mark;
            Path = path;
        }

        protected EmojiTag(IntPtr handle) : base(handle)
        {
        }
    }
}

