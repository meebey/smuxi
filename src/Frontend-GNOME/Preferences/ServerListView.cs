/*
 * $Id: PreferencesDialog.cs 142 2007-01-02 22:19:08Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/PreferencesDialog.cs $
 * $Rev: 142 $
 * $Author: meebey $
 * $Date: 2007-01-02 23:19:08 +0100 (Tue, 02 Jan 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2008 Mirco Bauer <meebey@meebey.net>
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
    public class ServerListView
    {
        private ServerListController     _Controller;
        private Gtk.Window               _Parent;
        
#region Widgets
        [Glade.Widget("ServersTreeView")]
        private Gtk.TreeView             _TreeView;
        private Gtk.TreeStore            _TreeStore;
        [Glade.Widget("ServersAddButton")]
        private Gtk.Button               _AddButton;
        [Glade.Widget("ServersEditButton")]
        private Gtk.Button               _EditButton;
        [Glade.Widget("ServersRemoveButton")]
        private Gtk.Button               _RemoveButton;
#endregion
        
        public ServerListView(Gtk.Window parent, Glade.XML gladeXml)
        {
            Trace.Call(parent, gladeXml);
            
            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            
            _Parent = parent;
            _Controller = new ServerListController(Frontend.UserConfig);
            
            gladeXml.BindFields(this);
            
            _AddButton.Clicked += new EventHandler(OnAddButtonClicked);
            _EditButton.Clicked += new EventHandler(OnEditButtonClicked);
            _RemoveButton.Clicked += new EventHandler(OnRemoveButtonClicked);
            
            _TreeView.AppendColumn(_("Protocol"), new Gtk.CellRendererText(), "text", 1); 
            _TreeView.AppendColumn(_("Hostname"), new Gtk.CellRendererText(), "text", 2); 
            
            _TreeStore = new Gtk.TreeStore(typeof(ServerModel),
                                           typeof(string), // protocol
                                           typeof(string) // hostname
                                           );
            _TreeView.RowActivated += OnTreeViewRowActivated;
            _TreeView.Selection.Changed += OnTreeViewSelectionChanged;
            _TreeView.Model = _TreeStore;
        }
        
        public virtual void Load()
        {
            Trace.Call();
            
            _TreeStore.Clear();
            
            // group servers by protocol
            Dictionary<string, List<ServerModel>> protocols = new Dictionary<string, List<ServerModel>>();
            IList<ServerModel> servers = _Controller.GetServerList();
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
                Gtk.TreeIter parentIter = _TreeStore.AppendValues(null, pair.Key, String.Empty);
                foreach (ServerModel server in pair.Value) {
                    // a server with an empty hostname has only one default/hardcoded
                    // hostname, thus don't create a sub-node for it
                    if (String.IsNullOrEmpty(server.Hostname)) {
                        _TreeStore.SetValue(parentIter, 0, server);
                        continue;
                    }
                    
                    _TreeStore.AppendValues(parentIter, server, String.Empty, server.Hostname);
                }
            }
            
            _TreeView.ExpandAll();
        }

        public virtual ServerModel GetCurrentServer()
        {
            Trace.Call();
            
            Gtk.TreeIter iter;
            if (!_TreeView.Selection.GetSelected(out iter)) {
                return null;
            }
            return (ServerModel) _TreeStore.GetValue(iter, 0);
        }
        
        public virtual void Add()
        {
            Trace.Call();
            
            ServerDialog dialog = new ServerDialog(_Parent, null, Frontend.Session.GetSupportedProtocols(), _Controller.GetNetworks());
            try {
                int res = dialog.Run();
                ServerModel server = dialog.GetServer();
                if (res != (int) Gtk.ResponseType.Ok) {
                    return;
                }

                _Controller.AddServer(server);
                _Controller.Save();

                // refresh view
                Load();
            } finally {
                dialog.Destroy();
            }
        }
        
        public virtual void Edit(ServerModel server)
        {
            Trace.Call(server);
            
            if (server == null) {
                throw new ArgumentNullException("server");
            }
            
            ServerDialog dialog = new ServerDialog(_Parent, server, Frontend.Session.GetSupportedProtocols(), _Controller.GetNetworks());
            int res = dialog.Run();
            server = dialog.GetServer();
            dialog.Destroy();
            if (res != (int) Gtk.ResponseType.Ok) {
                return;
            }
            
            _Controller.SetServer(server);
            _Controller.Save();
            
            // refresh the view
            Load();
        }
        
        public virtual void Remove(ServerModel server)
        {
            Trace.Call(server);
            
            if (server == null) {
                throw new ArgumentNullException("server");
            }
            
            Gtk.MessageDialog md = new Gtk.MessageDialog(null,
                                                         Gtk.DialogFlags.Modal,
                                                         Gtk.MessageType.Warning,
                                                         Gtk.ButtonsType.YesNo,
                _("Are you sure you want to delete the selected server?"));
            int result = md.Run();
            md.Destroy();
            if (result != (int) Gtk.ResponseType.Yes) {
                return;
            }
            
            _Controller.RemoveServer(server.Protocol, server.Hostname);
            _Controller.Save();
            
            // refresh the view
            Load();
        }
        
        protected virtual void OnTreeViewSelectionChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                if (GetCurrentServer() == null) {
                    _EditButton.Sensitive = false;
                    _RemoveButton.Sensitive = false;
                } else {
                    _EditButton.Sensitive = true;
                    _RemoveButton.Sensitive = true;
                }
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnAddButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                Add();
            } catch (InvalidOperationException ex) {
                Frontend.ShowError(_Parent, _("Unable to add server: "), ex);
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        protected virtual void OnEditButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ServerModel server = GetCurrentServer();
                if (server == null) {
                    return;
                }
                
                Edit(server);
            } catch (ApplicationException ex) {
                Frontend.ShowError(_Parent, _("Unable to edit server: "), ex);
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        protected virtual void OnRemoveButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ServerModel server = GetCurrentServer();
                if (server == null) {
                    return;
                }
                
                Remove(server);
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
                
                Edit(server);
            } catch (ApplicationException ex) {
                Frontend.ShowError(_Parent, _("Unable to edit server: "), ex);
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
