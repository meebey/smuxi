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
            string path_last_chars = @"a-zA-Z0-9#/%&=\-_+";
            string path_chars = path_last_chars + @")(?.,";
            string domainchars = @"[a-z0-9\-]+";
            string subdomain = domainchars + @"\.";
            string common_tld = @"de|es|im|us|com|net|org|info|biz|gov|name|edu|onion|museum";
            string tld = common_tld + @"|[a-z][a-z]";
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

            // addresses without protocol (heuristical)
            // include well known TLDs to prevent autogen.sh, configure.ac or
            // Gst.Buffer.Unref() from matching
            string heuristic_domain = @"(?:(?:" + subdomain + ")+(?:" + common_tld + ")|localhost)";
            string heuristic_address = heuristic_domain + "(?:" + path + ")?";
            regex = new Regex(heuristic_address, RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://{0}"
            });

            // Smuxi bugtracker
            regex = new Regex(@"smuxi#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://www.smuxi.org/issues/show/{1}"
            });

            // RFCs
            regex = new Regex(@"RFC[ -]?([0-9]+)");
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://www.ietf.org/rfc/rfc{1}.txt"
            });

            // bugtracker prefixes are taken from:
            // http://en.opensuse.org/openSUSE:Packaging_Patches_guidelines#Current_set_of_abbreviations

            // boost bugtracker
            regex = new Regex(@"boost#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "https://svn.boost.org/trac/boost/ticket/{1}"
            });

            // Claws bugtracker
            regex = new Regex(@"claws#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://www.thewildbeast.co.uk/claws-mail/bugzilla/show_bug.cgi?id={1}"
            });

            // CVE list
            regex = new Regex(@"CVE-[0-9]{4}-[0-9]{4,}", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://cve.mitre.org/cgi-bin/cvename.cgi?name={0}"
            });

            // CPAN bugtracker
            regex = new Regex(@"RT#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://rt.cpan.org/Public/Bug/Display.html?id={1}"
            });

            // Debian bugtracker
            regex = new Regex(@"deb#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugs.debian.org/{1}"
            });

            // Debian Security Advisories (DSA)
            regex = new Regex(@"DSA-([0-9]{4})(-[0-9]{1,2})?", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://www.debian.org/security/dsa-{1}"
            });

            // openSUSE feature tracker
            regex = new Regex(@"fate#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://features.opensuse.org/{1}"
            });

            // freedesktop bugtracker
            regex = new Regex(@"fdo#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugs.freedesktop.org/{1}"
            });

            // GNU bugtracker
            regex = new Regex(@"gnu#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://debbugs.gnu.org/{1}"
            });

            // GCC bugtracker
            regex = new Regex(@"gcc#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://gcc.gnu.org/bugzilla/show_bug.cgi?id={1}"
            });

            // GNOME bugtracker
            regex = new Regex(@"bgo#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugzilla.gnome.org/{1}"
            });

            // KDE bugtracker
            regex = new Regex(@"kde#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugs.kde.org/{1}"
            });

            // kernel bugtracker
            regex = new Regex(@"bko#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugzilla.kernel.org/show_bug.cgi?id={1}"
            });

            // launchpad bugtracker
            regex = new Regex(@"LP#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://launchpad.net/bugs/{1}"
            });

            // Mozilla bugtracker
            regex = new Regex(@"bmo#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugzilla.mozilla.org/{1}"
            });

            // Novell bugtracker
            regex = new Regex(@"bnc#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugzilla.novell.com/{1}"
            });

            // Redhat bugtracker
            regex = new Regex(@"rh#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugzilla.redhat.com/{1}"
            });

            // Samba bugtracker
            regex = new Regex(@"bso#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugzilla.samba.org/show_bug.cgi?id={1}"
            });

            // sourceforge bugtracker
            regex = new Regex(@"sf#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://sf.net/support/tracker.php?aid={1}"
            });

            // Xfce bugtracker
            regex = new Regex(@"bxo#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugzilla.xfce.org/show_bug.cgi?id={1}"
            });

            // Xamarin bugtracker
            regex = new Regex(@"bxc#([0-9]+)", RegexOptions.IgnoreCase);
            SmartLinks.Add(new SmartLink(regex) {
                LinkFormat = "http://bugzilla.xamarin.com/show_bug.cgi?id={1}"
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
