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
    [Serializable()]
    public class FormattedTextMessage
    {
        private TextColor _Color;
        private TextColor _BackgroundColor;
        private bool      _Underline;
        private bool      _Bold;
        private string    _Text;
        
        public TextColor Color {
            get {
                return _Color;
            }
        }
        
        public TextColor BackgroundColor {
            get {
                return _BackgroundColor;
            }
        }
        
        public bool Underline {
            get {
                return _Underline;
            }
        }
        
        public bool Bold {
            get {
                return _Bold;
            }
        }
        
        public string Text {
            get {
                return _Text;
            }
        }
        
        public FormattedTextMessage(TextColor color, TextColor bgcolor,
            bool underline, bool bold, string text)
        {
            if (color != null) {
                _Color = color;
            } else {
                _Color = new TextColor();
            }
            
            if (bgcolor != null) {
                _BackgroundColor = bgcolor;
            } else {
                _BackgroundColor = new TextColor();
            }
            
            _Underline = underline;
            _Bold = bold;
            _Text = text;
        }
    }
}
