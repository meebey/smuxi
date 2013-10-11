// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2009, 2011, 2013 Mirco Bauer <meebey@meebey.net>
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
#if GTK_BUILDER
using UI = Gtk.Builder.ObjectAttribute;
#endif

namespace Smuxi.Frontend.Gnome
{
    public partial class EngineAssistantCredentialsWidget : Gtk.Bin
    {
#if GTK_BUILDER
        #pragma warning disable 0649
        [UI] Gtk.Entry f_UsernameEntry;
        [UI] Gtk.Entry f_PasswordEntry;
        [UI] Gtk.Entry f_VerifyPasswordEntry;
        [UI] Gtk.Entry f_SshUsernameEntry;
        [UI] Gtk.Entry f_SshPasswordEntry;
        [UI] Gtk.VBox f_SshPasswordVBox;
        [UI] Gtk.FileChooserButton f_SshKeyfileChooserButton;
        #pragma warning restore
#endif

        public Gtk.Entry UsernameEntry {
            get {
                return f_UsernameEntry;
            }
        }
        
        public Gtk.Entry PasswordEntry {
            get {
                return f_PasswordEntry;
            }
        }
            
        public Gtk.Entry VerifyPasswordEntry {
            get {
                return f_VerifyPasswordEntry;
            }
        }

        public Gtk.Entry SshUsernameEntry {
            get {
                return f_SshUsernameEntry;
            }
        }

        public Gtk.Entry SshPasswordEntry {
            get {
                return f_SshPasswordEntry;
            }
        }

        public Gtk.VBox SshPasswordVBox {
            get {
                return f_SshPasswordVBox;
            }
        }
        
        public Gtk.FileChooserButton SshKeyfileChooserButton {
            get {
                return f_SshKeyfileChooserButton;
            }
        }

        public EngineAssistantCredentialsWidget()
        {
            Build();
        }

#if GTK_BUILDER
        protected virtual void Build()
        {
            var builder = new Gtk.Builder(null, "Assistants.Engine.CredentialsWidget.ui", null);
            builder.Autoconnect(this);
            Add((Gtk.Widget) builder.GetObject("EngineAssistantCredentialsWidget"));
            ShowAll();
        }
#endif
    }
}
