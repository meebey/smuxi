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
using System.Diagnostics;
using NUnit.Framework;

namespace Smuxi.Common
{
    [TestFixture]
    public class TraceTests
    {
        [Test]
        public void CallPerformance()
        {
            int runs = 1000;
            DateTime start, stop;

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                Trace.Call();
            }
            stop = DateTime.UtcNow;

            Console.WriteLine(
                "Trace.Call(): avg: {0:0.00} ms",
                (stop - start).TotalMilliseconds / runs
            );
        }

        [Test]
        public void GetMethodBasePerformance()
        {
            int runs = 1000;
            DateTime start, stop;

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                Trace.GetMethodBase();
            }
            stop = DateTime.UtcNow;

            Console.WriteLine(
                "Trace.GetMethodBase(): avg: {0:0.00} ms",
                (stop - start).TotalMilliseconds / runs
            );
        }

        [Test]
        public void NewStackTracePerformance()
        {
            int runs = 1000;
            DateTime start, stop;

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                new StackTrace();
            }
            stop = DateTime.UtcNow;

            Console.WriteLine(
                "new StackTrace(): avg: {0:0.00} ms",
                (stop - start).TotalMilliseconds / runs
            );
        }

        [Test]
        public void NewStackTraceFrame1Performance()
        {
            int runs = 1000;
            DateTime start, stop;

            start = DateTime.UtcNow;
            for (int i = 0; i < runs; i++) {
                new StackTrace(new StackFrame(1));
            }
            stop = DateTime.UtcNow;

            Console.WriteLine(
                "new StackTrace(new StackFrame(1)): avg: {0:0.00} ms",
                (stop - start).TotalMilliseconds / runs
            );
        }
    }
}
