// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2009 Mirco Bauer <meebey@meebey.net>
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
    public class TextColorTests
    {
        [Test]
        public void ToStringPerformance()
        {
            int runs = 1000;
            var color = new TextColor(0, 0, 0);
            DateTime start, stop;

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                color.ToString();
            }
            stop = DateTime.UtcNow;

            Console.WriteLine(
                "ToString(): avg: {0:0.00} ms",
                (stop - start).TotalMilliseconds / runs
            );
        }

        [Test]
        public void Equals()
        {
            TextColor a, b;

            a = TextColor.None;
            b = TextColor.None;
            Assert.IsTrue(a == b);

            a = TextColor.Black;
            b = TextColor.Black;
            Assert.IsTrue(a == b);

            a = TextColor.None;
            b = TextColor.Black;
            Assert.IsFalse(a == b);

            a = TextColor.None;
            b = TextColor.White;
            Assert.IsFalse(a == b);

            a = TextColor.Black;
            b = TextColor.White;
            Assert.IsFalse(a == b);

            a = TextColor.White;
            b = TextColor.White;
            Assert.IsTrue(a == b);
        }
    }
}
