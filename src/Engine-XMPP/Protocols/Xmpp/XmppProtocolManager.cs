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
using agsXMPP.protocol.iq.disco;
using agsXMPP.protocol.extensions.caps;
using agsXMPP.Net;
using Starksoft.Net.Proxy;
using agsXMPP.protocol.extensions.chatstates;

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
        XmppClientConnection JabberClient { get; set; }
        MucManager MucManager { get; set; }
        DiscoManager Disco { get; set; }
        string[] Nicknames { get; set; }
        
        Dictionary<Jid, XmppPersonModel> Contacts { get; set; }
        Dictionary<string, DiscoInfo> DiscoCache { get; set; }

        ChatModel NetworkChat { get; set; }
        GroupChatModel ContactChat { get; set; }
        
        XmppServerModel Server { get; set; }
        
        // facebook messed up, this is part of a hack to fix that messup
        string LastSentMessage { get; set; }
        bool SupressLocalMessageEcho { get; set; }
        bool AutoReconnect { get; set; }

        public override string NetworkID {
            get {
                return Host;
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
            Contacts = new Dictionary<Jid, XmppPersonModel>();
            DiscoCache = new Dictionary<string, DiscoInfo>();

            SupressLocalMessageEcho = false;
        }

        void OnStreamError(object sender, agsXMPP.Xml.Dom.Element e)
        {
            var error = e as agsXMPP.protocol.Error;
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            // TODO: create user readable error messages from the error.Condition
            //builder.AppendErrorText(error.Condition.ToString());
            switch(error.Condition) {
                case StreamErrorCondition.SystemShutdown:
                    builder.AppendErrorText(_("The Server has shut down"));
                    break;
                case StreamErrorCondition.Conflict:
                    builder.AppendErrorText(_("Another client logged in with the same resource, you have been disconnected"));
                    break;
                case StreamErrorCondition.SeeOtherHost:
                    Server.Hostname = e.GetTag("see-other-host");
                    Reconnect(null);
                    break;
                default:
                    builder.AppendErrorText(error.Text ?? error.Condition.ToString());
                    break;
            }
            Session.AddMessageToChat(NetworkChat, builder.ToMessage());
        }

        void OnAuthError(object sender, agsXMPP.Xml.Dom.Element e)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendErrorText(_("Authentication failed, either username does not exist or invalid password"));
            Session.AddMessageToChat(NetworkChat, builder.ToMessage());
            builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendMessage(_("if you want to create an account with the specified user and password, type /register now"));
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
                if (server.ServerID != null) {
                    Server.Load(Session.UserConfig, server.ServerID);
                }
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
            
            Host = Server.Hostname;
            Port = Server.Port;

            // TODO: use config for single network chat or once per network manager
            NetworkChat = Session.CreateChat<ProtocolChatModel>(
                Host, "Jabber " + Host, this
            );
            Session.AddChat(NetworkChat);
            Session.SyncChat(NetworkChat);

            Connect();
        }

        void Connect()
        {
            Trace.Call();
            Contacts.Clear();

            JabberClient = new XmppClientConnection();
            JabberClient.Resource = "Smuxi";
            JabberClient.AutoRoster = true;
            JabberClient.AutoPresence = true;
            JabberClient.OnMessage += OnMessage;
            JabberClient.OnClose += OnDisconnect;
            JabberClient.OnLogin += OnAuthenticate;
            JabberClient.OnError += OnError;
            JabberClient.OnStreamError += OnStreamError;
            JabberClient.OnPresence += OnPresence;
            JabberClient.OnRosterItem += OnRosterItem;
            JabberClient.OnReadXml += OnProtocol;
            JabberClient.OnWriteXml += OnWriteText;
            JabberClient.OnAuthError += OnAuthError;
            JabberClient.OnIq += OnIQ;
            JabberClient.AutoAgents = false; // outdated feature
            JabberClient.EnableCapabilities = true;
            JabberClient.Capabilities.Node = "https://smuxi.im";
            JabberClient.ClientVersion = Engine.VersionString;

            AutoReconnect = true;

            // identify smuxi
            var ident = JabberClient.DiscoInfo.AddIdentity();
            ident.Category = "client";
            ident.Type = "pc";
            ident.Name = Engine.VersionString;

            // add features here (this is just for notification of other clients)
            JabberClient.DiscoInfo.AddFeature().Var = "http://jabber.org/protocol/caps";
            JabberClient.DiscoInfo.AddFeature().Var = "jabber:iq:last";
            JabberClient.DiscoInfo.AddFeature().Var = "http://jabber.org/protocol/muc";
            JabberClient.DiscoInfo.AddFeature().Var = "http://jabber.org/protocol/disco#info";
            JabberClient.DiscoInfo.AddFeature().Var = "http://www.facebook.com/xmpp/messages";
            JabberClient.DiscoInfo.AddFeature().Var = "http://jabber.org/protocol/xhtml-im";

            Disco = new DiscoManager(JabberClient);
            Disco.AutoAnswerDiscoInfoRequests = true;

            // facebook own message echo
            ElementFactory.AddElementType("own-message", "http://www.facebook.com/xmpp/messages", typeof(OwnMessageQuery));

            MucManager = new MucManager(JabberClient);

            ApplyConfig(Session.UserConfig, Server);

            OpenContactChat();
            
            var proxySettings = new ProxySettings();
            proxySettings.ApplyConfig(Session.UserConfig);
            
            var protocol = Server.UseEncryption ? "xmpps" : "xmpp";
            var serverUri = String.Format("{0}://{1}:{2}", protocol,
                                          Server.Hostname, Server.Port);
            var proxy = proxySettings.GetWebProxy(serverUri);
            if (proxySettings.ProxyType != ProxyType.None) {
                
                var builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(_("Using proxy: {0}:{1}"),
                                   proxy.Address.Host,
                                   proxy.Address.Port);
                Session.AddMessageToChat(Chat, builder.ToMessage());
                
                var proxyScheme = proxy.Address.Scheme;
                var proxyType = Starksoft.Net.Proxy.ProxyType.None;
                try {
                    proxyType =
                        (Starksoft.Net.Proxy.ProxyType) Enum.Parse(
                            typeof(Starksoft.Net.Proxy.ProxyType), proxy.Address.Scheme, true
                        );
                } catch (ArgumentException ex) {
#if LOG4NET
                    _Logger.Error("ApplyConfig(): Couldn't parse proxy type: " +
                                  proxyScheme, ex);
#endif
                }
                var sock = JabberClient.ClientSocket as ClientSocket;
                
                ProxyClientFactory proxyFactory = new ProxyClientFactory();
                if (String.IsNullOrEmpty(proxySettings.ProxyUsername) &&
                    String.IsNullOrEmpty(proxySettings.ProxyPassword)) {
                    sock.Proxy = proxyFactory.CreateProxyClient(
                        proxyType,
                        proxy.Address.Host,
                        proxy.Address.Port
                    );
                } else {
                    sock.Proxy = proxyFactory.CreateProxyClient(
                        proxyType,
                        proxy.Address.Host,
                        proxy.Address.Port,
                        proxySettings.ProxyUsername,
                        proxySettings.ProxyPassword
                    );
                }
            }
            DebugWrite("calling JabberClient.Open()");
            JabberClient.Open();
        }
        
        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            AutoReconnect = true;
            JabberClient.Close();
        }
        
        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            
            IsConnected = false;
            AutoReconnect = false;
            JabberClient.Close();
        }

        public override void Dispose()
        {
            Trace.Call();

            base.Dispose();
            
            IsConnected = false;
            AutoReconnect = false;
            JabberClient.Close();
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

        public void OpenContactChat()
        {
            var chat = Session.GetChat("Contacts", ChatType.Group, this);

            if (chat == null) {
                ContactChat = Session.CreateChat<GroupChatModel>(
                    "Contacts", "Contacts", this
                );
                Session.AddChat(ContactChat);
            } else {
                Session.EnableChat(chat);
            }
            foreach (var pair in Contacts) {
                if (pair.Value.Resources.Count != 0) {
                    ContactChat.UnsafePersons.Add(pair.Key, pair.Value.ToPersonModel());
                }
            }
            Session.SyncChat(ContactChat);
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
                    JabberClient.Status = message;
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
                        case "joinas":
                            CommandJoinAs(command);
                            handled = true;
                            break;
                        case "join":
                            CommandJoin(command);
                            handled = true;
                            break;
                        case "invite":
                            CommandInvite(command);
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
                        case "whois":
                            CommandWhoIs(command);
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

        void printResource(MessageBuilder builder, XmppResourceModel res)
        {
            builder.AppendText("\n\tName: {0}", res.Name);
            var pres = res.Presence;
            builder.AppendText("\n\tPresence:");
            builder.AppendText("\n\t\tShow:\t{0}", pres.Show);
            builder.AppendText("\n\t\tStatus:\t{0}", pres.Status);
            builder.AppendText("\n\t\tLast:\t{0}", (pres.Last!=null)?pres.Last.Seconds.ToString():"");
            builder.AppendText("\n\t\tPriority:\t{0}", pres.Priority);
            builder.AppendText("\n\t\tType:\t{0}", pres.Type);
            builder.AppendText("\n\t\tXDelay:\t{0}", (pres.XDelay!=null)?pres.XDelay.Stamp.ToString():"");
            if (res.Disco != null) {
                builder.AppendText("\n\tFeatures:");
                foreach(var feat in res.Disco.GetFeatures()) {
                    builder.AppendText("\n\t\t{0}", feat.Var);
                }
            }
        }

        public void CommandWhoIs(CommandModel cmd)
        {
            Jid jid;
            if (cmd.DataArray.Length < 2) {
                if ((cmd.DataArray.Length == 1)
                    && (cmd.Chat is PersonChatModel)) {
                    jid = (cmd.Chat as PersonChatModel).Person.ID;
                } else {
                    NotEnoughParameters(cmd);
                    return;
                }
            } else {
                jid = GetJidFromNickname(cmd.DataArray[1]);
            }
            XmppPersonModel person;
            var builder = CreateMessageBuilder();
            if (!Contacts.TryGetValue(jid.Bare, out person)) {
                builder.AppendErrorText(_("Could not find contact {0}"), jid);
                cmd.FrontendManager.AddMessageToChat(cmd.Chat, builder.ToMessage());
                return;
            }
            if (!String.IsNullOrEmpty(jid.Resource)) {
                if (person.Resources.Count > 1) {
                    builder.AppendText(_("Contact {0} has {1} known resources"), jid.Bare, person.Resources.Count);
                }
                XmppResourceModel res;
                if (!person.Resources.TryGetValue(jid.Resource, out res)) {
                    builder.AppendErrorText(_("{0} is not a known resource"), jid.Resource);
                    cmd.FrontendManager.AddMessageToChat(cmd.Chat, builder.ToMessage());
                    return;
                }
                printResource(builder, res);
                cmd.FrontendManager.AddMessageToChat(cmd.Chat, builder.ToMessage());
                return;
            }
            builder.AppendText(_("Contact's Jid: {0}"), person.Jid);
            builder.AppendText("\n");
            switch (person.Subscription) {
                case SubscriptionType.both:
                    builder.AppendText(_("You have a mutual subscription with this contact"));
                    break;
                case SubscriptionType.none:
                    builder.AppendText(_("You have no subscription with this contact and this contact is not subscribed to you"));
                    break;
                case SubscriptionType.to:
                    builder.AppendText(_("You are subscribed to this contact, but the contact is not subcribed to you"));
                    break;
                case SubscriptionType.from:
                    builder.AppendText(_("You are not subscribed to this contact, but the contact is subcribed to you"));
                    break;
                default:
                    builder.AppendErrorText("Invalid Subscription, this is a bug");
                    break;
            }
            int i = 0;
            foreach(var res in person.Resources) {
                builder.AppendText("\nResource({0}):", i);
                printResource(builder, res.Value);
                i++;
            }
            i = 0;
            foreach(var res in person.MucResources) {
                builder.AppendText("\nMucResource({0}):", i);
                printResource(builder, res.Value);
                i++;
            }
            cmd.FrontendManager.AddMessageToChat(cmd.Chat, builder.ToMessage());
        }

        public void CommandContact(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            // todo: allow length of 2 in private chat windows
            if (cd.DataArray.Length < 3) {
                NotEnoughParameters(cd);
                return;
            }
            Jid jid = GetJidFromNickname(cd.DataArray[2]);
            string cmd = cd.DataArray[1];
            // the logic here is taken from
            // http://xmpp.org/rfcs/rfc3921.html#int
            switch (cmd) {
                case "addgroup":
                    if (cd.DataArray.Length < 4) {
                        NotEnoughParameters(cd);
                        return;
                    }
                    JabberClient.RosterManager.AddRosterItem(jid, null, cd.DataArray[3]);
                    break;
                case "addonly":
                    JabberClient.RosterManager.AddRosterItem(jid);
                    break;
                case "add":
                    XmppPersonModel person;
                    if (Contacts.TryGetValue(jid.Bare, out person)) {
                        if (person.Subscription == SubscriptionType.both) break;
                        if (person.Subscription != SubscriptionType.to) {
                            JabberClient.PresenceManager.Subscribe(jid);
                        }
                        if (person.Subscription != SubscriptionType.from) {
                            // in case we already know this contact… but he can't see us
                            JabberClient.PresenceManager.ApproveSubscriptionRequest(jid);
                        }
                    } else {
                        JabberClient.RosterManager.AddRosterItem(jid);
                        JabberClient.PresenceManager.Subscribe(jid);
                        JabberClient.PresenceManager.ApproveSubscriptionRequest(jid);
                    }
                    break;
                case "subscribe":
                    JabberClient.PresenceManager.Subscribe(jid);
                    break;
                case "unsubscribe":
                    // stop receiving status updates from this contact
                    // that contact will still receive your updates
                    JabberClient.PresenceManager.Unsubscribe(jid);
                    break;
                case "remove":
                case "rm":
                case "del":
                case "delete":
                    JabberClient.RosterManager.RemoveRosterItem(jid);
                    // unsubscribing is unnecessary, the server is required to do this
                    break;
                case "accept":
                case "allow":
                case "approve":
                case "auth":
                case "authorize":
                    JabberClient.PresenceManager.ApproveSubscriptionRequest(jid);
                    break;
                case "deny":
                case "refuse":
                    // stop the contact from receiving your updates
                    // you will still receive the contact's status updates
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
            "join muc-jid [password]",
            "joinas muc-jid nickname [password]",
            "part/leave [muc-jid]",
            "away [away-message]",
            "contact add/remove jid/nick",
            "contact rename jid/nick newnick"
            ,"priority away/online/temp priority-value"
            ,"advanced commands:"
            ,"contact addonly/subscribe/unsubscribe/approve/deny"
            ,"whois jid"
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
            XmppPersonModel it;
            Jid jid = nickname;
            if (Contacts.TryGetValue(jid, out it)) {
                // nickname is a jid we know
                return jid;
            }
            if (Contacts.TryGetValue(jid.Bare, out it)) {
                // is a jid with resource
                return jid;
            }

            // arg is not a jid in our rostermanager
            // find a jid to which the nickname belongs
            foreach (var pair in Contacts) {
                if (pair.Value.IdentityName != null &&
                    pair.Value.IdentityName.Replace(" ", "_") == nickname) {
                    return pair.Key;
                }
            }
            // not found in roster, message directly to jid
            // TODO: check jid for validity
            return jid;
        }
        
        void MessageQuery(Jid jid, string message)
        {
            var chat = GetOrCreatePersonChat(jid);
            if (!String.IsNullOrWhiteSpace(message)) {
                _Say(chat, message);
            }
        }
        
        public void CommandMessageQuery(CommandModel cd)
        {
            if (cd.DataArray.Length < 2) {
                NotEnoughParameters(cd);
                return;
            }
            Jid jid = GetJidFromNickname(cd.DataArray[1]);
            if (cd.DataArray.Length >= 3) {
                // we have a message
                string message = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);
                MessageQuery(jid, message);
            } else {
                MessageQuery(jid, null);
            }
        }
        
        public void CommandJoin(CommandModel cd)
        {
            if (cd.DataArray.Length < 2) {
                NotEnoughParameters(cd);
                return;
            }
            string password = null;
            if (cd.DataArray.Length > 2) {
                password = cd.DataArray[2];
            }
            JoinRoom(cd.DataArray[1], null, password);
        }

        void JoinRoom(Jid jid, string nickname, string password)
        {
            XmppGroupChatModel chat = (XmppGroupChatModel)GetChat(jid, ChatType.Group);
            if (nickname == null) {
                nickname = Nicknames[0];
            }
            MucManager.JoinRoom(jid, nickname, password);
            if (chat == null) {
                chat = Session.CreateChat<XmppGroupChatModel>(jid, jid, this);
                Session.AddChat(chat);
            }
            Session.DisableChat(chat);
            if (password != null) {
                chat.Password = password;
            }
            chat.IsSynced = false;
            chat.OwnNickname = nickname;
        }

        public void CommandJoinAs(CommandModel cd)
        {
            if (cd.DataArray.Length < 3) {
                NotEnoughParameters(cd);
                return;
            }
            string password = null;
            if (cd.DataArray.Length > 3) {
                password = cd.DataArray[3];
            }
            JoinRoom(cd.DataArray[1], cd.DataArray[2], password);
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

        public void CommandInvite(CommandModel cd)
        {
            if (cd.DataArray.Length < 3) {
                NotEnoughParameters(cd);
                return;
            }
            string password = null;
            if (cd.DataArray.Length > 3) {
                password = cd.DataArray[3];
            }
            Invite(cd.DataArray[2], cd.DataArray[1], null, password);
        }

        void Invite(Jid jid, Jid room, string reason, string password)
        {
            Invite(new Jid[]{jid}, room, reason, password);
        }
  
        public void Invite(string[] jids_string, string room, string reason, string password)
        {
            var jids = new Jid[jids_string.Length];
            for (int i = 0; i < jids.Length; i++) {
                jids[i] = jids_string[i];
            }
            Invite(jids, room, reason, password);
        }
        
        void Invite(Jid[] jid, Jid room, string reason, string password)
        {
            JoinRoom(room, null, password);
            XmppGroupChatModel chat = (XmppGroupChatModel)GetChat(room, ChatType.Group);
            // if no password is passed, but we are already in the chatroom and know
            // about a password, use that password
            if (password == null && chat != null) {
                password = chat.Password;
            }
            MucManager.Invite(jid, room, reason, password);
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
                if (contact.Resources.Count == 0) {
                    if (!full) continue;
                    status = "-";
                }
                builder = CreateMessageBuilder();
                builder.AppendText("{0} {1}\t({2}): {3},{4}"
                                   , status
                                   , contact.IdentityName
                                   , pair.Key
                                   , contact.Subscription
                                   , contact.Ask
                                   );
                foreach (var p in contact.Resources) {
                    builder.AppendText("\t|\t{0}:{1}:{2}"
                                       , p.Key
                                       , p.Value.Presence.Type.ToString()
                                       , p.Value.Presence.Priority
                                       );
                    if (!String.IsNullOrEmpty(p.Value.Presence.Status)) {
                        builder.AppendText(":\"{0}\"", p.Value.Presence.Status);
                    }
                }
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
                if (chat.ChatType == ChatType.Person) {
                    var _person = (chat as PersonChatModel).Person as PersonModel;
                    XmppPersonModel person = GetOrCreateContact(_person.ID, _person.IdentityName);
                    Jid jid = person.Jid;
                    if (!String.IsNullOrEmpty(jid.Resource)) {
                        JabberClient.Send(new Message(jid, XmppMessageType.chat, text));
                    } else {
                        var resources = person.GetResourcesWithHighestPriority();
                        if (resources.Count == 0) {
                            // no connected resource, send to bare jid
                            JabberClient.Send(new Message(jid.Bare, XmppMessageType.chat, text));
                        } else {
                            foreach (var res in resources) {
                                Jid j = jid;
                                j.Resource = res.Name;
                                JabberClient.Send(new Message(j, XmppMessageType.chat, text));
                            }
                        }
                    }
                } else if (chat.ChatType == ChatType.Group) {
                    JabberClient.Send(new Message(chat.ID, XmppMessageType.groupchat, text));
                    return; // don't show now. the message will be echoed back if it's sent successfully
                }
                if (SupressLocalMessageEcho) {
                    // don't show, facebook is bugging again
                    return;
                }
                LastSentMessage = text;
            }

            var builder = CreateMessageBuilder();
            builder.AppendSenderPrefix(Me);
            builder.AppendMessage(text);
            Session.AddMessageToChat(chat, builder.ToMessage());
        }

        void OnProtocol(object sender, string text)
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
                
                var document = new XmlDocument();
                document.LoadXml(text);
                document.WriteContentTo(xmlWriter);
                
                DebugRead("\n" + strWriter.ToString());
            } catch (XmlException) {
                DebugRead("\n" + text);
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

        public XmppPersonModel GetOrCreateContact(Jid jid, string name)
        {
            XmppPersonModel p;
            if (!Contacts.TryGetValue(jid.Bare, out p)) {
                p = new XmppPersonModel(jid, name, this);
                Contacts[jid.Bare] = p;
            }
            return p;
        }

        public void OnRosterItem(object sender, RosterItem rosterItem)
        {
            // setting to none also removes the person from chat, as we'd never get an offline message anymore
            if (rosterItem.Subscription == SubscriptionType.none
                || rosterItem.Subscription == SubscriptionType.remove) {
                if (rosterItem.Subscription == SubscriptionType.remove) {
                    Contacts.Remove(rosterItem.Jid);
                }
                if (ContactChat == null) return;
                lock (ContactChat) {
                    PersonModel oldp = ContactChat.GetPerson(rosterItem.Jid);
                    if (oldp == null) {
                        // doesn't exist, don't need to do anything
                        return;
                    }
                    Session.RemovePersonFromGroupChat(ContactChat, oldp);
                }
                return;
            }
            // create or update a roster item
            var contact = GetOrCreateContact(rosterItem.Jid.Bare, rosterItem.Name ?? rosterItem.Jid);
            contact.Temporary = false;
            contact.Subscription = rosterItem.Subscription;
            contact.Ask = rosterItem.Ask;
            contact.IdentityName = rosterItem.Name ?? rosterItem.Jid;
            contact.IdentityNameColored = null; // uncache

            if (ContactChat != null) {
                lock (ContactChat) {
                    PersonModel oldp = ContactChat.GetPerson(rosterItem.Jid.Bare);
                    if (oldp == null) {
                        // doesn't exist, don't need to do anything
                        return;
                    }
                    Session.RemovePersonFromGroupChat(ContactChat, oldp);
                    Session.AddPersonToGroupChat(ContactChat, contact.ToPersonModel());
                }
            }
            
            var chat = Session.GetChat(rosterItem.Jid.Bare, ChatType.Person, this) as PersonChatModel;
            if (chat != null) {
                // TODO: implement update chat
                var oldp = chat.Person;
                Session.RemoveChat(chat);
                chat = Session.CreatePersonChat(oldp, this);
                Session.AddChat(chat);
                Session.SyncChat(chat);
            }
        }
  
        void RequestCapabilities(Jid jid, Capabilities caps)
        {
            string hash = caps.Node + "#" + caps.Version;
            RequestCapabilities(jid, hash);
        }
        
        void RequestCapabilities(Jid jid, string hash)
        {
            // already in cache?
            DiscoInfo info;
            if (DiscoCache.TryGetValue(hash, out info)) {
                AddCapabilityToResource(jid, info);
                return;
            }
            // prevent duplicate requests
            DiscoCache[hash] = null;
            // request it
            Disco.DiscoverInformation(jid, OnDiscoInfo, hash);
        }
        
        void AddCapabilityToResource(Jid jid, DiscoInfo info)
        {
            XmppPersonModel contact;
            if (!Contacts.TryGetValue(jid.Bare, out contact)) return;
            XmppResourceModel res;
            if (!contact.Resources.TryGetValue(jid.Resource, out res)) return;
            res.Disco = info;
        }
        
        void OnDiscoInfo(object sender, IQ iq, object pars)
        {
            if (iq.Error != null) {
                var msg = CreateMessageBuilder();
                msg.AppendEventPrefix();
                msg.AppendErrorText(_("An error happened during service discovery for {0}: {1}"),
                                    iq.From,
                                    iq.Error.ErrorText ?? iq.Error.Condition.ToString());
                Session.AddMessageToChat(NetworkChat, msg.ToMessage());
                // clear item from cache so the request is done again some time
                DiscoCache.Remove(pars as string);
                return;
            }
            if (iq.Type != IqType.result)
            {
                throw new ArgumentException("discoinfoiq is not a result");
            }
            if (!(iq.Query is DiscoInfo)) {
                throw new ArgumentException("discoinfoiq query is not a discoinfo");
            }
            DiscoCache[pars as string] = iq.Query as DiscoInfo;
            if (String.IsNullOrEmpty(iq.From.User)) {
                // server capabilities
                var builder = CreateMessageBuilder();
                builder.AppendText("The Server supports the following features: ");
                Session.AddMessageToChat(NetworkChat, builder.ToMessage());
                foreach ( var feature in (iq.Query as DiscoInfo).GetFeatures()) {
                    builder = CreateMessageBuilder();
                    builder.AppendText(feature.Var);
                    Session.AddMessageToChat(NetworkChat, builder.ToMessage());
                }
            } else {
                AddCapabilityToResource(iq.From, iq.Query as DiscoInfo);
            }
        }
        
        MessageModel CreatePresenceUpdateMessage(Jid jid, PersonModel person, Presence pres)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendIdendityName(person);
            // print jid
            if (jid.Bare != person.IdentityName) {
                builder.AppendText(" [{0}]", jid.Bare);
            }
            // print the type (and in case of available detailed type)
            switch (pres.Type) {
                case PresenceType.available:
                    switch(pres.Show) {
                        case ShowType.NONE:
                            builder.AppendText(_(" is available"));
                            break;
                        case ShowType.away:
                            builder.AppendText(_(" is away"));
                            break;
                        case ShowType.xa:
                            builder.AppendText(_(" is extended away"));
                            break;
                        case ShowType.dnd:
                            builder.AppendText(_(" wishes not to be disturbed"));
                            break;
                        case ShowType.chat:
                            builder.AppendText(_(" wants to chat"));
                            break;
                            
                    }
                    break;
                case PresenceType.unavailable:
                    builder.AppendText(_(" is offline"));
                    break;
                case PresenceType.subscribe:
                    if ((person as XmppPersonModel).Ask == AskType.subscribe) {
                        builder = CreateMessageBuilder();
                        builder.AppendActionPrefix();
                        builder.AppendText(_("Automatically allowed "));
                        builder.AppendIdendityName(person);
                        builder.AppendText(_(" to subscribe to you, since you are already asking to subscribe"));
                    } else {
                        // you have to respond
                        builder.AppendText(_(" wishes to subscribe to you"));
                    }
                    break;
                case PresenceType.subscribed:
                    // you can now see their presences
                    builder.AppendText(_(" allowed you to subscribe"));
                    break;
                case PresenceType.unsubscribed:
                    if ((person as XmppPersonModel).Subscription == SubscriptionType.from) {
                        builder = CreateMessageBuilder();
                        builder.AppendActionPrefix();
                        builder.AppendText(_("Automatically removed "));
                        builder.AppendIdendityName(person);
                        builder.AppendText(_("'s subscription to your presences after loosing the subscription to theirs"));
                    } else {
                        // you cannot (anymore?) see their presences
                        builder.AppendText(_(" denied/removed your subscription"));
                    }
                    break;
                case PresenceType.unsubscribe:
                    // you might still be able to see their presences
                    builder.AppendText(_(" unsubscribed from you"));
                    break;
                case PresenceType.error:
                    if (pres.Error == null) break;
                    switch (pres.Error.Type) {
                        case ErrorType.cancel:
                            switch (pres.Error.Condition) {
                                case ErrorCondition.RemoteServerNotFound:
                                    builder.AppendErrorText(_("'s server could not be found"));
                                    break;
                                case ErrorCondition.Conflict:
                                    builder.AppendErrorText(_(" is already using your requested resource"));
                                    break;
                                default:
                                    if (!String.IsNullOrEmpty(pres.Error.ErrorText)) {
                                        builder.AppendErrorText(pres.Error.ErrorText);
                                    } else {
                                        builder.AppendErrorText(pres.Error.Condition.ToString());
                                    }
                                    break;
                            }
                            break;
                        default:
                            if (!String.IsNullOrEmpty(pres.Error.ErrorText)) {
                                builder.AppendErrorText(pres.Error.ErrorText);
                            } else {
                                builder.AppendErrorText(pres.Error.Type.ToString());
                            }
                            break;
                    }
                    break;
            }
            // print timestamp of presence
            if (pres.XDelay != null || pres.Last != null) {
                DateTime stamp;
                TimeSpan span;
                if (pres.XDelay != null) {
                    stamp = pres.XDelay.Stamp;
                    span = DateTime.Now.Subtract(stamp);
                } else if (pres.Last != null) {
                    span = TimeSpan.FromSeconds(pres.Last.Seconds);
                    stamp = DateTime.Now.Subtract(span);
                }
                string spanstr;
                if (span > TimeSpan.FromDays(1)) spanstr = span.ToString("dd':'hh':'mm':'ss' days'");
                else if (span > TimeSpan.FromHours(1)) spanstr = span.ToString("hh':'mm':'ss' hours'");
                else if (span > TimeSpan.FromMinutes(1)) spanstr = span.ToString("mm':'ss' minutes'");
                else spanstr = span.ToString("s' seconds'");
                
                string timestamp = null;
                try {
                    string format = Session.UserConfig["Interface/Notebook/TimestampFormat"] as string;
                    if (!String.IsNullOrEmpty(format)) {
                        timestamp = stamp.ToString(format);
                    }
                } catch (FormatException e) {
                    timestamp = "Timestamp Format ERROR: " + e.Message;
                }
                builder.AppendText(_(" since {0} ({1})"), timestamp, spanstr);
            }
            // print user defined message
            if (!String.IsNullOrWhiteSpace(pres.Status)) {
                builder.AppendText(": {0}", pres.Status);
            }
            return builder.ToMessage();
        }
        
        void OnGroupChatPresence(XmppGroupChatModel chat, Presence pres)
        {
            Jid jid = pres.From;
            XmppPersonModel person;
            // check whether we know the real jid of this muc user
            if (pres.MucUser != null &&
                pres.MucUser.Item != null &&
                pres.MucUser.Item.Jid != null
                ) {
                string nick = pres.From.Resource;
                if (!string.IsNullOrEmpty(pres.MucUser.Item.Nickname)) {
                    nick = pres.MucUser.Item.Nickname;
                }
                person = GetOrCreateContact(pres.MucUser.Item.Jid.Bare, nick);
            } else {
                // we do not know the real jid of this user, don't add it to our local roster
                person = new XmppPersonModel(jid, pres.From.Resource, this);
            }
            person.GetOrCreateMucResource(jid).Presence = pres;
            var msg = CreatePresenceUpdateMessage(person.Jid, person, pres);
            Session.AddMessageToChat(chat, msg);
            // clone directly to muc person chat
            // don't care about real jid, that has its own presence packets
            var personChat = Session.GetChat(jid, ChatType.Person, this);
            if (personChat != null) {
                Session.AddMessageToChat(personChat, msg);
            }
            switch (pres.Type) {
                case PresenceType.available:
                    // don't do anything if the contact already exists
                    if (chat.UnsafePersons.ContainsKey(person.ID)) {
                        return;
                    }
                    // is the chat synced? add the new contact the regular way
                    if (chat.IsSynced) {
                        Session.AddPersonToGroupChat(chat, person.ToPersonModel());
                        return;
                    }
                    
                    chat.UnsafePersons.Add(person.ID, person.ToPersonModel());
        
                    // did I join? then the chat roster is fully received
                    if (pres.From.Resource == chat.OwnNickname) {
                        chat.IsSynced = true;
                        Session.SyncChat(chat);
                        Session.EnableChat(chat);
                    }
                    break;
                case PresenceType.unavailable:
                    Session.RemovePersonFromGroupChat(chat, person.ToPersonModel());
                    // did I leave? then I "probably" left the room
                    if (pres.From.Resource == chat.OwnNickname) {
                        Session.RemoveChat(chat);
                    }
                break;
                case PresenceType.error:
                    if (pres.Error == null) break;
                    switch (pres.Error.Type) {
                        case ErrorType.cancel:
                            switch (pres.Error.Condition) {
                                case ErrorCondition.Conflict:
                                    // nickname already in use
                                    // autorejoin with _ appended to nickname
                                    JoinRoom(chat.ID, chat.OwnNickname + "_", chat.Password);
                                    break;
                            }
                            break;
                    }
                    break;
            }
        }

        void PrintPrivateChatPresence(XmppPersonModel person, Presence pres)
        {
            Jid jid = pres.From;
            MessageModel msg = CreatePresenceUpdateMessage(jid, person, pres);
            if (!String.IsNullOrEmpty(jid.Resource)) {
                var directchat = Session.GetChat(jid, ChatType.Person, this);
                if (directchat != null) {
                    // in case of direct chat we still send this message
                    Session.AddMessageToChat(directchat, msg);
                }
            }
            // a nonexisting resource going offline?
            if (pres.Type == PresenceType.unavailable) {
                if (!person.Resources.ContainsKey(jid.Resource??"")) {
                    return;
                }
            }
            var res = person.GetOrCreateResource(jid);
            var oldpres = res.Presence;
            res.Presence = pres;
            // highest pres
            Jid hjid = jid;
            Jid nextjid = jid;
            // 2nd highest pres
            Presence hpres = pres;
            Presence nextpres = null;
            bool amHighest = true;
            bool wasHighest = true;
            foreach (var pair in person.Resources) {
                if (pair.Value == res) continue;
                if (nextpres == null || pair.Value.Presence.Priority > nextpres.Priority) {
                    nextjid.Resource = pair.Key;
                    nextpres = pair.Value.Presence;
                }
                if (pair.Value.Presence.Priority > hpres.Priority) {
                    // someone has a higher priority than I do
                    // print the status of that resource
                    hjid.Resource = pair.Key;
                    hpres = pair.Value.Presence;
                    amHighest = false;
                }
                if (oldpres != null && pair.Value.Presence.Priority > oldpres.Priority) {
                    wasHighest = false;
                }
            }
            if (pres.Type == PresenceType.available) {
                // wasn't and isn't highiest prio -> ignore
                if (!wasHighest && !amHighest) return;
                // just another below zero prio -> ignore
                if (amHighest && pres.Priority < 0) return;
                // was highest, isn't anymore -> show presence of new highest
                if (wasHighest && !amHighest) {
                    msg = CreatePresenceUpdateMessage(hjid, person, hpres);
                }
            } else if (pres.Type == PresenceType.unavailable) {
                // still a resource left with positive priority
                if (nextpres != null && nextpres.Priority >= 0) {
                    msg = CreatePresenceUpdateMessage(nextjid, person, nextpres);
                }
            }
            var chat = Session.GetChat(jid.Bare, ChatType.Person, this);
            if (chat != null) {
                Session.AddMessageToChat(chat, msg);
            }
            if (ContactChat != null) {
                Session.AddMessageToChat(ContactChat, msg);
            }
        }

        void OnPrivateChatPresence(Presence pres)
        {
            Jid jid = pres.From;
            var person = GetOrCreateContact(jid.Bare, jid);
            PrintPrivateChatPresence(person, pres);
            switch (pres.Type) {
                case PresenceType.available:
                    if (pres.Priority < 0) break;
                    if (ContactChat == null) break;
                    lock (ContactChat) {
                        if (ContactChat.UnsafePersons.ContainsKey(jid.Bare)) break;
                        Session.AddPersonToGroupChat(ContactChat, person.ToPersonModel());
                    }
                    break;
                case PresenceType.unavailable:
                    person.RemoveResource(jid);
                    if (pres.Priority < 0) break;
                    if (ContactChat == null) break;
                    lock (ContactChat) {
                        if (!ContactChat.UnsafePersons.ContainsKey(jid.Bare)) break;
                        var pers = ContactChat.GetPerson(jid.Bare);
                        Session.RemovePersonFromGroupChat(ContactChat, pers);
                    }
                    break;
                case PresenceType.subscribe:
                    if (person.Ask == AskType.subscribe) {
                        // we are currently asking the contact OR are subscribed to him
                        // so we allow the contact to subscribe
                        // TODO: make the following dependent on some user setable boolean
                        JabberClient.PresenceManager.ApproveSubscriptionRequest(jid);
                    }
                    break;
                case PresenceType.subscribed:
                    // we are now able to see that contact's presences
                    break;
                case PresenceType.unsubscribed:
                    // the contact does not wish us to see his presences anymore
                    if (person.Subscription == SubscriptionType.from) {
                        // but the contact can still see us
                        // TODO: make the following dependent on some user setable boolean
                        JabberClient.PresenceManager.RefuseSubscriptionRequest(jid);
                    } else {
                        // TODO: this contact was just created in OnPresence… prevent it from doing that?
                        // TODO: this can happen when a subscription=none contact sends a deny…
                        Contacts.Remove(jid.Bare);
                    }
                    break;
                case PresenceType.unsubscribe:
                    // the contact does not wish to see our presence anymore?
                    // we could care less
                    break;
            }
        }
        
        void OnPresence(object sender, Presence pres)
        {
            Jid jid = pres.From;
            if (jid == JabberClient.MyJID) return; // we don't care about ourself
            if (pres.Capabilities != null && pres.Type == PresenceType.available) {
                // only test capabilities of users going online or changing something in their online state
                RequestCapabilities(jid, pres.Capabilities);
            }
            
            var groupChat = (XmppGroupChatModel) Session.GetChat(jid.Bare, ChatType.Group, this);
            
            if (groupChat != null) {
                OnGroupChatPresence(groupChat, pres);
            } else {
                OnPrivateChatPresence(pres);
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
                // TODO: or in case of a room message?
                person = new PersonModel(sender_id,
                                         sender_id,
                                         NetworkID, Protocol, this);
            }
            
            var builder = CreateMessageBuilder();
            // XXX maybe only a Google Talk bug requires this:
            if (msg.XDelay != null) {
                var stamp = msg.XDelay.Stamp;
                builder.TimeStamp = stamp;
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
            builder.AppendMessage(person, msg.Body.Trim());
            // mark highlights only for received messages
            if (person.ID != groupChat.OwnNickname) {
                builder.MarkHighlights();
            }
            Session.AddMessageToChat(groupChat, builder.ToMessage());
        }
        
        private void OnPrivateChatMessage(Message msg)
        {
            var chat = Session.GetChat(msg.From, ChatType.Person, this) as PersonChatModel;
            if (chat == null) {
                // in case full jid doesn't have a chat window, use bare jid
                chat = GetOrCreatePersonChat(msg.From.Bare);
            }
            var builder = CreateMessageBuilder();
            builder.AppendSenderPrefix(chat.Person, true);
            if (msg.Html != null) {
                builder.AppendHtmlMessage(msg.Html.ToString());
            } else {
                builder.AppendMessage(msg.Body.Trim());
            }
            builder.MarkHighlights();
            // todo: can private messages have an xdelay?
            if (msg.XDelay != null) {
                builder.TimeStamp = msg.XDelay.Stamp;
            }
            Session.AddMessageToChat(chat, builder.ToMessage());
        }

        void OnGroupChatMessageError (Message msg, XmppGroupChatModel chat)
        {
            var builder = CreateMessageBuilder();
            // TODO: nicer formatting
            if (msg.Error.ErrorText != null) {
                builder.AppendErrorText(msg.Error.ErrorText);
            } else {
                builder.AppendErrorText(msg.Error.ToString());
            }
            Session.AddMessageToChat(chat, builder.ToMessage());
        }

        void OnPrivateChatMessageError (Message msg, PersonChatModel chat)
        {
            var builder = CreateMessageBuilder();
            // TODO: nicer formatting
            if (msg.Error.ErrorText != null) {
                builder.AppendErrorText(msg.Error.ErrorText);
            } else {
                builder.AppendErrorText(msg.Error.ToString());
            }
            Session.AddMessageToChat(chat, builder.ToMessage());
        }

        private void OnMessage(object sender, Message msg)
        {
            // process chatstates
            if (msg.Chatstate != agsXMPP.protocol.extensions.chatstates.Chatstate.None) {
                OnChatState(msg);
            }
            if (String.IsNullOrEmpty(msg.Body)) {
                // TODO: capture events and stuff
                return;
            }
            switch (msg.Type) {
                case XmppMessageType.groupchat:
                    OnGroupChatMessage(msg);
                    break;
                case XmppMessageType.chat:
                case XmppMessageType.headline:
                case XmppMessageType.normal:
                    if (String.IsNullOrEmpty(msg.From.User)) {
                        OnServerMessage(msg);
                    } else if (msg.MucUser != null) {
                        OnMucMessage(msg);
                    } else {
                        OnPrivateChatMessage(msg);
                    }
                    break;
                case XmppMessageType.error:
                {
                    var chat = Session.GetChat(msg.From, ChatType.Group, this);
                    if (chat != null) {
                        OnGroupChatMessageError(msg, chat as XmppGroupChatModel);
                        break;
                    }
                    chat = Session.GetChat(msg.From, ChatType.Person, this);
                    if (chat != null) {
                        OnPrivateChatMessageError(msg, chat as PersonChatModel);
                        break;
                    }
                    // no person and no groupchat open? -> dump in networkchat
                    var builder = CreateMessageBuilder();
                    // TODO: nicer formatting
                    if (msg.Error.ErrorText != null) {
                        builder.AppendErrorText(msg.Error.ErrorText);
                    } else {
                        builder.AppendErrorText(msg.Error.ToString());
                    }
                    Session.AddMessageToChat(NetworkChat, builder.ToMessage());
                }
                    break;
            }
        }

        void OnMucMessage (Message msg)
        {
            User user = msg.MucUser;
            string text;
            if (user.Invite != null) {
                if (!String.IsNullOrWhiteSpace(user.Invite.Reason)) {
                    text = String.Format(_("You have been invited to {2} by {0} because {1}"),
                                         user.Invite.From,
                                         user.Invite.Reason,
                                         msg.From
                                         );
                } else {
                    text = String.Format(_("You have been invited to {1} by {0}"),
                                         user.Invite.From,
                                         msg.From
                                         );
                }
            } else {
                text = msg.ToString();
            }
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            var txt = builder.CreateText(text);
            txt.IsHighlight = true;
            builder.AppendText(txt);
            Session.AddMessageToChat(NetworkChat, builder.ToMessage());
            builder = CreateMessageBuilder();
            string url;
            if (!String.IsNullOrEmpty(user.Password)) {
                url = String.Format("xmpp:{0}?join;password={1}", msg.From, user.Password);
            } else {
                url = String.Format("xmpp:{0}?join", msg.From);
            }
            builder.AppendUrl(url, _("Accept invite (join room)"));
            Session.AddMessageToChat(NetworkChat, builder.ToMessage());
        }
        
        void OnChatState(Message msg)
        {
            if (msg.Body != null) return;
            switch (msg.Type) {
                case XmppMessageType.chat:
                case XmppMessageType.headline:
                case XmppMessageType.normal:
                {
                    var chat = GetChat(msg.From, ChatType.Person) as PersonChatModel;
                    // no full jid chat
                    if (chat == null) {
                        // create chat
                        chat = GetOrCreatePersonChat(msg.From.Bare);
                    }
                    var builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    builder.AppendIdendityName(chat.Person);
                    // TRANSLATOR: do NOT change the position of {0}!
                    builder.AppendText(_("{0} changed the chatstate to {1}"),
                                       String.Empty, msg.Chatstate.ToString());
                    Session.AddMessageToChat(chat, builder.ToMessage());
                }
                    break;
                default:
                    break;
            }
        }

        void OnServerMessage(Message msg)
        {
            var builder = CreateMessageBuilder();
            builder.AppendText("<{0}> {1}", msg.From, msg.Body);
            builder.MarkHighlights();
            // todo: can server messages have an xdelay?
            if (msg.XDelay != null) {
                builder.TimeStamp = msg.XDelay.Stamp;
            }
            Session.AddMessageToChat(NetworkChat, builder.ToMessage());
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
            
            if (!SupressLocalMessageEcho && (query.Body == LastSentMessage)) {
                SupressLocalMessageEcho = true;
                return;
            }
            var chat = GetOrCreatePersonChat(query.To);

            _Say(chat, query.Body, false);
        }

        private PersonChatModel GetOrCreatePersonChat(Jid jid)
        {
            var chat = (PersonChatModel) Session.GetChat(jid, ChatType.Person, this);
            if (chat != null) return chat;
            var person = GetOrCreateContact(jid.Bare, jid);
            PersonModel pers;
            if (!String.IsNullOrEmpty(jid.Resource)) {
                pers = new PersonModel(jid, person.IdentityName, NetworkID, Protocol, this);
            } else {
                pers = person.ToPersonModel();
            }
            chat = Session.CreatePersonChat(pers, this);
            Session.AddChat(chat);
            Session.SyncChat(chat);
            return chat;
        }

        void OnDisconnect(object sender)
        {
            Trace.Call(sender);
            if (ContactChat != null) {
                Session.DisableChat(ContactChat);
            }

            IsConnected = false;
            OnDisconnected(EventArgs.Empty);
            JabberClient = null;
            MucManager = null;
            Contacts = null;
            Disco = null;
            if (AutoReconnect) {
                Connect();
            }
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
            if (JabberClient.ServerCapabilities != null) {
                RequestCapabilities(JabberClient.MyJID.Server, JabberClient.ServerCapabilities.Version);
            }

            OnConnected(EventArgs.Empty);
        }

        private void ApplyConfig(UserConfig config, XmppServerModel server)
        {
            if (server.Username.Contains("@")) {
                var jid_user = server.Username.Split('@')[0];
                var jid_host = server.Username.Split('@')[1];
                JabberClient.ConnectServer = server.Hostname;
                JabberClient.AutoResolveConnectServer = false;
                JabberClient.Username = jid_user;
                JabberClient.Server = jid_host;
            } else {
                JabberClient.Server = server.Hostname;
                JabberClient.Username = server.Username;
            }
            JabberClient.Port = server.Port;
            JabberClient.Password = server.Password;

            Me = new PersonModel(
                String.Format("{0}@{1}",
                    JabberClient.Username,
                    JabberClient.Server
                ),
                JabberClient.Username
                , NetworkID, Protocol, this
            );
            Me.IdentityNameColored.ForegroundColor = new TextColor(0, 0, 255);
            Me.IdentityNameColored.BackgroundColor = TextColor.None;
            Me.IdentityNameColored.Bold = true;

            // XMPP specific settings
            JabberClient.Resource = server.Resource;
            
            Nicknames = (string[]) config["Connection/Nicknames"];

            JabberClient.UseStartTLS = server.UseEncryption;
            if (!server.ValidateServerCertificate) {
                JabberClient.ClientSocket.OnValidateCertificate += ValidateCertificate;
            }
        }

        private static bool ValidateCertificate(object sender,
                                         X509Certificate certificate,
                                         X509Chain chain,
                                         SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
