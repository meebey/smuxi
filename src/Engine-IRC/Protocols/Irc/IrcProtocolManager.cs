/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Meebey.SmartIrc4net;
using Smuxi.Common;
using IrcProxyType = Meebey.SmartIrc4net.ProxyType;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "IRC", Description = "Internet Relay Chat", Alias = "irc")]
    public class IrcProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string       _LibraryTextDomain = "smuxi-engine-irc";
        private IrcFeatures     _IrcClient;
        private ServerModel     _ServerModel;
        private string          _Host;
        private int             _Port;
        private string          _Network;
        private string[]        _Nicknames;
        private int             _CurrentNickname;
        private string          _Username;
        private string          _Password;
        private IrcPersonModel  _MyPerson;
        private FrontendManager _FrontendManager;
        private bool            _Listening;
        private ChatModel       _NetworkChat;
        private TimeSpan        _LastLag;
        private Thread          _RunThread;
        private Thread          _LagWatcherThread;
        private TaskQueue       _ChannelJoinQueue = new TaskQueue("JoinChannelQueue");
        private List<string>    _QueuedChannelJoinList = new List<string>();
        private List<string>    _ActiveChannelJoinList = new List<string>();
        private AutoResetEvent  _ActiveChannelJoinHandle = new AutoResetEvent(false);

        bool HasListMaskSearchSupport { get; set; }
        bool HasSafeListSupport { get; set; }
        IList<ChannelInfo> NetworkChannels { get; set; }
        DateTime NetworkChannelsAge { get; set; }
        TimeSpan NetworkChannelsMaxAge { get; set; }

        public override bool IsConnected {
            get {
                if ((_IrcClient != null) &&
                    (_IrcClient.IsConnected)) {
                    return true;
                }
                return false;
            }
        }
        
        public override string Host {
            get {
                if (_IrcClient == null) {
                    return null;
                }
                return _IrcClient.Address;
            }
        }
        
        public override int Port {
            get {
                if (_IrcClient == null) {
                    return -1;
                }
                return _IrcClient.Port;
            }
        }
        
        public override string NetworkID {
            get {
                if (String.IsNullOrEmpty(_Network)) {
                    return _IrcClient.Address;
                }
                return _Network;
            }
        }
        
        public override string Protocol {
            get {
                return "IRC";
            }
        }
        
        public override ChatModel Chat {
            get {
                return _NetworkChat;
            }
        }

        private string Prefix {
            get {
                if (_IrcClient == null) {
                    return String.Empty;
                }

                if (_MyPerson == null) {
                    return _IrcClient.Nickname;
                }

                return String.Format("{0}!{1}@{2}", _IrcClient.Nickname,
                                     _MyPerson.Ident, _MyPerson.Host);
            }
        }

        private IrcPersonModel MyPerson {
            get {
                if (_MyPerson == null) {
                    _MyPerson = CreatePerson(_IrcClient.Nickname);
                }
                return _MyPerson;
            }
        }
        
        public IrcProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);

            NetworkChannelsMaxAge = TimeSpan.FromMinutes(5);

            _IrcClient = new IrcFeatures();
            _IrcClient.AutoRetry = true;
            // keep retrying to connect forever
            _IrcClient.AutoRetryLimit = 0;
            _IrcClient.AutoRetryDelay = 120;
            _IrcClient.AutoReconnect = true;
            _IrcClient.AutoRelogin = true;
            _IrcClient.AutoRejoin = true;
            // HACK: SmartIrc4net <= 0.4.5.1 is not resetting the nickname list
            // after disconnect. This causes random nicks to be used when there
            // are many reconnects like when the network connection goes flaky,
            // see: http://projects.qnetp.net/issues/show/163
            _IrcClient.AutoNickHandling = false;
            _IrcClient.ActiveChannelSyncing = true;
            _IrcClient.CtcpVersion      = Engine.VersionString;
            _IrcClient.SendDelay        = 250;
            _IrcClient.OnRawMessage     += new IrcEventHandler(_OnRawMessage);
            _IrcClient.OnChannelMessage += new IrcEventHandler(_OnChannelMessage);
            _IrcClient.OnChannelAction  += new ActionEventHandler(_OnChannelAction);
            _IrcClient.OnChannelNotice  += new IrcEventHandler(_OnChannelNotice);
            _IrcClient.OnChannelActiveSynced += new IrcEventHandler(_OnChannelActiveSynced);
            _IrcClient.OnQueryMessage   += new IrcEventHandler(_OnQueryMessage);
            _IrcClient.OnQueryAction    += new ActionEventHandler(_OnQueryAction);
            _IrcClient.OnQueryNotice    += new IrcEventHandler(_OnQueryNotice);
            _IrcClient.OnJoin           += new JoinEventHandler(_OnJoin);
            _IrcClient.OnNames          += new NamesEventHandler(_OnNames);
            _IrcClient.OnPart           += new PartEventHandler(_OnPart);
            _IrcClient.OnKick           += new KickEventHandler(_OnKick);
            _IrcClient.OnNickChange     += new NickChangeEventHandler(_OnNickChange);
            _IrcClient.OnOp             += new OpEventHandler(_OnOp);
            _IrcClient.OnDeop           += new DeopEventHandler(_OnDeop);
            _IrcClient.OnVoice          += new VoiceEventHandler(_OnVoice);
            _IrcClient.OnDevoice        += new DevoiceEventHandler(_OnDevoice);
            _IrcClient.OnModeChange     += new IrcEventHandler(_OnModeChange);
            _IrcClient.OnTopic          += new TopicEventHandler(_OnTopic);
            _IrcClient.OnTopicChange    += new TopicChangeEventHandler(_OnTopicChange);
            _IrcClient.OnQuit           += new QuitEventHandler(_OnQuit);
            _IrcClient.OnRegistered     += new EventHandler(_OnRegistered);
            _IrcClient.OnDisconnected   += new EventHandler(_OnDisconnected);
            _IrcClient.OnAutoConnectError += OnAutoConnectError;
            _IrcClient.OnAway           += new AwayEventHandler(_OnAway);
            _IrcClient.OnUnAway         += new IrcEventHandler(_OnUnAway);
            _IrcClient.OnNowAway        += new IrcEventHandler(_OnNowAway);
            _IrcClient.OnCtcpRequest    += new CtcpEventHandler(_OnCtcpRequest);
            _IrcClient.OnCtcpReply      += new CtcpEventHandler(_OnCtcpReply);
            _IrcClient.OnWho            += OnWho;
            _IrcClient.OnInvite         += OnInvite;
            _IrcClient.OnReadLine       += OnReadLine;
            _IrcClient.OnWriteLine      += OnWriteLine;

            _IrcClient.CtcpUserInfo = (string) Session.UserConfig["Connection/Realname"];
            // disabled as we don't use / support DCC yet
            _IrcClient.CtcpDelegates.Remove("dcc");
            // finger we handle ourself, no little helga here!
            _IrcClient.CtcpDelegates["finger"] = delegate(CtcpEventArgs e) {
                _IrcClient.SendMessage(
                    SendType.CtcpReply, e.Data.Nick,
                    String.Format("{0} {1}",
                        e.CtcpCommand,
                        _IrcClient.CtcpUserInfo
                    )
                );
            };
            // time we handle ourself
            _IrcClient.CtcpDelegates["time"] = delegate(CtcpEventArgs e) {
                _IrcClient.SendMessage(
                    SendType.CtcpReply, e.Data.Nick,
                    String.Format("{0} {1}",
                        e.CtcpCommand,
                        DateTime.Now.ToString(
                            "ddd MMM dd HH:mm:ss yyyy",
                            DateTimeFormatInfo.InvariantInfo
                        )
                    )
                );
            };
        }

        private void OnWho(object sender, WhoEventArgs e)
        {
            if (e.WhoInfo.Nick == _IrcClient.Nickname) {
                // that's me!
                MyPerson.Ident = e.WhoInfo.Ident;
                MyPerson.Host = e.WhoInfo.Host;
                MyPerson.RealName = e.WhoInfo.Realname;
            }
        }

        private void OnInvite(object sender, InviteEventArgs e)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.MessageType = MessageType.Normal;
            builder.AppendIdendityName(CreatePerson(e.Who));
            // TRANSLATOR: do NOT change the position of {0}!
            var text = builder.CreateText(_("{0} invites you to {1}"),
                                          String.Empty, e.Channel);
            text.IsHighlight = true;
            builder.AppendText(text);
            Session.AddMessageToChat(_NetworkChat, builder.ToMessage());
        }

        void OnReadLine(object sender, ReadLineEventArgs e)
        {
            DebugRead(e.Line);
        }

        void OnWriteLine(object sender, WriteLineEventArgs e)
        {
            DebugWrite(e.Line);
        }

        public override string ToString()
        {
            string result = null;
            if (_IrcClient != null) {
                if (String.IsNullOrEmpty(_Network)) {
                    result += _IrcClient.Address;
                } else {
                    result += _Network;
                }
            }
            result += " (IRC)";
            
            if (IsConnected) {
                if (_IrcClient.IsAway) {
                    result += " (" + _("away") + ")";
                }
                if (_IrcClient.Lag > TimeSpan.FromSeconds(5)) {
                    result += String.Format(" ({0})",
                                    String.Format(
                                        // TRANSLATOR: {0} is the amount of seconds
                                        _("lag: {0} seconds"),
                                        (int) _IrcClient.Lag.TotalSeconds
                                    )
                              );
                }
            } else {
                result += " (" + _("not connected") + ")";
            }
            
            return result;
        }

        public override void Connect(FrontendManager fm, ServerModel server)
        {
            Trace.Call(fm, server);

            if (fm == null) {
                throw new ArgumentNullException("fm");
            }
            if (server == null) {
                throw new ArgumentNullException("server");
            }

            _FrontendManager = fm;
            _ServerModel = server;

            ApplyConfig(Session.UserConfig, server);

            // add fallbacks if only one nick was specified, else we get random
            // number nicks when nick collisions happen
            if (_Nicknames.Length == 1) {
                _Nicknames = new string[] { _Nicknames[0], _Nicknames[0] + "_", _Nicknames[0] + "__" };
            }

            // TODO: use config for single network chat or once per network manager
            _NetworkChat = Session.CreateChat<ProtocolChatModel>(
                _Network, "IRC " + _Network, this
            );

            // BUG: race condition when we use Session.AddChat() as it pushes this already
            // to the connected frontend and the frontend will sync and get the page 2 times!
            //Session.Chats.Add(_NetworkChat);
            // NOTABUG: the frontend manager needs to take care for that
            Session.AddChat(_NetworkChat);
            Session.SyncChat(_NetworkChat);

            _RunThread = new Thread(new ThreadStart(_Run));
            _RunThread.IsBackground = true;
            _RunThread.Name = String.Format(
                "IrcProtocolManager ({0}:{1}) listener",
                server.Hostname, server.Port
             );
            _RunThread.Start();
            
            _LagWatcherThread = new Thread(new ThreadStart(_LagWatcher));
            _LagWatcherThread.IsBackground = true;
            _LagWatcherThread.Name = String.Format(
                "IrcProtocolManager ({0}:{1}) lag watcher",
                server.Hostname, server.Port
             );
            _LagWatcherThread.Start();
        }

        public void Connect(FrontendManager fm)
        {
            Trace.Call(fm);
            
            try {
                MessageBuilder builder;
                if (!String.IsNullOrEmpty(_IrcClient.ProxyHost)) {
                    builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    builder.AppendText(_("Using proxy: {0}:{1}"),
                                       _IrcClient.ProxyHost,
                                       _IrcClient.ProxyPort);
                    Session.AddMessageToChat(Chat, builder.ToMessage());
                }

                string msg;
                msg = String.Format(_("Connecting to {0} port {1}..."), _Host, _Port);
                fm.SetStatus(msg);
                Session.AddTextToChat(_NetworkChat, "-!- " + msg);
                // TODO: add SSL support
                _IrcClient.Connect(_Host, _Port);
                fm.UpdateNetworkStatus();
                msg = String.Format(_("Connection to {0} established"), _Host);
                fm.SetStatus(msg);
                Session.AddTextToChat(_NetworkChat, "-!- " + msg);
                Session.AddTextToChat(_NetworkChat, "-!- " + _("Logging in..."));
                string realname = (string) Session.UserConfig["Connection/Realname"];
                if (realname.Trim().Length == 0) {
                    realname = "unset";
                }
                if (!Regex.IsMatch(_Username, "^[a-z0-9]+$", RegexOptions.IgnoreCase)) {
                    builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    builder.AppendWarningText(
                        "Warning: Your username (ident) contains special " +
                        "characters which the IRC server might refuse. " +
                        "If this happens please change your username in the " +
                        "server settings."
                    );
                    Session.AddMessageToChat(_NetworkChat, builder.ToMessage());
                }
                _IrcClient.Login(_Nicknames, realname, 0, _Username, _Password);
                
                foreach (string command in (string[]) Session.UserConfig["Connection/OnConnectCommands"]) {
                    if (command.Length == 0) {
                        continue;
                    } 
                    CommandModel cd = new CommandModel(_FrontendManager, _NetworkChat,
                        (string) Session.UserConfig["Interface/Entry/CommandCharacter"],
                        command);
                    
                    bool handled;
                    handled = Session.Command(cd);
                    if (!handled) {
                        Command(cd);
                    }
                }
                _Listening = true;
            } catch (CouldNotConnectException ex) {
                fm.SetStatus(_("Connection failed!"));
                Session.AddTextToChat(_NetworkChat, "-!- " + _("Connection failed! Reason: ") + ex.Message);
                throw;
            }
        }
        
        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            
            fm.SetStatus(_("Disconnecting..."));
            if (IsConnected) {
                Session.AddTextToChat(_NetworkChat, "-!- " + 
                    String.Format(_("Disconnecting from {0}..."),
                                  _IrcClient.Address));
                // else the Listen() thread would try to connect again
                _Listening = false;
                _IrcClient.Disconnect();
                fm.SetStatus(String.Format(_("Disconnected from {0}"),
                                           _IrcClient.Address));
                Session.AddTextToChat(_NetworkChat, "-!- " +
                    _("Connection closed"));
                
                // TODO: set someone else as current network manager?
            } else {
                fm.SetStatus(String.Empty);
                fm.AddTextToChat(_NetworkChat, "-!- " + _("Not connected"));
            }
            
            if (_RunThread != null && _RunThread.IsAlive) {
                try {
                    _RunThread.Abort();
                } catch (Exception ex) {
#if LOG4NET
                    _Logger.Error("_RunThread.Abort() failed:", ex);
#endif
                }
            }
            if (_LagWatcherThread != null && _LagWatcherThread.IsAlive) {
                try {
                    _LagWatcherThread.Abort();
                } catch (Exception ex) {
#if LOG4NET
                    _Logger.Error("_LagWatcherThread.Abort() failed:", ex);
#endif
                }
            }
            
            fm.UpdateNetworkStatus();
        }
        
        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            
            fm.SetStatus(_("Reconnecting..."));
            try {
                string msg;
                if (_IrcClient != null) {
                    if (_IrcClient.IsConnected) {
                        Session.AddTextToChat(
                            _NetworkChat,
                            String.Format(
                                "-!- " + _("Reconnecting to {0}..."),
                                _IrcClient.Address
                            )
                        );
                        ApplyConfig(Session.UserConfig, _ServerModel);
                        _IrcClient.Reconnect(true);
                        msg = String.Format(_("Connection to {0} established"),
                                            _IrcClient.Address);
                        fm.SetStatus(msg); 
                        Session.AddTextToChat(_NetworkChat, "-!- " + msg);
                    } else {
                        Connect(fm);
                    }
                } else {
                    msg =  _("Reconnect Error");
                    fm.SetStatus(msg);
                    Session.AddTextToChat(_NetworkChat, "-!- " + msg);
                }
            } catch (ConnectionException) {
                fm.SetStatus(String.Empty);
                fm.AddTextToChat(_NetworkChat, "-!- " + _("Not connected"));
            }
            fm.UpdateNetworkStatus();
        }

        public override void Dispose()
        {
            Trace.Call();

            _ChannelJoinQueue.Dispose();

            base.Dispose();
        }

        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);

            // invalidate channel list cache when too old
            if (NetworkChannels != null &&
                (DateTime.UtcNow - NetworkChannelsAge) > NetworkChannelsMaxAge) {
                NetworkChannels = null;
            }

            string searchPattern = null;
            if (filter == null || String.IsNullOrEmpty(filter.Name)) {
                // full channel list
            } else {
                if (!filter.Name.StartsWith("*") && !filter.Name.EndsWith("*")) {
                    searchPattern = String.Format("*{0}*", filter.Name);
                } else {
                    searchPattern = filter.Name;
                }
            }
            var channels = NetworkChannels;
            if (channels == null && HasSafeListSupport) {
                // fetch and cache full channel list from server
                channels = _IrcClient.GetChannelList(String.Empty);
                NetworkChannels = channels;
                NetworkChannelsAge = DateTime.UtcNow;
            } else if (channels == null && searchPattern != null &&
                HasListMaskSearchSupport) {
                channels = _IrcClient.GetChannelList(searchPattern);
            } else if (channels == null) {
                // Houston, we have a problem
                // no safelist and empty search pattern, the IRCd might kill us!
                channels = _IrcClient.GetChannelList(String.Empty);
                NetworkChannels = channels;
                NetworkChannelsAge = DateTime.UtcNow;
            }

            List<GroupChatModel> chats = new List<GroupChatModel>(channels.Count);
            foreach (ChannelInfo info in channels) {
                if (channels == NetworkChannels &&
                    searchPattern != null &&
                    !Pattern.IsMatch(info.Channel, searchPattern)) {
                    continue;
                }

                GroupChatModel chat = new GroupChatModel(
                    info.Channel,
                    info.Channel,
                    null
                );
                chat.PersonCount = info.UserCount;

                var topic = CreateMessageBuilder();
                topic.AppendMessage(info.Topic);
                chat.Topic = topic.ToMessage();

                chats.Add(chat);
            }
            
            return chats;
        }
        
        public override void OpenChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);
            
            CommandModel cmd = new CommandModel(fm, _NetworkChat, chat.ID);
            switch (chat.ChatType) {
                case ChatType.Person:
                    CommandMessage(cmd);
                    break;
                case ChatType.Group:
                    CommandJoin(cmd);
                    break;
            }
        }

        public override void CloseChat(FrontendManager fm, ChatModel chatInfo)
        {
            Trace.Call(fm, chatInfo);

            if (fm == null) {
                throw new ArgumentNullException("fm");
            }
            if (chatInfo == null) {
                throw new ArgumentNullException("chatInfo");
            }

            // get real chat object from session
            var chat = GetChat(chatInfo.ID, chatInfo.ChatType);
            if (chat == null) {
#if LOG4NET
                _Logger.Error("CloseChat(): Session.GetChat(" +
                              chatInfo.ID + ", " + chatInfo.ChatType + ")" +
                              " returned null!");
#endif
                return;
            }
            if (!chat.IsEnabled) {
                Session.RemoveChat(chat);
                return;
            }

            switch (chat.ChatType) {
                case ChatType.Person:
                    Session.RemoveChat(chat);
                    break;
                case ChatType.Group:
                    CommandModel cmd = new CommandModel(fm, _NetworkChat, chat.ID);
                    CommandPart(cmd);
                    break;
            }
        }
        
        public override void SetPresenceStatus(PresenceStatus status,
                                               string message)
        {
            Trace.Call(status, message);

            if (!_IrcClient.IsConnected) {
                return;
            }

            switch (status) {
                case PresenceStatus.Online:
                    if (!_IrcClient.IsAway) {
                        // nothing to do
                        return;
                    }
                    _IrcClient.RfcAway();
                    break;
                case PresenceStatus.Away:
                    if (String.IsNullOrEmpty(message)) {
                        // HACK: empty away message unsets away state on IRC
                        message = "away";
                    }
                    _IrcClient.RfcAway(message);
                    break;
            }
        }

        public override bool Command(CommandModel command)
        {
            Trace.Call(command);
            
            bool handled = false;
            if (IsConnected) {
                if (command.IsCommand) {
                    // commands which work when we have a connection
                    switch (command.Command) {
                        case "help":
                            CommandHelp(command);
                            handled = true;
                            break;
                        // commands which work on serverchat/channels/queries
                        case "j":
                        case "join":
                            CommandJoin(command);
                            handled = true;
                            break;
                        case "msg":
                        case "query":
                            CommandMessage(command);
                            handled = true;
                            break;
                        case "amsg":
                            CommandAllMessage(command);
                            handled = true;
                            break;
                        case "anotice":
                            CommandAllNotice(command);
                            handled = true;
                            break;
                        case "ame":
                            CommandAllMe(command);
                            handled = true;
                            break;
                        case "notice":
                            CommandNotice(command);
                            handled = true;
                            break;
                        case "nick":
                            CommandNick(command);
                            handled = true;
                            break;
                        case "raw":
                        case "quote":
                            CommandRaw(command);
                            handled = true;
                            break;
                        case "ping":
                            CommandPing(command);
                            handled = true;
                            break;
                        case "version":
                            CommandVersion(command);
                            handled = true;
                            break;
                        case "time":
                            CommandTime(command);
                            handled = true;
                            break;
                        case "finger":
                            CommandFinger(command);
                            handled = true;
                            break;
                        case "who":
                            CommandWho(command);
                            handled = true;
                            break;
                        case "whois":
                            CommandWhoIs(command);
                            handled = true;
                            break;
                        case "whowas":
                            CommandWhoWas(command);
                            handled = true;
                            break;
                        case "away":
                            CommandAway(command);
                            // send away on all other IRC networks too
                            foreach (IProtocolManager nm in Session.ProtocolManagers) {
                                if (nm == this) {
                                    // skip us, else we send it 2 times
                                    continue;
                                }
                                if (nm is IrcProtocolManager) {
                                    IrcProtocolManager ircnm = (IrcProtocolManager)nm;
                                    ircnm.CommandAway(command);
                                }
                            }
                            handled = true;
                            break;
                        case "ctcp":
                            CommandCtcp(command);
                            handled = true;
                            break;
                        // commands which only work on channels or queries
                        case "me":
                            CommandMe(command);
                            handled = true;
                           break;
                        case "say":
                            CommandSay(command);
                            handled = true;
                            break;
                        // commands which only work on channels
                        case "p":
                        case "part":
                            CommandPart(command);
                            handled = true;
                            break;
                        case "topic":
                            CommandTopic(command);
                            handled = true;
                            break;
                        case "cycle":
                        case "rejoin":
                            CommandCycle(command);
                            handled = true;
                            break;
                        case "op":
                            CommandOp(command);
                            handled = true;
                            break;
                        case "deop":
                            CommandDeop(command);
                            handled = true;
                            break;
                        case "voice":
                            CommandVoice(command);
                            handled = true;
                            break;
                        case "devoice":
                            CommandDevoice(command);
                            handled = true;
                            break;
                        case "ban":
                            CommandBan(command);
                            handled = true;
                            break;
                        case "unban":
                            CommandUnban(command);
                            handled = true;
                            break;
                        case "kick":
                            CommandKick(command);
                            handled = true;
                            break;
                        case "kickban":
                        case "kb":
                            CommandKickban(command);
                            handled = true;
                            break;
                        case "mode":
                            CommandMode(command);
                            handled = true;
                            break;
                        case "invite":
                            CommandInvite(command);
                            handled = true;
                            break;
                        case "names":
                            CommandNames(command);
                            handled = true;
                            break;
                        case "quit":
                            CommandQuit(command);
                            handled = true;
                            break;
                        default:
                            CommandFallback(command);
                            handled = true;
                            break;
                    }
                } else {
                    // normal text
                    if (command.Chat.ChatType == ChatType.Session ||
                        command.Chat.ChatType == ChatType.Protocol) {
                        // we are on the session chat or protocol chat 
                        _IrcClient.WriteLine(command.Data);
                    } else {
                        // split too long messages
                        var messages = SplitMessage("PRIVMSG", command.Chat.ID,
                                                    command.Data);
                        foreach (string message in messages) {
                            _Say(command.Chat, message);
                        }
                    }
                    handled = true;
                }
            } else {
                if (command.IsCommand) {
                    // commands which work even without beeing connected
                    switch (command.Command) {
                        case "help":
                            CommandHelp(command);
                            handled = true;
                            break;
                        case "connect":
                            CommandConnect(command);
                            handled = true;
                            break;
                    }
                } else {
                    // normal text, without connection
                    _NotConnected(command);
                    handled = true;
                }
            }
            
            return handled;
        }

        private void CommandFallback(CommandModel cmd)
        {
            string parameters;
            if (cmd.DataArray.Length <= 3) {
                parameters = cmd.Parameter;
            } else {
                parameters = String.Format("{0} :{1}",
                    cmd.DataArray[1],
                    String.Join(" ",
                        cmd.DataArray, 2,
                        cmd.DataArray.Length - 2));
            }
            string data = String.Format("{0}raw {1} {2}",
                                        cmd.CommandCharacter,
                                        cmd.Command,
                                        parameters);
            CommandModel command = new CommandModel(
                cmd.FrontendManager,
                cmd.Chat,
                cmd.CommandCharacter,
                data
            );
            CommandRaw(command);
        }

        public void CommandHelp(CommandModel cd)
        {
            var builder = CreateMessageBuilder();
            // TRANSLATOR: this line is used as label / category for a
            // list of commands below
            builder.AppendHeader(_("IrcProtocolManager Commands"));
            cd.FrontendManager.AddMessageToChat(cd.Chat, builder.ToMessage());

            string[] help = {
            "help",
            "connect irc server [port|+port] [password] [nicknames]",
            "say",
            "join/j channel(s) [key]",
            "part/p [channel(s)] [part-message]",
            "topic [new-topic]",
            "names",
            "cycle/rejoin",
            "msg/query (channel|nick) message",
            "amsg message",
            "me action-message",
            "ame action-message",
            "notice (channel|nick) message",
            "anotice message",
            "invite nick [channel]",
            "who nick/channel",
            "whois nick",
            "whowas nick",
            "ping nick",
            "version nick",
            "time nick",
            "finger nick",
            "mode new-mode",
            "away [away-message]",
            "kick nick(s) [reason]",
            "kickban/kb nick(s) [reason]",
            "ban [mask]",
            "unban mask",
            "voice nick",
            "devoice nick",
            "op nick",
            "deop nick",
            "nick newnick",
            "ctcp destination command [data]",
            "raw/quote irc-command",
            "quit [quit-message]",
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
            FrontendManager fm = cd.FrontendManager;

            var server = new IrcServerModel();
            if (cd.DataArray.Length >= 3) {
                server.Hostname = cd.DataArray[2];
            } else {
                server.Hostname = "localhost";
            }
            
            if (cd.DataArray.Length >= 4) {
                var port = cd.DataArray[3];
                var ssl = port.StartsWith("+");
                if (ssl) {
                    server.UseEncryption = true;
                    port = port.Substring(1);
                }
                try {
                    server.Port = Int32.Parse(port);
                } catch (FormatException) {
                    fm.AddTextToChat(
                        cd.Chat,
                        String.Format("-!- {0}",
                            String.Format(
                                _("Invalid port: {0}"),
                                cd.DataArray[3]
                            )
                        )
                    );
                    return;
                }
            } else {
                server.Port = 6667;
            }
            
            if (cd.DataArray.Length >= 5) {
                server.Password = cd.DataArray[4];
            }
            
            if (cd.DataArray.Length >= 6) {
                var nicks = new List<string>(1);
                nicks.Add(cd.DataArray[5]);
                server.Nicknames = nicks;
            }

            Connect(fm, server);
        }
        
        public void CommandSay(CommandModel cd)
        {
            _Say(cd.Chat, cd.Parameter);
        }
        
        private void _Say(ChatModel chat, string message)
        {
            if (!chat.IsEnabled) {
                return;
            }

            if (chat is PersonChatModel) {
                PersonModel person = ((PersonChatModel) chat).Person;
                IrcPersonModel ircperson = (IrcPersonModel) person;
                ircperson.IsAway = false;
            }

            _IrcClient.SendMessage(SendType.Message, chat.ID, message);

            var builder = CreateMessageBuilder();
            builder.AppendSenderPrefix(MyPerson);
            Match m = Regex.Match(message, String.Format(@"^@(?<nick>\S+)|^(?<nick>\S+)(?:\:|,)"));
            if (m.Success) {
                // this is probably a reply with a nickname
                string nick = m.Groups["nick"].Value;
#if LOG4NET
                _Logger.Debug("_Say(): detected reply with possible nick: '" + nick + "' in: '" + m.Value + "'");
#endif
                if (_IrcClient.GetChannelUser(chat.ID, nick) != null) {
                    // bingo, it's a nick on this channel
                    message = message.Substring(m.Value.Length);
                    var coloredNick = builder.CreateIdendityName(GetPerson(chat, nick));
                    coloredNick.Text = m.Value;
                    builder.AppendText(coloredNick);
                }
            }
            builder.AppendMessage(message);
            Session.AddMessageToChat(chat, builder.ToMessage(), true);
        }
        
        public void CommandJoin(CommandModel cd)
        {
            Trace.Call(cd);
            
            string channelStr = null;
            if ((cd.DataArray.Length >= 2) &&
                (cd.DataArray[1].Length >= 1)) {
                switch (cd.DataArray[1][0]) {
                    case '#':
                    case '!':
                    case '+':
                    case '&':
                        channelStr = cd.DataArray[1];
                        break;
                    default:
                        channelStr = "#" + cd.DataArray[1];
                        break;
                }
            } else {
                _NotEnoughParameters(cd);
                return;
            }
            

            string[] channels = channelStr.Split(',');
            string[] keys = null;
            if (cd.DataArray.Length > 2) {
                keys = cd.DataArray[2].Split(',');
            }
            
            int activeCount;
            lock (_ActiveChannelJoinList) {
                activeCount = _ActiveChannelJoinList.Count;
            }
            if (activeCount > 0) {
                // ok, these channels will be queued
                cd.FrontendManager.AddTextToChat(
                    _NetworkChat,
                    "-!- " +
                    String.Format(
                        _("Queuing joins: {0}"),
                        String.Join(" ", channels)
                    )
                );
            }

            int i = 0;
            foreach (string channel in channels) {
                string key = keys != null && keys.Length > i ? keys[i] : null;
                if (_IrcClient.IsJoined(channel)) {
                    cd.FrontendManager.AddTextToChat(
                        cd.Chat,
                        "-!- " +
                        String.Format(
                            _("Already joined to channel: {0}." +
                            " Type /window {0} to switch to it."),
                            channel));
                    continue;
                }

                lock (_QueuedChannelJoinList) {
                    _QueuedChannelJoinList.Add(channel);
                }

                // HACK: copy channel from foreach() into our scope
                string chan = channel;
                _ChannelJoinQueue.Queue(delegate {
                    try {
                        int count = 0;
                        string activeChans = null;
                        lock (_ActiveChannelJoinList) {
                            count = _ActiveChannelJoinList.Count;
                            if (count > 0) {
                                activeChans = String.Join(
                                    " ",  _ActiveChannelJoinList.ToArray()
                                );
                            }
                        }
                        if (count > 0) {
                            string queuedChans;
                            lock (_QueuedChannelJoinList) {
                                queuedChans = String.Join(
                                    " ",  _QueuedChannelJoinList.ToArray()
                                );
                            }
                            cd.FrontendManager.AddTextToChat(
                                _NetworkChat,
                                "-!- " +
                                String.Format(
                                    _("Active joins: {0} - Queued joins: {1}"),
                                    activeChans, queuedChans
                                )
                            );

#if LOG4NET
                            _Logger.Debug("CommandJoin(): waiting to join: " + chan);
#endif
                            _ActiveChannelJoinHandle.WaitOne();

                            lock (_ActiveChannelJoinList) {
                                activeChans = String.Join(
                                    " ",  _ActiveChannelJoinList.ToArray()
                                );
                            }
                            lock (_QueuedChannelJoinList) {
                                _QueuedChannelJoinList.Remove(chan);
                                queuedChans = String.Join(
                                    " ",  _QueuedChannelJoinList.ToArray()
                                );
                            }
                            // TRANSLATORS: final message will look like this:
                            // Joining: #chan1 - Remaining active joins: #chan2 / queued joins: #chan3
                            string msg = String.Format(_("Joining: {0}"), chan);
                            if (activeChans.Length > 0 || queuedChans.Length > 0) {
                                msg += String.Format(" - {0} ", _("Remaining"));
                                
                            }
                            if (activeChans.Length > 0) {
                                msg += String.Format(
                                    _("active joins: {0}"),
                                    activeChans
                                );
                            }
                            if (queuedChans.Length > 0) {
                                if (activeChans.Length > 0) {
                                    msg += " / ";
                                }
                                msg += String.Format(
                                    _("queued joins: {0}"),
                                    queuedChans
                                );
                            }
                            cd.FrontendManager.AddTextToChat(
                                _NetworkChat,
                                "-!- " + msg
                            );
                        } else {
                            lock (_QueuedChannelJoinList) {
                                _QueuedChannelJoinList.Remove(chan);
                            }
                            cd.FrontendManager.AddTextToChat(
                                _NetworkChat,
                                "-!- " +
                                String.Format(_("Joining: {0}"), chan)
                            );
                        }
#if LOG4NET
                        _Logger.Debug("CommandJoin(): joining: " + chan);
#endif
                        // we have a slot, show time!
                        if (key == null) {
                            _IrcClient.RfcJoin(chan);
                        } else {
                            _IrcClient.RfcJoin(chan, key);
                        }

                        // Some IRC networks are very kick happy and thus need
                        // some artificial delay between JOINs.
                        // We know our friendly networks though :)
                        string network = _Network == null ? String.Empty : _Network.ToLower();
                        switch (network) {
                            case "efnet":
                            case "freenode":
                            case "gimpnet":
                            case "ircnet":
                            case "oftc":
                                // give the IRCd some time to actually sent us a JOIN
                                // confirmation, else we will just hammer all channels
                                // in a single row
                                _ActiveChannelJoinHandle.WaitOne(2 * 1000, false);
                                break;
                            default:
                                // delay the queue for some extra seconds so new join
                                // attempts will not happen too early as some IRCds
                                // limit this and disconnect us if we are not brave
                                Thread.Sleep(2000);
                                break;
                        }
                    } catch (ThreadAbortException ex) {
#if LOG4NET
                        _Logger.Warn("ThreadAbortException when trying to join channel: "
                                      + chan, ex);
#endif
                    } catch (Exception ex) {
#if LOG4NET
                        _Logger.Error("Exception when trying to join channel: "
                                      + chan, ex);
#endif
                    }
                });

                i++;
            }
        }

        public void CommandCycle(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.Chat.ChatType == ChatType.Group) {
                if (cd.Chat.IsEnabled) {
                    // disable chat so we don't loose the message buffer
                    Session.DisableChat(cd.Chat);
                    _IrcClient.RfcPart(cd.Chat.ID);
                }
                _IrcClient.RfcJoin(cd.Chat.ID);
            }
        }
        
        public void CommandMessage(CommandModel cd)
        {
            Trace.Call(cd);
            
            if ((cd.DataArray.Length >= 2) &&
                (cd.DataArray[1].Length >= 1)) {
                switch (cd.DataArray[1][0]) {
                    case '#':
                    case '!':
                    case '+':
                    case '&':
                        // seems to be a channel
                        CommandMessageChannel(cd);
                        break;
                    default:
                        // seems to be a nick
                        CommandMessageQuery(cd);
                        break;
                }
            } else {
                CommandMessageQuery(cd);
            }
        }
        
        public void CommandMessageQuery(CommandModel cd)
        {
            ChatModel chat = null;
            if (cd.DataArray.Length >= 2) {
                string nickname = cd.DataArray[1];
                chat = GetChat(nickname, ChatType.Person);
                if (chat == null) {
                    var person = CreatePerson(nickname);
                    chat = Session.CreatePersonChat(person, nickname,
                                                    nickname, this);
                    Session.AddChat(chat);
                    Session.SyncChat(chat);
                }
            }
            
            if (cd.DataArray.Length >= 3) {
                string message = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);
                // ignore empty messages
                if (message.TrimEnd(' ').Length > 0) {
                    _Say(chat, message);
                }
            }
        }
        
        public void CommandMessageChannel(CommandModel cd)
        {
            if (cd.DataArray.Length >= 3) {
                string message = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);
                string channelname = cd.DataArray[1];

                ChatModel chat = GetChat(channelname, ChatType.Group);
                if (chat == null) {
                    // server chat as fallback if we are not joined
                    chat = _NetworkChat;
                    Session.AddTextToChat(chat, "<" + _IrcClient.Nickname + ":" + channelname + "> " + message, true);
                } else {
                     _Say(chat, message);
                }

                _IrcClient.SendMessage(SendType.Message, channelname, message);
            } else {
                _NotEnoughParameters(cd);
            }
        }

        private IList<string> SplitMessage(string command, string target, string message)
        {
            List<string> messages = new List<string>();
            int length;
            int line = 0;
            do {
                length = GetProtocolMessageLength(command, target, message);
                if (length <= 512) {
                    if (line > 0) {
                        // remove leading spaces as we are a new line
                        messages.Add(message.TrimStart(new char[] {' '}));
                    } else {
                        messages.Add(message);
                    }
                    break;
                }
                line++;

                int maxMsgLen = message.Length - (length - 512);
                string chunk = message.Substring(0, maxMsgLen);
                string nextChar = message.Substring(maxMsgLen, 1);
                if (nextChar != " ") {
                    // we split in the middle of a word, split it better!
                    int lastWordPos = chunk.LastIndexOf(" ");
                    if (lastWordPos > 0) {
                        // the chunk has to get smaller, else we run into an
                        // endless loop
                        chunk = chunk.Substring(0, lastWordPos);
                    }
                }
                // remove leading spaces as we are a new line
                messages.Add(chunk.TrimStart(new char[] {' '}));
                message = message.Substring(chunk.Length);
            } while (true);

            return messages;
        }

        public void CommandAllMessage(CommandModel cd)
        {
            if (cd.DataArray.Length < 2) {
                _NotEnoughParameters(cd);
                return;
            }
            
            string message = cd.Parameter;
            foreach (ChatModel chat in Chats) {
                if (chat.ChatType != ChatType.Group) {
                    // only show on group chats
                    continue;
                }

                CommandModel msgCmd = new CommandModel(
                    cd.FrontendManager,
                    cd.Chat,
                    String.Format("{0} {1}", chat.ID, message)
                );
                CommandMessageChannel(msgCmd);
            }
        }
        
        public void CommandAllNotice(CommandModel cd)
        {
            if (cd.DataArray.Length < 2) {
                _NotEnoughParameters(cd);
                return;
            }
            
            string message = cd.Parameter;
            foreach (ChatModel chat in Chats) {
                if (chat.ChatType != ChatType.Group) {
                    // only show on group chats
                    continue;
                }
                
                CommandModel msgCmd = new CommandModel(
                    cd.FrontendManager,
                    cd.Chat,
                    String.Format("{0} {1}", chat.ID, message)
                );
                CommandNotice(msgCmd);
            }
        }
        
        public void CommandAllMe(CommandModel cd)
        {
            if (cd.DataArray.Length < 2) {
                _NotEnoughParameters(cd);
                return;
            }
            
            string message = cd.Parameter;
            foreach (ChatModel chat in Chats) {
                if (chat.ChatType != ChatType.Group) {
                    // only show on group chats
                    continue;
                }
                
                CommandModel msgCmd = new CommandModel(
                    cd.FrontendManager,
                    chat,
                    message
                );
                CommandMe(msgCmd);
            }
        }
        
        public void CommandPart(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            if ((cd.DataArray.Length >= 2) &&
                (cd.DataArray[1].Length >= 1)) {
                // have to guess here if we got a channel passed or not
                switch (cd.DataArray[1][0]) {
                    case '#':
                    case '&':
                    case '!':
                    case '+':
                        // seems to be a channel
                        string[] channels = cd.DataArray[1].Split(new char[] {','});
                        string message = null;
                        if  (cd.DataArray.Length >= 3) {
                            message = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);
                        }
                        foreach (string channel in channels) {
                            if (message != null) {
                                _IrcClient.RfcPart(channel, message);
                            } else { 
                                _IrcClient.RfcPart(channel);
                            }
                        }
                        break;
                    default:
                        // sems to be only a part message
                        _IrcClient.RfcPart(chat.ID, cd.Parameter);
                        break;
                }
            } else {
                _IrcClient.RfcPart(chat.ID);
            }
        }
        
        public void CommandAway(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                _IrcClient.RfcAway(cd.Parameter);
            } else {
                _IrcClient.RfcAway();
            }
        }
        
        public void CommandCtcp(CommandModel cd)
        {
            if (cd.DataArray.Length >= 3) {
                string destination = cd.DataArray[1];
                string command = cd.DataArray[2].ToUpper();
                string parameters = String.Empty;
                if (cd.DataArray.Length >= 4) {
                    parameters = String.Join(" ", cd.DataArray, 3, cd.DataArray.Length-3);
                }
                Session.AddTextToChat(_NetworkChat, "[ctcp(" + destination + ")] " + command + " " + parameters);
                _IrcClient.SendMessage(SendType.CtcpRequest, destination, command + " " + parameters);
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        public void CommandPing(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                string destination = cd.DataArray[1];
                string timestamp = DateTime.Now.ToFileTime().ToString();
                Session.AddTextToChat(_NetworkChat, "[ctcp(" + destination + ")] PING " + timestamp);
                _IrcClient.SendMessage(SendType.CtcpRequest, destination, "PING " + timestamp);
            } else {
                _NotEnoughParameters(cd);
            }
        }

        public void CommandTime(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                string destination = cd.DataArray[1];
                Session.AddTextToChat(_NetworkChat, "[ctcp(" + destination + ")] TIME");
                _IrcClient.SendMessage(SendType.CtcpRequest, destination, "TIME");
            } else {
                _NotEnoughParameters(cd);
            }
        }

        public void CommandVersion(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                string destination = cd.DataArray[1];
                Session.AddTextToChat(_NetworkChat, "[ctcp(" + destination + ")] VERSION");
                _IrcClient.SendMessage(SendType.CtcpRequest, destination, "VERSION");
            } else {
                _NotEnoughParameters(cd);
            }
        }

        public void CommandFinger(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                string destination = cd.DataArray[1];
                Session.AddTextToChat(_NetworkChat, "[ctcp(" + destination + ")] FINGER");
                _IrcClient.SendMessage(SendType.CtcpRequest, destination, "FINGER");
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        public void CommandWho(CommandModel cd)
        {
            if (cd.DataArray.Length < 2) {
                _NotEnoughParameters(cd);
                return;
            }
            
            IList<WhoInfo> infos = _IrcClient.GetWhoList(cd.DataArray[1]);
            // irssi: * meebey    H   1  ~meebey@e176002059.adsl.alicedsl.de [Mirco Bauer]
            foreach (WhoInfo info in infos) {
                string mode;
                if (info.IsIrcOp) {
                    mode = _("IRC Op");
                } else if (info.IsOp) {
                    mode = _("Op");
                } else if (info.IsVoice) {
                    mode = _("Voice");
                } else {
                    mode = String.Empty;
                }
                string msg = String.Format(
                    "-!- {0} {1} {2}{3} {4} {5}@{6} [{7}]",
                    info.Channel,
                    info.Nick,
                    mode,
                    info.IsAway ? " (" + _("away") + ")" : String.Empty,
                    info.HopCount,
                    info.Ident,
                    info.Host,
                    info.Realname);
                Session.AddTextToChat(cd.Chat, msg);
            }
        }

        public void CommandWhoIs(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                _IrcClient.RfcWhois(cd.DataArray[1]);
            } else {
                if (cd.Chat is PersonChatModel) {
                    var pchat = (PersonChatModel) cd.Chat;
                    _IrcClient.RfcWhois(pchat.Person.ID);
                } else {
                    _NotEnoughParameters(cd);
                }
            }
        }
        
        public void CommandWhoWas(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                _IrcClient.RfcWhowas(cd.DataArray[1]);
            } else {
                if (cd.Chat is PersonChatModel) {
                    var pchat = (PersonChatModel) cd.Chat;
                    _IrcClient.RfcWhowas(pchat.Person.ID);
                } else {
                    _NotEnoughParameters(cd);
                }
            }
        }
        
        public void CommandTopic(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            ChatModel chat = cd.Chat;
            string channel = chat.ID;
            if (cd.DataArray.Length >= 2) {
                _IrcClient.RfcTopic(channel, cd.Parameter);
            } else {
                if (_IrcClient.IsJoined(channel)) {
                    string topic = _IrcClient.GetChannel(channel).Topic;
                    if (topic.Length > 0) {
                        var builder = CreateMessageBuilder();
                        builder.AppendEventPrefix();
                        // TRANSLATOR: do NOT change the position of {1}!
                        builder.AppendText(_("Topic for {0}: {1}"), channel, String.Empty);
                        builder.AppendMessage(topic);
                        fm.AddMessageToChat(chat, builder.ToMessage());
                    } else {
                        fm.AddTextToChat(chat,
                            "-!- " + String.Format(_("No topic set for {0}"), channel));
                    }
                }
            }
        }
        
        public void CommandOp(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            string channel = chat.ID;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Op(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.TrimEnd().Split(new char[] {' '});
                _IrcClient.Op(channel, candidates);
            } else {
                _NotEnoughParameters(cd);
            }
        }
    
        public void CommandDeop(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            string channel = chat.ID;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Deop(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.TrimEnd().Split(new char[] {' '});
                _IrcClient.Deop(channel, candidates);
            } else {
                _NotEnoughParameters(cd);
            }
        }

        public void CommandVoice(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            string channel = chat.ID;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Voice(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.TrimEnd().Split(new char[] {' '});
                _IrcClient.Voice(channel, candidates);
            } else {
                _NotEnoughParameters(cd);
            }
        }

        public void CommandDevoice(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            string channel = chat.ID;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Devoice(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.TrimEnd().Split(new char[] {' '});
                _IrcClient.Devoice(channel, candidates);
            } else {
                _NotEnoughParameters(cd);
            }
        }

        public void CommandBan(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            string channel = chat.ID;
            if (cd.DataArray.Length == 2) {
                // TODO: use a smart mask by default
                _IrcClient.Ban(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.TrimEnd().Split(new char[] {' '});
                _IrcClient.Ban(channel, candidates);
            } else {
                IList<BanInfo> infos = _IrcClient.GetBanList(channel);
                int i = 1;
                foreach (BanInfo info in infos) {
                    string msg = String.Format(
                        "-!- {0} - {1}: {2} {3}",
                        i++,
                        info.Channel,
                        _("ban"),
                        info.Mask
                    );
                    Session.AddTextToChat(cd.Chat, msg);
                }
                if (infos.Count == 0) {
                    Session.AddTextToChat(
                        cd.Chat,
                        String.Format(
                            "-!- {0} {1}",
                            _("No bans in channel"),
                            channel
                        )
                    );
                }
            }
        }

        public void CommandUnban(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            string channel = chat.ID;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Unban(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.TrimEnd().Split(new char[] {' '});
                _IrcClient.Unban(channel, candidates);
            } else {
                _NotEnoughParameters(cd);
            }
        }

        public void CommandKick(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            string channel = chat.ID;
            if (cd.DataArray.Length >= 2) {
                string[] candidates = cd.DataArray[1].Split(new char[] {','});
                if (cd.DataArray.Length >= 3) {
                    string reason = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);  
                    foreach (string nick in candidates) {
                        _IrcClient.RfcKick(channel, nick, reason);
                    }
                } else {
                    foreach (string nick in candidates) {
                        _IrcClient.RfcKick(channel, nick);
                    }
                }
            } else {
                _NotEnoughParameters(cd);
            }
        }

        public void CommandKickban(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            string channel = chat.ID;
            IrcUser ircuser;
            if (cd.DataArray.Length >= 2) {
                string[] candidates = cd.DataArray[1].Split(new char[] {','});
                if (cd.DataArray.Length >= 3) {
                    string reason = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);  
                    foreach (string nick in candidates) {
                        ircuser = _IrcClient.GetIrcUser(nick);
                        if (ircuser != null) {
                            _IrcClient.Ban(channel, "*!*" + ircuser.Ident + "@" + ircuser.Host);
                            _IrcClient.RfcKick(channel, nick, reason);
                        }
                    }
                } else {
                    foreach (string nick in candidates) {
                        ircuser = _IrcClient.GetIrcUser(nick);
                        if (ircuser != null) {
                            _IrcClient.Ban(channel, "*!*" + ircuser.Ident + "@" + ircuser.Host);
                            _IrcClient.RfcKick(channel, nick);
                        }
                    }
                }
            } else {
                _NotEnoughParameters(cd);
            }
        }

        public void CommandMode(CommandModel cd)
        {
            ChatModel chat = cd.Chat;
            if (cd.DataArray.Length >= 2) {
                // does cd.Chat cause a remoting call?
                if (chat.ChatType == ChatType.Group) {
                    string channel = chat.ID;
                    _IrcClient.RfcMode(channel, cd.Parameter);
                } else {
                    _IrcClient.RfcMode(_IrcClient.Nickname, cd.Parameter);
                }
            } else {
                if (chat.ChatType == ChatType.Group) {
                    Channel chan = _IrcClient.GetChannel(cd.Chat.ID);
                    cd.FrontendManager.AddTextToChat(cd.Chat, String.Format(
                                                                "-!- mode/{0} [{1}]",
                                                                chat.Name, chan.Mode));
                } else {
                    cd.FrontendManager.AddTextToChat(cd.Chat, String.Format(
                                                                "-!- Your user mode is [{0}]",
                                                                _IrcClient.Usermode));
                }
            }
        }

        public void CommandInvite(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            ChatModel chat = cd.Chat;
            string channel;
            if (cd.DataArray.Length >= 3) {
                channel = cd.DataArray[2];
            } else {
                channel = chat.ID;
            }
            if (cd.DataArray.Length >= 2) {
                if (!_IrcClient.IsJoined(channel, cd.DataArray[1])) {
                    _IrcClient.RfcInvite(cd.DataArray[1], channel);
                    fm.AddTextToChat(chat, "-!- " + String.Format(
                                                        _("Inviting {0} to {1}"),
                                                        cd.DataArray[1], channel));
                } else {
                    fm.AddTextToChat(chat, "-!- " + String.Format(
                                                        _("{0} is already on {1}"),
                                                        cd.DataArray[1], channel));
                }
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        public void CommandNames(CommandModel cd)
        {
            /*
            13:10 [Users #smuxi]
            13:10 [ CIA-5] [ d-best] [ meebey] [ meebey_] [ NotZh817] [ RAOF] 
            13:10 -!- Irssi: #smuxi: Total of 6 nicks [0 ops, 0 halfops, 0 voices, 6 normal]
            */
            
            FrontendManager fm = cd.FrontendManager;
            ChatModel chat = cd.Chat;
            if (!(chat is GroupChatModel)) {
                return;
            }
            GroupChatModel groupChat = (GroupChatModel) chat;
            
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText("[{0} {1}]", _("Users"), groupChat.Name);
            fm.AddMessageToChat(chat, builder.ToMessage());
            
            builder = CreateMessageBuilder();
            int opCount = 0;
            int voiceCount = 0;
            int normalCount = 0;
            builder.AppendEventPrefix();

            // sort nicklist
            var persons = groupChat.Persons;
            if (persons == null) {
                persons = new Dictionary<string, PersonModel>(0);
            }
            List<PersonModel> ircPersons = new List<PersonModel>(persons.Values);
            ircPersons.Sort((a, b) => (a.IdentityName.CompareTo(b.IdentityName)));
            builder.AppendText("[ ");
            foreach (IrcGroupPersonModel ircPerson in ircPersons) {
                string mode;
                if (ircPerson.IsOp) {
                    opCount++;
                    mode = "@";
                } else if (ircPerson.IsVoice) {
                    voiceCount++;
                    mode = "+";
                } else {
                    normalCount++;
                    mode = String.Empty;
                }
                if (!String.IsNullOrEmpty(mode)) {
                    builder.AppendText(mode);
                }
                builder.AppendNick(ircPerson);
                builder.AppendSpace();
            }
            builder.AppendText("]");
            fm.AddMessageToChat(chat, builder.ToMessage());

            builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText(
                String.Format(
                    _("Total of {0} users [{1} ops, {2} voices, {3} normal]"),
                    opCount + voiceCount + normalCount,
                    opCount,
                    voiceCount,
                    normalCount
                )
            );
            fm.AddMessageToChat(chat, builder.ToMessage());
        }

        public void CommandRaw(CommandModel cd)
        {
            _IrcClient.WriteLine(cd.Parameter);
        }
    
        public void CommandMe(CommandModel cd)
        {
            if (cd.DataArray.Length < 2) {
                _NotEnoughParameters(cd);
                return;
            }
            
            _IrcClient.SendMessage(SendType.Action, cd.Chat.ID, cd.Parameter);

            var builder = CreateMessageBuilder();
            builder.AppendActionPrefix();
            builder.AppendIdendityName(MyPerson);
            builder.AppendText(" ");
            builder.AppendMessage(cd.Parameter);
            Session.AddMessageToChat(cd.Chat, builder.ToMessage(), true);
        }
        
        public void CommandNotice(CommandModel cd)
        {
            if (cd.DataArray.Length >= 3) {
                string target = cd.DataArray[1];
                string message = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);  
                _IrcClient.SendMessage(SendType.Notice, target, message);
                
                ChatModel chat;
                if (_IrcClient.IsJoined(target)) {
                    chat = GetChat(target, ChatType.Group);
                } else {
                    // wasn't a channel but maybe a query
                    chat = GetChat(target, ChatType.Person);
                }
                if (chat == null) {
                    chat = _NetworkChat;
                }
                Session.AddTextToChat(chat, "[notice(" + target + ")] " +
                                      message, true);
            }
        }
        
        public void CommandNick(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                _IrcClient.RfcNick(cd.Parameter);
            }
        }
    
        public void CommandQuit(CommandModel cd)
        {
            Trace.Call(cd);
            
            string message = cd.Parameter;
            
            // else SmartIrc4net would reconnect us
            _IrcClient.AutoReconnect = false;
            // else the Listen() thread would try to connect again
            _Listening = false;
            
            // when we are disconnected, remove all chats
            _IrcClient.OnDisconnected += delegate {
                // cleanup all open chats
                Dispose();
            };
            
            // ok now we are ready to die
            if (message != null) {
                _IrcClient.RfcQuit(message);
            } else {
                _IrcClient.RfcQuit();
            }
        }
        
        private void _Run()
        {
            Trace.Call();
            
            try {
                Connect(_FrontendManager);
                
                while (_Listening) {
                    try {
                        _Listen();
#if LOG4NET
                        _Logger.Warn("_Run(): _Listen() returned.");
#endif
                    } catch (ThreadAbortException ex) {
                        throw;
                    } catch (Exception ex) {
#if LOG4NET
                        _Logger.Error("_Run(): exception in _Listen() occurred!" ,ex);
#endif
                        
                        Reconnect(_FrontendManager);
                    }
                    
                    // sleep for 10 seconds, we don't want to be abusive
                    System.Threading.Thread.Sleep(10000);
                }
            } catch (ThreadAbortException ex) {
#if LOG4NET
                _Logger.Debug("_Run(): thread aborted");
#endif
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
            }
            
            // don't need the FrontendManager anymore
            _FrontendManager = null;
        }
        
        private void _Listen()
        {
            try {
                _IrcClient.Listen();
            } catch (Exception ex) {
                Session.AddTextToChat(_NetworkChat, "-!- " + _("Connection error! Reason: ") + ex.Message);
                throw;
            }
        }
        
        private void _NotEnoughParameters(CommandModel cd)
        {
            cd.FrontendManager.AddTextToChat(
                cd.Chat,
                String.Format("-!- {0}",
                    String.Format(_("Not enough parameters for {0} command"),
                        cd.Command
                    )
                )
            );
        }
        
        private void _NotConnected(CommandModel cd)
        {
            cd.FrontendManager.AddTextToChat(
                cd.Chat, String.Format("-!- {0}", _("Not connected to server"))
            );
        }

        protected override bool ContainsHighlight (string msg)
        {
            Regex regex;
            // First check to see if our current nick is in there.
            regex = new Regex(String.Format("(^|\\W){0}($|\\W)",
                                            Regex.Escape(_IrcClient.Nickname)),
                              RegexOptions.IgnoreCase);
            if (regex.Match(msg).Success) {
                return true;
            } else {
                return base.ContainsHighlight(msg);
            }
        }

        private void ClearHighlights(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            foreach (MessagePartModel msgPart in msg.MessageParts) {
                if (!msgPart.IsHighlight || !(msgPart is TextMessagePartModel)) {
                    continue;
                }

                TextMessagePartModel textMsg = (TextMessagePartModel) msgPart;
                textMsg.IsHighlight = false;
                textMsg.ForegroundColor = null;
            }
        }

        private void MarkHighlights(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            bool containsHighlight = false;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
                if (!(msgPart is TextMessagePartModel)) {
                    continue;
                }

                TextMessagePartModel textMsg = (TextMessagePartModel) msgPart;
                if (String.IsNullOrEmpty(textMsg.Text)) {
                    // URLs without a link name don't have text
                    continue;
                }
                if (ContainsHighlight(textMsg.Text)) {
                    containsHighlight = true;
                }
            }

            if (!containsHighlight) {
                // nothing to do
                return;
            }

            // colorize the whole message
            var highlightColor = TextColor.Parse(
                (string) Session.UserConfig["Interface/Notebook/Tab/HighlightColor"]
            );
            foreach (MessagePartModel msgPart in msg.MessageParts) {
                if (!(msgPart is TextMessagePartModel)) {
                    continue;
                }

                TextMessagePartModel textMsg = (TextMessagePartModel) msgPart;
                if (textMsg.ForegroundColor != null &&
                    textMsg.ForegroundColor != TextColor.None) {
                    // HACK: don't overwrite colors as that would replace
                    // nick-colors for example
                    continue;
                }
                // HACK: we have to mark all parts as highlight else
                // ClearHighlights() has no chance to properly undo all
                // highlights
                textMsg.IsHighlight = true;
                textMsg.ForegroundColor = highlightColor;
            }
        }

        private void ApplyConfig(UserConfig config, ServerModel server)
        {
            _Host = server.Hostname;
            _Port = server.Port;
            if (String.IsNullOrEmpty(server.Network)) {
                _Network = server.Hostname;
            } else {
                _Network = server.Network;
            }
            if (String.IsNullOrEmpty(server.Username)) {
                _Username = (string) config["Connection/Username"];
            } else {
                _Username = server.Username;
            }
            _Password = server.Password;

            // internal fallbacks
            if (String.IsNullOrEmpty(_Username)) {
                _Username = "smuxi";
            }

            // IRC specific settings
            if (server is IrcServerModel) {
                var ircServer = (IrcServerModel) server;
                if (ircServer.Nicknames != null && ircServer.Nicknames.Count > 0) {
                    _Nicknames = ircServer.Nicknames.ToArray();
                }
            }

            // global fallbacks
            if (_Nicknames == null) {
                _Nicknames = (string[]) config["Connection/Nicknames"];
            }

            string encodingName = (string) config["Connection/Encoding"];
            if (String.IsNullOrEmpty(encodingName)) {
                _IrcClient.Encoding = Encoding.Default;
            } else {
                try {
                    _IrcClient.Encoding = Encoding.GetEncoding(encodingName);
                } catch (Exception ex) {
#if LOG4NET
                    _Logger.Warn("ApplyConfig(): Error getting encoding for: " +
                                 encodingName + " falling back to system encoding.", ex);
#endif
                    _IrcClient.Encoding = Encoding.Default;
                }
            }

            var proxySettings = new ProxySettings();
            proxySettings.ApplyConfig(config);
            var protocol = server.UseEncryption ? "ircs" : "irc";
            var serverUri = String.Format("{0}://{1}:{2}", protocol,
                                          server.Hostname, server.Port);
            var proxy = proxySettings.GetWebProxy(serverUri);
            if (proxy == null) {
                _IrcClient.ProxyType = IrcProxyType.None;
            } else {
                var proxyScheme = proxy.Address.Scheme;
                var ircProxyType = IrcProxyType.None;
                try {
                    // HACK: map proxy scheme to SmartIrc4net's ProxyType
                    ircProxyType = (IrcProxyType) Enum.Parse(
                        typeof(IrcProxyType), proxyScheme, true
                    );
                } catch (ArgumentException ex) {
#if LOG4NET
                    _Logger.Error("ApplyConfig(): Couldn't parse proxy type: " +
                                  proxyScheme, ex);
#endif
                }
                _IrcClient.ProxyType = ircProxyType;
                _IrcClient.ProxyHost = proxy.Address.Host;
                _IrcClient.ProxyPort = proxy.Address.Port;
                if (!String.IsNullOrEmpty(proxySettings.ProxyUsername)) {
                    _IrcClient.ProxyUsername = proxySettings.ProxyUsername;
                }
                if (!String.IsNullOrEmpty(proxySettings.ProxyPassword)) {
                    _IrcClient.ProxyPassword = proxySettings.ProxyPassword;
                }
            }

            if (server != null) {
                _IrcClient.UseSsl = server.UseEncryption;
                _IrcClient.ValidateServerCertificate = server.ValidateServerCertificate;
            }
        }

        private void _OnRawMessage(object sender, IrcEventArgs e)
        {
#if LOG4NET
            //_Logger.Debug("_OnRawMessage(): received: '" + e.Data.RawMessage + "'");
#endif
            bool handled = false;
            switch (e.Data.Type) {
                case ReceiveType.Who:
                case ReceiveType.List:
                case ReceiveType.Name:
                case ReceiveType.Login:
                case ReceiveType.Topic:
                case ReceiveType.BanList:
                case ReceiveType.ChannelMode:
                    // ignore
                    handled = true;
                    break;
            }

            if (e.Data.Message != null) {
                switch (e.Data.Type) {
                    case ReceiveType.Error:
                        _OnError(e);
                       handled = true;
                        break;
                    case ReceiveType.WhoIs:
                        _OnReceiveTypeWhois(e);
                       handled = true;
                        break;
                    case ReceiveType.WhoWas:
                        _OnReceiveTypeWhowas(e);
                        handled = true;
                        break;
                }
            }

            string chan;
            string nick;
            string msg;
            ChatModel chat;
            switch (e.Data.ReplyCode) {
                case ReplyCode.Null:
                case ReplyCode.Away: // already handled via _OnAway()
                case ReplyCode.NowAway: // already handled via _OnNowAway()
                case ReplyCode.UnAway: // already handled via _OnUnAway()
                case (ReplyCode) 329: // RPL_CREATIONTIME
                case (ReplyCode) 333: // RPL_TOPICWHOTIME: who set topic + timestamp
                    // ignore
                    break;
                case ReplyCode.Bounce: // RPL_ISUPPORT
                    // :friendly.landlord.eloxoph.com 005 meebey CHANTYPES=# PREFIX=(ohv)@%+ NETWORK=Eloxoph AWAYLEN=200 TOPICLEN=300 :are supported by this server
                    // :friendly.landlord.eloxoph.com 005 meebey CHANLIMIT=#:12 IRCD=WeIRCd NICKLEN=25 CASEMAPPING=ascii USERLEN=9 :are supported by this server
                    // :friendly.landlord.eloxoph.com 005 meebey CHANMODE=b,kl,,cimnOrst PENALTY MAXTARGETS=1 MAXBANS=50 MODES=5 LISTMODE=997 :are supported by this server
                    string line = String.Empty;
                    if (e.Data.RawMessageArray.Length >= 4) {
                        line = String.Join(
                            " ", e.Data.RawMessageArray, 3,
                            e.Data.RawMessageArray.Length - 3
                        );
                    }
                    string[] supportList = line.Split(' ');
                    foreach (string support in supportList) {
                        string supportKey = null;
                        string supportValue = null;
                        if (support.Contains("=")) {
                            supportKey = support.Split('=')[0];
                            supportValue = support.Split('=')[1];
                        } else {
                            supportKey = support;
                            supportValue = null;
                        }
                        switch (supportKey) {
                            case "NETWORK":
                                _Network = supportValue;
#if LOG4NET
                                _Logger.Debug(
                                    "_OnRawMessage(): detected IRC network: " +
                                    "'" + _Network + "'"
                                );
#endif
                                break;
                            case "ELIST":
                                HasListMaskSearchSupport = supportValue.Contains("M");
                                break;
                            case "SAFELIST":
                                HasSafeListSupport = true;
                                break;
                        }
                    }
                    break;
                case ReplyCode.ErrorNoSuchNickname:
                    nick = e.Data.RawMessageArray[3];
                    msg = "-!- " + String.Format(_("{0}: No such nick/channel"), nick);
                    chat = GetChat(nick, ChatType.Person);
                    if (chat != null) {
                        Session.AddTextToChat(chat, msg);
                    } else {
                        Session.AddTextToChat(_NetworkChat, msg);
                    }
                    break;
                case ReplyCode.ErrorChannelIsFull:
                case ReplyCode.ErrorInviteOnlyChannel:
                case ReplyCode.ErrorBadChannelKey:
                case ReplyCode.ErrorTooManyChannels:
                case ReplyCode.ErrorChannelOpPrivilegesNeeded:
                case ReplyCode.ErrorCannotSendToChannel:
                case ReplyCode.ErrorUnavailableResource:
                    chan = e.Data.RawMessageArray[3];
                    msg = "-!- " + chan + " " + e.Data.Message;
                    chat = GetChat(chan, ChatType.Group);
                    if (chat != null) {
                        Session.AddTextToChat(chat, msg);
                    } else {
                        Session.AddTextToChat(_NetworkChat, msg);
                    }

                    // if our own nick is temporarily not available then we
                    // need to deal this like an already used nick
                    if (chan == _IrcClient.Nickname) {
                        AutoRenick();
                    }
                    break;
                case ReplyCode.ErrorBannedFromChannel:
                    _OnErrorBannedFromChannel(e);
                    break;
                case ReplyCode.ErrorNicknameInUse:
                    _OnErrorNicknameInUse(e);
                    break;
                case ReplyCode.EndOfNames:
                    chan = e.Data.RawMessageArray[3];
                    GroupChatModel groupChat = (GroupChatModel)GetChat(
                       chan, ChatType.Group);
                    if (groupChat == null) {
                        break;
                    }
                    groupChat.IsSynced = true;
#if LOG4NET
                    _Logger.Debug("_OnRawMessage(): " + chan + " synced");
#endif
                    break;
                default:
                    if (!handled) {
                        var builder = CreateMessageBuilder();
                        builder.MessageType = MessageType.Event;

                        int replyCode = (int) e.Data.ReplyCode;
                        string numeric = String.Format("{0:000}", replyCode);
                        string constant;
                        if (Enum.IsDefined(typeof(ReplyCode), e.Data.ReplyCode)) {
                            constant = e.Data.ReplyCode.ToString();
                        } else {
                            constant = "?";
                        }

                        string parameters = String.Empty;
                        if (e.Data.RawMessageArray.Length >= 4) {
                            parameters = String.Join(
                                " ", e.Data.RawMessageArray, 3,
                                e.Data.RawMessageArray.Length - 3
                            );
                        }
                        int colonPosition = parameters.IndexOf(':');
                        if (colonPosition > 0) {
                            parameters = " " + parameters.Substring(0, colonPosition - 1);
                        } else {
                            parameters = String.Empty;
                        }

                        TextMessagePartModel msgPart;
                        msgPart = new TextMessagePartModel("[");
                        msgPart.ForegroundColor = IrcTextColor.Grey;
                        msgPart.Bold = true;
                        builder.AppendText(msgPart);

                        msgPart = new TextMessagePartModel(numeric);
                        if (replyCode >= 400 && replyCode <= 599) {
                            msgPart.ForegroundColor = new TextColor(255, 0, 0);
                        }
                        msgPart.Bold = true;
                        builder.AppendText(msgPart);

                        var response = String.Format(
                            " ({0}){1}",
                            constant,
                            parameters
                        );
                        builder.AppendText(response);

                        msgPart = new TextMessagePartModel("] ");
                        msgPart.ForegroundColor = IrcTextColor.Grey;
                        msgPart.Bold = true;
                        builder.AppendText(msgPart);

                        if (e.Data.Message != null) {
                            builder.MessageType = MessageType.Normal;
                            builder.AppendMessage(e.Data.Message);
                        }

                        Session.AddMessageToChat(_NetworkChat,
                                                 builder.ToMessage());
                    }
                    break;
            }
        }

        private void _OnError(IrcEventArgs e)
        {
            var builder = CreateMessageBuilder();
            var text = builder.CreateText(e.Data.Message);
            text.ForegroundColor = IrcTextColor.Red;
            text.Bold = true;
            text.IsHighlight = true;
            builder.AppendText(text);
            Session.AddMessageToChat(_NetworkChat, builder.ToMessage());

            if (e.Data.Message.ToLower().Contains("flood")) {
                _IrcClient.SendDelay += 250;

                Session.AddTextToChat(
                    _NetworkChat,
                    "-!- " + String.Format(
                         _("Increased send delay to {0}ms to avoid being " +
                           "flooded off the server again."),
                        _IrcClient.SendDelay
                    )
                );
            }
        }
        
        private void _OnErrorNicknameInUse(IrcEventArgs e)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            // TRANSLATOR: the final line will look like this:
            // -!- Nick {0} is already in use
            builder.AppendText(_("Nick"));
            builder.AppendSpace();

            var text = builder.CreateText(e.Data.RawMessageArray[3]);
            text.Bold = true;
            builder.AppendText(text);
            builder.AppendSpace();

            // TRANSLATOR: the final line will look like this:
            // -!- Nick {0} is already in use
            builder.AppendText(_("is already in use"));
            Session.AddMessageToChat(_NetworkChat, builder.ToMessage());

            AutoRenick();
        }
        
        private void _OnErrorBannedFromChannel(IrcEventArgs e)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText(_("Cannot join to channel:"));
            builder.AppendSpace();

            var text = builder.CreateText(e.Data.RawMessageArray[3]);
            text.Bold = true;
            builder.AppendText(text);
            builder.AppendSpace();

            builder.AppendText("({0})", _("You are banned"));
            Session.AddMessageToChat(_NetworkChat, builder.ToMessage());
        }
        
        private void _OnReceiveTypeWhois(IrcEventArgs e)
        {
            string nick = e.Data.RawMessageArray[3];
            ChatModel chat = GetChat(nick, ChatType.Person);
            if (chat == null) {
                chat = _NetworkChat;
            }
            switch (e.Data.ReplyCode) {
                case ReplyCode.WhoIsUser:
                    string ident = e.Data.RawMessageArray[4];
                    string host = e.Data.RawMessageArray[5];
                    string realname = e.Data.Message;
                    Session.AddTextToChat(chat, "-!- " + nick + " [" + ident + "@" + host + "]");
                    Session.AddTextToChat(chat, "-!-  realname: " + realname);
                    break;
                case ReplyCode.WhoIsServer:
                    string server = e.Data.RawMessageArray[4];
                    string serverinfo = e.Data.Message;
                    Session.AddTextToChat(chat, "-!-  server: " + server + " [" + serverinfo + "]");
                    break;
                case ReplyCode.WhoIsIdle:
                    string idle = e.Data.RawMessageArray[4];
                    try {
                        long timestamp = Int64.Parse(e.Data.RawMessageArray[5]);
                        DateTime signon =  new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        signon = signon.AddSeconds(timestamp).ToLocalTime();
                        Session.AddTextToChat(chat, "-!-  idle: "+idle+" [signon: "+signon.ToString()+"]");
                    } catch (FormatException) {
                    }
                    break;
                case ReplyCode.WhoIsChannels:
                    string channels = e.Data.Message;
                    Session.AddTextToChat(chat, "-!-  channels: " + channels);
                    break;
                case ReplyCode.WhoIsOperator:
                    Session.AddTextToChat(chat, "-!-  " + e.Data.Message);
                    break;
                case ReplyCode.EndOfWhoIs:
                    Session.AddTextToChat(chat, "-!-  " + e.Data.Message);
                    break;
            }
        }
        
        private void _OnReceiveTypeWhowas(IrcEventArgs e)
        {
            string nick = e.Data.RawMessageArray[3];
            ChatModel chat = GetChat(nick, ChatType.Person);
            if (chat == null) {
                chat = _NetworkChat;
            }
            switch (e.Data.ReplyCode) {
                case ReplyCode.WhoWasUser:
                    string ident = e.Data.RawMessageArray[4];
                    string host = e.Data.RawMessageArray[5];
                    string realname = e.Data.Message;
                    Session.AddTextToChat(chat, "-!- " + nick + " [" + ident + "@" + host + "]");
                    Session.AddTextToChat(chat, "-!-  realname: " + realname);
                    break;
                case ReplyCode.EndOfWhoWas:
                    Session.AddTextToChat(chat, "-!-  " + e.Data.Message);
                    break;
            }
        }
        
        private void _OnCtcpRequest(object sender, CtcpEventArgs e)
        {
            Session.AddTextToChat(_NetworkChat,
                String.Format(
                    // TRANSLATOR: {0}: nickname, {1}: ident@host,
                    // {2}: CTCP command, {3}: own nickname, {4}: CTCP parameter
                    // example:
                    // meebey [meebey@example.com] requested CTCP VERSION from meebey:
                    _("{0} [{1}] requested CTCP {2} from {3}: {4}"),
                    e.Data.Nick, e.Data.Ident+"@"+e.Data.Host,
                    e.CtcpCommand, _IrcClient.Nickname,
                    e.CtcpParameter
                )
            );
        }

        private void _OnCtcpReply(object sender, CtcpEventArgs e)
        {
            ChatModel chat = GetChat(e.Data);
            if (chat == null) {
                chat = _NetworkChat;
            }

            if (e.CtcpCommand == "PING") {
                try {
                    long timestamp = Int64.Parse(e.CtcpParameter);
                    if (!(timestamp >= 0)) {
                        return;
                    }
                    DateTime sent = DateTime.FromFileTime(timestamp);
                    string duration = DateTime.Now.Subtract(sent).TotalSeconds.ToString();

                    Session.AddTextToChat(chat, String.Format(
                                                    _("CTCP PING reply from {0}: {1} seconds"),
                                                    e.Data.Nick, duration));


                } catch (FormatException) {
                }
            } else {
                Session.AddTextToChat(chat, String.Format(
                                            _("CTCP {0} reply from {1}: {2}"),
                                            e.CtcpCommand, e.Data.Nick, e.CtcpParameter));
            }
        }

        private void _OnChannelMessage(object sender, IrcEventArgs e)
        {
            ChatModel chat = GetChat(e.Data.Channel, ChatType.Group) ?? _NetworkChat;

            var builder = CreateMessageBuilder();
            builder.AppendMessage(GetPerson(chat, e.Data.Nick), e.Data.Message);

            var msg = builder.ToMessage();
            MarkHighlights(msg);
            Session.AddMessageToChat(chat, msg);
        }
        
        private void _OnChannelAction(object sender, ActionEventArgs e)
        {
            ChatModel chat = GetChat(e.Data.Channel, ChatType.Group);

            var builder = CreateMessageBuilder();
            builder.AppendActionPrefix();
            builder.AppendIdendityName(GetPerson(chat, e.Data.Nick));
            builder.AppendText(" ");
            builder.AppendMessage(e.ActionMessage);
            
            var msg = builder.ToMessage();
            MarkHighlights(msg);
            Session.AddMessageToChat(chat, msg);
        }
        
        private void _OnChannelNotice(object sender, IrcEventArgs e)
        {
            ChatModel chat = GetChat(e.Data.Channel, ChatType.Group);

            var builder = CreateMessageBuilder();
            builder.AppendText("-{0}:{1}- ", e.Data.Nick, e.Data.Channel);
            builder.AppendMessage(e.Data.Message);

            var msg = builder.ToMessage();
            MarkHighlights(msg);
            Session.AddMessageToChat(chat, msg);
        }
        
        private void _OnQueryMessage(object sender, IrcEventArgs e)
        {
            var chat = (PersonChatModel) GetChat(e.Data.Nick, ChatType.Person);
            bool newChat = false;
            if (chat == null) {
                var person = CreatePerson(e.Data.Nick);
                person.Ident = e.Data.Ident;
                person.Host = e.Data.Host;
                chat = Session.CreatePersonChat(person, e.Data.Nick,
                                                e.Data.Nick, this);
                newChat = true;
            }

            var builder = CreateMessageBuilder();
            builder.AppendSenderPrefix(chat.Person, true);
            builder.AppendMessage(e.Data.Message);
            var msg = builder.ToMessage();

            if (newChat) {
                // don't create chats for filtered messages
                if (Session.IsFilteredMessage(chat, msg)) {
                    Session.LogMessage(chat, msg, true);
                    return;
                }
                Session.AddChat(chat);
                Session.SyncChat(chat);
            }

            Session.AddMessageToChat(chat, msg);
        }
        
        private void _OnQueryAction(object sender, ActionEventArgs e)
        {
            var chat = (PersonChatModel) GetChat(e.Data.Nick, ChatType.Person);
            bool newChat = false;
            if (chat == null) {
                var person = CreatePerson(e.Data.Nick);
                person.Ident = e.Data.Ident;
                person.Host = e.Data.Host;
                chat = Session.CreatePersonChat(person, e.Data.Nick,
                                                e.Data.Nick, this);
                newChat = true;
            }

            var builder = CreateMessageBuilder();
            builder.AppendActionPrefix();
            builder.AppendIdendityName(chat.Person, true);
            builder.AppendSpace();
            builder.AppendMessage(e.ActionMessage);
            var msg = builder.ToMessage();
            MarkHighlights(msg);

            if (newChat) {
                // don't create chats for filtered messages
                if (Session.IsFilteredMessage(chat, msg)) {
                    Session.LogMessage(chat, msg, true);
                    return;
                }
                Session.AddChat(chat);
                Session.SyncChat(chat);
            }

            Session.AddMessageToChat(chat, msg);
        }
        
        private void _OnQueryNotice(object sender, IrcEventArgs e)
        {
            var targetChats = new List<ChatModel>();
            if (e.Data.Nick != null) {
                var chat = (PersonChatModel) GetChat(e.Data.Nick, ChatType.Person);
                if (chat != null) {
                    targetChats.Add(chat);
                }
            }
            if (targetChats.Count == 0 && e.Data.Nick != null) {
                // always show on server chat
                targetChats.Add(_NetworkChat);
                // check if we share a channel with the sender
                lock (Chats) {
                    foreach (var chat in Chats) {
                        if (!(chat is GroupChatModel)) {
                            continue;
                        }
                        var groupChat = (GroupChatModel) chat;
                        if (groupChat.Persons == null) {
                            continue;
                        }
                        if (groupChat.Persons.ContainsKey(e.Data.Nick)) {
                            targetChats.Add(groupChat);
                        }
                    }
                }
            }
            if (targetChats.Count == 0) {
                // use server chat as fallback
                targetChats.Add(_NetworkChat);
            }

            var builder = CreateMessageBuilder();
            if (e.Data.Nick == null) {
                // server message
                builder.AppendText("!{0} ", e.Data.From);
            } else {
                builder.AppendText("-");
                builder.AppendIdendityName(GetPerson(targetChats[0],
                                                     e.Data.Nick));
                builder.AppendText(" ({0}@{1})- ", e.Data.Ident, e.Data.Host);
            }
            builder.AppendMessage(e.Data.Message);
            var msg = builder.ToMessage();
            MarkHighlights(msg);

            foreach (var targetChat in targetChats) {
                Session.AddMessageToChat(targetChat, msg);
            }
        }

        private void _OnJoin(object sender, JoinEventArgs e)
        {
            GroupChatModel groupChat = (GroupChatModel) GetChat(e.Channel, ChatType.Group);
            if (e.Data.Irc.IsMe(e.Who)) {
                // tell join handlers, that they need to wait!!
                lock (_ActiveChannelJoinList) {
                    _ActiveChannelJoinList.Add(e.Channel.ToLower());
                }

                if (groupChat == null) {
                    groupChat = Session.CreateChat<GroupChatModel>(
                        e.Channel, e.Channel, this
                    );
                    groupChat.UnsafePersonsComparer = StringComparer.OrdinalIgnoreCase;
                    Session.AddChat(groupChat);
                } else {
                    // chat still exists, so we we only need to enable it
                    // (sync is done in _OnChannelActiveSynced)
                    Session.EnableChat(groupChat);
                }
            } else {
                // someone else joined, let's add him to the channel chat
                // HACK: some buggy networks might send JOIN messages for users
                // that are already on the channel
                if (groupChat.UnsafePersons.ContainsKey(e.Who.ToLower())) {
#if LOG4NET
                   _Logger.Error("_OnJoin(): groupChat.UnsafePerson contains " +
                                  "already: '" + e.Who + "', ignoring...");
#endif
                    // ignore
                } else {
                    IrcUser siuser = _IrcClient.GetIrcUser(e.Who);
                    var icuser = CreateGroupPerson(e.Who);
                    icuser.Ident = siuser.Ident;
                    icuser.Host = siuser.Host;
                    groupChat.UnsafePersons.Add(icuser.NickName.ToLower(), icuser);
                    Session.AddPersonToGroupChat(groupChat, icuser);
                }
            }

            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendIdendityName(GetPerson(groupChat, e.Who));
            // TRANSLATOR: do NOT change the position of {0}!
            builder.AppendText(_("{0} [{1}] has joined {2}"),
                               String.Empty,
                               String.Format("{0}@{1}", e.Data.Ident, e.Data.Host),
                               e.Channel);

            var msg = builder.ToMessage();
            Session.AddMessageToChat(groupChat, msg);
        }
        
        private void _OnNames(object sender, NamesEventArgs e)
        {
#if LOG4NET
            // logging noise
            //_Logger.Debug("_OnNames() e.Channel: " + e.Channel);
#endif
            GroupChatModel groupChat = (GroupChatModel) GetChat(e.Data.Channel, ChatType.Group);
            if (groupChat != null && groupChat.IsSynced) {
                // nothing todo for us
                return;
            }

            // would be nice if SmartIrc4net would take care of removing prefixes
            foreach (string user in e.UserList) {
                // skip empty users (some IRC servers send an extra space)
                if (user.TrimEnd(' ').Length == 0) {
                    continue;
                }
                string username = user;
                
                switch (user[0]) {
                    case '@':
                    case '+':
                    // RFC VIOLATION
                    // some IRC network do this and break our nice smuxi...
                    case '&':
                    case '%':
                    case '~':
                        username = user.Substring(1);
                        break;
                }
                
                var groupPerson = CreateGroupPerson(username);
                
                groupChat.UnsafePersons.Add(groupPerson.NickName.ToLower(), groupPerson);
#if LOG4NET
                // logging noise
                //_Logger.Debug("_OnNames() added user: " + username + " to: " + groupChat.Name);
#endif
            }
        }
        
        private void _OnChannelActiveSynced(object sender, IrcEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnChannelActiveSynced() e.Data.Channel: " + e.Data.Channel);
#endif

            lock (_ActiveChannelJoinList) {
                _ActiveChannelJoinList.Remove(e.Data.Channel.ToLower());
            }
            // tell the currently waiting join task item from the task queue
            // that one channel is finished
            _ActiveChannelJoinHandle.Set();

            GroupChatModel groupChat = (GroupChatModel) GetChat(e.Data.Channel, ChatType.Group);
            if (groupChat == null) {
#if LOG4NET
                _Logger.Error("_OnChannelActiveSynced(): GetChat(" + e.Data.Channel + ", ChatType.Group) returned null!");
#endif
                return;
            }

            Channel channel = _IrcClient.GetChannel(e.Data.Channel);
            foreach (ChannelUser channelUser in channel.Users.Values) {
                IrcGroupPersonModel groupPerson = (IrcGroupPersonModel) groupChat.GetPerson(channelUser.Nick);
                if (groupPerson == null) {
                    // we should not get here anymore, _OnNames creates the users already
#if LOG4NET
                    _Logger.Error("_OnChannelActiveSynced(): groupChat.GetPerson(" + channelUser.Nick + ") returned null!");
#endif
                    continue;
                }
                
                groupPerson.RealName = channelUser.Realname;
                groupPerson.Ident    = channelUser.Ident;
                groupPerson.Host     = channelUser.Host;
                groupPerson.IsOp     = channelUser.IsOp;
                groupPerson.IsVoice  = channelUser.IsVoice;
            }

            // prime-time
            Session.SyncChat(groupChat);
        }
        
        private void _OnPart(object sender, PartEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnPart() e.Channel: "+e.Channel+" e.Who: "+e.Who);
#endif
            GroupChatModel groupChat = (GroupChatModel) GetChat(e.Channel, ChatType.Group);
            // only remove the chat if it was enabled, that way we can retain
            // the message buffer
            if (e.Data.Irc.IsMe(e.Who)) {
                if (groupChat.IsEnabled) {
                    Session.RemoveChat(groupChat);
                }
                // nothing else we can do
                return;
            }
            
            PersonModel person = groupChat.GetPerson(e.Who);
            if (person == null) {
#if LOG4NET
                // HACK: some buggy networks might send PART messages for users
                // that are not on the channel
                _Logger.Error("_OnPart(): groupChat.GetPerson(" + e.Who + ") returned null!");
#endif
            } else {
                Session.RemovePersonFromGroupChat(groupChat, person);
            }

            var builder = CreateMessageBuilder();
            builder.MessageType = MessageType.Event;
            builder.AppendEventPrefix();
            builder.AppendIdendityName(GetPerson(groupChat, e.Who));
            // TRANSLATOR: do NOT change the position of {0}!
            builder.AppendText(_("{0} [{1}] has left {2}"),
                               String.Empty,
                               String.Format("{0}@{1}", e.Data.Ident, e.Data.Host),
                               e.Channel);

            if (!String.IsNullOrEmpty(e.PartMessage)) {
                builder.AppendText("[");
                // colors in part messages are annoying
                builder.StripColors = true;
                builder.AppendMessage(e.PartMessage);
                builder.AppendText("]");
            }

            Session.AddMessageToChat(groupChat, builder.ToMessage());
        }
        
        private void _OnKick(object sender, KickEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnKick() e.Channel: "+e.Channel+" e.Whom: "+e.Whom);
#endif
            var chat = (GroupChatModel) GetChat(e.Channel, ChatType.Group);
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            if (e.Data.Irc.IsMe(e.Whom)) {
                // TRANSLATOR: do NOT change the position of {1}!
                builder.AppendText(_("You were kicked from {0} by {1}"),
                                   e.Channel, String.Empty);
                builder.AppendIdendityName(GetPerson(chat, e.Who));
                builder.AppendText(" [").AppendMessage(e.KickReason).AppendText("]");
                Session.AddMessageToChat(chat, builder.ToMessage());
                Session.DisableChat(chat);
            } else {
                PersonModel user = chat.GetPerson(e.Whom);
                Session.RemovePersonFromGroupChat(chat, user);
                builder.AppendIdendityName(GetPerson(chat, e.Whom));
                // TRANSLATOR: do NOT change the position of {0} and {2}!
                builder.AppendText(_("{0} was kicked from {1} by {2}"),
                                   String.Empty, e.Channel, String.Empty);
                builder.AppendIdendityName(GetPerson(chat, e.Who));
                builder.AppendText(" [").AppendMessage(e.KickReason).AppendText("]");
                Session.AddMessageToChat(chat, builder.ToMessage());
            }
        }
        
        private void _OnNickChange(object sender, NickChangeEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnNickChange() e.OldNickname: "+e.OldNickname+" e.NewNickname: "+e.NewNickname);
#endif
            if (e.Data.Irc.IsMe(e.NewNickname)) {
                _MyPerson = CreatePerson(e.NewNickname, MyPerson.RealName,
                                         MyPerson.Ident, MyPerson.Host);

                var builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                // TRANSLATOR: do NOT change the position of {0}!
                builder.AppendText(_("You're now known as {0}"),
                                  String.Empty);
                builder.AppendIdendityName(CreatePerson(e.NewNickname));

                Session.AddMessageToChat(_NetworkChat, builder.ToMessage());
            }
            
            IrcUser ircuser = e.Data.Irc.GetIrcUser(e.NewNickname);
            if (ircuser != null) {
                foreach (string channel in ircuser.JoinedChannels) {
                    GroupChatModel cchat = (GroupChatModel)GetChat(channel, ChatType.Group);
                    
                    // clone the old user to a new user
                    IrcGroupPersonModel olduser = (IrcGroupPersonModel) cchat.GetPerson(e.OldNickname);
                    if (olduser == null) {
#if LOG4NET
                        _Logger.Error("cchat.GetPerson(e.OldNickname) returned null! cchat.Name: "+cchat.Name+" e.OldNickname: "+e.OldNickname);
#endif
                        continue;
                    }
                    var newuser = CreateGroupPerson(e.NewNickname);
                    newuser.RealName = olduser.RealName;
                    newuser.Ident = olduser.Ident;
                    newuser.Host = olduser.Host;
                    newuser.IsOp = olduser.IsOp;
                    newuser.IsVoice = olduser.IsVoice;
                    
                    Session.UpdatePersonInGroupChat(cchat, olduser, newuser);
                    
                    var builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    if (e.Data.Irc.IsMe(e.NewNickname)) {
                        // TRANSLATOR: do NOT change the position of {0}!
                        builder.AppendText(_("You're now known as {0}"),
                                           String.Empty);
                    } else {
                        builder.AppendIdendityName(olduser);
                        // TRANSLATOR: do NOT change the position of {0} or {1}!
                        builder.AppendText(_("{0} is now known as {1}"),
                                           String.Empty,
                                           String.Empty);
                    }
                    builder.AppendIdendityName(newuser);
                    Session.AddMessageToChat(cchat, builder.ToMessage());
                }
            }
        }
        
        private void _OnTopic(object sender, TopicEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)GetChat(e.Channel, ChatType.Group);
            var topic = CreateMessageBuilder();
            topic.AppendMessage(e.Topic);
            Session.UpdateTopicInGroupChat(cchat, topic.ToMessage());
        }
        
        private void _OnTopicChange(object sender, TopicChangeEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)GetChat(e.Channel, ChatType.Group);
            var builder = CreateMessageBuilder();
            builder.AppendMessage(e.NewTopic);
            Session.UpdateTopicInGroupChat(cchat, builder.ToMessage());

            builder = CreateMessageBuilder();
            builder.AppendEventPrefix();

            string who;
            if (String.IsNullOrEmpty(e.Who)) {
                // server changed topic
                builder.AppendText(e.Data.From);
            } else {
                builder.AppendIdendityName(GetPerson(cchat, e.Who));
            }

            // TRANSLATOR: do NOT change the position of {0} and {2}!
            builder.AppendText(_("{0} changed the topic of {1} to: {2}"),
                             String.Empty, e.Channel, String.Empty);
            builder.AppendMessage(e.NewTopic);
            Session.AddMessageToChat(cchat, builder.ToMessage());
        }
        
        private void _OnOp(object sender, OpEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)GetChat(e.Channel, ChatType.Group);
            IrcGroupPersonModel user = (IrcGroupPersonModel)cchat.GetPerson(e.Whom);
            if (user != null) {
                user.IsOp = true;
                Session.UpdatePersonInGroupChat(cchat, user, user);
#if LOG4NET
            } else {
                _Logger.Error("_OnOp(): cchat.GetPerson(e.Whom) returned null! cchat.Name: "+cchat.Name+" e.Whom: "+e.Whom);
#endif
            }
        }
        
        private void _OnDeop(object sender, DeopEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)GetChat(e.Channel, ChatType.Group);
            IrcGroupPersonModel user = (IrcGroupPersonModel)cchat.GetPerson(e.Whom);
            if (user != null) {
                user.IsOp = false;
                Session.UpdatePersonInGroupChat(cchat, user, user);
#if LOG4NET
            } else {
                _Logger.Error("_OnDeop(): cchat.GetPerson(e.Whom) returned null! cchat.Name: "+cchat.Name+" e.Whom: "+e.Whom);
#endif
            }
        }
        
        private void _OnVoice(object sender, VoiceEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)GetChat(e.Channel, ChatType.Group);
            IrcGroupPersonModel user = (IrcGroupPersonModel)cchat.GetPerson(e.Whom);
            if (user != null) {
                user.IsVoice = true;
                Session.UpdatePersonInGroupChat(cchat, user, user);
#if LOG4NET
            } else {
                _Logger.Error("cchat.GetPerson(e.Whom) returned null! cchat.Name: "+cchat.Name+" e.Whom: "+e.Whom);
#endif
            }
        }
        
        private void _OnDevoice(object sender, DevoiceEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)GetChat(e.Channel, ChatType.Group);
            IrcGroupPersonModel user = (IrcGroupPersonModel)cchat.GetPerson(e.Whom);
            if (user != null) {
                user.IsVoice = false;
                Session.UpdatePersonInGroupChat(cchat, user, user);
#if LOG4NET
            } else {
                _Logger.Error("cchat.GetPerson(e.Whom) returned null! cchat.Name: "+cchat.Name+" e.Whom: "+e.Whom);
#endif
            }
        }
        
        private void _OnModeChange(object sender, IrcEventArgs e)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();

            string modechange;
            string who = null;
            ChatModel target = null;
            switch (e.Data.Type) {
                case ReceiveType.UserModeChange:
                    modechange = e.Data.Message;
                    who = e.Data.Irc.Nickname;
                    target = _NetworkChat;

                    // TRANSLATOR: do NOT change the position of {1}!
                    builder.AppendText(_("Mode change [{0}] for user {1}"),
                                       modechange, String.Empty);
                    builder.AppendIdendityName(CreatePerson(who));
                    break;
                case ReceiveType.ChannelModeChange:
                    modechange = String.Join(" ", e.Data.RawMessageArray, 3,
                                             e.Data.RawMessageArray.Length - 3);
                    target = GetChat(e.Data.Channel, ChatType.Group);

                    // TRANSLATOR: do NOT change the position of {2}!
                    builder.AppendText(_("mode/{0} [{1}] by {2}"),
                                       e.Data.Channel, modechange, String.Empty);

                    if (e.Data.Nick != null && e.Data.Nick.Length > 0) {
                        who = e.Data.Nick;
                        builder.AppendIdendityName(GetPerson(target, who));
                    } else {
                        // server changed mode
                        who = e.Data.From;
                        builder.AppendText(who);
                    }
                    break;
            }

            if (target == null) {
#if LOG4NET
                 _Logger.Error("_OnModeChange(): target is null!");
#endif
                return;
            }

            Session.AddMessageToChat(target, builder.ToMessage());
        }
        
        private void _OnQuit(object sender, QuitEventArgs e)
        {
#if LOG4NET
            // logging noise
            //_Logger.Debug("_Quit() e.Who: "+e.Who);
#endif
            if (e.Data.Irc.IsMe(e.Who)) {
                // _OnDisconnect() handles this
            } else {
                var builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendIdendityName(CreatePerson(e.Who));
                // TRANSLATOR: do NOT change the position of {0}!
                builder.AppendText(_("{0} [{1}] has quit"),
                                   String.Empty,
                                   String.Format("{0}@{1}",
                                                 e.Data.Ident, e.Data.Host));
                builder.AppendText(" [");
                // colors are annoying in quit messages
                builder.StripColors = true;
                builder.AppendMessage(e.QuitMessage);
                builder.AppendText("]");
                var quitMsg = builder.ToMessage();
                foreach (ChatModel chat in Chats) {
                    if (chat.ChatType == ChatType.Group) {
                        GroupChatModel cchat = (GroupChatModel)chat;
                        PersonModel user = cchat.GetPerson(e.Who);
                        if (user != null) {
                            // he is on this channel, let's remove him
                            Session.RemovePersonFromGroupChat(cchat, user);
                            Session.AddMessageToChat(cchat, quitMsg);
                        }
                    } else if ((chat.ChatType == ChatType.Person) &&
                               (chat.ID == e.Who)) {
                        Session.AddMessageToChat(chat, quitMsg);
                    }
                }
            }
        }
        
        private void _OnRegistered(object sender, EventArgs e)
        {
            OnConnected(EventArgs.Empty);

            // preliminary person
            _MyPerson = CreatePerson(_IrcClient.Nickname);

            // WHO ourself so OnWho() can retrieve our ident, host and realname
            _IrcClient.RfcWho(_IrcClient.Nickname);
        }

        protected override void OnConnected(EventArgs e)
        {
            foreach (ChatModel chat in Chats) {
                // re-enable all person chats
                if (chat.ChatType == ChatType.Person) {
                    Session.EnableChat(chat);
                    // and re-sync them else new messages are not processed in
                    // the FrontendManager
                    Session.SyncChat(chat);
                }
                // group chats are handled in _OnJoin()
            }

            base.OnConnected(e);
        }
        
        private void _OnDisconnected(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            // reset join queue
            lock (_ActiveChannelJoinList) {
                _ActiveChannelJoinList.Clear();
            }
            lock (_QueuedChannelJoinList) {
                _QueuedChannelJoinList.Clear();
            }
            _ChannelJoinQueue.Reset(true);
            _ActiveChannelJoinHandle.Reset();

            OnDisconnected(EventArgs.Empty);
        }

        private void OnAutoConnectError(object sender, AutoConnectErrorEventArgs e)
        {
            Trace.Call(sender, e);

            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText(_("Connection to {0} port {1} has failed " +
                                 "(attempt {2}), retrying in {3} seconds..."),
                               e.Address, e.Port, _IrcClient.AutoRetryAttempt,
                               _IrcClient.AutoRetryDelay);
            Session.AddMessageToChat(_NetworkChat, builder.ToMessage());
        }

        protected override void OnDisconnected(EventArgs e)
        {
            foreach (ChatModel chat in Chats) {
                // don't disable the protocol chat, else the user loses all
                // control for the protocol manager! e.g. after a manual
                // reconnect or server-side disconnect
                if (chat.ChatType == ChatType.Protocol) {
                    continue;
                }

                Session.DisableChat(chat);
            }

            // reset the nickname list, so if we connect again we will start
            // using the best nickname again
            _CurrentNickname = 0;

            base.OnDisconnected(e);
        }
        
        private void _OnAway(object sender, AwayEventArgs e)
        {
            ChatModel chat = GetChat(e.Who, ChatType.Person);

            if (chat == null) {
                chat = _NetworkChat;
            } else {
                PersonModel person = ((PersonChatModel) chat).Person;
                IrcPersonModel ircperson = (IrcPersonModel) person;

                if (ircperson.AwayMessage != e.AwayMessage) {
                    ircperson.AwayMessage = e.AwayMessage;
                    ircperson.IsAwaySeen = false;
                    ircperson.IsAway = true;
                }

                if (ircperson.IsAwaySeen) {
                    return;
                }
                ircperson.IsAwaySeen = true;
            }
            Session.AddTextToChat(chat, "-!- " + String.Format(
                                                    _("{0} is away: {1}"),
                                                    e.Who, e.AwayMessage));
        }

        private void _OnUnAway(object sender, IrcEventArgs e)
        {
            Session.AddTextToChat(_NetworkChat, "-!- " + _("You are no longer marked as being away"));
            Session.UpdateNetworkStatus();
        }
        
        private void _OnNowAway(object sender, IrcEventArgs e)
        {
            Session.AddTextToChat(_NetworkChat, "-!- " + _("You have been marked as being away"));
            Session.UpdateNetworkStatus();
        }
        
        private void _LagWatcher()
        {
            try {
                while (true) {
                    // check every 10 seconds
                    Thread.Sleep(10000);
                    
                    if (_IrcClient == null ||
                        !_IrcClient.IsConnected) {
                        // nothing to do
                        continue;
                    }
                    
                    TimeSpan lag = _IrcClient.Lag;
                    TimeSpan diff = lag - _LastLag;
                    int absDiff = Math.Abs((int) diff.TotalSeconds);
                    // update network status if the lag changed over 5 seconds
                    if (absDiff > 5) {
#if LOG4NET
                        _Logger.Debug("_LagWatcher(): lag: " + lag.TotalSeconds + " seconds, difference: " + absDiff + " seconds");
#endif
                        Session.UpdateNetworkStatus();
                    }
                    _LastLag = lag;
                }
            } catch (ThreadAbortException ex) {
#if LOG4NET
                _Logger.Debug("_LagWatcher(): thread aborted");
#endif
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
            }
        }

        private int GetProtocolMessageLength(string command,
                                             string target,
                                             string message)
        {
            // :<prefix> <command> <target> :<message><crlf>
            return 1 + Prefix.Length + 1 +
                   command.Length + 1 +
                   target.Length + 2 +
                   _IrcClient.Encoding.GetByteCount(message) + 2;
        }

        private IrcPersonModel GetPerson(ChatModel chat, string nick)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (nick == null) {
                throw new ArgumentNullException("nick");
            }

            IrcPersonModel person = null;
            if (chat is GroupChatModel) {
                var groupChat = (GroupChatModel) chat;
                person = (IrcPersonModel) groupChat.GetPerson(nick);
            } else if (chat is PersonChatModel) {
                var personChat = (PersonChatModel) chat;
                if (nick == personChat.Person.ID) {
                    person = (IrcPersonModel) personChat.Person;
                } else if (nick == MyPerson.ID) {
                    person = MyPerson;
                }
            }

            if (person == null) {
#if LOG4NET
                _Logger.Warn("GetPerson(" + chat + ", " + nick + "): person is null!");
#endif
                person = CreatePerson(nick);
            }

            return person;
        }

        private IrcPersonModel CreatePerson(string nick)
        {
            return CreatePerson(nick, null, null, null);
        }

        private IrcPersonModel CreatePerson(string nick, string realname,
                                            string ident, string host)
        {
            var person = new IrcPersonModel(nick, realname,ident, host,
                                            NetworkID, this);
            if (_IrcClient.IsMe(nick)) {
                person.IdentityNameColored.ForegroundColor = IrcTextColor.Blue;
                person.IdentityNameColored.BackgroundColor = TextColor.None;
                person.IdentityNameColored.Bold = true;
            }
            return person;
        }

        private IrcGroupPersonModel CreateGroupPerson(string nick)
        {
            var person = new IrcGroupPersonModel(nick, NetworkID, this);
            if (_IrcClient.IsMe(nick)) {
                person.IdentityNameColored.ForegroundColor = IrcTextColor.Blue;
                person.IdentityNameColored.BackgroundColor = TextColor.None;
                person.IdentityNameColored.Bold = true;
            }
            return person;
        }

        private ChatModel GetChat(IrcMessageData msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            if (msg.Channel != null) {
                // group chat message
                return GetChat(msg.Channel, ChatType.Group);
            }
            if (msg.Nick != null) {
                // person chat message
                return GetChat(msg.Nick, ChatType.Person);
            }
            if (msg.From != null) {
                // server message
                return _NetworkChat;
            }

            return null;
        }

        protected override MessageBuilder CreateMessageBuilder()
        {
            var builder = new IrcMessageBuilder();
            builder.ApplyConfig(Session.UserConfig);
            return builder;
        }

        void AutoRenick()
        {
            if (_IrcClient.AutoNickHandling ||
                _IrcClient.IsRegistered) {
                return;
            }

            // allright, we have to care then and try a different nick as
            // we don't have a nick yet
            string nick;
            if (_CurrentNickname == _Nicknames.Length - 1) {
                // we tried all nicks already, so fallback to random
                 Random rand = new Random();
                int number = rand.Next(999);
                nick = _Nicknames[_CurrentNickname].Substring(0, 5) + number;
            } else {
                _CurrentNickname++;
                nick = _Nicknames[_CurrentNickname];
            }

            _IrcClient.RfcNick(nick, Priority.Critical);
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
        
        public static string NormalizeNick(string nickname)
        {
            string normalized = nickname;

            normalized = normalized.ToLower();
            normalized = normalized.Replace("[", "{");
            normalized = normalized.Replace("]", "}");
            normalized = normalized.Replace("\\", "|");
            normalized = normalized.Replace("~", "^");

            return normalized;
        }
        
        public static bool CompareNicks(string a, string b)
        {
            return NormalizeNick(a) == NormalizeNick(b);
        }
    }
}
