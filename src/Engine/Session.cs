/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2010 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Threading;
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
        private FilterListController                  _FilterListController;
        private ICollection<FilterModel>              _Filters;
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
            _UserConfig.Changed += OnUserConfigChanged;
            _FilterListController = new FilterListController(_UserConfig);
            _Filters = _FilterListController.GetFilterList().Values;
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
            lock (_FrontendManagers) {
                _FrontendManagers[uri] = fm;
            }
            
            // if this is the first frontend, we process OnStartupCommands
            if (!_OnStartupCommandsProcessed) {
                _OnStartupCommandsProcessed = true;

                string str;
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

                str = _("After you have made a connection the list of " +
                        "available commands changes. Use the /help command " +
                        "again to see the extended command list.");
                msg = new MessageModel();
                msg.MessageParts.Add(
                    new TextMessagePartModel(null, null, false,
                            true, false, str));
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

                    bool isError = false;
                    try {
                        IProtocolManager protocolManager = Connect(server, fm);

                        // if the connect command was correct, we should be
                        // able to get the chat model
                        if (protocolManager.Chat == null) {
                            isError = true;
                        }
                    } catch (Exception ex) {
#if LOG4NET
                        f_Logger.Error("RegisterFrontendUI(): Exception during "+
                                       "automatic connect: ", ex);
#endif
                        isError = true;
                    }
                    if (isError) {
                        fm.AddTextToChat(
                            _SessionChat,
                            String.Format(
                                _("Automatic connect to {0} failed!"),
                                server.Hostname + ":" + server.Port
                            )
                        );
                        continue;
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
            lock (_FrontendManagers) {
                foreach (KeyValuePair<string, FrontendManager> kv in _FrontendManagers) {
                    if (kv.Value == fm) {
                        key = kv.Key;
                        break;
                    }
                }
            }
            if (key == null) {
#if LOG4NET
                f_Logger.Debug("DeregisterFrontendManager(fm): could not find " +
                               "frontend manager (probably already cleanly " +
                               " deregistered), ignoring...");
#endif
                return;
            }
            
            lock (_FrontendManagers) {
                _FrontendManagers.Remove(key);
            }

#if LOG4NET
            f_Logger.Debug("DeregisterFrontendUI(fm): disposing FrontendManager");
#endif
            fm.Dispose();
        }
        
        public void DeregisterFrontendUI(IFrontendUI ui)
        {
            Trace.Call(ui);
            
            if (ui == null) {
                throw new ArgumentNullException("ui");
            }
            
            string uri = GetUri(ui);
#if LOG4NET
            f_Logger.Debug("DeregisterFrontendUI(ui): deregistering UI with URI: "+uri);
#endif
            FrontendManager manager;
            lock (_FrontendManagers) {
                _FrontendManagers.TryGetValue(uri, out manager);
                _FrontendManagers.Remove(uri);
            }
            if (manager == null) {
#if LOG4NET
                f_Logger.Error("DeregisterFrontendUI(ui): can't dispose as FrontendManager not found with URI: " + uri);
#endif
            } else {
#if LOG4NET
                f_Logger.Debug("DeregisterFrontendUI(ui): disposing FrontendManager with URI: " + uri);
#endif
                manager.Dispose();
            }
        }
        
        public FrontendManager GetFrontendManager(IFrontendUI ui)
        {
            Trace.Call(ui);
            
            if (ui == null) {
                throw new ArgumentNullException("ui");
            }
            
            lock (_FrontendManagers) {
                return _FrontendManagers[GetUri(ui)];
            }
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
            
            lock (_Chats) {
                foreach (ChatModel chat in _Chats) {
                    if ((chat.ID.ToLower() == id.ToLower()) &&
                        (chat.ChatType == chatType) &&
                        (chat.ProtocolManager == networkManager)) {
                        return chat;
                    }
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
                if (cd.Chat.ChatType == ChatType.Session &&
                    cd.FrontendManager.CurrentProtocolManager == null) {
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
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            msgPart.Text = "[" + _("Engine Commands") + "]";
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
                cd.FrontendManager.AddTextToChat(cd.Chat, "-!- " + line);
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
                try {
                    protocolManager = CreateProtocolManager(protocol);
                    _ProtocolManagers.Add(protocolManager);
                } catch (ArgumentException ex) {
                    if (ex.ParamName != "protocol") {
                        throw;
                    }
                    // this is an unknown protocol error
                    fm.AddTextToChat(
                        fm.CurrentChat,
                        String.Format("-!- {0}", ex.Message)
                    );
                    return;
                }
            }
            // HACK: this is hacky as the Command parser of the protocol manager
            // will pass this command to it's connect method only if cd was
            // constructed correctly beginning with /connect
            // So make sure it's like it needs to be!
            if (cd.Command != "connect") {
                string cmd = String.Format("{0}connect {1}",
                                cd.CommandCharacter,
                                String.Join(" ", cd.DataArray, 1,
                                            cd.DataArray.Length - 1));
                cd = new CommandModel(fm, cd.Chat, cd.CommandCharacter, cmd);
            }

            // run in background so it can't block the command queue
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    protocolManager.Command(cd);

                    // set this as current protocol manager
                    // but only if there was none set (we might be on a chat for example)
                    // or if this is the neutral "smuxi" tab
                    if (fm.CurrentProtocolManager == null ||
                        (fm.CurrentChat != null && fm.CurrentChat.ChatType == ChatType.Session)) {
                        fm.CurrentProtocolManager = protocolManager;
                        fm.UpdateNetworkStatus();
                    }
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error("CommandConnect(): ", ex);
#endif
                    fm.AddTextToChat(cd.Chat, "-!- " + _("Connect failed!"));
                }
            });
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
                lock (_ProtocolManagers) {
                    foreach (IProtocolManager nm in _ProtocolManagers) {
                        if (nm.Host.ToLower() == server.ToLower()) {
                            nm.Disconnect(fm);
                            _ProtocolManagers.Remove(nm);
                            return;
                        }
                    }
                }
                fm.AddTextToChat(
                    cd.Chat,
                    "-!- " +
                    String.Format(
                        _("Disconnect failed - could not find server: {0}"),
                        server
                    )
                );
            } else {
                var pm = cd.Chat.ProtocolManager;
                if (pm == null) {
                    return;
                }
                pm.Disconnect(fm);
                _ProtocolManagers.Remove(pm);
            }
        }
        
        public void CommandReconnect(CommandModel cd)
        {
            Trace.Call(cd);
            
            if (cd == null) {
                throw new ArgumentNullException("cd");
            }
            
            var pm = cd.Chat.ProtocolManager;
            if (pm == null) {
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    pm.Reconnect(cd.FrontendManager);
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error("CommandReconnect(): ", ex);
#endif
                    cd.FrontendManager.AddTextToChat(cd.Chat, "-!- " +
                        _("Reconnect failed!"));
                }
            });
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
                        fm.AddTextToChat(cd.Chat, "-!- " +
                            _("Configuration reloaded"));
                        break;
                    case "save":
                        _Config.Save();
                        fm.AddTextToChat(cd.Chat, "-!- " +
                            _("Configuration saved"));
                        break;
                    default:
                        fm.AddTextToChat(cd.Chat, "-!- " + 
                            _("Invalid parameter for config; use load or save"));
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
                        fm.AddTextToChat(cd.Chat, "-!- " + 
                            _("Invalid parameter for network; use list, switch, or close"));
                        break;
                }
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        private void _CommandNetworkList(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            fm.AddTextToChat(cd.Chat, "-!- " + _("Networks") + ":");
            lock (_ProtocolManagers) {
                foreach (IProtocolManager nm in _ProtocolManagers) {
                    fm.AddTextToChat(cd.Chat, "-!- " +
                        _("Type") + ": " + nm.Protocol + " " +
                        _("Host") + ": " + nm.Host + " " + 
                        _("Port") + ": " + nm.Port);
                }
            }
        }
        
        private void _CommandNetworkClose(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            IProtocolManager pm = null;
            if (cd.DataArray.Length >= 3) {
                // named protocol manager
                string host = cd.DataArray[2].ToLower();
                lock (_ProtocolManagers) {
                    foreach (IProtocolManager protocolManager in _ProtocolManagers) {
                        if (protocolManager.Host.ToLower() == host) {
                            pm = protocolManager;
                            break;
                        }
                    }
                }
                if (pm == null) {
                    fm.AddTextToChat(cd.Chat, "-!- " +
                        String.Format(_("Network close failed - could not find network with host: {0}"),
                                      host));
                    return;
                }
            } else if (cd.DataArray.Length >= 2) {
                // network manager of chat
                pm = cd.Chat.ProtocolManager;
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
                lock (_ProtocolManagers) {
                    foreach (IProtocolManager nm in _ProtocolManagers) {
                        if (nm.Host.ToLower() == host) {
                            fm.CurrentProtocolManager = nm;
                            fm.UpdateNetworkStatus();
                            return;
                        }
                    }
                }
                fm.AddTextToChat(cd.Chat, "-!- " +
                    String.Format(_("Network switch failed - could not find network with host: {0}"),
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
            cd.FrontendManager.AddTextToChat(
                cd.Chat,
                String.Format("-!- {0}",
                    _("Not connected to any network")
                )
            );
        }
        
        private void _NotEnoughParameters(CommandModel cd)
        {
            cd.FrontendManager.AddTextToChat(
                cd.Chat,
                String.Format("-!- {0}",
                    String.Format(
                        _("Not enough parameters for {0} command"),
                        cd.Command
                    )
                )
            );
        }
        
        public void UpdateNetworkStatus()
        {
            Trace.Call();
            
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.UpdateNetworkStatus();
                }
            }
        }
        
        public void AddChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            lock (_Chats) {
                _Chats.Add(chat);
            }
            
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.AddChat(chat);
                }
            }
        }
        
        public void RemoveChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            lock (_Chats) {
                if (!_Chats.Remove(chat)) {
#if LOG4NET
                    f_Logger.Warn("RemoveChat(): _Chats.Remove(" + chat + ") failed, ignoring...");
#endif
                    return;
                }
            }
            
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.RemoveChat(chat);
                }
            }
        }
        
        public void EnableChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            chat.IsEnabled = true;
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.EnableChat(chat);
                }
            }
        }
        
        public void DisableChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            chat.IsEnabled = false;
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.DisableChat(chat);
                }
            }
        }
        
        public void SyncChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.SyncChat(chat);
                }
            }
        }
        
        public void AddTextToChat(ChatModel chat, string text)
        {
            AddTextToChat(chat, text, false);
        }

        public void AddTextToChat(ChatModel chat, string text, bool ignoreFilters)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (text == null) {
                throw new ArgumentNullException("text");
            }
            
            AddMessageToChat(chat, new MessageModel(text), ignoreFilters);
        }
        
        public void AddMessageToChat(ChatModel chat, MessageModel msg)
        {
            AddMessageToChat(chat, msg, false);
        }

        public void AddMessageToChat(ChatModel chat, MessageModel msg,
                                     bool ignoreFilters)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }
            
            bool isFiltered = !ignoreFilters && IsFilteredMessage(chat, msg);
            LogMessage(chat, msg, isFiltered);
            if (isFiltered) {
                return;
            }

            int buffer_lines = (int) UserConfig["Interface/Notebook/EngineBufferLines"];
            if (buffer_lines > 0) {
                chat.UnsafeMessages.Add(msg);
                if (chat.UnsafeMessages.Count > buffer_lines) {
                    chat.UnsafeMessages.RemoveAt(0);
                }
            }
            
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.AddMessageToChat(chat, msg);
                }
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
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.AddPersonToGroupChat(groupChat, person);
                }
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
            // FIXME: do we have to lock groupChat.UnsafePersons here?
            // probably not, as long as the ProtocolManager who owns this chat
            // is only running one thread
            groupChat.UnsafePersons.Remove(oldPerson.ID.ToLower());
            groupChat.UnsafePersons.Add(newPerson.ID.ToLower(), newPerson);
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.UpdatePersonInGroupChat(groupChat, oldPerson, newPerson);
                }
            }
        }
    
        public void UpdateTopicInGroupChat(GroupChatModel groupChat, MessageModel topic)
        {
            if (groupChat == null) {
                throw new ArgumentNullException("groupChat");
            }
            if (topic == null) {
                throw new ArgumentNullException("topic");
            }

#if LOG4NET
            f_Logger.Debug("UpdateTopicInGroupChat() groupChat.Name: " + groupChat.Name + " topic: " + topic.ToString());
#endif
            groupChat.Topic = topic;
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.UpdateTopicInGroupChat(groupChat, topic);
                }
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
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.RemovePersonFromGroupChat(groupChat, person);
                }
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
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.SetNetworkStatus(status);
                }
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
            lock (_FrontendManagers) {
                foreach (FrontendManager fm in _FrontendManagers.Values) {
                    fm.SetStatus(status);
                }
            }
        }
        
        public IList<string> GetSupportedProtocols()
        {
            return _ProtocolManagerFactory.GetProtocols();
        }
        
        public IProtocolManager Connect(ServerModel server, FrontendManager frontendManager)
        {
            Trace.Call(server, frontendManager);
            
            if (server == null) {
                throw new ArgumentNullException("server");
            }
            if (String.IsNullOrEmpty(server.Protocol)) {
                throw new ArgumentNullException("server.Protocol");
            }
            if (frontendManager == null) {
                throw new ArgumentNullException("frontendManager");
            }

            IProtocolManager protocolManager = CreateProtocolManager(
                server.Protocol
            );
            _ProtocolManagers.Add(protocolManager);

            string password = null;
            // only pass non-empty passwords to Connect()
            if (!String.IsNullOrEmpty(server.Password)) {
                password = server.Password;
            }
            protocolManager.Connect(frontendManager, server.Hostname,
                                    server.Port, server.Username, password);
            if (protocolManager.Chat == null) {
                // just in case the ProtocolManager is not setting the
                // protocol chat
                throw new ApplicationException(_("Connect failed."));
            }

            if (server.OnConnectCommands != null && server.OnConnectCommands.Count > 0) {
                protocolManager.Connected += delegate {
                    foreach (string command in server.OnConnectCommands) {
                        if (command.Length == 0) {
                            continue;
                        }
                        CommandModel cd = new CommandModel(
                            frontendManager,
                            protocolManager.Chat,
                            (string) _UserConfig["Interface/Entry/CommandCharacter"],
                            command
                        );
                        protocolManager.Command(cd);
                    }
                };
            }

            return protocolManager;
        }
        
        private IProtocolManager CreateProtocolManager(string protocol)
        {
            ProtocolManagerInfoModel info =
                _ProtocolManagerFactory.GetProtocolManagerInfoByAlias(protocol);
            if (info == null) {
                if (_ProtocolManagerFactory.ProtocolManagerInfos.Count != 1) {
                    throw new ArgumentException(
                            String.Format(
                                _("No protocol manager found for the protocol: {0}"),
                                protocol
                            ),
                            "protocol"
                    );
                }

                // ok, we forgive the user not passing a valid protocol by
                // falling back to the only available protocol
                info = _ProtocolManagerFactory.ProtocolManagerInfos[0];
            }

            return _ProtocolManagerFactory.CreateProtocolManager(info, this);
        }
        
        public void LogMessage(ChatModel chat, MessageModel msg, bool isFiltered)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            if (!(bool) UserConfig["Logging/Enabled"]) {
                return;
            }
            if (isFiltered && !(bool) UserConfig["Logging/LogFilteredMessages"]) {
                return;
            }

            if (chat.ChatType == ChatType.Session ||
                chat.ChatType == ChatType.Protocol) {
                return;
            }

            try {
                var logPath = Platform.LogPath;
                var protocol = chat.ProtocolManager.Protocol.ToLower();
                // HACK: twitter retrieves older messages and we don't want to
                // re-log those when the twitter connection is re-opened
                if (protocol == "twitter") {
                    return;
                }
                var network = chat.ProtocolManager.NetworkID.ToLower();
                logPath = Path.Combine(logPath, protocol);
                if (network != protocol) {
                    logPath = Path.Combine(logPath, network);
                }
                if (!Directory.Exists(logPath)) {
                    Directory.CreateDirectory(logPath);
                }
                var chatId = chat.ID.Replace(" ", "_").ToLower();
                logPath = Path.Combine(logPath, String.Format("{0}.log", chatId));
                using (var stream = File.AppendText(logPath)) {
                    stream.WriteLine(
                        String.Format(
                            "[{0:yyyy-MM-dd HH:mm:ss}] {1}",
                            msg.TimeStamp.ToLocalTime(),
                            msg.ToString()
                        )
                    );
                }
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("LogMessage(): logging error", ex);
#endif
            }
        }

        public bool IsFilteredMessage(ChatModel chat, MessageModel msg)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            return IsFilteredMessage(chat, msg.ToString(), msg.MessageType);
        }

        public bool IsFilteredMessage(ChatModel chat, string msg,
                                      MessageType msgType)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            lock (_Filters) {
                foreach (var filter in _Filters) {
                    if (!String.IsNullOrEmpty(filter.Protocol) &&
                        chat.ProtocolManager != null &&
                        filter.Protocol != chat.ProtocolManager.Protocol) {
                        continue;
                    }
                    if (filter.ChatType.HasValue &&
                        filter.ChatType != chat.ChatType) {
                        continue;
                    }
                    if (!String.IsNullOrEmpty(filter.ChatID) &&
                        !Pattern.IsMatch(chat.ID, filter.ChatID)) {
                        continue;
                    }
                    if (filter.MessageType.HasValue &&
                        filter.MessageType != msgType) {
                        continue;
                    }
                    if (!String.IsNullOrEmpty(filter.MessagePattern)) {
                        var pattern = filter.MessagePattern;
                        if (!Pattern.ContainsPatternCharacters(pattern)) {
                            // use globbing by default
                            pattern = String.Format("*{0}*", pattern);
                        }
                        if (!Pattern.IsMatch(msg, pattern)) {
                            continue;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

       void OnUserConfigChanged(object sender, ConfigChangedEventArgs e)
       {
            if (e.Key.StartsWith("Filters/")) {
#if LOG4NET
                f_Logger.Debug("OnUserConfigChanged(): refreshing filters");
#endif
                // referesh filters
                // TODO: use a timeout here to only refresh once in 1 second
                _Filters = _FilterListController.GetFilterList().Values;
            }
       }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
