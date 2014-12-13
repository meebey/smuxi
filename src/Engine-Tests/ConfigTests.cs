// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 Mirco Bauer <meebey@meebey.net>
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
    [TestFixture]
    public class ConfigTests
    {
        [Test]
        public void Indexer()
        {
            var config = new Config();
            config["Server/Port"] = 123;
            Assert.AreEqual(123, (int) config["Server/Port"]);
        }

        [Test]
        public void ConfigOverridesIntValue()
        {
            var config = new Config();
            var overrides = config.Overrides;
            overrides.Add("Server/Port", 7689);

            config["Server/Port"] = 123;
            Assert.AreEqual(7689, (int) config["Server/Port"]);
        }

        [Test]
        public void ConfigOverridesRegexKey()
        {
            var config = new Config();
            var overrides = config.Overrides;
            overrides.Add("/Engine/Users/.*/MessageBuffer/PersistencyType/", "PersistentSqlite");

            config["Engine/Users/local/MessageBuffer/PersistencyType"] = "Volatile";
            Assert.AreEqual("PersistentSqlite", (string) config["Engine/Users/local/MessageBuffer/PersistencyType"]);
        }
    }
}
