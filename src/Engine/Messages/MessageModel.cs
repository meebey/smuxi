/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2010 Mirco Bauer <meebey@meebey.net>
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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    [DataContract]
    public class MessageModel : ISerializable
    {
        static readonly Regex NickRegex = new Regex("^<([^ ]+)> ");
        private DateTime                f_TimeStamp;
        private IList<MessagePartModel> f_MessageParts;
        private MessageType             f_MessageType;
        [NonSerialized]
        private bool                    f_IsCompactable;

        [DataMember]
        public DateTime TimeStamp {
            get {
                return f_TimeStamp;
            }
            set {
                f_TimeStamp = value;
            }
        }

        [DataMember]
        public IList<MessagePartModel> MessageParts {
            get {
                return f_MessageParts;
            }
        }

        [IgnoreDataMember]
        public bool IsEmpty {
            get {
                return f_MessageParts.Count == 0;
            }
        }
        
        [DataMember]
        public MessageType MessageType {
            get {
                return f_MessageType;
            }
            set {
                f_MessageType = value;
            }
        }
        
        [IgnoreDataMember]
        public bool IsCompactable {
            get {
                return f_IsCompactable;
            }
            set {
                f_IsCompactable = value;
            }
        }

        public MessageModel()
        {
            f_TimeStamp    = DateTime.UtcNow;
            f_MessageParts = new List<MessagePartModel>();
            IsCompactable  = true;
        }
        
        public MessageModel(string text, MessageType msgType) : this()
        {
            f_MessageParts.Add(new TextMessagePartModel(null, null, false, false, false, text));
            f_MessageType = msgType;
        }

        public MessageModel(string text) : this(text, MessageType.Normal)
        {
        }
        
        public MessageModel(MessageModel msg) : this()
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            var writer = SerializationWriter.GetWriter();
            msg.GetObjectData(writer);
            var data = writer.GetData();
            var reader = SerializationReader.GetReader(data);
            SetObjectData(reader);
        }

        protected MessageModel(SerializationInfo info, StreamingContext ctx)
        {
            SerializationReader sr = SerializationReader.GetReader(info);
            SetObjectData(sr);
        }
        
        protected virtual void SetObjectData(SerializationReader sr)
        {
            f_TimeStamp    = sr.ReadDateTime();
            f_MessageParts = sr.ReadList<MessagePartModel>();
            f_MessageType  = (MessageType) sr.ReadInt32();
        }

        protected virtual void GetObjectData(SerializationWriter sw)
        {
            if (IsCompactable) {
                // OPT: compact all parts before serialization
                Compact();
            }

            sw.Write(f_TimeStamp);
            sw.Write(f_MessageParts);
            sw.Write((Int32) f_MessageType);
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx) 
        {
            SerializationWriter sw = SerializationWriter.GetWriter(); 
            GetObjectData(sw);
            sw.AddToInfo(info);
        }
        
        public override string ToString()
        {
            // OPT: StringBuilder's default of 16 chars is way too short for
            // a regular message. A regular message should be around 128 to
            // 256 chars
            StringBuilder sb = new StringBuilder(256);
            foreach (MessagePartModel part in MessageParts) {
               sb.Append(part.ToString());
            }
            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return f_TimeStamp.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MessageModel)) {
                return false;
            }

            var msg = (MessageModel) obj;
            return Equals(msg);
        }

        public bool Equals(MessageModel msg)
        {
            if ((object) msg == null) {
                return false;
            }

            // millisecond precision
            if (Math.Abs(f_TimeStamp.Subtract(msg.TimeStamp).TotalMilliseconds) > 1) {
                return false;
            }
            if (f_MessageType != msg.MessageType) {
                return false;
            }
            if (f_MessageParts.Count != msg.MessageParts.Count) {
                return false;
            }
            for (int i = 0; i < f_MessageParts.Count; i++) {
                if (f_MessageParts[i] != msg.MessageParts[i]) {
                    return false;
                }
            }

            return true;
        }

        public void Compact()
        {
            // the idea is to glue each text part that has the same attributes
            // to a combined new one to reduce the number of parts as they are
            // expensive when serialized

            // nothing to glue
            if (MessageParts.Count <= 1) {
                return;
            }

            var parts = new List<MessagePartModel>(MessageParts.Count);
            StringBuilder gluedText = null;
            bool dontMoveNext = false;
            var iter = MessageParts.GetEnumerator();
            while (dontMoveNext || iter.MoveNext()) {
                dontMoveNext = false;
                var current = iter.Current;
                parts.Add(current);

                // we can only glue pure text (not URLs etc)
                if (current.GetType() != typeof(TextMessagePartModel)) {
                    continue;
                }

                var currentText = (TextMessagePartModel) current;
                while (iter.MoveNext()) {
                    var next = iter.Current;
                    if (next.GetType() != typeof(TextMessagePartModel)) {
                        parts.Add(next);
                        break;
                    }

                    var nextText = (TextMessagePartModel) next;
                    if (!currentText.AttributesEquals(nextText)) {
                        // they aren't the same! no candidate for glueing :/
                        // but maybe the next part is
                        dontMoveNext = true;
                        break;
                    }

                    // glue time!
                    if (gluedText == null) {
                        // this is the first element of the gluing
                        gluedText = new StringBuilder(256);
                        gluedText.Append(currentText.Text);
                    }
                    gluedText.Append(nextText.Text);
                }

                if (gluedText != null) {
                    currentText.Text = gluedText.ToString();
                    gluedText = null;
                }
            }

            f_MessageParts = parts;
        }

        public string GetNick()
        {
            // HACK: MessageModel doesn't contain a Sender/Origin property
            // yet, thus we have to retrieve the information from the
            // meesage itself
            // TODO: extend MessageModel with Origin property
            var msgText = ToString();
            var match = NickRegex.Match(msgText);
            if (match.Success && match.Groups.Count >= 2) {
                return match.Groups[1].Value;
            }

            return null;
        }

        public static bool operator ==(MessageModel a, MessageModel b)
        {
            if (System.Object.ReferenceEquals(a, b)) {
                return true;
            }

            if ((object) a == null || (object) b == null) {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(MessageModel a, MessageModel b)
        {
            return !(a == b);
        }
    }
}
