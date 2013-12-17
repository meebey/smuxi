// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
    public class CommandModelTests
    {
        [Test]
        public void Parser()
        {
            var cmd = new CommandModel(null, null, "/", "/test foobar");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("test", cmd.Command);
            Assert.AreEqual("foobar", cmd.Parameter);

            cmd = new CommandModel(null, null, "/", "/test");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("test", cmd.Command);
            Assert.AreEqual("", cmd.Parameter);

            cmd = new CommandModel(null, null, "/", "/generate_messages 100");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("generate_messages", cmd.Command);
            Assert.AreEqual("100", cmd.Parameter);

            cmd = new CommandModel(null, null, "/", "/test foo bar");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("test", cmd.Command);
            Assert.AreEqual("foo bar", cmd.Parameter);

            cmd = new CommandModel(null, null, "/", "/test  foo bar");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("test", cmd.Command);
            Assert.AreEqual(" foo bar", cmd.Parameter);

            cmd = new CommandModel(null, null, "/", "/test foo bar ");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("test", cmd.Command);
            Assert.AreEqual("foo bar ", cmd.Parameter);

            cmd = new CommandModel(null, null, "/", "/test  foo bar ");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("test", cmd.Command);
            Assert.AreEqual(" foo bar ", cmd.Parameter);

            cmd = new CommandModel(null, null, "/", @"/test ""foo bar""");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("test", cmd.Command);
            Assert.AreEqual("\"foo bar\"", cmd.Parameter);
            Assert.AreEqual("foo bar", cmd.DataArray[1]);

            cmd = new CommandModel(null, null, "/", @"/test ""foo"" bar");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("test", cmd.Command);
            Assert.AreEqual("\"foo\" bar", cmd.Parameter);
            Assert.AreEqual("foo", cmd.DataArray[1]);
            Assert.AreEqual("bar", cmd.DataArray[2]);

            cmd = new CommandModel(null, null, "/", @"/test """"");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("test", cmd.Command);
            Assert.AreEqual("\"\"", cmd.Parameter);
            Assert.AreEqual("", cmd.DataArray[1]);

            cmd = new CommandModel(null, null, "/", @"//test");
            Assert.IsFalse(cmd.IsCommand);
            Assert.AreEqual("", cmd.Parameter);
            Assert.AreEqual("/test", cmd.DataArray[0]);

            cmd = new CommandModel(null, null, "/", @"/join blub@conf.nowhere.info ""password with spaces"" ""nickname with spaces""");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("join", cmd.Command);
            Assert.AreEqual("blub@conf.nowhere.info", cmd.DataArray[1]);
            Assert.AreEqual("password with spaces", cmd.DataArray[2]);
            Assert.AreEqual("nickname with spaces", cmd.DataArray[3]);

            cmd = new CommandModel(null, null, "/", @"/test bla""blub");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("bla\"blub", cmd.Parameter);
            Assert.AreEqual("/test", cmd.DataArray[0]);
            Assert.AreEqual("bla\"blub", cmd.DataArray[1]);

            cmd = new CommandModel(null, null, "/", @"/test ""blub""");
            Assert.IsTrue(cmd.IsCommand);
            Assert.AreEqual("\"blub\"", cmd.Parameter);
            Assert.AreEqual("/test", cmd.DataArray[0]);
            Assert.AreEqual("blub", cmd.DataArray[1]);
        }
    }
}

