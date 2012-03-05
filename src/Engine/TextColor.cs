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
using System.Runtime.Serialization;
using System.Globalization;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    public class TextColor : ISerializable
    {
        public static readonly TextColor None  = new TextColor();
        public static readonly TextColor Black = new TextColor(0, 0, 0);
        public static readonly TextColor White = new TextColor(255, 255, 255);
        public static readonly TextColor Grey  = new TextColor(128, 128, 128);
        
        private int f_Value;
        
        public int Value {
            get {
                return f_Value;
            }
            set {
                f_Value = value;
            }
        }
        
        public string HexCode {
            get {
                return f_Value.ToString("X6");
            }
        }
        
        public byte Red {
            get {
                return (byte) ((f_Value & 0xFF0000) >> 16);
            }
        }
        
        public byte Green {
            get {
                return (byte) ((f_Value & 0xFF00) >> 8);
            }
        }
        
        public byte Blue {
            get {
                return (byte) (f_Value & 0xFF);
            }
        }
        
        public TextColor()
        {
            f_Value = -1;
        }
        
        public TextColor(int value)
        {
            f_Value = value;
        }
        
        public TextColor(uint value)
        {
            f_Value = checked((int) value);
        }

        public TextColor(byte red, byte green, byte blue)
        {
            f_Value = red << 16 | green << 8 | blue;
        }
        
        protected TextColor(SerializationInfo info, StreamingContext ctx)
        {
            SerializationReader sr = SerializationReader.GetReader(info);
            SetObjectData(sr);
        }
        
        protected virtual void SetObjectData(SerializationReader sr)
        {
            f_Value = sr.ReadInt32();
        }
        
        protected virtual void GetObjectData(SerializationWriter sw)
        {
            sw.Write(f_Value);
        }
        
        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx) 
        {
            SerializationWriter sw = SerializationWriter.GetWriter(); 
            GetObjectData(sw);
            sw.AddToInfo(info);
        }

        public static TextColor Parse(string hexCode)
        {
            if (hexCode == null) {
                throw new ArgumentNullException("hexCode");
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
            return new TextColor((byte) red, (byte) green, (byte) blue);
        }

        public override string ToString()
        {
            return String.Format("#{0}", HexCode);
        }
        
        public override bool Equals(object obj)
        {
            TextColor value = obj as TextColor;
            return Equals(value);
        }
        
        public bool Equals(TextColor value)
        {
            if ((object) value == null) {
                return false;
            }
            
            return f_Value == value.Value; 
        }

        public override int GetHashCode()
        {
            return f_Value.GetHashCode();
        }
        
        public static bool operator ==(TextColor x, TextColor y)
        {
            if (Object.ReferenceEquals(x, y)) {
                return true;
            }

            if (((object) x == null) || ((object) y == null)) {
                return false;
            }

            return x.Equals(y);
        }
        
        public static bool operator !=(TextColor x, TextColor y)
        {
            return !(x == y);
        }
    }
}
