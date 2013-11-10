// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013-2014 Mirco Bauer <meebey@meebey.net>
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
using Twitterizer;
using Twitterizer.Streaming;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class TwitterSearchStream : IDisposable
    {
#if LOG4NET
        static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        TwitterProtocolManager ProtocolManager { get; set; }
        Session Session { get; set; }
        TwitterStream Stream { get; set; }
        GroupChatModel Chat { get; set; }
        RateLimiter MessageRateLimiter { get; set; }

        public TwitterSearchStream(TwitterProtocolManager protocolManager,
                                   GroupChatModel chat, string keyword,
                                   OAuthTokens tokens, WebProxy proxy)
        {
            if (protocolManager == null) {
                throw new ArgumentNullException("protocolManager");
            }
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (keyword == null) {
                throw new ArgumentNullException("keyword");
            }
            if (tokens == null) {
                throw new ArgumentNullException("tokens");
            }

            ProtocolManager = protocolManager;
            Session = protocolManager.Session;
            Chat = chat;

            var options = new StreamOptions();
            options.Track.Add(keyword);

            Stream = new TwitterStream(tokens, null, options);
            Stream.Proxy = proxy;
            Stream.StartPublicStream(OnStreamStopped, OnStatusCreated, OnStatusDeleted, OnEvent);

            MessageRateLimiter = new RateLimiter(5, TimeSpan.FromSeconds(5));
        }

        ~TwitterSearchStream()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            Trace.Call(disposing);

            Stream.EndStream();
            Stream.Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected TwitterMessageBuilder CreateMessageBuilder()
        {
            var builder = new TwitterMessageBuilder();
            builder.ApplyConfig(Session.UserConfig);
            return builder;
        }

        void OnStreamStopped(StopReasons reason)
        {
            Trace.Call(reason);

            try {
                Session.DisableChat(Chat);
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("OnStreamStopped()", ex);
#endif
            }
        }

        void OnStatusCreated(TwitterStatus status)
        {
            Trace.Call(status);

            try {
                if (MessageRateLimiter.IsRateLimited) {
                    return;
                }
                MessageRateLimiter++;

                var sender = ProtocolManager.GetPerson(status.User);
                var userId = status.User.Id.ToString();
                lock (Chat.UnsafePersons) {
                    if (!Chat.UnsafePersons.ContainsKey(userId)) {
                        Session.AddPersonToGroupChat(Chat, sender);
                    }
                }
                var msg = CreateMessageBuilder().
                    Append(status, sender).
                    ToMessage();
                Session.AddMessageToChat(Chat, msg);
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("OnStatusCreated()", ex);
#endif
            }
        }

        void OnStatusDeleted(TwitterStreamDeletedEvent status)
        {
            Trace.Call(status);

            try {
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("OnStatusDeleted()", ex);
#endif
            }
        }

        void OnEvent(TwitterStreamEvent @event)
        {
            Trace.Call(@event);

            try {
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("OnEvent()", ex);
#endif
            }
        }
    }
}
