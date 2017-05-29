// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010-2015, 2017 Mirco Bauer <meebey@meebey.net>
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

        string ServerID { get; set; }
        ServerModel Server { get; set; }

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

        public string Protocol {
            set {
                if (value == null) {
                    // clear selection
                    f_ProtocolComboBox.Active = -1;
                    return;
                }

                Gtk.ListStore store = (Gtk.ListStore) ProtocolComboBox.Model;
                int protocolPosition = -1;
                int j = 0;
                foreach (object[] row in store) {
                    var protocolId = (string) row[1];
                    if (protocolId == value) {
                        protocolPosition = j;
                        break;
                    }
                    j++;
                }
                if (protocolPosition == -1) {
                    var iter = store.AppendValues(
                        String.Format(
                            "{0} ({1})",
                            value,
                            _("Unsupported")
                        ),
                        value
                    );
                    f_ProtocolComboBox.SetActiveIter(iter);
                    return;
                }
                f_ProtocolComboBox.Active = protocolPosition;
            }
        }

        public Gtk.ComboBoxEntry NetworkComboBoxEntry {
            get {
                return f_NetworkComboBoxEntry;
            }
        }

        public Gtk.Entry NicknameEntry {
            get {
                return f_NicknameEntry;
            }
        }

        public Gtk.Entry RealnameEntry {
            get {
                return f_RealnameEntry;
            }
        }

        public Gtk.CheckButton OnStartupConnectCheckButton {
            get {
                return f_OnStartupConnectCheckButton;
            }
        }
        
        public bool ShowHostname {
            set {
                f_HostnameLabel.Visible = value;
                f_HostnameEntry.Visible = value;
                f_PortLabel.Visible = value;
                f_PortSpinButton.Visible = value;
            }
        }

        public bool ShowNetwork {
            set {
                f_NetworkLabel.Visible = value;
                f_NetworkComboBoxEntry.Visible = value;
            }
        }

        public bool ShowNickname {
            set {
                // Smuxi < 0.11 does not support server specific nickname
                if (Frontend.EngineProtocolVersion < new Version(0, 11)) {
                    value = false;
                }
                f_NicknameLabel.Visible = value;
                f_NicknameEntry.Visible = value;
            }
        }

        public bool ShowRealname {
            set {
                // Smuxi < 0.11 does not support server specific realname
                if (Frontend.EngineProtocolVersion < new Version(0, 11)) {
                    value = false;
                }
                f_RealnameLabel.Visible = value;
                f_RealnameEntry.Visible = value;
            }
        }

       public bool ShowPassword {
            set {
                f_PasswordLabel.Visible = value;
                f_PasswordEntry.Visible = value;
                f_ShowPasswordCheckButton.Visible = value;
            }
        }

        public bool SupportUseEncryption {
            set {
                f_UseEncryptionCheckButton.Sensitive = value;
                f_ValidateServerCertificateCheckButton.Sensitive = value;
                if (!value) {
                    f_UseEncryptionCheckButton.Active = false;
                    f_ValidateServerCertificateCheckButton.Active = false;
                }
                CheckUseEncryptionCheckButton();
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

            Server = server;

            // protocol is part of the PKEY, not allowed to change
            f_ProtocolComboBox.Sensitive = false;

            Protocol = server.Protocol;
            ServerID = server.ServerID;
            f_HostnameEntry.Text = server.Hostname;
            f_NetworkComboBoxEntry.Entry.Text = server.Network;
            if (String.IsNullOrEmpty(server.Nickname)) {
                var defaultNicknames = (string[]) Frontend.UserConfig["Connection/Nicknames"];
                f_NicknameEntry.Text = String.Join(" ", defaultNicknames);
            } else {
                f_NicknameEntry.Text = server.Nickname;
            }
            if (String.IsNullOrEmpty(server.Realname)) {
                var defaultRealname = (string) Frontend.UserConfig["Connection/Realname"];
                f_RealnameEntry.Text = defaultRealname;
            } else {
                f_RealnameEntry.Text = server.Realname;
            }
            f_UsernameEntry.Text = server.Username;
            // HACK: Twitter username is part of the PKEY, not allowed to change
            if (server.Protocol == "Twitter") {
                f_UsernameEntry.Sensitive = false;
            } else {
                f_UsernameEntry.Sensitive = true;
            }
            f_PasswordEntry.Text = server.Password;
            f_UseEncryptionCheckButton.Active = server.UseEncryption;
            f_ValidateServerCertificateCheckButton.Active =
                server.ValidateServerCertificate;
            f_PortSpinButton.Value = server.Port;
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
            var server = Server;
            if (server == null) {
                server = new ServerModel();
            }
            server.Protocol = f_ProtocolComboBox.ActiveText;
            server.ServerID = ServerID;
            server.Hostname = f_HostnameEntry.Text.Trim();
            server.Network  = f_NetworkComboBoxEntry.Entry.Text.Trim();
            server.Port     = f_PortSpinButton.ValueAsInt;
            server.Username = f_UsernameEntry.Text.Trim();
            // HACK: use Twitter username as hostname for multi-account support
            if (f_ProtocolComboBox.ActiveText == "Twitter") {
                server.Hostname = server.Username;
            }
            server.Password = f_PasswordEntry.Text;
            server.Nickname = f_NicknameEntry.Text.Trim();
            server.Realname = f_RealnameEntry.Text.Trim();
            server.UseEncryption = f_UseEncryptionCheckButton.Active;
            server.ValidateServerCertificate =
                f_ValidateServerCertificateCheckButton.Active;
            server.OnStartupConnect = f_OnStartupConnectCheckButton.Active;
            if (f_OnConnectCommandsTextView.Sensitive) {
                server.OnConnectCommands =
                    f_OnConnectCommandsTextView.Buffer.Text.Split('\n');
            } else {
                server.OnConnectCommands = new List<string>();
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

            var store = new Gtk.ListStore(typeof(string),
                                          typeof(string));
            // fill protocols in ListStore
            foreach (string protocol in protocols) {
                store.AppendValues(protocol, protocol);
            }
            store.SetSortColumnId(0, Gtk.SortType.Ascending);
            f_ProtocolComboBox.Model = store;

            try {
                // select IRC by default (if available)
                Protocol = "IRC";
            } catch (ArgumentOutOfRangeException) {
            }
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

            var defaultNicknames = (string[]) Frontend.UserConfig["Connection/Nicknames"];
            f_NicknameEntry.Text = String.Join(" ", defaultNicknames);
            var defaultRealname = (string) Frontend.UserConfig["Connection/Realname"];
            f_RealnameEntry.Text = defaultRealname;
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
            if (!useEncryption) {
                f_ValidateServerCertificateCheckButton.Active = false;
            }
            switch (f_ProtocolComboBox.ActiveText) {
                case "IRC":
                    if (f_PortSpinButton.Value == 6667 ||
                        f_PortSpinButton.Value == 6697) {
                        f_PortSpinButton.Value = useEncryption ? 6697 : 6667;
                    }
                    break;
                case "JabbR":
                    if (f_PortSpinButton.Value == 80 ||
                        f_PortSpinButton.Value == 443) {
                        f_PortSpinButton.Value = useEncryption ? 443 : 80;
                    }
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
                    ShowHostname = true;
                    ShowNetwork = true;
                    ShowNickname = true;
                    ShowRealname = true;
                    ShowPassword = true;
                    SupportUseEncryption = true;

                    f_HostnameEntry.Sensitive = true;
                    f_NetworkComboBoxEntry.Sensitive = true;

                    f_PortSpinButton.Value = 6667;
                    f_PortSpinButton.Sensitive = true;
                    break;
                case "Facebook":
                    ShowHostname = false;
                    ShowNetwork = false;
                    ShowNickname = false;
                    ShowRealname = false;
                    ShowPassword = true;
                    SupportUseEncryption = true;
                    f_HostnameEntry.Text = "chat.facebook.com";
                    f_PortSpinButton.Value = 5222;
                    break;
                case "XMPP":
                    ShowHostname = true;
                    ShowNetwork = false;
                    ShowNickname = false;
                    ShowRealname = false;
                    ShowPassword = true;
                    SupportUseEncryption = true;
                
                    f_HostnameEntry.Sensitive = true;
                    f_NetworkComboBoxEntry.Entry.Text = String.Empty;
                    f_NetworkComboBoxEntry.Sensitive = false;

                    f_PortSpinButton.Value = 5222;
                    f_PortSpinButton.Sensitive = true;
                    break;
                // this protocols have static servers
                case "AIM":
                case "ICQ":
                case "MSNP":
                    ShowHostname = false;
                    ShowNetwork = false;
                    ShowNickname = false;
                    ShowRealname = false;
                    ShowPassword = true;
                    SupportUseEncryption = false;

                    f_HostnameEntry.Text = String.Empty;
                    f_HostnameEntry.Sensitive = false;
                    f_NetworkComboBoxEntry.Entry.Text = String.Empty;
                    f_NetworkComboBoxEntry.Sensitive = false;

                    f_PortSpinButton.Value = 0;
                    f_PortSpinButton.Sensitive = false;
                    break;
                case "Twitter":
                    ShowHostname = false;
                    ShowNetwork = false;
                    ShowNickname = false;
                    ShowRealname = false;
                    ShowPassword = false;
                    SupportUseEncryption = true;
                    // engine always uses https
                    f_UseEncryptionCheckButton.Active = true;
                    f_UseEncryptionCheckButton.Sensitive = false;

                    f_HostnameEntry.Text = String.Empty;
                    f_PortSpinButton.Value = 443;
                    f_PortSpinButton.Sensitive = false;
                    f_NetworkComboBoxEntry.Entry.Text = String.Empty;
                    f_PasswordEntry.Text = String.Empty;
                    break;
                case "Campfire":
                    ShowHostname = true;
                    ShowNetwork = false;
                    ShowNickname = false;
                    ShowRealname = false;
                    ShowPassword = true;
                    SupportUseEncryption = true;
                    // engine always uses https
                    f_UseEncryptionCheckButton.Active = true;
                    f_UseEncryptionCheckButton.Sensitive = false;

                    f_HostnameEntry.Text = ".campfirenow.com";
                    f_HostnameEntry.Sensitive = true;
                    f_PortSpinButton.Value = 443;
                    f_PortSpinButton.Sensitive = false;
                    f_NetworkComboBoxEntry.Entry.Text = String.Empty;
                    f_PasswordEntry.Text = String.Empty;
                    break;
                case "JabbR":
                    ShowHostname = true;
                    ShowNetwork = false;
                    ShowNickname = false;
                    ShowRealname = false;
                    ShowPassword = true;
                    SupportUseEncryption = true;

                    f_HostnameEntry.Text = "jabbr.net";
                    f_HostnameEntry.Sensitive = true;
                    f_PortSpinButton.Value = 443;
                    f_PortSpinButton.Sensitive = true;
                    f_UseEncryptionCheckButton.Active = true;
                    f_NetworkComboBoxEntry.Entry.Text = String.Empty;
                    f_PasswordEntry.Text = String.Empty;
                    break;
                // in case we don't know / handle the protocol here, make
                // sure we grant maximum flexibility for the input
                default:
                    ShowHostname = true;
                    ShowNetwork = true;
                    ShowNickname = true;
                    ShowRealname = true;
                    ShowPassword = true;
                    SupportUseEncryption = true;

                    f_HostnameEntry.Sensitive = true;
                    f_PortSpinButton.Sensitive = true;
                    f_UseEncryptionCheckButton.Sensitive = true;
                    f_ValidateServerCertificateCheckButton.Sensitive = true;
                    break;
            }
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
