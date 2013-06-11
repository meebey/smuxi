// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2011 Mirco Bauer
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
using Stfl;

namespace Smuxi.Frontend.Stfl
{
    [TestFixture]
    public class TextViewTests
    {
        [Test]
        public void WrapLine()
        {
            List<string> wrappedLine;

            // unchanged if window is wide enough
            wrappedLine = TextView.WrapLine("foobar", 10);
            Assert.AreEqual(1, wrappedLine.Count);
            Assert.AreEqual("foobar", wrappedLine[0]);

            wrappedLine = TextView.WrapLine("foobar", 6);
            Assert.AreEqual(1, wrappedLine.Count);
            Assert.AreEqual("foobar", wrappedLine[0]);

            // break in the middle of words if necessary
            wrappedLine = TextView.WrapLine("foobar", 4);
            Assert.AreEqual(2, wrappedLine.Count);
            Assert.AreEqual("foob", wrappedLine[0]);
            Assert.AreEqual("ar",   wrappedLine[1]);

            // break between words if possible ("foob ar me", not "foob arm e")
            wrappedLine = TextView.WrapLine("foobar me", 4);
            Assert.AreEqual(3, wrappedLine.Count);
            Assert.AreEqual("foob", wrappedLine[0]);
            Assert.AreEqual("ar", wrappedLine[1]);
            Assert.AreEqual("me", wrappedLine[2]);

            wrappedLine = TextView.WrapLine("foobar me", 5);
            Assert.AreEqual(2, wrappedLine.Count);
            Assert.AreEqual("fooba", wrappedLine[0]);
            Assert.AreEqual("r me", wrappedLine[1]);

            // handle formatting rationally
            wrappedLine = TextView.WrapLine("<b>foobar</b>", 20);
            Assert.AreEqual(1, wrappedLine.Count);
            Assert.AreEqual("<b>foobar</b>", wrappedLine[0]);

            wrappedLine = TextView.WrapLine("<b>foobar</b>", 6);
            Assert.AreEqual(1, wrappedLine.Count);
            Assert.AreEqual("<b>foobar</b>", wrappedLine[0]);

            wrappedLine = TextView.WrapLine("foo <b>bar</b> me", 6);
            Assert.AreEqual(2, wrappedLine.Count);
            Assert.AreEqual("foo", wrappedLine[0]);
            Assert.AreEqual("<b>bar</b> me", wrappedLine[1]);

            wrappedLine = TextView.WrapLine("foo <b>bar</b> me", 7);
            Assert.AreEqual(2, wrappedLine.Count);
            Assert.AreEqual("foo <b>bar</b>", wrappedLine[0]);
            Assert.AreEqual("me", wrappedLine[1]);

            wrappedLine = TextView.WrapLine("foo <b>bar </b>me", 7);
            Assert.AreEqual(2, wrappedLine.Count);
            Assert.AreEqual("foo <b>bar</>", wrappedLine[0]);
            Assert.AreEqual("</b>me", wrappedLine[1]);

            // handle escaped opening angled brackets correctly
            wrappedLine = TextView.WrapLine("I <b><>3</b> you", 4);
            Assert.AreEqual(2, wrappedLine.Count);
            Assert.AreEqual("I <b><>3</b>", wrappedLine[0]);
            Assert.AreEqual("you", wrappedLine[1]);

            wrappedLine = TextView.WrapLine("1<b><></b>23", 1);
            Assert.AreEqual(4, wrappedLine.Count);
            Assert.AreEqual("1", wrappedLine[0]);
            Assert.AreEqual("<b><></>", wrappedLine[1]);
            Assert.AreEqual("</b>2", wrappedLine[2]);
            Assert.AreEqual("3", wrappedLine[3]);
        }
    }
}
