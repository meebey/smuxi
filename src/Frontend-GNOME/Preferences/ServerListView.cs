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
    public class ServerListView
    {
        private ServerListController _Controller;
        [Glade.Widget("ServersTreeView")]
        private Gtk.TreeView             _TreeView;
        private Gtk.TreeStore            _TreeStore;
        [Glade.Widget("ServersAddButton")]
        private Gtk.Button               _AddButton;
        [Glade.Widget("ServersEditButton")]
        private Gtk.Button               _EditButton;
        [Glade.Widget("ServersRemoveButton")]
        private Gtk.Button               _RemoveButton;
        
        public ServerListView(Glade.XML gladeXml)
        {
            _Controller = new ServerListController(Frontend.UserConfig);
            
            gladeXml.BindFields(this);
            
            _AddButton.Clicked += new EventHandler(_OnAddButtonClicked);
            _EditButton.Clicked += new EventHandler(_OnEditButtonClicked);
            _RemoveButton.Clicked += new EventHandler(_OnRemoveButtonClicked);
            
            _TreeView.AppendColumn(_("Protocol"), new Gtk.CellRendererText(), "text", 1); 
            _TreeView.AppendColumn(_("Hostname"), new Gtk.CellRendererText(), "text", 2); 
            
            _TreeStore = new Gtk.TreeStore(typeof(ServerModel),
                                           typeof(string), // protocol
                                           typeof(string) // hostname
                                           );
            _TreeView.Model = _TreeStore;
        }
        
        private void _OnAddButtonClicked(object sender, EventArgs e)
        {
            try {
                ServerView serverView = new ServerView(null, Frontend.Session.GetSupportedProtocols(), _Controller.GetNetworks());
                int res = serverView.Run();
                serverView.Destroy();
                if ((Gtk.ResponseType)res == Gtk.ResponseType.Ok) {
                    _Controller.AddServer(serverView.Server);
                    _Controller.Save();
                    Load();
                }
                
                /*
                Gtk.TreeIter iter = _ListStore.AppendValues(new ServerModel(), String.Empty, false, false, false);
                _TreeView.Selection.SelectIter(iter);
                */
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        private void _OnEditButtonClicked(object sender, EventArgs e)
        {
            try {
                Gtk.TreeIter iter;
                if (!_TreeView.Selection.GetSelected(out iter)) {
                    return;
                }
                ServerModel server = (ServerModel) _TreeStore.GetValue(iter, 0);
                
                ServerView serverView = new ServerView(server, Frontend.Session.GetSupportedProtocols(), _Controller.GetNetworks());
                int res = serverView.Run();
                serverView.Destroy();
                if ((Gtk.ResponseType)res == Gtk.ResponseType.Ok) {
                    _Controller.SetServer(serverView.Server);
                    _Controller.Save();
                    Load();
                }
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        private void _OnRemoveButtonClicked(object sender, EventArgs e)
        {
            try {
                Gtk.TreeIter iter;
                if (!_TreeView.Selection.GetSelected(out iter)) {
                    return;
                }
                ServerModel server = (ServerModel) _TreeStore.GetValue(iter, 0);
                Gtk.MessageDialog md = new Gtk.MessageDialog(null,
                                                             Gtk.DialogFlags.Modal,
                                                             Gtk.MessageType.Warning,
                                                             Gtk.ButtonsType.YesNo,
                    _("Are you sure you want to delete the selected server?"));
                int result = md.Run();
                md.Destroy();
                if ((Gtk.ResponseType)result == Gtk.ResponseType.Yes) {
                    _Controller.RemoveServer(server.Protocol, server.Hostname);
                    _Controller.Save();
                    Load();
                }
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        public void Load()
        {
            _TreeStore.Clear();
            
            // sort servers
            Dictionary<string, List<ServerModel>> protocols = new Dictionary<string,List<ServerModel>>();
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
            
            // add sorted servers to treeview
            foreach (KeyValuePair<string, List<ServerModel>> pair in protocols) {
                Gtk.TreeIter parentIter = _TreeStore.AppendValues(null, pair.Key, String.Empty);
                foreach (ServerModel server in pair.Value) {
                    _TreeStore.AppendValues(parentIter, server, String.Empty, server.Hostname);
                }
            }
            
            _TreeView.ExpandAll();
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
