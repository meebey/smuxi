/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2011 Mirco Bauer <meebey@meebey.net>
 * Copyright (c) 2011 Tuukka Hastrup <Tuukka.Hastrup@iki.fi>
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
using System.Net.Security;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using jabber;
using jabber.client;
using jabber.connection;
using jabber.protocol;
using jabber.protocol.client;
using jabber.protocol.iq;
using XmppMessageType = jabber.protocol.client.MessageType;
using XmppProxyType = jabber.connection.ProxyType;

using Smuxi.Common;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "jabber")]
    public class JabberProtocolManager : XmppProtocolManager
    {
        public JabberProtocolManager(Session session) : base(session)
        {
        }
    }
    
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "xmpp")]
    public class XmppProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private JabberClient    _JabberClient;
        private RosterManager   _RosterManager;
        private ConferenceManager _ConferenceManager;
        private FrontendManager _FrontendManager;
        private ChatModel       _NetworkChat;
        private PresenceManager _PresenceManager;

        PersonModel MyPerson { get; set; }

        public override string NetworkID {
            get {
                if (!String.IsNullOrEmpty(_JabberClient.NetworkHost)) {
                    return _JabberClient.NetworkHost;
                }
                if (!String.IsNullOrEmpty(_JabberClient.Server)) {
                    return _JabberClient.Server;
                }
                return "XMPP";
            }
        }
        
        public override string Protocol {
            get {
                return "XMPP";
            }
        }
        
        public override ChatModel Chat {
            get {
                return _NetworkChat;
            }
        }

        public XmppProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);

            _JabberClient = new JabberClient();
            _JabberClient.Resource = "Smuxi";
            _JabberClient.AutoLogin = true;
            _JabberClient.AutoPresence = false;
            _JabberClient.OnStreamInit += OnStreamInit;
            _JabberClient.OnMessage += OnMessage;
            _JabberClient.OnConnect += OnConnect;
            _JabberClient.OnDisconnect += OnDisconnect;
            _JabberClient.OnAuthenticate += OnAuthenticate;
            _JabberClient.OnError += OnError;
            _JabberClient.OnProtocol += OnProtocol;
            _JabberClient.OnWriteText += OnWriteText;
            _JabberClient.OnIQ += OnIQ;

            _RosterManager = new RosterManager();
            _RosterManager.Stream = _JabberClient;

            _PresenceManager = new PresenceManager();
            _PresenceManager.Stream = _JabberClient;

            _ConferenceManager = new ConferenceManager();
            _ConferenceManager.Stream = _JabberClient;
            _ConferenceManager.OnJoin += OnJoin;
            _ConferenceManager.OnLeave += OnLeave;
            _ConferenceManager.OnParticipantJoin += OnParticipantJoin;
            _ConferenceManager.OnParticipantLeave += OnParticipantLeave;
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
            Host = server.Hostname;
            Port = server.Port;

            ApplyConfig(Session.UserConfig, server);

            // TODO: use config for single network chat or once per network manager
            _NetworkChat = Session.CreateChat<ProtocolChatModel>(
                NetworkID, "Jabber " + Host, this
            );
            Session.AddChat(_NetworkChat);
            Session.SyncChat(_NetworkChat);

            if (!String.IsNullOrEmpty(_JabberClient.ProxyHost)) {
                var builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(_("Using proxy: {0}:{1}"),
                                   _JabberClient.ProxyHost,
                                   _JabberClient.ProxyPort);
                Session.AddMessageToChat(Chat, builder.ToMessage());
            }
            _JabberClient.Connect();
        }
        
        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            _JabberClient.Close();
            _JabberClient.Connect();
        }
        
        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            _JabberClient.Close(false);
        }

        public override void Dispose()
        {
            Trace.Call();

            base.Dispose();

            _JabberClient.Dispose();
        }

        public override string ToString()
        {
            string result = "Jabber ";
            if (_JabberClient != null) {
                result += _JabberClient.Server + ":" + _JabberClient.Port;
            }
            
            if (!IsConnected) {
                result += " (" + _("not connected") + ")";
            }
            return result;
        }
        
        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);
            
            throw new NotImplementedException();
        }

        public override void OpenChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);
            
            throw new NotImplementedException();
        }

        public override void CloseChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);

            if (chat.ChatType == ChatType.Group) {
                _ConferenceManager.GetRoom(chat.ID+"/"+_JabberClient.User).Leave("Closed");
            } else {
                Session.RemoveChat(chat);
            }
        }

        public override void SetPresenceStatus(PresenceStatus status,
                                               string message)
        {
            Trace.Call(status, message);

            if (!IsConnected || !_JabberClient.IsAuthenticated) {
                return;
            }

            PresenceType? xmppType = null;
            string xmppShow = null;
            switch (status) {
                case PresenceStatus.Online:
                    xmppType = PresenceType.available;
                    break;
                case PresenceStatus.Away:
                    xmppType = PresenceType.available;
                    xmppShow = "away";
                    break;
                case PresenceStatus.Offline:
                    xmppType = PresenceType.unavailable;
                    break;
            }
            if (xmppType == null) {
                return;
            }

            _JabberClient.Presence(xmppType.Value, message, xmppShow,
                                   _JabberClient.Priority);
        }

        public override bool Command(CommandModel command)
        {
            bool handled = false;
            if (IsConnected) {
                if (command.IsCommand) {
                    switch (command.Command) {
                        case "help":
                            CommandHelp(command);
                            handled = true;
                            break;
                        case "msg":
                        case "query":
                            CommandMessageQuery(command);
                            handled = true;
                            break;
                        case "say":
                            CommandSay(command);
                            handled = true;
                            break;
                        case "join":
                            CommandJoin(command);
                            handled = true;
                            break;
                        case "part":
                        case "leave":
                            CommandPart(command);
                            handled = true;
                            break;
                        case "away":
                            CommandAway(command);
                            handled = true;
                            break;
                        case "roster":
                            CommandRoster(command);
                            handled = true;
                            break;
                    }
                } else {
                    _Say(command.Chat, command.Data);
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
                    NotConnected(command);
                    handled = true;
                }
            }
            
            return handled;
        }

        public void CommandHelp(CommandModel cmd)
        {
            var builder = CreateMessageBuilder();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            builder.AppendHeader(_("XMPP Commands"));
            cmd.FrontendManager.AddMessageToChat(cmd.Chat, builder.ToMessage());

            string[] help = {
            "help",
            "connect xmpp/jabber server port username password [resource]",
            "msg/query jid message",
            "say message",
            "join muc-jid",
            "part/leave [muc-jid]",
            "away [away-message]"
            };
            
            foreach (string line in help) { 
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                cmd.FrontendManager.AddMessageToChat(cmd.Chat, builder.ToMessage());
            }
        }
        
        public void CommandConnect(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            
            var server = new XmppServerModel();
            if (cd.DataArray.Length >= 3) {
                server.Hostname = cd.DataArray[2];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            if (cd.DataArray.Length >= 4) {
                try {
                    server.Port = Int32.Parse(cd.DataArray[3]);
                } catch (FormatException) {
                    fm.AddTextToChat(
                        cd.Chat,
                        "-!- " + String.Format(
                                    _("Invalid port: {0}"),
                                    cd.DataArray[3]));
                    return;
                }
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            if (cd.DataArray.Length >= 5) {
                server.Username = cd.DataArray[4];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            if (cd.DataArray.Length >= 6) {
                server.Password = cd.DataArray[5];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            if (cd.DataArray.Length >= 7) {
                server.Resource = cd.DataArray[6];
            }

            Connect(fm, server);
        }
        
        public void CommandMessageQuery(CommandModel cd)
        {
            ChatModel chat = null;
            if (cd.DataArray.Length >= 2) {
                string nickname = cd.DataArray[1];
                JID jid = null;
                foreach (JID j in _RosterManager) {
                    Item item = _RosterManager[j];
                    if (item.Nickname != null &&
                        item.Nickname.Replace(" ", "_") == nickname) {
                        jid = item.JID;
                        break;
                    }
                }
                if (jid == null) {
                    jid = nickname; // TODO check validity
                }

                chat = GetChat(jid, ChatType.Person);
                if (chat == null) {
                    PersonModel person = new PersonModel(jid, nickname,
                                                         NetworkID, Protocol,
                                                         this);
                    chat = Session.CreatePersonChat(person, jid, nickname, this);
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
        
        public void CommandJoin(CommandModel cd)
        {
            if (cd.DataArray.Length < 2) {
                NotEnoughParameters(cd);
                return;
            }

            string jid = cd.DataArray[1];
            ChatModel chat = GetChat(jid, ChatType.Group);
            if (chat == null) {
                _ConferenceManager.GetRoom(jid+"/"+_JabberClient.User).Join();
            }
        }

        public void CommandPart(CommandModel cd)
        {
            string jid;
            if (cd.DataArray.Length >= 2)
                jid = cd.DataArray[1];
            else
                jid = cd.Chat.ID;
            ChatModel chat = GetChat(jid, ChatType.Group);
            if (chat != null) {
                _ConferenceManager.GetRoom(jid+"/"+_JabberClient.User).Leave("Part");
            }
        }

        public void CommandAway(CommandModel cd)
        {
            if (cd.DataArray.Length >= 2) {
                SetPresenceStatus(PresenceStatus.Away, cd.Parameter);
            } else {
                SetPresenceStatus(PresenceStatus.Online, null);
            }
        }

        public void CommandRoster(CommandModel cd)
        {
            bool full = false;
            if (cd.Parameter == "full") {
                full = true;
            }

            MessageBuilder builder = CreateMessageBuilder();
            builder.AppendHeader("Roster");
            cd.FrontendManager.AddMessageToChat(cd.Chat, builder.ToMessage());

            foreach (JID j in _RosterManager) {
                string status = "+";
                if (!_PresenceManager.IsAvailable(j)) {
                    if (!full) continue;
                    status = "-";
                }
                string nick = _RosterManager[j].Nickname;
                string mesg = "";
                Presence item = _PresenceManager[j];
                if (item != null) {
                    if (item.Show != null && item.Show.Length != 0) {
                        status = item.Show;
                    }
                    mesg = item.Status;
                }
                builder = CreateMessageBuilder();
                builder.AppendText("{0}\t{1}\t({2}): {3}", status, nick, j, mesg);
                cd.FrontendManager.AddMessageToChat(cd.Chat, builder.ToMessage());
            }
        }

        public void CommandSay(CommandModel cd)
        {
            _Say(cd.Chat, cd.Parameter);
        }  
        
        private void _Say(ChatModel chat, string text)
        {
            _Say(chat, text, true);
        }

        private void _Say(ChatModel chat, string text, bool send)
        {
            if (!chat.IsEnabled) {
                return;
            }
            
            if (send) {
                string target = chat.ID;
                if (chat.ChatType == ChatType.Person) {
                    _JabberClient.Message(target, text);
                } else if (chat.ChatType == ChatType.Group) {
                    var room = _ConferenceManager.GetRoom(
                        String.Format(
                            "{0}/{1}",
                            target, _JabberClient.User
                        )
                    );
                    room.PublicMessage(text);
                    return; // don't show now. the message will be echoed back if it's sent successfully
                }
            }

            var builder = CreateMessageBuilder();
            builder.AppendSenderPrefix(MyPerson);
            builder.AppendMessage(text);
            Session.AddMessageToChat(chat, builder.ToMessage());
        }
        
        void OnStreamInit(object sender, ElementStream stream)
        {
            Trace.Call(sender, stream);

            stream.AddType("own-message", "http://www.facebook.com/xmpp/messages", typeof(OwnMessageQuery));
        }

        void OnProtocol(object sender, XmlElement tag)
        {
            if (!DebugProtocol) {
                return;
            }

            try {
                var strWriter = new StringWriter();
                var xmlWriter = new XmlTextWriter(strWriter);
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.Indentation = 2;
                xmlWriter.IndentChar =  ' ';
                tag.WriteTo(xmlWriter);

                DebugRead("\n" + strWriter.ToString());
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error("OnProtocol(): Exception", ex);
#endif
            }
        }

        void OnWriteText(object sender, string text)
        {
            if (!DebugProtocol) {
                return;
            }

            try {
                if (text != null && text.Trim().Length == 0) {
                    DebugWrite(text);
                    return;
                }

                var strWriter = new StringWriter();
                var xmlWriter = new XmlTextWriter(strWriter);
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.Indentation = 2;
                xmlWriter.IndentChar =  ' ';

                var document = new XmlDocument();
                document.LoadXml(text);
                document.WriteContentTo(xmlWriter);

                DebugWrite("\n" + strWriter.ToString());
            } catch (XmlException) {
                // HACK: in case of an invalid doucment fallback to
                // plain string logging
                DebugWrite("\n" + text);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error("OnWriteText(): Exception", ex);
#endif
            }
        }

        private void OnMessage(object sender, Message msg)
        {
            if (msg.Body == null) {
                return;
            }

            var delay = msg["delay"];
            string stamp = null;
            if (delay != null) {
                stamp = delay.Attributes["stamp"].Value;
            }
            bool display = true;

            ChatModel chat = null;
            PersonModel person = null;
            if (msg.Type != XmppMessageType.groupchat) {
                var sender_jid = msg.From.Bare;
                var personChat = (PersonChatModel) Session.GetChat(
                    sender_jid, ChatType.Person, this
                );
                if (personChat == null) {
                    person = CreatePerson(msg.From);
                    personChat = Session.CreatePersonChat(
                        person, sender_jid, person.IdentityName, this
                    );
                    Session.AddChat(personChat);
                    Session.SyncChat(personChat);
                } else {
                    person = personChat.Person;
                }
                chat = personChat;
            } else {
                string group_jid = msg.From.Bare;
                string group_name = group_jid;
                string sender_jid = msg.From.ToString();
                XmppGroupChatModel groupChat = (XmppGroupChatModel) Session.GetChat(group_jid, ChatType.Group, this);
                if (groupChat == null) {
                    // FIXME shouldn't happen?
                    groupChat = Session.CreateChat<XmppGroupChatModel>(
                        group_jid, group_name, this
                    );
                    Session.AddChat(groupChat);
                    Session.SyncChat(groupChat);
                }
                person = groupChat.GetPerson(msg.From.Resource);
                if (person == null) {
                    // happens in case of a delayed message if the participant has left meanwhile
                    person = new PersonModel(msg.From.Resource,
                                             msg.From.Resource,
                                             NetworkID, Protocol, this);
                }

                // XXX maybe only a Google Talk bug requires this:
                if (stamp != null) {
                    // XXX can't use > because of seconds precision :-(
                    if (stamp.CompareTo(groupChat.LatestSeenStamp) >= 0) {
                        groupChat.LatestSeenStamp = stamp;
                    } else {
                        display = false; // already seen newer delayed message
                    }
                    if (groupChat.SeenNewMessages) {
                        display = false; // already seen newer messages
                    }
                } else {
                    groupChat.SeenNewMessages = true;
                }

                chat = groupChat;
            }

            if (display) {
                var builder = CreateMessageBuilder();
                if (msg.Type != XmppMessageType.error) {
                    builder.AppendMessage(person, msg.Body);
                } else {
                    // TODO: nicer formatting
                    builder.AppendMessage(msg.Error.ToString());
                }
                if (stamp != null) {
                    string format = DateTimeFormatInfo.InvariantInfo.UniversalSortableDateTimePattern.Replace(" ", "T");
                    builder.TimeStamp = DateTime.ParseExact(stamp, format, null);
                }
                Session.AddMessageToChat(chat, builder.ToMessage());
            }
        }

        void OnIQ(object sender, IQ iq)
        {
            Trace.Call(sender, iq);

            if (iq.Query is OwnMessageQuery) {
                OnIQOwnMessage((OwnMessageQuery) iq.Query);
                iq.Handled = true;
            }
        }

        void OnIQOwnMessage(OwnMessageQuery query)
        {
            if (query.Self) {
                // we send this message from Smuxi, nothing to do...
                return;
            }

            var target_jid = query.To.Bare;
            var contact = _RosterManager[target_jid];
            string nickname = null;
            if (contact == null || String.IsNullOrEmpty(contact.Nickname)) {
                nickname = target_jid;
            } else {
                nickname = contact.Nickname;
            }
            var chat = (PersonChatModel) Session.GetChat(target_jid,
                                                         ChatType.Person, this);
            if (chat == null) {
                var person = new PersonModel(target_jid, nickname, NetworkID,
                                             Protocol, this);
                chat = Session.CreatePersonChat(
                    person, target_jid, nickname, this
                );
                Session.AddChat(chat);
                Session.SyncChat(chat);
            }

            _Say(chat, query.Body, false);
        }

        void OnJoin(Room room)
        {
            AddPersonToGroup(room, room.Nickname);
        }

        void OnLeave(Room room, Presence presence)
        {
            var chat = Session.GetChat(room.JID.Bare, ChatType.Group, this);
            if (chat.IsEnabled)
                Session.RemoveChat(chat);
        }

        void OnParticipantJoin(Room room, RoomParticipant roomParticipant)
        {
            AddPersonToGroup(room, roomParticipant.Nick);
        }

        private void AddPersonToGroup(Room room, string nickname)
        {
            string jid = room.JID.Bare;
            var chat = (GroupChatModel) Session.GetChat(jid, ChatType.Group, this);
            // first notice we're joining a group chat is the participant info:
            if (chat == null) {
                chat = Session.CreateChat<XmppGroupChatModel>(jid, jid, this);
                Session.AddChat(chat);
                Session.SyncChat(chat);
            }

            PersonModel person;
            lock(chat.UnsafePersons) {
                person = chat.GetPerson(nickname);
                if (person != null) {
                    return;
                }

                person = new PersonModel(nickname, nickname,
                                         NetworkID, Protocol, this);
                chat.UnsafePersons.Add(nickname, person);
                Session.AddPersonToGroupChat(chat, person);
            }
        }
        
        public void OnParticipantLeave(Room room, RoomParticipant roomParticipant)
        {
            string jid = room.JID.Bare;
            var chat = (GroupChatModel) Session.GetChat(jid, ChatType.Group, this);
            string nickname = roomParticipant.Nick;

            PersonModel person;
            lock(chat.UnsafePersons) {
                person = chat.GetPerson(nickname);
                if (person == null) {
                    return;
                }

                chat.UnsafePersons.Remove(nickname);
                Session.RemovePersonFromGroupChat(chat, person);
            }
        }

        void OnConnect(object sender, StanzaStream stream)
        {
            Trace.Call(sender, stream);
        }

        void OnDisconnect(object sender)
        {
            Trace.Call(sender);

            IsConnected = false;
            OnDisconnected(EventArgs.Empty);
        }

        void OnError(object sender, Exception ex)
        {
            Trace.Call(sender);

#if LOG4NET
            _Logger.Error("OnError(): Exception", ex);
#endif

            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText(_("Error: {0}"), String.Empty);
            builder.AppendMessage(ex.Message);
            Session.AddMessageToChat(_NetworkChat, builder.ToMessage());
        }

        void OnAuthenticate(object sender)
        {
            Trace.Call(sender);

            IsConnected = true;

            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText(_("Authenticated"));
            Session.AddMessageToChat(Chat, builder.ToMessage());

            // send initial presence
            SetPresenceStatus(PresenceStatus.Online, null);

            OnConnected(EventArgs.Empty);
        }

        private void ApplyConfig(UserConfig config, ServerModel server)
        {
            if (server.Username.Contains("@")) {
                var jid_user = server.Username.Split('@')[0];
                var jid_host = server.Username.Split('@')[1];
                _JabberClient.NetworkHost = server.Hostname;
                _JabberClient.User = jid_user;
                _JabberClient.Server = jid_host;
            } else {
                _JabberClient.Server = server.Hostname;
                _JabberClient.User = server.Username;
            }
            _JabberClient.Port = server.Port;
            _JabberClient.Password = server.Password;

            MyPerson = CreatePerson(
                String.Format("{0}@{1}",
                    _JabberClient.User,
                    _JabberClient.Server
                ),
                _JabberClient.User
            );
            MyPerson.IdentityNameColored.ForegroundColor = new TextColor(0, 0, 255);
            MyPerson.IdentityNameColored.BackgroundColor = TextColor.None;
            MyPerson.IdentityNameColored.Bold = true;

            // XMPP specific settings
            if (server is XmppServerModel) {
                var xmppServer = (XmppServerModel) server;
                _JabberClient.Resource = xmppServer.Resource;
            }

            // fallback
            if (String.IsNullOrEmpty(_JabberClient.Resource)) {
                _JabberClient.Resource = "smuxi";
            }

            _JabberClient.OnInvalidCertificate -= ValidateCertificate;

            _JabberClient.AutoStartTLS = server.UseEncryption;
            if (!server.ValidateServerCertificate) {
                _JabberClient.OnInvalidCertificate += ValidateCertificate;
            }

            var proxySettings = new ProxySettings();
            proxySettings.ApplyConfig(Session.UserConfig);
            var protocol = server.UseEncryption ? "xmpps" : "xmpp";
            var serverUri = String.Format("{0}://{1}:{2}", protocol,
                                          server.Hostname, server.Port);
            var proxy = proxySettings.GetWebProxy(serverUri);
            if (proxy == null) {
                _JabberClient.Proxy = XmppProxyType.None;
            } else {
                var proxyScheme = proxy.Address.Scheme;
                var xmppProxyType = XmppProxyType.None;
                try {
                    // HACK: map proxy scheme to SmartIrc4net's ProxyType
                    xmppProxyType = (XmppProxyType) Enum.Parse(
                        typeof(XmppProxyType), proxyScheme, true
                    );
                } catch (ArgumentException ex) {
#if LOG4NET
                    _Logger.Error("ApplyConfig(): Couldn't parse proxy type: " +
                                  proxyScheme, ex);
#endif
                }
                _JabberClient.Proxy = xmppProxyType;
                _JabberClient.ProxyHost = proxy.Address.Host;
                _JabberClient.ProxyPort = proxy.Address.Port;
                _JabberClient.ProxyUsername = proxySettings.ProxyUsername;
                _JabberClient.ProxyPassword = proxySettings.ProxyPassword;
            }
        }

        private static bool ValidateCertificate(object sender,
                                         X509Certificate certificate,
                                         X509Chain chain,
                                         SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        PersonModel CreatePerson(JID jid)
        {
            if (jid == null) {
                throw new ArgumentNullException("jid");
            }
            var contact = _RosterManager[jid.Bare];
            string nickname = null;
            if (contact == null || String.IsNullOrEmpty(contact.Nickname)) {
                nickname = jid.Bare;
            } else {
                nickname = contact.Nickname;
            }
            return CreatePerson(jid.Bare, nickname);
        }

        PersonModel CreatePerson(string jid, string nickname)
        {
            return new PersonModel(jid, nickname, NetworkID, Protocol, this);
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
