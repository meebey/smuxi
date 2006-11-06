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
            
            // if this was the first frontend, we process OnStartupCommands
            if (_FrontendManagers.Count == 1) {
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
                        _CommandHelp(cd);
                        break;
                    case "server":
                    case "connect":
                        _CommandConnect(cd);
                        handled = true;
                        break;
                    case "disconnect":
                        _CommandDisconnect(cd);
                        handled = true;
                        break;
                    case "reconnect":
                        _CommandReconnect(cd);
                        handled = true;
                        break;
                    case "config":
                        _CommandConfig(cd);
                        handled = true;
                        break;
                    case "quit":
                        _CommandQuit(cd);
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
        
        private void _CommandHelp(CommandData cd)
        {
            string[] help = {
            "[Engine Commands]",
            "help",
            "connect/server [server] [port] [password] [nick]",
            "disconnect",
            "config (save|load)",
            "quit [quitmessage]",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToCurrentPage("-!- "+line);
            }
        }
        
        private void _CommandConnect(CommandData cd)
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
            if (fm.CurrentNetworkManager != null &&
                fm.CurrentNetworkManager is IrcNetworkManager &&
                !fm.CurrentNetworkManager.IsConnected) {
                ircm = (IrcNetworkManager)fm.CurrentNetworkManager;
            } else {
                ircm = new IrcNetworkManager(this);
                _NetworkManagers.Add(ircm);
            }
            ircm.Connect(fm, server, port, nicks, user, pass);
            
            if (fm.CurrentNetworkManager == null) {
                // only set this new network manager if there was none set
                fm.CurrentNetworkManager = ircm;
                fm.UpdateNetworkStatus();
            }
        }
        
        private void _CommandDisconnect(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                string server = cd.DataArray[1];
                foreach (INetworkManager nm in _NetworkManagers) {
                    if (nm is IrcNetworkManager) {
                        IrcNetworkManager im = (IrcNetworkManager)nm;
                        if (im.Server.ToLower() == server.ToLower()) {
                            im.Disconnect(fm);
                            _NetworkManagers.Remove(im);
                            break;
                        }
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
        
        private void _CommandReconnect(CommandData cd)
        {
        }
        
        private void _CommandConfig(CommandData cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                switch (cd.DataArray[1].ToLower()) {
                    case "load":
                        _Config.Load();
                        fm.AddTextToCurrentPage("-!- Configuration reloaded");
                        break;
                    case "save":
                        _Config.Save();
                        fm.AddTextToCurrentPage("-!- Configuration saved");
                        break;
                    default:
                        fm.AddTextToCurrentPage("-!- wrong paramater for config, use load or save");
                        break;
                }
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        private void _CommandQuit(CommandData cd)
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
        
        private void _NotConnected(CommandData cd)
        {
            cd.FrontendManager.AddTextToCurrentPage("-!- " + _("Not connected to any network"));
        }
        
        private void _NotEnoughParameters(CommandData cd)
        {
            cd.FrontendManager.AddTextToCurrentPage(
                "-!- " + String.Format(_("Not enough parameters for {0} command"), cd.Command));
        }
        
        public void AddPage(Page page)
        {
#if LOG4NET
            _Logger.Debug("AddPage() page.Name: "+page.Name);
#endif
            _Pages.Add(page);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddPage(page);
                fm.SyncPage(page);
            }
        }
        
        public void RemovePage(Page page)
        {
#if LOG4NET
            _Logger.Debug("RemovePage() page.Name: "+page.Name);
#endif
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
