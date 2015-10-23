// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010, 2013, 2015 Mirco Bauer <meebey@meebey.net>
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
using System.Linq;
using System.Web;
using System.Text;
using System.Collections.Generic;
using Twitterizer;
using Twitterizer.Entities;

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

            // MessageModel serializer expects UTC values
            TimeStamp = status.CreatedDate.ToUniversalTime();
            if (sender != null) {
                AppendSenderPrefix(sender, isHighlight);
            }
            if (status.RetweetedStatus != null) {
                AppendFormat("RT @{0}: ", status.RetweetedStatus.User.ScreenName);
                Append(status.RetweetedStatus, null, isHighlight);
                return this;
            }

            var entities = status.Entities ?? new TwitterEntityCollection();
            var sortedEntities = entities.OrderBy(x => x.StartIndex);

            var previousEntityEndIndex = 0;
            foreach (var entity in sortedEntities) {
                if (entity.StartIndex - previousEntityEndIndex > 0) {
                    var leadingText = status.Text.Substring(
                        previousEntityEndIndex, entity.StartIndex - previousEntityEndIndex
                    );
                    AppendMessage(leadingText);
                }

                var entityText = status.Text.Substring(
                    entity.StartIndex, entity.EndIndex - entity.StartIndex
                );
                if (entity is TwitterHashTagEntity) {
                    // TODO: create built-in search link
                    AppendMessage(entityText);
                } else if (entity is TwitterUrlEntity) {
                    var urlEntity = (TwitterUrlEntity) entity;
                    AppendUrl(urlEntity.ExpandedUrl, urlEntity.DisplayUrl);
                } else if (entity is TwitterMentionEntity) {
                    // TODO: create built-in timeline link
                    AppendMessage(entityText);
                } else {
                    AppendMessage(entityText);
                }
                previousEntityEndIndex = entity.EndIndex;
            }
            var lastEntity = sortedEntities.LastOrDefault();
            if (lastEntity == null) {
                AppendMessage(status.Text);
            } else {
                var suffix = status.Text.Substring(lastEntity.EndIndex);
                AppendMessage(suffix);
            }

            if (status.QuotedStatus != null) {
                AppendSpace();
                AppendFormat("QT @{0}: ", status.QuotedStatus.User.ScreenName);
                Append(status.QuotedStatus, null, false);
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

        public override MessageBuilder AppendMessage(string msg)
        {
            msg = NormalizeNewlines(msg);
            return base.AppendMessage(HttpUtility.HtmlDecode(msg));
        }
    }
}
