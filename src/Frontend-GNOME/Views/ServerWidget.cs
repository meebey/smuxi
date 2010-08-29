// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class ServerWidget : Gtk.Bin
    {
        Gtk.ListStore f_NetworkListStore;

        public Gtk.Entry HostnameEntry {
            get {
                return f_HostnameEntry;
            }
        }

        public Gtk.ComboBox ProtocolComboBox {
            get {
                return f_ProtocolComboBox;
            }
        }

        public Gtk.ComboBoxEntry NetworkComboBoxEntry {
            get {
                return f_NetworkComboBoxEntry;
            }
        }

        public Gtk.CheckButton OnStartupConnectCheckButton {
            get {
                return f_OnStartupConnectCheckButton;
            }
        }

        public ServerWidget()
        {
            Trace.Call();

            f_NetworkListStore = new Gtk.ListStore(typeof(string));

            Build();
            Init();
        }

        public void Load(ServerModel server)
        {
            Trace.Call(server);

            // protocol is part of the PKEY, not allowed to change
            f_ProtocolComboBox.Sensitive = false;
            Gtk.ListStore store = (Gtk.ListStore) ProtocolComboBox.Model;

            int protocolPosition = -1;
            int j = 0;
            foreach (object[] row in store) {
                string protocolName = (string) row[0];
                if (protocolName == server.Protocol) {
                    protocolPosition = j;
                    break;
                }
                j++;
            }
            if (protocolPosition == -1) {
                throw new ApplicationException("Unsupported protocol: " + server.Protocol);
            }
            f_ProtocolComboBox.Active = protocolPosition;

            // hostname is part of the PKEY, not allowed to change
            f_HostnameEntry.Sensitive = false;
            f_HostnameEntry.Text = server.Hostname;

            f_PortSpinButton.Value = server.Port;
            f_NetworkComboBoxEntry.Entry.Text = server.Network;
            f_UsernameEntry.Text = server.Username;
            f_PasswordEntry.Text = server.Password;
            f_UseEncryptionCheckButton.Active = server.UseEncryption;
            f_ValidateServerCertificateCheckButton.Active =
                server.ValidateServerCertificate;
            OnStartupConnectCheckButton.Active = server.OnStartupConnect;
            if (server.OnConnectCommands == null ||
                server.OnConnectCommands.Count == 0) {
                f_OnConnectCommandsTextView.Buffer.Text = String.Empty;
            } else {
                // LAME: replace me when we have .NET 3.0
                string[] commands = new string[server.OnConnectCommands.Count];
                server.OnConnectCommands.CopyTo(commands, 0);
                f_OnConnectCommandsTextView.Buffer.Text = String.Join(
                    "\n", commands
                );
            }
        }
        
        public ServerModel GetServer()
        {
            ServerModel server = new ServerModel();
            server.Protocol = f_ProtocolComboBox.ActiveText;
            server.Hostname = f_HostnameEntry.Text.Trim();
            server.Network  = f_NetworkComboBoxEntry.Entry.Text.Trim();
            server.Port     = f_PortSpinButton.ValueAsInt;
            server.Username = f_UsernameEntry.Text.Trim();
            server.Password = f_PasswordEntry.Text;
            server.UseEncryption = f_UseEncryptionCheckButton.Active;
            server.ValidateServerCertificate =
                f_ValidateServerCertificateCheckButton.Active;
            server.OnStartupConnect = f_OnStartupConnectCheckButton.Active;
            if (f_OnConnectCommandsTextView.Sensitive) {
                server.OnConnectCommands =
                    f_OnConnectCommandsTextView.Buffer.Text.Split('\n');
            }

            return server;
        }

        public void InitProtocols(IList<string> protocols)
        {
            Trace.Call(protocols);

            if (protocols == null) {
                throw new ArgumentNullException("protocols");
            }

            f_ProtocolComboBox.Clear();
            var cell = new Gtk.CellRendererText();
            f_ProtocolComboBox.PackStart(cell, false);
            f_ProtocolComboBox.AddAttribute(cell, "text", 0);

            Gtk.ListStore store = new Gtk.ListStore(typeof(string));
            // fill protocols in ListStore
            foreach (string protocol in protocols) {
                store.AppendValues(protocol);
            }
            store.SetSortColumnId(0, Gtk.SortType.Ascending);
            f_ProtocolComboBox.Model = store;
            f_ProtocolComboBox.Active = 0;
        }

        public void InitNetworks(IList<string> networks)
        {
            Trace.Call(networks);

            if (networks == null) {
                throw new ArgumentNullException("networks");
            }

            f_NetworkComboBoxEntry.Visible = true;
            
            // fill protocols in ListStore
            f_NetworkListStore.Clear();
            foreach (string network in networks) {
                f_NetworkListStore.AppendValues(network);
            }
            f_NetworkListStore.SetSortColumnId(0, Gtk.SortType.Ascending);
            f_NetworkComboBoxEntry.Model = f_NetworkListStore;
            f_NetworkComboBoxEntry.TextColumn = 0;
        }

        private void Init()
        {
            f_ProtocolComboBox.Changed += delegate {
                CheckProtocolComboBox();
            };
            f_ShowPasswordCheckButton.Clicked += delegate {
                CheckShowPasswordCheckButton();
            };
            f_IgnoreOnConnectCommandsCheckButton.Toggled += delegate {
                CheckIgnoreOnConnectCommandsCheckButton();            
            };
            f_UseEncryptionCheckButton.Clicked += delegate {
                CheckUseEncryptionCheckButton();
            };
        }

        protected virtual void CheckIgnoreOnConnectCommandsCheckButton()
        {
            Trace.Call();

            f_OnConnectCommandsTextView.Sensitive =
                !f_IgnoreOnConnectCommandsCheckButton.Active;
        }

        protected virtual void CheckShowPasswordCheckButton()
        {
            Trace.Call();

            f_PasswordEntry.Visibility = f_ShowPasswordCheckButton.Active;
        }

        protected virtual void CheckUseEncryptionCheckButton()
        {
            Trace.Call();

            var useEncryption = f_UseEncryptionCheckButton.Active;
            f_ValidateServerCertificateCheckButton.Sensitive = useEncryption;
            switch (f_ProtocolComboBox.ActiveText) {
                case "IRC":
                    f_PortSpinButton.Value = useEncryption ? 6697 : 6669;
                    break;
            }
        }

        protected virtual void CheckProtocolComboBox()
        {
            Trace.Call();
            
            // HACK: hardcoded default list, not so nice
            // suggest sane port defaults
            // TODO: this should be replaced with some ProtocolInfo class
            // that contains exactly this kind of information
            switch (f_ProtocolComboBox.ActiveText) {
                case "IRC":
                    f_HostnameEntry.Sensitive = true;
                    f_NetworkComboBoxEntry.Sensitive = true;

                    f_PortSpinButton.Value = 6667;
                    f_PortSpinButton.Sensitive = true;
                    f_UseEncryptionCheckButton.Active = false;
                    f_UseEncryptionCheckButton.Sensitive = true;
                    f_ValidateServerCertificateCheckButton.Active = false;
                    f_ValidateServerCertificateCheckButton.Sensitive = true;
                    break;
                case "XMPP":
                    f_HostnameEntry.Sensitive = true;
                    f_NetworkComboBoxEntry.Entry.Text = String.Empty;
                    f_NetworkComboBoxEntry.Sensitive = false;

                    f_PortSpinButton.Value = 5222;
                    f_PortSpinButton.Sensitive = true;
                    f_UseEncryptionCheckButton.Active = false;
                    f_UseEncryptionCheckButton.Sensitive = false;
                    f_ValidateServerCertificateCheckButton.Active = false;
                    f_ValidateServerCertificateCheckButton.Sensitive = false;
                    break;
                // this protocols have static servers
                case "AIM":
                case "ICQ":
                case "MSNP":
                case "Twitter":
                    f_HostnameEntry.Text = String.Empty;
                    f_HostnameEntry.Sensitive = false;
                    f_NetworkComboBoxEntry.Entry.Text = String.Empty;
                    f_NetworkComboBoxEntry.Sensitive = false;

                    f_PortSpinButton.Value = 0;
                    f_PortSpinButton.Sensitive = false;
                    f_UseEncryptionCheckButton.Active = false;
                    f_UseEncryptionCheckButton.Sensitive = false;
                    f_ValidateServerCertificateCheckButton.Active = false;
                    f_ValidateServerCertificateCheckButton.Sensitive = false;
                    break;
                // in case we don't know / handle the protocol here, make
                // sure we grant maximum flexibility for the input
                default:
                    f_HostnameEntry.Sensitive = true;
                    f_PortSpinButton.Sensitive = true;
                    f_UseEncryptionCheckButton.Sensitive = true;
                    f_ValidateServerCertificateCheckButton.Sensitive = true;
                    break;
            }
        }
    }
}
