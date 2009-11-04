// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2009 Mirco Bauer <meebey@meebey.net>
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
using System.Threading;
using System.Collections.Generic;
using Twitterizer.Framework;
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
        Twitter                 f_Twitter;
        TwitterUser             f_TwitterUser;
        string                  f_Username;
        ProtocolChatModel       f_ProtocolChat;
        List<PersonModel>       f_Friends;
        List<GroupChatModel>    f_GroupChats = new List<GroupChatModel>();

        GroupChatModel          f_FriendsTimelineChat;
        AutoResetEvent          f_FriendsTimelineEvent = new AutoResetEvent(false);
        Thread                  f_UpdateFriendsTimelineThread;
        int                     f_UpdateFriendsTimelineInterval = 120;
        Int64?                  f_LastFriendsTimelineStatusID;
        DateTime                f_LastFriendsUpdate;

        GroupChatModel          f_RepliesChat;
        Thread                  f_UpdateRepliesThread;
        int                     f_UpdateRepliesInterval = 120;
        Int64?                  f_LastReplyStatusID;

        GroupChatModel          f_DirectMessagesChat;
        AutoResetEvent          f_DirectMessageEvent = new AutoResetEvent(false);
        Thread                  f_UpdateDirectMessagesThread;
        int                     f_UpdateDirectMessagesInterval = 120;
        Int64?                  f_LastDirectMessageReceivedStatusID;
        Int64?                  f_LastDirectMessageSentStatusID;

        bool                    f_Listening;
        bool                    f_IsConnected;

        public override string NetworkID {
            get {
                return "Twitter";
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

        public TwitterProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);

            f_ProtocolChat = new ProtocolChatModel(NetworkID, "Twitter", this);

            f_FriendsTimelineChat = new GroupChatModel(
                TwitterChatType.FriendsTimeline.ToString(),
                _("Friends Timeline"),
                this
            );
            f_GroupChats.Add(f_FriendsTimelineChat);

            f_RepliesChat = new GroupChatModel(
                TwitterChatType.Replies.ToString(),
                _("Replies"),
                this
            );
            f_GroupChats.Add(f_RepliesChat);

            f_DirectMessagesChat = new GroupChatModel(
                TwitterChatType.DirectMessages.ToString(),
                _("Direct Messages"),
                this
            );
            f_GroupChats.Add(f_DirectMessagesChat);
        }

        public override void Connect(FrontendManager fm, string host, int port,
                                     string username, string password)
        {
            Trace.Call(fm, host, port, username, "XXX");

            f_Username = username;
            f_Twitter = new Twitter(username, password, "Smuxi");

            Session.AddChat(f_ProtocolChat);
            Session.SyncChat(f_ProtocolChat);

            string msg;
            msg = String.Format(_("Connecting to Twitter..."));
            fm.SetStatus(msg);
            Session.AddTextToChat(f_ProtocolChat, "-!- " + msg);
            try {
                // for some reason VerifyCredentials() always fails
                //bool login = Twitter.VerifyCredentials(username, password);
                bool login = true;
                if (!login) {
                    fm.SetStatus(_("Login failed!"));
                    Session.AddTextToChat(f_ProtocolChat,
                        "-!- " + _("Login failed! Username and/or password are " +
                        "incorrect.")
                    );
                    return;
                }
            } catch (Exception ex) {
                fm.SetStatus(_("Connection failed!"));
                Session.AddTextToChat(f_ProtocolChat,
                    "-!- " + _("Connection failed! Reason: ") + ex.Message
                );
                return;
            }
            f_IsConnected = true;
            msg =_("Successfully connected to Twitter.");
            fm.SetStatus(msg);
            Session.AddTextToChat(f_ProtocolChat, "-!- " + msg);

            f_Listening = true;
            // twitter is sometimes pretty slow, so fetch this in the background
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    msg =_("Fetching user details from Twitter, please wait...");
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + msg);

                    UpdateUser();

                    msg =_("Finished fetching user details.");
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + msg);

                    f_FriendsTimelineChat.PersonCount = f_TwitterUser.NumberOfFriends;
                    f_RepliesChat.PersonCount = f_TwitterUser.NumberOfFriends;
                    f_DirectMessagesChat.PersonCount = f_TwitterUser.NumberOfFriends;
                } catch (Exception ex) {
                    msg =_("Failed to fetch user details from Twitter. Reason: ");
#if LOG4NET
                    f_Logger.Error("Connect(): " + msg, ex);
#endif
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + msg + ex.Message);
                }
            });
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    msg =_("Fetching friends from Twitter, please wait...");
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + msg);

                    UpdateFriends();

                    msg =_("Finished fetching friends.");
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + msg);
                } catch (Exception ex) {
                    msg =_("Failed to fetch friends from Twitter. Reason: ");
#if LOG4NET
                    f_Logger.Error("Connect(): " + msg, ex);
#endif
                    Session.AddTextToChat(f_ProtocolChat, "-!- " + msg + ex.Message);
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

        private void OpenPrivateChat(int userId)
        {
            OpenPrivateChat(userId.ToString());
        }

        private void OpenPrivateChat(string userId)
        {
            ChatModel chat =  Session.GetChat(
                userId,
                ChatType.Person,
                this
            );

            if (chat != null) {
                return;
            }

            TwitterUser user = f_Twitter.User.Show(userId);
            PersonModel person = new PersonModel(
                user.ID.ToString(),
                user.ScreenName,
                NetworkID,
                Protocol,
                this
            );
            PersonChatModel personChat = new PersonChatModel(
                person,
                user.ID.ToString(),
                user.ScreenName,
                this
            );
            Session.AddChat(personChat);
            Session.SyncChat(personChat);
        }

        public override void CloseChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);

            Session.RemoveChat(chat);
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
            return NetworkID;
        }

        public void CommandHelp(CommandModel cd)
        {
            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;

            fmsgti = new TextMessagePartModel();
            fmsgti.Text = _("[TwitterProtocolManager Commands]");
            fmsgti.Bold = true;
            fmsg.MessageParts.Add(fmsgti);

            Session.AddMessageToChat(cd.FrontendManager.CurrentChat, fmsg);

            string[] help = {
                "help",
                "connect twitter username password",
            };

            foreach (string line in help) {
                cd.FrontendManager.AddTextToCurrentChat("-!- " + line);
            }
        }

        public void CommandConnect(CommandModel cd)
        {
            string user;
            if (cd.DataArray.Length >= 3) {
                user = cd.DataArray[2];
            } else {
                NotEnoughParameters(cd);
                return;
            }

            string pass;
            if (cd.DataArray.Length >= 4) {
                pass = cd.DataArray[3];
            } else {
                NotEnoughParameters(cd);
                return;
            }

            Connect(cd.FrontendManager, null, 0, user, pass);
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
                        PostUpdate(cmd.Data);
                        break;
                    case TwitterChatType.DirectMessages:
                        fm.AddTextToCurrentChat(
                            "-!- " +
                            _("Cannot send message, no target specified. "+
                              "Use: /msg $nick message")
                        );
                        break;
                }
            } else if (cmd.Chat.ChatType == ChatType.Person) {
                SendMessage(cmd.Chat.ID, cmd.Data);
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

            TwitterUser user = null;
            try {
                user = f_Twitter.User.Show(nickname);
            } catch (NullReferenceException) {
                // HACK: User.Show() might throw an NRE if the user does not exist
                // trying to handle this gracefully
            }
            if (user == null) {
                fm.AddTextToCurrentChat("-!- " +
                    _("Could not send message, the specified user does not exist.")
                );
                return;
            }

            OpenPrivateChat(user.ID);

            if (cmd.DataArray.Length >= 3) {
                string message = String.Join(" ", cmd.DataArray, 2, cmd.DataArray.Length-2);
                SendMessage(user.ID.ToString(), message);
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
                (a, b) => (a.Created.CompareTo(b.Created))
            );
            return sortedTimeline;
        }

        private void UpdateFriendsTimelineThread()
        {
            Trace.Call();

            try {
                // query the timeline only after we have fetched the user and friends
                while (f_TwitterUser == null || f_Friends == null) {
                    Thread.Sleep(1000);
                }

                // populate friend list
                lock (f_Friends) {
                    foreach (PersonModel friend in f_Friends) {
                        f_FriendsTimelineChat.UnsafePersons.Add(friend.ID, friend);
                    }
                }
                Session.AddChat(f_FriendsTimelineChat);
                Session.SyncChat(f_FriendsTimelineChat);

                while (f_Listening) {
                    try {
                        UpdateFriendsTimeline();
                    } catch (TwitterizerException ex) {
                        if (ex.Message.Contains("rate limited")) {
                            // ignore and keep trying
                        } else {
                            throw;
                        }
                    }

                    // only poll once per interval or when we get fired
                    f_FriendsTimelineEvent.WaitOne(
                        f_UpdateFriendsTimelineInterval * 1000
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
                string msg =_("Error occured while fetching the friends timeline from Twitter. Reason: ");
                Session.AddTextToChat(f_ProtocolChat, "-!- " + msg + ex.Message);
            }
#if LOG4NET
            f_Logger.Debug("UpdateFriendsTimelineThread(): finishing thread.");
#endif
        }

        private void UpdateFriendsTimeline()
        {
            Trace.Call();

#if LOG4NET
            f_Logger.Debug("UpdateFriendsTimeline(): getting friend timeline from twitter...");
#endif
            TwitterParameters parameters = new TwitterParameters();
            parameters.Add(TwitterParameterNames.Count, 50);
            if (f_LastFriendsTimelineStatusID != null) {
                parameters.Add(TwitterParameterNames.SinceID,
                               f_LastFriendsTimelineStatusID);
            }
            TwitterStatusCollection timeline =
                f_Twitter.Status.FriendsTimeline(parameters);
#if LOG4NET
            f_Logger.Debug("UpdateFriendsTimeline(): done. New tweets: " +
                (timeline == null ? 0 : timeline.Count));
#endif
            if (timeline == null || timeline.Count == 0) {
                return;
            }

            List<TwitterStatus> sortedTimeline = SortTimeline(timeline);
            foreach (TwitterStatus status in sortedTimeline) {
                MessageModel msg = CreateMessage(
                    status.Created,
                    status.TwitterUser.ScreenName,
                    status.Text
                );
                Session.AddMessageToChat(f_FriendsTimelineChat, msg);

                f_LastFriendsTimelineStatusID = status.ID;
            }
        }

        private void UpdateRepliesThread()
        {
            Trace.Call();

            try {
                // query the replies only after we have fetched the user and friends
                while (f_TwitterUser == null || f_Friends == null) {
                    Thread.Sleep(1000);
                }

                // populate friend list
                lock (f_Friends) {
                    foreach (PersonModel friend in f_Friends) {
                        f_RepliesChat.UnsafePersons.Add(friend.ID, friend);
                    }
                }
                Session.AddChat(f_RepliesChat);
                Session.SyncChat(f_RepliesChat);

                while (f_Listening) {
                    try {
                        UpdateReplies();
                    } catch (TwitterizerException ex) {
                        if (ex.Message.Contains("rate limited")) {
                            // ignore and keep trying
                        } else {
                            throw;
                        }
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
                string msg =_("Error occured while fetching the replies from Twitter. Reason: ");
                Session.AddTextToChat(f_ProtocolChat, "-!- " + msg + ex.Message);
            }
#if LOG4NET
            f_Logger.Debug("UpdateRepliesThread(): finishing thread.");
#endif
        }

        private void UpdateReplies()
        {
            Trace.Call();

#if LOG4NET
            f_Logger.Debug("UpdateReplies(): getting replies from twitter...");
#endif
            TwitterParameters parameters = new TwitterParameters();
            parameters.Add(TwitterParameterNames.Count, 50);
            if (f_LastReplyStatusID != null) {
                parameters.Add(TwitterParameterNames.SinceID,
                               f_LastReplyStatusID);
            }
            TwitterStatusCollection timeline =
                f_Twitter.Status.Replies(parameters);
#if LOG4NET
            f_Logger.Debug("UpdateReplies(): done. New replies: " +
                (timeline == null ? 0 : timeline.Count));
#endif
            if (timeline == null || timeline.Count == 0) {
                return;
            }

            // if this isn't the first time we receive replies, this is new!
            bool highlight = f_LastReplyStatusID != null;
            List<TwitterStatus> sortedTimeline = SortTimeline(timeline);
            foreach (TwitterStatus status in sortedTimeline) {
                MessageModel msg = CreateMessage(
                    status.Created,
                    status.TwitterUser.ScreenName,
                    status.Text,
                    highlight
                );
                Session.AddMessageToChat(f_RepliesChat, msg);

                f_LastReplyStatusID = status.ID;
            }
        }

        private void UpdateDirectMessagesThread()
        {
            Trace.Call();

            try {
                // query the messages only after we have fetched the user and friends
                while (f_TwitterUser == null || f_Friends == null) {
                    Thread.Sleep(1000);
                }

                // populate friend list
                lock (f_Friends) {
                    foreach (PersonModel friend in f_Friends) {
                        f_DirectMessagesChat.UnsafePersons.Add(friend.ID, friend);
                    }
                }
                Session.AddChat(f_DirectMessagesChat);
                Session.SyncChat(f_DirectMessagesChat);

                while (f_Listening) {
                    try {
                        UpdateDirectMessages();
                    } catch (TwitterizerException ex) {
                        if (ex.Message.Contains("rate limited")) {
                            // ignore and keep trying
                        } else {
                            throw;
                        }
                    }

                    // only poll once per interval or when we get fired
                    f_DirectMessageEvent.WaitOne(
                        f_UpdateDirectMessagesInterval * 1000
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
                string msg =_("Error occured while fetching direct messages from Twitter. Reason: ");
                Session.AddTextToChat(f_ProtocolChat, "-!- " + msg + ex.Message);
            }
#if LOG4NET
            f_Logger.Debug("UpdateDirectMessagesThread(): finishing thread.");
#endif
        }

        private void UpdateDirectMessages()
        {
            Trace.Call();

            TwitterParameters parameters;
#if LOG4NET
            f_Logger.Debug("UpdateDirectMessages(): getting received direct messages from twitter...");
#endif
            parameters = new TwitterParameters();
            parameters.Add(TwitterParameterNames.Count, 50);
            if (f_LastDirectMessageReceivedStatusID != null) {
                parameters.Add(TwitterParameterNames.SinceID,
                               f_LastDirectMessageReceivedStatusID);
            }
            TwitterStatusCollection receivedTimeline =
                f_Twitter.DirectMessages.DirectMessages(parameters);
#if LOG4NET
            f_Logger.Debug("UpdateDirectMessages(): done. New messages: " +
                (receivedTimeline == null ? 0 : receivedTimeline.Count));
#endif

#if LOG4NET
            f_Logger.Debug("UpdateDirectMessages(): getting sent direct messages from twitter...");
#endif
            parameters = new TwitterParameters();
            parameters.Add(TwitterParameterNames.Count, 50);
            if (f_LastDirectMessageSentStatusID != null) {
                parameters.Add(TwitterParameterNames.SinceID,
                               f_LastDirectMessageSentStatusID);
            }
            TwitterStatusCollection sentTimeline =
                f_Twitter.DirectMessages.DirectMessagesSent(parameters);
#if LOG4NET
            f_Logger.Debug("UpdateDirectMessages(): done. New messages: " +
                (sentTimeline == null ? 0 : sentTimeline.Count));
#endif

            TwitterStatusCollection timeline = new TwitterStatusCollection();
            if (receivedTimeline != null) {
                foreach (TwitterStatus status in receivedTimeline) {
                    timeline.Add(status);
                }
            }
            if (sentTimeline != null) {
                foreach (TwitterStatus status in sentTimeline) {
                    timeline.Add(status);
                }
            }

            if (timeline.Count == 0) {
                // nothing to do
                return;
            }

            List<TwitterStatus> sortedTimeline = SortTimeline(timeline);
            foreach (TwitterStatus status in sortedTimeline) {
                // if this isn't the first time a receive a direct message,
                // this is a new one!
                bool highlight = receivedTimeline.Contains(status) &&
                                 f_LastDirectMessageReceivedStatusID != null;
                MessageModel msg = CreateMessage(
                    status.Created,
                    status.TwitterUser.ScreenName,
                    status.Text,
                    highlight
                );
                Session.AddMessageToChat(f_DirectMessagesChat, msg);

                // if there is a tab open for this user put the message there too
                string userId;
                if (receivedTimeline.Contains(status)) {
                    // this is a received message
                    userId =  status.TwitterUser.ID.ToString();
                } else {
                    // this is a sent message
                    userId = status.Recipient.ID.ToString();
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
                foreach (TwitterStatus status in receivedTimeline) {
                    f_LastDirectMessageReceivedStatusID = status.ID;
                    break;
                }
            }
            if (sentTimeline != null) {
                // first one is the newest
                foreach (TwitterStatus status in sentTimeline) {
                    f_LastDirectMessageSentStatusID = status.ID;
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
            f_Logger.Debug("UpdateFriends(): getting friends from twitter...");
#endif
            TwitterUserCollection friends = f_Twitter.User.Friends();
#if LOG4NET
            f_Logger.Debug("UpdateFriends(): done. Friends: " +
                (friends == null ? 0 : friends.Count));
#endif
            if (friends == null || friends.Count == 0) {
                return;
            }

            List<PersonModel> persons = new List<PersonModel>(friends.Count);
            foreach (TwitterUser friend in friends) {
                PersonModel person = new PersonModel(
                    friend.ID.ToString(),
                    friend.ScreenName,
                    NetworkID,
                    Protocol,
                    this
                );
                persons.Add(person);
            }
            f_Friends = persons;
        }

        private void UpdateUser()
        {
#if LOG4NET
            f_Logger.Debug("UpdateUser(): getting user details from twitter...");
#endif
            f_TwitterUser = f_Twitter.User.Show(f_Username);
#if LOG4NET
            f_Logger.Debug("UpdateUser(): done.");
#endif
        }

        protected override TextColor GetIdentityNameColor(string identityName)
        {
            if (identityName == f_TwitterUser.ScreenName) {
                return f_BlueTextColor;
            }

            return base.GetIdentityNameColor(identityName);
        }

        private MessageModel CreateMessage(DateTime when, string from,
                                           string message)
        {
            return CreateMessage(when, from, message, false);
        }

        private MessageModel CreateMessage(DateTime when, string from,
                                           string message, bool highlight)
        {
            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;
            msg.TimeStamp = when;

            msgPart = new TextMessagePartModel();
            msgPart.Text = "<";
            msg.MessageParts.Add(msgPart);

            msgPart = new TextMessagePartModel();
            msgPart.Text = from;
            msgPart.ForegroundColor = GetIdentityNameColor(from);
            msg.MessageParts.Add(msgPart);

            msgPart = new TextMessagePartModel();
            msgPart.Text = "> ";
            msg.MessageParts.Add(msgPart);

            msgPart = new TextMessagePartModel(message);
            msgPart.IsHighlight = highlight;
            msg.MessageParts.Add(msgPart);

            ParseUrls(msg);

            return msg;
        }

        private void PostUpdate(string text)
        {
            f_Twitter.Status.Update(text);
            f_FriendsTimelineEvent.Set();
        }

        private void SendMessage(string target, string text)
        {
            f_Twitter.DirectMessages.New(target, text);
            f_DirectMessageEvent.Set();
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }
    }
}
