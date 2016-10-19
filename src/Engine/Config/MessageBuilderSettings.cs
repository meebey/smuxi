// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2011, 2014-2015 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class MessageBuilderSettings
    {
        static List<MessagePatternModel> BuiltinPatterns { get; set; }
        public List<MessagePatternModel> UserPatterns { get; set; }
        public List<MessagePatternModel> Patterns { get; set; }
        public bool NickColors { get; set; }
        public bool StripFormattings { get; set; }
        public bool StripColors { get; set; }
        public TextColor HighlightColor { get; set; }
        public List<string> HighlightWords { get; set; }
        public bool Emojis { get; set; }

        static MessageBuilderSettings()
        {
            BuiltinPatterns = new List<MessagePatternModel>();
            InitBuiltinSmartLinks();
        }

        public MessageBuilderSettings()
        {
            NickColors = true;

            // No need to lock BuiltinPatterns as List<T> is thread-safe for
            // multiple readers as long as there is no writer at the same time.
            // BuiltinPatterns is only written once before the first instance
            // of MessageBuilderSettings is created via the static initializer.
            Patterns = new List<MessagePatternModel>(BuiltinPatterns);
        }

        public MessageBuilderSettings(MessageBuilderSettings settings)
        {
            if (settings == null) {
                throw new ArgumentNullException("settings");
            }

            UserPatterns = new List<MessagePatternModel>(settings.UserPatterns);
            Patterns = new List<MessagePatternModel>(settings.Patterns);
            NickColors = settings.NickColors;
            StripFormattings = settings.StripFormattings;
            StripColors = settings.StripColors;
            HighlightColor = settings.HighlightColor;
            HighlightWords = settings.HighlightWords;
        }

        static void InitBuiltinSmartLinks()
        {
            string path_last_chars = @"a-zA-Z0-9#/%&@=\-_+;:~'";
            string path_chars = path_last_chars + @"\(\)\[\]\{\}?!.,";
            string domainchars = @"[a-z0-9\-]+";
            string subdomain = domainchars + @"\.";
            string common_tld = @"de|es|im|us|com|net|org|info|biz|gov|name|edu|onion|museum";
            string any_tld = @"[a-z]+";
            string ip6 = @"(?:[0-9a-f]{0,4}:){1,7}[0-9a-f]{1,4}";
            string quoted_ip6 = @"\[" + ip6 + @"\]";
            string ip4 = @"(?:[0-9]{1,3}\.){3}[0-9]{1,3}";
            string ip = "(?:" + ip4 + "|" + ip6 + "|" + quoted_ip6 + ")";
            string domain = @"(?:(?:" + subdomain + ")+(?:" + any_tld + ")|localhost)";
            string bare_host = @"[a-z]+";
            string host = "(?:" + domain + "|" + bare_host + "|" + ip + ")";
            string short_number = "[1-9][0-9]{0,4}";
            string port = ":" + short_number;
            string user = "[a-z0-9._%+-]+@";
            string host_port = host + "(?:" + port + ")?";
            string user_host_port = "(?:" + user + ")?" + host_port;
            string user_domain = user + domain;
            string path = @"/(?:["+ path_chars +"]*["+ path_last_chars +"]+)?";
            string protocol = @"[a-z][a-z0-9\-+]*://";
            string protocol_user_host_port_path = protocol + user_host_port + "(?:" + path + ")?";

            // facebook attachment
            var regex = new Regex(
                @"(<[1-9][0-9]* attachments?>) (http://www\.facebook\.com/messages/\?action=read&tid=[0-9a-f]+)",
                RegexOptions.Compiled
            );
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "{2}",
                TextFormat = "{1}",
            });

            // protocol://user@domain:port/path
            regex = new Regex(
                protocol_user_host_port_path,
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
            BuiltinPatterns.Add(new MessagePatternModel(regex));

            // email
            regex = new Regex(
                @"(?:mailto:)?(" + user_domain + ")",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "mailto:{1}"
            });

            // addresses without protocol (heuristical)
            // include well known TLDs to prevent autogen.sh, configure.ac or
            // Gst.Buffer.Unref() from matching
            string heuristic_domain = @"(?:(?:" + subdomain + ")+(?:" + common_tld + ")|localhost)";
            string heuristic_address = heuristic_domain + "(?:" + path + ")?";
            regex = new Regex(
                heuristic_address,
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://{0}"
            });

            // Smuxi bugtracker
            regex = new Regex(@"smuxi#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "https://smuxi.im/issues/show/{1}"
            });

            // RFCs
            regex = new Regex(@"RFC[ -]?([0-9]+) (?:s\.|ss\.|sec\.|sect\.|section) ?([1-9][0-9.]*)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://tools.ietf.org/html/rfc{1}#section-{2}"
            });
            regex = new Regex(@"RFC[ -]?([0-9]+) (?:p\.|pp\.|page) ?(" + short_number + ")",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://tools.ietf.org/html/rfc{1}#page-{2}"
            });
            regex = new Regex(@"RFC[ -]?([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://www.ietf.org/rfc/rfc{1}.txt"
            });

            // XEPs
            regex = new Regex(@"XEP[ -]?([0-9]{4})",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://xmpp.org/extensions/xep-{1}.html"
            });

            // ISO
            regex = new Regex(@"ISO[ -]?([0-9]{4,5}(?:-[0-9]+)?(?::[0-9]{4})?)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://www.iso.org/iso/home/search.htm?qt={1}&published=on"
            });

            // ECMA
            regex = new Regex(@"ECMA[ -]?([0-9]{1,4})",
                                  RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://www.ecma-international.org/publications/standards/ECMA-{1}.htm"
            });

            // IEEE: IEEE-1394b, IEEE 802.11, IEEE 802.1ap-2008, IEEE 802.1AEbn-2011
            regex = new Regex(@"IEEE[ -]?([0-9]{1,4}(?:\.[0-9]{1,4})?(?:[a-z]{1,4})?(?:-[0-9]{4})?)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://odysseus.ieee.org/query.html?qt={1}&style=standard"
            });

            // bugtracker prefixes are taken from:
            // http://en.opensuse.org/openSUSE:Packaging_Patches_guidelines#Current_set_of_abbreviations

            // boost bugtracker
            regex = new Regex(@"boost#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "https://svn.boost.org/trac/boost/ticket/{1}"
            });

            // Claws bugtracker
            regex = new Regex(@"claws#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://www.thewildbeast.co.uk/claws-mail/bugzilla/show_bug.cgi?id={1}"
            });

            // CVE list
            regex = new Regex(@"CVE-[0-9]{4}-[0-9]{4,}",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://cve.mitre.org/cgi-bin/cvename.cgi?name={0}"
            });

            // CPAN bugtracker
            regex = new Regex(@"RT#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://rt.cpan.org/Public/Bug/Display.html?id={1}"
            });

            // Debian bugtracker
            regex = new Regex(@"deb#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugs.debian.org/{1}"
            });

            // Debian Security Advisories (DSA)
            regex = new Regex(@"DSA[ -]?([0-9]{4})(-[0-9]{1,2})?",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://www.debian.org/security/dsa-{1}"
            });

            // openSUSE feature tracker
            regex = new Regex(@"fate#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://features.opensuse.org/{1}"
            });

            // freedesktop bugtracker
            regex = new Regex(@"fdo#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugs.freedesktop.org/{1}"
            });

            // GNU bugtracker
            regex = new Regex(@"gnu#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://debbugs.gnu.org/{1}"
            });

            // GCC bugtracker
            regex = new Regex(@"gcc#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://gcc.gnu.org/bugzilla/show_bug.cgi?id={1}"
            });

            // GNOME bugtracker
            regex = new Regex(@"bgo#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugzilla.gnome.org/{1}"
            });

            // KDE bugtracker
            regex = new Regex(@"kde#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugs.kde.org/{1}"
            });

            // kernel bugtracker
            regex = new Regex(@"bko#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugzilla.kernel.org/show_bug.cgi?id={1}"
            });

            // launchpad bugtracker
            regex = new Regex(@"LP#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://launchpad.net/bugs/{1}"
            });

            // Mozilla bugtracker
            regex = new Regex(@"bmo#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugzilla.mozilla.org/{1}"
            });

            // Novell bugtracker
            regex = new Regex(@"bnc#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugzilla.novell.com/{1}"
            });

            // Redhat bugtracker
            regex = new Regex(@"rh#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugzilla.redhat.com/{1}"
            });

            // Samba bugtracker
            regex = new Regex(@"bso#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugzilla.samba.org/show_bug.cgi?id={1}"
            });

            // sourceforge bugtracker
            regex = new Regex(@"sf#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://sf.net/support/tracker.php?aid={1}"
            });

            // Xfce bugtracker
            regex = new Regex(@"bxo#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugzilla.xfce.org/show_bug.cgi?id={1}"
            });

            // Xamarin bugtracker
            regex = new Regex(@"bxc#([0-9]+)",
                              RegexOptions.IgnoreCase | RegexOptions.Compiled);
            BuiltinPatterns.Add(new MessagePatternModel(regex) {
                LinkFormat = "http://bugzilla.xamarin.com/show_bug.cgi?id={1}"
            });

            // TODO: msgid -> http://mid.gmane.org/{1}
            // TODO: ISSN/ISBN
            // TODO: Path: / or X:\
            // TODO: GPS -> Google Maps
            // TODO: IP -> Browser / Whois
            // TODO: Domain -> Browser / Whois
            // TODO: ANSI
            // TODO: maybe more on http://ikiwiki.info/shortcuts/
            // TODO: JID
        }

        public void ApplyConfig(UserConfig userConfig)
        {
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }

            NickColors = (bool) userConfig["Interface/Notebook/Channel/NickColors"];
            StripColors = (bool) userConfig["Interface/Notebook/StripColors"];
            StripFormattings = (bool) userConfig["Interface/Notebook/StripFormattings"];
            HighlightColor = TextColor.Parse(
                (string) userConfig["Interface/Notebook/Tab/HighlightColor"]
            );
            HighlightWords = new List<string>(
                (string[]) userConfig["Interface/Chat/HighlightWords"]
            );
            Emojis = (bool) userConfig["Interface/Chat/Emojis"];

            var patternController = new MessagePatternListController(userConfig);
            var userPatterns = patternController.GetList();
            var builtinPatterns = BuiltinPatterns;
            var patterns = new List<MessagePatternModel>(builtinPatterns.Count +
                                                         userPatterns.Count);
            // No need to lock BuiltinPatterns as List<T> is thread-safe for
            // multiple readers as long as there is no writer at the same time.
            // BuiltinPatterns is only written once before the first instance
            // of MessageBuilderSettings is created via the static initializer.
            patterns.AddRange(builtinPatterns);
            patterns.AddRange(userPatterns);
            if (Emojis) {
                // Emoji
                var regex = new Regex(@":(\w+):", RegexOptions.Compiled);
                patterns.Add(new MessagePatternModel(regex) {
                    MessagePartType = typeof(ImageMessagePartModel),
                    LinkFormat = "smuxi-emoji://{1}",
                });
            }
            Patterns = patterns;
            UserPatterns = userPatterns;
        }
    }
}
