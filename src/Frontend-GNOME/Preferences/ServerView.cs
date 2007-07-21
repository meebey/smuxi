/*
 * $Id: PreferencesDialog.cs 142 2007-01-02 22:19:08Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/PreferencesDialog.cs $
 * $Rev: 142 $
 * $Author: meebey $
 * $Date: 2007-01-02 23:19:08 +0100 (Tue, 02 Jan 2007) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System;
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class ServerView
    {
        private ServerListController _Controller;
        private Gtk.TreeView             _TreeView;
        private Gtk.ListStore            _ListStore;
        private Glade.XML                _Glade;
        [Glade.Widget("ServerDialog")]
        private Gtk.Dialog               _Dialog;
        [Glade.Widget("OKButton")]
        private Gtk.Button               _OKButton;
        [Glade.Widget("CancelButton")]
        private Gtk.Button               _CancelButton;
        [Glade.Widget("ProtocolComboBox")]
        private Gtk.ComboBox             _ProtocolComboBox;
        [Glade.Widget("HostnameEntry")]
        private Gtk.Entry                _HostnameEntry;
        [Glade.Widget("PortSpinButton")]
        private Gtk.SpinButton           _PortSpinButton;
        [Glade.Widget("NetworkComboBoxEntry")]
        private Gtk.ComboBoxEntry        _NetworkComboBoxEntry;
        [Glade.Widget("UsernameEntry")]
        private Gtk.Entry                _UsernameEntry;
        [Glade.Widget("PasswordEntry")]
        private Gtk.Entry                _PasswordEntry;
        [Glade.Widget("OnStartupConnectCheckButton")]
        private Gtk.CheckButton          _OnStartupConnectCheckButton;
        [Glade.Widget("OnConnectCommandsTextView")]
        private Gtk.TextView             _OnConnectCommandsTextView;
        private ServerModel              _ServerModel;
        
        public ServerModel Server {
            get {
                return _ServerModel;
            }
        }
        
        public ServerView(ServerModel server, IList<string> supportedProtocols, IList<string> networks)
        {
            Trace.Call(server);

            _Glade = new Glade.XML(null, Frontend.GladeFilename, "ServerDialog", null);
            _Glade.BindFields(this);
            
            Gtk.ComboBox cb;
            Gtk.CellRendererText cell;
            Gtk.ListStore store;
            
            // initialize networks
            Gtk.ComboBoxEntry cbe = _NetworkComboBoxEntry;
            cbe.Clear();
            cell = new Gtk.CellRendererText();
            cbe.PackStart(cell, false);
            cbe.AddAttribute(cell, "text", 0);
            store = new Gtk.ListStore(typeof(string));
            // fill protocols in ListStore
            foreach (string network in networks) {
                store.AppendValues(network);
            }
            store.SetSortColumnId(0, Gtk.SortType.Ascending);
            cbe.Model = store;
            cbe.TextColumn = 0;
            
            // initialize protocols
            // glade might initialize it already!
            cb = _ProtocolComboBox;
            cb.Clear();
            cb.Changed += new EventHandler(_OnProtocolComboBoxChanged);
            cell = new Gtk.CellRendererText();
            cb.PackStart(cell, false);
            cb.AddAttribute(cell, "text", 0);
            store = new Gtk.ListStore(typeof(string));
            // fill protocols in ListStore
            foreach (string protocol in supportedProtocols) {
                store.AppendValues(protocol);
            }
            store.SetSortColumnId(0, Gtk.SortType.Ascending);
            cb.Model = store;
            cb.Active = 0;
            int j = 0;
            if (server != null) {
                // protocol is part of the PKEY, not allowed to change
                cb.Sensitive = false;
                foreach (object[] row in store) {
                    string protocolName = (string) row[0];
                    if (protocolName == server.Protocol) {
                        cb.Active = j;
                        break;
                    }
                    j++;
                }

                // hostname is part of the PKEY, not allowed to change
                _HostnameEntry.Sensitive = false;
                _HostnameEntry.Text = server.Hostname;

                _PortSpinButton.Value = server.Port;
                _NetworkComboBoxEntry.Entry.Text = server.Network;
                _UsernameEntry.Text = server.Username;
                _PasswordEntry.Text = server.Password;
                _OnStartupConnectCheckButton.Active = server.OnStartupConnect;
                string[] commands = new string[server.OnConnectCommands.Count];
                server.OnConnectCommands.CopyTo(commands, 0);
                _OnConnectCommandsTextView.Buffer.Text = String.Join("\n", commands);
            }
        }
        
        public int Run()
        {
            int res = _Dialog.Run();
            if ((Gtk.ResponseType)res == Gtk.ResponseType.Ok) {
                _ServerModel = new ServerModel();
                _ServerModel.Protocol = _ProtocolComboBox.ActiveText;
                _ServerModel.Hostname = _HostnameEntry.Text;
                _ServerModel.Port = _PortSpinButton.ValueAsInt;
                _ServerModel.Network = _NetworkComboBoxEntry.Entry.Text;
                _ServerModel.Username = _UsernameEntry.Text;
                _ServerModel.Password = _PasswordEntry.Text;
                _ServerModel.OnStartupConnect = _OnStartupConnectCheckButton.Active;
                _ServerModel.OnConnectCommands = _OnConnectCommandsTextView.Buffer.Text.Split(new char[] {'\n'});
            }
            return res;
        }
        
        public void Destroy()
        {
            _Dialog.Destroy();
        }
        
        private void _OnProtocolComboBoxChanged(object sender, EventArgs e)
        {
            // suggest sane port defaults
            switch (_ProtocolComboBox.ActiveText) {
                case "IRC":
                    _PortSpinButton.Value = 6667;
                    break;
                case "XMPP":
                    _PortSpinButton.Value = 5222;
                    break;
            }
        }
    }
}
