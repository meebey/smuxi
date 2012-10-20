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

using agsXMPP;
using agsXMPP.protocol;
using agsXMPP.protocol.iq.roster;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq;
using agsXMPP.protocol.x.muc;
using XmppMessageType = agsXMPP.protocol.client.MessageType;

using Smuxi.Common;
using agsXMPP.Factory;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "jabber")]
    public class JabberProtocolManager : XmppProtocolManager
    {
        public JabberProtocolManager(Session session) : base(session)
        {
        }
    }
    
    public class ContactInfo
    {
        public string name { get; set; }
        public Jid jid { get; set; }
        public PresenceType presence { get; set; }
        public string message { get; set; }
    }
    
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "xmpp")]
    public class XmppProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        XmppClientConnection JabberClient { get; set; }
        MucManager MucManager { get; set; }
        
        System.Collections.Generic.Dictionary<Jid, ContactInfo> Contacts = new System.Collections.Generic.Dictionary<Jid, ContactInfo>();

        ChatModel NetworkChat { get; set; }
        GroupChatModel ContactChat { get; set; }
        
        XmppServerModel Server { get; set; }

        public override string NetworkID {
            get {
                if (JabberClient != null) {
                    if (!String.IsNullOrEmpty(JabberClient.Server)) {
                        return JabberClient.Server;
                    }
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

            JabberClient = new XmppClientConnection();
            JabberClient.Resource = "Smuxi";
            JabberClient.AutoRoster = true;
            JabberClient.AutoPresence = true;
            JabberClient.OnMessage += OnMessage;
            JabberClient.OnClose += OnDisconnect;
            JabberClient.OnLogin += OnAuthenticate;
            JabberClient.OnError += OnError;
            JabberClient.OnPresence += OnPresence;
            JabberClient.OnRosterItem += OnRosterItem;
            JabberClient.OnReadXml += OnProtocol;
            JabberClient.OnWriteXml += OnWriteText;
            JabberClient.OnAuthError += OnAuthError;
            JabberClient.OnIq += OnIQ;
            JabberClient.AutoAgents = false;
            
            // facebook own message echo
            ElementFactory.AddElementType("own-message", "http://www.facebook.com/xmpp/messages", typeof(OwnMessageQuery));
            
            MucManager = new MucManager(JabberClient);
        }

        void OnAuthError (object sender, agsXMPP.Xml.Dom.Element e)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendErrorText("Authentication failed, either username does not exist or invalid password");
            Session.AddMessageToChat(NetworkChat, builder.ToMessage());
            builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendMessage("if you want to create an account with the specified user and password, type /register now");
            Session.AddMessageToChat(NetworkChat, builder.ToMessage());
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
            
            if (server is XmppServerModel) {
                Server = (XmppServerModel) server;
            } else {
                Server = new XmppServerModel();
                Server.Load(Session.UserConfig, server.ServerID);
                // HACK: previous line overwrites any passed values with the values from config
                // thus we have to copy the original values:
                Server.Hostname = server.Hostname;
                Server.Network = server.Network;
                Server.OnConnectCommands = server.OnConnectCommands;
                Server.OnStartupConnect = server.OnStartupConnect;
                Server.Password = server.Password;
                Server.Port = server.Port;
                Server.Protocol = server.Protocol;
                Server.ServerID = server.ServerID;
                Server.UseEncryption = server.UseEncryption;
                Server.Username = server.Username;
                Server.ValidateServerCertificate = server.ValidateServerCertificate;
            }
            
            Host = server.Hostname;
            Port = server.Port;

            ApplyConfig(Session.UserConfig, Server);

            // TODO: use config for single network chat or once per network manager
            NetworkChat = Session.CreateChat<ProtocolChatModel>(
                NetworkID, "Jabber " + Host, this
            );
            Session.AddChat(NetworkChat);
            Session.SyncChat(NetworkChat);

            OpenContactChat();

            /*
            if (!String.IsNullOrEmpty(JabberClient.proxy??)) {
                var builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(_("Using proxy: {0}:{1}"),
                                   JabberClient.ProxyHost,
                                   JabberClient.ProxyPort);
                Session.AddMessageToChat(Chat, builder.ToMessage());
            }
            */
            JabberClient.Server = Server.Hostname;
            JabberClient.Port = Server.Port;
            JabberClient.Username = Server.Username;
            JabberClient.Password = Server.Password;
            DebugWrite("calling JabberClient.Open()");
            JabberClient.Open();
        }
        
        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            JabberClient.Close();
            JabberClient.Open();
        }
        
        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            JabberClient.Close();
            IsConnected = false;
        }

        public override void Dispose()
        {
            Trace.Call();

            base.Dispose();
            
            IsConnected = false;
            JabberClient.Close();
            JabberClient = null;
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
            foreach (var pair in Contacts) {
                if (pair.Value.presence == PresenceType.available) {
                    lock (ContactChat) {
                        Session.AddPersonToGroupChat(ContactChat, CreatePerson(pair.Key));
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
                MucManager.LeaveRoom(chat.ID, ((XmppGroupChatModel)chat).OwnNickname);
            } else if (chat.ChatType == ChatType.Person) {
                Session.RemoveChat(chat);
            } else {
#if LOG4NET
                _Logger.Error("CloseChat(): Invalid chat type");
#endif
            }
        }

        public override void SetPresenceStatus(PresenceStatus status,
                                               string message)
        {
            Trace.Call(status, message);

            if (!IsConnected || !JabberClient.Authenticated) {
                return;
            }

            switch (status) {
                case PresenceStatus.Online:
                    JabberClient.Show = ShowType.NONE;
                    JabberClient.Priority = Server.Priorities[status];
                    break;
                case PresenceStatus.Away:
                    JabberClient.Priority = Server.Priorities[status];
                    JabberClient.Show = ShowType.away;
                    JabberClient.Status = "afk";
                    break;
            }

            JabberClient.SendMyPresence();
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
                        case "contact":
                            CommandContact(command);
                            handled = true;
                            break;
                        case "priority":
                            CommandPriority(command);
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

        public void CommandContact (CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            // todo: allow length of 2 in private chat windows
            if (cd.DataArray.Length < 3) {
                NotEnoughParameters(cd);
                return;
            }
            Jid jid = GetJidFromNickname(cd.DataArray[2]);
            string cmd = cd.DataArray[1];
            switch (cmd) {
                case "add":
                case "subscribe":
                    // also use GetJidFromNickname(jid) here, so jid is checked for validity
                    JabberClient.RosterManager.AddRosterItem(jid);
                    JabberClient.PresenceManager.Subscribe(jid);
                    break;
                case "remove":
                case "rm":
                case "del":
                    JabberClient.RosterManager.RemoveRosterItem(jid);
                    JabberClient.PresenceManager.Unsubscribe(jid);
                    break;
                case "accept":
                case "allow":
                    JabberClient.PresenceManager.ApproveSubscriptionRequest(jid);
                    break;
                case "deny":
                    JabberClient.PresenceManager.RefuseSubscriptionRequest(jid);
                    break;
                case "rename":
                    if (cd.DataArray.Length < 4) {
                        NotEnoughParameters(cd);
                        return;
                    }
                    JabberClient.RosterManager.UpdateRosterItem(jid, cd.DataArray[3]);
                    break;
                default:
                    var builder = CreateMessageBuilder();
                    builder.AppendText(_("Invalid Contact command: {0}"), cmd);
                    fm.AddMessageToChat(cd.Chat, builder.ToMessage());
                    return;
            }
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
            "msg/query jid/nick message",
            "say message",
            "join muc-jid [custom-chat-nick]",
            "part/leave [muc-jid]",
            "away [away-message]",
            "contact add/remove/accept/deny jid/nick",
            "contact rename jid/nick newnick"
            ,"priority away/online/temp priority-value"
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

        public void CommandPriority(CommandModel command)
        {
            if (command.DataArray.Length < 3) {
                var builder = CreateMessageBuilder();
                builder.AppendText(_("Priority for Available is: {0}"), Server.Priorities[PresenceStatus.Online]);
                command.FrontendManager.AddMessageToChat(command.Chat, builder.ToMessage());
                builder = CreateMessageBuilder();
                builder.AppendText(_("Priority for Away is: {0}"), Server.Priorities[PresenceStatus.Away]);
                command.FrontendManager.AddMessageToChat(command.Chat, builder.ToMessage());
                return;
            }
            string subcmd = command.DataArray[1];
            int prio;
            if (!int.TryParse(command.DataArray[2], out prio) || prio < -128 || prio > 127) {
                var builder = CreateMessageBuilder();
                builder.AppendText(_("Invalid Priority: {0} (valid priorities are between -128 and 127 inclusive)"), command.DataArray[2]);
                command.FrontendManager.AddMessageToChat(command.Chat, builder.ToMessage());
                return;
            }
            JabberClient.Priority = prio;
            bool change_current_prio = false;
            switch (subcmd) {
                case "temp":
                case "temporary":
                    change_current_prio = true;
                    // only set priority
                    break;
                case "away":
                    Server.Priorities[PresenceStatus.Away] = prio;
                    change_current_prio = (JabberClient.Show == ShowType.away);
                    JabberClient.Priority = prio;
                    break;
                case "online":
                case "available":
                    Server.Priorities[PresenceStatus.Online] = prio;
                    change_current_prio = (JabberClient.Show == ShowType.NONE);
                    JabberClient.Priority = prio;
                    break;
                default:
                    return;
            }
            if (change_current_prio) {
                // set priority and keep all other presence info
                JabberClient.SendMyPresence();
            }
        }

        private Jid GetJidFromNickname(string nickname)
        {
            ContactInfo it;
            if (Contacts.TryGetValue(nickname, out it)) {
                return it.jid;
            }

            // arg is not a jid in our rostermanager
            // find a jid to which the nickname belongs
            foreach (var pair in Contacts) {
                if (pair.Value.name != null &&
                    pair.Value.name.Replace(" ", "_") == nickname) {
                    return pair.Key;
                }
            }
            // not found in roster, message directly to jid
            // TODO: check jid for validity
            return nickname;
        }
        
        public void CommandMessageQuery(CommandModel cd)
        {
            ChatModel chat = null;
            if (cd.DataArray.Length >= 2) {
                string arg = cd.DataArray[1];
                Jid jid = GetJidFromNickname(arg);
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
            XmppGroupChatModel chat = (XmppGroupChatModel)GetChat(jid, ChatType.Group);
            string nickname = JabberClient.Username;
            if (cd.DataArray.Length > 2) {
                nickname = cd.DataArray[2];
            }
            if (chat == null) {
                MucManager.JoinRoom(jid, nickname);
                chat = Session.CreateChat<XmppGroupChatModel>(jid, jid, this);
                chat.OwnNickname = nickname;
                Session.AddChat(chat);
            }
        }

        public void CommandPart(CommandModel cd)
        {
            string jid;
            if (cd.DataArray.Length >= 2)
                jid = cd.DataArray[1];
            else
                jid = cd.Chat.ID;
            XmppGroupChatModel chat = (XmppGroupChatModel)GetChat(jid, ChatType.Group);
            if (chat != null) {
                MucManager.LeaveRoom(jid, chat.OwnNickname);
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

            foreach (var pair in Contacts) {
                string status = "+";
                var contact = pair.Value;
                if (contact.presence == PresenceType.available) {
                    if (!full) continue;
                    status = "-";
                }
                builder = CreateMessageBuilder();
                builder.AppendText("{0}\t{1}\t({2}): {3}", status, contact.name, pair.Key, contact.message);
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
                    JabberClient.Send(new Message(target, XmppMessageType.chat, text));
                } else if (chat.ChatType == ChatType.Group) {
                    JabberClient.Send(new Message(target, XmppMessageType.groupchat, text));
                    return; // don't show now. the message will be echoed back if it's sent successfully
                }
            }

            var builder = CreateMessageBuilder();
            builder.AppendSenderPrefix(Me);
            builder.AppendMessage(text);
            Session.AddMessageToChat(chat, builder.ToMessage());
        }


        void OnProtocol(object sender, string tag)
        {
            if (!DebugProtocol) {
                return;
            }

            try {

                DebugRead("\n" + tag);
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


        public void OnRosterItem(object sender, RosterItem ri)
        {
            ContactInfo contact = new ContactInfo();
            contact.jid = ri.Jid;
            contact.name = ri.Name;
            Contacts[ri.Jid] = contact;

            if (ContactChat == null) return;
            lock (ContactChat) {
                PersonModel oldp = ContactChat.GetPerson(ri.Jid);
                if (oldp == null) {
                    // doesn't exist, don't need to do anything
                    return;
                }
                PersonModel newp = CreatePerson(ri.Jid);
                Session.UpdatePersonInGroupChat(ContactChat, oldp, newp);
            }
        }

        MessageModel CreatePresenceUpdateMessage(PersonModel person, PresenceType type, ShowType show, string message)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendIdendityName(person);
            if (person.ID != person.IdentityName) {
                builder.AppendText(" [{0}]", person.ID);
            }
            switch (type) {
                case PresenceType.available:
                    switch (show) {
                        case ShowType.NONE:
                            builder.AppendText(_(" is now available"));
                            break;
                        case ShowType.away:
                            builder.AppendText(_(" is now away"));
                            break;
                        case ShowType.dnd:
                            builder.AppendText(_(" wishes not to be disturbed"));
                            break;
                        case ShowType.xa:
                            builder.AppendText(_(" is now on extended away"));
                            break;
                        default:
                            builder.AppendText(_(" changed to an unknown state: {0}"), show);
                            break;
                    }
                    if (!String.IsNullOrEmpty(message)) {
                        builder.AppendText(": {0}", message);
                    }
                    break;
                case PresenceType.unavailable:
                    builder.AppendText(_(" is now offline"));
                    break;
                case PresenceType.subscribe:
                    builder.AppendText(_(" wishes to subscribe to you"));
                    break;
                case PresenceType.subscribed:
                    builder.AppendText(_(" allows you to subscribe"));
                    break;
                default:
                    builder.AppendErrorText(" Error: unknown presence type: {0}", type.ToString());
                    break;
            }
            return builder.ToMessage();
        }

        MessageModel CreateMucPresenceUpdateMessage(string muc, string nickname, PresenceType type, ShowType show, string message, bool isUpdate)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendIdendityName(new PersonModel("", nickname, "", "", this));

            switch (type) {
                case PresenceType.available:
                    switch (show) {
                        case ShowType.NONE:
                            if (isUpdate) {
                                builder.AppendText(_(" is now available"));
                            } else {
                                builder.AppendText(_(" has joined {0}"), muc);
                            }
                            break;
                        case ShowType.away:
                            builder.AppendText(_(" is now away"));
                            break;
                        case ShowType.dnd:
                            builder.AppendText(_(" wishes not to be disturbed"));
                            break;
                        case ShowType.xa:
                            builder.AppendText(_(" is now on extended away"));
                            break;
                        default:
                            builder.AppendText(_(" changed to an unknown state: {0}"), show);
                            break;
                    }
                    if (!String.IsNullOrEmpty(message)) {
                        builder.AppendText(": {0}", message);
                    }
                    break;
                case PresenceType.unavailable:
                    builder.AppendText(_(" has left {0}"), muc);
                    break;
                default:
                    builder.AppendErrorText(" Error: unknown presence type in muc: {0}", type.ToString());
                    break;
            }
            return builder.ToMessage();
        }

        void OnPresence(object sender, Presence pres)
        {
            // catch error presence packets
            if (pres.Type == PresenceType.error) {
                var builder = CreateMessageBuilder();
                builder.AppendErrorText("An error presence packet has been received, this is most likely a bug in the client: {0}", pres.ToString());
                Session.AddMessageToChat(NetworkChat, builder.ToMessage());
                return;
            }

            Jid jid = pres.From;
            var groupChat = (XmppGroupChatModel) Session.GetChat(jid.Bare, ChatType.Group, this);

            if (groupChat != null) {
                // is it a muc?
                lock (groupChat) {
                    bool isUpdate = groupChat.UnsafePersons.ContainsKey(jid.Resource);
                    var msg_ = CreateMucPresenceUpdateMessage(jid.Bare, jid.Resource, pres.Type, pres.Show, pres.Status, isUpdate);
                    Session.AddMessageToChat(groupChat, msg_);
                }
                return;
            }
            PersonModel person = CreatePerson(jid.Bare);
            var msg = CreatePresenceUpdateMessage(person, pres.Type, pres.Show, pres.Status);
            if (ContactChat != null) {
                // is the Contact Chat open?
                lock (ContactChat) {
                    Session.AddMessageToChat(ContactChat, msg);
                    PersonModel p = ContactChat.GetPerson(jid.Bare);
                    switch (pres.Type) {
                        case PresenceType.available:
                            // anyone who is online/away/dnd will be added to the list
                            if (p != null) {
                                // p already exists, don't add a new person
                                Session.UpdatePersonInGroupChat(ContactChat, p, person);
                            } else {
                                Session.AddPersonToGroupChat(ContactChat, person);
                            }
                        break;
                        case PresenceType.unavailable:
                            if (p == null) {
                                // doesn't exist, got an offline message w/o a preceding online message?
                                return;
                            }
                            Session.RemovePersonFromGroupChat(ContactChat, p);
                        break;
                    }
                }
            }
            var personChat = Session.GetChat(jid, ChatType.Person, this);
            if (personChat != null) {
                // is there a private chat open?
                lock (personChat) {
                    Session.AddMessageToChat(personChat, msg);
                }
            }
        }
        
        private void OnGroupChatMessage(Message msg)
        {
            string group_jid = msg.From.Bare;
            XmppGroupChatModel groupChat = (XmppGroupChatModel) Session.GetChat(group_jid, ChatType.Group, this);
            // resource can be empty for room messages
            var sender_id = msg.From.Resource ?? msg.From.Bare;
            var person = groupChat.GetPerson(sender_id);
            if (person == null) {
                // happens in case of a delayed message if the participant has left meanwhile
                person = new PersonModel(sender_id,
                                         sender_id,
                                         NetworkID, Protocol, this);
            }
            
            var builder = CreateMessageBuilder();
            // XXX maybe only a Google Talk bug requires this:
            if (msg.XDelay != null) {
                var stamp = msg.XDelay.Stamp;
                builder.TimeStamp = stamp;
                // XXX can't use > because of seconds precision :-(
                if (stamp > groupChat.LatestSeenStamp) {
                    groupChat.LatestSeenStamp = stamp;
                } else {
                    return; // already seen newer delayed message
                }
                if (groupChat.SeenNewMessages) {
                    return; // already seen newer messages
                }
            } else {
                groupChat.SeenNewMessages = true;
            }
            
            // public message
            builder.AppendMessage(person, msg.Body);
            // mark highlights only for received messages
            if (person.ID != groupChat.OwnNickname) {
                builder.MarkHighlights();
            }
            Session.AddMessageToChat(groupChat, builder.ToMessage());
        }
        
        private void OnPrivateChatMessage(Message msg)
        {
            var sender_jid = msg.From;
            var personChat = (PersonChatModel) Session.GetChat(
                sender_jid, ChatType.Person, this
            );
            PersonModel person = null;
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
            var builder = CreateMessageBuilder();
            builder.AppendSenderPrefix(person, true);
            builder.AppendMessage(msg.Body);
            builder.MarkHighlights();
            // todo: can private messages have an xdelay?
            if (msg.XDelay != null) {
                builder.TimeStamp = msg.XDelay.Stamp;
            }
            Session.AddMessageToChat(personChat, builder.ToMessage());
        }

        private void OnMessage(object sender, Message msg)
        {
            if (String.IsNullOrEmpty(msg.Body)) {
                // TODO: capture events and stuff
                return;
            }
            switch (msg.Type) {
                case XmppMessageType.groupchat:
                    OnGroupChatMessage(msg);
                    break;
                case XmppMessageType.chat:
                    OnPrivateChatMessage(msg);
                    break;
                case XmppMessageType.error:
                {
                    var builder = CreateMessageBuilder();
                    // TODO: nicer formatting
                    builder.AppendMessage(msg.Error.ToString());
                    Session.AddMessageToChat(NetworkChat, builder.ToMessage());
                }
                    break;
                case XmppMessageType.headline:
                {
                    var builder = CreateMessageBuilder();
                    // TODO: this is just a dump, do something proper!
                    builder.AppendMessage(msg.Nickname.ToString());
                    builder.AppendMessage(msg.Subject);
                    builder.AppendMessage(msg.Thread);
                    builder.AppendMessage(msg.Body);
                    Session.AddMessageToChat(NetworkChat, builder.ToMessage());
                }
                    break;
                case XmppMessageType.normal:
                {
                    // TODO: this is just a dump, do something proper!
                    var builder = CreateMessageBuilder();
                    builder.AppendMessage(msg.ToString());
                    Session.AddMessageToChat(NetworkChat, builder.ToMessage());
                }
                    break;
            }
        }
  
        
        void OnIQ(object sender, IQ iq)
        {
            Trace.Call(sender, iq);

            // not as pretty as the previous implementation, but it works
            var elem = iq.SelectSingleElement("own-message");
            if (elem is OwnMessageQuery) {
                OnIQOwnMessage((OwnMessageQuery) elem);
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

        private void AddPersonToGroup(XmppGroupChatModel chat, Jid jid)
        {
            lock (chat) {
                if (chat.UnsafePersons.ContainsKey(jid)) {
                    return;
                }
                // manual construction is necessary for group chats
                var person = CreatePerson(jid, jid.Resource);
                if (chat.IsSynced) {
                    Session.AddPersonToGroupChat(chat, person);
                } else {
                    chat.UnsafePersons.Add(jid, person);
                }
            }

            // did I join? then the chat roster is fully received
            if (!chat.IsSynced && jid.Resource == chat.OwnNickname) {
                chat.IsSynced = true;
                Session.SyncChat(chat);
            }
        }

        public void RemovePersonFromGroupChat(XmppGroupChatModel chat, Jid jid)
        {
            lock (chat) {
                var person = chat.GetPerson(jid);
                if (person == null) {
                    return;
                }

                Session.RemovePersonFromGroupChat(chat, person);
            }
            // did I leave? then I "probably" left the room
            if (jid.Resource == chat.OwnNickname) {
                Session.RemoveChat(chat);
            }
        }

        void OnDisconnect(object sender)
        {
            Trace.Call(sender);

            IsConnected = false;
            
            var chats = new List<ChatModel>();
            foreach (var chat in Session.Chats) {
                if (chat.ProtocolManager != this) continue;
                if (chat.ChatType != ChatType.Group) continue;
                chats.Add(chat);
            }
            foreach (var chat in chats) {
                Session.RemoveChat(chat);
            }
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
            //SetPresenceStatus(PresenceStatus.Online, null);

            OnConnected(EventArgs.Empty);
        }

        private void ApplyConfig(UserConfig config, XmppServerModel server)
        {
            if (server.Username.Contains("@")) {
                var jid_user = server.Username.Split('@')[0];
                var jid_host = server.Username.Split('@')[1];
                //JabberClient.NetworkHost = server.Hostname;
                JabberClient.Username = jid_user;
                JabberClient.Server = jid_host;
            } else {
                JabberClient.Server = server.Hostname;
                JabberClient.Username = server.Username;
            }
            JabberClient.Port = server.Port;
            JabberClient.Password = server.Password;

            Me = CreatePerson(
                String.Format("{0}@{1}",
                    JabberClient.Username,
                    JabberClient.Server
                ),
                JabberClient.Username
            );
            Me.IdentityNameColored.ForegroundColor = new TextColor(0, 0, 255);
            Me.IdentityNameColored.BackgroundColor = TextColor.None;
            Me.IdentityNameColored.Bold = true;

            // XMPP specific settings
            JabberClient.Resource = server.Resource;

            JabberClient.UseStartTLS = server.UseEncryption;
            if (!server.ValidateServerCertificate) {
                JabberClient.ClientSocket.OnValidateCertificate += ValidateCertificate;
            }
#if false
            var proxySettings = new ProxySettings();
            proxySettings.ApplyConfig(Session.UserConfig);
            var protocol = server.UseEncryption ? "xmpps" : "xmpp";
            var serverUri = String.Format("{0}://{1}:{2}", protocol,
                                          server.Hostname, server.Port);
            var proxy = proxySettings.GetWebProxy(serverUri);
            if (proxy == null) {
                JabberClient.ClientSocket..Proxy = XmppProxyType.None;
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
#endif
        }

        private static bool ValidateCertificate(object sender,
                                         X509Certificate certificate,
                                         X509Chain chain,
                                         SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        PersonModel CreatePerson(Jid jid)
        {
            if (jid == null) {
                throw new ArgumentNullException("jid");
            }
            string nickname = null;
            ContactInfo contact;
            if (!Contacts.TryGetValue(jid.Bare, out contact) || String.IsNullOrEmpty(contact.name)) {
                nickname = jid;
            } else {
                nickname = contact.name;
            }
            return CreatePerson(jid, nickname);
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
