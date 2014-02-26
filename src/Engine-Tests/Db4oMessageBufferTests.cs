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
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [TestFixture]
    public class Db4oMessageBufferTests : MessageBufferTestsBase
    {
        protected override IMessageBuffer CreateBuffer()
        {
            var dbFile = Path.Combine(Platform.GetBuffersPath("testuser"),
                                      "testprot");
            dbFile = Path.Combine(dbFile, "testnet");
            dbFile = Path.Combine(dbFile, "testchat.db4o");
            if (File.Exists(dbFile)) {
                File.Delete(dbFile);
            }

            return OpenBuffer();
        }

        protected override IMessageBuffer OpenBuffer()
        {
            return new Db4oMessageBuffer("testuser", "testprot", "testnet", "testchat");
        }

        [Test]
        public void Reopen()
        {
            Buffer.Dispose();

            Buffer = OpenBuffer();
            Enumerator();
        }

        [Test]
        public void ImplicitFlush()
        {
            // generate 32 extra messsages to exceed the buffer size which
            // forces a flush of the buffer to db4o
            var bufferCount = Buffer.Count;
            var msgs = new List<MessageModel>(Buffer);
            for (int i = 1; i <= 32; i++) {
                var builder = new MessageBuilder();
                builder.AppendText("msg{0}", bufferCount + i);
                var msg = builder.ToMessage();
                msgs.Add(msg);
                Buffer.Add(msg);
            }

            int j = 0;
            foreach (var msg in Buffer) {
                Assert.AreEqual(msgs[j++].ToString(), msg.ToString());
            }
            Assert.AreEqual(msgs.Count, j);
        }

        [Test]
        public void ImplicitRemoveAt()
        {
            Buffer.MaxCapacity = 16;
            // generate 32 extra messsages to exceed the max capacity which
            // forces a RemoveAt() call of the oldest messages
            var bufferCount = Buffer.Count;
            var msgs = new List<MessageModel>(Buffer);
            for (int i = 1; i <= 32; i++) {
                var builder = new MessageBuilder();
                builder.AppendText("msg{0}", bufferCount + i);
                var msg = builder.ToMessage();
                msgs.Add(msg);
                Buffer.Add(msg);
            }

            Assert.AreEqual(Buffer.MaxCapacity, Buffer.Count);
            Assert.AreEqual(msgs[32 - (Buffer.MaxCapacity - bufferCount)].ToString(), Buffer[0].ToString());
        }
    }
}
