/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using System.Globalization;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public static class TextColorTools
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static Dictionary<int, TextColor> f_BestContrastColors;

        static TextColorTools()
        {
            f_BestContrastColors = new Dictionary<int, TextColor>(1024);
        }

        public static TextColor GetBestTextColor(TextColor fgColor, TextColor bgColor)
        {
            return GetBestTextColor(fgColor, bgColor, TextColorContrast.Medium);
        }

        public static TextColor GetBestTextColor(TextColor fgColor,
                                                 TextColor bgColor,
                                                 TextColorContrast neededContrast)
        {
            if (fgColor == null) {
                throw new ArgumentNullException("fgColor");
            }
            if (bgColor == null) {
                throw new ArgumentNullException("bgColor");
            }

            TextColor bestColor;
            int key = fgColor.Value ^ bgColor.Value ^ (int) neededContrast;
            if (f_BestContrastColors.TryGetValue(key, out bestColor)) {
                return bestColor;
            }

            double brDiff = GetBritnessDifference(bgColor, TextColor.White);
            int modifier = 0;
            // for bright backgrounds we need to go from bright to dark colors
            // for better contrast and for dark backgrounds the opposite
            if (brDiff < 127) {
                // bright background
                modifier = -10;
            } else {
                // dark background
                modifier = 10;
            }

            double lastDifference = 0;
            bestColor = fgColor;
            int attempts = 1;
            while (true) {
                double difference = GetLuminanceDifference(bestColor, bgColor);
                double needed = ((int) neededContrast) / 10d;
                if (difference > needed) {
                    break;
                }

#if LOG4NET && COLOR_DEBUG
                f_Logger.Debug("GetBestTextColor(): color has bad contrast: " +
                               bestColor + " difference: " + difference +
                               " needed: " + needed);
#endif

                // change the fg color
                int red   = bestColor.Red   + modifier;
                int green = bestColor.Green + modifier;
                int blue  = bestColor.Blue  + modifier;

                // cap to allowed values
                if (modifier > 0) {
                    if (red > 255) {
                        red = 255;
                    }
                    if (green > 255) {
                        green = 255;
                    }
                    if (blue > 255) {
                        blue = 255;
                    }
                } else {
                    if (red < 0) {
                        red = 0;
                    }
                    if (green < 0) {
                        green = 0;
                    }
                    if (blue < 0) {
                        blue = 0;
                    }
                }

                bestColor = new TextColor((byte) red, (byte) green, (byte) blue);
                
                // in case we found no good color
                if (bestColor == TextColor.White ||
                    bestColor == TextColor.Black) {
                    break;
                }
                attempts++;
            }
#if LOG4NET && COLOR_DEBUG
            f_Logger.Debug(
                String.Format(
                    "GetBestTextColor(): found good contrast: {0}|{1}={2} " +
                    "({3}) attempts: {4}", fgColor, bgColor,  bestColor,
                    neededContrast, attempts
                )
            );
#endif
            f_BestContrastColors.Add(key, bestColor);

            return bestColor;
        }

        // algorithm ported from PHP to C# from:
        // http://www.splitbrain.org/blog/2008-09/18-calculating_color_contrast_with_php
        public static double GetLuminanceDifference(TextColor color1, TextColor color2)
        {
            double L1 = 0.2126d * Math.Pow(color1.Red   / 255d, 2.2d) +
                        0.7152d * Math.Pow(color1.Green / 255d, 2.2d) +
                        0.0722d * Math.Pow(color1.Blue  / 255d, 2.2d);
            double L2 = 0.2126d * Math.Pow(color2.Red   / 255d, 2.2d) +
                        0.7152d * Math.Pow(color2.Green / 255d, 2.2d) +
                        0.0722d * Math.Pow(color2.Blue  / 255d, 2.2d);
            if (L1 > L2) {
                return (L1 + 0.05d) / (L2 + 0.05d);
            } else {
                return (L2 + 0.05d) / (L1 + 0.05d);
            }
        }

        public static double GetBritnessDifference(TextColor color1, TextColor color2)
        {
            double br1 = (299d * color1.Red +
                          587d * color1.Green +
                          114d * color1.Blue) / 1000d;
            double br2 = (299d * color2.Red +
                          587d * color2.Green +
                          114d * color2.Blue) / 1000d;
            return Math.Abs(br1 - br2);
        }

        // algorithm ported from JavaScript to C# from:
        // http://mjijackson.com/2008/02/rgb-to-hsl-and-rgb-to-hsv-color-model-conversion-algorithms-in-javascript
        internal static HslColor ToHSL(TextColor color)
        {
            var R = color.Red / 255d;
            var G = color.Green / 255d;
            var B = color.Blue / 255d;
            var max = Math.Max(Math.Max(R, G), B);
            var min = Math.Min(Math.Min(R, G), B);

            double H = 0d, S, L;
            var range = max + min;
            L = range / 2d;
            if (max == min) {
                S = 0d; // achromatic
            } else {
                var diff = max - min;
                S = L > 0.5d ? diff / (2 - diff) : diff / range;
                if (max == R) {
                    H = (G - B) / diff + (G < B ? 6d : 0d);
                } else if (max == G) {
                    H = (B - R) / diff + 2;
                } else if (max == B) {
                    H = (R - G) / diff + 4;
                }
                H /= 6;
            }
            return new HslColor(H, S, L);
        }

        public static TextColor GetNearestColor(TextColor color, IEnumerable<TextColor> palette)
        {
            if (palette == null) {
                throw new ArgumentNullException("palette");
            }

            var hslColor1 = ToHSL(color);
            TextColor nearestColor = null;
            double nearestDifference = Double.MaxValue;
            foreach (var color2 in palette) {
                // compute the Euclidean distance between the two HSL colors
                // without root square as we only compare the values
                // see http://en.wikipedia.org/wiki/Color_difference#Delta_E
                var hslColor2 = ToHSL(color2);
                var H1 = hslColor1.Hue;
                var S1 = hslColor1.Saturation;
                var L1 = hslColor1.Lightness;
                var H2 = hslColor2.Hue;
                var S2 = hslColor2.Saturation;
                var L2 = hslColor2.Lightness;
                var Hdelta = H1 - H2;
                var Sdelta = S1 - S2;
                var Ldelta = L1 - L2;
                var deltaE = (Hdelta * Hdelta) +
                             (Sdelta * Sdelta) +
                             (Ldelta * Ldelta);
                if (deltaE < nearestDifference) {
                    nearestDifference = deltaE;
                    nearestColor = color2;
                }
                if (deltaE == 0d) {
                    // found perfect match, can't get better than that
                    break;
                }
            }
            return nearestColor;
        }

        internal class HslColor
        {
            public double Hue { get; set; }
            public double Saturation { get; set; }
            public double Lightness { get; set; }

            public HslColor(double H, double S, double L)
            {
                Hue = H;
                Saturation = S;
                Lightness = L;
            }
        }
    }
}
