/*
 * $Id: ChannelPage.cs 138 2006-12-23 17:11:57Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/ChannelPage.cs $
 * $Rev: 138 $
 * $Author: meebey $
 * $Date: 2006-12-23 18:11:57 +0100 (Sat, 23 Dec 2006) $
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
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    public partial class ConnectDialog : Gtk.Dialog
    {
        private ServerListController f_Controller;
        private Gtk.TreeStore        f_TreeStore;
        private ServerModel          f_ServerModel;
        
        public ServerModel Server {
            get {
                return f_ServerModel;
            }
        }
        
        public ConnectDialog()
        {
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

        protected virtual void OnTreeViewSelectionChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                if (GetCurrentServer() == null) {
                    f_ConnectButton.Sensitive = false;
                } else {
                    f_ConnectButton.Sensitive = true;
                }
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnConnectButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                f_ServerModel = GetCurrentServer();
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
        
        protected virtual ServerModel GetCurrentServer()
        {
            Trace.Call();
            
            Gtk.TreeIter iter;
            if (!f_TreeView.Selection.GetSelected(out iter)) {
                return null;
            }
            return (ServerModel) f_TreeStore.GetValue(iter, 0);
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
