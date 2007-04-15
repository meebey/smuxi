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

namespace Meebey.Smuxi.Engine
{
    [Serializable]
    public class TextMessagePartModel : MessagePartModel
    {
        private TextColor _ForegroundColor;
        private TextColor _BackgroundColor;
        private bool      _Underline;
        private bool      _Bold;
        private bool      _Italic;
        private string    _Text;
        
        public TextColor ForegroundColor {
            get {
                return _ForegroundColor;
            }
            set {
                _ForegroundColor = value;
            }
        }
        
        public TextColor BackgroundColor {
            get {
                return _BackgroundColor;
            }
            set {
                _BackgroundColor = value;
            }
        }
        
        public bool Underline {
            get {
                return _Underline;
            }
            set {
                _Underline = value;
            }
        }
        
        public bool Bold {
            get {
                return _Bold;
            }
            set {
                _Bold = value;
            }
        }
        
        public bool Italic {
            get {
                return _Italic;
            }
            set {
                _Italic = value;
            }
        }
        
        public string Text {
            get {
                return _Text;
            }
            set {
                _Text = value;
            }
        }
        
        public TextMessagePartModel() :
                               base(MessagePartType.Text)
        {
            _ForegroundColor = new TextColor();
            _BackgroundColor = new TextColor();
        }
        
        public TextMessagePartModel(TextColor fgColor, TextColor bgColor,
                                    bool underline, bool bold, bool italic,
                                    string text, bool highlight) :
                               base(MessagePartType.Text, highlight)
        {
            if (fgColor != null) {
                _ForegroundColor = fgColor;
            } else {
                _ForegroundColor = new TextColor();
            }
            
            if (bgColor != null) {
                _BackgroundColor = bgColor;
            } else {
                _BackgroundColor = new TextColor();
            }
            
            _Underline = underline;
            _Bold = bold;
            _Italic = italic;
            _Text = text;
        }
        
        public TextMessagePartModel(TextColor fgColor, TextColor bgColor,
                                    bool underline, bool bold, bool italic,
                                    string text) :
                               this(fgColor, bgColor, underline, bold, italic, text, false)
        {
        }
    }
}
