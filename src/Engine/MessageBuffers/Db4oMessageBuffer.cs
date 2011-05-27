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
using System.Collections.Generic;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Defragment;
using Db4objects.Db4o.Diagnostic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class Db4oMessageBuffer : MessageBufferBase
    {
#if LOG4NET
        static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        const int        DefaultFlushInterval = 16;
        List<MessageModel> f_Index;
        int              FlushInterval { get; set; }
        int              FlushCounter { get; set; }
        IObjectContainer Database { get; set; }
        string           DatabaseFile { get; set; }
        string           SessionUsername { get; set; }
#if DB4O_8_0
        IEmbeddedConfiguration DatabaseConfiguration { get; set; }
#else
        IConfiguration         DatabaseConfiguration { get; set; }
#endif

        private List<MessageModel> Index {
            get {
                if (f_Index == null) {
                    RestoreIndex();
                }
                return f_Index;
            }
            set {
                f_Index = value;
            }
        }

        public override MessageModel this[int index] {
            get {
                var dbMsg = Index[index];
                var msg = GetMessage(dbMsg);
                return msg;
            }
            set {
                throw new NotImplementedException();
            }
        }

        public override int Count {
            get {
                return Index.Count;
            }
        }

        public Db4oMessageBuffer(string sessionUsername, string protocol,
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

            FlushInterval = DefaultFlushInterval;
            DatabaseFile = GetDatabaseFile();
#if DB4O_8_0
            DatabaseConfiguration = Db4oEmbedded.NewConfiguration();
            DatabaseConfiguration.Common.AllowVersionUpdates = true;
            DatabaseConfiguration.Common.ActivationDepth = 0;
            DatabaseConfiguration.Common.ObjectClass(typeof(MessageModel)).
                                         Indexed(true);
            DatabaseConfiguration.Common.ObjectClass(typeof(MessageModel)).
                                         ObjectField("f_TimeStamp").
                                         Indexed(true);
            //DatabaseConfiguration.Common.Diagnostic.AddListener(new DiagnosticToConsole());
#else
            DatabaseConfiguration = Db4oFactory.Configure();
            DatabaseConfiguration.AllowVersionUpdates(true);
            DatabaseConfiguration.ObjectClass(typeof(MessageModel)).
                                  ObjectField("f_TimeStamp").Indexed(true);
#endif
            try {
                //DefragDatabase();
                OpenDatabase();
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("Db4oMessageBuffer(): failed to open message " +
                             "database: " + DatabaseFile, ex);
#endif
                throw;
            }
        }

        ~Db4oMessageBuffer()
        {
            Dispose(false);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (Database == null) {
                return;
            }

            CloseDatabase();
            Database = null;
            Index = null;
        }

        public override void Add(MessageModel item)
        {
            if (item == null) {
                throw new ArgumentNullException("item");
            }

            if (MaxCapacity > 0 && Index.Count >= MaxCapacity) {
                RemoveAt(0);
            }

            // TODO: auto-flush every 60 seconds
            var dbMsg = new MessageModel(item);
            Index.Add(dbMsg);
            Database.Store(dbMsg);
            Database.Deactivate(dbMsg, 5);
            FlushCounter++;
            if (FlushCounter >= FlushInterval) {
                Flush();
                FlushCounter = 0;
            }
        }

        public override void Clear()
        {
            foreach (var msg in Index) {
                Database.Delete(msg);
            }
            ResetIndex();
        }

        public override bool Contains(MessageModel item)
        {
            if (item == null) {
                throw new ArgumentNullException("item");
            }

            // TODO: benchmark me!
            //return Database.Query<MessageModel>().Contains(item);
            return IndexOf(item) != -1;
        }

        public override void CopyTo(MessageModel[] array, int arrayIndex)
        {
            if (array == null) {
                throw new ArgumentNullException("array");
            }

            int i = arrayIndex;
            foreach (var msg in this) {
                array[i++] = msg;
            }
        }

        public override IEnumerator<MessageModel> GetEnumerator()
        {
            foreach (var dbMsg in Index) {
                yield return GetMessage(dbMsg);
            }
        }

        public override int IndexOf(MessageModel item)
        {
            if (item == null) {
                throw new ArgumentNullException("item");
            }

            var res = Database.QueryByExample(item);
            // return -1 if not found
            if (res.Count == 0) {
                return -1;
            }
            var msg = (MessageModel) res[0];
            return Index.FindIndex(delegate(MessageModel match) {
                return Object.ReferenceEquals(msg, match);
            });
        }

        public override void Insert(int index, MessageModel item)
        {
            throw new NotSupportedException();
        }

        public override void RemoveAt(int index)
        {
            if (index < 0 || index >= Index.Count) {
                throw new ArgumentOutOfRangeException("index");
            }

            var item = Index[index];
            Index.RemoveAt(index);
            if (item == null) {
#if LOG4NET
                Logger.Error(
                    String.Format("RemoveAt(): index: {0} is null!", index)
                );
#endif
                return;
            }

            // we have to pass an activated object in order to delete it :/
            Database.Activate(item, 1);
            Database.Delete(item);
            // TODO: auto-commit after some timeout
        }

        public override bool Remove(MessageModel item)
        {
            if (item == null) {
                throw new ArgumentNullException("item");
            }

            if (!Contains(item)) {
                return false;
            }
            Index.Remove(item);
            Database.Delete(item);
            return true;
        }

        public override IList<MessageModel> GetRange(int offset, int limit)
        {
            if (offset < 0) {
                throw new ArgumentException(
                    "offset must be greater than or equal to 0.", "offset"
                );
            }
            // Neither Count nor the Indexer have to be synchronized as the
            // messages might move from the buffer to the db4o index but that
            // doesn't change the Count neither affects the combined indexer
            // BUG?: but what about MaxCapacity which will remove oldest items
            // when new messages are added, our loop here would become
            // inconsistent!
            var bufferCount = Count;
            var rangeCount = Math.Min(bufferCount, limit);
            var range = new List<MessageModel>(rangeCount);
            for (int i = offset; i < offset + limit && i < bufferCount; i++) {
                range.Add(this[i]);
            }
            return range;
        }

        string GetDatabaseFile()
        {
            var dbPath = Platform.GetBuffersPath(SessionUsername);
            var protocol = Protocol.ToLower();
            var network = NetworkID.ToLower();
            dbPath = Path.Combine(dbPath, protocol);
            if (network != protocol) {
                dbPath = Path.Combine(dbPath, network);
            }
            dbPath = IOSecurity.GetFilteredPath(dbPath);
            if (!Directory.Exists(dbPath)) {
                Directory.CreateDirectory(dbPath);
            }

            var chatId = IOSecurity.GetFilteredFileName(ChatID.ToLower());
            dbPath = Path.Combine(dbPath, String.Format("{0}.db4o", chatId));
            return dbPath;
        }

        void OpenDatabase()
        {
#if DB4O_8_0
            Database = Db4oEmbedded.OpenFile(DatabaseConfiguration,
                                             DatabaseFile);
#else
            Database = Db4oFactory.OpenFile(DatabaseConfiguration,
                                            DatabaseFile);
#endif
        }

        void CloseDatabase()
        {
            Flush();
            FlushIndex();

            Database.Close();
            Database.Dispose();
        }

        void DefragDatabase()
        {
            if (!File.Exists(DatabaseFile)) {
                return;
            }

            var backupFile = String.Format(
                "{0}.bak_{1}.{2}",
                DatabaseFile,
                Db4oVersion.Major,
                Db4oVersion.Minor
            );
            var defragConfig = new DefragmentConfig(
                DatabaseFile,
                backupFile
            );
            defragConfig.ForceBackupDelete(true);
            Defragment.Defrag(defragConfig);
        }

        MessageModel GetMessage(MessageModel dbMsg)
        {
            Database.Activate(dbMsg, 5);
            var msg = new MessageModel(dbMsg);
            Database.Deactivate(dbMsg, 5);
            return msg;
        }

        void RestoreIndex()
        {
            var index = FetchIndex();
            if (index == null) {
#if LOG4NET
                Logger.Info("RestoreIndex(): Rebuilding index...");
#endif
                BuildIndex();
                FlushIndex();
                return;
            }

            f_Index = index;
        }

        List<MessageModel> FetchIndex()
        {
            DateTime start = DateTime.UtcNow, stop;
            var indexes = Database.Query<List<MessageModel>>();
            if (indexes.Count == 0) {
                return null;
            }
            if (indexes.Count > 1) {
                // we can't deal with multiple indexes, so drop them all
                foreach (var idx in indexes) {
                    Database.Activate(idx, 0);
                    Database.Delete(idx);
                }
                return null;
            }

            var index = indexes[0];
            Database.Activate(index, 1);
            var msgCount = Database.Query<MessageModel>().Count;
            if (index.Count != msgCount) {
#if LOG4NET
                Logger.Warn(
                    String.Format(
                        "FetchIndex(): index out of sync! index count: {0} " +
                        "vs message count: {1}",
                        index.Count, msgCount
                    )
                );
#endif
                Database.Delete(index);
                return null;
            }
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "FetchIndex(): query, activation and validation took: " +
                    "{0:0.00} ms, items: {1}",
                    (stop - start).TotalMilliseconds, index.Count
                )
            );
#endif
            return index;
        }

        void BuildIndex()
        {
            DateTime start = DateTime.UtcNow, stop;
            var query = Database.Query();
            query.Constrain(typeof(MessageModel));
            query.Descend("f_TimeStamp").OrderAscending();
            var index = query.Execute();
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "BuildIndex(): query took: {0:0.00} ms, items: {1}",
                    (stop - start).TotalMilliseconds, index.Count
                )
            );
#endif
            start = DateTime.UtcNow;
            var indexCapacity = Math.Max(index.Count, MaxCapacity);
            Index = new List<MessageModel>(indexCapacity);
            foreach (var msg in index) {
                Index.Add((MessageModel) msg);
            }
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "BuildIndex(): building index took: {0:0.00} ms",
                    (stop - start).TotalMilliseconds
                )
            );
#endif
        }

        void ResetIndex()
        {
            Index = null;
        }

        void FlushIndex()
        {
            DateTime start = DateTime.UtcNow, stop;
            Database.Store(f_Index);
            Database.Commit();
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "FlushIndex(): flushing index with {0} items took: {1} ms",
                    f_Index.Count, (stop - start).TotalMilliseconds
                )
            );
#endif
        }

        void Flush()
        {
            DateTime start = DateTime.UtcNow, stop;
            Database.Commit();
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "Flush(): flushing {0} items took: {1} ms",
                    FlushCounter, (stop - start).TotalMilliseconds
                )
            );
#endif
        }
    }
}
