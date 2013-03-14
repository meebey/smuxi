// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
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
    }
}
