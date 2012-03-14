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
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.Text;
using LevelDB;
using Smuxi.Engine.Dto;

namespace Smuxi.Engine
{
    public class LevelDBMessageBuffer : MessageBufferBase
    {
#if LOG4NET
        static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        const string MessageCountKey = "__Count";
        const string MessageNumberKey = "__Number";

        Int64 MessageNumber { get; set; }
        Int64 MessageCount { get; set; }
        bool Disposed { get; set; }
        DB Database { get; set; }
        string DatabasePath { get; set; }

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
                var json = Database.Get(key);
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

        static LevelDBMessageBuffer()
        {
            JsConfig<MessagePartModel>.ExcludeTypeInfo = true;
        }

        public LevelDBMessageBuffer(string sessionUsername, string protocol,
                                    string networkId, string chatId) :
                               base(sessionUsername, protocol, networkId, chatId)
        {
            var bufferPath = GetBufferPath();
            DatabasePath = bufferPath + ".leveldb";

            var cleanDB = !Directory.Exists(DatabasePath);
            var options = new Options() {
                CreateIfMissing = true,
                // in a 4KB block fit 8 x 500 byte messages
                // 64 blocks == 512 messages
                // 64 blocks * 4KB block == 256KB used memory
                BlockCache = new Cache(64),
            };
            DateTime start, stop;
            start = DateTime.UtcNow;
            Database = new DB(options, DatabasePath);
            stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
            f_Logger.DebugFormat("ctor(): leveldb_open() " +
                                 "took: {0:0.00} ms",
                                 (stop - start).TotalMilliseconds);
#endif
            if (cleanDB) {
                // force metadata fields to be the first keys
                FlushMessageCount();
                FlushMessageNumber();
            } else {
                FetchMessageNumber();
                FetchMessageCount();
            }
        }

        public override void Add(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }
            CheckDisposed();

            var msgNumber = MessageNumber;
            var msgFileName = GetMessageKey(msgNumber++);
            var msgContent = JsonSerializer.SerializeToString(msg);
            Database.Put(msgFileName, msgContent);
            MessageNumber = msgNumber;
            MessageCount++;
            Flush();
        }

        public override IList<MessageModel> GetRange(int offset, int limit)
        {
            var range = new List<MessageModel>(limit);
            var msgKey = GetMessageKey(offset);
            var options = Native.leveldb_readoptions_create();
            //Native.leveldb_readoptions_set_fill_cache(options, false);
            IntPtr iter = Native.leveldb_create_iterator(Database.Handle, options);
            for (Native.leveldb_iter_seek(iter, msgKey);
                 Native.leveldb_iter_valid(iter) && range.Count < limit;
                 Native.leveldb_iter_next(iter)) {
                string key = Native.leveldb_iter_key(iter);
                if (key == null || key.StartsWith("__")) {
                    // ignore internal fields
                    continue;
                }
                string json = Native.leveldb_iter_value(iter);
                var dto = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
                range.Add(dto.ToMessage());
            }
            Native.leveldb_iter_destroy(iter);
            return range;
        }

        public override void Clear()
        {
            throw new NotImplementedException ();
        }

        public override bool Contains(MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override void CopyTo(MessageModel[] array, int arrayIndex)
        {
            throw new NotImplementedException ();
        }

        public override bool Remove(MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override IEnumerator<MessageModel> GetEnumerator()
        {
            foreach (var entry in Database) {
                if (entry.Key == null || !entry.Key.EndsWith(".v1.json")) {
                    // ignore non json keys
                    continue;
                }
                var json = entry.Value;
                var dto = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
                yield return dto.ToMessage();
            }
        }

        public override int IndexOf(MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override void Insert(int index, MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override void RemoveAt(int index)
        {
            throw new NotImplementedException ();
        }

        void FlushMessageCount()
        {
            Database.Put(MessageCountKey, MessageCount.ToString());
        }

        void FlushMessageNumber()
        {
            Database.Put(MessageNumberKey, MessageNumber.ToString());
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

            var db = Database;
            if (db != null) {
                DateTime start, stop;
                start = DateTime.UtcNow;
                Database.Dispose();
                stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
                f_Logger.DebugFormat("Dispose(): leveldb_close() took: {0:0.00} ms",
                                     (stop - start).TotalMilliseconds);
#endif
                Database = null;
            }
        }

        void CheckDisposed()
        {
            if (!Disposed) {
                return;
            }
            throw new ObjectDisposedException(this.GetType().Name);
        }

        string GetMessageKey(Int64 number)
        {
            // TODO: align key to 16 or 32 bytes? also as the keys are sorted
            return String.Format("{0}.v1.json", number);
        }

        void FetchMessageCount()
        {
            var strCount = Database.Get(MessageCountKey);
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
            var options = Native.leveldb_readoptions_create();
            IntPtr iter = Native.leveldb_create_iterator(Database.Handle,
                                                         options);
            Native.leveldb_iter_seek_to_first(iter);
            while (Native.leveldb_iter_valid(iter)) {
                string key = Native.leveldb_iter_key(iter);
                if (key != null && key.EndsWith(".json")) {
                    // only count json files
                    count++;
                }
                Native.leveldb_iter_next(iter);
            }
            Native.leveldb_iter_destroy(iter);
            MessageCount = count;

            stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
            f_Logger.DebugFormat("FetchMessageCount(): scan took: {0:0.00} ms",
                                 (stop - start).TotalMilliseconds);
#endif
        }

        void FetchMessageNumber()
        {
            var strNumber = Database.Get(MessageNumberKey);
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
            var options = Native.leveldb_readoptions_create();
            IntPtr iter = Native.leveldb_create_iterator(Database.Handle,
                                                         options);
            Native.leveldb_iter_seek_to_first(iter);
            while (Native.leveldb_iter_valid(iter)) {
                string key = Native.leveldb_iter_key(iter);
                if (key != null && key.EndsWith(".json")) {
                    // only check json files
                    var strMsgNumber = key.Substring(0, key.IndexOf("."));
                    var intMsgNumber = 0L;
                    Int64.TryParse(strMsgNumber, out intMsgNumber);
                    if (intMsgNumber > msgNumber) {
                        msgNumber = intMsgNumber;
                    }
                }
                Native.leveldb_iter_next(iter);
            }
            Native.leveldb_iter_destroy(iter);
            MessageNumber = msgNumber;

            stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
            f_Logger.DebugFormat("FetchMessageNumber(): " +
                                 "full scan took: {0:0.00} ms",
                                 (stop - start).TotalMilliseconds);
#endif
        }
    }
}
