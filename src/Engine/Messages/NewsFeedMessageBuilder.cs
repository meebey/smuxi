// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2011 Mirco Bauer <meebey@meebey.net>
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
using System.Text;
using System.Text.RegularExpressions;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class NewsFeedMessageBuilder : MessageBuilder
    {
        public NewsFeedMessageBuilder() : base()
        {
        }

        MessageBuilder AppendFeedTitle(FeedEntry e)
        {
            return AppendHeader(e.NewsFeedTitle);
        }

        MessageBuilder AppendFeedEntryTitle(FeedEntry e)
        {
            if (!String.IsNullOrEmpty(e.Title)) {
                if (!String.IsNullOrEmpty(e.Link)) {
                    AppendUrl(e.Link, e.Title);
                } else {
                    AppendText(e.Title);
                }
            } else {
                if (!String.IsNullOrEmpty(e.Link)) {
                    AppendUrl(e.Link);
                } else {
                    AppendText("Feed item has no text and no link");
                }
            }
            AppendText(" ({0})", e.Timestamp.ToString());
            return this;
        }

        MessageBuilder AppendFeedEntryContent(FeedEntry e)
        {
            if (!String.IsNullOrEmpty(e.Content)) {
                AppendText("\n    ");
                AppendHtmlMessage(e.Content);
            } else if (!String.IsNullOrEmpty(e.Description)) {
                AppendText("\n    ");
                AppendHtmlMessage(e.Description);
            }
            return this;
        }

        public MessageBuilder AppendWithFeedTitle(FeedEntry e)
        {
            AppendEventPrefix();
            TimeStamp = e.Timestamp;

            AppendFeedTitle(e);
            AppendFeedEntryTitle(e);
            AppendFeedEntryContent(e);
            return this;
        }

        public MessageBuilder Append(FeedEntry e)
        {
            AppendEventPrefix();
            TimeStamp = e.Timestamp;

            AppendFeedEntryTitle(e);
            AppendFeedEntryContent(e);
            return this;
        }
    }
}
