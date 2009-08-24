/*
 * $Id: Config.cs 100 2005-08-07 14:54:22Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/Config.cs $
 * $Rev: 100 $
 * $Author: meebey $
 * $Date: 2005-08-07 16:54:22 +0200 (Sun, 07 Aug 2005) $
 *
 * Smuxi - Smart MUltipleXed Irc
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

namespace Smuxi.Engine
{
    [Serializable]
    public static class IrcTextColor
    {
        private static TextColor _Normal      = new TextColor(-1);
        private static TextColor _White       = new TextColor(0xFFFFFF);
        private static TextColor _Black       = new TextColor(0x000000);
        private static TextColor _Blue        = new TextColor(0x0000FF);
        private static TextColor _Green       = new TextColor(0x008000);
        private static TextColor _Red         = new TextColor(0xFF0000);
        private static TextColor _Brown       = new TextColor(0xA52A2A);
        private static TextColor _Purple      = new TextColor(0x800080);
        private static TextColor _Orange      = new TextColor(0xFFA500);
        private static TextColor _Yellow      = new TextColor(0xFFFF00);
        private static TextColor _LightGreen  = new TextColor(0x00FF00);
        private static TextColor _Teal        = new TextColor(0x008080);
        private static TextColor _LightCyan   = new TextColor(0xE0FFFF);
        //static private TextColor _LightBlue   = new TextColor(0xADD8E6);
        private static TextColor _LightBlue   = new TextColor(0xA4C8E0);
        private static TextColor _LightPurple = new TextColor(0xEE82EE);
        private static TextColor _Grey        = new TextColor(0x808080);
        private static TextColor _LightGrey   = new TextColor(0xD3D3D3);
        
        public static TextColor Normal {
            get {
                return _Normal;
            }
            set {
                _Normal = value;
            }
        }
        
        public static TextColor White {
            get {
                return _White;
            }
            set {
                _White = value;
            }
        }
        
        public static TextColor Black {
            get {
                return _Black;
            }
            set {
                _Black = value;
            }
        }
        
        public static TextColor Blue {
            get {
                return _Blue;
            }
            set {
                _Blue = value;
            }
        }
        
        public static TextColor Green {
            get {
                return _Green;
            }
            set {
                _Green = value;
            }
        }
        
        public static TextColor Red {
            get {
                return _Red;
            }
            set {
                _Red = value;
            }
        }
        
        public static TextColor Brown {
            get {
                return _Brown;
            }
            set {
                _Brown = value;
            }
        }

        public static TextColor Purple {
            get {
                return _Purple;
            }
            set {
                _Purple = value;
            }
        }

        public static TextColor Orange {
            get {
                return _Orange;
            }
            set {
                _Orange = value;
            }
        }

        public static TextColor Yellow {
            get {
                return _Yellow;
            }
            set {
                _Yellow = value;
            }
        }

        public static TextColor LightGreen {
            get {
                return _LightGreen;
            }
            set {
                _LightGreen = value;
            }
        }

        public static TextColor Teal {
            get {
                return _Teal;
            }
            set {
                _Teal = value;
            }
        }

        public static TextColor LightCyan {
            get {
                return _LightCyan;
            }
            set {
                _LightCyan = value;
            }
        }

        public static TextColor LightBlue {
            get {
                return _LightBlue;
            }
            set {
                _LightBlue = value;
            }
        }

        public static TextColor LightPurple {
            get {
                return _LightPurple;
            }
            set {
                _LightPurple = value;
            }
        }

        public static TextColor Grey {
            get {
                return _Grey;
            }
            set {
                _Grey = value;
            }
        }

        public static TextColor LightGrey {
            get {
                return _LightGrey;
            }
            set {
                _LightGrey = value;
            }
        }
    }
}
