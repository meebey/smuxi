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
    public class LevelDBMessageBuffer : KeyValueMessageBufferBase
    {
#if LOG4NET
        static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        DB Database { get; set; }
        string DatabasePath { get; set; }
        bool Disposed { get; set; }

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
                if (key == null || key.StartsWith(InternalFieldPrefix)) {
                    // ignore internal fields
                    continue;
                }
                string json = Native.leveldb_iter_value(iter);
                range.Add(GetMessage(json));
            }
            Native.leveldb_iter_destroy(iter);
            return range;
        }

        public override void Dispose()
        {
            base.Dispose();

            var disposed = Disposed;
            if (disposed) {
                return;
            }

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

        protected override void Put(string key, string value)
        {
            CheckDisposed();
            Database.Put(key, value);
        }

        protected override string Get(string key)
        {
            CheckDisposed();
            return Database.Get(key);
        }

        protected override IEnumerable<string> GetKeys()
        {
            CheckDisposed();
            var options = Native.leveldb_readoptions_create();
            IntPtr iter = Native.leveldb_create_iterator(Database.Handle,
                                                         options);
            Native.leveldb_iter_seek_to_first(iter);
            while (Native.leveldb_iter_valid(iter)) {
                string key = Native.leveldb_iter_key(iter);
                yield return key;
                Native.leveldb_iter_next(iter);
            }
            Native.leveldb_iter_destroy(iter);
        }

        protected override IEnumerable<string> GetValues()
        {
            CheckDisposed();
            var options = Native.leveldb_readoptions_create();
            IntPtr iter = Native.leveldb_create_iterator(Database.Handle,
                                                         options);
            Native.leveldb_iter_seek_to_first(iter);
            while (Native.leveldb_iter_valid(iter)) {
                string value = Native.leveldb_iter_value(iter);
                yield return value;
                Native.leveldb_iter_next(iter);
            }
            Native.leveldb_iter_destroy(iter);
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetKeyValuePairs()
        {
            foreach (var entry in Database) {
                yield return entry;
            }
        }
    }
}
