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
using System.Collections;
using System.Collections.Generic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public abstract class MessageBufferBase : IMessageBuffer
    {
        protected string Protocol { get; set; }
        protected string NetworkID { get; set; }
        protected string ChatID { get; set; }
        public    int    MaxCapacity { get; set; }

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        protected MessageBufferBase()
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract int Count { get; }
        public abstract MessageModel this[int index] { get; set; }

        public abstract void Add(MessageModel item);
        public abstract void Clear();
        public abstract bool Contains(MessageModel item);
        public abstract void CopyTo(MessageModel[] array, int arrayIndex);
        public abstract bool Remove(MessageModel item);
        public abstract IEnumerator<MessageModel> GetEnumerator();
        public abstract int IndexOf(MessageModel item);
        public abstract void Insert(int index, MessageModel item);
        public abstract void RemoveAt(int index);
        public abstract IList<MessageModel> GetRange(int offset, int limit);
        public abstract void Dispose();
    }
}
