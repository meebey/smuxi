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

namespace Smuxi.Frontend.Gnome
{
    public partial class EngineAssistantConnectionWidget : Gtk.Bin
    {
        public Gtk.CheckButton UseSshTunnelCheckButton {
            get {
                return f_UseSshTunnelCheckButton;
            }
        }
        
        public Gtk.Entry SshHostEntry {
            get {
                return f_SshHostEntry;
            }
        }

        public Gtk.SpinButton SshPortSpinButton {
            get {
                return f_SshPortSpinButton;
            }
        }
        
        public Gtk.Entry HostEntry {
            get {
                return f_HostEntry;
            }
        }

        public Gtk.SpinButton PortSpinButton {
            get {
                return f_PortSpinButton;
            }
        }

        public EngineAssistantConnectionWidget()
        {
            Build();
            
            f_SshHostEntry.HasFocus = true;
        }
    }
}
