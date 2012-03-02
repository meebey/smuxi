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
        Int64 f_MessageNumber = -1;
        Int64 f_MessageCount = -1;

        IntPtr Database { get; set; }
        string DatabasePath { get; set; }

        Int64 MessageNumber {
            get {
                if (f_MessageNumber != -1) {
                    return f_MessageNumber;
                }
                var searchKey = "Number";
                var options = Native.leveldb_readoptions_create();
                var strNumber = Native.leveldb_get(Database, options, searchKey);
                if (!String.IsNullOrEmpty(strNumber)) {
                    // yay we have a cached number value
                    var intNumber = 0L;
                    Int64.TryParse(strNumber, out intNumber);
                    f_MessageNumber = intNumber;
                    return f_MessageNumber;
                }

                // darn, we have make a full filename scan :/
                var msgNumber = 0L;
                options = Native.leveldb_readoptions_create();
                IntPtr iter = Native.leveldb_create_iterator(Database, options);
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
                MessageNumber = msgNumber;
                return f_MessageNumber;
            }
            set {
                var writeOptions = Native.leveldb_writeoptions_create();
                Native.leveldb_put(Database, writeOptions, "Number",
                                   value.ToString());
                f_MessageNumber = value;
            }
        }

        public override MessageModel this[int index] {
            get {
                //return GetRange(index, 1).First();
                var options = Native.leveldb_readoptions_create();
                var key = String.Format("{0}.v1.json", index);
                var json = Native.leveldb_get(Database, options, key);
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

        public override int Count {
            get {
                if (f_MessageCount != -1) {
                    return (int) f_MessageCount;
                }
                var searchKey = "Count";
                var options = Native.leveldb_readoptions_create();
                var strCount = Native.leveldb_get(Database, options, searchKey);
                if (!String.IsNullOrEmpty(strCount)) {
                    // yay we have a cached count value
                    var intCount = 0L;
                    Int64.TryParse(strCount, out intCount);
                    f_MessageCount = intCount;
                    return (int) f_MessageCount;
                }

                // darn, we have make a full filename scan :/
                options = Native.leveldb_readoptions_create();
                var count = 0;
                IntPtr iter = Native.leveldb_create_iterator(Database, options);
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

                var writeOptions = Native.leveldb_writeoptions_create();
                Native.leveldb_put(Database, writeOptions, "Count", count.ToString());
                f_MessageCount = count;
                return (int) f_MessageCount;
            }
        }

        public LevelDBMessageBuffer(string sessionUsername, string protocol,
                                    string networkId, string chatId) :
                               base(sessionUsername, protocol, networkId, chatId)
        {
            var bufferPath = GetBufferPath();
            DatabasePath = bufferPath + ".leveldb";

            var options = Native.leveldb_options_create();
            Native.leveldb_options_set_create_if_missing(options, '1');
            //Native.leveldb_options_set_max_open_files(options, 1);
            Database = Native.leveldb_open(options, DatabasePath);
        }

        public override void Add(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }
            var msgNumber = MessageNumber;
            var msgFileName = String.Format("{0}.v1.json", msgNumber++);
            var msgContent = JsonSerializer.SerializeToString(msg);
            var options = Native.leveldb_writeoptions_create();
            Native.leveldb_put(Database, options, msgFileName, msgContent);
            f_MessageNumber = msgNumber;
            if (f_MessageCount == -1) {
                var count = Count;
            }
            f_MessageCount++;
        }

        /*
        public override IList<MessageModel> GetRange(int offset, int limit)
        {
            var chunkMessages = new List<MessageModel>();
            var chunkFileName = GetChunkFileName(offset);
            var chunkFilePath = Path.Combine(ChunkBasePath, chunkFileName);
            if (File.Exists(chunkFilePath)) {
                using (var reader = File.OpenRead(chunkFilePath))
                using (var textReader = new StreamReader(reader)) {
                    var dtoMsgs = DeserializeChunk(textReader);
                    foreach (var dtoMsg in dtoMsgs) {
                        chunkMessages.Add(dtoMsg.ToMessage());
                    }
                }
            }
            // append CurrentChunk if it follows the offset
            if (offset >= CurrentChunkOffset && offset < CurrentChunkOffset + MaxChunkSize) {
                foreach (var dtoMsg in CurrentChunk) {
                    chunkMessages.Add(dtoMsg.ToMessage());
                }
            }
            var chunkStart = (offset / MaxChunkSize) * MaxChunkSize;
            var msgOffset = offset - chunkStart;
            var msgLimit = Math.Min(chunkMessages.Count - msgOffset, limit);
            var msgs = new List<MessageModel>(limit);
            msgs.AddRange(chunkMessages.GetRange(msgOffset, msgLimit));
            return msgs;
        }
        */

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
            throw new NotImplementedException ();
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

        public override void Flush()
        {
            // NOOP
        }

        public override void Dispose()
        {
            Flush();

            var db = Database;
            if (db != IntPtr.Zero) {
                Database = IntPtr.Zero;
                Native.leveldb_close(db);
            }
        }
    }
}
