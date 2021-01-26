// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2009-2015 Mirco Bauer <meebey@meebey.net>
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
using System.Linq;
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

        TwitterStatus[]         StatusIndex { get; set; }
        int                     StatusIndexOffset { get; set; }
        Dictionary<string, TwitterSearchStream> SearchStreams { get; set; }

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
                _("Replies & Mentions"),
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

            StatusIndex = new TwitterStatus[99];
            SearchStreams = new Dictionary<string, TwitterSearchStream>();
        }

        public override void Connect(FrontendManager fm, ServerModel server)
        {
            Trace.Call(fm, server);

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
                    // needed for favicon
                    if (!whitelist.Contains("www.twitter.com")) {
                        whitelist.Add("www.twitter.com");
                    }
                    if (!whitelist.Contains("api.twitter.com")) {
                        whitelist.Add("api.twitter.com");
                    }
                    if (!whitelist.Contains("stream.twitter.com")) {
                        whitelist.Add("stream.twitter.com");
                    }
                }
            }

            string msgStr = _("Connecting to Twitter...");
            if (fm != null) {
                fm.SetStatus(msgStr);
            }
            var msg = CreateMessageBuilder().
                AppendEventPrefix().AppendText(msgStr).ToMessage();
            Session.AddMessageToChat(Chat, msg);
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
                if (fm != null) {
                    fm.SetStatus(_("Connection failed!"));
                }
                msg = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendErrorText(
                        _("Connection failed! Reason: {0}"),
                        ex.Message).
                    ToMessage();
                Session.AddMessageToChat(Chat, msg);
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
                    msg = CreateMessageBuilder().
                        AppendEventPrefix().AppendText(message).ToMessage();
                    Session.AddMessageToChat(Chat, msg);

                    UpdateUser();

                    message = _("Finished fetching user details.");
                    msg = CreateMessageBuilder().
                        AppendEventPrefix().AppendText(message).ToMessage();
                    Session.AddMessageToChat(Chat, msg);

                    f_IsConnected = true;
                    message =_("Successfully connected to Twitter.");
                    if (fm != null) {
                        fm.UpdateNetworkStatus();
                        fm.SetStatus(message);
                    }

                    msg = CreateMessageBuilder().
                        AppendEventPrefix().AppendText(message).ToMessage();
                    Session.AddMessageToChat(Chat, msg);
                    f_Listening = true;

                    f_FriendsTimelineChat.PersonCount = 
                    f_RepliesChat.PersonCount = 
                    f_DirectMessagesChat.PersonCount = (int) f_TwitterUser.NumberOfFriends;

                    OnConnected(EventArgs.Empty);

                } catch (Exception ex) {
                    var message = _("Failed to fetch user details from Twitter. Reason: ");
#if LOG4NET
                    f_Logger.Error("Connect(): " + message, ex);
#endif
                    msg = CreateMessageBuilder().
                        AppendEventPrefix().
                        AppendErrorText(message + ex.Message).
                        ToMessage();
                    Session.AddMessageToChat(Chat, msg);

                    if (fm != null) {
                        fm.SetStatus(_("Connection failed!"));
                    }
                    msg = CreateMessageBuilder().
                        AppendEventPrefix().
                        AppendErrorText(_("Connection failed! Reason: {0}"),
                                        ex.Message).
                        ToMessage();
                    Session.AddMessageToChat(Chat, msg);
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

                    msg = CreateMessageBuilder().
                        AppendEventPrefix().
                        AppendText(
                            _("Fetching friends from Twitter, please wait...")
                        ).
                        ToMessage();
                    Session.AddMessageToChat(Chat, msg);

                    UpdateFriends();

                    msg = CreateMessageBuilder().
                        AppendEventPrefix().
                        AppendText(_("Finished fetching friends.")).
                        ToMessage();
                    Session.AddMessageToChat(Chat, msg);
                } catch (Exception ex) {
                    var message = _("Failed to fetch friends from Twitter. Reason: ");
#if LOG4NET
                    f_Logger.Error("Connect(): " + message, ex);
#endif
                    msg = CreateMessageBuilder().
                        AppendEventPrefix().
                        AppendErrorText(message + ex.Message).
                        ToMessage();
                    Session.AddMessageToChat(Chat, msg);
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

            TwitterChatType? chatType = null;
            if (chat.ChatType == ChatType.Group) {
                try {
                    chatType = (TwitterChatType) Enum.Parse(
                        typeof(TwitterChatType),
                        chat.ID
                    );
                } catch (ArgumentException) {
                }
            }
            if (chatType.HasValue) {
               switch (chatType.Value) {
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
            } else {
                // no static/singleton chat, but maybe a search?
                TwitterSearchStream stream;
                lock (SearchStreams) {
                    if (SearchStreams.TryGetValue(chat.ID, out stream)) {
                        SearchStreams.Remove(chat.ID);
                        stream.Dispose();
                    }
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
                        case "timeline":
                            CommandTimeline(command);
                            handled = true;
                            break;
                        case "follow":
                            CommandFollow(command);
                            handled = true;
                            break;
                        case "unfollow":
                            CommandUnfollow(command);
                            handled = true;
                            break;
                        case "search":
                        case "join":
                            CommandSearch(command);
                            handled = true;
                            break;
                        case "rt":
                        case "retweet":
                            CommandRetweet(command);
                            handled = true;
                            break;
                        case "reply":
                            CommandReply(command);
                            handled = true;
                            break;
                        case "say":
                            CommandSay(command);
                            handled = true;
                            break;
                        case "del":
                        case "delete":
                            CommandDelete(command);
                            handled = true;
                            break;
                        case "fav":
                        case "favourite":
                        case "favorite":
                            CommandFavorite(command);
                            handled = true;
                            break;
                        case "unfav":
                        case "unfavourite":
                        case "unfavorite":
                            CommandUnfavorite(command);
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
            Session.AddMessageToFrontend(cd, builder.ToMessage());

            string[] help = {
                "connect twitter username",
                "pin pin-number",
                "follow screen-name|user-id",
                "unfollow screen-name|user-id",
                "search keyword",
                "retweet/rt index-number|tweet-id",
                "reply index-number|tweet-id message",
                "delete/del index-number|tweet-id",
                "favorite/fav index-number|tweet-id",
                "unfavorite/unfav index-number|tweet-id",
            };

            foreach (string line in help) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                Session.AddMessageToFrontend(cd, builder.ToMessage());
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
            if (cmd.Chat.ChatType == ChatType.Group) {
                TwitterChatType twitterChatType = (TwitterChatType)
                    Enum.Parse(typeof(TwitterChatType), cmd.Chat.ID);
                switch (twitterChatType) {
                    case TwitterChatType.FriendsTimeline:
                    case TwitterChatType.Replies: {
                        try {
                            PostUpdate(cmd);
                        } catch (Exception ex) {
                            var msg = CreateMessageBuilder().
                                AppendEventPrefix().
                                AppendErrorText(
                                    _("Could not update status - Reason: {0}"),
                                    ex.Message).
                                ToMessage();
                            Session.AddMessageToFrontend(cmd, msg);
                        }
                        break;
                    }
                    case TwitterChatType.DirectMessages: {
                        var msg = CreateMessageBuilder().
                            AppendEventPrefix().
                            AppendErrorText(
                                _("Cannot send message - no target specified. " +
                                  "Use: /msg $nick message")).
                            ToMessage();
                        Session.AddMessageToFrontend(cmd, msg);
                        break;
                    }
                }
            } else if (cmd.Chat.ChatType == ChatType.Person) {
                try {
                    SendMessage(cmd);
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error(ex);
#endif
                    var msg = CreateMessageBuilder().
                        AppendEventPrefix().
                        AppendErrorText(
                            _("Could not send message - Reason: {0}"),
                            ex.Message).
                        ToMessage();
                    Session.AddMessageToFrontend(cmd, msg);
                }
            } else {
                // ignore protocol chat
            }
        }

        public void CommandTimeline(CommandModel cmd)
        {
            if (cmd.DataArray.Length < 2) {
                NotEnoughParameters(cmd);
                return;
            }

            string keyword = cmd.Parameter;
            string[] users = cmd.Parameter.Split(',');

            string chatName = users.Length > 1 ? _("Other timelines") : "@" + users[0];
            ChatModel chat;

            if (users.Length > 1) {
                chat = Session.CreateChat<GroupChatModel>(keyword, chatName, this);
            } else {
                var userResponse = TwitterUser.Show(f_OAuthTokens, users [0], f_OptionalProperties);
                CheckResponse(userResponse);
                var person = GetPerson(userResponse.ResponseObject);
                chat = Session.CreatePersonChat(person, person.ID + "/timeline",
                                                chatName, this);
            }

            var statuses = new List<TwitterStatus>();
            foreach (var user in users) {
                var opts = CreateOptions<UserTimelineOptions>();
                opts.ScreenName = user;
                var statusCollectionResponse = TwitterTimeline.UserTimeline(f_OAuthTokens, opts);
                CheckResponse(statusCollectionResponse);

                foreach (var status in statusCollectionResponse.ResponseObject) {
                    statuses.Add(status);
                }
            }

            var sortedStatuses = SortTimeline(statuses);
            foreach (var status in sortedStatuses) {
                AddIndexToStatus(status);
                var msg = CreateMessageBuilder().
                    Append(status, GetPerson(status.User)).ToMessage();
                chat.MessageBuffer.Add(msg);
                var userId = status.User.Id.ToString();
                var groupChat = chat as GroupChatModel;
                if (groupChat != null) {
                    if (!groupChat.UnsafePersons.ContainsKey(userId)) {
                        groupChat.UnsafePersons.Add(userId, GetPerson(status.User));
                    }
                }
            }
            Session.AddChat(chat);
            Session.SyncChat(chat);
        }

        public void CommandMessage(CommandModel cmd)
        {
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
                var msg = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendErrorText(_("Could not send message - the " +
                                      "specified user does not exist.")).
                    ToMessage();
                Session.AddMessageToFrontend(cmd, msg);
                return;
            }
            var user = response.ResponseObject;
            var chat = OpenPrivateChat(user.Id);

            if (cmd.DataArray.Length >= 3) {
                string message = String.Join(" ", cmd.DataArray, 2, cmd.DataArray.Length-2);
                try {
                    SendMessage(user.ScreenName, message);
                } catch (Exception ex) {
                    var msg = CreateMessageBuilder().
                        AppendEventPrefix().
                        AppendErrorText(
                            _("Could not send message - Reason: {0}"),
                            ex.Message).
                        ToMessage();
                    Session.AddMessageToFrontend(cmd.FrontendManager, chat, msg);
                }
            }
        }

        public void CommandFollow(CommandModel cmd)
        {
            if (cmd.DataArray.Length < 2) {
                NotEnoughParameters(cmd);
                return;
            }

            var chat = cmd.Chat as GroupChatModel;
            if (chat == null) {
                return;
            }

            var options = CreateOptions<CreateFriendshipOptions>();
            options.Follow = true;
            decimal userId;
            TwitterResponse<TwitterUser> res;
            if (Decimal.TryParse(cmd.Parameter, out userId)) {
                // parameter is an ID
                res = TwitterFriendship.Create(f_OAuthTokens, userId, options);
            } else {
                // parameter is a screen name
                var screenName = cmd.Parameter;
                res = TwitterFriendship.Create(f_OAuthTokens, screenName, options);
            }
            CheckResponse(res);
            var person = CreatePerson(res.ResponseObject);
            if (chat.GetPerson(person.ID) == null) {
                Session.AddPersonToGroupChat(chat, person);
            }
        }

        public void CommandUnfollow(CommandModel cmd)
        {
            if (cmd.DataArray.Length < 2) {
                NotEnoughParameters(cmd);
                return;
            }

            var chat = cmd.Chat as GroupChatModel;
            if (chat == null) {
                return;
            }

            PersonModel person;
            var persons = chat.Persons;
            if (persons.TryGetValue(cmd.Parameter, out person)) {
                // parameter is an ID
                decimal userId;
                Decimal.TryParse(cmd.Parameter, out userId);
                var res = TwitterFriendship.Delete(f_OAuthTokens, userId, f_OptionalProperties);
                CheckResponse(res);
            } else {
                // parameter is a screen name
                var screenName = cmd.Parameter;
                person = persons.SingleOrDefault((arg) => arg.Value.IdentityName == screenName).Value;
                if (person == null) {
                    return;
                }
                var res = TwitterFriendship.Delete(f_OAuthTokens, screenName, f_OptionalProperties);
                CheckResponse(res);
            }
            Session.RemovePersonFromGroupChat(chat, person);
        }

        public bool IsHomeTimeLine(ChatModel chatModel)
        {
            return chatModel.Equals(f_FriendsTimelineChat);
        }

        private List<TwitterStatus> SortTimeline(IList<TwitterStatus> timeline)
        {
            List<TwitterStatus> sortedTimeline =
                new List<TwitterStatus>(
                    timeline
                );
            sortedTimeline.Sort(
                (a, b) => (a.CreatedDate.CompareTo(b.CreatedDate))
            );
            return sortedTimeline;
        }

        public void CommandSearch(CommandModel cmd)
        {
            if (cmd.DataArray.Length < 2) {
                NotEnoughParameters(cmd);
                return;
            }

            var keyword = cmd.Parameter;
            var chatName = String.Format(_("Search {0}"), keyword);
            var chat = Session.CreateChat<GroupChatModel>(keyword, chatName, this);
            Session.AddChat(chat);
            var options = CreateOptions<SearchOptions>();
            options.Count = 50;
            var response = TwitterSearch.Search(f_OAuthTokens, keyword, options);
            CheckResponse(response);
            var search = response.ResponseObject;
            var sortedSearch = SortTimeline(search);
            foreach (var status in sortedSearch) {
                AddIndexToStatus(status);
                var msg = CreateMessageBuilder().
                    Append(status, GetPerson(status.User)).
                    ToMessage();
                chat.MessageBuffer.Add(msg);
                var userId = status.User.Id.ToString();
                if (!chat.UnsafePersons.ContainsKey(userId)) {
                    chat.UnsafePersons.Add(userId, GetPerson(status.User));
                }
            }
            Session.SyncChat(chat);

            var stream = new TwitterSearchStream(this, chat, keyword,
                                                 f_OAuthTokens, f_WebProxy);
            lock (SearchStreams) {
                SearchStreams.Add(chat.ID, stream);
            }
        }

        public void CommandRetweet(CommandModel cmd)
        {
            if (cmd.DataArray.Length < 2) {
                NotEnoughParameters(cmd);
                return;
            }


            TwitterStatus status = null;
            int indexId;
            if (Int32.TryParse(cmd.Parameter, out indexId)) {
                status = GetStatusFromIndex(indexId);
            }

            decimal statusId;
            if (status == null) {
                if (!Decimal.TryParse(cmd.Parameter, out statusId)) {
                    return;
                }
            } else {
                statusId = status.Id;
            }
            var response = TwitterStatus.Retweet(f_OAuthTokens, statusId, f_OptionalProperties);
            CheckResponse(response);
            status = response.ResponseObject;

            var msg = CreateMessageBuilder().
                Append(status, GetPerson(status.User)).
                ToMessage();
            Session.AddMessageToChat(f_FriendsTimelineChat, msg);
        }

        public void CommandReply(CommandModel cmd)
        {
            if (cmd.DataArray.Length < 3) {
                NotEnoughParameters(cmd);
                return;
            }

            var id = cmd.DataArray[1];
            TwitterStatus status = null;
            int indexId;
            if (Int32.TryParse(id, out indexId)) {
                status = GetStatusFromIndex(indexId);
            }

            decimal statusId;
            if (status == null) {
                if (!Decimal.TryParse(id, out statusId)) {
                    return;
                }
                var response = TwitterStatus.Show(f_OAuthTokens, statusId,
                                                  f_OptionalProperties);
                CheckResponse(response);
                status = response.ResponseObject;
            }

            var text = String.Join(" ", cmd.DataArray.Skip(2).ToArray());
            // the screen name must be somewhere in the message for replies
            if (!text.Contains("@" + status.User.ScreenName)) {
                text = String.Format("@{0} {1}", status.User.ScreenName, text);
            }
            var options = CreateOptions<StatusUpdateOptions>();
            options.InReplyToStatusId = status.Id;
            PostUpdate(text, options);
        }

        public void CommandDelete(CommandModel cmd)
        {
            if (cmd.DataArray.Length < 2) {
                NotEnoughParameters(cmd);
                return;
            }

            TwitterStatus status = null;
            int indexId;
            if (Int32.TryParse(cmd.Parameter, out indexId)) {
                status = GetStatusFromIndex(indexId);
            }

            decimal statusId;
            if (status == null) {
                if (!Decimal.TryParse(cmd.Parameter, out statusId)) {
                    return;
                }
            } else {
                statusId = status.Id;
            }
            var response = TwitterStatus.Delete(f_OAuthTokens, statusId, f_OptionalProperties);
            CheckResponse(response);
            status = response.ResponseObject;

            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                AppendFormat(_("Successfully deleted tweet {0}."), cmd.Parameter).
                ToMessage();
            Session.AddMessageToFrontend(cmd, msg);
        }

        public void CommandFavorite(CommandModel cmd)
        {
            if (cmd.DataArray.Length < 2) {
                NotEnoughParameters(cmd);
                return;
            }

            TwitterStatus status = null;
            int indexId;
            if (Int32.TryParse(cmd.Parameter, out indexId)) {
                status = GetStatusFromIndex(indexId);
            }

            decimal statusId;
            if (status == null) {
                if (!Decimal.TryParse(cmd.Parameter, out statusId)) {
                    return;
                }
            } else {
                statusId = status.Id;
            }
            var response = TwitterFavorite.Create(f_OAuthTokens, statusId, f_OptionalProperties);
            CheckResponse(response);
            status = response.ResponseObject;

            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                AppendFormat(_("Successfully favorited tweet {0}."), cmd.Parameter).
                ToMessage();
            Session.AddMessageToFrontend(cmd, msg);
        }

        public void CommandUnfavorite(CommandModel cmd)
        {
            if (cmd.DataArray.Length < 2) {
                NotEnoughParameters(cmd);
                return;
            }

            TwitterStatus status = null;
            int indexId;
            if (Int32.TryParse(cmd.Parameter, out indexId)) {
                status = GetStatusFromIndex(indexId);
            }

            decimal statusId;
            if (status == null) {
                if (!Decimal.TryParse(cmd.Parameter, out statusId)) {
                    return;
                }
            } else {
                statusId = status.Id;
            }
            var response = TwitterFavorite.Delete(f_OAuthTokens, statusId, f_OptionalProperties);
            CheckResponse(response);
            status = response.ResponseObject;

            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                AppendFormat(_("Successfully unfavorited tweet {0}."), cmd.Parameter).
                ToMessage();
            Session.AddMessageToFrontend(cmd, msg);
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
                var msg = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendErrorText(
                        _("An error occurred while fetching the friends " +
                          "timeline from Twitter. Reason: {0}"),
                        ex.Message).
                    ToMessage();
                Session.AddMessageToChat(Chat, msg);
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
                AddIndexToStatus(status);
                var msg = CreateMessageBuilder().
                    Append(status, GetPerson(status.User)).
                    ToMessage();
                Session.AddMessageToChat(f_FriendsTimelineChat, msg);

                if (status.User.Id.ToString() == Me.ID) {
                    OnMessageSent(
                        new MessageEventArgs(f_FriendsTimelineChat, msg, null,
                                             status.InReplyToScreenName ?? String.Empty)
                    );
                } else {
                    OnMessageReceived(
                        new MessageEventArgs(f_FriendsTimelineChat, msg,
                                             status.User.ScreenName,
                                             status.InReplyToScreenName ?? String.Empty)
                    );
                }

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
                var msg = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendErrorText(
                        _("An error occurred while fetching the replies " +
                          "from Twitter. Reason: {0}"),
                        ex.Message).
                    ToMessage();
                Session.AddMessageToChat(Chat, msg);
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
                AddIndexToStatus(status);
                var msg = CreateMessageBuilder().
                    Append(status, GetPerson(status.User), highlight).
                    ToMessage();
                Session.AddMessageToChat(f_RepliesChat, msg);

                OnMessageReceived(
                    new MessageEventArgs(f_RepliesChat, msg,
                                         status.User.ScreenName,
                                         status.InReplyToScreenName ?? String.Empty)
                );

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
                var msg = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendErrorText(
                        _("An error occurred while fetching direct messages " +
                          "from Twitter. Reason: {0}"),
                        ex.Message).
                    ToMessage();
                Session.AddMessageToChat(Chat, msg);
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
                var msg = CreateMessageBuilder().
                    Append(directMsg, GetPerson(directMsg.Sender), highlight).
                    ToMessage();
                Session.AddMessageToChat(f_DirectMessagesChat, msg);

                // if there is a tab open for this user put the message there too
                string userId;
                if (receivedTimeline.Contains(directMsg)) {
                    // this is a received message
                    userId =  directMsg.SenderId.ToString();

                    OnMessageReceived(
                        new MessageEventArgs(f_DirectMessagesChat, msg,
                                             directMsg.SenderScreenName, null)
                    );
                } else {
                    // this is a sent message
                    userId = directMsg.RecipientId.ToString();

                    OnMessageSent(
                        new MessageEventArgs(f_DirectMessagesChat, msg,
                                             null, directMsg.RecipientScreenName)
                    );
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
            Me = CreatePerson(f_TwitterUser);
#if LOG4NET
            f_Logger.Debug("UpdateUser(): done.");
#endif
        }

        protected new TwitterMessageBuilder CreateMessageBuilder()
        {
            return CreateMessageBuilder<TwitterMessageBuilder>();
        }

        private T CreateOptions<T>() where T : OptionalProperties, new()
        {
            var options = new T() {
                Proxy = f_WebProxy
            };
            return options;
        }

        void PostUpdate(CommandModel cmd)
        {
            var text = cmd.IsCommand ? cmd.Parameter : cmd.Data;
            PostUpdate(text);
        }

        void PostUpdate(string text)
        {
            PostUpdate(text, null);
        }

        void PostUpdate(string text, StatusUpdateOptions options)
        {
            if (options == null) {
                options = CreateOptions<StatusUpdateOptions>();
            }
            var res = TwitterStatus.Update(f_OAuthTokens, text, options);
            CheckResponse(res);
            f_FriendsTimelineEvent.Set();
        }

        void SendMessage(CommandModel cmd)
        {
            var text = cmd.IsCommand ? cmd.Parameter : cmd.Data;
            SendMessage(cmd.Chat.Name, text);
        }

        private void SendMessage(string target, string text)
        {
            var res = TwitterDirectMessage.Send(f_OAuthTokens, target, text,
                                                f_OptionalProperties);
            CheckResponse(res);
            f_DirectMessageEvent.Set();
        }

        void AddIndexToStatus(TwitterStatus status)
        {
            lock (StatusIndex) {
                var slot = ++StatusIndexOffset;
                if (slot > StatusIndex.Length) {
                    StatusIndexOffset = 1;
                    slot = 1;
                }
                StatusIndex[slot - 1] = status;
                status.Text = String.Format("[{0:00}] {1}", slot, status.Text);
                var rtStatus = status.RetweetedStatus;
                if (rtStatus != null) {
                    rtStatus.Text = String.Format("[{0:00}] {1}", slot,
                                                  rtStatus.Text);
                }
            }
        }

        TwitterStatus GetStatusFromIndex(int slot)
        {
            lock (StatusIndex) {
                if (slot > StatusIndex.Length || slot < 1) {
                    return null;
                }
                return StatusIndex[slot - 1];
            }
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

            if (exception.InnerException != null) {
                if (exception.InnerException is System.IO.IOException) {
                    // sometimes data can't be read from the transport connection, e.g.:
                    // System.Net.WebException: Unable to read data from the transport connection: Connection reset by peer
#if LOG4NET
                    f_Logger.Warn("CheckWebException(): ignored inner-exception", exception.InnerException);
#endif
                    return;
                } else {
#if LOG4NET
                    f_Logger.Error("CheckWebException(): inner-exception", exception.InnerException);
#endif

                }
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

        internal PersonModel GetPerson(TwitterUser user)
        {
            if (user == null) {
                throw new ArgumentNullException("user");
            }

            PersonModel person;
            if (f_Friends == null || !f_Friends.TryGetValue(user.Id.ToString(), out person)) {
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
