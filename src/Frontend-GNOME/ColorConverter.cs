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
using System.Globalization;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public static class ColorConverter
    {
        public static string GetHexCode(Gdk.Color color)
        {
            /*
            // this approach is changing the color instead of converting it, as byte wraps
            string hexcode = String.Format("{0}{1}{2}",
                                           ((byte) color.Red).ToString("X2"),
                                           ((byte) color.Green).ToString("X2"),
                                           ((byte) color.Blue).ToString("X2"));
            */
            string hexcode = String.Format("#{0}{1}{2}",
                                           (color.Red >> 8).ToString("X2"),
                                           (color.Green >> 8).ToString("X2"),
                                           (color.Blue >> 8).ToString("X2"));
            return hexcode;
        }

        public static Gdk.Color GetGdkColor(string hexCode)
        {
            if (hexCode == null) {
                throw new ArgumentNullException("hexCode");
            }

            var color = TextColor.Parse(hexCode);
            return new Gdk.Color(color.Red, color.Green, color.Blue);
        }

        public static TextColor GetTextColor(Gdk.Color color)
        {
            string hexcode = GetHexCode(color);
            // remove leading "#" character
            hexcode = hexcode.Substring(1);
            int value  = Int32.Parse(hexcode, NumberStyles.HexNumber);
            return new TextColor(value);
        }

        public static Gdk.Color GetGdkColor(TextColor textColor)
        {
            if (textColor == null) {
                throw new ArgumentNullException("textColor");
            }

            return GetGdkColor(textColor.HexCode);
        }
    }
}
