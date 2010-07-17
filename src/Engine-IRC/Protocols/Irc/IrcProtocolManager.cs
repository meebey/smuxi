/*
 * $Id: IrcProtocolManager.cs 149 2007-04-11 16:47:52Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/IrcProtocolManager.cs $
 * $Rev: 149 $
 * $Author: meebey $
 * $Date: 2007-04-11 18:47:52 +0200 (Wed, 11 Apr 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2009 Mirco Bauer <meebey@meebey.net>
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
using System.Globalization;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Meebey.SmartIrc4net;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public enum IrcControlCode : int
    {
        Bold      = 2,
        Color     = 3,
        Clear     = 15,
        Italic    = 26,
        Underline = 31,
    }

    [ProtocolManagerInfo(Name = "IRC", Description = "Internet Relay Chat", Alias = "irc")]
    public class IrcProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string       _LibraryTextDomain = "smuxi-engine-irc";
        private static char[]   _IrcControlChars;
        private IrcClient       _IrcClient;
        private string          _Host;
        private int             _Port;
        private string          _Network;
        private string[]        _Nicknames;
        private int             _CurrentNickname;
        private string          _Username;
        private string          _Password;
        private string          _Ident;
        private string          _ClientHost;
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

                return String.Format("{0}!{1}@{2}", _IrcClient.Nickname,
                                     _Ident, _ClientHost);
            }
        }

        static IrcProtocolManager()
        {
            int[] intValues = (int[])Enum.GetValues(typeof(IrcControlCode));
            char[] chars = new char[intValues.Length];
            int i = 0;
            foreach (int intValue in intValues) {
                chars[i++] = (char)intValue;
            }
            _IrcControlChars = chars;
        }
        
        public IrcProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);
            
            _IrcClient = new IrcClient();
            _IrcClient.AutoRetry = true;
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
            _IrcClient.OnAway           += new AwayEventHandler(_OnAway);
            _IrcClient.OnUnAway         += new IrcEventHandler(_OnUnAway);
            _IrcClient.OnNowAway        += new IrcEventHandler(_OnNowAway);
            _IrcClient.OnCtcpRequest    += new CtcpEventHandler(_OnCtcpRequest);
            _IrcClient.OnCtcpReply      += new CtcpEventHandler(_OnCtcpReply);
            _IrcClient.OnWho            += OnWho;
        }

        private void OnWho(object sender, WhoEventArgs e)
        {
            if (e.WhoInfo.Nick == _IrcClient.Nickname) {
                // that's me!
                _Ident = e.WhoInfo.Ident;
                _ClientHost = e.WhoInfo.Host;
            }
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

        public override void Connect(FrontendManager fm, string server, int port, string user, string pass)
        {
            Trace.Call(fm, server, port, user, pass);

            string[] nicks = (string[]) Session.UserConfig["Connection/Nicknames"];
            Connect(fm, server, port, nicks, user, pass);
        }
        
        public void Connect(FrontendManager fm, string server, int port, string[] nicks, string user, string pass)
        {
            Trace.Call(fm, server, port, nicks, user, pass);
            
            _FrontendManager = fm;
            _Host = server;
            _Port = port;
            _Nicknames = nicks;
            _Username = user;
            _Password = pass;

            // add fallbacks if only one nick was specified, else we get random
            // number nicks when nick collisions happen
            if (_Nicknames.Length == 1) {
                _Nicknames = new string[] { _Nicknames[0], _Nicknames[0] + "_", _Nicknames[0] + "__" };
            }

            ApplyConfig(Session.UserConfig);

            // TODO: use config for single network chat or once per network manager
            _NetworkChat = new ProtocolChatModel(NetworkID, "IRC " + server, this);
            
            // BUG: race condition when we use Session.AddChat() as it pushes this already
            // to the connected frontend and the frontend will sync and get the page 2 times!
            //Session.Chats.Add(_NetworkChat);
            // NOTABUG: the frontend manager needs to take care for that
            Session.AddChat(_NetworkChat);
            Session.SyncChat(_NetworkChat);

            _RunThread = new Thread(new ThreadStart(_Run));
            _RunThread.IsBackground = true;
            _RunThread.Name = "IrcProtocolManager ("+server+":"+port+") listener";
            _RunThread.Start();
            
            _LagWatcherThread = new Thread(new ThreadStart(_LagWatcher));
            _LagWatcherThread.Name = "IrcProtocolManager ("+server+":"+port+") lag watcher";
            _LagWatcherThread.Start();
        }

        public void Connect(FrontendManager fm)
        {
            Trace.Call(fm);
            
            try {
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
                        ApplyConfig(Session.UserConfig);
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
            
            string channel = null;
            if (filter != null) {
                if (!filter.Name.StartsWith("*") && !filter.Name.EndsWith("*")) {
                    channel = String.Format("*{0}*", filter.Name);
                } else {
                    channel = filter.Name;
                }
            }
            
            IList<ChannelInfo> infos = _IrcClient.GetChannelList(channel);
            List<GroupChatModel> chats = new List<GroupChatModel>(infos.Count);
            foreach (ChannelInfo info in infos) {
                GroupChatModel chat = new GroupChatModel(
                    info.Channel,
                    info.Channel,
                    null
                );
                chat.PersonCount = info.UserCount;

                MessageModel topic = new MessageModel();
                _IrcMessageToMessageModel(ref topic, info.Topic);
                chat.Topic = topic;

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

        public override void CloseChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);

            if (fm == null) {
                throw new ArgumentNullException("chat");
            }
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }

            // get real chat object from session
            chat = Session.GetChat(chat.ID, chat.ChatType, this);
            if (chat == null) {
#if LOG4NET
                _Logger.Error("CloseChat(): Session.GetChat(" + chat.ID + ", " + chat.ChatType + ", this) return null!");
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
            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;

            fmsgti = new TextMessagePartModel();
            // TRANSLATOR: this line is used as label / category for a
            // list of commands below
            fmsgti.Text = "[" + _("IrcProtocolManager Commands") + "]";
            fmsgti.Bold = true;
            fmsg.MessageParts.Add(fmsgti);
            
            cd.FrontendManager.AddMessageToChat(cd.Chat, fmsg);
            
            string[] help = {
            "help",
            "connect irc server port [password] [nicknames]",
            "say",
            "join/j channel(s) [key]",
            "part/p [channel(s)] [part-message]",
            "topic [new-topic]",
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
                cd.FrontendManager.AddTextToChat(cd.Chat, "-!- " + line);
            }
        }
        
        public void CommandConnect(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            
            string server;
            if (cd.DataArray.Length >= 3) {
                server = cd.DataArray[2];
            } else {
                server = "localhost";
            }
            
            int port;
            if (cd.DataArray.Length >= 4) {
                try {
                    port = Int32.Parse(cd.DataArray[3]);
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
                port = 6667;
            }
            
            string pass;                
            if (cd.DataArray.Length >=5) {
                pass = cd.DataArray[4];
            } else {
                pass = null;
            }
            
            string[] nicks;
            if (cd.DataArray.Length >= 6) {
                nicks = new string[] {cd.DataArray[5]};
            } else {
                nicks = (string[])Session.UserConfig["Connection/Nicknames"];
            }
            
            string username = (string)Session.UserConfig["Connection/Username"];
            
            Connect(fm, server, port, nicks, username, pass);
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

            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;

            _IrcClient.SendMessage(SendType.Message, chat.ID, message);

            msgPart = new TextMessagePartModel();
            msgPart.Text = "<";
            msg.MessageParts.Add(msgPart);
        
            msgPart = new TextMessagePartModel();
            msgPart.Text = _IrcClient.Nickname;
            msgPart.ForegroundColor = GetNickColor(_IrcClient.Nickname);
            msg.MessageParts.Add(msgPart);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = "> ";
            msg.MessageParts.Add(msgPart);

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
                    msgPart = new TextMessagePartModel();
                    msgPart.Text = m.Value;
                    msgPart.ForegroundColor = GetNickColor(nick);
                    msg.MessageParts.Add(msgPart);
                }
            }
            _IrcMessageToMessageModel(ref msg, message);
            // HACK: clear possible highlights so we can't highlight ourself!
            ClearHighlights(msg);

            Session.AddMessageToChat(chat, msg, true);
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
                    IrcPersonModel person = new IrcPersonModel(nickname,
                                                               NetworkID,
                                                               this);
                    chat = new PersonChatModel(person, nickname, nickname, this);
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
                    if (lastWordPos != -1) {
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
                _NotEnoughParameters(cd);
            }
        }
        
        public void CommandWhoWas(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                _IrcClient.RfcWhowas(cd.DataArray[1]);
            } else {
                _NotEnoughParameters(cd);
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
                        MessageModel msg = new MessageModel();
                        TextMessagePartModel textMsg;
                   
                        textMsg = new TextMessagePartModel();
                        // For translators: do NOT change the position of {1}!
                        textMsg.Text = "-!- " + String.Format(_("Topic for {0}: {1}"), channel, String.Empty);
                        msg.MessageParts.Add(textMsg);  

                        _IrcMessageToMessageModel(ref msg, topic);
                        // clear possible highlights in topic
                        ClearHighlights(msg);

                        fm.AddMessageToChat(chat, msg);
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
                foreach (string nick in candidates) {
                    _IrcClient.Op(channel, nick);
                }
                /*
                // requires SmartIrc4net >= 0.4.6
                _IrcClient.Op(channel, candidates);
                */
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
                foreach(string nick in candidates) {
                    _IrcClient.Deop(channel, nick);
                }
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
                foreach (string nick in candidates) {
                    _IrcClient.Voice(channel, nick);
                }
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
                foreach (string nick in candidates) {
                    _IrcClient.Devoice(channel, nick);
                }
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
                foreach (string nick in candidates) {
                    _IrcClient.Ban(channel, nick);
                }
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
                foreach (string nick in candidates) {
                    _IrcClient.Unban(channel, nick);
                }
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
            
            MessageModel msg;
            TextMessagePartModel textMsg;
            
            msg = new MessageModel();
            textMsg = new TextMessagePartModel();
            textMsg.Text = String.Format("-!- [{0} {1}]", _("Users"), groupChat.Name); 
            msg.MessageParts.Add(textMsg);
            fm.AddMessageToChat(chat, msg);
            
            int opCount = 0;
            int voiceCount = 0;
            int normalCount = 0;
            msg = new MessageModel();
            textMsg = new TextMessagePartModel();
            textMsg.Text = "-!- ";
            msg.MessageParts.Add(textMsg);

            // sort nicklist
            var persons = groupChat.Persons;
            if (persons == null) {
                persons = new Dictionary<string, PersonModel>(0);
            }
            List<PersonModel> ircPersons = new List<PersonModel>(persons.Values);
            ircPersons.Sort((a, b) => (a.IdentityName.CompareTo(b.IdentityName)));
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
                    mode = " ";
                }
                textMsg = new TextMessagePartModel();
                textMsg.Text = String.Format("[{0}", mode);
                msg.MessageParts.Add(textMsg);

                textMsg = new TextMessagePartModel();
                textMsg.Text = ircPerson.NickName;
                textMsg.ForegroundColor = GetNickColor(ircPerson.NickName);
                msg.MessageParts.Add(textMsg);

                textMsg = new TextMessagePartModel();
                textMsg.Text = "] ";
                msg.MessageParts.Add(textMsg);
            }
            fm.AddMessageToChat(chat, msg);

            msg = new MessageModel();
            textMsg = new TextMessagePartModel();
            textMsg.Text = String.Format(
                "-!- {0}",
                String.Format(
                    _("Total of {0} users [{1} ops, {2} voices, {3} normal]"),
                    opCount + voiceCount + normalCount,
                    opCount,
                    voiceCount,
                    normalCount
                )
            );
            msg.MessageParts.Add(textMsg);
            fm.AddMessageToChat(chat, msg);
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
            
            MessageModel msg = new MessageModel();
            TextMessagePartModel textMsg;
        
            textMsg = new TextMessagePartModel();
            textMsg.Text = " * ";
            msg.MessageParts.Add(textMsg);

            textMsg = new TextMessagePartModel();
            textMsg.Text = _IrcClient.Nickname + " ";
            textMsg.ForegroundColor = GetNickColor(_IrcClient.Nickname);
            msg.MessageParts.Add(textMsg);
            
            _IrcMessageToMessageModel(ref msg, cd.Parameter);
            // HACK: clear possible highlights so we can't highlight ourself!
            ClearHighlights(msg);

            Session.AddMessageToChat(cd.Chat, msg, true);
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
        
        private void _IrcMessageToMessageModel(ref MessageModel msg, string message)
        {
            Trace.Call(msg, message);
            
            // strip color and formatting if configured
            if ((bool)Session.UserConfig["Interface/Notebook/StripColors"]) {
                message = Regex.Replace(message, (char)IrcControlCode.Color +
                            "[0-9]{1,2}(,[0-9]{1,2})?", String.Empty);
            }
            if ((bool)Session.UserConfig["Interface/Notebook/StripFormattings"]) {
                message = Regex.Replace(message, String.Format("({0}|{1}|{2}|{3})",
                                                    (char)IrcControlCode.Bold,
                                                    (char)IrcControlCode.Clear,
                                                    (char)IrcControlCode.Italic,
                                                    (char)IrcControlCode.Underline), String.Empty);
            }

            // convert * / _ to mIRC control characters
            string[] messageParts = message.Split(new char[] {' '});
            // better regex? \*([^ *]+)\*
            //string pattern = @"^({0})([A-Za-z0-9]+?){0}$";
            string pattern = @"^({0})([^ *]+){0}$";
            for (int i = 0; i < messageParts.Length; i++) {
                messageParts[i] = Regex.Replace(messageParts[i], String.Format(pattern, @"\*"), (char)IrcControlCode.Bold      + "$1$2$1" + (char)IrcControlCode.Bold);
                messageParts[i] = Regex.Replace(messageParts[i], String.Format(pattern,  "_"),  (char)IrcControlCode.Underline + "$1$2$1" + (char)IrcControlCode.Underline);
                messageParts[i] = Regex.Replace(messageParts[i], String.Format(pattern,  "/"),  (char)IrcControlCode.Italic    + "$1$2$1" + (char)IrcControlCode.Italic);
            }
            message = String.Join(" ", messageParts);
            
            // crash: ^C^C0,7Dj Ler #Dj KanaL?na Girmek ZorunDaD?rLar UnutMay?N @>'^C0,4WwW.MaViGuL.NeT ^C4]^O ^C4]'
            // parse colors
            bool bold = false;
            bool underline = false;
            bool italic = false;
            bool color = false;
            TextColor fg_color = IrcTextColor.Normal;
            TextColor bg_color = IrcTextColor.Normal;
            bool controlCharFound;
            do {
                string submessage;
                int controlPos = message.IndexOfAny(_IrcControlChars);
                if (controlPos > 0) {
                    // control char found and we have normal text infront
                    controlCharFound = true;
                    submessage = message.Substring(0, controlPos);
                    message = message.Substring(controlPos);
                } else if (controlPos != -1) {
                    // control char found
                    controlCharFound = true;
                    
                    char controlChar = message.Substring(controlPos, 1)[0];
                    IrcControlCode controlCode = (IrcControlCode)controlChar;
                    string controlChars = controlChar.ToString();
                    switch (controlCode) {
                        case IrcControlCode.Clear:
#if LOG4NET
                            _Logger.Debug("_IrcMessageToMessageModel(): found clear control character");
#endif
                            bold = false;
                            underline = false;
                            italic = false;
                            
                            color = false;
                            fg_color = IrcTextColor.Normal;
                            bg_color = IrcTextColor.Normal;
                            break;
                        case IrcControlCode.Bold:
#if LOG4NET
                            _Logger.Debug("_IrcMessageToMessageModel(): found bold control character");
#endif
                            bold = !bold;
                            break;
                        case IrcControlCode.Underline:
#if LOG4NET
                            _Logger.Debug("_IrcMessageToMessageModel(): found underline control character");
#endif
                            underline = !underline;
                            break;
                        case IrcControlCode.Italic:
#if LOG4NET
                            _Logger.Debug("_IrcMessageToMessageModel(): found italic control character");
#endif
                            italic = !italic;
                            break;
                        case IrcControlCode.Color:
#if LOG4NET
                            _Logger.Debug("_IrcMessageToMessageModel(): found color control character");
#endif
                            color = !color;
                            string colorMessage = message.Substring(controlPos);
#if LOG4NET
                            _Logger.Debug("_IrcMessageToMessageModel(): colorMessage: '" + colorMessage + "'");
#endif
                            Match match = Regex.Match(colorMessage, (char)IrcControlCode.Color + "(?<fg>[0-9][0-9]?)(,(?<bg>[0-9][0-9]?))?");
                            if (match.Success) {
                                controlChars = match.Value;
                                int color_code;
                                if (match.Groups["fg"] != null) {
#if LOG4NET
                                    _Logger.Debug("_IrcMessageToMessageModel(): match.Groups[fg].Value: " + match.Groups["fg"].Value);
#endif
                                    try {
                                        color_code = Int32.Parse(match.Groups["fg"].Value);
                                        fg_color = _IrcTextColorToTextColor(color_code);
                                    } catch (FormatException) {
                                        fg_color = IrcTextColor.Normal;
                                    }
                                }
                                if (match.Groups["bg"] != null) {
#if LOG4NET
                                    _Logger.Debug("_IrcMessageToMessageModel(): match.Groups[bg].Value: " + match.Groups["bg"].Value);
#endif
                                    try {
                                        color_code = Int32.Parse(match.Groups["bg"].Value);
                                        bg_color = _IrcTextColorToTextColor(color_code);
                                    } catch (FormatException) {
                                        bg_color = IrcTextColor.Normal;
                                    }
                                }
                            } else {
                                controlChars = controlChar.ToString();
                                fg_color = IrcTextColor.Normal;
                                bg_color = IrcTextColor.Normal;
                            }
#if LOG4NET
                            _Logger.Debug("_IrcMessageToMessageModel(): fg_color.HexCode: " + String.Format("0x{0:X6}", fg_color.HexCode));
                            _Logger.Debug("_IrcMessageToMessageModel(): bg_color.HexCode: " + String.Format("0x{0:X6}", bg_color.HexCode));
#endif
                            break;
                    }
#if LOG4NET
                    _Logger.Debug("_IrcMessageToMessageModel(): controlChars.Length: " + controlChars.Length);
#endif

                    // check if there are more control chars in the rest of the message
                    int nextControlPos = message.IndexOfAny(_IrcControlChars, controlPos + controlChars.Length);
                    if (nextControlPos != -1) {
                        // more control chars found
                        submessage = message.Substring(controlChars.Length, nextControlPos - controlChars.Length);
                        message = message.Substring(nextControlPos);
                    } else {
                        // no next control char
                        // skip the control chars
                        submessage = message.Substring(controlChars.Length);
                        message = String.Empty;
                    }
                } else {
                    // no control char, nothing to do
                    controlCharFound = false;
                    submessage = message;
                }
                
                TextMessagePartModel msgPart = new TextMessagePartModel();
                msgPart.Text = submessage;
                msgPart.Bold = bold;
                msgPart.Underline = underline;
                msgPart.Italic = italic;
                msgPart.ForegroundColor = fg_color;
                msgPart.BackgroundColor = bg_color;
                msg.MessageParts.Add(msgPart);
            } while (controlCharFound);

            MarkHighlights(msg);

            // parse URLs
            ParseUrls(msg);
        }
        
        protected override bool ContainsHighlight (string msg)
        {
            Regex regex;
            // First check to see if our current nick is in there.
            regex = new Regex(String.Format("(^|\\W){0}($|\\W)", _IrcClient.Nickname), RegexOptions.IgnoreCase);
            if (regex.Match(msg).Success) {
                return true;
            } else {
                return base.ContainsHighlight(msg);
            }
        }

        private TextColor _IrcTextColorToTextColor(int color)
        {
            switch (color) {
                case 0:
                    return IrcTextColor.White;
                case 1:
                    return IrcTextColor.Black;
                case 2:
                    return IrcTextColor.Blue;
                case 3:
                    return IrcTextColor.Green;
                case 4:
                    return IrcTextColor.Red;
                case 5:
                    return IrcTextColor.Brown;
                case 6:
                    return IrcTextColor.Purple;
                case 7:
                    return IrcTextColor.Orange;
                case 8:
                    return IrcTextColor.Yellow;
                case 9:
                    return IrcTextColor.LightGreen;
                case 10:
                    return IrcTextColor.Teal;
                case 11:
                    return IrcTextColor.LightCyan;
                case 12:
                    return IrcTextColor.LightBlue;
                case 13:
                    return IrcTextColor.LightPurple;
                case 14:
                    return IrcTextColor.Grey;
                case 15:
                    return IrcTextColor.LightGrey;
                default:
                    return IrcTextColor.Normal;
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

        private void ApplyConfig(UserConfig config)
        {
            if (String.IsNullOrEmpty(_Username)) {
                _Username = (string) config["Connection/Username"];
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
        }

        private void _OnRawMessage(object sender, IrcEventArgs e)
        {
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
                        if (support.StartsWith("NETWORK=")) {
                            _Network = support.Split('=')[1];
#if LOG4NET
                            _Logger.Debug("_OnRawMessage(): detected IRC network: '" + _Network + "'");
#endif
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
                        MessageModel fmsg = new MessageModel();
                        fmsg.MessageType = MessageType.Event;

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
                        fmsg.MessageParts.Add(msgPart);

                        msgPart = new TextMessagePartModel(numeric);
                        if (replyCode >= 400 && replyCode <= 599) {
                            msgPart.ForegroundColor = new TextColor(255, 0, 0);
                        }
                        msgPart.Bold = true;
                        fmsg.MessageParts.Add(msgPart);

                        var response = String.Format(
                            " ({0}){1}",
                            constant,
                            parameters
                        );
                        msgPart = new TextMessagePartModel(response);
                        fmsg.MessageParts.Add(msgPart);

                        msgPart = new TextMessagePartModel("] ");
                        msgPart.ForegroundColor = IrcTextColor.Grey;
                        msgPart.Bold = true;
                        fmsg.MessageParts.Add(msgPart);

                        if (e.Data.Message != null) {
                            fmsg.MessageType = MessageType.Normal;
                            _IrcMessageToMessageModel(ref fmsg, e.Data.Message);
                        }

                        Session.AddMessageToChat(_NetworkChat, fmsg);
                    }
                    break;
            }
        }

        private void _OnError(IrcEventArgs e)
        {
            MessageModel msg = new MessageModel();
            TextMessagePartModel textMsg;

            textMsg = new TextMessagePartModel();
            textMsg.Text = e.Data.Message;
            textMsg.ForegroundColor = IrcTextColor.Red;
            textMsg.Bold = true;
            textMsg.IsHighlight = true;
            msg.MessageParts.Add(textMsg);
            Session.AddMessageToChat(_NetworkChat, msg);

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
            MessageModel msg = new MessageModel();
            TextMessagePartModel textMsg;

            textMsg = new TextMessagePartModel();
            // TRANSLATOR: the final line will look like this:
            // -!- Nick {0} is already in use
            textMsg.Text = "-!- " + _("Nick") + " ";
            msg.MessageParts.Add(textMsg);

            textMsg = new TextMessagePartModel();
            textMsg.Text = e.Data.RawMessageArray[3];
            textMsg.Bold = true;
            msg.MessageParts.Add(textMsg);

            textMsg = new TextMessagePartModel();
            // TRANSLATOR: the final line will look like this:
            // -!- Nick {0} is already in use
            textMsg.Text = " " + _("is already in use");
            msg.MessageParts.Add(textMsg);

            Session.AddMessageToChat(_NetworkChat, msg);

            if (!_IrcClient.AutoNickHandling &&
                !_IrcClient.IsRegistered) {
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
        }
        
        private void _OnErrorBannedFromChannel(IrcEventArgs e)
        {
            MessageModel msg = new MessageModel();
            TextMessagePartModel textMsg;
            
            textMsg = new TextMessagePartModel();
            textMsg.Text = "-!- " + _("Cannot join to channel:") + " ";
            msg.MessageParts.Add(textMsg);

            textMsg = new TextMessagePartModel();
            textMsg.Text = e.Data.RawMessageArray[3];
            textMsg.Bold = true;
            msg.MessageParts.Add(textMsg);

            textMsg = new TextMessagePartModel();
            textMsg.Text = " (" + _("You are banned") + ")";
            msg.MessageParts.Add(textMsg);

            Session.AddMessageToChat(_NetworkChat, msg);
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
            // DoS protection
            try {
                switch (e.CtcpCommand.ToLower()) {
                    case "time":
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
                        break;
                    case "finger":
                    case "userinfo":
                        _IrcClient.SendMessage(
                            SendType.CtcpReply, e.Data.Nick,
                            String.Format("{0} {1}",
                                e.CtcpCommand,
                                (string) Session.UserConfig["Connection/Realname"]
                            )
                        );
                        break;
                }
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error("_OnCtcpRequest()", ex);
#endif
            }

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
            ChatModel chat = GetChat(e.Data.Nick, ChatType.Person);
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
        
        protected TextColor GetNickColor(string nickname)
        {
            if (nickname == null) {
                throw new ArgumentNullException("nickname");
            }

            if (_IrcClient.IsMe(nickname)) {
                return IrcTextColor.Blue;
            }
            
            return GetIdentityNameColor(NormalizeNick(nickname.TrimEnd('_')));
        }
        
        private void _OnChannelMessage(object sender, IrcEventArgs e)
        {
            ChatModel chat = GetChat(e.Data.Channel, ChatType.Group);

            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            fmsgti = new TextMessagePartModel();
            fmsgti.Text = "<";
            fmsg.MessageParts.Add(fmsgti);

            fmsgti = new TextMessagePartModel();
            fmsgti.ForegroundColor = GetNickColor(e.Data.Nick);
            fmsgti.Text = e.Data.Nick;
            fmsg.MessageParts.Add(fmsgti);

            fmsgti = new TextMessagePartModel();
            fmsgti.Text = "> ";
            fmsg.MessageParts.Add(fmsgti);
            
            _IrcMessageToMessageModel(ref fmsg, e.Data.Message);
            
            Session.AddMessageToChat(chat, fmsg);
        }
        
        private void _OnChannelAction(object sender, ActionEventArgs e)
        {
            ChatModel chat = GetChat(e.Data.Channel, ChatType.Group);

            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            fmsgti = new TextMessagePartModel();
            fmsgti.Text = " * ";
            fmsg.MessageParts.Add(fmsgti);
            
            fmsgti = new TextMessagePartModel();
            fmsgti.ForegroundColor = GetNickColor(e.Data.Nick);
            fmsgti.Text = e.Data.Nick + " ";
            fmsg.MessageParts.Add(fmsgti);
            
            _IrcMessageToMessageModel(ref fmsg, e.ActionMessage);
            
            Session.AddMessageToChat(chat, fmsg);
        }
        
        private void _OnChannelNotice(object sender, IrcEventArgs e)
        {
            ChatModel chat = GetChat(e.Data.Channel, ChatType.Group);

            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            fmsgti = new TextMessagePartModel();
            fmsgti.Text = String.Format("-{0}:{1}- ", e.Data.Nick, e.Data.Channel);
            fmsg.MessageParts.Add(fmsgti);
            
            _IrcMessageToMessageModel(ref fmsg, e.Data.Message);
            
            Session.AddMessageToChat(chat, fmsg);
        }
        
        private void _OnQueryMessage(object sender, IrcEventArgs e)
        {
            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;

            msgPart = new TextMessagePartModel();
            msgPart.Text = String.Format("<{0}> ", e.Data.Nick);
            msgPart.IsHighlight = true;
            msg.MessageParts.Add(msgPart);

            _IrcMessageToMessageModel(ref msg, e.Data.Message);

            ChatModel chat = GetChat(e.Data.Nick, ChatType.Person);
            if (chat == null) {
                IrcPersonModel person = new IrcPersonModel(e.Data.Nick,
                                                           null,
                                                           e.Data.Ident,
                                                           e.Data.Host,
                                                           NetworkID,
                                                           this);
                chat = new PersonChatModel(person, e.Data.Nick, e.Data.Nick, this);
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
            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;

            msgPart = new TextMessagePartModel();
            msgPart.Text = String.Format(" * {0} ", e.Data.Nick);
            msgPart.IsHighlight = true;
            msg.MessageParts.Add(msgPart);

            _IrcMessageToMessageModel(ref msg, e.ActionMessage);

            ChatModel chat = GetChat(e.Data.Nick, ChatType.Person);
            if (chat == null) {
                IrcPersonModel person = new IrcPersonModel(e.Data.Nick,
                                                           null,
                                                           e.Data.Ident,
                                                           e.Data.Host,
                                                           NetworkID,
                                                           this);
                chat = new PersonChatModel(person, e.Data.Nick, e.Data.Nick, this);
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
            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;

            msgPart = new TextMessagePartModel();
            msgPart.Text = String.Format("-{0} ({1}@{2})- ",
                                        e.Data.Nick,
                                        e.Data.Ident,
                                        e.Data.Host);
            // notice shouldn't be a highlight
            //fmsgti.IsHighlight = true;
            msg.MessageParts.Add(msgPart);

            _IrcMessageToMessageModel(ref msg, e.Data.Message);

            ChatModel chat = null;
            if (e.Data.Nick != null) {
                chat = GetChat(e.Data.Nick, ChatType.Person);
            }
            if (chat == null) {
                // use server chat as fallback
                if (e.Data.Nick == null) {
                    // this seems to be a notice from the server
                    chat = _NetworkChat;
                } else {
                    // create new chat
                    IrcPersonModel person = new IrcPersonModel(e.Data.Nick,
                                                               null,
                                                               e.Data.Ident,
                                                               e.Data.Host,
                                                               NetworkID,
                                                               this);
                    chat = new PersonChatModel(person, e.Data.Nick, e.Data.Nick, this);
                    // don't create chats for filtered messages
                    if (Session.IsFilteredMessage(chat, msg)) {
                        Session.LogMessage(chat, msg, true);
                        return;
                    }
                    Session.AddChat(chat);
                    Session.SyncChat(chat);
                }
            }

            Session.AddMessageToChat(chat, msg);
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
                    groupChat = new GroupChatModel(e.Channel, e.Channel, this);
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
                    IrcGroupPersonModel icuser = new IrcGroupPersonModel(e.Who,
                                                                         NetworkID,
                                                                         this);
                    icuser.Ident = siuser.Ident;
                    icuser.Host = siuser.Host;
                    groupChat.UnsafePersons.Add(icuser.NickName.ToLower(), icuser);
                    Session.AddPersonToGroupChat(groupChat, icuser);
                }
            }

            MessageModel msg = new MessageModel();
            msg.MessageType = MessageType.Event;
            TextMessagePartModel textMsgPart;
            
            textMsgPart = new TextMessagePartModel();
            textMsgPart.Text = "-!- ";
            msg.MessageParts.Add(textMsgPart);
            
            textMsgPart = new TextMessagePartModel();
            textMsgPart.ForegroundColor = GetNickColor(e.Data.Nick);
            textMsgPart.Text = e.Who;
            msg.MessageParts.Add(textMsgPart);

            textMsgPart = new TextMessagePartModel();
            // For translators: do NOT change the position of {0}!
            textMsgPart.Text = String.Format(_("{0} [{1}] has joined {2}"),
                                             String.Empty,
                                             e.Data.Ident + "@" + e.Data.Host,
                                             e.Channel);
            msg.MessageParts.Add(textMsgPart);
            
            Session.AddMessageToChat(groupChat, msg);
        }
        
        private void _OnNames(object sender, NamesEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnNames() e.Channel: " + e.Channel);
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
                
                IrcGroupPersonModel groupPerson = new IrcGroupPersonModel(username,
                                                                     NetworkID,
                                                                     this);
                
                groupChat.UnsafePersons.Add(groupPerson.NickName.ToLower(), groupPerson);
#if LOG4NET
                _Logger.Debug("_OnNames() added user: " + username + " to: " + groupChat.Name);
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

            MessageModel msg = new MessageModel();
            msg.MessageType = MessageType.Event;
            TextMessagePartModel textMsgPart;
            
            textMsgPart = new TextMessagePartModel();
            textMsgPart.Text = "-!- ";
            msg.MessageParts.Add(textMsgPart);
            
            textMsgPart = new TextMessagePartModel();
            textMsgPart.ForegroundColor = GetNickColor(e.Data.Nick);
            textMsgPart.Text = e.Who;
            msg.MessageParts.Add(textMsgPart);

            textMsgPart = new TextMessagePartModel();
            textMsgPart.Text = String.Format(
                                    _("{0} [{1}] has left {2} [{3}]"),
                                    String.Empty,
                                    e.Data.Ident + "@" + e.Data.Host,
                                    e.Channel,
                                    e.PartMessage);
            msg.MessageParts.Add(textMsgPart);
            
            Session.AddMessageToChat(groupChat, msg);
        }
        
        private void _OnKick(object sender, KickEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnKick() e.Channel: "+e.Channel+" e.Whom: "+e.Whom);
#endif
            GroupChatModel cchat = (GroupChatModel) GetChat(e.Channel, ChatType.Group);
            if (e.Data.Irc.IsMe(e.Whom)) {
                Session.AddTextToChat(cchat,
                    "-!- " + String.Format(
                                _("You were kicked from {0} by {1} [{2}]"),
                                e.Channel, e.Who, e.KickReason));
                Session.DisableChat(cchat);
            } else {
                PersonModel user = cchat.GetPerson(e.Whom);
                Session.RemovePersonFromGroupChat(cchat, user);
                Session.AddTextToChat(cchat,
                    "-!- " + String.Format(
                                _("{0} was kicked from {1} by {2} [{3}]"),
                                e.Whom, e.Channel, e.Who, e.KickReason));
            }
        }
        
        private void _OnNickChange(object sender, NickChangeEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnNickChange() e.OldNickname: "+e.OldNickname+" e.NewNickname: "+e.NewNickname);
#endif
            if (e.Data.Irc.IsMe(e.NewNickname)) {
                MessageModel msg = new MessageModel();
                msg.MessageType = MessageType.Event;
                TextMessagePartModel textMsg;
                
                textMsg = new TextMessagePartModel();
                // For translators: do NOT change the position of {0}!
                textMsg.Text = "-!- " + String.Format(
                                            _("You're now known as {0}"),
                                            String.Empty);
                msg.MessageParts.Add(textMsg);

                textMsg = new TextMessagePartModel();
                textMsg.Text = e.NewNickname;
                textMsg.Bold = true;
                textMsg.ForegroundColor = GetNickColor(e.NewNickname);
                msg.MessageParts.Add(textMsg);

                Session.AddMessageToChat(_NetworkChat, msg);
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
                    IrcGroupPersonModel newuser = new IrcGroupPersonModel(
                                                        e.NewNickname,
                                                        NetworkID,
                                                        this);
                    newuser.RealName = olduser.RealName;
                    newuser.Ident = olduser.Ident;
                    newuser.Host = olduser.Host;
                    newuser.IsOp = olduser.IsOp;
                    newuser.IsVoice = olduser.IsVoice;
                    
                    Session.UpdatePersonInGroupChat(cchat, olduser, newuser);
                    
                    if (e.Data.Irc.IsMe(e.NewNickname)) {
                        MessageModel msg = new MessageModel();
                        msg.MessageType = MessageType.Event;
                        TextMessagePartModel textMsg;
                        
                        textMsg = new TextMessagePartModel();
                        // For translators: do NOT change the position of {0}!
                        textMsg.Text = "-!- " + String.Format(
                                                    _("You're now known as {0}"),
                                                    String.Empty);
                        msg.MessageParts.Add(textMsg);

                        textMsg = new TextMessagePartModel();
                        textMsg.Text = e.NewNickname;
                        textMsg.Bold = true;
                        textMsg.ForegroundColor = GetNickColor(e.NewNickname);
                        msg.MessageParts.Add(textMsg);

                        Session.AddMessageToChat(cchat, msg);
                    } else {
                        MessageModel msg = new MessageModel();
                        msg.MessageType = MessageType.Event;
                        TextMessagePartModel textMsg;
                
                        textMsg = new TextMessagePartModel();
                        textMsg.Text = "-!- ";
                        msg.MessageParts.Add(textMsg);
                        
                        textMsg = new TextMessagePartModel();
                        textMsg.Text = e.OldNickname;
                        textMsg.ForegroundColor = GetNickColor(e.OldNickname);
                        msg.MessageParts.Add(textMsg);
                        
                        textMsg = new TextMessagePartModel();
                        // For translators: do NOT change the position of {0} or {1}!
                        textMsg.Text = String.Format(
                                            _("{0} is now known as {1}"),
                                            String.Empty,
                                            String.Empty);
                        msg.MessageParts.Add(textMsg);

                        textMsg = new TextMessagePartModel();
                        textMsg.Text = e.NewNickname;
                        textMsg.ForegroundColor = GetNickColor(e.NewNickname);
                        msg.MessageParts.Add(textMsg);
                        
                        Session.AddMessageToChat(cchat, msg);
                    }
                }
            }
        }
        
        private void _OnTopic(object sender, TopicEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)GetChat(e.Channel, ChatType.Group);
            MessageModel topic = new MessageModel();
            _IrcMessageToMessageModel(ref topic, e.Topic);
            // HACK: clear possible highlights set in _IrcMessageToMessageModel()
            ClearHighlights(topic);
            Session.UpdateTopicInGroupChat(cchat, topic);
        }
        
        private void _OnTopicChange(object sender, TopicChangeEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)GetChat(e.Channel, ChatType.Group);
            MessageModel topic = new MessageModel();
            _IrcMessageToMessageModel(ref topic, e.NewTopic);
            // HACK: clear possible highlights set in _IrcMessageToMessageModel()
            ClearHighlights(topic);
            Session.UpdateTopicInGroupChat(cchat, topic);

            MessageModel msg = new MessageModel();
            msg.MessageType = MessageType.Event;
            TextMessagePartModel textMsg;

            textMsg = new TextMessagePartModel();
            textMsg.Text = "-!- ";
            msg.MessageParts.Add(textMsg);

            string who;
            if (String.IsNullOrEmpty(e.Who)) {
                who = e.Data.From;
            } else {
                who = e.Who;
            }
            textMsg = new TextMessagePartModel();
            textMsg.Text = who;
            textMsg.ForegroundColor = GetNickColor(who);
            msg.MessageParts.Add(textMsg);

            textMsg = new TextMessagePartModel();
            // For translators: do NOT change the position of {0}!
            textMsg.Text = String.Format(
                                _("{0} changed the topic of {1} to: {2}"),
                                String.Empty, e.Channel, e.NewTopic);
            msg.MessageParts.Add(textMsg);

            Session.AddMessageToChat(cchat, msg);
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
            MessageModel msg = new MessageModel();
            msg.MessageType = MessageType.Event;
            TextMessagePartModel textMsg;

            textMsg = new TextMessagePartModel();
            textMsg.Text = "-!- ";
            msg.MessageParts.Add(textMsg);

            string modechange;
            string who = null;
            ChatModel target = null;
            switch (e.Data.Type) {
                case ReceiveType.UserModeChange:
                    modechange = e.Data.Message;
                    who = e.Data.Irc.Nickname;
                    target = _NetworkChat;

                    textMsg = new TextMessagePartModel();
                    // For translators: do NOT change the position of {1}!
                    textMsg.Text =  String.Format(
                                        _("Mode change [{0}] for user {1}"),
                                        modechange, String.Empty);
                    msg.MessageParts.Add(textMsg);
                    break;
                case ReceiveType.ChannelModeChange:
                    modechange = String.Join(" ", e.Data.RawMessageArray, 3,
                                             e.Data.RawMessageArray.Length - 3);
                    target = GetChat(e.Data.Channel, ChatType.Group);
                    if (e.Data.Nick != null && e.Data.Nick.Length > 0) {
                        who = e.Data.Nick;
                    } else {
                        who = e.Data.From;
                    }

                    textMsg = new TextMessagePartModel();
                    // For translators: do NOT change the position of {2}!
                    textMsg.Text =   String.Format(
                                        _("mode/{0} [{1}] by {2}"),
                                        e.Data.Channel, modechange, String.Empty);
                    msg.MessageParts.Add(textMsg);
                    break;
            }
            
            textMsg = new TextMessagePartModel();
            textMsg.Text = who;
            textMsg.ForegroundColor = GetNickColor(who);
            msg.MessageParts.Add(textMsg);

            Session.AddMessageToChat(target, msg);
        }
        
        private void _OnQuit(object sender, QuitEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_Quit() e.Who: "+e.Who);
#endif
            if (e.Data.Irc.IsMe(e.Who)) {
                // _OnDisconnect() handles this
            } else {
                MessageModel quitMsg = new MessageModel();
                quitMsg.MessageType = MessageType.Event;
                TextMessagePartModel textMsg;
        
                textMsg = new TextMessagePartModel();
                textMsg.Text = "-!- ";
                quitMsg.MessageParts.Add(textMsg);
                
                textMsg = new TextMessagePartModel();
                textMsg.Text = e.Who;
                textMsg.ForegroundColor = GetNickColor(e.Who);
                quitMsg.MessageParts.Add(textMsg);
                
                textMsg = new TextMessagePartModel();
                textMsg.Text = String.Format(
                                    _("{0} [{1}] has quit [{2}]"),
                                    String.Empty,
                                    e.Data.Ident + "@" + e.Data.Host,
                                    e.QuitMessage);
                quitMsg.MessageParts.Add(textMsg);
                lock (Session.Chats) {
                    foreach (ChatModel chat in Session.Chats) {
                        if (chat.ProtocolManager != this) {
                            // we don't care about channels and queries the user was
                            // on other networks
                            continue;
                        }
                        
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
        }
        
        private void _OnRegistered(object sender, EventArgs e)
        {
            OnConnected(EventArgs.Empty);

            // WHO ourself so OnWho() can retrieve our ident and host
            _IrcClient.RfcWho(_IrcClient.Nickname);
        }

        protected override void OnConnected(EventArgs e)
        {
            lock (Session.Chats) {
                foreach (ChatModel chat in Session.Chats) {
                    // re-enable all person chats
                    if (chat.ProtocolManager == this &&
                        chat.ChatType == ChatType.Person) {
                        Session.EnableChat(chat);
                        // and re-sync them else new messages are not processed in
                        // the FrontendManager
                        Session.SyncChat(chat);
                    }
                }
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
        
        protected override void OnDisconnected(EventArgs e)
        {
            // only disable chats if we are listening, else we might be
            // disconnecting and removing disabled chats is prevented in the
            // FrontendManager.
            // Don't disable the protocol chat though, else the user loses all
            // control for the protocol manager! (e.g. after manual reconnect)
            if (_Listening) {
                lock (Session.Chats) {
                    foreach (ChatModel chat in Session.Chats) {
                        if (chat.ProtocolManager == this &&
                            chat.ChatType != ChatType.Protocol) {
                            Session.DisableChat(chat);
                        }
                    }
                }
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
#if LOG4NET
                    _Logger.Debug("_LagWatcher(): lag: " + lag.TotalSeconds + " seconds, difference: " + absDiff + " seconds");
#endif
                    // update network status if the lag changed over 5 seconds
                    if (absDiff > 5) {
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
