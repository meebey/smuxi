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
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    [DataContract]
    public abstract class MessagePartModel : ISerializable
    {
        private bool                     f_IsHighlight;
        
        [DataMember]
        public abstract string Type {
             get;
        }

        [DataMember]
        public bool IsHighlight {
            get {
                return f_IsHighlight;
            }
            set {
                f_IsHighlight = value;
            }
        }

        protected MessagePartModel()
        {
        }

        protected MessagePartModel(bool highlight)
        {
            f_IsHighlight = highlight;
        }

        protected MessagePartModel(SerializationInfo info, StreamingContext ctx)
        {
            SerializationReader sr = SerializationReader.GetReader(info);
            SetObjectData(sr);
        }

        protected virtual void SetObjectData(SerializationReader sr)
        {
            f_IsHighlight = sr.ReadBoolean();
        }
        
        protected virtual void GetObjectData(SerializationWriter sw)
        {
            sw.Write(f_IsHighlight);
        }
        
        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx) 
        {
            SerializationWriter sw = SerializationWriter.GetWriter(); 
            GetObjectData(sw);
            sw.AddToInfo(info);
        }

        public override int GetHashCode()
        {
            return f_IsHighlight.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MessagePartModel)) {
                return false;
            }

            var part = (MessagePartModel) obj;
            return Equals(part);
        }

        public virtual bool Equals(MessagePartModel part)
        {
            if ((object) part == null) {
                return false;
            }

            if (f_IsHighlight != part.IsHighlight) {
                return false;
            }

            return true;
        }

        public static bool operator ==(MessagePartModel a, MessagePartModel b)
        {
            if (System.Object.ReferenceEquals(a, b)) {
                return true;
            }

            if ((object) a == null || (object) b == null) {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(MessagePartModel a, MessagePartModel b)
        {
            return !(a == b);
        }
    }
}
