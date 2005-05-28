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
using System.Threading;
using System.Collections;
using Meebey.SmartIrc4net;

namespace Meebey.Smuxi.Engine
{
    public class IrcManager : PermanentComponent, INetworkManager
    {
        private IrcClient       _IrcClient;
        private Session         _Session;
        private string          _Server;
        private int             _Port;
        private string[]        _Nicknames;
        private string          _Username;
        private string          _Password;
        private FrontendManager _FrontendManager;
        
        public bool IsConnected
        {
            get {
                if ((_IrcClient != null) &&
                    (_IrcClient.IsConnected)) {
                    return true;
                }
                return false;
            }
        }
    
        public IrcManager(Session session)
        {
            _IrcClient = new IrcClient();
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
            _Session = session;
        }
    
        public void Connect(FrontendManager fm, string server, int port, string[] nicks, string user, string pass)
        {
            _FrontendManager = fm;
            _Server = server;
            _Port = port;
            _Nicknames = nicks;
            _Username = user;
            _Password = pass;
            
            Thread thread = new Thread(new ThreadStart(_Connect));
            thread.IsBackground = true;
            thread.Name = "IrcManager ("+server+":"+port+")";
            thread.Start();
        }
        
        private void _Connect()
        {
            string msg;
            Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            msg = "Connecting to "+_Server+" port "+_Port+"...";
            _FrontendManager.SetStatus(msg);
            _Session.AddTextToPage(spage, "-!- "+msg);
            try {
                _IrcClient.Connect(_Server, _Port);
                _FrontendManager.UpdateNetworkStatus();
                msg = "Connection to "+_Server+" established";
                _FrontendManager.SetStatus(msg);
                _Session.AddTextToPage(spage, "-!- "+msg);
                _Session.AddTextToPage(spage, "-!- Logging in...");
                if (_Password != null) {
                    _IrcClient.RfcPass(_Password, Priority.Critical);
                }
                _IrcClient.Login(_Nicknames, (string)_Session.UserConfig["Connection/Realname"], 0, _Username);
                // TODO: make OnConnectCommands working
                /*
                char command_char = ((string)_Session.UserConfig["Interface/Entry/CommandCharacter"])[0];
                foreach (string command in ((string[])_Session.UserConfig["Interface/Connection/OnConnectCommands"])) {
                        if ((command.Length > 0) && 
                            (command[0] == command_char)) {
                            _Session.Command(command);
                        }
                }
                */
                
                try {
                    // TODO: we must handle this somehow
                    //while (true) {
                        _IrcClient.Listen();
                    //}
                } catch (Exception e) {
                    //Gnosmirc.GUI.Crash(e);
                    //Gnosmirc.Quit();
                    throw e;
                }
            } catch (CouldNotConnectException e) {
                _FrontendManager.SetStatus("Connection failed!");
                _Session.AddTextToPage(spage, "-!- Connection failed! Reason: "+e.Message);
            }
            
            // don't need the FrontendManager anymore
            _FrontendManager = null;
        }
        
        public void Disconnect(FrontendManager fm)
        {
            fm.SetStatus("Disconnecting...");
            Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            if (IsConnected) {
                _Session.AddTextToPage(spage, "-!- Disconnecting from "+_IrcClient.Address+"...");
                _IrcClient.Disconnect();
                fm.SetStatus("Disconnected from "+_IrcClient.Address);
                _Session.AddTextToPage(spage, "-!- Connection closed");
                // TODO: set someone else as current network manager?
            } else {
                fm.SetStatus("Not connected!");
                fm.AddTextToPage(spage, "-!- Not connected");
            }
            fm.UpdateNetworkStatus();
        }
        
        /* not sure if this method makes sense
        public void Reconnect(FrontendManager fm)
        {
            fm.SetStatus("Reconnecting...");
            Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            try {
                string msg;
                if (_IrcClient != null) {
                    _Session.AddTextToPage(spage, "-!- Reconnecting to "+_IrcClient.Address+"...");
                    _IrcClient.Reconnect(true);
                    msg = "Connection to "+_IrcClient.Address+" established";
                    fm.SetStatus(msg); 
                    _Session.AddTextToPage(spage, "-!- "+msg);
                } else {
                    fm.SetStatus("Reconnect Error");
                    _Session.AddTextToPage(spage, "-!- Reconnect Error");
                }
            } catch (ConnectionException) {
                fm.SetStatus("Not connected!");
                fm.AddTextToPage(spage, "-!- Not connected");
            }
            fm.UpdateNetworkStatus();
        }
        */
        
        public override string ToString()
        {
            string result = "IRC ";
            if (IsConnected) {
                result += _IrcClient.Address+":"+_IrcClient.Port;
            } else {
                result += "(not connected)";
            }
            return result;
        }
        
        public bool Command(FrontendManager fm, string data)
        {
            bool handled = false;
            string[] dataex = data.Split(new char[] {' '});
            string parameter = String.Join(" ", dataex, 1, dataex.Length-1);
            string command = (dataex[0].Length > 1) ? dataex[0].Substring(1).ToLower() : "";
            bool is_command = (data[0] == ((string)_Session.UserConfig["Interface/Entry/CommandCharacter"])[0]);
            if (IsConnected) {
                if (is_command) {
                    // commands which only work when we have a connection
                    switch (command) {
                        // commands which work on serverpage/channels/queries
                        case "j":
                        case "join":
                            _CommandJoin(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "query":
                        case "msg":
                            _CommandQuery(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "notice":
                            _CommandNotice(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "nick":
                            _CommandNick(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "raw":
                        case "quote":
                            _CommandRaw(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "ping":
                            _CommandPing(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "whois":
                            _CommandWhoIs(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "whowas":
                            _CommandWhoWas(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "away":
                            _CommandAway(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        // commands which only work on channels or queries
                        case "me":
                           _CommandMe(fm, data, dataex, parameter);
                            handled = true;
                           break;
                        // commands which only work on a channels
                        case "p":
                        case "part":
                            _CommandPart(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "topic":
                            _CommandTopic(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "cycle":
                        case "rejoin":
                            _CommandPart(fm, data, dataex, parameter);
                            _CommandJoin(fm, data,
                            new string[] {"", fm.CurrentPage.Name},
                            parameter);
                            handled = true;
                            break;
                        case "op":
                            _CommandOp(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "deop":
                            _CommandDeop(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "voice":
                            _CommandVoice(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "devoice":
                            _CommandDevoice(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "ban":
                            _CommandBan(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "unban":
                            _CommandUnban(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "kick":
                            _CommandKick(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "kickban":
                        case "kb":
                            _CommandKickban(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "mode":
                            _CommandMode(fm, data, dataex, parameter);
                            handled = true;
                            break;
                        case "invite":
                            _CommandInvite(fm, data, dataex, parameter);
                            handled = true;
                            break;
                    }
                } else {
                    // normal text
                    if (fm.CurrentPage.PageType == PageType.Server) {
                        // we are on the server page
                        _IrcClient.WriteLine(data);
                    } else {
                        // we are on a channel or query page
                        _IrcClient.SendMessage(SendType.Message, fm.CurrentPage.Name,
                            data);
                        _Session.AddTextToPage(fm.CurrentPage, "<"+_IrcClient.Nickname+"> "+data);
                    }
                    handled = true;
                }
            } else {
                if (is_command) {
                    // commands which work even without beeing connected
                } else {
                    // normal text, without connection
                    _CommandNotConnected(fm, data, dataex, parameter);
                    handled = true;
                }
            }
            
            return handled;
        }
        
        private void _CommandJoin(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            string channel = null;
            if ((dataex.Length >= 2) &&
                (dataex[1].Length >= 1)) {
                switch (dataex[1][0]) {
                    case '#':
                    case '!':
                    case '+':
                    case '&':
                        channel = dataex[1];
                        break;
                    default:
                        channel = "#"+dataex[1];  
                        break;
                }
            }
        
            if (dataex.Length == 2) {
                _IrcClient.RfcJoin(channel);
            } else if (dataex.Length > 2) {
                _IrcClient.RfcJoin(channel, dataex[2]);
            }
        }
        
        private void _CommandQuery(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 2) {
                string nickname = dataex[1];
                Page page = _Session.GetPage(nickname, PageType.Query, NetworkType.Irc, this);
                if (page == null) {
                    page = new Page(nickname, PageType.Query, NetworkType.Irc, this);
                    _Session.AddPage(page);
                }
            }
            
            if (dataex.Length >= 3) {
                string message = String.Join(" ", dataex, 2, dataex.Length-2);
                string nickname = dataex[1];
                Page page = _Session.GetPage(nickname, PageType.Query, NetworkType.Irc, this);
                _IrcClient.SendMessage(SendType.Message, nickname, message);
                _Session.AddTextToPage(page, "<"+_IrcClient.Nickname+"> "+message);
            }
        }
        
        private void _CommandPart(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if ((dataex.Length >= 2) &&
                (dataex[1].Length >= 1)) {
                // have to guess here if we got a channel passed or not
                switch (dataex[1][0]) {
                    case '#':
                    case '&':
                    case '!':
                    case '+':
                        // seems to be a channel
                        string[] channels = dataex[1].Split(new char[] {','});
                        string message = null;
                        if  (dataex.Length >= 3) {
                            message = String.Join(" ", dataex, 2, dataex.Length-2);
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
                        _IrcClient.RfcPart(dataex[1], parameter);
                        break;
                }
            } else {
                Page page = fm.CurrentPage;
                _IrcClient.RfcPart(page.Name);
            }
        }
        
        private void _CommandAway(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 2) {
                _IrcClient.RfcAway(parameter);
            } else {
                _IrcClient.RfcAway();
            }
        }
        
        private void _CommandPing(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 2) {
                string destination = dataex[1];
                string timestamp = DateTime.Now.ToFileTime().ToString();
                Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
                _Session.AddTextToPage(spage, "[ctcp("+destination+")] PING "+timestamp);
                _IrcClient.SendMessage(SendType.CtcpRequest, destination, "PING "+timestamp);
            } else {
                fm.AddTextToCurrentPage("-!- Not enough parameters for ping command");
            }
        }
        
        private void _CommandWhoIs(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 2) {
                _IrcClient.RfcWhois(dataex[1]);
            } else {
                fm.AddTextToCurrentPage("-!- Not enough parameters for whois command");
            }
        }
        
        private void _CommandWhoWas(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 2) {
                _IrcClient.RfcWhowas(dataex[1]);
            } else {
                fm.AddTextToCurrentPage("-!- Not enough parameters for whowas command");
            }
        }
        
        private void _CommandTopic(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length >= 2) {
                _IrcClient.RfcTopic(channel, parameter);
            } else {
                if (_IrcClient.IsJoined(channel)) {
                    string topic = _IrcClient.GetChannel(channel).Topic;
                    if (topic.Length > 0) {
                        fm.AddTextToPage(page,
                            "-!- Topic for "+channel+": "+topic);
                    } else {
                        fm.AddTextToPage(page,
                        "-!- No topic set for "+channel);
                    }
                }
            }
        }
        
        private void _CommandOp(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length == 2) {
                _IrcClient.Op(channel, parameter);
            } else if (dataex.Length > 2) {
                string[] candidates = parameter.Split(new char[] {' '});
                foreach (string nick in candidates) {
                    _IrcClient.Op(channel, nick);
                }
            }
        }
    
        private void _CommandDeop(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length == 2) {
                _IrcClient.Deop(channel, parameter);
            } else if (dataex.Length > 2) {
                string[] candidates = parameter.Split(new char[] {' '});
                foreach(string nick in candidates) {
                    _IrcClient.Deop(channel, nick);
                }
            }
        }

        private void _CommandVoice(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length == 2) {
                _IrcClient.Voice(channel, parameter);
            } else if (dataex.Length > 2) {
                string[] candidates = parameter.Split(new char[] {' '});
                foreach (string nick in candidates) {
                    _IrcClient.Voice(channel, nick);
                }
            }
        }

        private void _CommandDevoice(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length == 2) {
                _IrcClient.Devoice(channel, parameter);
            } else if (dataex.Length > 2) {
                string[] candidates = parameter.Split(new char[] {' '});
                foreach (string nick in candidates) {
                    _IrcClient.Devoice(channel, nick);
                }
            }
        }

        private void _CommandBan(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length == 2) {
                _IrcClient.Ban(channel, parameter);
            } else if (dataex.Length > 2) {
                string[] candidates = parameter.Split(new char[] {' '});
                foreach(string nick in candidates) {
                    _IrcClient.Ban(channel, nick);
                }
            }
        }

        private void _CommandUnban(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length == 2) {
                _IrcClient.Unban(channel, parameter);
            } else if (dataex.Length > 2) {
                string[] candidates = parameter.Split(new char[] {' '});
                foreach (string nick in candidates) {
                    _IrcClient.Unban(channel, nick);
                }
            }
        }

        private void _CommandKick(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length >= 2) {
                string[] candidates = dataex[1].Split(new char[] {','});
                if (dataex.Length >= 3) {
                    string reason = String.Join(" ", dataex, 2, dataex.Length-2);  
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

        private void _CommandKickban(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            SmartIrc4net.IrcUser ircuser;
            if (dataex.Length >= 2) {
                string[] candidates = dataex[1].Split(new char[] {','});
                if (dataex.Length >= 3) {
                    string reason = String.Join(" ", dataex, 2, dataex.Length-2);  
                    foreach (string nick in candidates) {
                        ircuser = _IrcClient.GetIrcUser(nick);
                        if (ircuser != null) {
                            _IrcClient.Ban(channel, "*!*"+ircuser.Ident+"@"+ircuser.Host);
                            _IrcClient.RfcKick(channel, nick, reason);
                        }
                    }
                } else {
                    foreach (string nick in candidates) {
                        ircuser = _IrcClient.GetIrcUser(nick);
                        if (ircuser != null) {
                            _IrcClient.Ban(channel, "*!*"+ircuser.Ident+"@"+ircuser.Host);
                            _IrcClient.RfcKick(channel, nick);
                        }
                    }
                }
            }
        }

        private void _CommandMode(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            if (dataex.Length >= 2) {
                if (page.PageType == PageType.Server) {
                    _IrcClient.RfcMode(_IrcClient.Nickname, parameter);
                } else {
                    string channel = page.Name;
                    _IrcClient.RfcMode(channel, parameter);
                }
            }
        }

        private void _CommandInvite(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length >= 2) {
                if (!_IrcClient.IsJoined(channel, dataex[1])) {
                    _IrcClient.RfcInvite(dataex[1], channel);
                    fm.AddTextToPage(page, "-!- Inviting "+dataex[1]+" to "+channel);
                } else {
                    fm.AddTextToPage(page, "-!- "+dataex[1]+" is already on channel");
                }
            }
        }

        private void _CommandRaw(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            _IrcClient.WriteLine(parameter);
        }
    
        private void _CommandMe(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            Page page = fm.CurrentPage;
            string channel = page.Name;
            if (dataex.Length >= 2) {
                _IrcClient.SendMessage(SendType.Action, channel, parameter);
                _Session.AddTextToPage(page, " * "+_IrcClient.Nickname+" "+parameter);
            }
        }
    
        private void _CommandNotice(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 3) {
                string target = dataex[1];
                string message = String.Join(" ", dataex, 2, dataex.Length-2);  
                _IrcClient.SendMessage(SendType.Notice, target, message);
                if (_IrcClient.IsJoined(target)) {
                    Page page = _Session.GetPage(target, PageType.Query, NetworkType.Irc, this);
                    _Session.AddTextToPage(page, "[notice("+target+")] "+message);
                } else {
                    Page page = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
                    _Session.AddTextToPage(page, "[notice("+target+")] "+message);
                }
            }
        }
    
        private void _CommandNick(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            if (dataex.Length >= 2) {
                _IrcClient.RfcNick(parameter);
            }
        }
    
        private void _CommandNotConnected(FrontendManager fm, string data, string[] dataex, string parameter)
        {
            fm.AddTextToCurrentPage("-!- Not connected to server");
        }
        
        private void _OnRawMessage(object sender, IrcEventArgs e)
        {
            Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
    	    if (e.Data.Message != null) {
                switch (e.Data.Type) {
                    case ReceiveType.Error:
                    case ReceiveType.Info:
                    case ReceiveType.Invite:
                    case ReceiveType.List:
                    case ReceiveType.Login:
                    case ReceiveType.Motd:
                        _Session.AddTextToPage(spage, e.Data.Message);
                        break;
                    case ReceiveType.WhoIs:
                        _OnReceiveTypeWhois(e);
                        break;
                    case ReceiveType.WhoWas:
                        _OnReceiveTypeWhowas(e);
                        break;
                }
            }
            
            switch (e.Data.ReplyCode) {
                case ReplyCode.ErrorNoSuchNickname:
                    string nick = e.Data.RawMessageArray[3];
                    string msg = "-!- "+nick+": No such nick/channel";
                    Page page = _Session.GetPage(nick, PageType.Query, NetworkType.Irc, this);
                    if (page != null) {
                        _Session.AddTextToPage(page, msg);
                    } else {
                        _Session.AddTextToPage(spage, msg);
                    }
                    break;
            }    
        }
        
        private void _OnReceiveTypeWhois(IrcEventArgs e)
        {
            string nick = e.Data.RawMessageArray[3];
            Page page = _Session.GetPage(nick, PageType.Query, NetworkType.Irc, this);
            if (page == null) {
                page = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            }
            switch (e.Data.ReplyCode) {
                case ReplyCode.WhoIsUser:
                    string ident = e.Data.RawMessageArray[4];
                    string host = e.Data.RawMessageArray[5];
                    string realname = e.Data.Message;
                    _Session.AddTextToPage(page, "-!- "+nick+" ["+ident+"@"+host+"]");
                    _Session.AddTextToPage(page, "-!-  realname: "+realname);
                    break;
                case ReplyCode.WhoIsServer:
                    string server = e.Data.RawMessageArray[4];
                    string serverinfo = e.Data.Message;
                    _Session.AddTextToPage(page, "-!-  server: "+server+" ["+serverinfo+"]");
                    break;
                case ReplyCode.WhoIsIdle:
                    string idle = e.Data.RawMessageArray[4];
                    try {
                        long timestamp = Int64.Parse(e.Data.RawMessageArray[5]);
                        DateTime signon =  new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        signon = signon.AddSeconds(timestamp).ToLocalTime();
                        _Session.AddTextToPage(page, "-!-  idle: "+idle+" [signon: "+signon.ToString()+"]");
                    } catch (FormatException) {
                    }
                    break;
                case ReplyCode.WhoIsChannels:
                    string channels = e.Data.Message;
                    _Session.AddTextToPage(page, "-!-  channels: "+channels);
                    break;
                case ReplyCode.WhoIsOperator:
                    _Session.AddTextToPage(page, "-!-  "+e.Data.Message);
                    break;
                case ReplyCode.EndOfWhoIs:
                    _Session.AddTextToPage(page, "-!-  "+e.Data.Message);
                    break;
            }
        }
        
        private void _OnReceiveTypeWhowas(IrcEventArgs e)
        {
            string nick = e.Data.RawMessageArray[3];
            Page page = _Session.GetPage(nick, PageType.Query, NetworkType.Irc, this);
            if (page == null) {
                page = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            }
            switch (e.Data.ReplyCode) {
                case ReplyCode.WhoWasUser:
                    string ident = e.Data.RawMessageArray[4];
                    string host = e.Data.RawMessageArray[5];
                    string realname = e.Data.Message;
                    _Session.AddTextToPage(page, "-!- "+nick+" ["+ident+"@"+host+"]");
                    _Session.AddTextToPage(page, "-!-  realname: "+realname);
                    break;
                case ReplyCode.EndOfWhoWas:
                    _Session.AddTextToPage(page, "-!-  "+e.Data.Message);
                    break;
            }
        }
        
        private void _OnCtcpRequest(object sender, CtcpEventArgs e)
        {
            Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            _Session.AddTextToPage(spage, e.Data.Nick+" ["+e.Data.Ident+"@"+e.Data.Host+"] requested CTCP "+e.CtcpCommand+" from "+_IrcClient.Nickname+": "+e.CtcpParameter);
        }
        
        private void _OnCtcpReply(object sender, CtcpEventArgs e)
        {
            Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            if (e.CtcpCommand == "PING") {
                try {
                    long timestamp = Int64.Parse(e.CtcpParameter);
                    DateTime sent = DateTime.FromFileTime(timestamp);
                    string duration = DateTime.Now.Subtract(sent).TotalSeconds.ToString();
                    _Session.AddTextToPage(spage, "CTCP PING reply from "+e.Data.Nick+": "+duration+" seconds");
                } catch (FormatException) {
                }
            }
        }
        
        private void _OnChannelMessage(object sender, IrcEventArgs e)
        {
            Page page = _Session.GetPage(e.Data.Channel, PageType.Channel, NetworkType.Irc, this);
            _Session.AddTextToPage(page, "<"+e.Data.Nick+"> "+e.Data.Message);
        }
        
        private void _OnChannelAction(object sender, ActionEventArgs e)
        {
            Page page = _Session.GetPage(e.Data.Channel, PageType.Channel, NetworkType.Irc, this);
            _Session.AddTextToPage(page, " * "+e.Data.Nick+" "+e.ActionMessage);
        }
        
        private void _OnChannelNotice(object sender, IrcEventArgs e)
        {
            Page page = _Session.GetPage(e.Data.Channel, PageType.Channel, NetworkType.Irc, this);
            _Session.AddTextToPage(page, "-"+e.Data.Nick+":"+e.Data.Channel+"- "+e.Data.Message);
        }
        
        private void _OnQueryMessage(object sender, IrcEventArgs e)
        {
            Page page = _Session.GetPage(e.Data.Nick, PageType.Query, NetworkType.Irc, this);
            if (page == null) {
                page = new Page(e.Data.Nick, PageType.Query, NetworkType.Irc, this);
                _Session.AddPage(page);
            }
            
            _Session.AddTextToPage(page, "<"+e.Data.Nick+"> "+e.Data.Message);
        }
        
        private void _OnQueryAction(object sender, ActionEventArgs e)
        {
            Page page = _Session.GetPage(e.Data.Nick, PageType.Query, NetworkType.Irc, this);
            if (page == null) {
                page = new Page(e.Data.Nick, PageType.Query, NetworkType.Irc, this);
                _Session.AddPage(page);
            }
            
            _Session.AddTextToPage(page, " * "+e.Data.Nick+" "+e.ActionMessage);
        }
        
        private void _OnQueryNotice(object sender, IrcEventArgs e)
        {
            Page page = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            _Session.AddTextToPage(page,
                "-"+e.Data.Nick+"("+e.Data.Ident+"@"+e.Data.Host+")- "+e.Data.Message);
        }
        
        private void _OnJoin(object sender, JoinEventArgs e)
        {
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Channel, PageType.Channel, NetworkType.Irc, this);
            if (e.Data.Irc.IsMe(e.Who)) {
                cpage = new ChannelPage(e.Channel, NetworkType.Irc, this);
                _Session.AddPage(cpage);
            } else {
                // someone else joined, let's add him to the channel page
                SmartIrc4net.IrcUser siuser = _IrcClient.GetIrcUser(e.Who);
                IrcChannelUser icuser = new IrcChannelUser(e.Who, siuser.Realname,
                                        siuser.Ident, siuser.Host);
                _Session.AddUserToChannel(cpage, icuser);
            }
            
            _Session.AddTextToPage(cpage,
                "-!- "+e.Who+" ["+e.Data.Ident+"@"+e.Data.Host+"] has joined "+e.Channel);
        }
        
        private void _OnNames(object sender, NamesEventArgs e)
        {
#if LOG4NET
            Logger.IrcManager.Debug("_OnNames() e.Channel: "+e.Channel);
#endif
        }
        
        private void _OnChannelActiveSynced(object sender, IrcEventArgs e)
        {
#if LOG4NET
            Logger.IrcManager.Debug("_OnChannelActiveSynced() e.Data.Channel: "+e.Data.Channel);
#endif
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Data.Channel, PageType.Channel, NetworkType.Irc, this);
            SmartIrc4net.Channel schan = _IrcClient.GetChannel(e.Data.Channel);
            foreach (ChannelUser scuser in schan.Users.Values) {
                IrcChannelUser icuser = new IrcChannelUser(scuser.Nick, scuser.Realname,
                                            scuser.Ident, scuser.Host);
                if (scuser.IsOp) {
                    icuser.IsOp = scuser.IsOp;
                }
                if (scuser.IsVoice) {
                    icuser.IsVoice = scuser.IsVoice;
                }
                _Session.AddUserToChannel(cpage, icuser);
            }
        }
        
        private void _OnPart(object sender, PartEventArgs e)
        {
#if LOG4NET
            Logger.IrcManager.Debug("_OnPart() e.Channel: "+e.Channel+" e.Who: "+e.Who);
#endif
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Channel, PageType.Channel, NetworkType.Irc, this);
            if (e.Data.Irc.IsMe(e.Who)) {
                _Session.RemovePage(cpage);
            } else {
                User user = cpage.GetUser(e.Who);
                _Session.RemoveUserFromChannel(cpage, user);
                _Session.AddTextToPage(cpage,
                    "-!- "+e.Who+" ["+e.Data.Ident+"@"+e.Data.Host+"] has left "+e.Channel+" ["+e.PartMessage+"]");
            }
        }
        
        private void _OnKick(object sender, KickEventArgs e)
        {
#if LOG4NET
            Logger.IrcManager.Debug("_OnKick() e.Channel: "+e.Channel+" e.Whom: "+e.Whom);
#endif
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Channel, PageType.Channel, NetworkType.Irc, this);
            if (e.Data.Irc.IsMe(e.Whom)) {
                Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
                _Session.RemovePage(cpage);
                _Session.AddTextToPage(spage,
                    "-!- You was kicked from "+e.Channel+" by "+e.Who+" ["+e.KickReason+"]");
            } else {
                User user = cpage.GetUser(e.Who);
                _Session.RemoveUserFromChannel(cpage, user);
                _Session.AddTextToPage(cpage,
                    "-!- "+e.Whom+" was kicked from "+e.Channel+" by "+e.Who+" ["+e.KickReason+"]");
            }
        }
        
        private void _OnNickChange(object sender, NickChangeEventArgs e)
        {
#if LOG4NET
            Logger.IrcManager.Debug("_OnNickChange() e.OldNickname: "+e.OldNickname+" e.NewNickname: "+e.NewNickname);
#endif
            if (e.Data.Irc.IsMe(e.NewNickname)) {
                Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
                _Session.AddTextToPage(spage, "-!- You're now known as "+e.NewNickname);
            }
            
            SmartIrc4net.IrcUser ircuser = e.Data.Irc.GetIrcUser(e.NewNickname);
            if (ircuser != null) {
                foreach (string channel in ircuser.JoinedChannels) {
                    ChannelPage cpage = (ChannelPage)_Session.GetPage(channel, PageType.Channel, NetworkType.Irc, this);
                    
                    // clone the old user to a new user
                    IrcChannelUser olduser = (IrcChannelUser)cpage.GetUser(e.OldNickname);
                    IrcChannelUser newuser = new IrcChannelUser(e.NewNickname, ircuser.Realname,
                                        ircuser.Ident, ircuser.Host);
                    newuser.IsOp = olduser.IsOp;
                    newuser.IsVoice = olduser.IsVoice;
                    
                    _Session.UpdateUserInChannel(cpage, olduser, newuser);
                    
                    if (e.Data.Irc.IsMe(e.NewNickname)) {
                        _Session.AddTextToPage(cpage, "-!- You're now known as "+e.NewNickname);
                    } else {
                        _Session.AddTextToPage(cpage, "-!- "+e.OldNickname+" is now known as "+e.NewNickname);
                    }
                }
            }
        }
        
        private void _OnTopic(object sender, TopicEventArgs e)
        {
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Channel, PageType.Channel, NetworkType.Irc, this);
            _Session.UpdateTopicInChannel(cpage, e.Topic);
        }
        
        private void _OnTopicChange(object sender, TopicChangeEventArgs e)
        {
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Channel, PageType.Channel, NetworkType.Irc, this);
            _Session.UpdateTopicInChannel(cpage, e.NewTopic);
            _Session.AddTextToPage(cpage, "-!- "+e.Who+" changed the topic of "+e.Channel+" to: "+e.NewTopic);
        }
        
        private void _OnOp(object sender, OpEventArgs e)
        {
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Channel, PageType.Channel, NetworkType.Irc, this);
            IrcChannelUser user = (IrcChannelUser)cpage.GetUser(e.Whom);
            user.IsOp = true;
            _Session.UpdateUserInChannel(cpage, user, user);
        }
        
        private void _OnDeop(object sender, DeopEventArgs e)
        {
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Channel, PageType.Channel, NetworkType.Irc, this);
            IrcChannelUser user = (IrcChannelUser)cpage.GetUser(e.Whom);
            user.IsOp = false;
            _Session.UpdateUserInChannel(cpage, user, user);
        }
        
        private void _OnVoice(object sender, VoiceEventArgs e)
        {
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Channel, PageType.Channel, NetworkType.Irc, this);
            IrcChannelUser user = (IrcChannelUser)cpage.GetUser(e.Whom);
            user.IsVoice = true;
            _Session.UpdateUserInChannel(cpage, user, user);
        }
        
        private void _OnDevoice(object sender, DevoiceEventArgs e)
        {
            ChannelPage cpage = (ChannelPage)_Session.GetPage(e.Channel, PageType.Channel, NetworkType.Irc, this);
            IrcChannelUser user = (IrcChannelUser)cpage.GetUser(e.Whom);
            user.IsVoice = false;
            _Session.UpdateUserInChannel(cpage, user, user);
        }
        
        private void _OnModeChange(object sender, IrcEventArgs e)
        {
            string modechange;
            switch (e.Data.Type) {
                case ReceiveType.UserModeChange:
                    modechange = e.Data.Message;
                    Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
                    _Session.AddTextToPage(spage, "-!- Mode change ["+modechange+"] for user "+e.Data.Irc.Nickname);
                break;
                case ReceiveType.ChannelModeChange:
                    modechange = String.Join(" ", e.Data.RawMessageArray, 3, e.Data.RawMessageArray.Length-3);
                    string who;
                    if (e.Data.Nick != null && e.Data.Nick.Length > 0) {
                        who = e.Data.Nick;
                    } else {
                        who = e.Data.Host;
                    }
                    Page page = _Session.GetPage(e.Data.Channel, PageType.Channel, NetworkType.Irc, this);
                    _Session.AddTextToPage(page, "-!- mode/"+e.Data.Channel+" ["+modechange+"] by "+who);
                break;
            }
        }
        
        private void _OnQuit(object sender, QuitEventArgs e)
        {
#if LOG4NET
            Logger.IrcManager.Debug("_Quit() e.Who: "+e.Who);
#endif
            if (e.Data.Irc.IsMe(e.Who)) {
                // _OnDisconnect() handles this
            } else {
                foreach (Page page in _Session.Pages) {
                    if (page.PageType == PageType.Channel) {
                        ChannelPage cpage = (ChannelPage)page;
                        User user = cpage.GetUser(e.Who);
                        _Session.RemoveUserFromChannel(cpage, user);
                        _Session.AddTextToPage(cpage, "-!- "+e.Who+" ["+e.Data.Ident+"@"+e.Data.Host+"] has quit ["+e.QuitMessage+"]");
                    } else if ((page.PageType == PageType.Query) &&
                               (page.Name == e.Who)) {
                        _Session.AddTextToPage(page, "-!- "+e.Who+" ["+e.Data.Ident+"@"+e.Data.Host+"] has quit ["+e.QuitMessage+"]");
                    }
                }
            }
        }
        
        private void _OnDisconnected(object sender, EventArgs e)
        {
            // we can't delete directly, it will break the enumerator, let's use a list
            ArrayList removelist = new ArrayList();
            foreach (Page page in _Session.Pages) {
                if (page.NetworkManager == this) {
                    removelist.Add(page);
                }
            }
            
            // now we can delete
            foreach (Page page in removelist) {
                _Session.RemovePage(page);
            }
        }
        
        private void _OnAway(object sender, AwayEventArgs e)
        {
            Page page = _Session.GetPage(e.Who, PageType.Query, NetworkType.Irc, this);
            if (page == null) {
                page = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            }
            _Session.AddTextToPage(page, "-!- "+e.Who+" is away: "+e.AwayMessage);
        }

        private void _OnUnAway(object sender, IrcEventArgs e)
        {
            Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            _Session.AddTextToPage(spage, "-!- You are no longer marked as being away");
        }
        
        private void _OnNowAway(object sender, IrcEventArgs e)
        {
            Page spage = _Session.GetPage("Server", PageType.Server, NetworkType.Irc, null);
            _Session.AddTextToPage(spage, "-!- You have been marked as being away");
        }
    }
}
