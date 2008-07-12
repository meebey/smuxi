/*
 * $Id: PreferencesDialog.cs 73 2005-06-27 12:42:06Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/PreferencesDialog.cs $
 * $Rev: 73 $
 * $Author: meebey $
 * $Date: 2005-06-27 14:42:06 +0200 (Mon, 27 Jun 2005) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
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
    public partial class QuickConnectDialog : Gtk.Dialog 
    {
        private ServerListController f_Controller;
        private Gtk.TreeStore        f_TreeStore;
        private ServerModel          f_ServerModel;
        
        public ServerModel Server {
            get {
                return f_ServerModel;
            }
        }
        
        public QuickConnectDialog()
        {
            Trace.Call();
            
            Build();
            
            f_Controller = new ServerListController(Frontend.UserConfig);
            
            f_TreeView.AppendColumn(_("Protocol"), new Gtk.CellRendererText(), "text", 1); 
            f_TreeView.AppendColumn(_("Hostname"), new Gtk.CellRendererText(), "text", 2);
            
            f_TreeStore = new Gtk.TreeStore(
                typeof(ServerModel),
                typeof(string), // protocol
                typeof(string) // hostname
            );
            f_TreeView.RowActivated += OnTreeViewRowActivated;
            f_TreeView.Selection.Changed += OnTreeViewSelectionChanged;
            f_TreeView.Model = f_TreeStore;
        }
        
        public virtual void Load()
        {
            Trace.Call();
            
            LoadProtocols();
            LoadServers();
            
            CheckConnectButton();
        }
        
        protected void LoadProtocols()
        {
            Trace.Call();
            
            f_ProtocolComboBox.Clear();
            f_ProtocolComboBox.Changed += OnProtocolComboBoxChanged;
            Gtk.CellRenderer cell = new Gtk.CellRendererText();
            f_ProtocolComboBox.PackStart(cell, false);
            f_ProtocolComboBox.AddAttribute(cell, "text", 0);
            IList<string> supportedProtocols = Frontend.Session.GetSupportedProtocols();
            Gtk.ListStore store = new Gtk.ListStore(typeof(string));
            foreach (string protocol in supportedProtocols) {
                store.AppendValues(protocol);
            }
            store.SetSortColumnId(0, Gtk.SortType.Ascending);
            f_ProtocolComboBox.Model = store;
            f_ProtocolComboBox.Active = 0;
        }
        
        protected void LoadServers()
        {
            Trace.Call();
            
            f_TreeStore.Clear();
            
            // group servers by protocol
            Dictionary<string, List<ServerModel>> protocols = new Dictionary<string, List<ServerModel>>();
            IList<ServerModel> servers = f_Controller.GetServerList();
            foreach (ServerModel server in servers) {
                List<ServerModel> protocolServers = null;
                protocols.TryGetValue(server.Protocol, out protocolServers);
                if (protocolServers == null) {
                    protocolServers = new List<ServerModel>();
                    protocols.Add(server.Protocol, protocolServers);
                }
                protocolServers.Add(server);
            }
            
            // add grouped servers to treeview
            foreach (KeyValuePair<string, List<ServerModel>> pair in protocols) {
                Gtk.TreeIter parentIter = f_TreeStore.AppendValues(null, pair.Key, String.Empty);
                foreach (ServerModel server in pair.Value) {
                    // a server with an empty hostname has only one default/hardcoded
                    // hostname, thus don't create a sub-node for it
                    if (String.IsNullOrEmpty(server.Hostname)) {
                        f_TreeStore.SetValue(parentIter, 0, server);
                        continue;
                    }
                    
                    f_TreeStore.AppendValues(parentIter, server, String.Empty, server.Hostname);
                }
            }
            
            f_TreeView.ExpandAll();
        }
        
        protected virtual ServerModel GetCurrentServer()
        {
            Trace.Call();
            
            Gtk.TreeIter iter;
            if (!f_TreeView.Selection.GetSelected(out iter)) {
                return null;
            }
            return (ServerModel) f_TreeStore.GetValue(iter, 0);
        }
        
#region Event Handlers
        protected virtual void OnTreeViewSelectionChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ServerModel server = GetCurrentServer();
                if (server == null) {
                    return;
                }
                
                int protocolPosition = -1;
                int i = 0;
                foreach (object[] row in (Gtk.ListStore) f_ProtocolComboBox.Model) {
                    string protocol = (string) row[0];
                    if (protocol == server.Protocol) {
                        protocolPosition = i;
                        break;
                    }
                    i++;
                }
                
                if (protocolPosition == -1) {
                    throw new ApplicationException("Unsupported protocol: " + server.Protocol);
                }
                
                f_ProtocolComboBox.Active = protocolPosition;
                f_HostnameEntry.Text = server.Hostname;
                f_PortSpinButton.Value = server.Port;
                f_UsernameEntry.Text = server.Username;
                f_PasswordEntry.Text = server.Password;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnProtocolComboBoxChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                // HACK: hardcoded default list, not so nice
                // suggest sane port defaults
                switch (f_ProtocolComboBox.ActiveText) {
                    case "IRC":
                        f_HostnameEntry.Sensitive = true;
                        
                        f_PortSpinButton.Value = 6667;
                        f_PortSpinButton.Sensitive = true;
                        break;
                    case "XMPP":
                        f_HostnameEntry.Sensitive = true;
                        
                        f_PortSpinButton.Value = 5222;
                        f_PortSpinButton.Sensitive = true;
                        break;
                    case "AIM":
                    case "ICQ":
                    case "MSNP":
                        f_HostnameEntry.Text = String.Empty;
                        f_HostnameEntry.Sensitive = false;
                        
                        f_PortSpinButton.Value = 0;
                        f_PortSpinButton.Sensitive = false;
                        break;
                }
                
                CheckConnectButton();
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnTreeViewRowActivated(object sender, Gtk.RowActivatedArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ServerModel server = GetCurrentServer();
                if (server == null) {
                    return;
                }
                
                Respond(Gtk.ResponseType.Ok);
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnConnectButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                f_ServerModel = new ServerModel();
                f_ServerModel.Protocol = f_ProtocolComboBox.ActiveText;
                f_ServerModel.Hostname = f_HostnameEntry.Text;
                f_ServerModel.Port     = f_PortSpinButton.ValueAsInt;
                f_ServerModel.Username = f_UsernameEntry.Text;
                f_ServerModel.Password = f_PasswordEntry.Text;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
#endregion
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }

        protected virtual void CheckConnectButton()
        {
            Trace.Call();
            
            f_ConnectButton.Sensitive = !f_HostnameEntry.Sensitive ||
                                        f_HostnameEntry.Text.Trim().Length > 0;
        }
        
        protected virtual void OnHostnameEntryChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                CheckConnectButton();
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
    }
}
