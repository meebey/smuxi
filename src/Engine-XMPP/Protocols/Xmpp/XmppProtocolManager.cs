/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2013 Mirco Bauer <meebey@meebey.net>
 * Copyright (c) 2011 Tuukka Hastrup <Tuukka.Hastrup@iki.fi>
 * Copyright (c) 2013 Oliver Schneider <smuxi@oli-obk.de>
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using agsXMPP;
using agsXMPP.protocol;
using agsXMPP.protocol.client;
using agsXMPP.protocol.x.muc;
using agsXMPP.protocol.iq;
using agsXMPP.protocol.iq.roster;
using agsXMPP.protocol.iq.disco;
using agsXMPP.protocol.extensions.caps;
using agsXMPP.protocol.extensions.chatstates;
using XmppMessageType = agsXMPP.protocol.client.MessageType;
using agsXMPP.Factory;
using agsXMPP.Net;

using Starksoft.Net.Proxy;

using Smuxi.Common;
using System.Runtime.CompilerServices;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "jabber")]
    public class JabberProtocolManager : XmppProtocolManager
    {
        public override string Protocol {
            get {
                return "Jabber";
            }
        }

        public JabberProtocolManager(Session session) : base(session)
        {
        }
    }
    
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "xmpp")]
    public class XmppProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        static readonly string LibraryTextDomain = "smuxi-engine-xmpp";
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
        TimeSpan AutoReconnectDelay { get; set; }
        bool IsFacebook { get; set; }
        bool IsDisposed { get; set; }
        bool ShowChatStates { get; set; }
        // pidgin's psychic mode
        bool OpenNewChatOnChatState { get; set; }

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

        public override bool IsConnected {
            get {
                return JabberClient.Authenticated;
            }
        }

        public XmppProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);
            Contacts = new Dictionary<Jid, XmppPersonModel>();
            DiscoCache = new Dictionary<string, DiscoInfo>();

            SupressLocalMessageEcho = false;
            ShowChatStates = true;
            OpenNewChatOnChatState = true;

            JabberClient = new XmppClientConnection();
            JabberClient.AutoRoster = true;
            JabberClient.AutoPresence = true;
            JabberClient.OnMessage += OnMessage;
            JabberClient.OnClose += OnClose;
            JabberClient.OnLogin += OnLogin;
            JabberClient.OnError += OnError;
            JabberClient.OnStreamError += OnStreamError;
            JabberClient.OnPresence += OnPresence;
            JabberClient.OnRosterItem += OnRosterItem;
            JabberClient.OnReadXml += OnReadXml;
            JabberClient.OnWriteXml += OnWriteXml;
            JabberClient.OnAuthError += OnAuthError;
            JabberClient.OnIq += OnIq;
            JabberClient.SendingServiceUnavailable += OnSendingServiceUnavailable;
            JabberClient.AutoAgents = false; // outdated feature
            JabberClient.EnableCapabilities = true;
            JabberClient.Capabilities.Node = "https://smuxi.im";
            JabberClient.ClientVersion = Engine.VersionString;

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
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnSendingServiceUnavailable(object sender, SendingServiceUnavailableEventArgs e)
        {
            if (e.Stanza.To == null) {
                // can only be received by the server
                return;
            }
            if (e.Stanza.To == JabberClient.MyJID.Server) {
                // explicitly targeting the server
                return;
            }
            XmppPersonModel person;
            if (!Contacts.TryGetValue(e.Stanza.To.Bare, out person)) {
                e.Cancel = true;
                return;
            }
            if (person.Subscription != SubscriptionType.both &&
                person.Subscription != SubscriptionType.from) {
                e.Cancel = true;
                return;
            }
            // the person already knows we are online, this does not give away our privacy
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnStreamError(object sender, agsXMPP.Xml.Dom.Element e)
        {
            Trace.Call(sender, e);
            var error = e as agsXMPP.protocol.Error;
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            // TODO: create user readable error messages from the error.Condition
            //builder.AppendErrorText(error.Condition.ToString());
            switch (error.Condition) {
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

        [MethodImpl(MethodImplOptions.Synchronized)]
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Connect(FrontendManager fm, ServerModel server)
        {
            Trace.Call(fm, server);

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
                NetworkID, String.Format("{0} {1}", Protocol, Host), this
            );
            Session.AddChat(NetworkChat);
            Session.SyncChat(NetworkChat);

            Connect();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void Connect()
        {
            Trace.Call();
            Contacts.Clear();

            AutoReconnect = true;
            AutoReconnectDelay = TimeSpan.FromMinutes(1);

            ApplyConfig(Session.UserConfig, Server);

            OpenContactChat();
            
#if LOG4NET
            _Logger.Debug("calling JabberClient.Open()");
#endif
            IsFacebook = (JabberClient.Server == "chat.facebook.com");
            JabberClient.Open();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            // IsConnected checks for a working xmpp connection
            // we need to know the socket's state here
            if (JabberClient.XmppConnectionState != XmppConnectionState.Disconnected) {
                AutoReconnect = true;
                AutoReconnectDelay = TimeSpan.Zero;
                JabberClient.Close();
            } else {
                JabberClient.ClientSocket.OnValidateCertificate -= ValidateCertificate;
                JabberClient.SocketConnectionType = SocketConnectionType.Direct;
                Reconnect();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            AutoReconnect = false;
            JabberClient.Close();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Dispose()
        {
            Trace.Call();
            IsDisposed = true;

            base.Dispose();
            AutoReconnect = false;
            JabberClient.OnMessage -= OnMessage;
            JabberClient.OnClose -= OnClose;
            JabberClient.OnLogin -= OnLogin;
            JabberClient.OnError -= OnError;
            JabberClient.OnStreamError -= OnStreamError;
            JabberClient.OnPresence -= OnPresence;
            JabberClient.OnRosterItem -= OnRosterItem;
            JabberClient.OnReadXml -= OnReadXml;
            JabberClient.OnWriteXml -= OnWriteXml;
            JabberClient.OnAuthError -= OnAuthError;
            JabberClient.OnIq -= OnIq;
            JabberClient.ClientSocket.OnValidateCertificate -= ValidateCertificate;
            JabberClient.SendingServiceUnavailable -= OnSendingServiceUnavailable;
            JabberClient.SocketDisconnect();
        }

        // this method is used as status / title
        public override string ToString()
        {
            var status = String.Format("{0} ({1})", JabberClient.Server, Protocol);
            if (!IsConnected) {
                status += " (" + _("not connected") + ")";
            }
            return status;
        }

        DiscoItems ServerDiscoItems { get; set; }
        List<Jid> CachedMucJids { get; set; }
        Dictionary<Jid, DiscoInfo> CachedMucInfo { get; set; }
        DateTime CachedMucJidsTimeStamp { get; set; }

        // no need to synchronize this method as it only checks for null
        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);
            
            var list = new List<GroupChatModel>();
            if (ContactChat == null) {
                list.Add(new GroupChatModel("Contacts", "Contacts", this));
            }

            // find all transport/conference groups/whatnot
            DiscoItem[] discoItems;
            if (ServerDiscoItems == null) {
                var reset = new AutoResetEvent(false);
                lock (this) {
                    Disco.DiscoverItems(JabberClient.Server, (sender, e) => FindGroupChatsDiscoItems(e, reset));
                }
                reset.WaitOne();
            }
            lock (this) {
                if (ServerDiscoItems == null) {
                    return list;
                } else {
                    discoItems = ServerDiscoItems.GetDiscoItems();
                }
            }

            var resetList = new List<AutoResetEvent>();

            if ((CachedMucJids == null) ||
                ((DateTime.Now - CachedMucJidsTimeStamp) > TimeSpan.FromMinutes(5))) {
                // find all conference groups
                var mucList = new List<Jid>();
                foreach (var discoItem in discoItems) {
                    var reset = new AutoResetEvent(false);
                    var jid = discoItem.Jid;
                    lock (this) {
                        Disco.DiscoverInformation(discoItem.Jid, (sender, e) => FindGroupChatsItemDiscoInfo(e, reset, mucList, jid));
                    }
                    resetList.Add(reset);
                }
                foreach (var reset in resetList) {
                    reset.WaitOne();
                }
                resetList.Clear();

                // find all chats in all conference groups
                var jidList = new List<Jid>();
                foreach (var mucGroup in mucList) {
                    var reset = new AutoResetEvent(false);
                    lock (this) {
                        Disco.DiscoverItems(mucGroup, (sender, e) => FindGroupChatsDiscoMucs(e, reset, jidList));
                    }
                    resetList.Add(reset);
                }
                foreach (var reset in resetList) {
                    reset.WaitOne();
                }
                CachedMucJids = jidList;
                CachedMucJidsTimeStamp = DateTime.Now;
                CachedMucInfo = new Dictionary<Jid, DiscoInfo>();
            }

            // filter found items
            var filteredList = new List<Jid>();
            if (filter == null || String.IsNullOrEmpty(filter.Name)) {
                filteredList = CachedMucJids;
            } else {
                string searchPattern = null;
                if (!filter.Name.StartsWith("*") && !filter.Name.EndsWith("*")) {
                    searchPattern = String.Format("*{0}*", filter.Name);
                } else {
                    searchPattern = filter.Name;
                }
                foreach (var jid in CachedMucJids) {
                    if (!Pattern.IsMatch(jid, searchPattern)) {
                        continue;
                    }
                    filteredList.Add(jid);
                }
            }

            // get info on all chats matching the pattern
            resetList.Clear();
            foreach (var jid in CachedMucJids) {
                bool isCached = false;
                DiscoInfo info;
                lock (this) {
                    isCached = CachedMucInfo.TryGetValue(jid, out info);
                }
                if (isCached) {
                    FindGroupChatsChatInfoParse(jid, info, list);
                    continue;
                }
                var reset = new AutoResetEvent(false);
                lock (this) {
                    Disco.DiscoverInformation(jid, (sender, e) => FindGroupChatsChatInfo(e, reset, list));
                }
                resetList.Add(reset);
            }
            foreach (var reset in resetList) {
                reset.WaitOne();
            }
            return list;
        }

        void FindGroupChatsChatInfoParse(Jid jid, DiscoInfo items, List<GroupChatModel> list)
        {
            var ident = items.SelectSingleElement<DiscoIdentity>();
            string name;
            if (ident != null && !String.IsNullOrEmpty(ident.Name)) {
                name = ident.Name + " [" + jid + "]";
            } else {
                name = jid;
            }
            var chat = new GroupChatModel(jid, name, null);
            chat.PersonCount = -1;
            var x = items.SelectSingleElement<agsXMPP.protocol.x.data.Data>();
            if (x != null) {
                var users_field = x.GetField("muc#roominfo_occupants");
                var topic_field = x.GetField("muc#roominfo_subject");
                var desc_field = x.GetField("muc#roominfo_description");
                if (users_field != null) {
                    chat.PersonCount = int.Parse(users_field.GetValue());
                }
                if (topic_field != null) {
                    chat.Topic = new MessageModel(topic_field.GetValue());
                } else if (desc_field != null) {
                    chat.Topic = new MessageModel(desc_field.GetValue());
                }
            }
            lock (list) {
                list.Add(chat);
            }
        }

        void FindGroupChatsChatInfo(IQEventArgs e, AutoResetEvent reset, List<GroupChatModel> list)
        {
            if (e.IQ.Error == null) {
                var items = (DiscoInfo)e.IQ.Query;
                lock (this) {
                    CachedMucInfo[e.IQ.From] = items;
                }
                FindGroupChatsChatInfoParse(e.IQ.From, items, list);
            }
            e.Handled = true;
            reset.Set();
        }

        void FindGroupChatsDiscoMucs(IQEventArgs e, AutoResetEvent reset, List<Jid> list)
        {
            if (e.IQ.Error == null) {
                var items = (DiscoItems)e.IQ.Query;
                foreach (var item in items.GetDiscoItems()) {
                    // no locking required, these callbacks are sequential
                    list.Add(item.Jid);
                }
            }
            e.Handled = true;
            reset.Set();
        }

        void FindGroupChatsItemDiscoInfo(IQEventArgs e, AutoResetEvent reset, List<Jid> mucList, Jid jid)
        {
            if (e.IQ.Error == null) {
                var discoInfo = (DiscoInfo)e.IQ.Query;
                if (discoInfo.HasFeature(agsXMPP.Uri.MUC)) {
                    // no locking required, these callbacks are sequential
                    mucList.Add(jid);
                }
            }
            e.Handled = true;
            reset.Set();
        }

        void FindGroupChatsDiscoItems(IQEventArgs e, AutoResetEvent reset)
        {
            if (e.IQ.Error == null) {
                lock (this) {
                    ServerDiscoItems = (DiscoItems)e.IQ.Query;
                }
            }
            e.Handled = true;
            reset.Set();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OpenContactChat()
        {
            if (ContactChat == null) {
                ContactChat = Session.CreateChat<GroupChatModel>(
                    "Contacts", "Contacts", this
                );
                Session.AddChat(ContactChat);
            } else if (!ContactChat.IsEnabled) {
                Session.EnableChat(ContactChat);
            } else {
                // already open
                return;
            }

            foreach (var pair in Contacts) {
                if (pair.Value.Resources.Count != 0) {
                    ContactChat.UnsafePersons.Add(pair.Key, pair.Value.ToPersonModel());
                }
            }

            // HACK: lower probability of sync race condition during connect
            ThreadPool.QueueUserWorkItem(delegate {
                Thread.Sleep(5000);
                lock (this) {
                    if (IsDisposed) {
                        return;
                    }
                    if (ContactChat != null) {
                        Session.SyncChat(ContactChat);
                    }
                }
            });
        }

        // no need to synchronize as no members are accessed
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void CloseChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);

            if (chat == ContactChat) {
                Session.RemoveChat(chat);
                ContactChat = null;
            } else if (chat.ChatType == ChatType.Group) {
                if (IsConnected) {
                    MucManager.LeaveRoom(chat.ID, ((XmppGroupChatModel)chat).OwnNickname);
                } else {
                    Session.RemoveChat(chat);
                }
            } else if (chat.ChatType == ChatType.Person) {
                Session.RemoveChat(chat);
            } else {
#if LOG4NET
                _Logger.Error("CloseChat(): Invalid chat type");
#endif
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void SetPresenceStatus(PresenceStatus status,
                                               string message)
        {
            Trace.Call(status, message);

            if (!IsConnected) {
                return;
            }

            switch (status) {
                case PresenceStatus.Online:
                    JabberClient.Show = ShowType.NONE;
                    JabberClient.Priority = Server.Priorities[status];
                    JabberClient.Status = message;
                    break;
                case PresenceStatus.Away:
                    JabberClient.Priority = Server.Priorities[status];
                    JabberClient.Show = ShowType.away;
                    JabberClient.Status = message;
                    break;
            }

            JabberClient.SendMyPresence();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CommandRegister(CommandModel command)
        {
            Trace.Call(command);
            Connect();
            JabberClient.RegisterAccount = true;
            // TODO: add callbacks to process in case of error or success
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
                        case "me":
                            CommandMe(command);
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
                        case "register":
                            CommandRegister(command);
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

        public void CommandMe(CommandModel command)
        {
            if (command.Data.Length <= 4) {
                return;
            }

            string actionstring = command.Data.Substring(3);
            // http://xmpp.org/extensions/xep-0245.html
            // says we should append "/me " no matter what our command char is
            _Say(command.Chat, "/me" + actionstring, true, false);

            // groupchat echos messages anyway
            if (command.Chat.ChatType == ChatType.Person) {
                var builder = CreateMessageBuilder();
                builder.AppendActionPrefix();
                builder.AppendIdendityName(Me);
                builder.AppendText(actionstring);
                Session.AddMessageToChat(command.Chat, builder.ToMessage());
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
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

        [MethodImpl(MethodImplOptions.Synchronized)]
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
                Session.AddMessageToFrontend(cmd, builder.ToMessage());
                return;
            }
            if (!String.IsNullOrEmpty(jid.Resource)) {
                if (person.Resources.Count > 1) {
                    builder.AppendText(_("Contact {0} has {1} known resources"), jid.Bare, person.Resources.Count);
                }
                XmppResourceModel res;
                if (!person.Resources.TryGetValue(jid.Resource??"", out res)) {
                    builder.AppendErrorText(_("{0} is not a known resource"), jid.Resource);
                    Session.AddMessageToFrontend(cmd, builder.ToMessage());
                    return;
                }
                printResource(builder, res);
                Session.AddMessageToFrontend(cmd, builder.ToMessage());
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
                case SubscriptionType.remove:
#if LOG4NET
                    _Logger.Error("a contact with SubscriptionType remove has been found");
#endif
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
            Session.AddMessageToFrontend(cmd, builder.ToMessage());
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
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
                        JabberClient.RosterManager.UpdateRosterItem(jid, "");
                    } else {
                        var newNick = String.Join(" ", cd.DataArray.Skip(3).ToArray());
                        JabberClient.RosterManager.UpdateRosterItem(jid, newNick);
                    }
                    break;
                default:
                    var builder = CreateMessageBuilder();
                    builder.AppendText(_("Invalid Contact command: {0}"), cmd);
                    Session.AddMessageToFrontend(cd, builder.ToMessage());
                    return;
            }
        }

        public void CommandHelp(CommandModel cmd)
        {
            var builder = CreateMessageBuilder();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            builder.AppendHeader(_("{0} Commands"), Protocol);
            Session.AddMessageToFrontend(cmd, builder.ToMessage());

            string[] help = {
            "connect xmpp/jabber server port username password [resource]",
            "msg/query jid/nick message",
            "say message",
            "join muc-jid [password]",
            "part/leave [muc-jid]",
            "away [away-message]",
            "roster [full]",
            "contact add/remove jid/nick",
            "contact rename jid/nick [newnick]"
            };

            foreach (string line in help) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                Session.AddMessageToFrontend(cmd, builder.ToMessage());
            }

            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            builder = CreateMessageBuilder();
            builder.AppendHeader(_("Advanced {0} Commands"), Protocol);
            Session.AddMessageToFrontend(cmd, builder.ToMessage());

            string[] help2 = {
            "contact addonly/subscribe/unsubscribe/approve/deny",
            "whois jid",
            "joinas muc-jid nickname [password]",
            "priority away/online/temp priority-value"
            };

            foreach (string line in help2) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                Session.AddMessageToFrontend(cmd, builder.ToMessage());
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
                    Session.AddMessageToFrontend(cd, builder.ToMessage());
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CommandPriority(CommandModel command)
        {
            if (command.DataArray.Length < 3) {
                var builder = CreateMessageBuilder();
                builder.AppendText(_("Priority for Available is: {0}"), Server.Priorities[PresenceStatus.Online]);
                Session.AddMessageToFrontend(command, builder.ToMessage());
                builder = CreateMessageBuilder();
                builder.AppendText(_("Priority for Away is: {0}"), Server.Priorities[PresenceStatus.Away]);
                Session.AddMessageToFrontend(command, builder.ToMessage());
                return;
            }
            string subcmd = command.DataArray[1];
            int prio;
            if (!int.TryParse(command.DataArray[2], out prio) || prio < -128 || prio > 127) {
                var builder = CreateMessageBuilder();
                builder.AppendText(_("Invalid Priority: {0} (valid priorities are between -128 and 127 inclusive)"), command.DataArray[2]);
                Session.AddMessageToFrontend(command, builder.ToMessage());
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        Jid GetJidFromNickname(string nickname)
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        void MessageQuery(Jid jid, string message)
        {
            var chat = GetOrCreatePersonChat(jid);
            if (message != null && message.Trim().Length > 0) {
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

        [MethodImpl(MethodImplOptions.Synchronized)]
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

        [MethodImpl(MethodImplOptions.Synchronized)]
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

        void Invite(string[] jids_string, string room, string reason, string password)
        {
            var jids = new Jid[jids_string.Length];
            for (int i = 0; i < jids.Length; i++) {
                jids[i] = jids_string[i];
            }
            Invite(jids, room, reason, password);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CommandRoster(CommandModel cd)
        {
            bool full = false;
            if (cd.Parameter == "full") {
                full = true;
            }

            MessageBuilder builder = CreateMessageBuilder();
            builder.AppendHeader("Roster");
            Session.AddMessageToFrontend(cd, builder.ToMessage());

            foreach (var pair in Contacts) {
                string status = "+";
                var contact = pair.Value;
                if (contact.Resources.Count == 0) {
                    if (!full) {
                        continue;
                    }
                    status = "-";
                }
                builder = CreateMessageBuilder();
                builder.AppendText("{0} {1}\t({2}): {3},{4}",
                                   status,
                                   contact.IdentityName,
                                   pair.Key,
                                   contact.Subscription,
                                   contact.Ask
                );
                foreach (var p in contact.Resources) {
                    builder.AppendText("\t|\t{0}:{1}:{2}",
                                       p.Key,
                                       p.Value.Presence.Type.ToString(),
                                       p.Value.Presence.Priority
                    );
                    if (!String.IsNullOrEmpty(p.Value.Presence.Status)) {
                        builder.AppendText(":\"{0}\"", p.Value.Presence.Status);
                    }
                }
                Session.AddMessageToFrontend(cd, builder.ToMessage());
            }
        }

        public void CommandSay(CommandModel cd)
        {
            _Say(cd.Chat, cd.Parameter);
        }  

        void _Say(ChatModel chat, string text)
        {
            _Say(chat, text, true);
        }

        void _Say(ChatModel chat, string text, bool send)
        {
            _Say(chat, text, send, true);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void _Say(ChatModel chat, string text, bool send, bool display)
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
                    if ((jid.Server == "gmail.com") ||
                        (jid.Server == "googlemail.com")) {
                        // don't send to all high prio resources or to specific resources
                        // because gtalk clones any message to all resources anyway
                        JabberClient.Send(new Message(jid.Bare, XmppMessageType.chat, text));
                    } else if (!String.IsNullOrEmpty(jid.Resource)) {
                        JabberClient.Send(new Message(jid, XmppMessageType.chat, text));
                    } else {
                        var resources = person.GetResourcesWithHighestPriority();
                        if (resources.Count == 0) {
                            // no connected resource, send to bare jid
                            JabberClient.Send(new Message(jid.Bare, XmppMessageType.chat, text));
                        } else {
                            foreach (var res in resources) {
                                Jid j = new Jid(jid);
                                j.Resource = res.Name;
                                JabberClient.Send(new Message(j, XmppMessageType.chat, text));
                            }
                        }
                    }
                } else if (chat.ChatType == ChatType.Group) {
                    JabberClient.Send(new Message(chat.ID, XmppMessageType.groupchat, text));
                    return; // don't show now. the message will be echoed back if it's sent successfully
                }
                if (IsFacebook && SupressLocalMessageEcho) {
                    // don't show, facebook is bugging again
                    return;
                }
                LastSentMessage = text;
            }

            var builder = CreateMessageBuilder();
            builder.AppendSenderPrefix(Me);
            builder.AppendMessage(text);
            var msg = builder.ToMessage();
            if (display) {
                Session.AddMessageToChat(chat, msg);
            }
            OnMessageSent(
                new MessageEventArgs(chat, msg, null, chat.ID)
            );
        }

        void OnReadXml(object sender, string text)
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

        void OnWriteXml(object sender, string text)
        {
            if (!DebugProtocol) {
                return;
            }

            try {
                if (text == null || text.Trim().Length == 0) {
                    // suppress logging keep-alive messages
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        XmppPersonModel GetOrCreateContact(Jid jid, string name)
        {
            XmppPersonModel p;
            if (!Contacts.TryGetValue(jid.Bare, out p)) {
                p = new XmppPersonModel(jid, name, this);
                Contacts[jid.Bare] = p;
            }
            return p;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnRosterItem(object sender, RosterItem rosterItem)
        {
            // setting to none also removes the person from chat, as we'd never get an offline message anymore
            if (rosterItem.Subscription == SubscriptionType.none
                || rosterItem.Subscription == SubscriptionType.remove) {
                if (rosterItem.Subscription == SubscriptionType.remove) {
                    Contacts.Remove(rosterItem.Jid);
                }
                if (ContactChat == null) {
                    return;
                }
                PersonModel oldp = ContactChat.GetPerson(rosterItem.Jid);
                if (oldp == null) {
                    // doesn't exist, don't need to do anything
                    return;
                }
                Session.RemovePersonFromGroupChat(ContactChat, oldp);
                return;
            }
            // create or update a roster item
            var contact = GetOrCreateContact(rosterItem.Jid.Bare, rosterItem.Name ?? rosterItem.Jid);
            contact.Temporary = false;
            contact.Subscription = rosterItem.Subscription;
            contact.Ask = rosterItem.Ask;
            string oldIdentityName = contact.IdentityName;
            var oldIdentityNameColored = contact.IdentityNameColored;
            if (IsFacebook) {
                // facebook bug. prevent clearing of name
                if (rosterItem.Name != null) {
                    contact.IdentityName = rosterItem.Name;
                }
            } else {
                contact.IdentityName = rosterItem.Name ?? rosterItem.Jid;
            }

            if (oldIdentityName == contact.IdentityName) {
                // identity name didn't change
                // the rest of this function only handles changed identity names
                return;
            }

            contact.IdentityNameColored = null; // uncache

            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            string idstring = "";
            if (!IsFacebook && oldIdentityName != contact.Jid) {
                idstring = " [" + contact.Jid + "]";
            }
            oldIdentityNameColored.BackgroundColor = TextColor.None;
            builder.AppendFormat("{2}{1} is now known as {0}", contact, idstring, oldIdentityNameColored);

            if (ContactChat != null) {
                PersonModel oldp = ContactChat.GetPerson(rosterItem.Jid.Bare);
                if (oldp == null) {
                    // doesn't exist, don't need to do anything
                    return;
                }
                Session.UpdatePersonInGroupChat(ContactChat, oldp, contact.ToPersonModel());

                Session.AddMessageToChat(ContactChat, builder.ToMessage());
            }
            
            var chat = Session.GetChat(rosterItem.Jid.Bare, ChatType.Person, this) as PersonChatModel;
            if (chat != null) {
                // TODO: implement update chat
                var oldp = chat.Person;
                Session.RemoveChat(chat);
                chat = Session.CreatePersonChat(oldp, this);
                Session.AddChat(chat);
                Session.AddMessageToChat(chat, builder.ToMessage());
                Session.SyncChat(chat);
            }
        }

        void RequestCapabilities(Jid jid, Capabilities caps)
        {
            string hash = caps.Node + "#" + caps.Version;
            RequestCapabilities(jid, hash);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
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
            Disco.DiscoverInformation(jid,
                (object sender, IQEventArgs e) =>
                    OnDiscoInfo(e, hash)
            );
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void AddCapabilityToResource(Jid jid, DiscoInfo info)
        {
            XmppPersonModel contact;
            if (!Contacts.TryGetValue(jid.Bare, out contact)) {
                return;
            }
            XmppResourceModel res;
            if (!contact.Resources.TryGetValue(jid.Resource??"", out res)) {
                return;
            }
            res.Disco = info;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnDiscoInfo(IQEventArgs e, string hash)
        {
            if (e.IQ.Error != null) {
                var msg = CreateMessageBuilder();
                msg.AppendEventPrefix();
                msg.AppendErrorText(_("An error happened during service discovery for {0}: {1}"),
                                    e.IQ.From,
                                    e.IQ.Error.ErrorText ?? e.IQ.Error.Condition.ToString());
                Session.AddMessageToChat(NetworkChat, msg.ToMessage());
                // clear item from cache so the request is done again some time
                DiscoCache.Remove(hash);
                e.Handled = true;
                return;
            }
            if (e.IQ.Type != IqType.result) {
#if LOG4NET
                _Logger.Error("OnDiscoInfo(): iq is not a result");
#endif
                return;
            }
            if (!(e.IQ.Query is DiscoInfo)) {
#if LOG4NET
                _Logger.Error("OnDiscoInfo(): query is not a DiscoInfo");
#endif
                return;
            }
            var info = (DiscoInfo)e.IQ.Query;
            DiscoCache[hash] = info;
            e.Handled = true;
            if (String.IsNullOrEmpty(e.IQ.From.User)) {
                // server capabilities
                var builder = CreateMessageBuilder();
                builder.AppendText("The Server supports the following features: ");
                Session.AddMessageToChat(NetworkChat, builder.ToMessage());
                foreach (var feature in info.GetFeatures()) {
                    builder = CreateMessageBuilder();
                    builder.AppendText(feature.Var);
                    Session.AddMessageToChat(NetworkChat, builder.ToMessage());
                }
            } else {
                AddCapabilityToResource(e.IQ.From, info);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        MessageModel CreatePresenceUpdateMessage(Jid jid, PersonModel person, Presence pres)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            string idstring = "";
            // print jid (except in case of facebook where it is meaningless)
            if (!IsFacebook && jid.Bare != person.IdentityName) {
                idstring = String.Format(" [{0}]", jid.Bare);
            }
            // print the type (and in case of available detailed type)
            switch (pres.Type) {
                case PresenceType.available:
                    switch(pres.Show) {
                        case ShowType.NONE:
                            builder.AppendFormat(_("{0}{1} is available"), person, idstring);
                            builder.AppendPresenceState(person, MessageType.PresenceStateOnline);
                            break;
                        case ShowType.away:
                            builder.AppendFormat(_("{0}{1} is away"), person, idstring);
                            builder.AppendPresenceState(person, MessageType.PresenceStateAway);
                            break;
                        case ShowType.xa:
                            builder.AppendFormat(_("{0}{1} is extended away"), person, idstring);
                            builder.AppendPresenceState(person, MessageType.PresenceStateAway);
                            break;
                        case ShowType.dnd:
                            builder.AppendFormat(_("{0}{1} wishes not to be disturbed"), person, idstring);
                            builder.AppendPresenceState(person, MessageType.PresenceStateAway);
                            break;
                        case ShowType.chat:
                            builder.AppendFormat(_("{0}{1} wants to chat"), person, idstring);
                            builder.AppendPresenceState(person, MessageType.PresenceStateOnline);
                            break;
                    }
                    break;
                case PresenceType.unavailable:
                    builder.AppendPresenceState(person, MessageType.PresenceStateOffline);
                    builder.AppendFormat(_("{0}{1} is offline"), person, idstring);
                    break;
                case PresenceType.subscribe:
                    if ((person as XmppPersonModel).Ask == AskType.subscribe) {
                        builder = CreateMessageBuilder();
                        builder.AppendActionPrefix();
                        builder.AppendFormat(_("Automatically allowed {0} to subscribe to you, since you are already asking to subscribe"),
                                           person
                        );
                    } else {
                        builder.AppendFormat(_("{0}{1} wishes to subscribe to you"),
                                             person, idstring);
                        // you have to respond
                        builder.MarkAsHighlight();
                    }
                    break;
                case PresenceType.subscribed:
                    // you can now see their presences
                    builder.AppendFormat(_("{0}{1} allowed you to subscribe"), person, idstring);
                    break;
                case PresenceType.unsubscribed:
                    if ((person as XmppPersonModel).Subscription == SubscriptionType.from) {
                        builder = CreateMessageBuilder();
                        builder.AppendActionPrefix();
                        builder.AppendFormat(
                            _("Automatically removed {0}'s subscription to " +
                              "your presences after losing the subscription " +
                              "to theirs"),
                            person
                        );
                    } else {
                        // you cannot (anymore?) see their presences
                        builder.AppendFormat(_("{0}{1} denied/removed your subscription"), person, idstring);
                    }
                    break;
                case PresenceType.unsubscribe:
                    // you might still be able to see their presences
                    builder.AppendFormat(_("{0}{1} unsubscribed from you"), person, idstring);
                    break;
                case PresenceType.error:
                    if (pres.Error == null) {
                        builder.AppendErrorText(_("received a malformed error message: {0}"), pres);
                        break;
                    }
                    switch (pres.Error.Type) {
                        case ErrorType.cancel:
                            switch (pres.Error.Condition) {
                                case ErrorCondition.RemoteServerNotFound:
                                    builder.AppendErrorText(_("{0}{1}'s server could not be found"), person.IdentityName, idstring);
                                    break;
                                case ErrorCondition.Conflict:
                                    builder.AppendErrorText(_("{0}{1} is already using your requested resource"), person.IdentityName, idstring);
                                    break;
                                default:
                                    if (!String.IsNullOrEmpty(pres.Error.ErrorText)) {
                                        builder.AppendErrorText(pres.Error.ErrorText);
                                    } else {
                                        builder.AppendErrorText(
                                            _("There is currently no useful error message for {0}, {1}, {2}{3}"),
                                            pres.Error.Type,
                                            pres.Error.Condition,
                                            person.IdentityName,
                                            idstring);
                                    }
                                    break;
                            }
                            break;
                        case ErrorType.auth:
                            switch (pres.Error.Condition) {
                                case ErrorCondition.Forbidden:
                                    builder.AppendErrorText(
                                        _("You do not have permission to access {0}{1}")
                                        , person.IdentityName,
                                        idstring);
                                    break;
                                default:
                                    if (!String.IsNullOrEmpty(pres.Error.ErrorText)) {
                                        builder.AppendErrorText(pres.Error.ErrorText);
                                    } else {
                                        builder.AppendErrorText(
                                            _("There is currently no useful error message for {0}, {1}, {2}{3}"),
                                            pres.Error.Type,
                                            pres.Error.Condition,
                                            person.IdentityName,
                                            idstring);
                                    }
                                    break;
                            }
                            break;
                        default:
                            if (!String.IsNullOrEmpty(pres.Error.ErrorText)) {
                                builder.AppendErrorText(pres.Error.ErrorText);
                            } else {
                                builder.AppendErrorText(
                                    _("There is currently no useful error message for {0}, {1}, {2}{3}"),
                                    pres.Error.Type,
                                    pres.Error.Condition,
                                    person.IdentityName,
                                    idstring);
                            }
                            break;
                    }
                    break;
            }
            // print timestamp of presence
            if (pres.XDelay != null || pres.Last != null) {
                DateTime stamp = DateTime.MinValue;
                TimeSpan span = TimeSpan.MinValue;
                if (pres.XDelay != null) {
                    stamp = pres.XDelay.Stamp;
                    span = DateTime.Now.Subtract(stamp);
                } else if (pres.Last != null) {
                    span = TimeSpan.FromSeconds(pres.Last.Seconds);
                    stamp = DateTime.Now.Subtract(span);
                }
                string spanstr;
                if (span > TimeSpan.FromDays(1)) {
                    spanstr = String.Format(
                        "{0:00}:{1:00}:{2:00}:{3:00}",
                        span.TotalDays, span.Hours, span.Minutes, span.Seconds
                    );
                    spanstr = String.Format(_("{0} days"), spanstr);
                } else if (span > TimeSpan.FromHours(1)) {
                    spanstr = String.Format(
                        "{0:00}:{1:00}:{2:00}",
                        span.Hours, span.Minutes, span.Seconds
                    );
                    spanstr = String.Format(_("{0} hours"), spanstr);
                } else if (span > TimeSpan.FromMinutes(1)) {
                    spanstr = String.Format("{0:00}:{1:00}",
                                            span.Minutes, span.Seconds);
                    spanstr = String.Format(_("{0} minutes"), spanstr);
                } else {
                    spanstr = String.Format("{0:00}", span.Seconds);
                    spanstr = String.Format(_("{0} seconds"), spanstr);
                }

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
            if (pres.Status != null && pres.Status.Trim().Length > 0) {
                builder.AppendText(": {0}", pres.Status);
            }
            return builder.ToMessage();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void PrintGroupChatPresence(XmppGroupChatModel chat, XmppPersonModel person, Presence pres)
        {
            Jid jid = pres.From;
            XmppResourceModel resource;
            if (person.MucResources.TryGetValue(jid.Resource??"", out resource)) {
                if (resource.Presence.Show == pres.Show
                    && resource.Presence.Status == pres.Status
                    && resource.Presence.Last == pres.Last
                    && resource.Presence.XDelay == pres.XDelay
                    && resource.Presence.Priority == pres.Priority
                    && resource.Presence.Nickname == pres.Nickname
                    && resource.Presence.Type == pres.Type
                    ) {
                    // presence didn't change enough to warrent a display message -> abort
                    return;
                }
            }

            var msg = CreatePresenceUpdateMessage(person.Jid, person, pres);
            Session.AddMessageToChat(chat, msg);
            // clone directly to muc person chat
            // don't care about real jid, that has its own presence packets
            var personChat = Session.GetChat(jid, ChatType.Person, this);
            if (personChat != null) {
                Session.AddMessageToChat(personChat, msg);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnGroupChatPresence(XmppGroupChatModel chat, Presence pres)
        {
            Jid jid = pres.From;
            XmppPersonModel person;
            // check whether we know the real jid of this muc user
            if (pres.MucUser != null &&
                pres.MucUser.Item != null &&
                pres.MucUser.Item.Jid != null ) {
                string nick = pres.From.Resource;
                if (!string.IsNullOrEmpty(pres.MucUser.Item.Nickname)) {
                    nick = pres.MucUser.Item.Nickname;
                }
                person = GetOrCreateContact(pres.MucUser.Item.Jid.Bare, nick);
            } else {
                // we do not know the real jid of this user, don't add it to our local roster
                // BUG? pres.From.Resource can be null?
                person = new XmppPersonModel(jid, pres.From.Resource, this);
            }
            person.GetOrCreateMucResource(jid).Presence = pres;
            PrintGroupChatPresence(chat, person, pres);
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
                        // HACK: lower probability of sync race condition swallowing messages
                        ThreadPool.QueueUserWorkItem(delegate {
                            Thread.Sleep(1000);
                            lock (this) {
                                if (IsDisposed) {
                                    return;
                                }
                                chat.IsSynced = true;
                                Session.SyncChat(chat);
                                Session.EnableChat(chat);
                            }
                        });
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        void PrintPrivateChatPresence(XmppPersonModel person, Presence pres)
        {
            Jid jid = pres.From;
            XmppResourceModel resource;
            if (person.Resources.TryGetValue(jid.Resource??"", out resource)) {
                if (resource.Presence.Show == pres.Show
                    && resource.Presence.Status == pres.Status
                    && resource.Presence.Last == pres.Last
                    && resource.Presence.XDelay == pres.XDelay
                    && resource.Presence.Priority == pres.Priority
                    && resource.Presence.Type == pres.Type
                    ) {
                    // presence didn't change enough to warrent a display message -> abort
                    return;
                }
            }
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnPrivateChatPresence(Presence pres)
        {
            Jid jid = pres.From;
            var person = GetOrCreateContact(jid.Bare, jid);
            PrintPrivateChatPresence(person, pres);
            switch (pres.Type) {
                case PresenceType.available:
                    if (pres.Priority < 0) break;
                    if (ContactChat == null) break;
                    if (ContactChat.UnsafePersons.ContainsKey(jid.Bare)) break;
                    Session.AddPersonToGroupChat(ContactChat, person.ToPersonModel());
                    break;
                case PresenceType.unavailable:
                    person.RemoveResource(jid);
                    if (pres.Priority < 0) break;
                    if (ContactChat == null) break;
                    if (!ContactChat.UnsafePersons.ContainsKey(jid.Bare)) break;
                    var pers = ContactChat.GetPerson(jid.Bare);
                    Session.RemovePersonFromGroupChat(ContactChat, pers);
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnPresence(object sender, Presence pres)
        {
            Trace.Call(sender, pres);

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

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnGroupChatMessage(Message msg)
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
            
            // XXX maybe only a Google Talk bug requires this:
            if (msg.XDelay != null) {
                var stamp = msg.XDelay.Stamp;
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

            // mark highlights only for received messages
            bool hilight = person.ID != groupChat.OwnNickname;
            var message = CreateMessage(person, msg, hilight, false);
            Session.AddMessageToChat(groupChat, message);
            OnMessageReceived(
                new MessageEventArgs(groupChat, message, msg.From, groupChat.ID)
            );
        }

        void AddMessageToChatIfNotFiltered(MessageModel msg, ChatModel chat, bool isNew)
        {
            if (Session.IsFilteredMessage(chat, msg)) {
                Session.LogMessage(chat, msg, true);
                return;
            }
            if (isNew) {
                Session.AddChat(chat);
            }
            Session.AddMessageToChat(chat, msg, true);
            if (isNew) {
                Session.SyncChat(chat);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnPrivateChatMessage(Message msg)
        {
            var chat = Session.GetChat(msg.From, ChatType.Person, this) as PersonChatModel;
            bool isNew = false;
            if (chat == null) {
                // in case full jid doesn't have a chat window, use bare jid
                chat = GetOrCreatePersonChat(msg.From.Bare, out isNew);
            }
            var message = CreateMessage(chat.Person, msg, true, true);
            AddMessageToChatIfNotFiltered(message, chat, isNew);
            OnMessageReceived(
                new MessageEventArgs(chat, message, msg.From, null)
            );
        }

        MessageModel CreateMessage(PersonModel person, Message msg, bool mark_hilights, bool force_hilight)
        {
            var builder = CreateMessageBuilder();
            string msgstring;
            if (msg.Html != null) {
                msgstring = msg.Html.ToString();
            } else {
                msgstring = msg.Body.Trim();
            }

            if (msgstring.StartsWith("/me ")) {
                // leave the " " intact
                msgstring = msgstring.Substring(3);
                builder.AppendActionPrefix();
                builder.AppendIdendityName(person, force_hilight);
            } else {
                builder.AppendSenderPrefix(person, force_hilight);
            }

            if (msg.Html != null) {
                builder.AppendHtmlMessage(msgstring);
            } else {
                builder.AppendMessage(msgstring);
            }
            if (mark_hilights) {
                builder.MarkHighlights();
            }

            if (msg.XDelay != null) {
                builder.TimeStamp = msg.XDelay.Stamp;
            }
            return builder.ToMessage();
        }

        void OnGroupChatMessageError(Message msg, XmppGroupChatModel chat)
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

        void OnPrivateChatMessageError(Message msg, PersonChatModel chat)
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnMessage(object sender, Message msg)
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnMucMessage (Message msg)
        {
            User user = msg.MucUser;
            string text;
            if (user.Invite != null) {
                if (user.Invite.Reason != null && user.Invite.Reason.Trim().Length > 0) {
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnChatState(Message msg)
        {
            if (!ShowChatStates) {
                return;
            }
            if (msg.Body != null) {
                return;
            }
            switch (msg.Type) {
                case XmppMessageType.chat:
                case XmppMessageType.headline:
                case XmppMessageType.normal:
                {
                    var chat = GetChat(msg.From, ChatType.Person) as PersonChatModel;
                    bool isNew = false;
                    // no full jid chat
                    if (chat == null) {
                        // create chat
                        chat = GetOrCreatePersonChat(msg.From.Bare, out isNew);
                        if (isNew) {
                            if (!OpenNewChatOnChatState) {
                                return;
                            }
                            if (msg.Chatstate != Chatstate.composing) {
                                // there is NO reason to open a new chat window for
                                // a chatstate other than composing
                                return;
                            }
                            Session.AddChat(chat);
                        }
                    }
                    var builder = CreateMessageBuilder();
                    switch (msg.Chatstate) {
                        case Chatstate.composing:
                            builder.AppendChatState(chat.Person, MessageType.ChatStateComposing);
                            break;
                        case Chatstate.paused:
                            builder.AppendChatState(chat.Person, MessageType.ChatStatePaused);
                            break;
                        default:
                            builder.AppendChatState(chat.Person, MessageType.ChatStateReset);
                            break;
                    }
                    Session.AddMessageToChat(chat, builder.ToMessage());
                    if (isNew) {
                        Session.SyncChat(chat);
                    }
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

        void OnIq(object sender, IQEventArgs e)
        {
            Trace.Call(sender, e);

            // not as pretty as the previous implementation, but it works
            var elem = e.IQ.SelectSingleElement("own-message");
            if (elem is OwnMessageQuery) {
                OnIQOwnMessage((OwnMessageQuery) elem);
                e.Handled = true;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        PersonChatModel GetOrCreatePersonChat(Jid jid)
        {
            bool isNew;
            var chat = GetOrCreatePersonChat(jid, out isNew);
            if (isNew) {
                Session.AddChat(chat);
                Session.SyncChat(chat);
            }
            return chat;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        PersonChatModel GetOrCreatePersonChat(Jid jid, out bool isNew)
        {
            var chat = (PersonChatModel) Session.GetChat(jid, ChatType.Person, this);
            isNew = false;
            if (chat != null) return chat;
            var person = GetOrCreateContact(jid.Bare, jid);
            PersonModel pers;
            if (!String.IsNullOrEmpty(jid.Resource)) {
                pers = new PersonModel(jid, person.IdentityName, NetworkID, Protocol, this);
            } else {
                pers = person.ToPersonModel();
            }
            isNew = true;
            chat = Session.CreatePersonChat(pers, this);
            if (jid == JabberClient.MyJID || jid == JabberClient.MyJID.Bare) {
                var builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText("Note: you are now talking to yourself");
                Session.AddMessageToChat(chat, builder.ToMessage());
            }
            return chat;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnClose(object sender)
        {
            Trace.Call(sender);

            foreach (var chat in Chats) {
                // don't disable the protocol chat, else the user loses all
                // control for the protocol manager! e.g. after a manual
                // reconnect or server-side disconnect
                if (chat.ChatType == ChatType.Protocol) {
                    continue;
                }

                Session.DisableChat(chat);
            }

            OnDisconnected(EventArgs.Empty);

            // reset socket
            JabberClient.ClientSocket.OnValidateCertificate -= ValidateCertificate;
            JabberClient.SocketConnectionType = SocketConnectionType.Direct;

            if (AutoReconnect) {
                Reconnect(AutoReconnectDelay);
            }
        }

        void Reconnect()
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText(_("Reconnecting to {0}"),
                               JabberClient.Server);
            Session.AddMessageToChat(Chat, builder.ToMessage());
            Connect();
        }

        void Reconnect(TimeSpan span)
        {
            int delay = (int)span.TotalMilliseconds;
            if (delay <= 0) {
                Reconnect();
            }
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText(_("Reconnecting to {0} in {1} seconds"),
                               JabberClient.Server, span.TotalSeconds);
            Session.AddMessageToChat(Chat, builder.ToMessage());
            ThreadPool.QueueUserWorkItem(delegate {
                Thread.Sleep(delay);
                lock (this) {
                    // prevent this timer from calling connect after it has been closed
                    if (IsDisposed) {
                        return;
                    }
                    // prevent this timer from calling connect if during the timout
                    // some other event already began a connect
                    if (JabberClient.XmppConnectionState != XmppConnectionState.Disconnected) {
                        return;
                    }
                    Connect();
                }
            });
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnLogin(object sender)
        {
            Trace.Call(sender);

            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText(_("Authenticated"));
            Session.AddMessageToChat(Chat, builder.ToMessage());
            RequestCapabilities(JabberClient.Server, JabberClient.Server);

            OnConnected(EventArgs.Empty);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void ApplyConfig(UserConfig config, XmppServerModel server)
        {
            Nicknames = (string[]) config["Connection/Nicknames"];

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

            var proxySettings = new ProxySettings();
            proxySettings.ApplyConfig(config);
            var protocol = Server.UseEncryption ? "xmpps" : "xmpp";
            var serverUri = String.Format("{0}://{1}:{2}", protocol,
                                          Server.Hostname, Server.Port);
            var proxy = proxySettings.GetWebProxy(serverUri);
            var socket = JabberClient.ClientSocket as ClientSocket;
            if (proxy == null) {
                socket.Proxy = null;
            } else {
                var builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(_("Using proxy: {0}:{1}"),
                                   proxy.Address.Host,
                                   proxy.Address.Port);
                Session.AddMessageToChat(Chat, builder.ToMessage());

                var proxyScheme = proxy.Address.Scheme;
                var proxyType = Starksoft.Net.Proxy.ProxyType.None;
                try {
                    proxyType = (Starksoft.Net.Proxy.ProxyType) Enum.Parse(
                        typeof(Starksoft.Net.Proxy.ProxyType),
                        proxy.Address.Scheme,
                        true
                    );
                } catch (ArgumentException ex) {
#if LOG4NET
                    _Logger.Error("ApplyConfig(): Couldn't parse proxy type: " +
                                  proxyScheme, ex);
#endif
                }

                var proxyFactory = new ProxyClientFactory();
                if (String.IsNullOrEmpty(proxySettings.ProxyUsername) &&
                    String.IsNullOrEmpty(proxySettings.ProxyPassword)) {
                    socket.Proxy = proxyFactory.CreateProxyClient(
                        proxyType,
                        proxy.Address.Host,
                        proxy.Address.Port
                    );
                } else {
                    socket.Proxy = proxyFactory.CreateProxyClient(
                        proxyType,
                        proxy.Address.Host,
                        proxy.Address.Port,
                        proxySettings.ProxyUsername,
                        proxySettings.ProxyPassword
                    );
                }
            }

            Me = new PersonModel(
                String.Format("{0}@{1}",
                    JabberClient.Username,
                    JabberClient.Server
                ),
                JabberClient.Username,
                NetworkID, Protocol, this
            );
            Me.IdentityNameColored.ForegroundColor = new TextColor(0, 0, 255);
            Me.IdentityNameColored.BackgroundColor = TextColor.None;
            Me.IdentityNameColored.Bold = true;

            // XMPP specific settings
            JabberClient.Resource = server.Resource;

            if (server.UseEncryption) {
                // HACK: Google Talk doesn't support StartTLS :(
                if (server.Hostname == "talk.google.com" &&
                    server.Port == 5223) {
                    JabberClient.ForceStartTls = false;
                    JabberClient.UseSSL = true;
                } else {
                    JabberClient.ForceStartTls = true;
                }
            } else {
                JabberClient.ForceStartTls = false;
                JabberClient.UseStartTLS = true;
            }
            if (!server.ValidateServerCertificate) {
                JabberClient.ClientSocket.OnValidateCertificate += ValidateCertificate;
            }
        }

        static bool ValidateCertificate(object sender,
                                         X509Certificate certificate,
                                         X509Chain chain,
                                         SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, LibraryTextDomain);
        }
    }
}
