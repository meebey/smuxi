/*
 * $Id: Entry.cs 216 2007-11-05 22:56:57Z meebey $
 * $URL: svn+ssh://SmuxiSVN/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/Entry.cs $
 * $Rev: 216 $
 * $Author: meebey $
 * $Date: 2007-11-05 17:56:57 -0500 (Mon, 05 Nov 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System;
using System.Drawing;
using System.Globalization;

using Smuxi.Engine;

namespace Smuxi.Frontend.Swf
{
    public static class ColorTools
    {
        public static Color GetColor(TextColor textColor)
        {
            return GetColor(textColor.HexCode); 
        }
        
        public static Color GetColor(string hexcode)
        {
            if (hexcode.StartsWith("#")) {
                // remove leading "#" character
                hexcode = hexcode.Substring(1);
            }
            
            int red   = Int16.Parse(hexcode.Substring(0, 2), NumberStyles.HexNumber);
            int green = Int16.Parse(hexcode.Substring(2, 2), NumberStyles.HexNumber);
            int blue  = Int16.Parse(hexcode.Substring(4, 2), NumberStyles.HexNumber);
            return Color.FromArgb(red, green, blue);
        }

        public static Color GetColor(int value)
        {
            // value may or may not have the alpha section set
            // thus we need to be sure it's filled.  Bitwise
            // OR on the 8 most significant bits does just
            // this.
            //return Color.FromArgb(value | (int)0xFF000000);
            return Color.Empty;
        }
    }
}
