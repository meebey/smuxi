// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using ServiceStack.Text;
using Smuxi.Engine.Dto;

namespace Smuxi.Engine
{
    public abstract class KeyValueMessageBufferBase : MessageBufferBase
    {
#if LOG4NET
        static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif

        protected const string InternalFieldPrefix = "!";
        protected const string MessageCountKey = InternalFieldPrefix + "Count";
        protected const string MessageNumberKey = InternalFieldPrefix + "Number";

        protected Int64 MessageNumber { get; set; }
        protected Int64 MessageCount { get; set; }
        bool Disposed { get; set; }

        public override int Count {
            get {
                return (int) MessageCount;
            }
        }

        public override MessageModel this[int index] {
            get {
                CheckDisposed();

                // OPT: single get is faster than seek + get_value
                //return GetRange(index, 1).First();
                var key = GetMessageKey(index);
                var json = Get(key);
                if (json == null) {
                    throw new ArgumentOutOfRangeException("index");
                }
                var dto = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
                return dto.ToMessage();
            }
            set {
                throw new NotImplementedException();
            }
        }

        static KeyValueMessageBufferBase()
        {
            JsConfig<MessagePartModel>.ExcludeTypeInfo = true;
        }

        protected KeyValueMessageBufferBase()
        {
        }

        protected KeyValueMessageBufferBase(string sessionUsername,
                                            string protocol,
                                            string networkId,
                                            string chatId) :
                                       base(sessionUsername, protocol,
                                            networkId, chatId)
        {
        }

        public override void Add(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }
            CheckDisposed();

            var msgNumber = MessageNumber;
            var msgFileName = GetMessageKey(msgNumber++);
            var msgContent = GetJson(msg);
            Put(msgFileName, msgContent);
            MessageNumber = msgNumber;
            MessageCount++;
            Flush();
        }

        public override void Clear()
        {
            throw new NotImplementedException();
        }

        public override bool Contains(MessageModel item)
        {
            throw new NotImplementedException();
        }

        public override void CopyTo(MessageModel[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public override bool Remove(MessageModel item)
        {
            throw new NotImplementedException();
        }

        public override int IndexOf(MessageModel item)
        {
            throw new NotImplementedException();
        }

        public override void Insert(int index, MessageModel item)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            CheckDisposed();

            DateTime start, stop;
            start = DateTime.UtcNow;
            FlushMessageCount();
            FlushMessageNumber();
            stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG && DISABLED
            f_Logger.DebugFormat("Flush(): took: {0:0.00} ms",
                                 (stop - start).TotalMilliseconds);
#endif
        }

        public override void Dispose()
        {
            var disposed = Disposed;
            if (disposed) {
                return;
            }
            Flush();
            Disposed = true;
        }

        public override IEnumerator<MessageModel> GetEnumerator()
        {
            foreach (var entry in GetKeyValuePairs()) {
                if (entry.Key == null || !entry.Key.EndsWith(".v1.json")) {
                    // ignore non json keys
                    continue;
                }
                var json = entry.Value;
                yield return GetMessage(json);
            }
        }

        protected static string GetJson(MessageModel msg)
        {
            return JsonSerializer.SerializeToString(msg);
        }

        protected static MessageModel GetMessage(string json)
        {
            var dto = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
            return dto.ToMessage();
        }

        protected void FlushMessageCount()
        {
            Put(MessageCountKey, MessageCount.ToString());
        }

        protected void FlushMessageNumber()
        {
            Put(MessageNumberKey, MessageNumber.ToString());
        }

        protected void CheckDisposed()
        {
            if (!Disposed) {
                return;
            }
            throw new ObjectDisposedException(GetType().Name);
        }

        protected string GetMessageKey(Int64 number)
        {
            // align key to 32 bytes as the keys are sorted bytewise
            return String.Format("{0:000000000000000000000000}.v1.json", number);
        }

        protected void FetchMessageCount()
        {
            var strCount = Get(MessageCountKey);
            if (!String.IsNullOrEmpty(strCount)) {
                // yay we have a cached count value
                var intCount = 0L;
                Int64.TryParse(strCount, out intCount);
                MessageCount = intCount;
                return;
            }

            // darn, we have make a full filename scan :/
            DateTime start, stop;
            start = DateTime.UtcNow;

            var count = 0;
            foreach (var key in GetKeys()) {
                if (key != null && key.EndsWith(".json")) {
                    // only count json files
                    count++;
                }
            }
            MessageCount = count;

            stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
            f_Logger.DebugFormat("FetchMessageCount(): scan took: {0:0.00} ms",
                                 (stop - start).TotalMilliseconds);
#endif
        }

        protected void FetchMessageNumber()
        {
            var strNumber = Get(MessageNumberKey);
            if (!String.IsNullOrEmpty(strNumber)) {
                // yay we have a cached number value
                var intNumber = 0L;
                Int64.TryParse(strNumber, out intNumber);
                MessageNumber = intNumber;
                return;
            }

            // darn, we have make a full filename scan :/
            DateTime start, stop;
            start = DateTime.UtcNow;

            var msgNumber = 0L;
            foreach (var key in GetKeys()) {
                if (key == null || !key.EndsWith(".json")) {
                    // only check json files
                    continue;
                }
                var strMsgNumber = key.Substring(0, key.IndexOf("."));
                var intMsgNumber = 0L;
                Int64.TryParse(strMsgNumber, out intMsgNumber);
                if (intMsgNumber > msgNumber) {
                    msgNumber = intMsgNumber;
                }
            }
            MessageNumber = msgNumber;

            stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
            f_Logger.DebugFormat("FetchMessageNumber(): " +
                                 "full scan took: {0:0.00} ms",
                                 (stop - start).TotalMilliseconds);
#endif
        }

        protected abstract void Put(string key, string value);
        protected abstract string Get(string key);

        protected abstract IEnumerable<string> GetKeys();
        protected abstract IEnumerable<string> GetValues();
        protected abstract IEnumerable<KeyValuePair<string, string>> GetKeyValuePairs();
    }
}
