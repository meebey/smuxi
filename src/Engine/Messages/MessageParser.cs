// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Smuxi.Engine
{
    public static class MessageParser
    {
        static Regex UrlRegex { get; set; }
        static Regex SimleyRegex { get; set; }

        static MessageParser()
        {
            //urlRegex = "((([a-zA-Z][0-9a-zA-Z+\\-\\.]*:)?/{0,2}[0-9a-zA-Z;/?:@&=+$\\.\\-_!~*'()%]+)?(#[0-9a-zA-Z;/?:@&=+$\\.\\-_!~*'()%]+)?)");
            // It was constructed according to the BNF grammar given in RFC 2396 (http://www.ietf.org/rfc/rfc2396.txt).
            /*
            urlRegex = @"^(?<s1>(?<s0>[^:/\?#]+):)?(?<a1>" +
                                  @"//(?<a0>[^/\?#]*))?(?<p0>[^\?#]*)" +
                                  @"(?<q1>\?(?<q0>[^#]*))?" +
                                  @"(?<f1>#(?<f0>.*))?");
            */
            UrlRegex = new Regex(
                @"(^| )(((https?|ftp):\/\/)|www\.)" +
                @"(" +
                    @"([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)|" +
                    @"localhost|" +
                    @"([a-zA-Z0-9\-]+\.)*[a-zA-Z0-9\-]+\." +
                        @"(com|net|org|info|biz|gov|name|edu|[a-zA-Z][a-zA-Z])" +
                @")" +
                @"(:[0-9]+)?((\/|\?)[^ ""]*[^ ,;\.:"">)])?",
                RegexOptions.IgnoreCase
            );

            SimleyRegex = new Regex(@":-?(\(|\))");
        }

        public static void ParseUrls(MessageModel msg)
        {
            // clone MessageParts
            IList<MessagePartModel> parts = new List<MessagePartModel>(msg.MessageParts);
            foreach (MessagePartModel part in parts) {
                if (part is UrlMessagePartModel) {
                    // no need to reparse URL parts
                    continue;
                }
                if (!(part is TextMessagePartModel)) {
                    continue;
                }
                
                TextMessagePartModel textPart = (TextMessagePartModel) part;
                Match urlMatch = UrlRegex.Match(textPart.Text);
                // OPT: fast regex scan
                if (!urlMatch.Success) {
                    // no URLs in this MessagePart, nothing to do
                    continue;
                }
                
                // found URL(s)
                // remove current MessagePartModel as we need to split it
                int idx = msg.MessageParts.IndexOf(part);
                msg.MessageParts.RemoveAt(idx);
                
                string[] textPartParts = textPart.Text.Split(new char[] {' '});
                for (int i = 0; i < textPartParts.Length; i++) {
                    string textPartPart = textPartParts[i];
                    urlMatch = UrlRegex.Match(textPartPart);
                    if (urlMatch.Success) {
                        UrlMessagePartModel urlPart = new UrlMessagePartModel(textPartPart);
                        //urlPart.ForegroundColor = new TextColor();
                        msg.MessageParts.Insert(idx++, urlPart);
                        msg.MessageParts.Insert(idx++, new TextMessagePartModel(" "));
                    } else {
                        // FIXME: we put each text part into it's own object, instead of combining them (the smart way)
                        TextMessagePartModel notUrlPart = new TextMessagePartModel(textPartPart + " ");
                        // restore formatting / colors from the original text part
                        notUrlPart.IsHighlight     = textPart.IsHighlight;
                        notUrlPart.ForegroundColor = textPart.ForegroundColor;
                        notUrlPart.BackgroundColor = textPart.BackgroundColor;
                        notUrlPart.Bold            = textPart.Bold;
                        notUrlPart.Italic          = textPart.Italic;
                        notUrlPart.Underline       = textPart.Underline;
                        msg.MessageParts.Insert(idx++, notUrlPart);
                    }
                }
            }
        }

        public static void ParseSmileys(MessageModel msg)
        {
            // clone MessageParts
            IList<MessagePartModel> parts = new List<MessagePartModel>(msg.MessageParts);
            foreach (MessagePartModel part in parts) {
                if (!(part is TextMessagePartModel)) {
                    continue;
                }
                
                TextMessagePartModel textPart = (TextMessagePartModel) part;
                Match simleyMatch = SimleyRegex.Match(textPart.Text);
                // OPT: fast regex scan
                if (!simleyMatch.Success) {
                    // no smileys in this MessagePart, nothing to do
                    continue;
                }
                
                // found smiley(s)
                // remove current MessagePartModel as we need to split it
                int idx = msg.MessageParts.IndexOf(part);
                msg.MessageParts.RemoveAt(idx);
                
                string[] textPartParts = textPart.Text.Split(new char[] {' '});
                for (int i = 0; i < textPartParts.Length; i++) {
                    string textPartPart = textPartParts[i];
                    simleyMatch = SimleyRegex.Match(textPartPart);
                    if (simleyMatch.Success) {
                        string filename = null;
                        if (textPartPart == ":-)") {
                            filename = "smile.png";
                        }
                        ImageMessagePartModel imagePart = new ImageMessagePartModel(
                            filename,
                            textPartPart
                        );
                        msg.MessageParts.Insert(idx++, imagePart);
                        msg.MessageParts.Insert(idx++, new TextMessagePartModel(" "));
                    } else {
                        // FIXME: we put each text part into it's own object, instead of combining them (the smart way)
                        TextMessagePartModel notUrlPart = new TextMessagePartModel(textPartPart + " ");
                        // restore formatting / colors from the original text part
                        notUrlPart.IsHighlight     = textPart.IsHighlight;
                        notUrlPart.ForegroundColor = textPart.ForegroundColor;
                        notUrlPart.BackgroundColor = textPart.BackgroundColor;
                        notUrlPart.Bold            = textPart.Bold;
                        notUrlPart.Italic          = textPart.Italic;
                        notUrlPart.Underline       = textPart.Underline;
                        msg.MessageParts.Insert(idx++, notUrlPart);
                    }
                }
            }
        }
    }
}
