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
using System.Collections.Generic;
using NUnit.Framework;

namespace Smuxi.Engine
{
    [TestFixture]
    public class TextColorToolsTests
    {
        [Test]
        public void GetBestTextColorPerformance()
        {
            var colors = new List<TextColor>();
            colors.Add(TextColor.Parse("#000000"));
            colors.Add(TextColor.Parse("#000000"));
            colors.Add(TextColor.Parse("#FF0000"));
            colors.Add(TextColor.Parse("#00FF00"));
            colors.Add(TextColor.Parse("#0000FF"));
            colors.Add(TextColor.Parse("#FF00FF"));
            colors.Add(TextColor.Parse("#FFFF00"));
            colors.Add(TextColor.Parse("#FFFFFF"));
            colors.Add(TextColor.Parse("#1E0DD6"));
            colors.Add(TextColor.Parse("#1E0DD6"));
            colors.Add(TextColor.Parse("#219207"));
            colors.Add(TextColor.Parse("#429FB0"));
            colors.Add(TextColor.Parse("#352878"));
            colors.Add(TextColor.Parse("#52248B"));
            colors.Add(TextColor.Parse("#603D40"));
            colors.Add(TextColor.Parse("#872F56"));
            colors.Add(TextColor.Parse("#97608C"));
            colors.Add(TextColor.Parse("#055A4F"));
            colors.Add(TextColor.Parse("#05730C"));
            colors.Add(TextColor.Parse("#A45DDA"));
            colors.Add(TextColor.Parse("#279C2A"));
            colors.Add(TextColor.Parse("#D24F81"));
            colors.Add(TextColor.Parse("#45D6FA"));
            colors.Add(TextColor.Parse("#31DD0B"));
            colors.Add(TextColor.Parse("#429FB0"));
            colors.Add(TextColor.Parse("#05FC8F"));
            colors.Add(TextColor.Parse("#C1FFEF"));
            colors.Add(TextColor.Parse("#C1FFEF"));
            colors.Add(TextColor.Parse("#E4DA22"));

            var colorCombinations = new List<KeyValuePair<TextColor, TextColor>>();
            // bright background
            var bgBright = TextColor.Parse("#EBEBEB");
            foreach (var color in colors) {
                colorCombinations.Add(
                    new KeyValuePair<TextColor, TextColor>(color, bgBright)
                );
            }

            // dark background
            var bgDark   = TextColor.Parse("#2E3436");
            foreach (var color in colors) {
                colorCombinations.Add(
                    new KeyValuePair<TextColor, TextColor>(color, bgDark)
                );
            }

            // warmup the TextColorTools cache (trigger static ctors)
            TextColorTools.GetBestTextColor(TextColor.Black, TextColor.Black);

            DateTime dstart = DateTime.UtcNow;
            DateTime dstop = DateTime.UtcNow;
            Console.WriteLine("DateTime took: " + (dstop - dstart).TotalMilliseconds + " ms");
            int i = 0;
            foreach (var colorCombination in colorCombinations) {
                DateTime start, stop;
                start = DateTime.UtcNow;
                var best = TextColorTools.GetBestTextColor(
                    colorCombination.Key, colorCombination.Value
                );
                stop = DateTime.UtcNow;
                Console.WriteLine(
                    "GetBestTextColor(): #{0:00} {1}|{2}={3}  took: {4:0.00} ms",
                    i++, colorCombination.Key, colorCombination.Value, best,
                    (stop - start).TotalMilliseconds
                );
            }
        }

        [Test]
        public void GetNearestColor()
        {
            var palette = new List<TextColor>();
            palette.Add(TextColor.Parse("#000000"));
            palette.Add(TextColor.Parse("#FF0000"));
            palette.Add(TextColor.Parse("#00FF00"));
            palette.Add(TextColor.Parse("#0000FF"));
            palette.Add(TextColor.Parse("#FF00FF"));
            palette.Add(TextColor.Parse("#FFFF00"));
            palette.Add(TextColor.Parse("#FFFFFF"));

            var expected = TextColor.Parse("#FF0000");
            var actual = TextColorTools.GetNearestColor(TextColor.Parse("#FF1111"), palette);
            Assert.AreEqual(expected, actual);

            expected = TextColor.Parse("#FFFFFF");
            actual = TextColorTools.GetNearestColor(TextColor.Parse("#FF9999"), palette);
            Assert.AreEqual(expected, actual);
        }
    }
}
