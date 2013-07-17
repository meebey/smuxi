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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;

namespace Smuxi.Engine
{
    [TestFixture]
    public class MessageModelTests
    {
        [Test]
        public void Equals()
        {
            var msg = new MessageModel("test");
            Assert.IsFalse(msg.Equals(null));
            msg = new MessageModel();
            Assert.IsFalse(msg.Equals(null));
            Assert.IsFalse(msg == null);
        }

        [Test]
        public void CopyConstructor()
        {
            var builder = new MessageBuilder();
            builder.AppendEventPrefix();
            builder.AppendUrl("http://example.com");
            builder.AppendText("foobar");
            var msg = builder.ToMessage();
            var copiedMsg = new MessageModel(msg);

            Assert.AreNotSame(msg, copiedMsg);
            Assert.IsNotNull(copiedMsg.MessageParts);
            Assert.AreNotSame(msg.MessageParts, copiedMsg.MessageParts);
            Assert.AreEqual(msg, copiedMsg);
        }

        [Test]
        public void Compact()
        {
            var msg = new MessageModel("foo bar");
            msg.Compact();
            Assert.AreEqual(1, msg.MessageParts.Count);
            var textPart = (TextMessagePartModel) msg.MessageParts[0];
            Assert.AreEqual("foo bar", textPart.Text);

            msg = new MessageModel();
            msg.MessageParts.Add(new TextMessagePartModel("foo"));
            msg.MessageParts.Add(new TextMessagePartModel(" bar"));
            msg.MessageParts.Add(new TextMessagePartModel(" me"));
            msg.Compact();
            Assert.AreEqual(1, msg.MessageParts.Count);
            textPart = (TextMessagePartModel) msg.MessageParts[0];
            Assert.AreEqual("foo bar me", textPart.Text);

            msg = new MessageModel();
            msg.MessageParts.Add(new TextMessagePartModel("foo"));
            msg.MessageParts.Add(new TextMessagePartModel(" bar"));
            msg.MessageParts.Add(new TextMessagePartModel(" me", true));
            msg.Compact();
            Assert.AreEqual(2, msg.MessageParts.Count);
            textPart = (TextMessagePartModel) msg.MessageParts[0];
            Assert.AreEqual("foo bar", textPart.Text);
            textPart = (TextMessagePartModel) msg.MessageParts[1];
            Assert.AreEqual(" me", textPart.Text);
            Assert.IsTrue(textPart.IsHighlight);

            var msgA = new MessageModel();
            msgA.MessageParts.Add(new TextMessagePartModel(TextColor.Black, null, false, false, false, "foo"));
            msgA.MessageParts.Add(new TextMessagePartModel(TextColor.White, null, false, false, false, " bar"));
            var msgB = new MessageModel();
            msgB.TimeStamp = msgA.TimeStamp;
            msgB.MessageParts.Add(new TextMessagePartModel(TextColor.Black, null, false, false, false, "foo"));
            msgB.MessageParts.Add(new TextMessagePartModel(TextColor.White, null, false, false, false, " bar"));
            msgB.Compact();
            Assert.AreEqual(2, msg.MessageParts.Count);
            Assert.AreEqual(msgA, msgB);

            msg = new MessageModel();
            msg.MessageParts.Add(new TextMessagePartModel("foo "));
            msg.MessageParts.Add(new UrlMessagePartModel("http://foo.tld"));
            msg.MessageParts.Add(new UrlMessagePartModel("http://bar.tld"));
            msg.MessageParts.Add(new TextMessagePartModel(" bar", true));
            msg.MessageParts.Add(new TextMessagePartModel(" me", true));
            msg.MessageParts.Add(new TextMessagePartModel(" real"));
            msg.MessageParts.Add(new TextMessagePartModel(" good"));
            msg.Compact();
            Assert.AreEqual(5, msg.MessageParts.Count);

            Assert.IsInstanceOfType(typeof(TextMessagePartModel), msg.MessageParts[0]);
            textPart = (TextMessagePartModel) msg.MessageParts[0];
            Assert.AreEqual("foo ", textPart.Text);

            Assert.IsInstanceOfType(typeof(UrlMessagePartModel), msg.MessageParts[1]);
            var urlPart = (UrlMessagePartModel) msg.MessageParts[1];
            Assert.AreEqual("http://foo.tld", urlPart.Url);

            Assert.IsInstanceOfType(typeof(UrlMessagePartModel), msg.MessageParts[2]);
            urlPart = (UrlMessagePartModel) msg.MessageParts[2];
            Assert.AreEqual("http://bar.tld", urlPart.Url);

            Assert.IsInstanceOfType(typeof(TextMessagePartModel), msg.MessageParts[3]);
            textPart = (TextMessagePartModel) msg.MessageParts[3];
            Assert.AreEqual(" bar me", textPart.Text);
            Assert.IsTrue(textPart.IsHighlight);

            Assert.IsInstanceOfType(typeof(TextMessagePartModel), msg.MessageParts[4]);
            textPart = (TextMessagePartModel) msg.MessageParts[4];
            Assert.AreEqual(" real good", textPart.Text);
            Assert.IsFalse(textPart.IsHighlight);
        }

        [Test]
        public void CompactBenchmark()
        {
            var formatter = new BinaryFormatter();
            var topic = "Smuxi the IRC client for sophisticated users: http://smuxi.org/ | Smuxi 0.7.2.2 'Lovegood' released (2010-07-27) http://bit.ly/9nvsZF | FAQ: http://smuxi.org/faq/ | Deutsch? -> #smuxi.de | Español? -> #smuxi.es | Smuxi @ FOSDEM 2010 talk: http://bit.ly/anHJfm";
            var msg = new MessageModel(topic) {
                IsCompactable = false
            };

            var stream = new MemoryStream(1024);
            formatter.Serialize(stream, msg);
            Console.WriteLine("Parts: " + msg.MessageParts.Count);
            Console.WriteLine("Size: " + stream.Length);

            msg.Compact();
            stream = new MemoryStream(1024);
            formatter.Serialize(stream, msg);
            Console.WriteLine("Compacted Parts: " + msg.MessageParts.Count);
            Console.WriteLine("Compacted Size: " + stream.Length);


            // regular message without URL
            // <meebey> solange eine message aber keine url hat ist der vorteil nur gering (wenn ueberhaupt)
            msg = new MessageModel() {
                IsCompactable = false
            };
            msg.MessageParts.Add(new TextMessagePartModel("<"));
            msg.MessageParts.Add(
                new TextMessagePartModel(
                    TextColor.White, null, false, false, false, "meebey"
                )
            );
            msg.MessageParts.Add(new TextMessagePartModel("> "));
            msg.MessageParts.Add(
                new TextMessagePartModel(
                    "solange eine message aber keine url hat ist der " +
                    "vorteil nur gering (wenn ueberhaupt)"
                )
            );
            stream = new MemoryStream(1024);
            formatter.Serialize(stream, msg);
            Console.WriteLine("Parts: " + msg.MessageParts.Count);
            Console.WriteLine("Size: " + stream.Length);

            msg.Compact();
            stream = new MemoryStream(1024);
            formatter.Serialize(stream, msg);
            Console.WriteLine("Compacted Parts: " + msg.MessageParts.Count);
            Console.WriteLine("Compacted Size: " + stream.Length);


            // regular short message with URL
            // <meebey> http://www.smuxi.org/issues/show/107 kannst ja watchen
            msg = new MessageModel() {
                IsCompactable = false
            };
            msg.MessageParts.Add(new TextMessagePartModel("<"));
            msg.MessageParts.Add(
                new TextMessagePartModel(
                    TextColor.White, null, false, false, false, "meebey"
                )
            );
            msg.MessageParts.Add(new TextMessagePartModel("> "));
            msg.MessageParts.Add(
                new UrlMessagePartModel(
                    "http://www.smuxi.org/issues/show/107"
                )
            );
            msg.MessageParts.Add(new TextMessagePartModel(" kannst ja watchen"));

            stream = new MemoryStream(1024);
            formatter.Serialize(stream, msg);
            Console.WriteLine("Parts: " + msg.MessageParts.Count);
            Console.WriteLine("Size: " + stream.Length);

            msg.Compact();
            stream = new MemoryStream(1024);
            formatter.Serialize(stream, msg);
            Console.WriteLine("Compacted Parts: " + msg.MessageParts.Count);
            Console.WriteLine("Compacted Size: " + stream.Length);
        }
    }
}
