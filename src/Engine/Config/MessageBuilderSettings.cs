// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2011, 2014 Mirco Bauer <meebey@meebey.net>
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
    public class MessageBuilderSettings
    {
        public class SmartLink
        {
            public Regex MessagePartPattern { get; set; }
            public Type MessagePartType { get; set; }
            // what is linked to
            public string LinkFormat { get; set; }
            // what is displayed
            public string TextFormat { get; set; }

            public SmartLink(Regex pattern)
            {
                if (pattern == null) {
                    throw new ArgumentNullException("pattern");
                }
                MessagePartPattern = pattern;
                MessagePartType = typeof(UrlMessagePartModel);
            }
        }

        public List<SmartLink> SmartLinks { get; private set; }

        public MessageBuilderSettings()
        {
            SmartLinks = new List<SmartLink>();
            InitDefaultLinks();
        }

        void InitDefaultLinks()
        {
            string path_last_chars = @"a-z0-9#/%&=\-_+";
            string path_chars = path_last_chars + @")(?.,";
            string domainchars = @"[a-z0-9\-]+";
            string subdomain = domainchars + @"\.";
            string tld = @"com|net|org|info|biz|gov|name|edu|museum|[a-z][a-z]";
            string domain = @"(?:(?:" + subdomain + ")+(?:" + tld + ")|localhost)";
            string port = ":[1-9][0-9]{1,4}";
            string user = "[a-z0-9._%+-]+@";
            string domain_port = domain + "(?:" + port + ")?";
            string user_domain = user + domain;
            string user_domain_port = "(?:" + user + ")?" + domain_port;
            string path = @"/(?:["+ path_chars +"]*["+ path_last_chars +"]+)?";
            string address = user_domain_port + "(?:" + path + ")?";

            // facebook attachment
            var regex = new Regex(
                @"(<[1-9][0-9]* attachments?>) (http://www\.facebook\.com/messages/\?action=read&tid=[0-9a-f]+)"
            );
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "{2}",
                TextFormat = "{1}",
            });

            // protocol://domain
            regex = new Regex(@"[a-z][a-z0-9\-]*://" + address, RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex));

            // email
            regex = new Regex(@"(?:mailto:)?(" + user_domain + ")", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "mailto:{1}"
            });

            // addresses without protocol
            regex = new Regex(address, RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://{0}"
            });

            // Smuxi bugtracker
            regex = new Regex(@"smuxi#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://www.smuxi.org/issues/show/{1}"
            });

            // TODO: msgid -> http://mid.gmane.org/{1}
            // TODO: ISSN/ISBN
            // TODO: Path: / or X:\
            // TODO: GPS -> Google Maps
            // TODO: IP -> Browser / Whois
            // TODO: Domain -> Browser / Whois
            // TODO: ISO -> http://www.iso.org/iso/search.htm?qt={1}&published=on
            // TODO: ANSI
            // TODO: ECMA
            // TODO: maybe more on http://ikiwiki.info/shortcuts/
            // TODO: JID
        }

        public void ApplyConfig(UserConfig userConfig)
        {
        }
    }
}
