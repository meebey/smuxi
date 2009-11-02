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
    [ProtocolManagerInfo(Name = "Twitter", Description = "Twitter Micro-Blogging", Alias = "twitter")]
    public class TwitterProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        static readonly string f_LibraryTextDomain = "smuxi-engine-twitter";
        static readonly TextColor f_BlueTextColor = new TextColor(0x0000FF);
        Twitter           f_Twitter;
        TwitterUser       f_TwitterUser;
        ProtocolChatModel f_ProtocolChat;
        GroupChatModel    f_FriendsTimelineChat;
        AutoResetEvent    f_FriendsTimelineEvent = new AutoResetEvent(false);
        Int64?            f_LastFriendsTimelineStatusID;
        Thread            f_RunThread;
        bool              f_Listening;
        bool              f_IsConnected;

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
        }

        public override void Connect(FrontendManager fm, string host, int port,
                                     string username, string password)
        {
            Trace.Call(fm, host, port, username, "XXX");

            f_ProtocolChat = new ProtocolChatModel(NetworkID, "Twitter", this);
            Session.AddChat(f_ProtocolChat);
            Session.SyncChat(f_ProtocolChat);

            f_Twitter = new Twitter(username, password, "Smuxi");

            string msg;
            msg = String.Format(_("Connecting to Twitter..."));
            fm.SetStatus(msg);
            Session.AddTextToChat(f_ProtocolChat, "-!- " + msg);
            TwitterUserCollection friends;
            try {
                // for some reason VerifyCredentials() always fails
                //bool login = Twitter.VerifyCredentials(username, password);
                bool login = true;
                // as workaround we try to fetch the friend list here which
                // only works if the authorization was good
                friends = f_Twitter.User.Friends();
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

            f_TwitterUser = f_Twitter.User.Show(username);

            f_FriendsTimelineChat = new GroupChatModel(NetworkID, _("Friends Timeline"), this);
            Session.AddChat(f_FriendsTimelineChat);

            f_Listening = true;
            f_RunThread = new Thread(new ThreadStart(Run));
            f_RunThread.IsBackground = true;
            f_RunThread.Name = "TwitterProtocolManager listener";
            f_RunThread.Start();

            foreach (TwitterUser friend in friends) {
                PersonModel person = new PersonModel(
                    friend.ID.ToString(),
                    friend.ScreenName,
                    NetworkID,
                    Protocol,
                    this
                );
                f_FriendsTimelineChat.UnsafePersons.Add(person.ID, person);
            }
            Session.SyncChat(f_FriendsTimelineChat);
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

            throw new NotImplementedException();
        }

        public override bool Command(CommandModel command)
        {
            bool handled = false;
            if (command.IsCommand) {
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
                    PostMessage(command.Data);
                    handled = true;
                } else {
                    NotConnected(command);
                    handled = true;
                }
            }

            return handled;
        }

        private void NotConnected(CommandModel cd)
        {
            cd.FrontendManager.AddTextToCurrentChat(
                "-!- " + _("Not connected to Twitter")
            );
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
            if (cd.DataArray.Length >= 1) {
                user = cd.DataArray[2];
            } else {
                NotEnoughParameters(cd);
                return;
            }

            string pass;
            if (cd.DataArray.Length >= 2) {
                pass = cd.DataArray[3];
            } else {
                NotEnoughParameters(cd);
                return;
            }

            Connect(cd.FrontendManager, null, 0, user, pass);
        }

        private void Run()
        {
            Trace.Call();

            try {
                while (f_Listening) {
#if LOG4NET
                    f_Logger.Debug("Run(): getting friend timeline from twitter...");
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
                    f_Logger.Debug("Run(): done. New tweets: " +
                        (timeline == null ? 0 : timeline.Count));
#endif
                    if (timeline == null || timeline.Count == 0) {
                        f_FriendsTimelineEvent.WaitOne(60 * 1000);
                        continue;
                    }

                    // sort timeline
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

                    foreach (TwitterStatus status in sortedTimeline) {
                        MessageModel msg = CreateMessage(
                            status.Created,
                            status.TwitterUser.ScreenName,
                            status.Text
                        );
                        Session.AddMessageToChat(f_FriendsTimelineChat, msg);

                        f_LastFriendsTimelineStatusID = status.ID;
                    }

                    // only poll once per minute
                    f_FriendsTimelineEvent.WaitOne(60 * 1000);
                }
            } catch (ThreadAbortException ex) {
#if LOG4NET
                f_Logger.Debug("Run(): thread aborted");
#endif
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("Run(): Exception", ex);
#endif
            }
#if LOG4NET
            f_Logger.Debug("Run(): finishing thread.");
#endif
        }

        protected override TextColor GetIdentityNameColor(string identityName)
        {
            if (identityName == f_TwitterUser.ScreenName) {
                return f_BlueTextColor;
            }

            return base.GetIdentityNameColor(identityName);
        }

        private MessageModel CreateMessage(DateTime when, string from, string message)
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
            msg.MessageParts.Add(msgPart);

            ParseUrls(msg);

            return msg;
        }

        private void PostMessage(string text)
        {
            f_Twitter.Status.Update(text);
            f_FriendsTimelineEvent.Set();
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }
    }
}
