// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2011 Mirco Bauer <meebey@meebey.net>
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
using System.Collections;
using System.Collections.Generic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    public abstract class MessageBufferBase : IMessageBuffer
    {
        protected string Protocol { get; set; }
        protected string NetworkID { get; set; }
        protected string ChatID { get; set; }
        protected string SessionUsername { get; set; }
        public    int    MaxCapacity { get; set; }

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        protected MessageBufferBase()
        {
        }

        protected MessageBufferBase(string sessionUsername, string protocol,
                                    string networkId, string chatId)
        {
            if (sessionUsername == null) {
                throw new ArgumentNullException("sessionUsername");
            }
            if (protocol == null) {
                throw new ArgumentNullException("protocol");
            }
            if (networkId == null) {
                throw new ArgumentNullException("networkId");
            }
            if (chatId == null) {
                throw new ArgumentNullException("chatId");
            }

            SessionUsername = sessionUsername;
            Protocol = protocol;
            NetworkID = networkId;
            ChatID = chatId;
        }

        public virtual IList<MessageModel> GetRange(int offset, int limit)
        {
            if (offset < 0) {
                throw new ArgumentException(
                    "offset must be greater than or equal to 0.", "offset"
                );
            }

            var bufferCount = Count;
            var rangeCount = Math.Min(bufferCount, limit);
            var range = new List<MessageModel>(rangeCount);
            for (int i = offset; i < offset + limit && i < bufferCount; i++) {
                range.Add(this[i]);
            }
            return range;
        }

        public virtual void CopyTo(MessageModel[] array, int arrayIndex)
        {
            if (array == null) {
                throw new ArgumentNullException("array");
            }

            int i = arrayIndex;
            foreach (var msg in this) {
                array[i++] = msg;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected string GetBufferPath()
        {
            var path = Platform.GetBuffersPath(SessionUsername);
            var protocol = Protocol.ToLower();
            var network = NetworkID.ToLower();
            path = Path.Combine(path, protocol);
            if (network != protocol) {
                path = Path.Combine(path, network);
            }
            path = IOSecurity.GetFilteredPath(path);
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            var chatId = IOSecurity.GetFilteredFileName(ChatID.ToLower());
            return Path.Combine(path, chatId);
        }

        public abstract int Count { get; }
        public abstract MessageModel this[int index] { get; set; }

        public abstract void Add(MessageModel item);
        public abstract void Clear();
        public abstract bool Contains(MessageModel item);
        public abstract bool Remove(MessageModel item);
        public abstract IEnumerator<MessageModel> GetEnumerator();
        public abstract int IndexOf(MessageModel item);
        public abstract void Insert(int index, MessageModel item);
        public abstract void RemoveAt(int index);
        public abstract void Flush();
        public abstract void Dispose();
    }
}
