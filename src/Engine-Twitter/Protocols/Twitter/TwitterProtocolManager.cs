// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2009-2011 Mirco Bauer <meebey@meebey.net>
// 
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

using System;
using System.Net;
using System.Net.Security;
using System.Web;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections.Generic;
using Twitterizer;
using Twitterizer.Core;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public enum TwitterChatType {
        FriendsTimeline,
        Replies,
        DirectMessages
    }

    [ProtocolManagerInfo(Name = "Twitter", Description = "Twitter Micro-Blogging", Alias = "twitter")]
    public class TwitterProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        static readonly string f_LibraryTextDomain = "smuxi-engine-twitter";
        static readonly TextColor f_BlueTextColor = new TextColor(0x0000FF);
        
        OAuthTokens             f_OAuthTokens;
        string                  f_RequestToken;
        OptionalProperties      f_OptionalProperties;
        TwitterUser             f_TwitterUser;
        WebProxy                f_WebProxy;
        string                  f_Username;
        ProtocolChatModel       f_ProtocolChat;
        Dictionary<string, PersonModel> f_Friends;
        List<GroupChatModel>    f_GroupChats = new List<GroupChatModel>();

        GroupChatModel          f_FriendsTimelineChat;
        AutoResetEvent          f_FriendsTimelineEvent = new AutoResetEvent(false);
        Thread                  f_UpdateFriendsTimelineThread;
        int                     f_UpdateFriendsTimelineInterval = 120;
        decimal                 f_LastFriendsTimelineStatusID;
        DateTime                f_LastFriendsUpdate;

        GroupChatModel          f_RepliesChat;
        Thread                  f_UpdateRepliesThread;
        int                     f_UpdateRepliesInterval = 120;
        decimal                 f_LastReplyStatusID;

        GroupChatModel          f_DirectMessagesChat;
        AutoResetEvent          f_DirectMessageEvent = new AutoResetEvent(false);
        Thread                  f_UpdateDirectMessagesThread;
        int                     f_UpdateDirectMessagesInterval = 120;
        decimal                 f_LastDirectMessageReceivedStatusID;
        decimal                 f_LastDirectMessageSentStatusID;

        bool                    f_Listening;
        bool                    f_IsConnected;

        int                     ErrorResponseCount { get; set; }
        const int               MaxErrorResponseCount = 3;

        public override string NetworkID {
            get {
                if (f_TwitterUser == null) {
                    return "Twitter";
                }

                return String.Format("Twitter/{0}", f_TwitterUser.ScreenName);
            }
        }

        public override string Protocol {
            get {
                return "Twitter";
            }
        }

        public override ChatModel Chat {
            get {
                return f_ProtocolChat;
            }
        }
        
        protected bool HasTokens {
            get {
                return f_OAuthTokens != null &&
                       f_OAuthTokens.HasConsumerToken &&
                       f_OAuthTokens.HasAccessToken;
            }
        }

        public TwitterProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);

            f_FriendsTimelineChat = new GroupChatModel(
                TwitterChatType.FriendsTimeline.ToString(),
                _("Home Timeline"),
                this
            );
            f_FriendsTimelineChat.InitMessageBuffer(
                MessageBufferPersistencyType.Volatile
            );
            f_FriendsTimelineChat.ApplyConfig(Session.UserConfig);
            f_GroupChats.Add(f_FriendsTimelineChat);

            f_RepliesChat = new GroupChatModel(
                TwitterChatType.Replies.ToString(),
                _("Replies"),
                this
            );
            f_RepliesChat.InitMessageBuffer(
                MessageBufferPersistencyType.Volatile
            );
            f_RepliesChat.ApplyConfig(Session.UserConfig);
            f_GroupChats.Add(f_RepliesChat);

            f_DirectMessagesChat = new GroupChatModel(
                TwitterChatType.DirectMessages.ToString(),
                _("Direct Messages"),
                this
            );
            f_DirectMessagesChat.InitMessageBuffer(
                MessageBufferPersistencyType.Volatile
            );
            f_DirectMessagesChat.ApplyConfig(Session.UserConfig);
            f_GroupChats.Add(f_DirectMessagesChat);
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

            f_Username = server.Username;

            var proxySettings = new ProxySettings();
            proxySettings.ApplyConfig(Session.UserConfig);
            var twitterUrl = new OptionalProperties().APIBaseAddress;
            var proxy = proxySettings.GetWebProxy(twitterUrl);
            // HACK: Twitterizer will always use the system proxy if set to null
            // so explicitely override this by setting an empty proxy
            if (proxy == null) {
                f_WebProxy = new WebProxy();
            } else {
                f_WebProxy = proxy;
            }

            f_OptionalProperties = CreateOptions<OptionalProperties>();
            f_ProtocolChat = new ProtocolChatModel(NetworkID, "Twitter " + f_Username, this);
            f_ProtocolChat.InitMessageBuffer(
                MessageBufferPersistencyType.Volatile
            );
            f_ProtocolChat.ApplyConfig(Session.UserConfig);
            Session.AddChat(f_ProtocolChat);
            Session.SyncChat(f_ProtocolChat);

            MessageBuilder builder;
            if (proxy != null && proxy.Address != null) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(_("Using proxy: {0}:{1}"),
                                   proxy.Address.Host,
                                   proxy.Address.Port);
                Session.AddMessageToChat(Chat, builder.ToMessage());
            }

            if (!server.ValidateServerCertificate) {
                var whitelist = Session.CertificateValidator.HostnameWhitelist;
                lock (whitelist) {
                    if (!whitelist.Contains("api.twitter.com")) {
                        whitelist.Add("api.twitter.com");
                    }
                }
            }

            string msg;
            msg = String.Format(_("Connecting to Twitter..."));
            fm.SetStatus(msg);
            Session.AddTextToChat(f_ProtocolChat, "-!- " + msg);
            try {
                var key = GetApiKey();
                f_OAuthTokens = new OAuthTokens();
                f_OAuthTokens.ConsumerKey = key[0];
                f_OAuthTokens.ConsumerSecret = key[1];

                var password = server.Password ?? String.Empty;
                var access = password.Split('|');
                if (access.Length == 2) {
                    f_OAuthTokens.AccessToken = access[0];
                    f_OAuthTokens.AccessTokenSecret = access[1];

                    // verify access token
                    var options = CreateOptions<VerifyCredentialsOptions>();
                    var response = TwitterAccount.VerifyCredentials(
                        f_OAuthTokens, options
                    );
                    if (response.Result == RequestResult.Unauthorized) {
#if LOG4NET
                        f_Logger.Warn("Connect(): Invalid access token, " +
                                      "re-authorization required");
#endif
                        f_OAuthTokens.AccessToken = null;
                        f_OAuthTokens.AccessTokenSecret = null;
                    }
                }

                if (!f_OAuthTokens.HasAccessToken) {
                    // new account or basic auth user that needs to be migrated
                    var reqToken = OAuthUtility.GetRequestToken(key[0], key[1],
                                                            "oob", f_WebProxy);
                    f_RequestToken = reqToken.Token;
                    var authUri = OAuthUtility.BuildAuthorizationUri(f_RequestToken);
                    builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    builder.AppendText(_("Twitter authorization required."));
                    Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());

                    builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    // TRANSLATOR: do NOT change the position of {0}!
                    builder.AppendText(
                        _("Please open the following URL and click " +
                          "\"Allow\" to allow Smuxi to connect to your " +
                          "Twitter account: {0}"),
                        String.Empty
                    );
                    Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());

                    builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    builder.AppendText(" ");
                    builder.AppendUrl(authUri.AbsoluteUri);
                    Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());

                    builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    builder.AppendText(
                        _("Once you have allowed Smuxi to access your " +
                          "Twitter account, Twitter will provide a PIN.")
                    );
                    Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());

                    builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    builder.AppendText(_("Please type: /pin PIN_FROM_TWITTER"));
                    Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());
                }
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("Connect(): Exception", ex);
#endif
                fm.SetStatus(_("Connection failed!"));
                Session.AddTextToChat(f_ProtocolChat,
                    "-!- " + _("Connection failed! Reason: ") + ex.Message
                );
                return;
            }

            // twitter is sometimes pretty slow, so fetch this in the background
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    // FIXME: replace with AutoResetEvent
                    while (!HasTokens) {
                        Thread.Sleep(1000);
                    }
                    
                    var message = _("Fetching user details from Twitter, please wait...");
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + message);

                    UpdateUser();

                    message = _("Finished fetching user details.");
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + message);

                    f_IsConnected = true;
                    fm.UpdateNetworkStatus();
                    msg =_("Successfully connected to Twitter.");
                    fm.SetStatus(msg);
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + msg);
                    f_Listening = true;

                    f_FriendsTimelineChat.PersonCount = 
                    f_RepliesChat.PersonCount = 
                    f_DirectMessagesChat.PersonCount = (int) f_TwitterUser.NumberOfFriends;
                } catch (Exception ex) {
                    var message = _("Failed to fetch user details from Twitter. Reason: ");
#if LOG4NET
                    f_Logger.Error("Connect(): " + message, ex);
#endif
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + message + ex.Message);

                    fm.SetStatus(_("Connection failed!"));
                    Session.AddTextToChat(f_ProtocolChat,
                        "-!- " + _("Connection failed! Reason: ") + ex.Message
                    );
                }
            });
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    // FIXME: replace with AutoResetEvent
                    // f_TwitterUser needed for proper self detection in the
                    // CreatePerson() method
                    while (!HasTokens || f_TwitterUser == null) {
                        Thread.Sleep(1000);
                    }

                    var message = _("Fetching friends from Twitter, please wait...");
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + message);

                    UpdateFriends();

                    message = _("Finished fetching friends.");
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + message);
                } catch (Exception ex) {
                    var message = _("Failed to fetch friends from Twitter. Reason: ");
#if LOG4NET
                    f_Logger.Error("Connect(): " + message, ex);
#endif
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + message + ex.Message);
                }
            });

            OpenFriendsTimelineChat();
            OpenRepliesChat();
            OpenDirectMessagesChat();
        }

        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }

        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            f_Listening = false;
            f_FriendsTimelineEvent.Set();
        }

        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);

            return f_GroupChats;
        }

        public override void OpenChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);

            if (chat.ChatType == ChatType.Group) {
               TwitterChatType twitterChatType = (TwitterChatType)
                    Enum.Parse(typeof(TwitterChatType), chat.ID);
               switch (twitterChatType) {
                    case TwitterChatType.FriendsTimeline:
                        OpenFriendsTimelineChat();
                        break;
                    case TwitterChatType.Replies:
                        OpenRepliesChat();
                        break;
                    case TwitterChatType.DirectMessages:
                        OpenDirectMessagesChat();
                        break;
                }
                return;
            }

            OpenPrivateChat(chat.ID);
        }

        private void OpenFriendsTimelineChat()
        {
            ChatModel chat =  Session.GetChat(
                TwitterChatType.FriendsTimeline.ToString(),
                ChatType.Group,
                this
            );

            if (chat != null) {
                return;
            }

            if (f_UpdateFriendsTimelineThread != null &&
                f_UpdateFriendsTimelineThread.IsAlive) {
                return;
            }

            // BUG: causes a race condition as the frontend syncs the
            // unpopulated chat! So only add it if it's ready
            //Session.AddChat(f_FriendsTimelineChat);
            f_UpdateFriendsTimelineThread = new Thread(
                new ThreadStart(UpdateFriendsTimelineThread)
            );
            f_UpdateFriendsTimelineThread.IsBackground = true;
            f_UpdateFriendsTimelineThread.Name =
                "TwitterProtocolManager friends timeline listener";
            f_UpdateFriendsTimelineThread.Start();
        }

        private void OpenRepliesChat()
        {
            ChatModel chat =  Session.GetChat(
                TwitterChatType.Replies.ToString(),
                ChatType.Group,
                this
            );

            if (chat != null) {
                return;
            }

            if (f_UpdateRepliesThread != null &&
                f_UpdateRepliesThread.IsAlive) {
                return;
            }

            // BUG: causes a race condition as the frontend syncs the
            // unpopulated chat! So only add it if it's ready
            //Session.AddChat(f_RepliesChat);
            f_UpdateRepliesThread = new Thread(
                new ThreadStart(UpdateRepliesThread)
            );
            f_UpdateRepliesThread.IsBackground = true;
            f_UpdateRepliesThread.Name =
                "TwitterProtocolManager replies listener";
            f_UpdateRepliesThread.Start();
        }

        private void OpenDirectMessagesChat()
        {
            ChatModel chat =  Session.GetChat(
                TwitterChatType.DirectMessages.ToString(),
                ChatType.Group,
                this
            );

            if (chat != null) {
                return;
            }

            if (f_UpdateDirectMessagesThread != null &&
                f_UpdateDirectMessagesThread.IsAlive) {
                return;
            }

            // BUG: causes a race condition as the frontend syncs the
            // unpopulated chat! So only add it if it's ready
            //Session.AddChat(f_DirectMessagesChat);
            f_UpdateDirectMessagesThread = new Thread(
                new ThreadStart(UpdateDirectMessagesThread)
            );
            f_UpdateDirectMessagesThread.IsBackground = true;
            f_UpdateDirectMessagesThread.Name =
                "TwitterProtocolManager direct messages listener";
            f_UpdateDirectMessagesThread.Start();
        }

        private ChatModel OpenPrivateChat(string userId)
        {
            return OpenPrivateChat(Decimal.Parse(userId));
        }

        private ChatModel OpenPrivateChat(decimal userId)
        {
            ChatModel chat =  Session.GetChat(
                userId.ToString(),
                ChatType.Person,
                this
            );

            if (chat != null) {
                return chat;
            }

            var response = TwitterUser.Show(f_OAuthTokens, userId,
                                            f_OptionalProperties);
            CheckResponse(response);
            var user = response.ResponseObject;
            PersonModel person = CreatePerson(user);
            PersonChatModel personChat = new PersonChatModel(
                person,
                user.Id.ToString(),
                user.ScreenName,
                this
            );
            personChat.InitMessageBuffer(
                MessageBufferPersistencyType.Volatile
            );
            personChat.ApplyConfig(Session.UserConfig);
            Session.AddChat(personChat);
            Session.SyncChat(personChat);
            return personChat;
        }

        public override void CloseChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);

            if (chat.ChatType == ChatType.Group) {
               TwitterChatType twitterChatType = (TwitterChatType)
                    Enum.Parse(typeof(TwitterChatType), chat.ID);
               switch (twitterChatType) {
                    case TwitterChatType.FriendsTimeline:
                        if (f_UpdateFriendsTimelineThread != null &&
                            f_UpdateFriendsTimelineThread.IsAlive) {
                            f_UpdateFriendsTimelineThread.Abort();
                        }
                        break;
                    case TwitterChatType.Replies:
                        if (f_UpdateRepliesThread != null &&
                            f_UpdateRepliesThread.IsAlive) {
                            f_UpdateRepliesThread.Abort();
                        }
                        break;
                    case TwitterChatType.DirectMessages:
                        if (f_UpdateDirectMessagesThread != null &&
                            f_UpdateDirectMessagesThread.IsAlive) {
                            f_UpdateDirectMessagesThread.Abort();
                        }
                        break;
                }
            }

            Session.RemoveChat(chat);
        }

        public override void SetPresenceStatus(PresenceStatus status,
                                               string message)
        {
            Trace.Call(status, message);

            // TODO: implement me

            // should we send updates here?!?
        }

        public override bool Command(CommandModel command)
        {
            bool handled = false;
            if (command.IsCommand) {
                if (f_IsConnected) {
                    switch (command.Command) {
                        case "msg":
                        case "query":
                            CommandMessage(command);
                            handled = true;
                            break;
                    }
                }
                switch (command.Command) {
                    case "help":
                        CommandHelp(command);
                        handled = true;
                        break;
                    case "connect":
                        CommandConnect(command);
                        handled = true;
                        break;
                    case "pin":
                        CommandPin(command);
                        handled = true;
                        break;
                }
            } else {
                if (f_IsConnected) {
                    CommandSay(command);
                    handled = true;
                } else {
                    NotConnected(command);
                    handled = true;
                }
            }

            return handled;
        }

        public override string ToString()
        {
            if (f_TwitterUser == null) {
                return NetworkID;
            }

            return String.Format("{0} (Twitter)", f_TwitterUser.ScreenName);
        }

        public void CommandHelp(CommandModel cd)
        {
            var builder = CreateMessageBuilder();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            builder.AppendHeader(_("Twitter Commands"));
            cd.FrontendManager.AddMessageToChat(cd.Chat, builder.ToMessage());

            string[] help = {
                "connect twitter username",
                "pin pin-number",
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
            var server = new ServerModel();
            if (cd.DataArray.Length >= 3) {
                server.Username = cd.DataArray[2];
            } else {
                NotEnoughParameters(cd);
                return;
            }

            Connect(cd.FrontendManager, server);
        }

        public void CommandPin(CommandModel cd)
        {
            if (String.IsNullOrEmpty(cd.Parameter)) {
                NotEnoughParameters(cd);
                return;
            }
            var pin = cd.Parameter.Trim();

            MessageBuilder builder;
            if (String.IsNullOrEmpty(f_RequestToken)) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(_("No pending authorization request!"));
                Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());
                return;
            }
            var reqToken = f_RequestToken;
            f_RequestToken = null;

            var key = GetApiKey();
            OAuthTokenResponse response;
            try {
                response = OAuthUtility.GetAccessToken(key[0], key[1],
                                                       reqToken, pin,
                                                       f_WebProxy);
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("CommandPin(): GetAccessToken() threw Exception!", ex);
#endif
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                // TRANSLATOR: {0} contains the reason of the failure
                builder.AppendText(
                    _("Failed to authorize with Twitter: {0}"),
                    ex.Message
                );
                Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());

                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(
                    _("Twitter did not accept your PIN.  "  +
                      "Did you enter it correctly?")
                );
                Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());

                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(
                    _("Please retry by closing this tab and reconnecting to " +
                      "the Twitter \"{0}\" account."),
                    f_Username
                );
                Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());

                // allow the user to re-enter the pin
                // LAME: An incorrect PIN invalidates the request token!
                //f_RequestToken = reqToken;
                return;
            }
#if LOG4NET
            f_Logger.Debug("CommandPin(): retrieved " +
                           " AccessToken: " + response.Token + 
                           " AccessTokenSecret: " + response.TokenSecret +
                           " ScreenName: " + response.ScreenName +
                           " UserId: " + response.UserId);
#endif
            var servers = new ServerListController(Session.UserConfig);
            var server = servers.GetServer(Protocol, response.ScreenName);
            if (server == null) {
                server = new ServerModel() {
                    Protocol = Protocol,
                    Network  = String.Empty,
                    Hostname = response.ScreenName,
                    Username = response.ScreenName,
                    Password = String.Format("{0}|{1}", response.Token,
                                             response.TokenSecret),
                    OnStartupConnect = true
                };
                servers.AddServer(server);
                
                var obsoleteServer = servers.GetServer(Protocol, String.Empty);
                if (obsoleteServer != null &&
                    obsoleteServer.Username.ToLower() == response.ScreenName.ToLower()) {
                    // found an old server entry for this user using basic auth
                    servers.RemoveServer(Protocol, String.Empty);

                    builder = CreateMessageBuilder();
                    builder.AppendEventPrefix();
                    builder.AppendText(
                        _("Migrated Twitter account from basic auth to OAuth.")
                    );
                    Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());
                }
            } else {
                // update token
                server.Password = String.Format("{0}|{1}", response.Token,
                                                response.TokenSecret);
                servers.SetServer(server);
            }
            servers.Save();

            builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendText(_("Successfully authorized Twitter account " +
                                 "\"{0}\" for Smuxi"), response.ScreenName);
            Session.AddMessageToChat(f_ProtocolChat, builder.ToMessage());

            f_OAuthTokens.AccessToken = response.Token;
            f_OAuthTokens.AccessTokenSecret = response.TokenSecret;
            f_Username = response.ScreenName;
        }

        public void CommandSay(CommandModel cmd)
        {
            FrontendManager fm = cmd.FrontendManager;
            if (cmd.Chat.ChatType == ChatType.Group) {
                TwitterChatType twitterChatType = (TwitterChatType)
                    Enum.Parse(typeof(TwitterChatType), cmd.Chat.ID);
                switch (twitterChatType) {
                    case TwitterChatType.FriendsTimeline:
                    case TwitterChatType.Replies:
                        try {
                            PostUpdate(cmd.Data);
                        } catch (Exception ex) {
                            fm.AddTextToChat(cmd.Chat, "-!- " +
                                String.Format(_("Could not update status - Reason: {0}"),
                                              ex.Message)
                            );
                        }
                        break;
                    case TwitterChatType.DirectMessages:
                        fm.AddTextToChat(
                            cmd.Chat,
                            "-!- " +
                            _("Cannot send message - no target specified. "+
                              "Use: /msg $nick message")
                        );
                        break;
                }
            } else if (cmd.Chat.ChatType == ChatType.Person) {
                try {
                    SendMessage(cmd.Chat.Name, cmd.Data);
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error(ex);
#endif
                    fm.AddTextToChat(cmd.Chat, "-!- " +
                        String.Format(_("Could not send message - Reason: {0}"),
                                      ex.Message)
                    );
                }
            } else {
                // ignore protocol chat
            }
        }

        public void CommandMessage(CommandModel cmd)
        {
            FrontendManager fm = cmd.FrontendManager;
            string nickname;
            if (cmd.DataArray.Length >= 2) {
                nickname = cmd.DataArray[1];
            } else {
                NotEnoughParameters(cmd);
                return;
            }

            var response = TwitterUser.Show(f_OAuthTokens, nickname,
                                            f_OptionalProperties);
            if (response.Result != RequestResult.Success) {
                fm.AddTextToChat(cmd.Chat, "-!- " +
                    _("Could not send message - the specified user does not exist.")
                );
                return;
            }
            var user = response.ResponseObject;
            var chat = OpenPrivateChat(user.Id);

            if (cmd.DataArray.Length >= 3) {
                string message = String.Join(" ", cmd.DataArray, 2, cmd.DataArray.Length-2);
                try {
                    SendMessage(user.ScreenName, message);
                } catch (Exception ex) {
                    fm.AddTextToChat(chat, "-!- " +
                        String.Format(_("Could not send message - Reason: {0}"),
                                      ex.Message)
                    );
                }
            }
         }

        private List<TwitterStatus> SortTimeline(TwitterStatusCollection timeline)
        {
            List<TwitterStatus> sortedTimeline =
                new List<TwitterStatus>(
                    timeline.Count
                );
            foreach (TwitterStatus status in timeline) {
                sortedTimeline.Add(status);
            }
            sortedTimeline.Sort(
                (a, b) => (a.CreatedDate.CompareTo(b.CreatedDate))
            );
            return sortedTimeline;
        }

        private List<TwitterDirectMessage> SortTimeline(TwitterDirectMessageCollection timeline)
        {
            var sortedTimeline = new List<TwitterDirectMessage>(timeline.Count);
            foreach (TwitterDirectMessage msg in timeline) {
                sortedTimeline.Add(msg);
            }
            sortedTimeline.Sort(
                (a, b) => (a.CreatedDate.CompareTo(b.CreatedDate))
            );
            return sortedTimeline;
        }

        private void UpdateFriendsTimelineThread()
        {
            Trace.Call();

            try {
                // query the timeline only after we have fetched the user and friends
                while (f_TwitterUser == null /*|| f_TwitterUser.IsEmpty*/ ||
                       f_Friends == null) {
                    Thread.Sleep(1000);
                }

                // populate friend list
                lock (f_Friends) {
                    foreach (PersonModel friend in f_Friends.Values) {
                        f_FriendsTimelineChat.UnsafePersons.Add(friend.ID, friend);
                    }
                }
                Session.AddChat(f_FriendsTimelineChat);
                Session.SyncChat(f_FriendsTimelineChat);

                while (f_Listening) {
                    try {
                        UpdateFriendsTimeline();
                    } catch (TwitterizerException ex) {
                        CheckTwitterizerException(ex);
                    } catch (WebException ex) {
                        CheckWebException(ex);
                    }

                    // only poll once per interval or when we get fired
                    f_FriendsTimelineEvent.WaitOne(
                        f_UpdateFriendsTimelineInterval * 1000, false
                    );
                }
            } catch (ThreadAbortException) {
#if LOG4NET
                f_Logger.Debug("UpdateFriendsTimelineThread(): thread aborted");
#endif
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("UpdateFriendsTimelineThread(): Exception", ex);
#endif
                string msg =_("An error occurred while fetching the friends timeline from Twitter. Reason: ");
                Session.AddTextToChat(f_ProtocolChat, "-!- " + msg + ex.Message);
            } finally {
#if LOG4NET
                f_Logger.Debug("UpdateFriendsTimelineThread(): finishing thread.");
#endif
                lock (Session.Chats) {
                    if (Session.Chats.Contains(f_FriendsTimelineChat)) {
                        Session.RemoveChat(f_FriendsTimelineChat);
                    }
                }
                f_FriendsTimelineChat.UnsafePersons.Clear();
            }
        }

        private void UpdateFriendsTimeline()
        {
            Trace.Call();

#if LOG4NET
            f_Logger.Debug("UpdateFriendsTimeline(): getting friend timeline from twitter...");
#endif
            var options = CreateOptions<TimelineOptions>();
            options.SinceStatusId = f_LastFriendsTimelineStatusID;
            options.Count = 50;
            var response = TwitterTimeline.HomeTimeline(f_OAuthTokens,
                                                        options);
            // ignore temporarily issues
            if (IsTemporilyErrorResponse(response)) {
                return;
            }
            CheckResponse(response);
            var timeline = response.ResponseObject;
#if LOG4NET
            f_Logger.Debug("UpdateFriendsTimeline(): done. New tweets: " +
                           timeline.Count);
#endif
            if (timeline.Count == 0) {
                return;
            }

            List<TwitterStatus> sortedTimeline = SortTimeline(timeline);
            foreach (TwitterStatus status in sortedTimeline) {
                String text;
                // LAME: Twitter lies in the truncated field and says it's not
                // truncated while it is, thus always use retweet_status if
                // available
                if (status.RetweetedStatus != null) {
                    text = String.Format(
                        "RT @{0}: {1}",
                        status.RetweetedStatus.User.ScreenName,
                        status.RetweetedStatus.Text
                    );
                } else {
                    text = status.Text;
                }
                MessageModel msg = CreateMessage(
                    status.CreatedDate,
                    status.User,
                    text
                );
                Session.AddMessageToChat(f_FriendsTimelineChat, msg);

                f_LastFriendsTimelineStatusID = status.Id;
            }
        }

        private void UpdateRepliesThread()
        {
            Trace.Call();

            try {
                // query the replies only after we have fetched the user and friends
                while (f_TwitterUser == null /*|| f_TwitterUser.IsEmpty*/ ||
                       f_Friends == null) {
                    Thread.Sleep(1000);
                }

                // populate friend list
                lock (f_Friends) {
                    foreach (PersonModel friend in f_Friends.Values) {
                        f_RepliesChat.UnsafePersons.Add(friend.ID, friend);
                    }
                }
                Session.AddChat(f_RepliesChat);
                Session.SyncChat(f_RepliesChat);

                while (f_Listening) {
                    try {
                        UpdateReplies();
                    } catch (TwitterizerException ex) {
                        CheckTwitterizerException(ex);
                    } catch (WebException ex) {
                        CheckWebException(ex);
                    }

                    // only poll once per interval
                    Thread.Sleep(f_UpdateRepliesInterval * 1000);
                }
            } catch (ThreadAbortException) {
#if LOG4NET
                f_Logger.Debug("UpdateRepliesThread(): thread aborted");
#endif
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("UpdateRepliesThread(): Exception", ex);
#endif
                string msg =_("An error occurred while fetching the replies from Twitter. Reason: ");
                Session.AddTextToChat(f_ProtocolChat, "-!- " + msg + ex.Message);
            } finally {
#if LOG4NET
                f_Logger.Debug("UpdateRepliesThread(): finishing thread.");
#endif
                lock (Session.Chats) {
                    if (Session.Chats.Contains(f_RepliesChat)) {
                        Session.RemoveChat(f_RepliesChat);
                    }
                }
                f_RepliesChat.UnsafePersons.Clear();
            }
        }

        private void UpdateReplies()
        {
            Trace.Call();

#if LOG4NET
            f_Logger.Debug("UpdateReplies(): getting replies from twitter...");
#endif
            var options = CreateOptions<TimelineOptions>();
            options.SinceStatusId = f_LastReplyStatusID;
            var response = TwitterTimeline.Mentions(f_OAuthTokens, options);
            // ignore temporarily issues
            if (IsTemporilyErrorResponse(response)) {
                return;
            }
            CheckResponse(response);
            var timeline = response.ResponseObject;
#if LOG4NET
            f_Logger.Debug("UpdateReplies(): done. New replies: " + timeline.Count);
#endif
            if (timeline.Count == 0) {
                return;
            }

            // if this isn't the first time we receive replies, this is new!
            bool highlight = f_LastReplyStatusID != 0;
            List<TwitterStatus> sortedTimeline = SortTimeline(timeline);
            foreach (TwitterStatus status in sortedTimeline) {
                MessageModel msg = CreateMessage(
                    status.CreatedDate,
                    status.User,
                    status.Text,
                    highlight
                );
                Session.AddMessageToChat(f_RepliesChat, msg);

                f_LastReplyStatusID = status.Id;
            }
        }

        private void UpdateDirectMessagesThread()
        {
            Trace.Call();

            try {
                // query the messages only after we have fetched the user and friends
                while (f_TwitterUser == null ||
                       f_Friends == null) {
                    Thread.Sleep(1000);
                }

                // populate friend list
                lock (f_Friends) {
                    foreach (PersonModel friend in f_Friends.Values) {
                        f_DirectMessagesChat.UnsafePersons.Add(friend.ID, friend);
                    }
                }
                Session.AddChat(f_DirectMessagesChat);
                Session.SyncChat(f_DirectMessagesChat);

                while (f_Listening) {
                    try {
                        UpdateDirectMessages();
                    } catch (TwitterizerException ex) {
                        CheckTwitterizerException(ex);
                    } catch (WebException ex) {
                        CheckWebException(ex);
                    }

                    // only poll once per interval or when we get fired
                    f_DirectMessageEvent.WaitOne(
                        f_UpdateDirectMessagesInterval * 1000, false
                    );
                }
            } catch (ThreadAbortException) {
#if LOG4NET
                f_Logger.Debug("UpdateDirectMessagesThread(): thread aborted");
#endif
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("UpdateDirectMessagesThread(): Exception", ex);
#endif
                string msg =_("An error occurred while fetching direct messages from Twitter. Reason: ");
                Session.AddTextToChat(f_ProtocolChat, "-!- " + msg + ex.Message);
            } finally {
#if LOG4NET
                f_Logger.Debug("UpdateDirectMessagesThread(): finishing thread.");
#endif
                lock (Session.Chats) {
                    if (Session.Chats.Contains(f_DirectMessagesChat)) {
                        Session.RemoveChat(f_DirectMessagesChat);
                    }
                }
                f_DirectMessagesChat.UnsafePersons.Clear();
            }
        }

        private void UpdateDirectMessages()
        {
            Trace.Call();

#if LOG4NET
            f_Logger.Debug("UpdateDirectMessages(): getting received direct messages from twitter...");
#endif
            var options = CreateOptions<DirectMessagesOptions>();
            options.SinceStatusId = f_LastDirectMessageReceivedStatusID;
            options.Count = 50;
            var response = TwitterDirectMessage.DirectMessages(
                f_OAuthTokens, options
            );
            // ignore temporarily issues
            if (IsTemporilyErrorResponse(response)) {
                return;
            }
            CheckResponse(response);
            var receivedTimeline = response.ResponseObject;
#if LOG4NET
            f_Logger.Debug("UpdateDirectMessages(): done. New messages: " +
                (receivedTimeline == null ? 0 : receivedTimeline.Count));
#endif

#if LOG4NET
            f_Logger.Debug("UpdateDirectMessages(): getting sent direct messages from twitter...");
#endif
            var sentOptions = CreateOptions<DirectMessagesSentOptions>();
            sentOptions.SinceStatusId = f_LastDirectMessageSentStatusID;
            sentOptions.Count = 50;
            response = TwitterDirectMessage.DirectMessagesSent(
                f_OAuthTokens, sentOptions
            );
            // ignore temporarily issues
            if (IsTemporilyErrorResponse(response)) {
                return;
            }
            CheckResponse(response);
            var sentTimeline = response.ResponseObject;
#if LOG4NET
            f_Logger.Debug("UpdateDirectMessages(): done. New messages: " +
                (sentTimeline == null ? 0 : sentTimeline.Count));
#endif

            var timeline = new TwitterDirectMessageCollection();
            if (receivedTimeline != null) {
                foreach (TwitterDirectMessage msg in receivedTimeline) {
                    timeline.Add(msg);
                }
            }
            if (sentTimeline != null) {
                foreach (TwitterDirectMessage msg in sentTimeline) {
                    timeline.Add(msg);
                }
            }

            if (timeline.Count == 0) {
                // nothing to do
                return;
            }

            var sortedTimeline = SortTimeline(timeline);
            foreach (TwitterDirectMessage directMsg in sortedTimeline) {
                // if this isn't the first time a receive a direct message,
                // this is a new one!
                bool highlight = receivedTimeline.Contains(directMsg) &&
                                 f_LastDirectMessageReceivedStatusID != 0;
                MessageModel msg = CreateMessage(
                    directMsg.CreatedDate,
                    directMsg.Sender,
                    directMsg.Text,
                    highlight
                );
                Session.AddMessageToChat(f_DirectMessagesChat, msg);

                // if there is a tab open for this user put the message there too
                string userId;
                if (receivedTimeline.Contains(directMsg)) {
                    // this is a received message
                    userId =  directMsg.SenderId.ToString();
                } else {
                    // this is a sent message
                    userId = directMsg.RecipientId.ToString();
                }
                ChatModel chat =  Session.GetChat(
                    userId,
                    ChatType.Person,
                    this
                );
                if (chat != null) {
                    Session.AddMessageToChat(chat, msg);
                }
            }

            if (receivedTimeline != null) {
                // first one is the newest
                foreach (TwitterDirectMessage msg in receivedTimeline) {
                    f_LastDirectMessageReceivedStatusID = msg.Id;
                    break;
                }
            }
            if (sentTimeline != null) {
                // first one is the newest
                foreach (TwitterDirectMessage msg in sentTimeline) {
                    f_LastDirectMessageSentStatusID = msg.Id;
                    break;
                }
            }
        }

        private void UpdateFriends()
        {
            Trace.Call();

            if (f_Friends != null) {
                return;
            }

#if LOG4NET
            f_Logger.Debug("UpdateFriends(): fetching friend IDs from twitter...");
#endif
            var options = CreateOptions<UsersIdsOptions>();
            options.UserId = f_TwitterUser.Id;
            var response = TwitterFriendship.FriendsIds(
                f_OAuthTokens, options
            );
            CheckResponse(response);
            var friendIds = response.ResponseObject;
#if LOG4NET
            f_Logger.Debug("UpdateFriends(): done. Fetched IDs: " + friendIds.Count);
#endif

            var persons = new Dictionary<string, PersonModel>(friendIds.Count);
            // users/lookup only permits 100 users per call
            var pageSize = 100;
            var idList = new List<decimal>(friendIds);
            var idPages = new List<List<decimal>>();
            for (int offset = 0; offset < idList.Count; offset += pageSize) {
                var count = Math.Min(pageSize, idList.Count - offset);
                idPages.Add(idList.GetRange(offset, count));
            }
            foreach (var idPage in idPages) {
#if LOG4NET
                f_Logger.Debug("UpdateFriends(): fetching friends from twitter...");
#endif
                var userIds = new TwitterIdCollection(idPage);
                var lookupOptions = CreateOptions<LookupUsersOptions>();
                lookupOptions.UserIds = userIds;
                var lookupResponse = TwitterUser.Lookup(f_OAuthTokens, lookupOptions);
                CheckResponse(lookupResponse);
                var friends = lookupResponse.ResponseObject;
#if LOG4NET
                f_Logger.Debug("UpdateFriends(): done. Fetched friends: " + friends.Count);
#endif
                foreach (var friend in friends) {
                    var person = CreatePerson(friend);
                    persons.Add(person.ID, person);
                }
            }
            f_Friends = persons;
        }

        private void UpdateUser()
        {
#if LOG4NET
            f_Logger.Debug("UpdateUser(): getting user details from twitter...");
#endif
            var response = TwitterUser.Show(f_OAuthTokens, f_Username,
                                            f_OptionalProperties);
            CheckResponse(response);
            var user = response.ResponseObject;
            f_TwitterUser = user;
#if LOG4NET
            f_Logger.Debug("UpdateUser(): done.");
#endif
        }

        private MessageModel CreateMessage(DateTime when, TwitterUser from,
                                           string message)
        {
            return CreateMessage(when, from, message, false);
        }

        private MessageModel CreateMessage(DateTime when, TwitterUser from,
                                           string message, bool highlight)
        {
            if (from == null) {
                throw new ArgumentNullException("from");
            }
            if (message == null) {
                throw new ArgumentNullException("message");
            }

            var builder = CreateMessageBuilder();
            // MessageModel serializer expects UTC values
            builder.TimeStamp = when.ToUniversalTime();
            builder.AppendSenderPrefix(GetPerson(from), highlight);
            builder.AppendMessage(message);
            return builder.ToMessage();
        }

        private T CreateOptions<T>() where T : OptionalProperties, new()
        {
            var options = new T() {
                Proxy = f_WebProxy
            };
            return options;
        }

        private void PostUpdate(string text)
        {
            var options = CreateOptions<StatusUpdateOptions>();
            var res = TwitterStatus.Update(f_OAuthTokens, text, options);
            CheckResponse(res);
            f_FriendsTimelineEvent.Set();
        }

        private void SendMessage(string target, string text)
        {
            var res = TwitterDirectMessage.Send(f_OAuthTokens, target, text,
                                                f_OptionalProperties);
            CheckResponse(res);
            f_DirectMessageEvent.Set();
        }
        
        private void CheckTwitterizerException(TwitterizerException exception)
        {
            Trace.Call(exception == null ? null : exception.GetType());

            if (exception.InnerException is WebException) {
                CheckWebException((WebException) exception.InnerException);
                return;
            } else if (exception.InnerException != null) {
#if LOG4NET
                f_Logger.Warn("CheckTwitterizerException(): unknown inner exception: " + exception.InnerException.GetType(), exception.InnerException);
#endif
            }

            throw exception;
        }
        
        private void CheckWebException(WebException exception)
        {
            Trace.Call(exception == null ? null : exception.GetType());

            switch (exception.Status) {
                case WebExceptionStatus.ConnectFailure:
                case WebExceptionStatus.ConnectionClosed:
                case WebExceptionStatus.Timeout:
                case WebExceptionStatus.ReceiveFailure:
                case WebExceptionStatus.NameResolutionFailure:
                case WebExceptionStatus.ProxyNameResolutionFailure:
                    // ignore temporarly issues
#if LOG4NET
                    f_Logger.Warn("CheckWebException(): ignored exception", exception);
#endif
                    return;
            }

            /*
            http://apiwiki.twitter.com/HTTP-Response-Codes-and-Errors
            * 200 OK: Success!
            * 304 Not Modified: There was no new data to return.
            * 400 Bad Request: The request was invalid.  An accompanying error
            *     message will explain why. This is the status code will be
            *     returned during rate limiting.
            * 401 Unauthorized: Authentication credentials were missing or
            *     incorrect.
            * 403 Forbidden: The request is understood, but it has been
            *     refused.  An accompanying error message will explain why.
            *     This code is used when requests are being denied due to
            *     update limits.
            * 404 Not Found: The URI requested is invalid or the resource
            *     requested, such as a user, does not exists.
            * 406 Not Acceptable: Returned by the Search API when an invalid
            *     format is specified in the request.
            * 500 Internal Server Error: Something is broken.  Please post to
            *     the group so the Twitter team can investigate.
            * 502 Bad Gateway: Twitter is down or being upgraded.
            * 503 Service Unavailable: The Twitter servers are up, but
            *     overloaded with requests. Try again later. The search and
            *     trend methods use this to indicate when you are being rate
            *     limited.
            */
            HttpWebResponse httpRes = exception.Response as HttpWebResponse;
            if (httpRes == null) {
                throw exception;
            }
            switch (httpRes.StatusCode) {
                case HttpStatusCode.BadGateway:
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.GatewayTimeout:
                    // ignore temporarly issues
#if LOG4NET
                    f_Logger.Warn("CheckWebException(): ignored exception", exception);
#endif
                    return;
                default:
#if LOG4NET
                    f_Logger.Error("CheckWebException(): " +
                                   "Status: " + exception.Status + " " +
                                   "ResponseUri: " + exception.Response.ResponseUri);
#endif
                    throw exception;
            }
        }

        private void CheckResponse<T>(TwitterResponse<T> response) where T : ITwitterObject
        {
            if (response == null) {
                throw new ArgumentNullException("response");
            }

            if (response.Result == RequestResult.Success) {
                return;
            }

#if LOG4NET
            f_Logger.Error("CheckResponse(): " +
                           "RequestUrl: " + response.RequestUrl + " " +
                           "Result: " + response.Result + " " +
                           "Content:\n" + response.Content);
#endif

            // HACK: Twitter returns HTML code saying they are overloaded o_O
            if (response.Result == RequestResult.Unknown &&
                response.ErrorMessage == null) {
                response.ErrorMessage = _("Twitter didn't send a valid response, they're probably overloaded");
            }
            throw new TwitterizerException(response.ErrorMessage);
        }

        private bool IsTemporilyErrorResponse<T>(TwitterResponse<T> response)
                                        where T : ITwitterObject
        {
            if (response == null) {
                throw new ArgumentNullException("response");
            }

            switch (response.Result) {
                case RequestResult.Success:
                    // no error at all
                    ErrorResponseCount = 0;
                    return false;
                case RequestResult.ConnectionFailure:
                case RequestResult.RateLimited:
                case RequestResult.TwitterIsDown:
                case RequestResult.TwitterIsOverloaded:
                // probably "Twitter is over capacity"
                case RequestResult.Unknown:
#if LOG4NET
                    f_Logger.Debug("IsTemporilyErrorResponse(): " +
                                   "Detected temporily error " +
                                   "RequestUrl: " + response.RequestUrl + " " +
                                   "Result: " + response.Result + " " +
                                   "Content:\n" + response.Content);
#endif
                    return true;
            }

            if (ErrorResponseCount++ < MaxErrorResponseCount) {
#if LOG4NET
                f_Logger.WarnFormat(
                    "IsTemporilyErrorResponse(): Ignoring permanent error " +
                    "({0}/{1}) " +
                    "RequestUrl: {2} " +
                    "Result: {3} " +
                    "Content:\n{4}",
                    ErrorResponseCount,
                    MaxErrorResponseCount,
                    response.RequestUrl,
                    response.Result,
                    response.Content
                );
#endif
                return true;
            }

#if LOG4NET
            f_Logger.ErrorFormat(
                "IsTemporilyErrorResponse(): Detected permanent error " +
                "RequestUrl: {0} Result: {1} " +
                "Content:\n{2}",
                response.RequestUrl,
                response.Result,
                response.Content
            );
#endif
            return false;
        }

        private PersonModel GetPerson(TwitterUser user)
        {
            if (user == null) {
                throw new ArgumentNullException("user");
            }

            PersonModel person;
            if (!f_Friends.TryGetValue(user.Id.ToString(), out person)) {
                return CreatePerson(user);
            }
            return person;
        }

        private PersonModel CreatePerson(decimal userId)
        {
            var res = TwitterUser.Show(f_OAuthTokens, userId, f_OptionalProperties);
            CheckResponse(res);
            var user = res.ResponseObject;
            return CreatePerson(user);
        }

        private PersonModel CreatePerson(TwitterUser user)
        {
            if (user == null) {
                throw new ArgumentNullException("user");
            }

            var person = new PersonModel(
                user.Id.ToString(),
                user.ScreenName,
                NetworkID,
                Protocol,
                this
            );
            if (f_TwitterUser != null &&
                f_TwitterUser.ScreenName == user.ScreenName) {
                person.IdentityNameColored.ForegroundColor = f_BlueTextColor;
                person.IdentityNameColored.BackgroundColor = TextColor.None;
                person.IdentityNameColored.Bold = true;
            }
            return person;
        }

        protected override T CreateMessageBuilder<T>()
        {
            var builder = new TwitterMessageBuilder();
            builder.ApplyConfig(Session.UserConfig);
            return (T)(object) builder;
        }

        private string[] GetApiKey()
        {
            var key = Defines.TwitterApiKey.Split('|');
            if (key.Length != 2) {
                throw new InvalidOperationException("Invalid Twitter API key!");
            }

            return key;
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }
    }
}
