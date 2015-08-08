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
using NUnit.Framework;
using System.Threading;

namespace Smuxi.Common
{
    [TestFixture]
    public class RateLimiterTests
    {
        [Test]
        public void AboveLimit()
        {
            var limiter = new RateLimiter(10, TimeSpan.FromSeconds(10));
            for (int i = 0; i < 100; i++) {
                if (limiter.IsRateLimited) {
                    Assert.AreEqual(10, i);
                    break;
                }
                limiter++;
            }
        }

        [Test]
        public void BelowLimit()
        {
            var limiter = new RateLimiter(10, TimeSpan.FromMilliseconds(10));
            for (int i = 0; i < 100; i++) {
                if (limiter.IsRateLimited) {
                    break;
                }
                limiter++;
            }
            Thread.Sleep(10);
            Assert.IsFalse(limiter.IsRateLimited);
        }
    }
}

