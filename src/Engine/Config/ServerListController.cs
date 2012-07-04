/*
 * $Id: PreferencesDialog.cs 142 2007-01-02 22:19:08Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/PreferencesDialog.cs $
 * $Rev: 142 $
 * $Author: meebey $
 * $Date: 2007-01-02 23:19:08 +0100 (Tue, 02 Jan 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
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

namespace Smuxi.Engine
{
    public class ServerListController
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string _LibraryTextDomain = "smuxi-engine";
        private UserConfig _UserConfig;
        
        public ServerListController(UserConfig userConfig)
        {
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }
            
            _UserConfig = userConfig;
        }
        
        public IList<ServerModel> GetServerList()
        {
            // load user servers
            string[] servers = (string[]) _UserConfig["Servers/Servers"];
            IList<ServerModel> serverList = new List<ServerModel>();
            if (servers == null) {
                return serverList;
            }
            foreach (string server in servers) {
                string[] serverParts = server.Split(new char[] {'/'});
                string protocol = serverParts[0];
                string servername = serverParts[1];
                ServerModel ser = GetServer(protocol, servername);
                if (ser == null) {
#if LOG4NET
                    _Logger.Error("GetServerList(): GetServer(" + protocol + ", " + servername +") returned null! ignoring...");
#endif 
                    continue;
                }
                serverList.Add(ser);
            }
            return serverList;
        }
        
        public ServerModel GetServer(string protocol, string servername)
        {
            Trace.Call(protocol, servername);
            
            if (protocol == null) {
                throw new ArgumentNullException("protocol");
            }
            if (servername == null) {
                throw new ArgumentNullException("servername");
            }
            
            string prefix = "Servers/" + protocol + "/" + servername + "/";
            ServerModel server = new ServerModel();
            if (_UserConfig[prefix + "Hostname"] == null) {
                // server does not exist
                return null;
            }
            server.Protocol    = protocol;
            server.Hostname    = (string) _UserConfig[prefix + "Hostname"];
            server.Port        = (int)    _UserConfig[prefix + "Port"];
            server.Network     = (string) _UserConfig[prefix + "Network"];
            server.Username    = (string) _UserConfig[prefix + "Username"];
            server.Password    = (string) _UserConfig[prefix + "Password"];
            server.UseEncryption = (bool) _UserConfig[prefix + "UseEncryption"];
            server.ValidateServerCertificate =
                (bool) _UserConfig[prefix + "ValidateServerCertificate"];
            if (_UserConfig[prefix + "OnStartupConnect"] != null) {
                server.OnStartupConnect = (bool) _UserConfig[prefix + "OnStartupConnect"];
            }
            server.OnConnectCommands  = _UserConfig[prefix + "OnConnectCommands"] as IList<string>;
            return server;
        }
        
        public IList<string> GetNetworks()
        {
            Trace.Call();
            
            IList<string> networks = new List<string>();
            IList<ServerModel> servers = GetServerList();
            foreach (ServerModel server in servers) {
                if (!networks.Contains(server.Network)) {
                    networks.Add(server.Network);
                }
            }
            return networks;
        }

        public ServerModel GetServerByNetwork(string network)
        {
            Trace.Call(network);

            if (network == null) {
                throw new ArgumentNullException("network");
            }
            if (network.Trim().Length == 0) {
                throw new InvalidOperationException(_("Network must not be empty."));
            }

            var servers = GetServerList();
            foreach (var server in servers) {
                if (String.Compare(server.Network, network, true) == 0) {
                    return server;
                }
            }
            return null;
        }

        public void AddServer(ServerModel server)
        {
            Trace.Call(server);
            
            if (server == null) {
                throw new ArgumentNullException("server");
            }
            if (String.IsNullOrEmpty(server.Hostname)) {
                throw new InvalidOperationException(_("Server hostname must not be empty."));
            }
            if (server.Hostname.Contains("\n")) {
                throw new InvalidOperationException(_("Server hostname contains invalid characters (newline)."));
            }
            foreach (var s in GetServerList()) {
                if (s.Protocol == server.Protocol &&
                    s.Hostname == server.Hostname) {
                    throw new InvalidOperationException(
                        String.Format(_("Server '{0}' already exists."),
                                      server.Hostname)
                    );
                }
            }

            string prefix = "Servers/" + server.Protocol + "/" + server.Hostname + "/";
            _UserConfig[prefix + "Hostname"] = server.Hostname;
            _UserConfig[prefix + "Port"]     = server.Port;
            _UserConfig[prefix + "Network"]  = server.Network;
            _UserConfig[prefix + "Username"] = server.Username;
            _UserConfig[prefix + "Password"] = server.Password;
            _UserConfig[prefix + "UseEncryption"] = server.UseEncryption;
            _UserConfig[prefix + "ValidateServerCertificate"] =
                server.ValidateServerCertificate;
            _UserConfig[prefix + "OnStartupConnect"] = server.OnStartupConnect;
            _UserConfig[prefix + "OnConnectCommands"] = server.OnConnectCommands;
            
            string[] servers = (string[]) _UserConfig["Servers/Servers"];
            if (servers == null) {
                servers = new string[] {};
            }
            List<string> serverList = new List<string>(servers);
            serverList.Add(server.Protocol + "/" + server.Hostname);
            _UserConfig["Servers/Servers"] = serverList.ToArray();
        }

        public void SetServer(ServerModel server)
        {
            Trace.Call(server);
            
            if (server == null) {
                throw new ArgumentNullException("server");
            }
            
            string prefix = "Servers/" + server.Protocol + "/" + server.Hostname + "/";
            _UserConfig[prefix + "Hostname"] = server.Hostname;
            _UserConfig[prefix + "Port"]     = server.Port;
            _UserConfig[prefix + "Network"]  = server.Network;
            _UserConfig[prefix + "Username"] = server.Username;
            _UserConfig[prefix + "Password"] = server.Password;
            _UserConfig[prefix + "UseEncryption"] = server.UseEncryption;
            _UserConfig[prefix + "ValidateServerCertificate"] =
                server.ValidateServerCertificate;
            _UserConfig[prefix + "OnStartupConnect"] = server.OnStartupConnect;
            _UserConfig[prefix + "OnConnectCommands"] = server.OnConnectCommands;
        }
        
        public void RemoveServer(string protocol, string servername)
        {
            Trace.Call(protocol, servername);
            
            if (protocol == null) {
                throw new ArgumentNullException("protocol");
            }
            if (servername == null) {
                throw new ArgumentNullException("servername");
            }
            
            string server = "Servers/" + protocol + "/" + servername + "/";
            _UserConfig.Remove(server);

            string[] servers = (string[]) _UserConfig["Servers/Servers"];
            if (servers == null) {
                servers = new string[] {};
            }
            List<string> serverList = new List<string>(servers);
            int idx = serverList.IndexOf(protocol + "/" + servername);
            serverList.RemoveAt(idx);
            _UserConfig["Servers/Servers"] = serverList.ToArray();
        }
        
        public void Save()
        {
            _UserConfig.Save();
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
