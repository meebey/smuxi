// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012 Mirco Bauer <meebey@meebey.net>
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
    public class MessageParserTests
    {
        [Test]
        public void ParseUrlsSimple()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendUrl("http://example.com");
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("http://example.com");
            var actualMsg = builder.ToMessage();
            MessageParser.ParseUrls(actualMsg);

            Assert.AreEqual(expectedMsg, actualMsg);
        }

        [Test]
        public void ParseUrlsBrackets()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("foo ");
            builder.AppendUrl("http://example.com", "<http://example.com>");
            builder.AppendText(" bar");
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("foo <http://example.com> bar");
            var actualMsg = builder.ToMessage();
            MessageParser.ParseUrls(actualMsg);

            Assert.AreEqual(expectedMsg, actualMsg);
        }

        [Test]
        public void ParseUrlsParentheses()
        {
            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("foo (");
            builder.AppendUrl("http://example.com");
            builder.AppendText(") bar");
            var expectedMsg = builder.ToMessage();

            builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("foo (http://example.com) bar");
            var actualMsg = builder.ToMessage();
            MessageParser.ParseUrls(actualMsg);

            Assert.AreEqual(expectedMsg, actualMsg);
        }
    }
}
