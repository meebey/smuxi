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
        private int              _Version = 0;
        private Hashtable        _FrontendManagers = Hashtable.Synchronized(new Hashtable());
        private ArrayList        _NetworkManagers = ArrayList.Synchronized(new ArrayList());
        private ArrayList        _Chats = ArrayList.Synchronized(new ArrayList());
        private Config           _Config;
        private UserConfig       _UserConfig;
        private bool             _OnStartupCommandsProcessed;
        
        public ArrayList NetworkManagers {
            get {
                return _NetworkManagers;
            }
        }
        
        public ArrayList Chats {
            get {
                return _Chats;
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
        
        public Session(Config config, string personname)
        {
            _Config = config;
            _UserConfig = new UserConfig(config, personname);
            
            ChatModel chat = new ChatModel("smuxi", ChatType.Network, null);
            _Chats.Add(chat);
            MessageModel fm = new MessageModel();
            fm.MessageParts.Add(
                new TextMessagePartModel(IrcTextColor.Red, null, false,
                        true, false, _("Welcome to Smuxi")));
            AddMessageToChat(chat, fm); 
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
                ChatModel smuxiChat = GetChat("smuxi", ChatType.Network, null);
                foreach (string command in (string[])_UserConfig["OnStartupCommands"]) {
                    if (command.Length == 0) {
                        continue;
                    }
                    CommandModel cd = new CommandModel(fm, smuxiChat,
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
        
        public ChatModel GetChat(string name, ChatType chatType, INetworkManager networkManager)
        {
            return GetChat(name, chatType, NetworkProtocol.None, networkManager);
        }                                     
                                         
        public ChatModel GetChat(string name, ChatType chatType,
                                 NetworkProtocol networkProtocol, INetworkManager networkManager)
        {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            
            foreach (ChatModel chat in _Chats) {
                if ((chat.Name.ToLower() == name.ToLower()) &&
                    (chat.ChatType == chatType) &&
                    /*(chat.NetworkProtocol == networkProtocol) && */
                    (chat.NetworkManager == networkManager)) {
                    return chat;
                }
            }
            
            return null;
        }
        
        public bool Command(CommandModel cd)
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
        
        public void CommandHelp(CommandModel cd)
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
                cd.FrontendManager.AddTextToCurrentChat("-!- " + line);
            }
        }
        
        public void CommandConnect(CommandModel cd)
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
                    fm.AddTextToCurrentChat("-!- " + String.Format(
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
            
            string person = (string)UserConfig["Connection/PersonModelname"];
            
            IrcNetworkManager ircm = null;
            foreach (INetworkManager nm in _NetworkManagers) {
                if (nm is IrcNetworkManager &&
                    nm.Host == server &&
                    nm.Port == port) {
                    // reuse network manager
                    if (nm.IsConnected) {
                        fm.AddTextToCurrentChat("-!- " + String.Format(
                            _("Already connected to: {0}:{1}"), server, port));
                        return;
                    }
                    ircm = (IrcNetworkManager) nm;
                    break;
                }
            }
            if (ircm == null) {
                ircm = new IrcNetworkManager(this);
                _NetworkManagers.Add(ircm);
            }
            ircm.Connect(fm, server, port, nicks, person, pass);
            
            // set this as current network manager
            fm.CurrentNetworkManager = ircm;
            fm.UpdateNetworkStatus();
        }
        
        public void CommandDisconnect(CommandModel cd)
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
                fm.AddTextToCurrentChat("-!- " + String.Format(
                                                    _("Disconnect failed, could not find server: {0}"),
                                                    server));
            } else {
                fm.CurrentNetworkManager.Disconnect(fm);
                _NetworkManagers.Remove(fm.CurrentNetworkManager);
            }
        }
        
        public void CommandReconnect(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            fm.CurrentNetworkManager.Reconnect(fm);
        }
        
        public void CommandQuit(CommandModel cd)
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
        
        public void CommandConfig(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                switch (cd.DataArray[1].ToLower()) {
                    case "load":
                        _Config.Load();
                        fm.AddTextToCurrentChat("-!- " +
                            _("Configuration reloaded"));
                        break;
                    case "save":
                        _Config.Save();
                        fm.AddTextToCurrentChat("-!- " +
                            _("Configuration saved"));
                        break;
                    default:
                        fm.AddTextToCurrentChat("-!- " + 
                            _("Invalid paramater for config, use load or save"));
                        break;
                }
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        public void CommandNetwork(CommandModel cd)
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
                        fm.AddTextToCurrentChat("-!- " + 
                            _("Invalid paramater for network, use list, switch or close"));
                        break;
                }
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        private void _CommandNetworkList(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            fm.AddTextToCurrentChat("-!- " + _("Networks") + ":");
            foreach (INetworkManager nm in _NetworkManagers) {
                fm.AddTextToCurrentChat("-!- " +
                    _("Type") + ": " + nm.NetworkProtocol.ToString().ToUpper() + " " +
                    _("Host") + ": " + nm.Host + " " + 
                    _("Port") + ": " + nm.Port);
            }
        }
        
        private void _CommandNetworkClose(CommandModel cd)
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
                fm.AddTextToCurrentChat("-!- " +
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
        
        private void _CommandNetworkSwitch(CommandModel cd)
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
                fm.AddTextToCurrentChat("-!- " +
                    String.Format(_("Network switch failed, could not find network with host: {0}"),
                                  host));
            } else if (cd.DataArray.Length >= 2) {
                // next network manager
                fm.NextNetworkManager();
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        private void _NotConnected(CommandModel cd)
        {
            cd.FrontendManager.AddTextToCurrentChat("-!- " + _("Not connected to any network"));
        }
        
        private void _NotEnoughParameters(CommandModel cd)
        {
            cd.FrontendManager.AddTextToCurrentChat("-!- " +
                String.Format(_("Not enough parameters for {0} command"), cd.Command));
        }
        
        public void UpdateNetworkStatus()
        {
            Trace.Call();
            
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdateNetworkStatus();
            }
        }
        
        public void AddChat(ChatModel chat)
        {
        	Trace.Call(chat);
        	
            _Chats.Add(chat);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddChat(chat);
                fm.SyncChat(chat);
            }
        }
        
        public void RemoveChat(ChatModel chat)
        {
        	Trace.Call(chat);
        	
            _Chats.Remove(chat);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.RemoveChat(chat);
            }
        }
        
        public void EnableChat(ChatModel chat)
        {
        	Trace.Call(chat);
        	
        	chat.IsEnabled = true;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.EnableChat(chat);
            }
        }
        
        public void DisableChat(ChatModel chat)
        {
        	Trace.Call(chat);
        	
        	chat.IsEnabled = false;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.DisableChat(chat);
            }
        }
        
        public void SyncChat(ChatModel chat)
        {
        	Trace.Call(chat);
        	
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.SyncChat(chat);
            }
        }
        
        public void AddTextToChat(ChatModel chat, string text)
        {
            AddMessageToChat(chat, new MessageModel(text));
        }
        
        public void AddMessageToChat(ChatModel chat, MessageModel fmsg)
        {
            int buffer_lines = (int)UserConfig["Interface/Notebook/EngineBufferLines"];
            if (buffer_lines > 0) {
                chat.UnsafeMessages.Add(fmsg);
                if (chat.UnsafeMessages.Count > buffer_lines) {
                    chat.UnsafeMessages.RemoveAt(0);
                }
            }
            
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddMessageToChat(chat, fmsg);
            }
        }
        
        public void AddPersonToGroupChat(GroupChatModel groupChat, PersonModel person)
        {
#if LOG4NET
            _Logger.Debug("AddPersonToGroupChat() groupChat.Name: "+groupChat.Name+" person.IdentityName: "+person.IdentityName);
#endif
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddPersonToGroupChat(groupChat, person);
            }
        }
        
        public void UpdatePersonInGroupChat(GroupChatModel groupChat, PersonModel oldperson, PersonModel newperson)
        {
#if LOG4NET
            _Logger.Debug("UpdatePersonModelInGroupChat() groupChat.Name: " + groupChat.Name + " oldperson.IdentityName: " + oldperson.IdentityName + " newperson.IdentityName: "+newperson.IdentityName);
#endif
            groupChat.UnsafePersons.Remove(oldperson.ID.ToLower());
            groupChat.UnsafePersons.Add(newperson.ID.ToLower(), newperson);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdatePersonInGroupChat(groupChat, oldperson, newperson);
            }
        }
    
        public void UpdateTopicInGroupChat(GroupChatModel groupChat, string topic)
        {
#if LOG4NET
            _Logger.Debug("UpdateTopicInGroupChat() groupChat.Name: " + groupChat.Name + " topic: " + topic);
#endif
            groupChat.Topic = topic;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdateTopicInGroupChat(groupChat, topic);
            }
        }
    
        public void RemovePersonFromGroupChat(GroupChatModel groupChat, PersonModel person)
        {
#if LOG4NET
            _Logger.Debug("RemovePersonModelFromGroupChat() groupChat.Name: " + groupChat.Name + " person.ID: "+person.ID);
#endif
            groupChat.UnsafePersons.Remove(person.ID.ToLower());
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.RemovePersonFromGroupChat(groupChat, person);
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
