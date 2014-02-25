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
        protected MessageModel SimpleMessage { get; set; }

        protected abstract IMessageBuffer CreateBuffer();
        protected abstract IMessageBuffer OpenBuffer();

        static MessageBufferTestsBase()
        {
            log4net.Config.BasicConfigurator.Configure();
        }

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
            builder.AppendErrorText("msg3");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg4");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg5");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg6");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg7");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg8");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg9");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg10");
            TestMessages.Add(builder.ToMessage());

            builder = new MessageBuilder();
            builder.AppendText("msg11");
            TestMessages.Add(builder.ToMessage());

            foreach (var msg in TestMessages) {
                Buffer.Add(msg);
            }

            builder = new MessageBuilder();
            builder.AppendIdendityName(
                new ContactModel("meeebey", "meebey", "netid", "netprot")
            );
            builder.AppendSpace();
            builder.AppendText("solange eine message aber keine url hat ist der vorteil nur gering (wenn ueberhaupt)");
            SimpleMessage = builder.ToMessage();
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
        public void IndexerAfterFlush()
        {
            Buffer.Flush();
            for (int i = 0; i < TestMessages.Count; i++) {
                Assert.AreEqual(TestMessages[i], Buffer[i]);
            }
        }

        [Test]
        public void IndexerBenchmark()
        {
            var bufferType = Buffer.GetType().Name;
            int runs = 10000;
            DateTime start, stop;
            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var msg1 = Buffer[0];
                var msg2 = Buffer[1];
                var msg3 = Buffer[2];
            }
            stop = DateTime.UtcNow;

            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "{0}[]: avg: {1:0.00} ms runs: {2} took: {3:0.00} ms",
                bufferType,
                total / runs,
                runs,
                total
            );
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

            range = Buffer.GetRange(10, 10);
            Assert.AreEqual(1, range.Count);
            Assert.AreEqual(TestMessages[10], range[0]);
        }

        [Test]
        public void GetRangeAfterFlush()
        {
            Buffer.Flush();
            var range = Buffer.GetRange(0, 3);
            Assert.AreEqual(3, range.Count);
            Assert.AreEqual(TestMessages[0], range[0]);
            Assert.AreEqual(TestMessages[1], range[1]);
            Assert.AreEqual(TestMessages[2], range[2]);
        }

        [Test]
        [Explicit]
        public void GetRangeBenchmarkWarm()
        {
            RunGetRangeBenchmark(false, 50000, 200);
            RunGetRangeBenchmark(false, 50000, 1000);
        }

        [Test]
        [Explicit]
        public void GetRangeBenchmarkCold()
        {
            RunGetRangeBenchmark(true, 50000, 200);
            RunGetRangeBenchmark(true, 50000, 1000);
        }

        public void RunGetRangeBenchmark(bool cold, int itemCount, int readCount)
        {
            Buffer.Dispose();
            Buffer = CreateBuffer();

            var bufferType = Buffer.GetType().Name;
            // generate items
            for (int i = 0; i < itemCount; i++) {
                Buffer.Add(new MessageModel(SimpleMessage));
            }
            // flush/close buffer
            if (cold) {
                Buffer.Dispose();
            } else {
                Buffer.Flush();
            }

            int runs = 10;
            var messageCount = 0;
            DateTime start, stop;
            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                if (cold) {
                    Buffer = OpenBuffer();
                }
                // retrieve the last X messages
                messageCount += Buffer.GetRange(itemCount - readCount, readCount).Count;
                if (cold) {
                    Buffer.Dispose();
                }
            }
            stop = DateTime.UtcNow;
            Assert.AreEqual(runs * readCount, messageCount);

            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "{0} {1}.GetRange({2}, {3}): avg: {4:0.00} ms ({5:0.00} ms per item) items: {6} runs: {7} took: {8:0.00} ms",
                cold ? "Cold" : "Warm",
                bufferType,
                itemCount - readCount,
                readCount,
                total / runs,
                total / runs / messageCount,
                itemCount,
                runs,
                total
            );
        }

        [Test]
        [Explicit]
        public void OpenBufferBenchmark()
        {
            RunOpenBufferBenchmark(1);
            RunOpenBufferBenchmark(10000);
            RunOpenBufferBenchmark(50000);
        }

        public void RunOpenBufferBenchmark(int itemCount)
        {
            Buffer.Dispose();
            Buffer = CreateBuffer();

            var bufferType = Buffer.GetType().Name;
            // generate items
            for (int i = 0; i < itemCount; i++) {
                Buffer.Add(new MessageModel(SimpleMessage));
            }
            // flush/close buffer
            Buffer.Dispose();

            int runs = 10;
            var messageCount = 0;
            DateTime start, stop;
            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                Buffer = OpenBuffer();
                messageCount += Buffer.Count;
                Buffer.Dispose();
            }
            stop = DateTime.UtcNow;
            Assert.AreEqual(runs * itemCount, messageCount);

            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "{0}(): avg: {1:0.00} ms items: {2} runs: {3} took: {4:0.00} ms",
                bufferType,
                total / runs,
                itemCount,
                runs,
                total
            );
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
        [Explicit]
        public void AddBenchmark()
        {
            RunAddBenchmark(15000);
            RunAddBenchmark(30000);
            RunAddBenchmark(50000);
        }

        public void RunAddBenchmark(int itemCount)
        {
            Buffer.Dispose();
            Buffer = CreateBuffer();

            DateTime start, stop;
            start = DateTime.UtcNow;
            for (int i = 0; i < itemCount; i++) {
                Buffer.Add(new MessageModel(SimpleMessage));
            }
            // force flush
            Buffer.Flush();
            stop = DateTime.UtcNow;

            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "Buffer.Add(msg): avg: {0:0.00} ms/msg items: {1} took: {2:0.00} ms",
                total / itemCount,
                itemCount,
                total
            );
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
