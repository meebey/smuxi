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
    }
}
