/*
 * $Id: Config.cs 100 2005-08-07 14:54:22Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/Config.cs $
 * $Rev: 100 $
 * $Author: meebey $
 * $Date: 2005-08-07 16:54:22 +0200 (Sun, 07 Aug 2005) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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
using System.Runtime.Serialization;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    public class TextMessagePartModel : MessagePartModel
    {
        private TextColor f_ForegroundColor;
        private TextColor f_BackgroundColor;
        private bool      f_Underline;
        private bool      f_Bold;
        private bool      f_Italic;
        private string    f_Text;
        
        public TextColor ForegroundColor {
            get {
                return f_ForegroundColor;
            }
            set {
                f_ForegroundColor = value;
            }
        }
        
        public TextColor BackgroundColor {
            get {
                return f_BackgroundColor;
            }
            set {
                f_BackgroundColor = value;
            }
        }
        
        public bool Underline {
            get {
                return f_Underline;
            }
            set {
                f_Underline = value;
            }
        }
        
        public bool Bold {
            get {
                return f_Bold;
            }
            set {
                f_Bold = value;
            }
        }
        
        public bool Italic {
            get {
                return f_Italic;
            }
            set {
                f_Italic = value;
            }
        }
        
        public string Text {
            get {
                return f_Text;
            }
            set {
                f_Text = value;
            }
        }
        
        public TextMessagePartModel() :
                               base()
        {
            f_ForegroundColor = new TextColor();
            f_BackgroundColor = new TextColor();
        }
        
        public TextMessagePartModel(string text) :
                               this(null, null, false, false, false, text, false)
        {
        }
        
        public TextMessagePartModel(string text, bool highlight) :
                               this(null, null, false, false, false, text, highlight)
        {
        }
        
        public TextMessagePartModel(TextColor fgColor, TextColor bgColor,
                                    bool underline, bool bold, bool italic,
                                    string text, bool highlight) :
                               base(highlight)
        {
            if (fgColor != null) {
                f_ForegroundColor = fgColor;
            } else {
                f_ForegroundColor = new TextColor();
            }
            
            if (bgColor != null) {
                f_BackgroundColor = bgColor;
            } else {
                f_BackgroundColor = new TextColor();
            }
            
            f_Underline = underline;
            f_Bold      = bold;
            f_Italic    = italic;
            f_Text      = text;
        }
        
        public TextMessagePartModel(TextColor fgColor, TextColor bgColor,
                                    bool underline, bool bold, bool italic,
                                    string text) :
                               this(fgColor, bgColor, underline, bold, italic, text, false)
        {
        }

        protected TextMessagePartModel(SerializationInfo info, StreamingContext ctx) :
                                  base(info, ctx)
        {
        }
        
        protected override void SetObjectData(SerializationReader sr)
        {
            base.SetObjectData(sr);
            
            f_ForegroundColor = new TextColor(sr.ReadInt32());
            f_BackgroundColor = new TextColor(sr.ReadInt32());
            f_Underline       = sr.ReadBoolean();
            f_Bold            = sr.ReadBoolean();
            f_Italic          = sr.ReadBoolean();
            f_Text            = sr.ReadString();
        }
        
        protected override void GetObjectData(SerializationWriter sw)
        {
            base.GetObjectData(sw);

            sw.Write(f_ForegroundColor.Value);
            sw.Write(f_BackgroundColor.Value);
            sw.Write(f_Underline);
            sw.Write(f_Bold);
            sw.Write(f_Italic);
            sw.Write(f_Text);
        }
    }
}
