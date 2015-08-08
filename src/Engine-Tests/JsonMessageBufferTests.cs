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
using System.IO;
using NUnit.Framework;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [TestFixture]
    public class JsonMessageBufferTests : MessageBufferTestsBase
    {
        protected override IMessageBuffer CreateBuffer()
        {
            var dbPath = Path.Combine(Platform.GetBuffersPath("testuser"),
                                      "testprot");
            dbPath = Path.Combine(dbPath, "testnet");
            dbPath = Path.Combine(dbPath, "testchat.v1.json");
            if (Directory.Exists(dbPath)) {
                Directory.Delete(dbPath, true);
            }

            return OpenBuffer();
        }

        protected override IMessageBuffer OpenBuffer()
        {
            return new JsonMessageBuffer("testuser", "testprot", "testnet", "testchat");
        }

        [Test]
        public void GetChunkFileName()
        {
            var buffer = (JsonMessageBuffer) Buffer;
            Assert.AreEqual("0-999.json", buffer.GetChunkFileName(0L));
            Assert.AreEqual("0-999.json", buffer.GetChunkFileName(1L));
            Assert.AreEqual("0-999.json", buffer.GetChunkFileName(999L));
            Assert.AreEqual("1000-1999.json", buffer.GetChunkFileName(1000L));
            Assert.AreEqual("1000-1999.json", buffer.GetChunkFileName(1999L));
            Assert.AreEqual("2000-2999.json", buffer.GetChunkFileName(2500L));
            Assert.AreEqual("9223372036854774000-9223372036854774999.json", buffer.GetChunkFileName(Int64.MaxValue - 900));
        }

        [Test]
        [ExpectedException(typeof(OverflowException))]
        public void GetChunkFileNameMaxValue()
        {
            var buffer = (JsonMessageBuffer) Buffer;
            buffer.GetChunkFileName(Int64.MaxValue);
        }
    }
}
