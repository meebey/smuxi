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
        JabberClient JabberClient { get; set; }
        RosterManager RosterManager { get; set; }
        ConferenceManager ConferenceManager { get; set; }
        PresenceManager PresenceManager { get; set; }

        ChatModel NetworkChat { get; set; }
        GroupChatModel ContactChat { get; set; }

        public override string NetworkID {
            get {
                if (!String.IsNullOrEmpty(JabberClient.NetworkHost)) {
                    return JabberClient.NetworkHost;
                }
                if (!String.IsNullOrEmpty(JabberClient.Server)) {
                    return JabberClient.Server;
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
                return NetworkChat;
            }
        }

        public XmppProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);

            JabberClient = new JabberClient();
            JabberClient.Resource = "Smuxi";
            JabberClient.AutoLogin = true;
            JabberClient.AutoPresence = false;
            JabberClient.OnStreamInit += OnStreamInit;
            JabberClient.OnMessage += OnMessage;
            JabberClient.OnConnect += OnConnect;
            JabberClient.OnDisconnect += OnDisconnect;
            JabberClient.OnAuthenticate += OnAuthenticate;
            JabberClient.OnError += OnError;
            JabberClient.OnProtocol += OnProtocol;
            JabberClient.OnWriteText += OnWriteText;
            JabberClient.OnIQ += OnIQ;

            RosterManager = new RosterManager();
            RosterManager.Stream = JabberClient;
            RosterManager.OnRosterItem += OnRosterItem;

            PresenceManager = new PresenceManager();
            PresenceManager.Stream = JabberClient;
            JabberClient.OnPresence += OnPresence;

            ConferenceManager = new ConferenceManager();
            ConferenceManager.Stream = JabberClient;
            ConferenceManager.OnJoin += OnJoin;
            ConferenceManager.OnLeave += OnLeave;
            ConferenceManager.OnParticipantJoin += OnParticipantJoin;
            ConferenceManager.OnParticipantLeave += OnParticipantLeave;
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

            Host = server.Hostname;
            Port = server.Port;

            ApplyConfig(Session.UserConfig, server);

            // TODO: use config for single network chat or once per network manager
            NetworkChat = Session.CreateChat<ProtocolChatModel>(
                NetworkID, "Jabber " + Host, this
            );
            Session.AddChat(NetworkChat);
            Session.SyncChat(NetworkChat);

            OpenContactChat();

            if (!String.IsNullOrEmpty(JabberClient.ProxyHost)) {
                var builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(_("Using proxy: {0}:{1}"),
                                   JabberClient.ProxyHost,
                                   JabberClient.ProxyPort);
                Session.AddMessageToChat(Chat, builder.ToMessage());
            }
            JabberClient.Connect();
        }
        
        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            JabberClient.Close();
            JabberClient.Connect();
        }
        
        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            JabberClient.Close(false);
        }

        public override void Dispose()
        {
            Trace.Call();

            base.Dispose();

            JabberClient.Dispose();
        }

        public override string ToString()
        {
            string result = "Jabber ";
            if (JabberClient != null) {
                result += JabberClient.Server + ":" + JabberClient.Port;
            }
            
            if (!IsConnected) {
                result += " (" + _("not connected") + ")";
            }
            return result;
        }
        
        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);
            
            var list = new List<GroupChatModel>();
            if (ContactChat == null) {
                list.Add(new GroupChatModel("Contacts", "Contacts", this));
            }
            return list;
        }

        public void OpenContactChat ()
        {
            var chat = Session.GetChat("Contacts", ChatType.Group, this);

            if (chat != null) return;

            ContactChat = Session.CreateChat<GroupChatModel>(
                "Contacts", "Contacts", this
            );
            Session.AddChat(ContactChat);
            Session.SyncChat(ContactChat);
            foreach(JID jid in PresenceManager) {
                if (PresenceManager.IsAvailable(jid)) {
                    lock (ContactChat) {
                        Session.AddPersonToGroupChat(ContactChat, CreatePerson(jid));
                    }
                }
            }
        }

        public override void OpenChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);
            if (chat.ID == "Contacts") {
                OpenContactChat();
                return;
            }
            CommandModel cmd = new CommandModel(fm, NetworkChat, chat.ID);
            switch (chat.ChatType) {
                case ChatType.Person:
                    CommandMessageQuery(cmd);
                    break;
                case ChatType.Group:
                    CommandJoin(cmd);
                    break;
            }
        }

        public override void CloseChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);

            if (chat == ContactChat) {
                Session.RemoveChat(chat);
                ContactChat = null;
            } else if (chat.ChatType == ChatType.Group) {
                ConferenceManager.GetRoom(chat.ID+"/"+JabberClient.User).Leave("Closed");
            } else {
                Session.RemoveChat(chat);
            }
        }

        public override void SetPresenceStatus(PresenceStatus status,
                                               string message)
        {
            Trace.Call(status, message);

            if (!IsConnected || !JabberClient.IsAuthenticated) {
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

            JabberClient.Presence(xmppType.Value, message, xmppShow,
                                  JabberClient.Priority);
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
                    var builder = CreateMessageBuilder();
                    builder.AppendText(_("Invalid port: {0}"), cd.DataArray[3]);
                    fm.AddMessageToChat(cd.Chat, builder.ToMessage());
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
                string arg = cd.DataArray[1];
                Item it = RosterManager[arg];
                JID jid = null;
                // arg is not a jid in our rostermanager
                if (it == null) {
                    // find a jid to which the nickname belongs
                    foreach (JID j in RosterManager) {
                        Item item = RosterManager[j];
                        if (item.Nickname != null &&
                            item.Nickname.Replace(" ", "_") == arg) {
                            jid = item.JID;
                            break;
                        }
                    }
                    if (jid == null) {
                        // not found in roster, message directly to jid
                        // TODO: check jid for validity
                        jid = arg;
                    }
                } else {
                    jid = it.JID;
                }

                chat = GetChat(jid, ChatType.Person);
                if (chat == null) {
                    PersonModel person = CreatePerson(jid);
                    chat = Session.CreatePersonChat(person, jid, person.IdentityName, this);
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
                ConferenceManager.GetRoom(jid+"/"+JabberClient.User).Join();
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
                ConferenceManager.GetRoom(jid+"/"+JabberClient.User).Leave("Part");
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

            foreach (JID j in RosterManager) {
                string status = "+";
                if (!PresenceManager.IsAvailable(j)) {
                    if (!full) continue;
                    status = "-";
                }
                string nick = RosterManager[j].Nickname;
                string mesg = "";
                Presence item = PresenceManager[j];
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
            if (chat == ContactChat) {
                return;
            }
            
            if (send) {
                string target = chat.ID;
                if (chat.ChatType == ChatType.Person) {
                    JabberClient.Message(target, text);
                } else if (chat.ChatType == ChatType.Group) {
                    var room = ConferenceManager.GetRoom(
                        String.Format(
                            "{0}/{1}",
                            target, JabberClient.User
                        )
                    );
                    room.PublicMessage(text);
                    return; // don't show now. the message will be echoed back if it's sent successfully
                }
            }

            var builder = CreateMessageBuilder();
            builder.AppendSenderPrefix(Me);
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

        public void OnRosterItem(object sender, Item ri)
        {
            string jid = ri.JID.Bare;

            if (ContactChat == null) return;
            lock (ContactChat) {
                PersonModel oldp = ContactChat.GetPerson(jid);
                if (oldp == null) {
                    // doesn't exist, don't need to do anything
                    return;
                }
                PersonModel newp = CreatePerson(jid);
                Session.UpdatePersonInGroupChat(ContactChat, oldp, newp);
            }
        }

        void OnPresence(object sender, Presence pres)
        {
            JID jid = pres.From;
            var groupChat = (XmppGroupChatModel) Session.GetChat(jid.Bare, ChatType.Group, this);

            MessageBuilder builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            PersonModel person = null;
            if (groupChat != null) {
                person = new PersonModel("", jid.Resource, "", "", this);
            } else {
                person = CreatePerson(jid.Bare);
            }
            builder.AppendIdendityName(person);
            if (jid != person.IdentityName) {
                builder.AppendText(" [{0}]", jid);
            }

            switch (pres.Type) {
                case PresenceType.available:
                    // groupchat is already managed
                    if (groupChat == null) {
                        if (ContactChat != null) {
                            // anyone who is online/away/dnd will be added to the list
                            lock (ContactChat) {
                                PersonModel p = ContactChat.GetPerson(jid.Bare);
                                if (p != null) {
                                    // p already exists, don't add a new person
                                    Session.UpdatePersonInGroupChat(ContactChat, p, person);
                                } else {
                                    Session.AddPersonToGroupChat(ContactChat, person);
                                }
                            }
                        }
                    }
                    if (pres.Show == null) {
                        builder.AppendText(_(" is now available"));
                    } else if (pres.Show == "away") {
                        builder.AppendText(_(" is now away"));
                    } else if (pres.Show == "dnd") {
                        builder.AppendText(_(" wishes not to be disturbed"));
                    } else {
                        builder.AppendText(_(" set status to {0}"), pres.Show);
                    }
                    if (pres.Status == null) break;
                    if (pres.Status.Length == 0) break;
                    builder.AppendText(": {0}", pres.Status);
                    break;
                case PresenceType.unavailable:
                    builder.AppendText(_(" is now offline"));
                    if(groupChat == null) {
                        if (ContactChat != null) {
                            lock (ContactChat) {
                                PersonModel p = ContactChat.GetPerson(jid.Bare);
                                if (p == null) {
                                    // doesn't exist, got an offline message w/o a preceding online message?
                                    return;
                                }
                                Session.RemovePersonFromGroupChat(ContactChat, p);
                            }
                        }
                    }
                    break;
                case PresenceType.subscribe:
                    builder.AppendText(_(" wishes to subscribe to you"));
                    break;
                case PresenceType.subscribed:
                    builder.AppendText(_(" allows you to subscribe"));
                    break;
            }
            if (groupChat != null) {
                Session.AddMessageToChat(groupChat, builder.ToMessage());
            } else if (ContactChat != null) {
                Session.AddMessageToChat(ContactChat, builder.ToMessage());
            }
            var personChat = Session.GetChat(jid.Bare, ChatType.Person, this);
            if (personChat != null) {
                Session.AddMessageToChat(personChat, builder.ToMessage());
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
            var chat = (PersonChatModel) Session.GetChat(target_jid,
                                                         ChatType.Person, this);
            if (chat == null) {
                var person = CreatePerson(query.To);
                chat = Session.CreatePersonChat(person, this);
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

            lock (chat) {
                var person = chat.GetPerson(nickname);
                if (person != null) {
                    return;
                }

                person = CreatePerson(nickname);
                Session.AddPersonToGroupChat(chat, person);
            }
        }
        
        public void OnParticipantLeave(Room room, RoomParticipant roomParticipant)
        {
            string jid = room.JID.Bare;
            var chat = (GroupChatModel) Session.GetChat(jid, ChatType.Group, this);
            string nickname = roomParticipant.Nick;

            lock (chat) {
                var person = chat.GetPerson(nickname);
                if (person == null) {
                    return;
                }

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
            builder.AppendErrorText(_("Error: {0}"), String.Empty);
            builder.AppendMessage(ex.Message);
            Session.AddMessageToChat(NetworkChat, builder.ToMessage());
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
                JabberClient.NetworkHost = server.Hostname;
                JabberClient.User = jid_user;
                JabberClient.Server = jid_host;
            } else {
                JabberClient.Server = server.Hostname;
                JabberClient.User = server.Username;
            }
            JabberClient.Port = server.Port;
            JabberClient.Password = server.Password;

            Me = CreatePerson(
                String.Format("{0}@{1}",
                    JabberClient.User,
                    JabberClient.Server
                ),
                JabberClient.User
            );
            Me.IdentityNameColored.ForegroundColor = new TextColor(0, 0, 255);
            Me.IdentityNameColored.BackgroundColor = TextColor.None;
            Me.IdentityNameColored.Bold = true;

            // XMPP specific settings
            if (server is XmppServerModel) {
                var xmppServer = (XmppServerModel) server;
                JabberClient.Resource = xmppServer.Resource;
            }

            // fallback
            if (String.IsNullOrEmpty(JabberClient.Resource)) {
                JabberClient.Resource = "smuxi";
            }

            JabberClient.OnInvalidCertificate -= ValidateCertificate;

            JabberClient.AutoStartTLS = server.UseEncryption;
            if (!server.ValidateServerCertificate) {
                JabberClient.OnInvalidCertificate += ValidateCertificate;
            }

            var proxySettings = new ProxySettings();
            proxySettings.ApplyConfig(Session.UserConfig);
            var protocol = server.UseEncryption ? "xmpps" : "xmpp";
            var serverUri = String.Format("{0}://{1}:{2}", protocol,
                                          server.Hostname, server.Port);
            var proxy = proxySettings.GetWebProxy(serverUri);
            if (proxy == null) {
                JabberClient.Proxy = XmppProxyType.None;
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
                JabberClient.Proxy = xmppProxyType;
                JabberClient.ProxyHost = proxy.Address.Host;
                JabberClient.ProxyPort = proxy.Address.Port;
                JabberClient.ProxyUsername = proxySettings.ProxyUsername;
                JabberClient.ProxyPassword = proxySettings.ProxyPassword;
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
            var contact = RosterManager[jid.Bare];
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
