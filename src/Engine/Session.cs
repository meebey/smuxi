/**
 * $Id: AssemblyInfo.cs 34 2004-09-05 14:46:59Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/Gnosmirc/trunk/src/AssemblyInfo.cs $
 * $Rev: 34 $
 * $Author: meebey $
 * $Date: 2004-09-05 16:46:59 +0200 (Sun, 05 Sep 2004) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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

namespace Meebey.Smuxi.Engine
{
    public class Session : PermanentComponent, IFrontendUI 
    {
        private int        _Version = 0;
        private Hashtable  _FrontendManagers = Hashtable.Synchronized(new Hashtable());
        private ArrayList  _NetworkManagers = ArrayList.Synchronized(new ArrayList());
        private ArrayList  _Pages = ArrayList.Synchronized(new ArrayList());
        private Config     _Config;
        private UserConfig _UserConfig;
        
        public ArrayList NetworkManagers
        {
            get {
                return _NetworkManagers;
            }
        }
        
        public ArrayList Pages
        {
            get {
                return _Pages;
            }
        }
    
        public int Version
        {
            get {
                return _Version;
            }
        }

        public Config Config
        {
            get {
                return _Config;
            }
        }
                    
        public UserConfig UserConfig
        {
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
        }
        
        public void RegisterFrontendUI(IFrontendUI ui)
        {
            string uri = RemotingServices.GetObjectUri((MarshalByRefObject)ui);
            if (uri == null) {
                uri = "local";
            }
#if LOG4NET
            Logger.UI.Debug("Registering UI with URI: "+uri);
#endif
            // add the FrontendManager to the hashtable with an unique .NET remoting identifier
            _FrontendManagers.Add(uri, new FrontendManager(this, ui));
        }
        
        public void DeregisterFrontendUI(IFrontendUI ui)
        {
            string uri = RemotingServices.GetObjectUri((MarshalByRefObject)ui);
            if (uri == null) {
                uri = "local";
            }
#if LOG4NET
            Logger.UI.Debug("Deregistering UI with URI: "+uri);
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
        
        public bool Command(FrontendManager fm, string data)
        {
            bool handled = false;
            string[] dataex = data.Split(new char[] {' '});
            string parameter = String.Join(" ", dataex, 1, dataex.Length-1);
            string command = (dataex[0].Length > 1) ? dataex[0].Substring(1).ToLower() : "";
            bool is_command = (data[0] == ((string)UserConfig["Interface/Entry/CommandCharacter"])[0]);
            if (is_command) {
                switch (command) {
                    case "server":
                    case "connect":
                        _CommandConnect(fm, data, dataex, parameter);
                        handled = true;
                        break;
                    case "disconnect":
                        _CommandDisconnect(fm, data, dataex, parameter);
                        handled = true;
                        break;
                    case "reconnect":
                        _CommandReconnect(fm, data, dataex, parameter);
                        handled = true;
                        break;
                    case "config":
                        _CommandConfig(fm, data, dataex, parameter);
                        handled = true;
                        break;
                    case "help":
                        _CommandHelp(fm, data, dataex, parameter);
                        break;
                }
            } else {
                // normal text
                if (fm.CurrentNetworkManager == null) {
                    _CommandNotConnected(fm, data, dataex, parameter);
                    handled = true;
                }
            }
            
            return handled;
        }
        
        private void _CommandHelp(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            string[] help = {
            "Gnosmirc commands:",
            "help",
            "connect/server [server] [port] [password] [nick]",
            "window (number|close)",
            "join/j channel(s) [key]",
            "part/p [channel(s)] [partmessage]",
            "topic [newtopic]",
            "cycle/rejoin",
            "msg/query nick message",
            "me actionmessage",
            "notice (channel|nick) message",
            "invite nick",
            "whois nick",
            "whowas nick",
            "ping nick",
            "mode newmode",
            "away [awaymessage]",
            "kick nick(s) [reason]",
            "kickban/kb nick(s) [reason]",
            "ban mask",
            "unban mask",
            "voice nick",
            "devoice nick",
            "op nick",
            "deop nick",
            "nick newnick",
            "raw/quote irccommand",
            "exec command",
            "quit [quitmessage]",
            "config (save|load)",
            };
            
            foreach (string line in help) { 
                fm.AddTextToCurrentPage(line);
            }
        }
        
        private void _CommandConnect(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            string server;
            if (dataex.Length >= 2) {
                server = dataex[1];
            } else {
                server = "localhost";
            }
            
            int port;
            if (dataex.Length >= 3) {
                try {
                    port = Int32.Parse(dataex[2]);
                } catch (FormatException) {
                    fm.AddTextToCurrentPage("-!- Invalid port: "+dataex[2]);
                    return;
                }
            } else {
                port = 6667;
            }
            
            string pass;                
            if (dataex.Length >=4) {
                pass = dataex[3];
            } else {
                pass = null;
            }
            
            string[] nicks;
            if (dataex.Length >= 5) {
                nicks = new string[] {dataex[4]};
            } else {
                nicks = (string[])UserConfig["Connection/Nicknames"];
            }
            
            string user = (string)UserConfig["Connection/Username"];
            IrcManager ircm = new IrcManager(this);
            _NetworkManagers.Add(ircm);
            ircm.Connect(fm, server, port, nicks, user, pass);
            if (fm.CurrentNetworkManager == null) {
                // only set this new network manager if there was none set
                fm.CurrentNetworkManager = ircm;
                fm.UpdateNetworkStatus();
            }
        }
        
        private void _CommandDisconnect(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            fm.CurrentNetworkManager.Disconnect(fm);
            _NetworkManagers.Remove(fm.CurrentNetworkManager);
        }
        
        private void _CommandReconnect(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            //fm.CurrentNetworkManager.Reconnect(fm);
        }
        
        private void _CommandConfig(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 2) {
                switch (dataex[1].ToLower()) {
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
                fm.AddTextToCurrentPage("-!- Not enough parameters for config command");
            }
        }
        
        private void _CommandNotConnected(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            fm.AddTextToCurrentPage("-!- Not connected to any network");
        }
        
        public void AddPage(Page page)
        {
#if LOG4NET
            Logger.Session.Debug("AddPage() page.Name: "+page.Name);
#endif
            _Pages.Add(page);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddPage(page);
            }
        }
        
        public void AddTextToPage(Page page, string text)
        {
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddTextToPage(page, text);
            }
        }
        
        public void RemovePage(Page page)
        {
#if LOG4NET
            Logger.Session.Debug("RemovePage() page.Name: "+page.Name);
#endif
            _Pages.Remove(page);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.RemovePage(page);
            }
        }
        
        public void AddUserToChannel(ChannelPage cpage, User user)
        {
#if LOG4NET
            Logger.Session.Debug("AddUserToChannel() cpage.Name: "+cpage.Name+" user.Nickname: "+user.Nickname);
#endif
            cpage.Users.Add(user.Nickname.ToLower(), user);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddUserToChannel(cpage, user);
            }
        }
        
        public void UpdateUserInChannel(ChannelPage cpage, User olduser, User newuser)
        {
#if LOG4NET
            Logger.Session.Debug("UpdateUserInChannel() cpage.Name: "+cpage.Name+" olduser.Nickname: "+olduser.Nickname+" newuser.Nickname: "+newuser.Nickname);
#endif
            cpage.Users.Remove(olduser.Nickname.ToLower());
            cpage.Users.Add(newuser.Nickname.ToLower(), newuser);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdateUserInChannel(cpage, olduser, newuser);
            }
        }
    
        public void UpdateTopicInChannel(ChannelPage cpage, string topic)
        {
#if LOG4NET
            Logger.Session.Debug("UpdateTopicInChannel() cpage.Name: "+cpage.Name+" topic: "+topic);
#endif
            cpage.Topic = topic;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdateTopicInChannel(cpage, topic);
            }
        }
    
        public void RemoveUserFromChannel(ChannelPage cpage, User user)
        {
#if LOG4NET
            Logger.Session.Debug("RemoveUserFromChannel() cpage.Name: "+cpage.Name+" user.Nickname: "+user.Nickname);
#endif
            cpage.Users.Remove(user.Nickname.ToLower());
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.RemoveUserFromChannel(cpage, user);
            }
        }
        
        public void SetNetworkStatus(string status)
        {
#if LOG4NET
            Logger.Session.Debug("SetNetworkStatus() status: "+status);
#endif
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.SetNetworkStatus(status);
            }
        }
        
        public void SetStatus(string status)
        {
#if LOG4NET
            Logger.Session.Debug("SetStatus() status: "+status);
#endif
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.SetStatus(status);
            }
        }
    }
}
