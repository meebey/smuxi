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
using System.IO;
using System.Linq;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class ThemeSettings
    {
        private Gdk.Color?            f_BackgroundColor;
        private Gdk.Color?            f_ForegroundColor;
        private Pango.FontDescription f_FontDescription;
        private Gdk.Color             f_HighlightColor;
        private Gdk.Color             f_ActivityColor;
        private Gdk.Color             f_NoActivityColor;
        private Gdk.Color             f_EventColor;

        public Nullable<Gdk.Color> BackgroundColor {
            get {
                return f_BackgroundColor;
            }
        }

        public Pango.FontDescription FontDescription {
            get {
                return f_FontDescription;
            }
        }

        public Nullable<Gdk.Color> ForegroundColor {
            get {
                return f_ForegroundColor;
            }
        }

        public Gdk.Color ActivityColor {
            get {
                return f_ActivityColor;
            }
        }

        public Gdk.Color EventColor {
            get {
                return f_EventColor;
            }
        }

        public Gdk.Color HighlightColor {
            get {
                return f_HighlightColor;
            }
        }

        public Gdk.Color NoActivityColor {
            get {
                return f_NoActivityColor;
            }
        }
        
        public ThemeSettings()
        {
        }
        
        public ThemeSettings(UserConfig config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            
            string bgStr = (string) config["Interface/Chat/BackgroundColor"];
            if (!String.IsNullOrEmpty(bgStr)) {
                Gdk.Color bgColor = Gdk.Color.Zero;
                if (Gdk.Color.Parse(bgStr, ref bgColor)) {
                    f_BackgroundColor = bgColor;
                }
            } else {
                f_BackgroundColor = null;
            }

            string fgStr = (string) config["Interface/Chat/ForegroundColor"];
            if (!String.IsNullOrEmpty(fgStr)) {
                Gdk.Color fgColor = Gdk.Color.Zero;
                if (Gdk.Color.Parse(fgStr, ref fgColor)) {
                    f_ForegroundColor = fgColor;
                }
            } else {
                f_ForegroundColor = null;
            }

            string colorStr;
            Gdk.Color color;
            colorStr = (string) config["Interface/Notebook/Tab/HighlightColor"];
            color = Gdk.Color.Zero;
            if (Gdk.Color.Parse(colorStr, ref color)) {
                f_HighlightColor = color;
            }

            colorStr = (string) config["Interface/Notebook/Tab/ActivityColor"];
            color = Gdk.Color.Zero;
            if (Gdk.Color.Parse(colorStr, ref color)) {
                f_ActivityColor = color;
            }

            colorStr = (string) config["Interface/Notebook/Tab/NoActivityColor"];
            color = Gdk.Color.Zero;
            if (Gdk.Color.Parse(colorStr, ref color)) {
                f_NoActivityColor = color;
            }

            colorStr = (string) config["Interface/Notebook/Tab/EventColor"];
            color = Gdk.Color.Zero;
            if (Gdk.Color.Parse(colorStr, ref color)) {
                f_EventColor = color;
            }

            string fontFamily = (string) config["Interface/Chat/FontFamily"];
            string fontStyle = (string) config["Interface/Chat/FontStyle"];
            int fontSize = 0;
            if (config["Interface/Chat/FontSize"] != null) {
                fontSize = (int) config["Interface/Chat/FontSize"];
            }
            Pango.FontDescription fontDescription = new Pango.FontDescription();
            if (String.IsNullOrEmpty(fontFamily)) {
                // HACK: use Consolas or Fixed-Sys on Windows by default
                if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                    var context = Frontend.MainWindow.CreatePangoContext();
                    if (context.Families.Any(family => family.Name == "Consolas")) {
                        // this system has Consolas available, let's use it!
                        fontDescription.Family = "Consolas, monospace";
                        // Consolas only looks good in size 11
                        fontDescription.Size = 11 * 1024;
                        fontDescription.Weight = Pango.Weight.Normal;
                        fontDescription.Style = Pango.Style.Normal;
                    } else {
                        // too bad, fallback to FixedSys then
                        fontDescription.Family = "FixedsysTTF, monospace";
                        // FixedSys only looks good in size 11
                        fontDescription.Size = 11 * 1024;
                        fontDescription.Weight = Pango.Weight.Bold;
                        fontDescription.Style = Pango.Style.Normal;
                    }
                } else {
                    // use Monospace and Bold by default
                    fontDescription.Family = "monospace";
                    // black bold font on white background looks odd 
                    //fontDescription.Weight = Pango.Weight.Bold;
                }
            } else {
                fontDescription.Family = fontFamily;
                string frontWeigth = null;
                if (fontStyle.Contains(" ")) {
                    int pos = fontStyle.IndexOf(" ");
                    frontWeigth = fontStyle.Substring(0, pos);
                    fontStyle = fontStyle.Substring(pos + 1);
                }
                fontDescription.Style = (Pango.Style) Enum.Parse(typeof(Pango.Style), fontStyle);
                if (frontWeigth != null) {
                    fontDescription.Weight = (Pango.Weight) Enum.Parse(typeof(Pango.Weight), frontWeigth);
                }
                fontDescription.Size = fontSize * 1024;
            }
            f_FontDescription = fontDescription;
        }
    }
}
