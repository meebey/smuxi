/*
 * $Id: IrcNetworkManager.cs 149 2007-04-11 16:47:52Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/IrcNetworkManager.cs $
 * $Rev: 149 $
 * $Author: meebey $
 * $Date: 2007-04-11 18:47:52 +0200 (Wed, 11 Apr 2007) $
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
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Collections;
using Meebey.SmartIrc4net;
using Meebey.Smuxi.Common;

namespace Meebey.Smuxi.Engine
{
    public enum IrcControlCode : int
    {
        Bold      = 2,
        Color     = 3,
        Clear     = 15,
        Italic    = 26,
        Underline = 31,
    }
    
    public class IrcNetworkManager : PermanentRemoteObject, INetworkManager
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static char[]   _IrcControlChars;
        private IrcClient       _IrcClient;
        private Session         _Session;
        private string          _Server;
        private int             _Port;
        private string[]        _Nicknames;
        private string          _Username;
        private string          _Password;
        private FrontendManager _FrontendManager;
        private bool            _Listening;
        private ChatModel       _NetworkChat;
        
        public bool IsConnected {
            get {
                if ((_IrcClient != null) &&
                    (_IrcClient.IsConnected)) {
                    return true;
                }
                return false;
            }
        }
        
        public string Host {
            get {
                if (_IrcClient == null) {
                    return null;
                }
                return _IrcClient.Address;
            }
        }
        
        public int Port {
            get {
                if (_IrcClient == null) {
                    return -1;
                }
                return _IrcClient.Port;
            }
        }
        
        public string NetworkID {
            get {
                // TODO: implement me
                //return _IrcClient.Network;
                return _IrcClient.Address;
            }
        }
        
        public NetworkProtocol NetworkProtocol {
            get {
                return NetworkProtocol.Irc;
            }
        }

        static IrcNetworkManager()
        {
            int[] intValues = (int[])Enum.GetValues(typeof(IrcControlCode));
            char[] chars = new char[intValues.Length];
            int i = 0;
            foreach (int intValue in intValues) {
                chars[i++] = (char)intValue;
            }
            _IrcControlChars = chars;
        }
        
        public IrcNetworkManager(Session session)
        {
            Trace.Call(session);
            
            _Session = session;
            
            _IrcClient = new IrcClient();
            _IrcClient.AutoRetry = true;
            _IrcClient.AutoReconnect = true;
            _IrcClient.AutoRelogin = true;
            _IrcClient.AutoRejoin = true;
            _IrcClient.ActiveChannelSyncing = true;
            _IrcClient.CtcpVersion      = Engine.VersionString;
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
            _IrcClient.OnDisconnected   += new EventHandler(_OnDisconnected);
            _IrcClient.OnAway           += new AwayEventHandler(_OnAway);
            _IrcClient.OnUnAway         += new IrcEventHandler(_OnUnAway);
            _IrcClient.OnNowAway        += new IrcEventHandler(_OnNowAway);
            _IrcClient.OnCtcpRequest    += new CtcpEventHandler(_OnCtcpRequest);
            _IrcClient.OnCtcpReply      += new CtcpEventHandler(_OnCtcpReply);
            
            string encodingName = (string) _Session.UserConfig["Connection/Encoding"];
            if (encodingName != null && encodingName.Length != 0) {
                try {
                    _IrcClient.Encoding = Encoding.GetEncoding(encodingName);
                } catch (Exception ex) {
#if LOG4NET
                    _Logger.Warn("IrcNetworkManager(): Error getting encoding for: " +
                                 encodingName + " falling back to system encoding.", ex);
#endif
                    _IrcClient.Encoding = Encoding.Default;
                }
            } else {
                _IrcClient.Encoding = Encoding.Default;
            }
        }
        
        public void Dispose()
        {
            Trace.Call();
            
            // we can't delete directly, it will break the enumerator, let's use a list
            ArrayList removelist = new ArrayList();
            foreach (ChatModel  chat in _Session.Chats) {
                if (chat.NetworkManager == this) {
                    removelist.Add(chat);
                }
            }
            
            // now we can delete
            foreach (ChatModel  chat in removelist) {
                _Session.RemoveChat(chat);
            }
        }
        
        public override string ToString()
        {
            string result = "IRC ";
            if (_IrcClient != null) {
                result += _IrcClient.Address + ":" + _IrcClient.Port;
            }
            
            if (IsConnected) {
                if (_IrcClient.IsAway) {
                    result += " (" + _("away") + ")";
                }
            } else {
                result += " (" + _("not connected") + ")";
            }
            return result;
        }
        
        public void Connect(FrontendManager fm, string server, int port, string[] nicks, string user, string pass)
        {
            Trace.Call(fm, server, port, nicks, user, pass);
            
            _FrontendManager = fm;
            _Server = server;
            _Port = port;
            _Nicknames = nicks;
            _Username = user;
            _Password = pass;
            
            // TODO: use config for single network chat or once per network manager
            _NetworkChat = new ChatModel("IRC " + server, ChatType.Network, this);
            _Session.Chats.Add(_NetworkChat);
            
            Thread thread = new Thread(new ThreadStart(_Run));
            thread.IsBackground = true;
            thread.Name = "IrcNetworkManager ("+server+":"+port+")";
            thread.Start();
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
                        System.Threading.Thread.Sleep(1000);
                    } catch (Exception ex) {
#if LOG4NET
                        _Logger.Error("_Run(): exception in _Listen() occurred!" ,ex);
#endif
                        Reconnect(_FrontendManager);
                    }
                }
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
                string msg;
                _Session.AddTextToChat(_NetworkChat, "-!- " + _("Connection error! Reason: ") + ex.Message);
                throw;
            }
        }
        
        public void Connect(FrontendManager fm)
        {
            Trace.Call(fm);
            
            try {
                string msg;
                msg = String.Format(_("Connecting to {0} port {1}..."), _Server, _Port);
                fm.SetStatus(msg);
                _Session.AddTextToChat(_NetworkChat, "-!- " + msg);
                
                _IrcClient.Connect(_Server, _Port);
                fm.UpdateNetworkStatus();
                msg = String.Format(_("Connection to {0} established"), _Server);
                fm.SetStatus(msg);
                _Session.AddTextToChat(_NetworkChat, "-!- " + msg);
                _Session.AddTextToChat(_NetworkChat, "-!- " + _("Logging in..."));
                if (_Password != null) {
                    _IrcClient.RfcPass(_Password, Priority.Critical);
                }
                _IrcClient.Login(_Nicknames, (string)_Session.UserConfig["Connection/Realname"], 0, _Username);
                
                foreach (string command in (string[])_Session.UserConfig["Connection/OnConnectCommands"]) {
                    if (command.Length == 0) {
                        continue;
                    } 
                    CommandModel cd = new CommandModel(_FrontendManager, _NetworkChat,
                        (string)_Session.UserConfig["Interface/Entry/CommandCharacter"],
                        command);
                        
                    bool handled;
                    handled = _Session.Command(cd);
                    if (!handled) {
                        Command(cd);
                    }
                }
                _Listening = true;
            } catch (CouldNotConnectException ex) {
                _FrontendManager.SetStatus(_("Connection failed!"));
                _Session.AddTextToChat(_NetworkChat, "-!- " + _("Connection failed! Reason: ") + ex.Message);
                throw;
            }
        }
        
        public void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            
            fm.SetStatus(_("Disconnecting..."));
            if (IsConnected) {
                _Session.AddTextToChat(_NetworkChat, "-!- " + 
                    String.Format(_("Disconnecting from {0}..."), _IrcClient.Address));
                _IrcClient.Disconnect();
                fm.SetStatus(String.Format(_("Disconnected from {0}"), _IrcClient.Address));
                _Session.AddTextToChat(_NetworkChat, "-!- " +
                    _("Connection closed"));
                
                _Listening = false;
                // TODO: set someone else as current network manager?
            } else {
                fm.SetStatus(_("Not connected!"));
                fm.AddTextToChat(_NetworkChat, "-!- " +
                    _("Not connected"));
            }
            fm.UpdateNetworkStatus();
        }
        
        public void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            
            fm.SetStatus("Reconnecting...");
            try {
                string msg;
                if (_IrcClient != null) {
                    _Session.AddTextToChat(_NetworkChat, "-!- Reconnecting to " + _IrcClient.Address+"...");
                    _IrcClient.Reconnect(true);
                    msg = "Connection to "+_IrcClient.Address+" established";
                    fm.SetStatus(msg); 
                    _Session.AddTextToChat(_NetworkChat, "-!- "+msg);
                } else {
                    fm.SetStatus("Reconnect Error");
                    _Session.AddTextToChat(_NetworkChat, "-!- Reconnect Error");
                }
            } catch (ConnectionException) {
                fm.SetStatus("Not connected!");
                fm.AddTextToChat(_NetworkChat, "-!- Not connected");
            }
            fm.UpdateNetworkStatus();
        }
        
        public bool Command(CommandModel command)
        {
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
                            foreach (INetworkManager nm in _Session.NetworkManagers) {
                                if (nm == this) {
                                    // skip us, else we send it 2 times
                                    continue;
                                }
                                if (nm is IrcNetworkManager) {
                                    IrcNetworkManager ircnm = (IrcNetworkManager)nm;
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
                        case "quit":
                            CommandQuit(command);
                            handled = true;
                            break;
                    }
                } else {
                    // normal text
                    if (command.FrontendManager.CurrentChat.ChatType == ChatType.Network) {
                        // we are on the server chat
                        _IrcClient.WriteLine(command.Data);
                    } else {
                        // we are on a channel or query chat
                        _Say(command, command.Data);
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
                    }
                } else {
                    // normal text, without connection
                    _NotConnected(command);
                    handled = true;
                }
            }
            
            return handled;
        }
        
        public void CommandHelp(CommandModel cd)
        {
            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;

            fmsgti = new TextMessagePartModel();
            fmsgti.Text = "[IrcNetworkManager Commands]";
            fmsgti.Bold = true;
            fmsg.MessageParts.Add(fmsgti);
            
            _Session.AddMessageToChat(cd.FrontendManager.CurrentChat, fmsg);
            
            string[] help = {
            "help",
            "say",
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
            "ctcp destination command [data]",
            "raw/quote irccommand",
            "quit [quitmessage]",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToCurrentChat("-!- " + line);
            }
        }
        
        private void _Say(CommandModel cd, string message)
        {
            string channelName = cd.FrontendManager.CurrentChat.Name;
            
            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            if (cd.FrontendManager.CurrentChat.IsEnabled) {
                _IrcClient.SendMessage(SendType.Message, channelName, message);
                
                fmsgti = new TextMessagePartModel();
                fmsgti.Text = "<";
                fmsg.MessageParts.Add(fmsgti);
            
                fmsgti = new TextMessagePartModel();
                fmsgti.Text = _IrcClient.Nickname;
                fmsgti.ForegroundColor = IrcTextColor.Blue;
                fmsg.MessageParts.Add(fmsgti);
                
                fmsgti = new TextMessagePartModel();
                fmsgti.Text = "> ";
                fmsg.MessageParts.Add(fmsgti);
                
                _IrcMessageToMessageModel(ref fmsg, message);
            } else {
                fmsgti = new TextMessagePartModel();
                fmsgti.Text = "-!- " +
                    String.Format(
                        _("Not joined to channel: {0}. Please rejoin."),
                        channelName);
                fmsg.MessageParts.Add(fmsgti);
            }
            
            _Session.AddMessageToChat(cd.FrontendManager.CurrentChat, fmsg);
        }
        
        public void CommandSay(CommandModel cd)
        {
            _Say(cd, cd.Parameter);
        }
        
        public void CommandJoin(CommandModel cd)
        {
            string channel = null;
            if ((cd.DataArray.Length >= 2) &&
                (cd.DataArray[1].Length >= 1)) {
                switch (cd.DataArray[1][0]) {
                    case '#':
                    case '!':
                    case '+':
                    case '&':
                        channel = cd.DataArray[1];
                        break;
                    default:
                        channel = "#" + cd.DataArray[1];  
                        break;
                }
            } else {
                _NotEnoughParameters(cd);
                return;
            }
            
            if (_IrcClient.IsJoined(channel)) {
                cd.FrontendManager.AddTextToCurrentChat(
                    "-!- " +
                    String.Format(
                        _("Already joined to channel: {0}." +
                        " Type /window {0} to switch to it"),
                        channel));
                return;
            }
            
            if (cd.DataArray.Length == 2) {
                _IrcClient.RfcJoin(channel);
            } else if (cd.DataArray.Length > 2) {
                _IrcClient.RfcJoin(channel, cd.DataArray[2]);
            }
        }
        
        public void CommandCycle(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            if (cd.Chat.ChatType == ChatType.Group) {
                 CommandPart(cd);
                 CommandJoin(new CommandModel(fm, cd.Chat, cd.Chat.Name));
            }
        }
        
        public void CommandMessage(CommandModel cd)
        {
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
            if (cd.DataArray.Length >= 2) {
                string nickname = cd.DataArray[1];
                ChatModel chat = _Session.GetChat(nickname, ChatType.Person, NetworkProtocol.Irc, this);
                if (chat == null) {
                    chat = new ChatModel(nickname, ChatType.Person, this);
                    _Session.AddChat(chat);
                }
            }
            
            if (cd.DataArray.Length >= 3) {
                string message = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);
                string nickname = cd.DataArray[1];
                ChatModel chat = _Session.GetChat(nickname, ChatType.Person, NetworkProtocol.Irc, this);
                _IrcClient.SendMessage(SendType.Message, nickname, message);
                _Session.AddTextToChat(chat, "<" + _IrcClient.Nickname + "> " + message);
            }
        }
        
        public void CommandMessageChannel(CommandModel cd)
        {
            if (cd.DataArray.Length >= 3) {
                string message = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);
                string channelname = cd.DataArray[1];
                ChatModel chat = _Session.GetChat(channelname, ChatType.Group, NetworkProtocol.Irc, this);
                if (chat == null) {
                    // server chat as fallback if we are not joined
                    chat = _NetworkChat;
                }
                _IrcClient.SendMessage(SendType.Message, channelname, message);
                _Session.AddTextToChat(chat, "<" + _IrcClient.Nickname + ":" + channelname + "> " + message);
            } else {
                _NotEnoughParameters(cd);
            }
        }
        
        public void CommandPart(CommandModel cd)
        {
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
                        // sems to be a part message
                        _IrcClient.RfcPart(cd.DataArray[1], cd.Parameter);
                        break;
                }
            } else {
                ChatModel chat = cd.FrontendManager.CurrentChat;
                _IrcClient.RfcPart(chat.Name);
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
                _Session.AddTextToChat(_NetworkChat, "[ctcp(" + destination + ")] " + command + " " + parameters);
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
                _Session.AddTextToChat(_NetworkChat, "[ctcp(" + destination + ")] PING " + timestamp);
                _IrcClient.SendMessage(SendType.CtcpRequest, destination, "PING " + timestamp);
            } else {
                _NotEnoughParameters(cd);
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
            ChatModel chat = fm.CurrentChat;
            string channel = chat.Name;
            if (cd.DataArray.Length >= 2) {
                _IrcClient.RfcTopic(channel, cd.Parameter);
            } else {
                if (_IrcClient.IsJoined(channel)) {
                    string topic = _IrcClient.GetChannel(channel).Topic;
                    if (topic.Length > 0) {
                        fm.AddTextToChat(chat,
                            "-!- " + String.Format(_("Topic for {0}: {1}"), channel, topic));
                    } else {
                        fm.AddTextToChat(chat,
                            "-!- " + String.Format(_("No topic set for {0}"), channel));
                    }
                }
            }
        }
        
        public void CommandOp(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            string channel = chat.Name;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Op(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.Split(new char[] {' '});
                foreach (string nick in candidates) {
                    _IrcClient.Op(channel, nick);
                }
            }
        }
    
        public void CommandDeop(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            string channel = chat.Name;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Deop(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.Split(new char[] {' '});
                foreach(string nick in candidates) {
                    _IrcClient.Deop(channel, nick);
                }
            }
        }

        public void CommandVoice(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            string channel = chat.Name;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Voice(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.Split(new char[] {' '});
                foreach (string nick in candidates) {
                    _IrcClient.Voice(channel, nick);
                }
            }
        }

        public void CommandDevoice(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            string channel = chat.Name;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Devoice(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.Split(new char[] {' '});
                foreach (string nick in candidates) {
                    _IrcClient.Devoice(channel, nick);
                }
            }
        }

        public void CommandBan(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            string channel = chat.Name;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Ban(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.Split(new char[] {' '});
                foreach(string nick in candidates) {
                    _IrcClient.Ban(channel, nick);
                }
            }
        }

        public void CommandUnban(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            string channel = chat.Name;
            if (cd.DataArray.Length == 2) {
                _IrcClient.Unban(channel, cd.Parameter);
            } else if (cd.DataArray.Length > 2) {
                string[] candidates = cd.Parameter.Split(new char[] {' '});
                foreach (string nick in candidates) {
                    _IrcClient.Unban(channel, nick);
                }
            }
        }

        public void CommandKick(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            string channel = chat.Name;
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
            }
        }

        public void CommandKickban(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            string channel = chat.Name;
            SmartIrc4net.IrcUser ircuser;
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
            }
        }

        public void CommandMode(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            if (cd.DataArray.Length >= 2) {
                if (chat.ChatType == ChatType.Network) {
                    _IrcClient.RfcMode(_IrcClient.Nickname, cd.Parameter);
                } else {
                    string channel = chat.Name;
                    _IrcClient.RfcMode(channel, cd.Parameter);
                }
            }
        }

        public void CommandInvite(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            ChatModel chat = fm.CurrentChat;
            string channel = chat.Name;
            if (cd.DataArray.Length >= 2) {
                if (!_IrcClient.IsJoined(channel, cd.DataArray[1])) {
                    _IrcClient.RfcInvite(cd.DataArray[1], channel);
                    fm.AddTextToChat(chat, "-!- " + String.Format(
                                                        _("Inviting {0} to {1}"),
                                                        cd.DataArray[1], channel));
                } else {
                    fm.AddTextToChat(chat, "-!- " + String.Format(
                                                        _("{0} is already on channel"),
                                                        cd.DataArray[1]));
                }
            }
        }

        public void CommandRaw(CommandModel cd)
        {
            _IrcClient.WriteLine(cd.Parameter);
        }
    
        public void CommandMe(CommandModel cd)
        {
            ChatModel chat = cd.FrontendManager.CurrentChat;
            string channel = chat.Name;
            if (cd.DataArray.Length >= 2) {
                _IrcClient.SendMessage(SendType.Action, channel, cd.Parameter);
                _Session.AddTextToChat(chat, " * " + _IrcClient.Nickname + " " + cd.Parameter);
            }
        }
    
        public void CommandNotice(CommandModel cd)
        {
            if (cd.DataArray.Length >= 3) {
                string target = cd.DataArray[1];
                string message = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);  
                _IrcClient.SendMessage(SendType.Notice, target, message);
                
                // BUG: proping via GetChat() is more reliable
                ChatModel chat;
                if (_IrcClient.IsJoined(target)) {
                    chat = _Session.GetChat(target, ChatType.Person, NetworkProtocol.Irc, this);
                } else {
                    chat = _NetworkChat;
                }
                _Session.AddTextToChat(chat, "[notice(" + target + ")] " + message);
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
            string message = cd.Parameter; 
            if (message != null) {
                _IrcClient.RfcQuit(message);
            } else {
                _IrcClient.RfcQuit();
            }
        }
        
        private void _NotEnoughParameters(CommandModel cd)
        {
            cd.FrontendManager.AddTextToCurrentChat(
                "-!- " + String.Format(_("Not enough parameters for {0} command"), cd.Command));
        }
        
        private void _NotConnected(CommandModel cd)
        {
            cd.FrontendManager.AddTextToCurrentChat("-!- " + _("Not connected to server"));
        }
        
        private void _IrcMessageToMessageModel(ref MessageModel fmsg, string message)
        {
            TextMessagePartModel fmsgti;
            MessagePartModel fmsgi;
            
            /*
            int urlPos = message.IndexOf("http://");
            if (urlPos != -1) {
                MessageModelUrlItem fmsgui = new MessageModelUrlItem();
                fmsgui.Url = 
                fmsg.Items.Add(
            }
			*/
			
            // strip color and formatting if configured
            if ((bool)_Session.UserConfig["Interface/Notebook/StripColors"]) {
                message = Regex.Replace(message, (char)IrcControlCode.Color +
                            "[0-9]{1,2}(,[0-9]{1,2})?", String.Empty);
            }
            if ((bool)_Session.UserConfig["Interface/Notebook/StripFormattings"]) {
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
                        default:
                            break;
                    }
#if LOG4NET
                    _Logger.Debug("_IrcMessageToMessageModel(): controlChars.Length: " + controlChars.Length);
#endif

                    int nextControlPos = message.IndexOfAny(_IrcControlChars, controlPos + 1);
                    if (nextControlPos != -1) {
                        // BUG: length is wrong
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
                
                bool highlight = false;
                if (submessage.IndexOf(_IrcClient.Nickname) != -1) {
                    highlight = true;
                    string highlightColor = (string) _Session.UserConfig["Interface/Notebook/Tab/HighlightColor"];
                    fg_color = new TextColor(Int32.Parse(highlightColor.Substring(1), NumberStyles.HexNumber));
                }
                
                fmsgti = new TextMessagePartModel();
                fmsgti.Text = submessage;
                fmsgti.Bold = bold;
                fmsgti.Underline = underline;
                fmsgti.Italic = italic;
                fmsgti.ForegroundColor = fg_color;
                fmsgti.BackgroundColor = bg_color;
                fmsgti.IsHighlight = highlight;
                fmsg.MessageParts.Add(fmsgti);
            } while (controlCharFound);
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
        
        private void _OnRawMessage(object sender, IrcEventArgs e)
        {
    	    if (e.Data.Message != null) {
                switch (e.Data.Type) {
                    case ReceiveType.Error:
                    case ReceiveType.Info:
                    case ReceiveType.Invite:
                    case ReceiveType.List:
                    case ReceiveType.Login:
                        _Session.AddTextToChat(_NetworkChat, e.Data.Message);
                        break;
                    case ReceiveType.Motd:
                        MessageModel fmsg = new MessageModel();
                        _IrcMessageToMessageModel(ref fmsg, e.Data.Message);
                        _Session.AddMessageToChat(_NetworkChat, fmsg);
                        break;
                    case ReceiveType.WhoIs:
                        _OnReceiveTypeWhois(e);
                        break;
                    case ReceiveType.WhoWas:
                        _OnReceiveTypeWhowas(e);
                        break;
                }
            }
            
            string chan;
            string nick;
            string msg;
            ChatModel chat;            
            switch (e.Data.ReplyCode) {
                case ReplyCode.ErrorNoSuchNickname:
                    nick = e.Data.RawMessageArray[3];
                    msg = "-!- " + String.Format(_("{0}: No such nick/channel"), nick);
                    chat = _Session.GetChat(nick, ChatType.Person, NetworkProtocol.Irc, this);
                    if (chat != null) {
                        _Session.AddTextToChat(chat, msg);
                    } else {
                        _Session.AddTextToChat(_NetworkChat, msg);
                    }
                    break;
                case ReplyCode.ErrorChannelOpPrivilegesNeeded:
                    chan = e.Data.RawMessageArray[3];
                    msg = "-!- " + chan + " " + e.Data.Message;
                    chat = _Session.GetChat(chan, ChatType.Group, NetworkProtocol.Irc, this);
                    if (chat != null) {
                        _Session.AddTextToChat(chat, msg);
                    } else {
                        _Session.AddTextToChat(_NetworkChat, msg);
                    }
                    break;
                case ReplyCode.EndOfNames:
                    chan = e.Data.RawMessageArray[3]; 
                    GroupChatModel groupChat = (GroupChatModel)_Session.GetChat(
                       chan, ChatType.Group, NetworkProtocol.Irc, this);
                    groupChat.IsSynced = true;
#if LOG4NET
                    _Logger.Debug("_OnRawMessage(): " + chan + " synced");
#endif
                    break;
            }
        }
        
        private void _OnReceiveTypeWhois(IrcEventArgs e)
        {
            string nick = e.Data.RawMessageArray[3];
            ChatModel chat = _Session.GetChat(nick, ChatType.Person, NetworkProtocol.Irc, this);
            if (chat == null) {
                chat = _NetworkChat;
            }
            switch (e.Data.ReplyCode) {
                case ReplyCode.WhoIsUser:
                    string ident = e.Data.RawMessageArray[4];
                    string host = e.Data.RawMessageArray[5];
                    string realname = e.Data.Message;
                    _Session.AddTextToChat(chat, "-!- " + nick + " [" + ident + "@" + host + "]");
                    _Session.AddTextToChat(chat, "-!-  realname: " + realname);
                    break;
                case ReplyCode.WhoIsServer:
                    string server = e.Data.RawMessageArray[4];
                    string serverinfo = e.Data.Message;
                    _Session.AddTextToChat(chat, "-!-  server: " + server + " [" + serverinfo + "]");
                    break;
                case ReplyCode.WhoIsIdle:
                    string idle = e.Data.RawMessageArray[4];
                    try {
                        long timestamp = Int64.Parse(e.Data.RawMessageArray[5]);
                        DateTime signon =  new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        signon = signon.AddSeconds(timestamp).ToLocalTime();
                        _Session.AddTextToChat(chat, "-!-  idle: "+idle+" [signon: "+signon.ToString()+"]");
                    } catch (FormatException) {
                    }
                    break;
                case ReplyCode.WhoIsChannels:
                    string channels = e.Data.Message;
                    _Session.AddTextToChat(chat, "-!-  channels: " + channels);
                    break;
                case ReplyCode.WhoIsOperator:
                    _Session.AddTextToChat(chat, "-!-  " + e.Data.Message);
                    break;
                case ReplyCode.EndOfWhoIs:
                    _Session.AddTextToChat(chat, "-!-  " + e.Data.Message);
                    break;
            }
        }
        
        private void _OnReceiveTypeWhowas(IrcEventArgs e)
        {
            string nick = e.Data.RawMessageArray[3];
            ChatModel chat = _Session.GetChat(nick, ChatType.Person, NetworkProtocol.Irc, this);
            if (chat == null) {
                chat = _NetworkChat;
            }
            switch (e.Data.ReplyCode) {
                case ReplyCode.WhoWasUser:
                    string ident = e.Data.RawMessageArray[4];
                    string host = e.Data.RawMessageArray[5];
                    string realname = e.Data.Message;
                    _Session.AddTextToChat(chat, "-!- " + nick + " [" + ident + "@" + host + "]");
                    _Session.AddTextToChat(chat, "-!-  realname: " + realname);
                    break;
                case ReplyCode.EndOfWhoWas:
                    _Session.AddTextToChat(chat, "-!-  " + e.Data.Message);
                    break;
            }
        }
        
        private void _OnCtcpRequest(object sender, CtcpEventArgs e)
        {
            _Session.AddTextToChat(_NetworkChat, String.Format(
                                            _("{0} [{1}] requested CTCP {2} from {3}: {4}"),
                                            e.Data.Nick, e.Data.Ident+"@"+e.Data.Host,
                                            e.CtcpCommand, _IrcClient.Nickname,
                                            e.CtcpParameter));
        }
        
        private void _OnCtcpReply(object sender, CtcpEventArgs e)
        {
            if (e.CtcpCommand == "PING") {
                try {
                    long timestamp = Int64.Parse(e.CtcpParameter);
                    if (!(timestamp >= 0)) {
                        return;
                    }
                    DateTime sent = DateTime.FromFileTime(timestamp);
                    string duration = DateTime.Now.Subtract(sent).TotalSeconds.ToString();
                    _Session.AddTextToChat(_NetworkChat, String.Format(
                                                    _("CTCP PING reply from {0}: {1} seconds"),
                                                    e.Data.Nick, duration));
                } catch (FormatException) {
                }
            } else {
                _Session.AddTextToChat(_NetworkChat, String.Format(
                                            _("CTCP {0} reply from {1}: {2}"),
                                            e.CtcpCommand, e.Data.Nick, e.CtcpParameter));
            }
        }
        
        private void _OnChannelMessage(object sender, IrcEventArgs e)
        {
            ChatModel chat = _Session.GetChat(e.Data.Channel, ChatType.Group, NetworkProtocol.Irc, this);

            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            fmsgti = new TextMessagePartModel();
            fmsgti.Text = String.Format("<{0}> ", e.Data.Nick);
            fmsg.MessageParts.Add(fmsgti);
            
            _IrcMessageToMessageModel(ref fmsg, e.Data.Message);
            
            _Session.AddMessageToChat(chat, fmsg);
        }
        
        private void _OnChannelAction(object sender, ActionEventArgs e)
        {
            ChatModel chat = _Session.GetChat(e.Data.Channel, ChatType.Group, NetworkProtocol.Irc, this);

            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            fmsgti = new TextMessagePartModel();
            fmsgti.Text = String.Format(" * {0} ", e.Data.Nick);
            fmsg.MessageParts.Add(fmsgti);
            
            _IrcMessageToMessageModel(ref fmsg, e.ActionMessage);
            
            _Session.AddMessageToChat(chat, fmsg);
        }
        
        private void _OnChannelNotice(object sender, IrcEventArgs e)
        {
            ChatModel chat = _Session.GetChat(e.Data.Channel, ChatType.Group, NetworkProtocol.Irc, this);

            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            fmsgti = new TextMessagePartModel();
            fmsgti.Text = String.Format("-{0}:{1}- ", e.Data.Nick, e.Data.Channel);
            fmsg.MessageParts.Add(fmsgti);
            
            _IrcMessageToMessageModel(ref fmsg, e.Data.Message);
            
            _Session.AddMessageToChat(chat, fmsg);
        }
        
        private void _OnQueryMessage(object sender, IrcEventArgs e)
        {
            ChatModel chat = _Session.GetChat(e.Data.Nick, ChatType.Person, NetworkProtocol.Irc, this);
            if (chat == null) {
                chat = new ChatModel(e.Data.Nick, ChatType.Person, this);
                _Session.AddChat(chat);
            }
            
            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            fmsgti = new TextMessagePartModel();
            fmsgti.Text = String.Format("<{0}> ", e.Data.Nick);
            fmsg.MessageParts.Add(fmsgti);
            
            _IrcMessageToMessageModel(ref fmsg, e.Data.Message);
            
            _Session.AddMessageToChat(chat, fmsg);
        }
        
        private void _OnQueryAction(object sender, ActionEventArgs e)
        {
            ChatModel chat = _Session.GetChat(e.Data.Nick, ChatType.Person, NetworkProtocol.Irc, this);
            if (chat == null) {
                chat = new ChatModel(e.Data.Nick, ChatType.Person, this);
                _Session.AddChat(chat);
            }
            
            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            fmsgti = new TextMessagePartModel();
            fmsgti.Text = String.Format(" * {0} ", e.Data.Nick);
            fmsg.MessageParts.Add(fmsgti);
            
            _IrcMessageToMessageModel(ref fmsg, e.ActionMessage);
            
            _Session.AddMessageToChat(chat, fmsg);
        }
        
        private void _OnQueryNotice(object sender, IrcEventArgs e)
        {
            ChatModel chat = null;
            if (e.Data.Nick != null) {
                chat = _Session.GetChat(e.Data.Nick, ChatType.Person, NetworkProtocol.Irc, this);
            }
            if (chat == null) {
                // use server chat as fallback
                chat = _NetworkChat;
            }

            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;
            
            fmsgti = new TextMessagePartModel();
            fmsgti.Text = String.Format("-{0} ({1}@{2})- ", e.Data.Nick, e.Data.Ident, e.Data.Host);
            fmsg.MessageParts.Add(fmsgti);
            
            _IrcMessageToMessageModel(ref fmsg, e.Data.Message);
            
            _Session.AddMessageToChat(chat, fmsg);
        }
        
        private void _OnJoin(object sender, JoinEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            if (e.Data.Irc.IsMe(e.Who)) {
                if (cchat == null) {
                    cchat = new GroupChatModel(e.Channel, this);
                    _Session.AddChat(cchat);
                } else {
                    // chat still exists, so we we only need to enable it
                    _Session.EnableChat(cchat);
                }
            } else {
                // someone else joined, let's add him to the channel chat
                SmartIrc4net.IrcUser siuser = _IrcClient.GetIrcUser(e.Who);
                IrcGroupPersonModel icuser = new IrcGroupPersonModel(e.Who, siuser.Realname,
                                        siuser.Ident, siuser.Host, NetworkID, this);
                 cchat.UnsafePersons.Add(icuser.NickName.ToLower(), icuser);
                _Session.AddPersonToGroupChat(cchat, icuser);
            }
            
            _Session.AddTextToChat(cchat,
                "-!- " + String.Format(
                            _("{0} [{1}] has joined {2}"),
                            e.Who, e.Data.Ident + "@" + e.Data.Host, e.Channel));
        }
        
        private void _OnNames(object sender, NamesEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnNames() e.Channel: "+e.Channel);
#endif
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Data.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            if (cchat.IsSynced) {
                // nothing todo for us
                return;
            }
            
            //bool op;
            //bool voice;
            foreach (string user in e.UserList) {
                if (user.TrimEnd(' ').Length == 0) {
                    continue;
                }
                string username = user;
                
                //op = false;
                //voice = false;
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
				
                IrcGroupPersonModel icuser = new IrcGroupPersonModel(username, NetworkID, this);
                /*
                if (op) {
                    icuser.IsOp = true;
                }
                if (voice) {
                    icuser.IsVoice = true;
                }
                */
                
                // don't tell any frontend yet that there is new data, SyncPage() will do it
                //_Session.AddUserToChannel(cchat, icuser);
                cchat.UnsafePersons.Add(icuser.NickName.ToLower(), icuser);
#if LOG4NET
                _Logger.Debug("_OnNames() added user: " + username + " to: " + cchat.Name);
#endif
            }
        }
        
        private void _OnChannelActiveSynced(object sender, IrcEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnChannelActiveSynced() e.Data.Channel: "+e.Data.Channel);
#endif
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Data.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            SmartIrc4net.Channel schan = _IrcClient.GetChannel(e.Data.Channel);
            foreach (ChannelUser scuser in schan.Users.Values) {
                IrcGroupPersonModel icuser = (IrcGroupPersonModel)cchat.GetPerson(scuser.Nick);
                if (icuser == null) {
                    /*
                    icuser = new IrcGroupPersonModel(scuser.Nick, scuser.Realname,
                                    scuser.Ident, scuser.Host);
                    // don't tell any frontend yet that there is new data, SyncPage() will do it
                    //_Session.AddUserToChannel(cchat, icuser);
                    cchat.UnsafeUsers.Add(icuser.Nickname.ToLower(), icuser);
                    */
                    // we should not get here anymore, _OnNames creates the users already
                    _Logger.Error("_OnChannelActiveSynced(): cchat.GetPerson(" + scuser.Nick + ") returned null!");
                }
                icuser.RealName = scuser.Realname;
                icuser.Ident = scuser.Ident;
                icuser.Host = scuser.Host;
                icuser.IsOp = scuser.IsOp;
                icuser.IsVoice = scuser.IsVoice;
                
                // don't tell any frontend yet that there is new data, SyncPage() will do it
                //_Session.UpdatePersonInGroupChat(cchat, icuser, icuser);
            }
            _Session.SyncChat(cchat);
        }
        
        private void _OnPart(object sender, PartEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnPart() e.Channel: "+e.Channel+" e.Who: "+e.Who);
#endif
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            if (e.Data.Irc.IsMe(e.Who)) {
                _Session.RemoveChat(cchat);
            } else {
                PersonModel user = cchat.GetPerson(e.Who);
                _Session.RemovePersonFromGroupChat(cchat, user);
                _Session.AddTextToChat(cchat,
                    "-!- " + String.Format(
                                _("{0} [{1}] has left {2} [{3}]"),
                                e.Who, e.Data.Ident + "@" + e.Data.Host, e.Channel, e.PartMessage));
            }
        }
        
        private void _OnKick(object sender, KickEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_OnKick() e.Channel: "+e.Channel+" e.Whom: "+e.Whom);
#endif
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            if (e.Data.Irc.IsMe(e.Whom)) {
                _Session.DisableChat(cchat);
                _Session.AddTextToChat(cchat,
                    "-!- " + String.Format(
                                _("You was kicked from {0} by {1} [{2}]"),
                                e.Channel, e.Who, e.KickReason));
            } else {
                PersonModel user = cchat.GetPerson(e.Whom);
                _Session.RemovePersonFromGroupChat(cchat, user);
                _Session.AddTextToChat(cchat,
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
                _Session.AddTextToChat(_NetworkChat, "-!- " + String.Format(
                                                        _("You're now known as {0}"),
                                                        e.NewNickname));
            }
            
            SmartIrc4net.IrcUser ircuser = e.Data.Irc.GetIrcUser(e.NewNickname);
            if (ircuser != null) {
                foreach (string channel in ircuser.JoinedChannels) {
                    GroupChatModel cchat = (GroupChatModel)_Session.GetChat(channel, ChatType.Group, NetworkProtocol.Irc, this);
                    
                    // clone the old user to a new user
                    IrcGroupPersonModel olduser = (IrcGroupPersonModel)cchat.GetPerson(e.OldNickname);
                    if (olduser == null) {
#if LOG4NET
                        _Logger.Error("cchat.GetPerson(e.OldNickname) returned null! cchat.Name: "+cchat.Name+" e.OldNickname: "+e.OldNickname);
#endif
                        continue;
                    }
                    IrcGroupPersonModel newuser = new IrcGroupPersonModel(e.NewNickname, ircuser.Realname,
                                        ircuser.Ident, ircuser.Host, NetworkID, this);
                    newuser.IsOp = olduser.IsOp;
                    newuser.IsVoice = olduser.IsVoice;
                    
                    _Session.UpdatePersonInGroupChat(cchat, olduser, newuser);
                    
                    if (e.Data.Irc.IsMe(e.NewNickname)) {
                        _Session.AddTextToChat(cchat, "-!- " + String.Format(
                                                                _("You're now known as {0}"),
                                                                e.NewNickname));
                    } else {
                        _Session.AddTextToChat(cchat, "-!- " + String.Format(
                                                                _("{0} is now known as {1}"),
                                                                e.OldNickname, e.NewNickname));
                    }
                }
            }
        }
        
        private void _OnTopic(object sender, TopicEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            _Session.UpdateTopicInGroupChat(cchat, e.Topic);
        }
        
        private void _OnTopicChange(object sender, TopicChangeEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            _Session.UpdateTopicInGroupChat(cchat, e.NewTopic);
            _Session.AddTextToChat(cchat, "-!- " + String.Format(
                                                    _("{0} changed the topic of {1} to: {2}"),
                                                    e.Who, e.Channel, e.NewTopic));
        }
        
        private void _OnOp(object sender, OpEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            IrcGroupPersonModel user = (IrcGroupPersonModel)cchat.GetPerson(e.Whom);
            if (user != null) {
                user.IsOp = true;
                _Session.UpdatePersonInGroupChat(cchat, user, user);
#if LOG4NET
            } else {
                _Logger.Error("_OnOp(): cchat.GetPerson(e.Whom) returned null! cchat.Name: "+cchat.Name+" e.Whom: "+e.Whom);
#endif
            }
        }
        
        private void _OnDeop(object sender, DeopEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            IrcGroupPersonModel user = (IrcGroupPersonModel)cchat.GetPerson(e.Whom);
            if (user != null) {
                user.IsOp = false;
                _Session.UpdatePersonInGroupChat(cchat, user, user);
#if LOG4NET
            } else {
                _Logger.Error("_OnDeop(): cchat.GetPerson(e.Whom) returned null! cchat.Name: "+cchat.Name+" e.Whom: "+e.Whom);
#endif
            }
        }
        
        private void _OnVoice(object sender, VoiceEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            IrcGroupPersonModel user = (IrcGroupPersonModel)cchat.GetPerson(e.Whom);
            if (user != null) {
                user.IsVoice = true;
                _Session.UpdatePersonInGroupChat(cchat, user, user);
#if LOG4NET
            } else {
                _Logger.Error("cchat.GetPerson(e.Whom) returned null! cchat.Name: "+cchat.Name+" e.Whom: "+e.Whom);
#endif
            }
        }
        
        private void _OnDevoice(object sender, DevoiceEventArgs e)
        {
            GroupChatModel cchat = (GroupChatModel)_Session.GetChat(e.Channel, ChatType.Group, NetworkProtocol.Irc, this);
            IrcGroupPersonModel user = (IrcGroupPersonModel)cchat.GetPerson(e.Whom);
            if (user != null) {
                user.IsVoice = false;
                _Session.UpdatePersonInGroupChat(cchat, user, user);
#if LOG4NET
            } else {
                _Logger.Error("cchat.GetPerson(e.Whom) returned null! cchat.Name: "+cchat.Name+" e.Whom: "+e.Whom);
#endif
            }
        }
        
        private void _OnModeChange(object sender, IrcEventArgs e)
        {
            string modechange;
            switch (e.Data.Type) {
                case ReceiveType.UserModeChange:
                    modechange = e.Data.Message;
                    _Session.AddTextToChat(_NetworkChat, "-!- " + String.Format(
                                                            _("Mode change [{0}] for user {1}"),
                                                            modechange, e.Data.Irc.Nickname));
                break;
                case ReceiveType.ChannelModeChange:
                    modechange = String.Join(" ", e.Data.RawMessageArray, 3, e.Data.RawMessageArray.Length-3);
                    string who;
                    if (e.Data.Nick != null && e.Data.Nick.Length > 0) {
                        who = e.Data.Nick;
                    } else {
                        who = e.Data.From;
                    }
                    ChatModel chat = _Session.GetChat(e.Data.Channel, ChatType.Group, NetworkProtocol.Irc, this);
                    _Session.AddTextToChat(chat, "-!- " + String.Format(
                                                            _("mode/{0} [{1}] by {2}"),
                                                            e.Data.Channel, modechange, who));
                break;
            }
        }
        
        private void _OnQuit(object sender, QuitEventArgs e)
        {
#if LOG4NET
            _Logger.Debug("_Quit() e.Who: "+e.Who);
#endif
            if (e.Data.Irc.IsMe(e.Who)) {
                // _OnDisconnect() handles this
            } else {
                foreach (ChatModel chat in _Session.Chats) {
                    if (chat.NetworkManager != this) {
                        // we don't care about channels and queries the user was
                        // on other networks
                        continue;
                    }
                    
                    if (chat.ChatType == ChatType.Group) {
                        GroupChatModel cchat = (GroupChatModel)chat;
                        PersonModel user = cchat.GetPerson(e.Who);
                        if (user != null) {
                            // he is on this channel, let's remove him
                            _Session.RemovePersonFromGroupChat(cchat, user);
                            _Session.AddTextToChat(cchat, "-!- " + String.Format(
                                                                    _("{0} [{1}] has quit [{2}]"),
                                                                    e.Who, e.Data.Ident + "@" + e.Data.Host, e.QuitMessage));
                        }
                    } else if ((chat.ChatType == ChatType.Person) &&
                               (chat.Name == e.Who)) {
                        _Session.AddTextToChat(chat, "-!- " + String.Format(
                                                                _("{0} [{1}] has quit [{2}]"),
                                                                e.Who, e.Data.Ident + "@" + e.Data.Host, e.QuitMessage));
                    }
                }
            }
        }
        
        private void _OnDisconnected(object sender, EventArgs e)
        {
            foreach (ChatModel chat in _Session.Chats) {
                if (chat.NetworkManager == this) {
                    _Session.DisableChat(chat);
                }
            }
        }
        
        private void _OnAway(object sender, AwayEventArgs e)
        {
            ChatModel chat = _Session.GetChat(e.Who, ChatType.Person, NetworkProtocol.Irc, this);
            if (chat == null) {
                chat = _NetworkChat;
            }
            _Session.AddTextToChat(chat, "-!- " + String.Format(
                                                    _("{0} is away: {1}"),
                                                    e.Who, e.AwayMessage));
        }

        private void _OnUnAway(object sender, IrcEventArgs e)
        {
            _Session.AddTextToChat(_NetworkChat, "-!- " + _("You are no longer marked as being away"));
            _Session.UpdateNetworkStatus();
        }
        
        private void _OnNowAway(object sender, IrcEventArgs e)
        {
            _Session.AddTextToChat(_NetworkChat, "-!- " + _("You have been marked as being away"));
            _Session.UpdateNetworkStatus();
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
