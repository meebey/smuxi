// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010, 2013 Mirco Bauer <meebey@meebey.net>
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
using System.Web;
using System.Text;
using Twitterizer;

namespace Smuxi.Engine
{
    public class TwitterMessageBuilder : MessageBuilder
    {
        public TwitterMessageBuilder Append(TwitterStatus status,
                                            ContactModel sender,
                                            bool isHighlight)
        {
            if (status == null) {
                throw new ArgumentNullException("status");
            }
            if (sender == null) {
                throw new ArgumentNullException("sender");
            }

            // MessageModel serializer expects UTC values
            TimeStamp = status.CreatedDate.ToUniversalTime();
            AppendSenderPrefix(sender, isHighlight);

            // LAME: Twitter lies in the truncated field and says it's not
            // truncated while it is, thus always use retweet_status if
            // available
            if (status.RetweetedStatus != null) {
                var text = String.Format(
                    "RT @{0}: {1}",
                    status.RetweetedStatus.User.ScreenName,
                    status.RetweetedStatus.Text
                );
                AppendMessage(text);
            } else {
                AppendMessage(status.Text);
            }
            return this;
        }

        public TwitterMessageBuilder Append(TwitterStatus status, ContactModel sender)
        {
            return Append(status, sender, false);
        }

        public TwitterMessageBuilder Append(TwitterDirectMessage status,
                                            ContactModel sender,
                                            bool isHighlight)
        {
            if (status == null) {
                throw new ArgumentNullException("status");
            }
            if (sender == null) {
                throw new ArgumentNullException("sender");
            }

            // MessageModel serializer expects UTC values
            TimeStamp = status.CreatedDate.ToUniversalTime();
            AppendSenderPrefix(sender, isHighlight);
            AppendMessage(status.Text);
            return this;
        }

        public TwitterMessageBuilder Append(TwitterSearchResult search,
                                            ContactModel sender,
                                            bool isHighlight)
        {
            if (search == null) {
                throw new ArgumentNullException("search");
            }
            if (sender == null) {
                throw new ArgumentNullException("sender");
            }

            // MessageModel serializer expects UTC values
            TimeStamp = search.CreatedDate.ToUniversalTime();
            AppendSenderPrefix(sender, isHighlight);
            AppendMessage(search.Text);
            return this;
        }

        public TwitterMessageBuilder Append(TwitterSearchResult search,
                                            ContactModel sender)
        {
            return Append(search, sender, false);
        }

        public override MessageBuilder AppendMessage(string msg)
        {
            if (msg.Contains("\n")) {
                var normalized = new StringBuilder(msg.Length);
                msg = msg.Replace("\r\n", "\n");
                foreach (var msgPart in msg.Split('\n')) {
                    var trimmed = msgPart.TrimEnd(' ');
                    if (trimmed.Length == 0) {
                        // skip empty lines
                        continue;
                    }
                    normalized.AppendFormat("{0} ", trimmed);
                }
                // remove trailing space
                normalized.Length--;
                msg = normalized.ToString();
            }
            return base.AppendMessage(HttpUtility.HtmlDecode(msg));
        }
    }
}
