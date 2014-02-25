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
using Smuxi.Engine.Dto;

namespace Smuxi.Engine
{
    [TestFixture]
    public class MessageModelTests
    {
        MessageModel SimpleMessage { get; set; }
        MessageModel ComplexMessage { get; set; }

        [TestFixtureSetUp]
        public void SetUp()
        {
            var builder = new MessageBuilder();
            builder.AppendIdendityName(
                new ContactModel("meeebey", "meebey", "netid", "netprot")
            );
            builder.AppendSpace();
            builder.AppendText("solange eine message aber keine url hat ist der vorteil nur gering (wenn ueberhaupt)");
            SimpleMessage = builder.ToMessage();

            var topic = "Smuxi the IRC client for sophisticated users: http://smuxi.org/ | Smuxi 0.7.2.2 'Lovegood' released (2010-07-27) http://bit.ly/9nvsZF | FAQ: http://smuxi.org/faq/ | Deutsch? -> #smuxi.de | Español? -> #smuxi.es | Smuxi @ FOSDEM 2010 talk: http://bit.ly/anHJfm";
            builder = new MessageBuilder();
            builder.AppendMessage(topic);
            ComplexMessage = builder.ToMessage();
        }

        [Test]
        public void Equals()
        {
            var msg = new MessageModel("test");
            Assert.IsFalse(msg.Equals(null));
            msg = new MessageModel();
            Assert.IsFalse(msg.Equals(null));
            Assert.IsFalse(msg == null);

            msg = new MessageModel(SimpleMessage);
            Assert.IsTrue(msg.Equals(SimpleMessage));
            var textPart = (TextMessagePartModel) msg.MessageParts[0];
            textPart.ForegroundColor = TextColor.Grey;
            Assert.IsFalse(msg.Equals(SimpleMessage));
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
        public void LameCopyConstructor()
        {
            var copiedMsg = new MessageModel(SimpleMessage);

            Assert.AreNotSame(SimpleMessage, copiedMsg);
            Assert.IsNotNull(copiedMsg.MessageParts);
            Assert.AreNotSame(SimpleMessage.MessageParts, copiedMsg.MessageParts);
            Assert.AreEqual(SimpleMessage, copiedMsg);
        }

        [Test]
        [Explicit]
        public void LameCopyConstructorBenchmark()
        {
            int runs = 50000;
            DateTime start, stop;

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var copiedMsg = new MessageModel(SimpleMessage);
            }
            stop = DateTime.UtcNow;
            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "Ctor(Simple): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var copiedMsg = new MessageModel(ComplexMessage);
            }
            stop = DateTime.UtcNow;
            total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "Ctor(Complex): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );
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
        [Explicit]
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

        [Test]
        [Explicit]
        public void BinarySerializeDeserializeBenchmark()
        {
        }

        [Test]
        public void ServiceStackJsonSerialize()
        {
            //ServiceStack.Text.JsConfig<TextColor>.SerializeFn = color => color.ToString();
            ServiceStack.Text.JsConfig<MessagePartModel>.ExcludeTypeInfo = true;

            ComplexMessage.TimeStamp = DateTime.Parse("2012-01-01T00:00:00Z").ToUniversalTime();
            var json = ServiceStack.Text.JsonSerializer.SerializeToString(ComplexMessage);
            //var json = ServiceStack.Text.TypeSerializer.SerializeAndFormat(TestMessage);
            //Console.WriteLine(json);
            Console.WriteLine(ServiceStack.Text.JsvFormatter.Format(json));
            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);
            Assert.AreEqual(@"{""TimeStamp"":""\/Date(1325376000000)\/"",""MessageParts"":[{""Type"":""Text"",""ForegroundColor"":{""Value"":-1},""BackgroundColor"":{""Value"":-1},""Underline"":false,""Bold"":false,""Italic"":false,""Text"":""Smuxi the IRC client for sophisticated users: "",""IsHighlight"":false},{""Type"":""URL"",""Url"":""http://smuxi.org/"",""Protocol"":""Http"",""ForegroundColor"":{""Value"":-1},""BackgroundColor"":{""Value"":-1},""Underline"":false,""Bold"":false,""Italic"":false,""IsHighlight"":false},{""Type"":""Text"",""ForegroundColor"":{""Value"":-1},""BackgroundColor"":{""Value"":-1},""Underline"":false,""Bold"":false,""Italic"":false,""Text"":"" | Smuxi 0.7.2.2 'Lovegood' released (2010-07-27) "",""IsHighlight"":false},{""Type"":""URL"",""Url"":""http://bit.ly/9nvsZF"",""Protocol"":""Http"",""ForegroundColor"":{""Value"":-1},""BackgroundColor"":{""Value"":-1},""Underline"":false,""Bold"":false,""Italic"":false,""IsHighlight"":false},{""Type"":""Text"",""ForegroundColor"":{""Value"":-1},""BackgroundColor"":{""Value"":-1},""Underline"":false,""Bold"":false,""Italic"":false,""Text"":"" | FAQ: "",""IsHighlight"":false},{""Type"":""URL"",""Url"":""http://smuxi.org/faq/"",""Protocol"":""Http"",""ForegroundColor"":{""Value"":-1},""BackgroundColor"":{""Value"":-1},""Underline"":false,""Bold"":false,""Italic"":false,""IsHighlight"":false},{""Type"":""Text"",""ForegroundColor"":{""Value"":-1},""BackgroundColor"":{""Value"":-1},""Underline"":false,""Bold"":false,""Italic"":false,""Text"":"" | Deutsch? -> #smuxi.de | Español? -> #smuxi.es | Smuxi @ FOSDEM 2010 talk: "",""IsHighlight"":false},{""Type"":""URL"",""Url"":""http://bit.ly/anHJfm"",""Protocol"":""Http"",""ForegroundColor"":{""Value"":-1},""BackgroundColor"":{""Value"":-1},""Underline"":false,""Bold"":false,""Italic"":false,""IsHighlight"":false},{""Type"":""Text"",""ForegroundColor"":{""Value"":-1},""BackgroundColor"":{""Value"":-1},""Underline"":false,""Bold"":false,""Italic"":false,""Text"":"" "",""IsHighlight"":false}],""MessageType"":""Normal""}",
                            json);
        }

        [Test]
        [Explicit]
        public void ServiceStackJsonSerializeBenchmark()
        {
            ServiceStack.Text.JsConfig<MessagePartModel>.ExcludeTypeInfo = true;

            int runs = 50000;
            DateTime start, stop;

            MessageModel msg = null;
            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var json = ServiceStack.Text.JsonSerializer.SerializeToString(SimpleMessage);
            }
            stop = DateTime.UtcNow;
            //Assert.AreEqual(ComplexMessage, msg);
            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "Serialize(Simple): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var json = ServiceStack.Text.JsonSerializer.SerializeToString(ComplexMessage);
            }
            stop = DateTime.UtcNow;
            //Assert.AreEqual(ComplexMessage, msg);
            total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "Serialize(Complex): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );
        }

        [Test]
        [Explicit]
        public void ServiceStackJsonSerializeDeserializeBenchmark()
        {
            ServiceStack.Text.JsConfig<MessagePartModel>.ExcludeTypeInfo = true;

            int runs = 50000;
            DateTime start, stop;
            MessageModel msg = null;

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var json = ServiceStack.Text.JsonSerializer.SerializeToString(SimpleMessage);
                var dtoMsg = ServiceStack.Text.JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
                msg = dtoMsg.ToMessage();
            }
            stop = DateTime.UtcNow;
            Assert.AreEqual(SimpleMessage, msg);
            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "ServiceStackJsonSerialize+Deserialize(Simple): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var json = ServiceStack.Text.JsonSerializer.SerializeToString(ComplexMessage);
                var dtoMsg = ServiceStack.Text.JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
                msg = dtoMsg.ToMessage();
            }
            stop = DateTime.UtcNow;
            Assert.AreEqual(ComplexMessage, msg);
            total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "ServiceStackJsonSerialize+Deserialize(Complex): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );
        }

        [Test]
        public void NewtonsoftJsonSerialize()
        {
            var serializer = new Newtonsoft.Json.JsonSerializer() {
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            };
            var writer = new StringWriter();
            serializer.Serialize(writer, ComplexMessage);
            Console.WriteLine(writer.ToString());
        }

        [Test]
        [Explicit]
        public void NewtonsoftJsonSerializeBenchmark()
        {
            var serializer = new Newtonsoft.Json.JsonSerializer() {
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            };

            int runs = 50000;
            DateTime start, stop;

            MessageModel msg = null;
            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var writer = new StringWriter();
                serializer.Serialize(writer, SimpleMessage);
                var json = writer.ToString();
            }
            stop = DateTime.UtcNow;
            Assert.AreEqual(ComplexMessage, msg);
            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "NewtonsoftJsonSerialize(SimpleMessage): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var writer = new StringWriter();
                serializer.Serialize(writer, ComplexMessage);
            }
            stop = DateTime.UtcNow;
            Assert.AreEqual(ComplexMessage, msg);
            total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "NewtonsoftJsonSerialize(ComplexMessage): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );
        }

        [Test]
        [Explicit]
        public void NewtonsoftJsonSerializeDeserializeBenchmark()
        {
            var serializer = new Newtonsoft.Json.JsonSerializer() {
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            };

            int runs = 50000;
            DateTime start, stop;

            MessageModel msg = null;
            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var writer = new StringWriter();
                serializer.Serialize(writer, SimpleMessage);
                var reader = new StringReader(writer.ToString());
                var jsonReader = new Newtonsoft.Json.JsonTextReader(reader);
                var dtoMsg = serializer.Deserialize<MessageDtoModelV1>(jsonReader);
            }
            stop = DateTime.UtcNow;
            Assert.AreEqual(ComplexMessage, msg);
            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "NewtonsoftJsonSerialize+Deserialize(Simple): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                var writer = new StringWriter();
                serializer.Serialize(writer, ComplexMessage);
                var reader = new StringReader(writer.ToString());
                var jsonReader = new Newtonsoft.Json.JsonTextReader(reader);
                var dtoMsg = serializer.Deserialize<MessageDtoModelV1>(jsonReader);
            }
            stop = DateTime.UtcNow;
            Assert.AreEqual(ComplexMessage, msg);
            total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "NewtonsoftJsonSerialize+Deserialize(ComplexMessage): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );
        }
    }
}
