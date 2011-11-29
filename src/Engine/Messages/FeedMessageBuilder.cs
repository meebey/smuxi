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
    public class FeedMessageBuilder : MessageBuilder
    {
        public FeedMessageBuilder() : base()
        {
        }

        public MessageBuilder Append(AtomEntry entry)
        {
            AppendEventPrefix();
            AppendUrl(entry.Link[0].Url, entry.Title.Text);
            AppendText(" ({0})\n", entry.Published.ToShortDateString());
            foreach (var content in entry.Content) {
                // TODO: convert HTML: <h*> to AppendHeader, and more
                // HACK: align with action prefix
                AppendText("    ");
                AppendMessage(HtmlToText(content.Text));
            }
            return this;
        }

        string HtmlToText(string html)
        {
            if (html.Contains("\n")) {
                var normalized = new StringBuilder(html.Length);
                html = html.Replace("\r\n", "\n");
                foreach (var htmlPart in html.Split('\n')) {
                    var trimmed = htmlPart.TrimEnd(' ');
                    if (trimmed.Length == 0) {
                        // skip empty lines
                        continue;
                    }
                    normalized.AppendFormat("{0} ", trimmed);
                }
                // remove trailing space
                normalized.Length--;
                html = normalized.ToString();
            }
            // strip all HTML tags
            var text = Regex.Replace(html, "<[^>]+>", String.Empty);
            // strip leading and trailing whitespace
            text = text.Trim();
            return text;
        }
    }
}
