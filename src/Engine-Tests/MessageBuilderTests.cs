// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013-2015 Mirco Bauer <meebey@meebey.net>
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
using NUnit.Framework;

namespace Smuxi.Engine
{
    [TestFixture]
    public class MessageBuilderTests
    {
        [Test]
        public void AppendHtmlMessageBold()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            var textPart = builder.CreateText("Test");
            textPart.Bold = true;
            builder.Append(textPart);
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendHtmlMessage("<b>Test</b>");
            var actualMsg = builder.ToMessage();
            Assert.AreEqual(expectedMsg, actualMsg);
        }

        [Test]
        public void AppendHtmlUrlMessage()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            string html = @"<a href=""url"">urltext</a>";
            builder.AppendHtmlMessage(html);
            var actualMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendUrl("url", "urltext");

            var expectedMsg = builder.ToMessage();
            Assert.AreEqual(expectedMsg, actualMsg);
        }

        [Test]
        public void AppendHtmlMessageWithUrls()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            string html = @"<p>TextA<a href=""url"">urltext</a>TextB</p>";
            builder.AppendHtmlMessage(html);
            var actualMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("TextA");
            builder.AppendUrl("url", "urltext");
            builder.AppendText("TextB");

            var expectedMsg = builder.ToMessage();
            Assert.AreEqual(expectedMsg, actualMsg);
        }

        [Test]
        public void AppendHtmlMessageWithNewlines()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            string html = "<p>TextA\nTextB<p>\nTextC</p>\n</p>";
            builder.AppendHtmlMessage(html);
            var actualMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("TextA");
            builder.AppendSpace();
            builder.AppendText("TextB");
            builder.AppendSpace();
            builder.AppendText("TextC");

            var expectedMsg = builder.ToMessage();
            Assert.AreEqual(expectedMsg, actualMsg);
        }

        [Test]
        public void AppendHtmlMessageCssFgRed()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            var textPart = builder.CreateText("Test");
            textPart.ForegroundColor = new TextColor(255, 0, 0);
            builder.Append(textPart);
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendHtmlMessage("<div style=\"color: #FF0000\">Test</div>");
            var actualMsg = builder.ToMessage();
            Assert.AreEqual(expectedMsg, actualMsg);
        }

        [Test]
        public void AppendHtmlMessageCssFgRedBgWhite()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            var textPart = builder.CreateText("Test");
            textPart.ForegroundColor = new TextColor(255, 0, 0);
            textPart.BackgroundColor = new TextColor(255, 255, 255);
            builder.Append(textPart);
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendHtmlMessage(
                "<div style=\"" +
                    "color: #FF0000; " +
                    "background-color: #FFFFFF" +
                "\">Test</div>");
            var actualMsg = builder.ToMessage();
            Assert.AreEqual(expectedMsg, actualMsg);
        }


        [Test]
        public void AppendHtmlMessageCssFgRedBgBlue()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            var textPart = builder.CreateText("Test");
            textPart.ForegroundColor = new TextColor(255, 0, 0);
            textPart.BackgroundColor = new TextColor(0, 0, 255);
            builder.Append(textPart);
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendHtmlMessage(
                "<div style=\"" +
                    "color: #FF0000; " +
                    "background: #0000FF url('smiley.gif') no-repeat fixed center" +
                "\">Test</div>");
            var actualMsg = builder.ToMessage();
            Assert.AreEqual(expectedMsg, actualMsg);
        }


        [Test]
        public void AppendFormatWithoutPlaceholders()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("I hope this works");
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendFormat("I hope this works");
            var actualMsg = builder.ToMessage();

            Assert.AreEqual(expectedMsg, actualMsg);
        }


        [Test]
        public void AppendFormatWithStrings()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("The quick brown fox jumps over the lazy dog.");
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendFormat("The quick brown {0} jumps over the lazy {1}.", "fox", "dog");
            var actualMsg = builder.ToMessage();

            Assert.AreEqual(expectedMsg, actualMsg);
        }


        [Test]
        public void AppendFormatWithRepeatedStrings()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("The quick brown fox jumps over the lazy fox.");
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendFormat("The quick brown {0} jumps over the lazy {0}.", "fox", "dog");
            var actualMsg = builder.ToMessage();

            Assert.AreEqual(expectedMsg, actualMsg);
        }


        [Test]
        public void AppendFormatWithBracedStrings()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("{{virtual hugs}}");
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendFormat("{{{{{0} hugs}}}}", "virtual");
            var actualMsg = builder.ToMessage();

            Assert.AreEqual(expectedMsg, actualMsg);
        }


        [Test]
        public void AppendFormatWithSubmessage()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("I wonder if I can trick this bot to op me.");
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("op");
            var insideMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendFormat("I wonder if I can trick this bot to {0} me.", insideMsg);
            var actualMsg = builder.ToMessage();

            Assert.AreEqual(expectedMsg, actualMsg);
        }


        [Test]
        [ExpectedException(typeof(System.FormatException))]
        public void AppendFormatMissingClosingBrace()
        {
            var builder = new MessageBuilder();
            builder.AppendFormat("Hello, {0!", "world");
        }

        [Test]
        [ExpectedException(typeof(System.FormatException))]
        public void AppendFormatMissingOpeningBrace()
        {
            var builder = new MessageBuilder();
            builder.AppendFormat("Hello, 0}!", "world");
        }

        [Test]
        [ExpectedException(typeof(System.FormatException))]
        public void AppendFormatPlaceholderOverflow()
        {
            var builder = new MessageBuilder();
            builder.AppendFormat("Hello, {1}!", "world");
        }

        [Test]
        [ExpectedException(typeof(System.FormatException))]
        public void AppendFormatNegativePlaceholder()
        {
            var builder = new MessageBuilder();
            builder.AppendFormat("Hello, {-1}!", "world");
        }

        [Test]
        [ExpectedException(typeof(System.FormatException))]
        public void AppendFormatNonIntegerPlaceholder()
        {
            var builder = new MessageBuilder();
            builder.AppendFormat("Hello, {zeroth}!", "world");
        }

        [Test]
        [ExpectedException(typeof(System.FormatException))]
        public void AppendFormatNonIntegerBraceChaos()
        {
            // "{{" -> escaped brace, verbatim text
            // "{{" -> escaped brace, verbatim text
            // "virtual " -> verbatim text
            // "{" -> placeholder starts
            // "0" -> placeholder text
            // "}}" -> escaped brace, placeholder text
            // "}}" -> escaped brace, placeholder text
            // "}" -> placeholder ends
            // => invalid placeholder name "0}}"
            // (same behavior as String.Format)
            var builder = new MessageBuilder();
            builder.AppendFormat("{{{{virtual {0}}}}}", "hugs");
        }

        void TestMessage(string message, MessageModel expectedMsg)
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendMessage(message);
            var actualMsg = builder.ToMessage();
            Assert.AreEqual(expectedMsg, actualMsg);
        }

        [Test]
        public void AppendTextUrlParsingSanity()
        {
            var msg = @"http://ab.cd.ef.de-hlub.gummi.museum/my_script%20windows.php?test=blub&blar=93";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel(msg));
            TestMessage(msg, builder.ToMessage());
            /*
            var msg = @"<http://smuxi.im/#sometag>";
            var msg = @"hey look at this: http://test.org, it is really cool";
            var msg = @"have you recently looked at xkcd.org?";
            var msg = @"my homepage (http://mine.my) has nothing on it";
            var msg = @"[smuxi] meebey pushed 2 new commits to stable: https://github.com/meebey/smuxi/compare/153153feddd4...ff7d23a7550c";
            var msg = @"[372 (Motd)] - FOSSCON [http://www.fosscon.org] and fossevents ";
            var msg = @"[372 (Motd)] - page (http://freenode.net/policy.shtml). Thank you for usin";
            var msg = @"look at all those deprecated fields pidgin still sets: <c xmlns=""http://jabber.org/protocol/caps"" hash=""sha-1"" node=""http://pidgin.im/"" ext=""voice-v1 camera-v1 video-v1"" ver=""AcN1/PEN8nq7AHD+9jpxMV4U6YM="" />";
            var msg = @"16:04:11 <clonkspot> Glückwunsch! (@YouTube http://t.co/IXjWtfGJ5d)";
            var msg = @"This is a http://sentence.that/ends.with?a. This is another sentence.";
            */
        }

        [Test]
        public void AppendTextUrlParsingLtGtBrackets()
        {
            var msg = @"<http://smuxi.im/#sometag>";
            var url = @"http://smuxi.im/#sometag";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("<"));
            builder.Append(new UrlMessagePartModel(url));
            builder.Append(new TextMessagePartModel(">"));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendTextUrlParsingUrlEndsInComma()
        {
            var msg = @"hey look at this: http://test.org, it is really cool";
            var url = @"http://test.org";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("hey look at this: "));
            builder.Append(new UrlMessagePartModel(url));
            builder.Append(new TextMessagePartModel(", it is really cool"));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendTextUrlParsingUrlNoProtocol()
        {
            var msg = @"hey look at this: test.org";
            var url = @"http://test.org";
            var urltext = @"test.org";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("hey look at this: "));
            builder.Append(new UrlMessagePartModel(url, urltext));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendTextUrlParsingEndsInQuestionmark()
        {
            var msg = @"have you recently looked at xkcd.com?";
            var url = @"http://xkcd.com";
            var urltext = @"xkcd.com";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("have you recently looked at "));
            builder.Append(new UrlMessagePartModel(url, urltext));
            builder.Append(new TextMessagePartModel("?"));
            TestMessage(msg, builder.ToMessage());
            /*
            var msg = @"my homepage (http://mine.my) has nothing on it";
            var msg = @"[smuxi] meebey pushed 2 new commits to stable: https://github.com/meebey/smuxi/compare/153153feddd4...ff7d23a7550c";
            var msg = @"[372 (Motd)] - FOSSCON [http://www.fosscon.org] and fossevents ";
            var msg = @"[372 (Motd)] - page (http://freenode.net/policy.shtml). Thank you for usin";
            var msg = @"look at all those deprecated fields pidgin still sets: <c xmlns=""http://jabber.org/protocol/caps"" hash=""sha-1"" node=""http://pidgin.im/"" ext=""voice-v1 camera-v1 video-v1"" ver=""AcN1/PEN8nq7AHD+9jpxMV4U6YM="" />";
            var msg = @"16:04:11 <clonkspot> Glückwunsch! (@YouTube http://t.co/IXjWtfGJ5d)";
            var msg = @"This is a http://sentence.that/ends.with?a. This is another sentence.";
            */
        }

        [Test]
        public void AppendTextUrlParsingUrlInBrackets()
        {
            var msg = @"my homepage (http://mine.my) has nothing on it";
            var url = @"http://mine.my";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("my homepage ("));
            builder.Append(new UrlMessagePartModel(url));
            builder.Append(new TextMessagePartModel(") has nothing on it"));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendTextUrlGithubMessage()
        {
            var msg = @"[smuxi] meebey pushed 2 new commits to stable: https://github.com/meebey/smuxi/compare/153153feddd4...ff7d23a7550c";
            var url = @"https://github.com/meebey/smuxi/compare/153153feddd4...ff7d23a7550c";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("[smuxi] meebey pushed 2 new commits to stable: "));
            builder.Append(new UrlMessagePartModel(url));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendTextUrlSquareBrackets()
        {
            var msg = @"[372 (Motd)] - FOSSCON [http://www.fosscon.org] and fossevents ";
            var url = @"http://www.fosscon.org";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("[372 (Motd)] - FOSSCON ["));
            builder.Append(new UrlMessagePartModel(url));
            builder.Append(new TextMessagePartModel("] and fossevents "));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendTextUrlNormalBrackets()
        {
            var msg = @"[372 (Motd)] - page (http://freenode.net/policy.shtml). Thank you for usin";
            var url = @"http://freenode.net/policy.shtml";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("[372 (Motd)] - page ("));
            builder.Append(new UrlMessagePartModel(url));
            builder.Append(new TextMessagePartModel("). Thank you for usin"));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendTextUrlMultipleInQuotes()
        {
            var msg = @"look at all those deprecated fields pidgin still sets: <c xmlns=""http://jabber.org/protocol/caps"" hash=""sha-1"" node=""http://pidgin.im/"" ext=""voice-v1 camera-v1 video-v1"" ver=""AcN1/PEN8nq7AHD+9jpxMV4U6YM="" />";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel(@"look at all those deprecated fields pidgin still sets: <c xmlns="""));
            builder.Append(new UrlMessagePartModel("http://jabber.org/protocol/caps"));
            builder.Append(new TextMessagePartModel(@""" hash=""sha-1"" node="""));
            builder.Append(new UrlMessagePartModel("http://pidgin.im/"));
            builder.Append(new TextMessagePartModel(@""" ext=""voice-v1 camera-v1 video-v1"" ver=""AcN1/PEN8nq7AHD+9jpxMV4U6YM="" />"));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendTextUrlEndsInClosedBracket()
        {
            var msg = @"16:04:11 <clonkspot> Glückwunsch! (@YouTube http://t.co/IXjWtfGJ5d)";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel(@"16:04:11 <clonkspot> Glückwunsch! (@YouTube "));
            builder.Append(new UrlMessagePartModel("http://t.co/IXjWtfGJ5d"));
            builder.Append(new TextMessagePartModel(@")"));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendTextUrlEndsInDot()
        {
            var msg = @"This is a http://sentence.th/at/ends.with?a. This is another sentence.";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel(@"This is a "));
            builder.Append(new UrlMessagePartModel("http://sentence.th/at/ends.with?a"));
            builder.Append(new TextMessagePartModel(@". This is another sentence."));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendMessageWithOddUrls()
        {
            var msg = @"zack: http://anonscm.debian.org/gitweb/?p=lintian/lintian.git;a=blob;f=checks/source-copyright.desc;h=3276a57e81b1c8c38073e667221e262df1a606c0;hb=167170d7911473a726f7e77008d8b2246a6822e8";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("zack: "));
            builder.Append(new UrlMessagePartModel("http://anonscm.debian.org/gitweb/?p=lintian/lintian.git;a=blob;f=checks/source-copyright.desc;h=3276a57e81b1c8c38073e667221e262df1a606c0;hb=167170d7911473a726f7e77008d8b2246a6822e8"));
            TestMessage(msg, builder.ToMessage());

            msg = "http://sources.debian.net/src/kfreebsd-10/10.0~svn259778-1/sys/cddl/dev/dtrace/dtrace_anon.c";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://sources.debian.net/src/kfreebsd-10/10.0~svn259778-1/sys/cddl/dev/dtrace/dtrace_anon.c"));
            TestMessage(msg, builder.ToMessage());

            msg = "http://www.stack.nl/~jilles/cgi-bin/hgwebdir.cgi/charybdis/raw-rev/9d769851c1c7";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://www.stack.nl/~jilles/cgi-bin/hgwebdir.cgi/charybdis/raw-rev/9d769851c1c7"));
            TestMessage(msg, builder.ToMessage());

            msg = "<RAOF> meebey: Associated mono branch is master-experimental in git+ssh://git.debian.org/~/public_git/mono.git";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("<RAOF> meebey: Associated mono branch is master-experimental in "));
            builder.Append(new UrlMessagePartModel("git+ssh://git.debian.org/~/public_git/mono.git"));
            TestMessage(msg, builder.ToMessage());

            msg = "<knocte> meebey: does this URL highlight ok with latest master?  https://groups.google.com/forum/#!topic/fsharp-opensource/KLejo_vw5R4";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("<knocte> meebey: does this URL highlight ok with latest master?  "));
            builder.Append(new UrlMessagePartModel("https://groups.google.com/forum/#!topic/fsharp-opensource/KLejo_vw5R4"));
            TestMessage(msg, builder.ToMessage());

            msg = "<astronouth7303> found another bad URL: http://www.flickr.com/photos/34962649@N00/12000715226/in/photostream/";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("<astronouth7303> found another bad URL: "));
            builder.Append(new UrlMessagePartModel("http://www.flickr.com/photos/34962649@N00/12000715226/in/photostream/"));
            TestMessage(msg, builder.ToMessage());

            msg = "http://en.wikipedia.org/Talk:Main_Page";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://en.wikipedia.org/Talk:Main_Page"));
            TestMessage(msg, builder.ToMessage());

            msg = "http://en.wikipedia.org/wiki/Godunov's_scheme";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://en.wikipedia.org/wiki/Godunov's_scheme"));
            TestMessage(msg, builder.ToMessage());

            msg = "<astronouth7303> ok, this is just trippy URL matching: http://couchdb.local/mydb/_magic";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("<astronouth7303> ok, this is just trippy URL matching: "));
            builder.Append(new UrlMessagePartModel("http://couchdb.local/mydb/_magic"));
            TestMessage(msg, builder.ToMessage());

            msg = "https://web.archive.org/web/20050208144213/http://www.jaganelli.de/";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("https://web.archive.org/web/20050208144213/http://www.jaganelli.de/"));
            TestMessage(msg, builder.ToMessage());

            msg = "irc://freenode/smuxi";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel(msg));
            TestMessage(msg, builder.ToMessage());

            msg = "http://www.test.de/bilder.html?data[meta_id]=13895&data[bild_id]=7";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://www.test.de/bilder.html?data[meta_id]=13895&data[bild_id]=7"));
            TestMessage(msg, builder.ToMessage());

            msg = "https://eu.api.soyoustart.com/console/#/order/dedicated/server/{serviceName}#GET";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel(msg));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        [Ignore]
        public void BrokenAppendMessageWithOddUrls()
        {
        }

        [Test]
        public void AppendMessageWithNonUrls()
        {
        }

        [Test]
        [Ignore]
        public void BrokenAppendMessageWithNonUrls()
        {
            var msg = "org.gnome.Foo.desktop";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("org.gnome.Foo.desktop"));
            TestMessage(msg, builder.ToMessage());

            msg = "ServiceStack.Common";
            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("ServiceStack.Common"));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendMessageWithSmartLinks()
        {
            var msg = "RFC2812";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://www.ietf.org/rfc/rfc2812.txt", "RFC2812"));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendBrokenMail()
        {
            var msg = "mailto:/larry@google.com";
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new TextMessagePartModel("mailto:/"));
            builder.Append(new UrlMessagePartModel("mailto:larry@google.com", "larry@google.com"));
            TestMessage(msg, builder.ToMessage());
        }

        [Test]
        public void AppendIPv4Links()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://127.0.0.1"));
            TestMessage("http://127.0.0.1", builder.ToMessage());

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://93.220.211.43:40000"));
            TestMessage("http://93.220.211.43:40000", builder.ToMessage());
        }

        [Test]
        public void AppendIPv6Links()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://[::1]"));
            TestMessage("http://[::1]", builder.ToMessage());

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://[2003:71:ce67:e700:3631:c4ff:fe2b:f874]:40000/"));
            TestMessage("http://[2003:71:ce67:e700:3631:c4ff:fe2b:f874]:40000/", builder.ToMessage());

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.Append(new UrlMessagePartModel("http://[2a01:4f8:a0:7041::2]/"));
            TestMessage("http://[2a01:4f8:a0:7041::2]/", builder.ToMessage());
        }
    }
}
