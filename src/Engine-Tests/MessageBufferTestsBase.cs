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
using System.Collections.Generic;
using NUnit.Framework;

namespace Smuxi.Engine
{
    public abstract class MessageBufferTestsBase
    {
        protected IMessageBuffer Buffer { get; set; }
        protected List<MessageModel> TestMessages { get; set; }

        protected abstract IMessageBuffer CreateBuffer();

        [SetUp]
        public void SetUp()
        {
            Buffer = CreateBuffer();
            TestMessages = new List<MessageModel>();

            var builder = new MessageBuilder();
            builder.TimeStamp = DateTime.MinValue;
            builder.AppendText("msg1");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg2");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg3");
            TestMessages.Add(builder.ToMessage());

            foreach (var msg in TestMessages) {
                Buffer.Add(msg);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Buffer.Dispose();
        }

        [Test]
        public void Count()
        {
            Assert.AreEqual(TestMessages.Count, Buffer.Count);
        }

        [Test]
        public void Indexer()
        {
            for (int i = 0; i < TestMessages.Count; i++) {
                Assert.AreEqual(TestMessages[i], Buffer[i]);
            }
        }

        [Test]
        public void IndexOf()
        {
            Assert.AreEqual(1, Buffer.IndexOf(TestMessages[1]));

            var builder = new MessageBuilder();
            builder.AppendText("non-existent");
            var msg = builder.ToMessage();
            Assert.AreEqual(-1, Buffer.IndexOf(msg));
        }

        [Test]
        public void Contains()
        {
            Assert.IsTrue(Buffer.Contains(TestMessages[0]));

            var msg = new MessageBuilder();
            msg.AppendText("testfoo");
            Assert.IsFalse(Buffer.Contains(msg.ToMessage()));
        }

        [Test]
        public void GetRange()
        {
            var range = Buffer.GetRange(0, 3);
            Assert.AreEqual(3, range.Count);
            Assert.AreEqual(TestMessages[0], range[0]);
            Assert.AreEqual(TestMessages[1], range[1]);
            Assert.AreEqual(TestMessages[2], range[2]);

            range = Buffer.GetRange(0, 1);
            Assert.AreEqual(1, range.Count);
            Assert.AreEqual(TestMessages[0], range[0]);

            range = Buffer.GetRange(1, 1);
            Assert.AreEqual(1, range.Count);
            Assert.AreEqual(TestMessages[1], range[0]);

            range = Buffer.GetRange(2, 1);
            Assert.AreEqual(1, range.Count);
            Assert.AreEqual(TestMessages[2], range[0]);
        }

        [Test]
        public void Add()
        {
            MessageBuilder msg = new MessageBuilder();
            msg.AppendText("test");

            int count = Buffer.Count;
            Buffer.Add(msg.ToMessage());
            Assert.AreEqual(count + 1, Buffer.Count);
        }

        [Test]
        public void Clear()
        {
            Buffer.Clear();
            Assert.AreEqual(0, Buffer.Count);
        }

        [Test]
        public void RemoveAt()
        {
            Buffer.RemoveAt(0);
            Assert.AreEqual(TestMessages[1], Buffer[0]);
            Assert.AreEqual(TestMessages[2], Buffer[1]);

            Buffer.RemoveAt(1);
            Assert.AreEqual(TestMessages[1], Buffer[0]);
        }

        [Test]
        public void Enumerator()
        {
            int i = 0;
            foreach (var msg in Buffer) {
                Assert.AreEqual(TestMessages[i++].ToString(), msg.ToString());
            }
            Assert.AreEqual(TestMessages.Count, i);
        }
    }
}
