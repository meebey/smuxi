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
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Smuxi.Engine
{
    public class MessageBuilderSettings
    {

        public class SmartLink
        {
            public enum ETargetType
            {
                Text, Url, Image
            }

            public Regex MessagePartPattern { get; set; }
            // what is linked to
            public string LinkFormat { get; set; }
            // what is displayed
            public string TextFormat { get; set; }
            public ETargetType Type { get; set; }
        };

        public List<SmartLink> SmartLinks { get; private set; }

        void CreateSmartLink(Regex regex)
        {
            CreateSmartLink(regex, null, null);
        }

        void CreateSmartLink(Regex regex, string linkPattern)
        {
            CreateSmartLink(regex, linkPattern, null);
        }

        void CreateSmartLink(Regex regex, string linkPattern, string textPattern)
        {
            var link = new SmartLink();
            link.MessagePartPattern = regex;
            link.LinkFormat = linkPattern;
            link.TextFormat = textPattern;
            link.Type = SmartLink.ETargetType.Url;
            SmartLinks.Add(link);
        }

        void CreateSmartText(Regex regex, string textPattern)
        {
            var link = new SmartLink();
            link.MessagePartPattern = regex;
            link.TextFormat = textPattern;
            link.Type = SmartLink.ETargetType.Text;
            SmartLinks.Add(link);
        }

        public MessageBuilderSettings()
        {
            SmartLinks = new List<SmartLink>();
            string path_last_chars = @"a-z0-9#/%&=\-_+";
            string path_chars = path_last_chars + @")(?.,";
            string domainchars = @"[a-z0-9\-]+";
            string subdomain = domainchars + @"\.";
            string tld = @"com|net|org|info|biz|gov|name|edu|museum|[a-z][a-z]";
            string domain = @"(?:(?:" + subdomain + ")+(?:" + tld + ")|localhost)";
            string port = ":[1-9][0-9]{1,4}";
            string domain_port = domain + "(?:" + port + ")?";
            string path = @"/(?:["+ path_chars +"]*["+ path_last_chars +"]+)?";
            string address = domain_port + "(?:" + path + ")?";

            // facebook attachment
            CreateSmartLink(new Regex(@"(<[1-9][0-9]* attachments?>) (http://www\.facebook\.com/messages/\?action=read&tid=[0-9a-f]+)"), "{2}", "{1}");

            // protocol://domain
            CreateSmartLink(new Regex(@"[a-z][a-z0-9\-]*://" + address, RegexOptions.IgnoreCase));

            // E-Mail
            CreateSmartLink(new Regex(@"([a-z0-9._%+-]+@(?:[a-z0-9-]+\.)+[a-z]{2,})", RegexOptions.IgnoreCase), "mailto:{0}");
            // addresses without protocol
            CreateSmartLink(new Regex(address, RegexOptions.IgnoreCase), "http://{0}");
            // smuxi bugtracker
            CreateSmartLink(new Regex(@"#([0-9]+)"), "http://www.smuxi.org/issues/{0}");

            // TODO: msgid -> http://mid.gmane.org/{1}
            // TODO: RFC -> http://www.ietf.org/rfc/rfc{1}.txt
            // TODO: CVE-YYYY-XXXX -> http://cve.mitre.org/cgi-bin/cvename.cgi?name={1}
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
