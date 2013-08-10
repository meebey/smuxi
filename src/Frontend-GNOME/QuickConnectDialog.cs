/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008, 2010 Mirco Bauer <meebey@meebey.net>
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
        
        public QuickConnectDialog(Gtk.Window parent) :
                             base(null, parent,
                                  Gtk.DialogFlags.DestroyWithParent)
        {
            Trace.Call(parent);

            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
                
            Build();
            
            TransientFor = parent;
            
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

            f_Widget.InitProtocols(Frontend.Session.GetSupportedProtocols());
            // these fields doesn't make sense here
            f_Widget.OnStartupConnectCheckButton.Visible = false;
            f_Widget.NetworkComboBoxEntry.Sensitive = false;
            f_Widget.ProtocolComboBox.Changed += delegate {
                CheckConnectButton();
            };
            f_Widget.HostnameEntry.Changed += delegate {
                CheckConnectButton();
            };
        }
        
        public virtual void Load()
        {
            Trace.Call();
            
            LoadServers();
            
            CheckConnectButton();
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
                
                f_Widget.Load(server);
                // we are not editing server entries here instead we use
                // whatever values are entered
                f_Widget.ProtocolComboBox.Sensitive = true;
                // this field doesn't make sense here
                f_Widget.NetworkComboBoxEntry.Sensitive = false;
                // only enable the hostname field if there it's not empty, as
                // some protocols don't allow custom hosts, e.g. twitter
                if (!String.IsNullOrEmpty(f_Widget.HostnameEntry.Text)) {
                    f_Widget.HostnameEntry.Sensitive = true;
                }
            } catch (ApplicationException ex) {
                Frontend.ShowError(this, _("Unable to load server: "), ex);
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
                
                f_ServerModel = server;
                Respond(Gtk.ResponseType.Ok);
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnConnectButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                f_ServerModel = f_Widget.GetServer();
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
            
            f_ConnectButton.Sensitive =
                !f_Widget.HostnameEntry.Visible ||
                f_Widget.HostnameEntry.Text.Trim().Length > 0;
            if (f_ConnectButton.Sensitive &&
                f_Widget.ProtocolComboBox.ActiveText == "Campfire" &&
                f_Widget.HostnameEntry.Text == ".campfirenow.com") {
                f_ConnectButton.Sensitive = false;
            }
        }
    }
}
