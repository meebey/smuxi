// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012, 2014 Mirco Bauer <meebey@meebey.net>
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
using ServiceStack.Text;
using Smuxi.Engine.Dto;

namespace Smuxi.Engine
{
    [TestFixture]
    public class MessageDtoModelV1Tests
    {
        MessageModel SimpleMessage { get; set; }
        MessageModel ComplexMessage { get; set; }
        MessageDtoModelV1 SimpleMessageDtoV1 { get; set; }
        MessageDtoModelV1 ComplexMessageDtoV1 { get; set; }
        string SimpleMessageJson { get; set; }
        string ComplexMessageJson { get; set; }

        [TestFixtureSetUp]
        public void SetUp()
        {
            JsConfig<MessagePartModel>.ExcludeTypeInfo = true;

            var builder = new MessageBuilder();
            builder.AppendSenderPrefix(
                new ContactModel("meeebey", "meebey", "netid", "netprot")
            );
            builder.AppendText("solange eine message aber keine url hat ist der vorteil nur gering (wenn ueberhaupt)");
            SimpleMessage = builder.ToMessage();
            SimpleMessageJson = JsonSerializer.SerializeToString(SimpleMessage);
            SimpleMessageDtoV1 = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(SimpleMessageJson);

            var topic = "Smuxi the IRC client for sophisticated users: http://smuxi.org/ | Smuxi 0.7.2.2 'Lovegood' released (2010-07-27) http://bit.ly/9nvsZF | FAQ: http://smuxi.org/faq/ | Deutsch? -> #smuxi.de | Español? -> #smuxi.es | Smuxi @ FOSDEM 2010 talk: http://bit.ly/anHJfm";
            builder = new MessageBuilder();
            builder.AppendMessage(topic);
            builder.AppendText(" ");
            builder.AppendUrl("https://www.smuxi.org/issues/show/428", "smuxi#428");
            ComplexMessage = builder.ToMessage();
            ComplexMessageJson = JsonSerializer.SerializeToString(ComplexMessage);
            ComplexMessageDtoV1 = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(ComplexMessageJson);
        }

        [Test]
        public void SerializeDeserializeSimpleMessage()
        {
            var dtoMsg = new MessageDtoModelV1(SimpleMessage);
            var json = JsonSerializer.SerializeToString(dtoMsg);
            var dtoMsg2 = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
            Assert.AreEqual(dtoMsg.ToMessage(), dtoMsg2.ToMessage());
            Assert.AreEqual(SimpleMessage, dtoMsg.ToMessage());
            Assert.AreEqual(SimpleMessage, dtoMsg2.ToMessage());
        }

        [Test]
        public void SerializeDeserializeComplexMessage()
        {
            var dtoMsg = new MessageDtoModelV1(ComplexMessage);
            var json = JsonSerializer.SerializeToString(dtoMsg);
            var dtoMsg2 = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
            Assert.AreEqual(dtoMsg.ToMessage(), dtoMsg2.ToMessage());
            Assert.AreEqual(ComplexMessage, dtoMsg.ToMessage());
            Assert.AreEqual(ComplexMessage, dtoMsg2.ToMessage());
        }

        [Test]
        public void ToMessageBenchmark()
        {
            int runs = 50000;
            DateTime start, stop;

            start = DateTime.UtcNow;
            MessageModel msg = null;
            for (int i = 0; i < runs; i++) {
                msg = SimpleMessageDtoV1.ToMessage();
            }
            stop = DateTime.UtcNow;
            var total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "SimpleMessageDtoV1.ToMessage(): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                msg = ComplexMessageDtoV1.ToMessage();
            }
            stop = DateTime.UtcNow;
            total = (stop - start).TotalMilliseconds;
            Console.WriteLine(
                "ComplexMessageDtoV1.ToMessage(): avg: {0:0.00} ms runs: {1} took: {2:0.00} ms",
                total / runs,
                runs,
                total
            );
        }
    }
}
