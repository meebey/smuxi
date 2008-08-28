/*
 * $Id: ChannelPage.cs 138 2006-12-23 17:11:57Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/ChannelPage.cs $
 * $Rev: 138 $
 * $Author: meebey $
 * $Date: 2006-12-23 18:11:57 +0100 (Sat, 23 Dec 2006) $
 *
 * smuxi - Smart MUltipleXed Irc
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
using System.Globalization;

using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public static class ColorTools
    {
        public static string GetHexCodeColor(Gdk.Color color)
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
        
        public static TextColor GetTextColor(Gdk.Color color)
        {
            string hexcode = GetHexCodeColor(color);
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
        
        public static Gdk.Color GetGdkColor(string hexCode)
        {
            Trace.Call(hexCode);
            
            if (hexCode == null) {
                throw new ArgumentNullException("hexcode");
            }
            
            if (hexCode.StartsWith("#")) {
                // remove leading "#" character
                hexCode = hexCode.Substring(1);
            }
            
            if (hexCode.Length != 6) {
                throw new ArgumentException("Hexcode value must be exact 6 characters long (without prefix).", "hexCode");
            }
            
            int red   = Int16.Parse(hexCode.Substring(0, 2), NumberStyles.HexNumber);
            int green = Int16.Parse(hexCode.Substring(2, 2), NumberStyles.HexNumber);
            int blue  = Int16.Parse(hexCode.Substring(4, 2), NumberStyles.HexNumber);
            return new Gdk.Color((byte)red, (byte)green, (byte)blue);
        }
        
        /*
        public static TextColor GetBestTextColor(TextColor fgColor, TextColor bgColor)
        {
            if (fgColor == null) {
                throw new ArgumentNullException("fgColor");
            }
            if (bgColor == null) {
                throw new ArgumentNullException("bgColor");
            }
            
            int bestColor  = fgColor.Value;
            int minDiff    = int.Parse("303030", NumberStyles.HexNumber); // Min difference
            int maxHex     = int.Parse("FFFFFF", NumberStyles.HexNumber); // White, so we don't get higher.
            int fgIntColor = fgColor.Value;
            int bgIntColor = bgColor.Value;
            
            int lowerDiff;
            if (bgIntColor - minDiff > 0) { // If the bgColor - the difference is still less than black...
                lowerDiff = bgIntColor - minDiff; // ... set lower diff to the value.
            } else { // else set it to black.
                lowerDiff = 0;
            }
            
            int upperDiff;
            if (bgIntColor + minDiff < maxHex) { // If the bgColor + the difference is still less than white...
                upperDiff = bgIntColor + minDiff; // ... set the upper diff to the value.
            } else { // Else set it to white.
                upperDiff = maxHex;
            }
            
            if (fgIntColor > lowerDiff && fgIntColor < upperDiff) { // If the foreground color is within the range of the minimum accepted difference...
                bestColor = maxHex - fgIntColor; // ... invert the color.
                if (bestColor > lowerDiff && bestColor < upperDiff) { // If it happens that it's still within the range after the inversion...
                    if (bestColor < bgIntColor ) { // ... see if it's bigger or smaller than the background color...
                        bestColor = fgIntColor - minDiff; // ... so we can either substract the difference ...
                    } else  {
                        bestColor = fgIntColor + minDiff; // ... or add the difference.
                    }
                }
            }
            
            // cap color to allowed values
            if (bestColor < 0) {
                bestColor = 0;
            }
            if (bestColor > maxHex) {
                bestColor = maxHex;
            }
            
            return new TextColor(bestColor);
        }
        */
        
        public static TextColor GetBestTextColor(TextColor fgColor, TextColor bgColor)
        {
            if (fgColor == null) {
                throw new ArgumentNullException("fgColor");
            }
            if (bgColor == null) {
                throw new ArgumentNullException("bgColor");
            }
            
            int[] fgColors   = { fgColor.Red, fgColor.Green, fgColor.Blue  };
            int[] bgColors   = { bgColor.Red, bgColor.Green, bgColor.Blue };
            int[] bestColors = new int[3];

            for (int i = 0; i < 3; i++ ) {
                bestColors[i] = (Math.Abs(bgColors[i] - fgColors[i]) < 0x40) ?
                                fgColors[i] | 0x80 : fgColors[i];
            }
            
            return new TextColor((byte) bestColors[0], (byte) bestColors[1], (byte) bestColors[2]);
        }
    }
}
