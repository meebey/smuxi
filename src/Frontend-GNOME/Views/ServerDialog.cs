// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010, 2012-2013 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using Gtk.Extensions;
using Smuxi.Common;
using Smuxi.Engine;
#if GTK_BUILDER
using UI = Gtk.Builder.ObjectAttribute;
#endif

namespace Smuxi.Frontend.Gnome
{
    public partial class ServerDialog : Gtk.Dialog
    {
#if GTK_BUILDER
        ServerWidget f_Widget;
        #pragma warning disable 0649
        [UI("OkButton")] Gtk.Button f_OkButton;
        #pragma warning restore
#endif

#if !GTK_BUILDER
        public ServerDialog(Gtk.Window parent, ServerModel server,
                            IList<string> supportedProtocols,
                            IList<string> networks) :
                       base(null, parent, Gtk.DialogFlags.DestroyWithParent)
        {
            Trace.Call(parent, server, supportedProtocols, networks);

            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            if (supportedProtocols == null) {
                throw new ArgumentNullException("supportedProtocols");
            }
            if (networks == null) {
                throw new ArgumentNullException("networks");
            }

            Build();
            Init(parent, server, supportedProtocols, networks);
        }
#else
        public ServerDialog(Gtk.Window parent, Gtk.Builder builder, IntPtr handle,
                            ServerModel server,
                            IList<string> supportedProtocols,
                            IList<string> networks) :
                       base(handle)
        {
            Trace.Call(parent, builder, handle, server, supportedProtocols, networks);

            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            if (builder == null) {
                throw new ArgumentNullException("builder");
            }
            if (handle == IntPtr.Zero) {
                throw new ArgumentException("handle", "handle must not be zero.");
            }
            if (supportedProtocols == null) {
                throw new ArgumentNullException("supportedProtocols");
            }
            if (networks == null) {
                throw new ArgumentNullException("networks");
            }

            builder.Autoconnect(this);
            f_Widget = new ServerWidget();
            ContentArea.Add(f_Widget);
            Init(parent, server, supportedProtocols, networks);

            ShowAll();
        }
#endif

        void Init(Gtk.Window parent, ServerModel server,
                  IList<string> supportedProtocols,
                  IList<string> networks)
        {
            TransientFor = parent;

            f_Widget.InitProtocols(supportedProtocols);
            f_Widget.InitNetworks(networks);

            f_Widget.ProtocolComboBox.Changed += delegate {
                CheckOkButton();
            };
            f_Widget.HostnameEntry.Changed += delegate {
                CheckOkButton();
            };
            CheckOkButton();

            if (server != null) {
                try {
                    f_Widget.Load(server);
                } catch (Exception) {
                    Destroy();
                    throw;
                }
            }
        }

        protected virtual void CheckOkButton()
        {
            Trace.Call();

            f_OkButton.Sensitive = true;
            switch (f_Widget.ProtocolComboBox.GetActiveText()) {
                case "Campfire":
                    if (f_Widget.HostnameEntry.Text == ".campfirenow.com") {
                        f_OkButton.Sensitive = false;
                    }
                    break;
            }
        }

        public ServerModel GetServer()
        {
            return f_Widget.GetServer();
        }
    }
}
