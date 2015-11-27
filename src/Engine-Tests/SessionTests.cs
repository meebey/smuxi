// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2015 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using NUnit.Framework;

namespace Smuxi.Engine
{
    [TestFixture]
    public class SessionTests
    {
        Session Session { get; set; }
        TestProtocolManager Protocol { get; set; }

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            Engine.Init();
            Session = new Session(Engine.Config,
                                  Engine.ProtocolManagerFactory,
                                  "local");
            Protocol = new TestProtocolManager(Session);
        }

        [Test]
        public void IsFilteredMessage()
        {
            var chat = new GroupChatModel("testchat", "Test Chat", Protocol);
            Assert.IsFalse(Session.IsFilteredMessage(chat, "foobar", MessageType.Normal));

            var filters = new List<FilterModel>(Session.Filters);
            filters.Add(
                new FilterModel() {
                    NetworkID = "TESTnet",
                    MessagePattern = "/^filter me$/"
                }
            );
            filters.Add(
                new FilterModel() {
                    NetworkID = "OTHERnet"
                }
            );
            Session.Filters = filters;
            Assert.IsTrue(Session.IsFilteredMessage(chat, "filter me", MessageType.Normal));
            Assert.IsFalse(Session.IsFilteredMessage(chat, "do not filter me", MessageType.Normal));
        }
    }
}
