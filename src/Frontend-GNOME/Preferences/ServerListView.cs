/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2008, 2010, 2012-2013, 2016 Mirco Bauer <meebey@meebey.net>
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
#if GTK_SHARP_3
using TreeModel = Gtk.ITreeModel;
#else
using TreeModel = Gtk.TreeModel;
#endif
using UI = Gtk.Builder.ObjectAttribute;

namespace Smuxi.Frontend.Gnome
{
    public class ServerListView : Gtk.Bin
    {
        private ServerListController     _Controller;
        private Gtk.Window               _Parent;
        
#region Widgets
        [UI("ServersTreeView")]
        private Gtk.TreeView             _TreeView;
        private Gtk.TreeStore            _TreeStore;
        [UI("EditServerToolButton")]
        Gtk.ToolButton _EditButton;
        [UI("RemoveServerToolButton")]
        Gtk.ToolButton _RemoveButton;
#endregion
        
        public ServerListView(Gtk.Window parent)
        {
            Trace.Call(parent);

            if (parent == null) {
                throw new ArgumentNullException("parent");
            }

            _Parent = parent;

            Build();
            Init();
            ShowAll();
        }

        void Build()
        {
            global::Stetic.BinContainer.Attach(this);
            var builder = new Gtk.Builder(null, "ServerListWidget.ui", null);
            builder.Autoconnect(this);
            var box = (Gtk.Widget) builder.GetObject("ServerListBox");
            Add(box);
        }

        void Init()
        {
            _Controller = new ServerListController(Frontend.UserConfig);

            _TreeView.AppendColumn(_("Protocol"), new Gtk.CellRendererText(), "text", 1); 
            _TreeView.AppendColumn(_("Hostname"), new Gtk.CellRendererText(), "text", 2); 
            
            _TreeStore = new Gtk.TreeStore(typeof(ServerModel),
                                           typeof(string), // protocol
                                           typeof(string) // hostname
                                           );
            _TreeStore.SetSortColumnId(0, Gtk.SortType.Ascending);
            _TreeStore.SetSortFunc(0, SortTreeStore);
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
            
            var dialog = new ServerDialog(_Parent, null,
                                          Frontend.Session.GetSupportedProtocols(),
                                          _Controller.GetNetworks());
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
            
            var dialog = new ServerDialog(_Parent, server,
                                          Frontend.Session.GetSupportedProtocols(),
                                          _Controller.GetNetworks());
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
            
            _Controller.RemoveServer(server.Protocol, server.ServerID);
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

        protected virtual int SortTreeStore(TreeModel model,
                                            Gtk.TreeIter iter1,
                                            Gtk.TreeIter iter2)
        {
            var server1 = (ServerModel) model.GetValue(iter1, 0);
            var server2 = (ServerModel) model.GetValue(iter2, 0);
            // protocol nodes don't have a ServerModel
            if (server1 == null && server2 == null) {
                return 0;
            }
            if (server2 == null) {
                return 1;
            }
            if (server1 == null) {
                return -1;
            }
            var s1 = String.Format("{0}/{1}:{2} ({3})",
                                   server1.Protocol, server1.Hostname,
                                   server1.Port, server1.ServerID);
            var s2 = String.Format("{0}/{1}:{2} ({3})",
                                   server2.Protocol, server2.Hostname,
                                   server2.Port, server2.ServerID);
            return s1.CompareTo(s2);
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
