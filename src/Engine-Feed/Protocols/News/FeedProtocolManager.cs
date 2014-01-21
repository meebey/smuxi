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
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using Smuxi.Common;
using System.Timers;

namespace Smuxi.Engine
{

    [ProtocolManagerInfo(Name = "Feed", Description = "FeedReader", Alias = "feed")]
    public class FeedProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        static readonly string f_LibraryTextDomain = "smuxi-engine-feed";
        ProtocolChatModel NetworkChat { get; set;}
        ServerModel ServerModel { get; set;}

        Timer FeedsTimer { get; set; }

        List<Feed> Feeds { get; set; }
        List<Feed> NewFeeds { get; set; }

        public override string NetworkID {
            get {
                return "Feed";
            }
        }

        public override string Protocol {
            get {
                return "Feed";
            }
        }

        public override ChatModel Chat {
            get {
                return NetworkChat;
            }
        }

        public FeedProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);
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

            ServerModel = server;

            NetworkChat = Session.CreateChat<ProtocolChatModel>(
                NetworkID, server.Hostname, this
            );
            Session.AddChat(NetworkChat);
            Session.SyncChat(NetworkChat);

            Feeds = new List<Feed>();
            NewFeeds = new List<Feed>();
            FeedsTimer = new Timer();

            OnConnected(EventArgs.Empty);

            FeedsTimer.Interval = 1;
            FeedsTimer.Elapsed += HandleFeedsTimerElapsed;
            FeedsTimer.AutoReset = false;
            FeedsTimer.Start();
        }

        bool CreateFeed(CommandModel command, string addr)
        {
            try {
                Uri u = new Uri(addr);
                if (!ServerModel.ValidateServerCertificate) {
                    var whitelist = Session.CertificateValidator.HostnameWhitelist;
                    lock (whitelist) {
                        if (!whitelist.Contains(u.Host)) {
                            whitelist.Add(u.Host);
                        }
                    }
                }
                var feed = new Feed(u);
                feed.ProxySettings = new ProxySettings();
                feed.ProxySettings.ApplyConfig(Session.UserConfig);
                lock (NewFeeds) {
                    NewFeeds.Add(feed);
                }
#if LOG4NET
                f_Logger.Info("CreateFeed(): adding feed: " + addr);
#endif
                return true;
            } catch {
                var builder = CreateMessageBuilder();
                builder.AppendErrorText(_("{0} is not a valid Uri"), addr);
                Session.AddMessageToFrontend(command, builder.ToMessage());
                return false;
            }
        }

        void HandleFeedsTimerElapsed(object sender, ElapsedEventArgs e)
        {
            UpdateFeeds();
            FeedsTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            FeedsTimer.Start();
        }

        public void UpdateFeedsEarly()
        {
            FeedsTimer.Interval = 1;
            FeedsTimer.Stop();
            FeedsTimer.Start();
        }

        void UpdateFeeds()
        {
            Trace.Call();
            List<FeedEntry> list = new List<FeedEntry>();

            // get new feeds from main thread
            List<Feed> newfeeds;
            lock (NewFeeds) {
                newfeeds = NewFeeds;
                NewFeeds = new List<Feed>();
            }
            Feeds.AddRange(newfeeds);

            foreach (Feed feed in Feeds) {
#if LOG4NET
                f_Logger.Info("UpdateFeeds(): checking feed: " + feed.Url.AbsoluteUri);
#endif
                List<FeedEntry> newitems = null;
                try {
                    newitems = feed.GetNewItems();
                } catch (Exception e) {
#if LOG4NET
                    f_Logger.ErrorFormat("Could not fetch {0} : {1}", feed.Url, e);
#endif
                }
                if (newitems != null) {
#if LOG4NET
                    f_Logger.Info("UpdateFeeds(): found " + newitems.Count + " new items");
#endif
                    list.AddRange(newitems);
                } else {
#if LOG4NET
                    f_Logger.Info("UpdateFeeds(): found no new items");
#endif
                }
            }
#if LOG4NET
            f_Logger.Info("UpdateFeeds(): found a total of " + list.Count + " new items");
#endif
            list.Sort( (a, b) => (a.Timestamp.CompareTo(b.Timestamp)) );
            foreach (FeedEntry e in list) {
#if LOG4NET
                f_Logger.Info("UpdateFeeds(): sending feed to chat: " + e.Title);
                f_Logger.Info("UpdateFeeds(): Desc: " + e.Description);
                f_Logger.Info("UpdateFeeds(): Content: " + e.Content);
#endif
                var builder = CreateMessageBuilder<NewsFeedMessageBuilder>();
                builder.AppendWithFeedTitle(e);
                Session.AddMessageToChat(NetworkChat, builder.ToMessage());
            }
        }

        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            // does nothing, should we reparse ALL feeds here?
        }

        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            // does nothing, should the timer be stopped?
            FeedsTimer.Close();
        }

        public override bool Command(CommandModel command)
        {
            bool handled = false;
            if (command.IsCommand) {
                switch (command.Command) {
                    case "add":
                        CommandAdd(command);
                        handled = true;
                        break;
                    case "list":
                        CommandList(command);
                        handled = true;
                        break;
                    case "help":
                        CommandHelp(command);
                        handled = true;
                        break;
                }
            } else {
                handled = CreateFeed(command, command.DataArray[0]);
            }

            return handled;
        }

        public void CommandList(CommandModel command)
        {
            var builder = CreateMessageBuilder();
            builder.AppendHeader(_("Feed List"));
            Session.AddMessageToFrontend(command, builder.ToMessage());
            // todo, many slow feeds might delay this command
            lock (Feeds) {
                foreach (var feed in Feeds) {
                    builder = CreateMessageBuilder();
                    builder.AppendText(feed.Url.AbsoluteUri);
                    Session.AddMessageToFrontend(command, builder.ToMessage());
                }
            }
        }

        public void CommandAdd(CommandModel command)
        {
            Trace.Call(command);
            if (command.DataArray.Length != 2) {
                var builder = CreateMessageBuilder();
                builder.AppendErrorText(_("/add requires (only) a feed url as argument"));
                Session.AddMessageToFrontend(command, builder.ToMessage());
                return;
            }
            CreateFeed(command, command.DataArray[1]);
            UpdateFeedsEarly();
        }

        public void CommandHelp(CommandModel cd)
        {
            var builder = CreateMessageBuilder();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            builder.AppendHeader(_("Feed Commands"));
            Session.AddMessageToFrontend(cd, builder.ToMessage());

            string[] help = {
                "help",
                "add url",
                "list"
            };

            foreach (string line in help) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                Session.AddMessageToFrontend(cd, builder.ToMessage());
            }
        }

        static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }

        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            throw new NotImplementedException ();
        }

        public override void OpenChat(FrontendManager fm, ChatModel chat)
        {
            throw new NotImplementedException ();
        }

        public override void CloseChat(FrontendManager fm, ChatModel chat)
        {
            throw new NotImplementedException ();
        }

        public override void SetPresenceStatus(PresenceStatus status, string message)
        {
            // do nothing
        }
    }
}
