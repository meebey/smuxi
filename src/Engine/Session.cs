/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
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
using System.Collections;
using System.Runtime.Remoting;
using Meebey.Smuxi.Common;

namespace Meebey.Smuxi.Engine
{
    public class Session : PermanentRemoteObject, IFrontendUI 
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private int        _Version = 0;
        private Hashtable  _FrontendManagers = Hashtable.Synchronized(new Hashtable());
        private ArrayList  _NetworkManagers = ArrayList.Synchronized(new ArrayList());
        private ArrayList  _Pages = ArrayList.Synchronized(new ArrayList());
        private Config     _Config;
        private UserConfig _UserConfig;
        private bool       _OnStartupCommandsProcessed;
        
        public ArrayList NetworkManagers {
            get {
                return _NetworkManagers;
            }
        }
        
        public ArrayList Pages {
            get {
                return _Pages;
            }
        }
    
        public int Version {
            get {
                return _Version;
            }
        }

        public Config Config {
            get {
                return _Config;
            }
        }
        
        public UserConfig UserConfig {
            get {
                return _UserConfig;
            }
        }
        
        public Session(Config config, string username)
        {
            _Config = config;
            _UserConfig = new UserConfig(config, username);
            
            Page spage = new Page("Server", PageType.Server, NetworkType.Irc, null);
            _Pages.Add(spage);
            FormattedMessage fm = new FormattedMessage();
            fm.Items.Add(
                new FormattedMessageItem(FormattedMessageItemType.Text,
                    new FormattedMessageTextItem(IrcTextColor.Red, null, false,
                        true, false, _("Welcome to Smuxi"))));
            AddMessageToPage(spage, fm); 
        }
        
        public void RegisterFrontendUI(IFrontendUI ui)
        {
            string uri = RemotingServices.GetObjectUri((MarshalByRefObject)ui);
            if (uri == null) {
                uri = "local";
            }
#if LOG4NET
            _Logger.Debug("Registering UI with URI: "+uri);
#endif
            // add the FrontendManager to the hashtable with an unique .NET remoting identifier
            FrontendManager fm = new FrontendManager(this, ui);
            _FrontendManagers.Add(uri, fm);
            
            // if this is the first frontend, we process OnStartupCommands
            if (!_OnStartupCommandsProcessed) {
                _OnStartupCommandsProcessed = true;
                foreach (string command in (string[])_UserConfig["OnStartupCommands"]) {
                    if (command.Length == 0) {
                        continue;
                    }
                    CommandData cd = new CommandData(fm,
                        (string)_UserConfig["Interface/Entry/CommandCharacter"],
                        command);
                    bool handled;
                    handled = Command(cd);
                    if (!handled) {
                        if (fm.CurrentNetworkManager != null) {
                            fm.CurrentNetworkManager.Command(cd);
                        }
                    }
                }
            }
        }
        
        public void DeregisterFrontendUI(IFrontendUI ui)
        {
            string uri = RemotingServices.GetObjectUri((MarshalByRefObject)ui);
            if (uri == null) {
                uri = "local";
            }
#if LOG4NET
            _Logger.Debug("Deregistering UI with URI: "+uri);
#endif
            _FrontendManagers.Remove(uri);
        }
        
        public FrontendManager GetFrontendManager(IFrontendUI ui)
        {
            string uri = RemotingServices.GetObjectUri((MarshalByRefObject)ui);
            if (uri == null) {
                uri = "local";
            }
            return (FrontendManager)_FrontendManagers[uri];
        }
        
        public Page GetPage(string name, PageType ptype, NetworkType ntype, INetworkManager nm)
        {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            
            foreach (Page page in _Pages) {
                if ((page.Name.ToLower() == name.ToLower()) &&
                    (page.PageType == ptype) &&
                    (page.NetworkType == ntype) &&
                    (page.NetworkManager == nm)) {
                    return page;
                }
            }
            
            return null;
        }
        
        public bool Command(CommandData cd)
        {
            bool handled = false;
            if (cd.IsCommand) {
                switch (cd.Command) {
                    case "help":
                        CommandHelp(cd);
                        break;
                    case "server":
                    case "connect":
                        CommandConnect(cd);
                        handled = true;
                        break;
                    case "disconnect":
                        CommandDisconnect(cd);
                        handled = true;
                        break;
                    case "reconnect":
                        CommandReconnect(cd);
                        handled = true;
                        break;
                    case "network":
                        CommandNetwork(cd);
                        handled = true;
                        break;
                    case "config":
                        CommandConfig(cd);
                        handled = true;
                        break;
                    case "quit":
                        CommandQuit(cd);
                        handled = true;
                        break;
                }
            } else {
                // normal text
                if (cd.FrontendManager.CurrentNetworkManager == null) {
                    _NotConnected(cd);
                    handled = true;
                }
            }
            
            return handled;
        }
        
        public void CommandHelp(CommandData cd)
        {
            string[] help = {
            "[Engine Commands]",
            "help",
            "connect/server [server] [port] [password] [nick]",
            "disconnect",
            "network list",
            "network close [server]",
            "network switch [server]",
            "config (save|load)",
            "quit [quitmessage]",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToCurrentPage("-!- "+line);
            }
        }
        
        public void CommandConnect(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            
            string server;
            if (cd.DataArray.Length >= 2) {
                server = cd.DataArray[1];
            } else {
                server = "localhost";
            }
            
            int port;
            if (cd.DataArray.Length >= 3) {
                try {
                    port = Int32.Parse(cd.DataArray[2]);
                } catch (FormatException) {
                    fm.AddTextToCurrentPage("-!- " + String.Format(
                                                        _("Invalid port: {0}"),
                                                        cd.DataArray[2]));
                    return;
                }
            } else {
                port = 6667;
            }
            
            string pass;                
            if (cd.DataArray.Length >=4) {
                pass = cd.DataArray[3];
            } else {
                pass = null;
            }
            
            string[] nicks;
            if (cd.DataArray.Length >= 5) {
                nicks = new string[] {cd.DataArray[4]};
            } else {
                nicks = (string[])UserConfig["Connection/Nicknames"];
            }
            
            string user = (string)UserConfig["Connection/Username"];
            
            IrcNetworkManager ircm;
            ircm = new IrcNetworkManager(this);
            ircm.Connect(fm, server, port, nicks, user, pass);
            _NetworkManagers.Add(ircm);
            
            // set this as current network manager
            fm.CurrentNetworkManager = ircm;
            fm.UpdateNetworkStatus();
        }
        
        public void CommandDisconnect(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                string server = cd.DataArray[1];
                foreach (INetworkManager nm in _NetworkManagers) {
                    if (nm.Host.ToLower() == server.ToLower()) {
                        nm.Disconnect(fm);
                        _NetworkManagers.Remove(nm);
                        return;
                    }
                }
                fm.AddTextToCurrentPage("-!- " + String.Format(
                                                    _("Disconnect failed, could not find server: {0}"),
                                                    server));
            } else {
                fm.CurrentNetworkManager.Disconnect(fm);
                _NetworkManagers.Remove(fm.CurrentNetworkManager);
            }
        }
        
        public void CommandReconnect(CommandData cd)
        {
        }
        
        public void CommandQuit(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            string message = cd.Parameter;
            foreach (INetworkManager nm in _NetworkManagers) {
                if (message == null) {
                    nm.Disconnect(fm);
                } else {
                    if (nm is IrcNetworkManager) {
                        IrcNetworkManager im = (IrcNetworkManager)nm;
                        im.CommandQuit(cd);
                    } else {
                        nm.Disconnect(fm);
                    }
                }
            }
        }
        
        public void CommandConfig(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                switch (cd.DataArray[1].ToLower()) {
                    case "load":
                        _Config.Load();
                        fm.AddTextToCurrentPage("-!- " +
                            _("Configuration reloaded"));
                        break;
                    case "save":
                        _Config.Save();
                        fm.AddTextToCurrentPage("-!- " +
                            _("Configuration saved"));
                        break;
                    default:
                        fm.AddTextToCurrentPage("-!- " + 
                            _("Invalid paramater for config, use load or save"));
                        break;
                }
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        public void CommandNetwork(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                switch (cd.DataArray[1].ToLower()) {
                    case "list":
                        _CommandNetworkList(cd);
                        break;
                    case "switch":
                        _CommandNetworkSwitch(cd);
                        break;
                    case "close":
                        _CommandNetworkClose(cd);
                        break;
                    default:
                        fm.AddTextToCurrentPage("-!- " + 
                            _("Invalid paramater for network, use list, switch or close"));
                        break;
                }
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        private void _CommandNetworkList(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            fm.AddTextToCurrentPage("-!- " + _("Networks") + ":");
            foreach (INetworkManager nm in _NetworkManagers) {
                fm.AddTextToCurrentPage("-!- " +
                    _("Type") + ": " + nm.Type.ToString().ToUpper() + " " +
                    _("Host") + ": " + nm.Host + " " + 
                    _("Port") + ": " + nm.Port);
            }
        }
        
        private void _CommandNetworkClose(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 3) {
                // named network manager
                string host = cd.DataArray[2].ToLower();
                foreach (INetworkManager nm in _NetworkManagers) {
                    if (nm.Host.ToLower() == host) {
                        nm.Disconnect(fm);
                        nm.Dispose();
                        _NetworkManagers.Remove(nm);
                        fm.NextNetworkManager();
                        return;
                    }
                }
                fm.AddTextToCurrentPage("-!- " +
                    String.Format(_("Network switch failed, could not find network with host: {0}"),
                                  host));
            } else if (cd.DataArray.Length >= 2) {
                // current network manager
                fm.CurrentNetworkManager.Disconnect(fm);
                fm.CurrentNetworkManager.Dispose();
                _NetworkManagers.Remove(fm.CurrentNetworkManager);
                fm.NextNetworkManager();
            }
        }
        
        private void _CommandNetworkSwitch(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 3) {
                // named network manager
                string host = cd.DataArray[2].ToLower();
                foreach (INetworkManager nm in _NetworkManagers) {
                    if (nm.Host.ToLower() == host) {
                        fm.CurrentNetworkManager = nm;
                        fm.UpdateNetworkStatus();
                        return;
                    }
                }
                fm.AddTextToCurrentPage("-!- " +
                    String.Format(_("Network switch failed, could not find network with host: {0}"),
                                  host));
            } else if (cd.DataArray.Length >= 2) {
                // next network manager
                fm.NextNetworkManager();
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        private void _NotConnected(CommandData cd)
        {
            cd.FrontendManager.AddTextToCurrentPage("-!- " + _("Not connected to any network"));
        }
        
        private void _NotEnoughParameters(CommandData cd)
        {
            cd.FrontendManager.AddTextToCurrentPage("-!- " +
                String.Format(_("Not enough parameters for {0} command"), cd.Command));
        }
        
        public void UpdateNetworkStatus()
        {
            Trace.Call();
            
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdateNetworkStatus();
            }
        }
        
        public void AddPage(Page page)
        {
        	Trace.Call(page);
        	
            _Pages.Add(page);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddPage(page);
                fm.SyncPage(page);
            }
        }
        
        public void RemovePage(Page page)
        {
        	Trace.Call(page);
        	
            _Pages.Remove(page);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.RemovePage(page);
            }
        }
        
        public void EnablePage(Page page)
        {
        	Trace.Call(page);
        	
        	page.IsEnabled = true;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.EnablePage(page);
            }
        }
        
        public void DisablePage(Page page)
        {
        	Trace.Call(page);
        	
        	page.IsEnabled = false;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.DisablePage(page);
            }
        }
        
        public void SyncPage(Page page)
        {
        	Trace.Call(page);
        	
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.SyncPage(page);
            }
        }
        
        public void AddTextToPage(Page page, string text)
        {
            AddMessageToPage(page, new FormattedMessage(text));
        }
        
        public void AddMessageToPage(Page page, FormattedMessage fmsg)
        {
            int buffer_lines = (int)UserConfig["Interface/Notebook/EngineBufferLines"];
            if (buffer_lines > 0) {
                page.UnsafeBuffer.Add(fmsg);
                if (page.UnsafeBuffer.Count > buffer_lines) {
                    page.UnsafeBuffer.RemoveAt(0);
                }
            }
            
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddMessageToPage(page, fmsg);
            }
        }
        
        public void AddUserToChannel(ChannelPage cpage, User user)
        {
#if LOG4NET
            _Logger.Debug("AddUserToChannel() cpage.Name: "+cpage.Name+" user.Nickname: "+user.Nickname);
#endif
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddUserToChannel(cpage, user);
            }
        }
        
        public void UpdateUserInChannel(ChannelPage cpage, User olduser, User newuser)
        {
#if LOG4NET
            _Logger.Debug("UpdateUserInChannel() cpage.Name: "+cpage.Name+" olduser.Nickname: "+olduser.Nickname+" newuser.Nickname: "+newuser.Nickname);
#endif
            cpage.UnsafeUsers.Remove(olduser.Nickname.ToLower());
            cpage.UnsafeUsers.Add(newuser.Nickname.ToLower(), newuser);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdateUserInChannel(cpage, olduser, newuser);
            }
        }
    
        public void UpdateTopicInChannel(ChannelPage cpage, string topic)
        {
#if LOG4NET
            _Logger.Debug("UpdateTopicInChannel() cpage.Name: "+cpage.Name+" topic: "+topic);
#endif
            cpage.Topic = topic;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdateTopicInChannel(cpage, topic);
            }
        }
    
        public void RemoveUserFromChannel(ChannelPage cpage, User user)
        {
#if LOG4NET
            _Logger.Debug("RemoveUserFromChannel() cpage.Name: "+cpage.Name+" user.Nickname: "+user.Nickname);
#endif
            cpage.UnsafeUsers.Remove(user.Nickname.ToLower());
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.RemoveUserFromChannel(cpage, user);
            }
        }
        
        public void SetNetworkStatus(string status)
        {
#if LOG4NET
            _Logger.Debug("SetNetworkStatus() status: "+status);
#endif
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.SetNetworkStatus(status);
            }
        }
        
        public void SetStatus(string status)
        {
#if LOG4NET
            _Logger.Debug("SetStatus() status: "+status);
#endif
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.SetStatus(status);
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
