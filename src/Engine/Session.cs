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
using System.Collections.Generic;
using System.Runtime.Remoting;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class Session : PermanentRemoteObject, IFrontendUI 
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string                _LibraryTextDomain = "smuxi-engine";
        private int                                   _Version = 0;
        private IDictionary<string, FrontendManager>  _FrontendManagers; 
        private IList<IProtocolManager>               _ProtocolManagers;
        private IList<ChatModel>                      _Chats;
        private SessionChatModel                      _SessionChat;
        private Config                                _Config;
        private string                                _Username;
        private ProtocolManagerFactory                _ProtocolManagerFactory;
        private UserConfig                            _UserConfig;
        private bool                                  _OnStartupCommandsProcessed;
        
        public IList<IProtocolManager> ProtocolManagers {
            get {
                return _ProtocolManagers;
            }
        }
        
        public IList<ChatModel> Chats {
            get {
                return _Chats;
            }
        }
        
        public SessionChatModel SessionChat {
            get {
                return _SessionChat;
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
        
        public bool IsLocal {
            get {
                return _Username == "local";
            }
        }
        
        public Session(Config config, ProtocolManagerFactory protocolManagerFactory,
                       string username)
        {
            Trace.Call(config, protocolManagerFactory, username);
            
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            if (protocolManagerFactory == null) {
                throw new ArgumentNullException("protocolManagerFactory");
            }
            if (username == null) {
                throw new ArgumentNullException("username");
            }
            
            _Config = config;
            _ProtocolManagerFactory = protocolManagerFactory;
            _Username = username;
            
            _FrontendManagers = new Dictionary<string, FrontendManager>();
            _ProtocolManagers = new List<IProtocolManager>();
            _UserConfig = new UserConfig(config, username);
            _Chats = new List<ChatModel>();
            
            _SessionChat = new SessionChatModel("smuxi", "smuxi");
            _Chats.Add(_SessionChat);
        }
        
        public void RegisterFrontendUI(IFrontendUI ui)
        {
            Trace.Call(ui);
            
            if (ui == null) {
                throw new ArgumentNullException("ui");
            }
            
            string uri = GetUri(ui);
#if LOG4NET
            f_Logger.Debug("Registering UI with URI: " + uri);
#endif
            // add the FrontendManager to the hashtable with an unique .NET remoting identifier
            FrontendManager fm = new FrontendManager(this, ui);
            _FrontendManagers[uri] = fm;
            
            // if this is the first frontend, we process OnStartupCommands
            if (!_OnStartupCommandsProcessed) {
                _OnStartupCommandsProcessed = true;
                
                MessageModel msg;
                msg = new MessageModel();
                msg.MessageParts.Add(
                    new TextMessagePartModel(new TextColor(0xFF0000), null, false,
                            true, false, _("Welcome to Smuxi")));
                AddMessageToChat(_SessionChat, msg);
                
                msg = new MessageModel();
                msg.MessageParts.Add(
                    new TextMessagePartModel(null, null, false,
                            true, false, _("Type /help to get a list of available commands.")));
                AddMessageToChat(_SessionChat, msg);

                msg = new MessageModel();
                msg.MessageParts.Add(
                    new TextMessagePartModel(null, null, false,
                            true, false, _("After you have made a connection the list of available commands changes, just use /help again.")));
                AddMessageToChat(_SessionChat, msg);
                
                foreach (string command in (string[])_UserConfig["OnStartupCommands"]) {
                    if (command.Length == 0) {
                        continue;
                    }
                    CommandModel cd = new CommandModel(fm, _SessionChat,
                        (string)_UserConfig["Interface/Entry/CommandCharacter"],
                        command);
                    bool handled;
                    handled = Command(cd);
                    if (!handled) {
                        if (fm.CurrentProtocolManager != null) {
                            fm.CurrentProtocolManager.Command(cd);
                        }
                    }
                }
                
                // process server specific connects/commands
                ServerListController serverCon = new ServerListController(_UserConfig);
                IList<ServerModel> servers = serverCon.GetServerList();
                foreach (ServerModel server in servers) {
                    if (!server.OnStartupConnect) {
                        continue;
                    }
                    
                    IProtocolManager protocolManager = _CreateProtocolManager(fm, server.Protocol);
                    if (protocolManager == null) {
                        continue;
                    }
                    
                    _ProtocolManagers.Add(protocolManager);
                    string password = null;
                    // only pass non-empty passwords to Connect()
                    if (!String.IsNullOrEmpty(server.Password)) {
                        password = server.Password;
                    }
                    protocolManager.Connect(fm, server.Hostname, server.Port,
                                            server.Username,
                                            password);
                    // if the connect command was correct, we should be able to get
                    // the chat model
                    if (protocolManager.Chat == null) {
                        fm.AddTextToChat(_SessionChat, String.Format(_("Automatic connect to {0} failed!"), server.Hostname + ":" + server.Port));
                        continue;
                    }
                    
                    if (server.OnConnectCommands != null && server.OnConnectCommands.Count > 0) {
                        // copy the server variable into the loop scope, else it will always be the same object in the anonymous method!
                        ServerModel ser = server;
                        protocolManager.Connected += delegate {
                            foreach (string command in ser.OnConnectCommands) {
                                if (command.Length == 0) {
                                    continue;
                                }
                                CommandModel cd = new CommandModel(fm,
                                                                   protocolManager.Chat,
                                                                   (string)_UserConfig["Interface/Entry/CommandCharacter"],
                                                                   command);
                                protocolManager.Command(cd);
                            }
                        };
                    }                                   
                }
            }
        }
        
        internal void DeregisterFrontendManager(FrontendManager fm)
        {
            Trace.Call(fm);

            if (fm == null) {
                throw new ArgumentNullException("fm");
            }
            
            string key = null;
            foreach (KeyValuePair<string, FrontendManager> kv in _FrontendManagers) {
                if (kv.Value == fm) {
                    key = kv.Key;
                    break;
                }
            }
            if (key == null) {
#if LOG4NET
                f_Logger.Debug("DeregisterFrontendManager(fm): could not find " +
                               "frontend manager (probably already cleanly " +
                               " deregistered), ignoring...");
#endif
                //throw new InvalidOperationException("Could not find key for frontend manager in _FrontendManagers.");
                return;
            }
            
            _FrontendManagers.Remove(key);
        }
        
        public void DeregisterFrontendUI(IFrontendUI ui)
        {
            Trace.Call(ui);
            
            if (ui == null) {
                throw new ArgumentNullException("ui");
            }
            
            string uri = GetUri(ui);
#if LOG4NET
            f_Logger.Debug("Deregistering UI with URI: "+uri);
#endif
            _FrontendManagers.Remove(uri);
        }
        
        public FrontendManager GetFrontendManager(IFrontendUI ui)
        {
            Trace.Call(ui);
            
            if (ui == null) {
                throw new ArgumentNullException("ui");
            }
            
            return _FrontendManagers[GetUri(ui)];
        }
        
        private string GetUri(IFrontendUI ui)
        {
            if (ui == null) {
                throw new ArgumentNullException("ui");
            }
            
            if (IsLocal) {
                return "local";
            }
            
            return RemotingServices.GetObjectUri((MarshalByRefObject)ui);
        }
        
        public static bool IsLocalFrontend(IFrontendUI ui)
        {
            if (ui == null) {
                throw new ArgumentNullException("ui");
            }
            
            return RemotingServices.GetObjectUri((MarshalByRefObject)ui) == null;
        }
        
        public ChatModel GetChat(string id, ChatType chatType, IProtocolManager networkManager)
        {
            if (id == null) {
                throw new ArgumentNullException("id");
            }
            
            foreach (ChatModel chat in _Chats) {
                if ((chat.ID.ToLower() == id.ToLower()) &&
                    (chat.ChatType == chatType) &&
                    (chat.ProtocolManager == networkManager)) {
                    return chat;
                }
            }
            
            return null;
        }
        
        public bool Command(CommandModel cd)
        {
            Trace.Call(cd);
            
            if (cd == null) {
                throw new ArgumentNullException("cd");
            }
            
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
                }
            } else {
                // normal text
                if (cd.FrontendManager.CurrentProtocolManager == null) {
                    _NotConnected(cd);
                    handled = true;
                }
            }
            
            return handled;
        }
        
        public void CommandHelp(CommandModel cd)
        {
            Trace.Call(cd);
            
            if (cd == null) {
                throw new ArgumentNullException("cd");
            }
            
            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = _("[Engine Commands]");
            msgPart.Bold = true;
            msg.MessageParts.Add(msgPart);
            
            cd.FrontendManager.AddMessageToChat(cd.Chat, msg);
            
            string[] help = {
                "help",
                "connect/server protocol [protocol-parameters]",
                "disconnect",
                "network list",
                "network close [server]",
                "network switch [server]",
                "config (save|load)",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToCurrentChat("-!- " + line);
            }
        }
        
        public void CommandConnect(CommandModel cd)
        {
            Trace.Call(cd);
            
            if (cd == null) {
                throw new ArgumentNullException("cd");
            }
            
            FrontendManager fm = cd.FrontendManager;
            
            string protocol;
            if (cd.DataArray.Length >= 2) {
                protocol = cd.DataArray[1];
            } else {
                _NotEnoughParameters(cd);
                return;
            }
            
            IProtocolManager protocolManager = null;
            // TODO: detect matching protocol managers, how to parse host and port
            // though in a protocol neutral ?
            /*
            foreach (IProtocolManager nm in _ProtocolManagers) {
                if (nm.Host == server &&
                    nm.Port == port) {
                    // reuse network manager
                    if (nm.IsConnected) {
                        fm.AddTextToCurrentChat("-!- " + String.Format(
                            _("Already connected to: {0}:{1}"), server, port));
                        return;
                    }
                    networkManager = nm;
                    break;
                }
            }
            */

            if (protocolManager == null) {
                protocolManager = _CreateProtocolManager(fm, protocol);
                if (protocolManager == null) {
                    return;
                }
                _ProtocolManagers.Add(protocolManager);
            }
            // HACK: this is hacky as the Command parser of the protocol manager
            // will pass this command to it's connect method only if cd was
            // constructed correctly beginning with /connect
            // So make sure it's like it needs to be!
            if (cd.Command != "connect") {
                throw new ArgumentException("cd.Command must be 'connect' but was: '" + cd.Command + "'.", "cd");
            }
            protocolManager.Command(cd);
            
            // set this as current protocol manager
            // but only if there was none set (we might be on a chat for example)
            // or if this is the neutral "smuxi" tab
            if (fm.CurrentProtocolManager == null ||
                fm.CurrentChat != null && fm.CurrentChat.ChatType == ChatType.Session) {
                fm.CurrentProtocolManager = protocolManager;
                fm.UpdateNetworkStatus();
            }
        }
        
        public void CommandDisconnect(CommandModel cd)
        {
            Trace.Call(cd);
            
            if (cd == null) {
                throw new ArgumentNullException("cd");
            }
            
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 2) {
                string server = cd.DataArray[1];
                foreach (IProtocolManager nm in _ProtocolManagers) {
                    if (nm.Host.ToLower() == server.ToLower()) {
                        nm.Disconnect(fm);
                        _ProtocolManagers.Remove(nm);
                        return;
                    }
                }
                fm.AddTextToCurrentChat(
                    "-!- " +
                    String.Format(
                        _("Disconnect failed, could not find server: {0}"),
                        server
                    )
                );
            } else {
                fm.CurrentProtocolManager.Disconnect(fm);
                _ProtocolManagers.Remove(fm.CurrentProtocolManager);
            }
        }
        
        public void CommandReconnect(CommandModel cd)
        {
            Trace.Call(cd);
            
            if (cd == null) {
                throw new ArgumentNullException("cd");
            }
            
            FrontendManager fm = cd.FrontendManager;
            fm.CurrentProtocolManager.Reconnect(fm);
        }
        
        public void CommandConfig(CommandModel cd)
        {
            Trace.Call(cd);
            
            if (cd == null) {
                throw new ArgumentNullException("cd");
            }
            
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
            Trace.Call(cd);
            
            if (cd == null) {
                throw new ArgumentNullException("cd");
            }
            
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
            foreach (IProtocolManager nm in _ProtocolManagers) {
                fm.AddTextToCurrentChat("-!- " +
                    _("Type") + ": " + nm.Protocol + " " +
                    _("Host") + ": " + nm.Host + " " + 
                    _("Port") + ": " + nm.Port);
            }
        }
        
        private void _CommandNetworkClose(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            IProtocolManager pm = null;
            if (cd.DataArray.Length >= 3) {
                // named protocol manager
                string host = cd.DataArray[2].ToLower();
                foreach (IProtocolManager protocolManager in _ProtocolManagers) {
                    if (protocolManager.Host.ToLower() == host) {
                        pm = protocolManager;
                        break;
                    }
                }
                if (pm == null) {
                    fm.AddTextToCurrentChat("-!- " +
                        String.Format(_("Network close failed, could not find network with host: {0}"),
                                      host));
                    return;
                }
            } else if (cd.DataArray.Length >= 2) {
                // current network manager
                pm = fm.CurrentProtocolManager;
            }
            
            if (pm != null) {
                pm.Disconnect(fm);
                pm.Dispose();
                // Dispose() takes care of removing the chat from session (frontends)
                _ProtocolManagers.Remove(pm);
                fm.NextProtocolManager();
            }
        }
        
        private void _CommandNetworkSwitch(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 3) {
                // named network manager
                string host = cd.DataArray[2].ToLower();
                foreach (IProtocolManager nm in _ProtocolManagers) {
                    if (nm.Host.ToLower() == host) {
                        fm.CurrentProtocolManager = nm;
                        fm.UpdateNetworkStatus();
                        return;
                    }
                }
                fm.AddTextToCurrentChat("-!- " +
                    String.Format(_("Network switch failed, could not find network with host: {0}"),
                                  host));
            } else if (cd.DataArray.Length >= 2) {
                // next network manager
                fm.NextProtocolManager();
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
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            _Chats.Add(chat);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddChat(chat);
            }
        }
        
        public void RemoveChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            _Chats.Remove(chat);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.RemoveChat(chat);
            }
        }
        
        public void EnableChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            chat.IsEnabled = true;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.EnableChat(chat);
            }
        }
        
        public void DisableChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            chat.IsEnabled = false;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.DisableChat(chat);
            }
        }
        
        public void SyncChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.SyncChat(chat);
            }
        }
        
        public void AddTextToChat(ChatModel chat, string text)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (text == null) {
                throw new ArgumentNullException("text");
            }
            
            AddMessageToChat(chat, new MessageModel(text));
        }
        
        public void AddMessageToChat(ChatModel chat, MessageModel msg)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }
            
            int buffer_lines = (int) UserConfig["Interface/Notebook/EngineBufferLines"];
            if (buffer_lines > 0) {
                chat.UnsafeMessages.Add(msg);
                if (chat.UnsafeMessages.Count > buffer_lines) {
                    chat.UnsafeMessages.RemoveAt(0);
                }
            }
            
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddMessageToChat(chat, msg);
            }
        }
        
        public void AddPersonToGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            if (groupChat == null) {
                throw new ArgumentNullException("groupChat");
            }
            if (person == null) {
                throw new ArgumentNullException("person");
            }
            
#if LOG4NET
            f_Logger.Debug("AddPersonToGroupChat() groupChat.Name: "+groupChat.Name+" person.IdentityName: "+person.IdentityName);
#endif
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.AddPersonToGroupChat(groupChat, person);
            }
        }
        
        public void UpdatePersonInGroupChat(GroupChatModel groupChat, PersonModel oldPerson, PersonModel newPerson)
        {
            if (groupChat == null) {
                throw new ArgumentNullException("groupChat");
            }
            if (oldPerson == null) {
                throw new ArgumentNullException("oldPerson");
            }
            if (newPerson == null) {
                throw new ArgumentNullException("newPerson");
            }
            
#if LOG4NET
            f_Logger.Debug("UpdatePersonInGroupChat()" +
                          " groupChat.Name: " + groupChat.Name +
                          " oldPerson.IdentityName: " + oldPerson.IdentityName +
                          " newPerson.IdentityName: " + newPerson.IdentityName);
#endif
            groupChat.UnsafePersons.Remove(oldPerson.ID.ToLower());
            groupChat.UnsafePersons.Add(newPerson.ID.ToLower(), newPerson);
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdatePersonInGroupChat(groupChat, oldPerson, newPerson);
            }
        }
    
        public void UpdateTopicInGroupChat(GroupChatModel groupChat, string topic)
        {
            if (groupChat == null) {
                throw new ArgumentNullException("groupChat");
            }
            if (topic == null) {
                throw new ArgumentNullException("topic");
            }

#if LOG4NET
            f_Logger.Debug("UpdateTopicInGroupChat() groupChat.Name: " + groupChat.Name + " topic: " + topic);
#endif
            groupChat.Topic = topic;
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.UpdateTopicInGroupChat(groupChat, topic);
            }
        }
    
        public void RemovePersonFromGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            if (groupChat == null) {
                throw new ArgumentNullException("groupChat");
            }
            if (person == null) {
                throw new ArgumentNullException("person");
            }
            
#if LOG4NET
            f_Logger.Debug("RemovePersonFromGroupChat() groupChat.Name: " + groupChat.Name + " person.ID: "+person.ID);
#endif
            groupChat.UnsafePersons.Remove(person.ID.ToLower());
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.RemovePersonFromGroupChat(groupChat, person);
            }
        }
        
        public void SetNetworkStatus(string status)
        {
            if (status == null) {
                throw new ArgumentNullException("status");
            }
            
#if LOG4NET
            f_Logger.Debug("SetNetworkStatus() status: "+status);
#endif
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.SetNetworkStatus(status);
            }
        }
        
        public void SetStatus(string status)
        {
            if (status == null) {
                throw new ArgumentNullException("status");
            }
            
#if LOG4NET
            f_Logger.Debug("SetStatus() status: "+status);
#endif
            foreach (FrontendManager fm in _FrontendManagers.Values) {
                fm.SetStatus(status);
            }
        }
        
        public IList<string> GetSupportedProtocols()
        {
            return _ProtocolManagerFactory.GetProtocols();
        }
        
        public void Connect(FrontendManager frontendManager, string protocol,
                            string hostname, int port,
                            string username, string password)
        {
            Trace.Call(frontendManager, protocol, hostname, port, username, "XXX");
            
            IProtocolManager protocolManager = _CreateProtocolManager(
                frontendManager,
                protocol
            );
            if (protocolManager == null) {
                throw new ApplicationException(_("No protocol manager found for that protocol: " + protocol));
            }
            
            _ProtocolManagers.Add(protocolManager);
            // only pass non-empty passwords to Connect()
            if (String.IsNullOrEmpty(password)) {
                password = null;
            }
            protocolManager.Connect(frontendManager, hostname, port,
                                    username, password);
        }
        
        private IProtocolManager _CreateProtocolManager(FrontendManager fm, string protocol)
        {
            ProtocolManagerInfoModel info = _ProtocolManagerFactory.GetProtocolManagerInfoByAlias(protocol); 
            if (info == null) {
                fm.AddTextToCurrentChat("-!- " + String.Format(
                        _("Unknown protocol: {0}"), protocol));
                return null;
            }
            return _ProtocolManagerFactory.CreateProtocolManager(info, this);
        }
        
        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
