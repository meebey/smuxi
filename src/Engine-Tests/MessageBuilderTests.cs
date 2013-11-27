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
    }
}
