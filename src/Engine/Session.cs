/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2012 Mirco Bauer <meebey@meebey.net>
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
using System.Net;
using System.Linq;
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
        private List<ChatModel>                       _Chats;
        private SessionChatModel                      _SessionChat;
        private Config                                _Config;
        private string                                _Username;
        private ProtocolManagerFactory                _ProtocolManagerFactory;
        private UserConfig                            _UserConfig;
        private FilterListController                  _FilterListController;
        private ICollection<FilterModel>              _Filters;
        private bool                                  _OnStartupCommandsProcessed;
        Timer NewsFeedTimer { get; set; }
        List<string> SeenNewsFeedIds { get; set; }
        DateTime NewsFeedLastModified { get; set; }
        TimeSpan NewsFeedUpdateInterval { get; set; }
        TimeSpan NewsFeedRetryInterval { get; set; }

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
        
        public string Username {
            get {
                return _Username;
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

            InitSessionChat();

            SeenNewsFeedIds = new List<string>();
            NewsFeedUpdateInterval = TimeSpan.FromHours(12);
            NewsFeedRetryInterval = TimeSpan.FromMinutes(5);
            NewsFeedTimer = new Timer(delegate { UpdateNewsFeed(); }, null,
                                      TimeSpan.Zero, NewsFeedUpdateInterval);
        }

        protected MessageBuilder CreateMessageBuilder()
        {
            var builder = new MessageBuilder();
            builder.ApplyConfig(UserConfig);
            return builder;
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

            CheckPresenceStatus();
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

            CheckPresenceStatus();
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
                        if (cd.Chat.ChatType == ChatType.Session) {
                            handled = true;
                        }
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
                    case "shutdown":
                        CommandShutdown(cd);
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
            var builder = CreateMessageBuilder();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            builder.AppendHeader(_("Engine Commands"));
            cd.FrontendManager.AddMessageToChat(cd.Chat, builder.ToMessage());

            string[] help = {
                "help",
                "connect/server protocol [protocol-parameters]",
                "connect/server network",
                "disconnect [server]",
                "network list",
                "network close [network]",
                "network switch [network]",
                "config (save|load)",
                "shutdown"
            };

            foreach (string line in help) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                cd.FrontendManager.AddMessageToChat(cd.Chat, builder.ToMessage());
            }
        }
        
        public void CommandConnect(CommandModel cd)
        {
            Trace.Call(cd);
            
            if (cd == null) {
                throw new ArgumentNullException("cd");
            }
            
            FrontendManager fm = cd.FrontendManager;

            string protocol = null;
            ServerModel server = null;
            // first lookup by network name
            if (cd.DataArray.Length == 2) {
                var network = cd.Parameter;
                var serverSettings = new ServerListController(UserConfig);
                server = serverSettings.GetServerByNetwork(network);
            } else if (cd.DataArray.Length >= 3) {
                protocol = cd.DataArray[1];
            } else if (cd.DataArray.Length >= 2) {
                // HACK: simply assume the user meant irc if not specified as
                // Smuxi is still primarly an IRC client
                protocol = "irc";
                string cmd = String.Format("{0}connect irc {1}",
                                           cd.CommandCharacter, cd.Parameter);
                cd = new CommandModel(fm, cd.Chat, cd.CommandCharacter, cmd);
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

            if (protocolManager == null && server == null) {
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
                    if (protocolManager == null && server != null) {
                        protocolManager = Connect(server, fm);
                    } else {
                        protocolManager.Command(cd);
                    }

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
            IProtocolManager victim = null;
            if (cd.DataArray.Length >= 2) {
                string server = cd.DataArray[1];
                victim = GetProtocolManagerByHost(server);
                if (victim == null) {
                    fm.AddTextToChat(
                        cd.Chat,
                        "-!- " +
                        String.Format(
                            _("Disconnect failed - could not find server: {0}"),
                            server
                        )
                    );
                    return;
                }
            } else {
                victim = cd.Chat.ProtocolManager;
            }

            if (victim == null) {
                return;
            }
            victim.Disconnect(fm);
            victim.Dispose();
            _ProtocolManagers.Remove(victim);
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
        
        public void CommandShutdown(CommandModel cmd)
        {
            Trace.Call(cmd);

            if (cmd == null) {
                throw new ArgumentNullException("cmd");
            }

#if LOG4NET
            f_Logger.Info("Shutting down...");
#endif
            lock (_ProtocolManagers) {
                foreach (var protocolManager in _ProtocolManagers) {
                    protocolManager.Disconnect(cmd.FrontendManager);
                    protocolManager.Dispose();
                }
            }

            if (IsLocal) {
                // allow the frontend to cleanly terminate
                return;
            }

#if LOG4NET
            f_Logger.Debug("Terminating process...");
#endif
            Environment.Exit(0);
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
                        _("Protocol") + ": " + nm.Protocol + " " +
                        _("Network") + ": " + nm.NetworkID + " " +
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
                string network = cd.DataArray[2];
                pm = GetProtocolManagerByNetwork(network);
                if (pm == null) {
                    fm.AddTextToChat(cd.Chat, "-!- " +
                        String.Format(_("Network close failed - could not find network: {0}"),
                                      network));
                    return;
                }
            } else if (cd.DataArray.Length >= 2) {
                // network manager of chat
                pm = cd.Chat.ProtocolManager;
            }

            if (pm == null) {
                return;
            }

            // disconnect in background as could be blocking
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    pm.Disconnect(fm);
                    pm.Dispose();
                    // Dispose() takes care of removing the chat from session (frontends)
                    _ProtocolManagers.Remove(pm);
                    fm.NextProtocolManager();
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error("_CommandNetworkClose(): Exception", ex);
#endif
                }
            });
        }
        
        private void _CommandNetworkSwitch(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.DataArray.Length >= 3) {
                // named network manager
                string network = cd.DataArray[2];
                var pm = GetProtocolManagerByNetwork(network);
                if (pm == null) {
                    fm.AddTextToChat(cd.Chat, "-!- " +
                        String.Format(
                            _("Network switch failed - could not find network: {0}"),
                                  network));
                    return;
                }
                fm.CurrentProtocolManager = pm;
                fm.UpdateNetworkStatus();
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

        public T CreateChat<T>(string id,
                               string name,
                               IProtocolManager protocolManager)
                              where T : ChatModel
        {
            Trace.Call(id, name, protocolManager);

            T chat;
            Type chatType = typeof(T);
            if (chatType == typeof(SessionChatModel)) {
                chat = (T) Activator.CreateInstance(chatType, id, name);
            } else if (chatType == typeof(PersonChatModel)) {
                throw new NotSupportedException(
                    "PersonModel is not supported, use " +
                    "Session.CreatePersionChat() instead"
                );
            } else {
                chat = (T) Activator.CreateInstance(chatType,
                                                    id, name, protocolManager);
            }
            chat.ApplyConfig(UserConfig);
            return chat;
        }

        public PersonChatModel CreatePersonChat(PersonModel person,
                                                string id, string name,
                                                IProtocolManager protocolManager)
        {
            Trace.Call(person, id, name, protocolManager);

            var chat = new PersonChatModel(person, id, name, protocolManager);
            chat.ApplyConfig(UserConfig);
            return chat;
        }

        public void AddChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }

            chat.Position = GetSortedChatPosition(chat);
            lock (_Chats) {
                _Chats.Add(chat);
                if (chat.Position == -1) {
                    chat.Position = _Chats.IndexOf(chat);
                } else {
                    MoveChat(chat, chat.Position);
                }
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
                    chat.Close();
                    return;
                }
                chat.Close();

                // refresh chat positions
                foreach (var schat in _Chats) {
                    schat.Position = _Chats.IndexOf(schat);
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
        
        public void MoveChat(ChatModel chat, int newPosition)
        {
            Trace.Call(chat, newPosition);

            if (chat == null) {
                throw new ArgumentNullException("chat");
            }

            lock (_Chats) {
                _Chats.Remove(chat);
                _Chats.Insert(newPosition, chat);
                foreach (var schat in _Chats) {
                    schat.Position = _Chats.IndexOf(schat);
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

            lock (chat.MessageBuffer) {
                try {
                    chat.MessageBuffer.Add(msg);
                } catch (Exception ex) {
#if LOG4NET
                    Trace.Call(chat, msg, ignoreFilters);
                    f_Logger.Error(
                        "AddMessageToChat(): " +
                        "chat.MessageBuffer.Add() threw exception!", ex
                    );
#endif
                    if (chat.MessageBuffer is Db4oMessageBuffer) {
#if LOG4NET
                        f_Logger.Error(
                            "AddMessageToChat(): " +
                            "Falling back to volatile message buffer..."
                        );
#endif
                        chat.ResetMessageBuffer();
                        chat.InitMessageBuffer(MessageBufferPersistencyType.Volatile);

                        var builder = new MessageBuilder();
                        builder.AppendEventPrefix();
                        builder.AppendErrorText(
                            _("Failed to write to chat history. " +
                              "Your chat history will not be preserved. " +
                              "Reason: {0}"),
                            ex.Message
                        );
                        chat.MessageBuffer.Add(builder.ToMessage());

                        chat.MessageBuffer.Add(msg);
                    }
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
            protocolManager.Connect(frontendManager, server);
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
                // HACK: twitter retrieves older messages and we don't want to
                // re-log those when the twitter connection is re-opened
                var protocol = chat.ProtocolManager.Protocol.ToLower();
                if (protocol == "twitter") {
                    return;
                }
                using (var stream = File.AppendText(chat.LogFile)) {
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

        public void CheckPresenceStatus()
        {
            Trace.Call();

            var newStatus = PresenceStatus.Unknown;
            var newMessage = String.Empty;
            lock (_FrontendManagers) {
                if (_FrontendManagers.Count == 0) {
                    newStatus = PresenceStatus.Away;
                    newMessage = "away from keyboard";
                } else {
                    newStatus = PresenceStatus.Online;
                }
            }

            if (newStatus == PresenceStatus.Unknown) {
                return;
            }

            UpdatePresenceStatus(newStatus, newMessage);
        }

        void UpdatePresenceStatus(PresenceStatus status, string message)
        {
            lock (_ProtocolManagers) {
                foreach (var manager in _ProtocolManagers) {
                    manager.SetPresenceStatus(status, message);
                }
            }
        }

        IProtocolManager GetProtocolManagerByHost(string network)
        {
            lock (_ProtocolManagers) {
                foreach (var manager in _ProtocolManagers) {
                    if (String.Compare(manager.Host, network, true) == 0) {
                        return manager;
                    }
                }
            }
            return null;
        }

        IProtocolManager GetProtocolManagerByNetwork(string network)
        {
            lock (_ProtocolManagers) {
                foreach (var manager in _ProtocolManagers) {
                    if (String.Compare(manager.NetworkID, network, true) == 0) {
                        return manager;
                    }
                }
            }
            return null;
        }

        int GetSortedChatPosition(ChatModel chatModel)
        {
            int position = chatModel.Position;
            if (position != -1) {
                return position;
            }

            ChatType type = chatModel.ChatType;
            if (type != ChatType.Person &&
                type != ChatType.Group) {
                return position;
            }

            // new group person and group chats behind their protocol chat
            IProtocolManager pm = chatModel.ProtocolManager;
            lock (_Chats) {
                foreach (var chat in _Chats) {
                    if (chat.ChatType == ChatType.Protocol &&
                        chat.ProtocolManager == pm) {
                        position = _Chats.IndexOf(chat) + 1;
                        break;
                    }
                }

                if (position == -1) {
                    return position;
                }

                // now find the first chat with a different protocol manager
                foreach (var chat in _Chats.Skip(position)) {
                    if (chat.ProtocolManager != pm) {
                        return _Chats.IndexOf(chat);
                    }
                }
            }

            // if there was no next protocol manager, simply append
            // the chat way to the end
            return -1;
        }

        void InitSessionChat()
        {
            _SessionChat = new SessionChatModel("smuxi", "Smuxi");
            _Chats.Add(_SessionChat);

            var builder = CreateMessageBuilder();
            var text = builder.CreateText(_("Welcome to Smuxi"));
            text.ForegroundColor = new TextColor(255, 0, 0);
            text.Bold = true;
            builder.AppendText(text);
            builder.AppendText(Environment.NewLine);

            text = builder.CreateText(
                _("Type /help to get a list of available commands.")
            );
            text.Bold = true;
            builder.AppendText(text);
            builder.AppendText(Environment.NewLine);

            text = builder.CreateText(_("After you have made a connection " +
                "the list of available commands changes. Go to the newly " +
                "opened connection tab and use the /help command again to " +
                "see the extended command list."));
            text.Bold = true;
            builder.AppendText(text);
            builder.AppendText(Environment.NewLine);

            builder.AppendText(Environment.NewLine);

            builder.AppendHeader("Smuxi News");
            AddMessageToChat(_SessionChat,builder.ToMessage());
        }

        void UpdateNewsFeed()
        {
            Trace.Call();

            try {
                var url = "http://news.smuxi.org/feed.php";
                var req = WebRequest.Create(url);
                var proxySettings = new ProxySettings();
                proxySettings.ApplyConfig(UserConfig);
                req.Proxy = proxySettings.GetWebProxy(url);
                if (req is HttpWebRequest) {
                    var httpReq = (HttpWebRequest) req;
                    httpReq.UserAgent = Engine.VersionString;
                    if (NewsFeedLastModified != DateTime.MinValue) {
                        httpReq.IfModifiedSince = NewsFeedLastModified;
                    }
                }
                var res = req.GetResponse();
                if (res is HttpWebResponse) {
                    var httpRes = (HttpWebResponse) res;
                    if (httpRes.StatusCode == HttpStatusCode.NotModified) {
                        return;
                    }
                    NewsFeedLastModified = httpRes.LastModified;
                }
                var feed = AtomFeed.Load(res.GetResponseStream());
                var sortedEntries = feed.Entry.OrderBy(x => x.Published);
                foreach (var entry in sortedEntries) {
                    if (SeenNewsFeedIds.Contains(entry.Id)) {
                        continue;
                    }
                    SeenNewsFeedIds.Add(entry.Id);

                    var msg = new FeedMessageBuilder();
                    msg.Append(entry);
                    if (!msg.IsEmpty) {
                        msg.AppendText("\n");
                        AddMessageToChat(SessionChat, msg.ToMessage());
                    }
                }
            } catch (WebException ex) {
                switch (ex.Status) {
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.ConnectionClosed:
                    case WebExceptionStatus.Timeout:
                    case WebExceptionStatus.ReceiveFailure:
                    case WebExceptionStatus.NameResolutionFailure:
                    case WebExceptionStatus.ProxyNameResolutionFailure:
#if LOG4NET
                        f_Logger.Warn(
                            String.Format(
                                "UpdateNewsFeed(): Temporarily issue " +
                                "detected, retrying in {0} min...",
                                NewsFeedRetryInterval.Minutes
                            ),
                            ex
                        );
#endif
                        NewsFeedTimer.Change(NewsFeedRetryInterval, NewsFeedUpdateInterval);
                        break;
                }
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("UpdateNewsFeed(): Exception, ignored...", ex);
#endif
            }
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
