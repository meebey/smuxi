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

namespace Smuxi.Common
{
    [TestFixture]
    public class PatternTests
    {
        [Test]
        public void IsMatchExact()
        {
            Assert.IsTrue(Pattern.IsMatch("foo", "foo"),        "#1");
            Assert.IsFalse(Pattern.IsMatch("foo", "foobar"),    "#2");
            Assert.IsFalse(Pattern.IsMatch("foo", "barfoo"),    "#3");
            Assert.IsFalse(Pattern.IsMatch("foo", "barfoobar"), "#4");

            Assert.IsTrue(Pattern.IsMatch("",     ""),    "#5");
            Assert.IsFalse(Pattern.IsMatch("",    "foo"), "#6");
        }

        [Test]
        public void IsMatchGlobbing()
        {
            Assert.IsTrue(Pattern.IsMatch("foo",     "*foo"), "#1");
            Assert.IsTrue(Pattern.IsMatch("barfoo",  "*foo"), "#2");
            Assert.IsFalse(Pattern.IsMatch("foobar", "*foo"), "#3");
            Assert.IsFalse(Pattern.IsMatch("",       "*foo"), "#4");

            Assert.IsTrue(Pattern.IsMatch("foo",     "foo*"), "#5");
            Assert.IsTrue(Pattern.IsMatch("foobar",  "foo*"), "#6");
            Assert.IsFalse(Pattern.IsMatch("barfoo", "foo*"), "#7");
            Assert.IsFalse(Pattern.IsMatch("",       "foo*"), "#8");

            Assert.IsTrue(Pattern.IsMatch("foo",       "*foo*"), "#9");
            Assert.IsTrue(Pattern.IsMatch("barfoo",    "*foo*"), "#10");
            Assert.IsTrue(Pattern.IsMatch("foobar",    "*foo*"), "#11");
            Assert.IsTrue(Pattern.IsMatch("barfoobar", "*foo*"), "#12");
            Assert.IsFalse(Pattern.IsMatch("fo",       "*foo*"), "#13");
            Assert.IsFalse(Pattern.IsMatch("",         "*foo*"), "#14");

            Assert.IsTrue(Pattern.IsMatch("foo", "*"), "#15");
            Assert.IsTrue(Pattern.IsMatch("",    "*"), "#16");
        }

        [Test]
        public void IsMatchRegex()
        {
            Assert.IsTrue(Pattern.IsMatch("foo", "/foo/"),  "#1");
            Assert.IsTrue(Pattern.IsMatch("foo", "/^foo/"), "#2");
            Assert.IsTrue(Pattern.IsMatch("foo", "/foo$/"), "#3");
            Assert.IsTrue(Pattern.IsMatch("foo", "/.*/"),   "#4");
        }

        [Test]
        public void ContainsPatternCharacters()
        {
            Assert.IsTrue(Pattern.ContainsPatternCharacters("*foo"),  "#1");
            Assert.IsTrue(Pattern.ContainsPatternCharacters("foo*"),  "#2");
            Assert.IsTrue(Pattern.ContainsPatternCharacters("*foo*"), "#3");
            Assert.IsTrue(Pattern.ContainsPatternCharacters("*"),     "#4");
            Assert.IsTrue(Pattern.ContainsPatternCharacters("/foo/"), "#5");
            Assert.IsFalse(Pattern.ContainsPatternCharacters(""),     "#6");
            Assert.IsFalse(Pattern.ContainsPatternCharacters("foo"),  "#7");
            Assert.IsFalse(Pattern.ContainsPatternCharacters("/"),    "#8");
        }
    }
}
