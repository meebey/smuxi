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
using System.Collections.Generic;

namespace Smuxi.Engine
{
    public static class TextColorPalettes
    {
        public static List<TextColor> LinuxConsole { get; set; }
        public static List<TextColor> Xterm { get; set; }

        static TextColorPalettes() {
            var hexColors = new string[] {
                "#000000",
                "#800000",
                "#008000",
                "#808000",
                "#000080",
                "#800080",
                "#008080",
                "#c0c0c0",
                "#808080",
                "#ff0000",
                "#00ff00",
                "#ffff00",
                "#0000ff",
                "#ff00ff",
                "#00ffff",
                "#ffffff"
            };
            LinuxConsole = new List<TextColor>(16);
            foreach (var hexColor in hexColors) {
                LinuxConsole.Add(TextColor.Parse(hexColor));
            }

            Xterm = new List<TextColor>(256);
            Xterm.AddRange(LinuxConsole);
            // TODO: add all xterm colors
            // http://www.calmar.ws/vim/256-xterm-24bit-rgb-color-chart.html
        }
    }
}
