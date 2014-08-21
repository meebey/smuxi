// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012 
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
    public class PersonTag : Gtk.TextTag
    {
        public string ID { get; private set; }
        public string IdentityName { get; private set; }
        
        public PersonTag(string id, string identityName) : base(null)
        {
            if (id == null) {
                throw new ArgumentNullException("id");
            }
            if (identityName == null) {
                throw new ArgumentNullException("identityName");
            }

            ID = id;
            IdentityName = identityName;
        }

        protected PersonTag(IntPtr handle) : base(handle)
        {
        }
    }
}
