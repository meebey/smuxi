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
        const string     LibraryTextDomain = "smuxi-engine";
        const int        DefaultFlushInterval = 16;
        List<Int64>      f_Index;
        int              FlushInterval { get; set; }
        int              FlushCounter { get; set; }
        IObjectContainer Database { get; set; }
        string           DatabaseFile { get; set; }
        bool             IsEmptyDatabase { get; set; }
        string           SessionUsername { get; set; }
        bool             AggressiveGC { get; set; }
#if DB4O_8_0
        IEmbeddedConfiguration DatabaseConfiguration { get; set; }
#else
        IConfiguration         DatabaseConfiguration { get; set; }
#endif

        private List<Int64> Index {
            get {
                InitIndex();
                return f_Index;
            }
        }

        public override MessageModel this[int index] {
            get {
                var dbId = Index[index];
                return GetMessage(dbId);
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

        private Db4oMessageBuffer()
        {
            FlushInterval = DefaultFlushInterval;
        }

        public Db4oMessageBuffer(string sessionUsername, string protocol,
                                 string networkId, string chatId) : this()
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

            AggressiveGC = true;
            DatabaseFile = GetDatabaseFile();
            InitDatabase();
        }

        public Db4oMessageBuffer(string dbPath) : this()
        {
            if (dbPath == null) {
                throw new ArgumentNullException("dbPath");
            }

            DatabaseFile = dbPath;
            InitDatabase();
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
            ResetIndex();
        }

        public override void Add(MessageModel item)
        {
            if (item == null) {
                throw new ArgumentNullException("item");
            }

            // make sure the index is initialized at this point else we will
            // load the 1st added item of db4o and end up with a duplicate here
            InitIndex();

            if (MaxCapacity > 0 && Index.Count >= MaxCapacity) {
                RemoveAt(0);
            }

            // TODO: auto-flush every 60 seconds
            var dbMsg = new MessageModel(item);
            Database.Store(dbMsg);
            Database.Deactivate(dbMsg, 5);
            var dbId = Database.Ext().GetID(dbMsg);
            Index.Add(dbId);
            FlushCounter++;
            if (FlushCounter >= FlushInterval) {
                Flush();
            }
        }

        public override void Clear()
        {
            foreach (var dbId in Index) {
                var dbMsg = Database.Ext().GetByID(dbId);
                Database.Delete(dbMsg);
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
            foreach (var dbId in Index) {
                yield return GetMessage(dbId);
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
            var dbMsg = (MessageModel) res[0];
            var dbId = Database.Ext().GetID(dbMsg);
            return Index.IndexOf(dbId);
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

            var dbId = Index[index];
            Index.RemoveAt(index);

            var dbMsg = Database.Ext().GetByID(dbId);
            if (dbMsg == null) {
#if LOG4NET
                Logger.Error(
                    String.Format("RemoveAt(): Database.Ext().GetByID({0}) " +
                                  "with index {1} returned null!", dbId, index)
                );
#endif
                return;
            }
            Database.Delete(dbMsg);
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
            var dbId = Database.Ext().GetID(item);
            Index.Remove(dbId);
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
            // Session.AddMessageToChat() and ChatModel.get_Messages lock()s
            // us though thus newly added messages are waiting for us to finish
            var bufferCount = Count;
            var rangeCount = Math.Min(bufferCount, limit);
            var range = new List<MessageModel>(rangeCount);
            for (int i = offset; i < offset + limit && i < bufferCount; i++) {
                range.Add(this[i]);
            }
            return range;
        }

        public static int OptimizeAllBuffers(Db4oMessageBufferOptimizationTypes opts)
        {
            DateTime start = DateTime.UtcNow, stop;
            var dbPath = Platform.GetBuffersBasePath();
            var dbFiles = Directory.GetFiles(dbPath, "*.db4o",
                                             SearchOption.AllDirectories);
            foreach (var dbFile in dbFiles) {
#if LOG4NET
                Logger.Info(String.Format(_("Optimizing: {0}..."), dbFile));
#endif
                try {
                    using (var buffer = new Db4oMessageBuffer(dbFile)) {
                        buffer.AggressiveGC = false;
                        if ((opts & Db4oMessageBufferOptimizationTypes.Defrag) != 0) {
                            buffer.CloseDatabase();
                            buffer.DefragDatabase();
                            buffer.InitDatabase();
                        }
                        if ((opts & Db4oMessageBufferOptimizationTypes.Index) != 0) {
                            buffer.RebuildIndex();
                        }
                    }
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Debug("OptimizeAllBuffers(): Failed to optimize: " +
                                 dbFile + " Exception: ", ex);
                    Logger.InfoFormat(_("Failed to optimize: {0}. Reason: {1}"),
                                      dbFile, ex.Message);
#endif
                }
            }
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "OptimizeAllBuffers(): optimizing buffers took: {0:0} second(s)",
                    (stop - start).TotalSeconds
                )
            );
#endif
            return dbFiles.Length;
        }

        private void InitDatabase()
        {
            ConfigureDatabase();
            try {
                OpenDatabase();
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("InitDatabase(): failed to open message " +
                             "database: " + DatabaseFile, ex);
#endif
                FixDatabase(ex);
                // WORXNOWPLX
                OpenDatabase();

                var builder = new MessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendErrorText(
                    _("Your chat history is no longer available because of " +
                      "an error but will be preserved from now on.")
                );
                Add(builder.ToMessage());
            }
        }

        void ConfigureDatabase()
        {
#if DB4O_8_0
            DatabaseConfiguration = Db4oEmbedded.NewConfiguration();
            DatabaseConfiguration.Common.AllowVersionUpdates = true;
            DatabaseConfiguration.Common.ActivationDepth = 0;
            //DatabaseConfiguration.Common.Queries.EvaluationMode(QueryEvaluationMode.Lazy);
            DatabaseConfiguration.Common.WeakReferenceCollectionInterval = 60 * 1000;
            //DatabaseConfiguration.Common.Diagnostic.AddListener(new DiagnosticToConsole());
            var msgConf = DatabaseConfiguration.Common.ObjectClass(typeof(MessageModel));
            msgConf.CascadeOnActivate(true);
            msgConf.CascadeOnDelete(true);
            msgConf.Indexed(true);
            msgConf.ObjectField("f_TimeStamp").Indexed(true);
#else
            DatabaseConfiguration = Db4oFactory.Configure();
            DatabaseConfiguration.AllowVersionUpdates(true);
            DatabaseConfiguration.ObjectClass(typeof(MessageModel)).
                                  ObjectField("f_TimeStamp").Indexed(true);
#endif
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
            IsEmptyDatabase = !File.Exists(DatabaseFile);
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
            if (Database.Ext().IsClosed()) {
                return;
            }

            Flush();
            FlushIndex();

            Database.Close();
            Database.Dispose();
        }

        void FixDatabase(Exception ex)
        {
            try {
                CloseDatabase();
            } catch {
            }

            // do some sanity checks before we assume the database is
            // really broken
            if (!File.Exists(DatabaseFile)) {
                throw new FileNotFoundException(DatabaseFile);
            }
            try {
                using (File.OpenWrite(DatabaseFile));
            } catch {
                throw;
            }

            var timestamp = DateTime.Now.ToString("s");
            timestamp = timestamp.Replace("T", "_").Replace(":", "_");
            var brokenDbFile = String.Format("{0}_{1}_broken",
                                           DatabaseFile, timestamp);
            var brokenLogFile = String.Format("{0}.log", brokenDbFile);
            File.WriteAllText(brokenLogFile, ex.ToString());
#if LOG4NET
            Logger.DebugFormat("FixDatabase(): moving broken database " +
                               "from: {0} to: {1}", DatabaseFile,
                               brokenDbFile);
#endif
            File.Move(DatabaseFile, brokenDbFile);

            ConfigureDatabase();
        }

        void DefragDatabase()
        {
            if (!File.Exists(DatabaseFile)) {
                return;
            }

            DateTime start = DateTime.UtcNow, stop;
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
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "DefragDatabase(): defrag took: {0:0.0} ms",
                    (stop - start).TotalMilliseconds
                )
            );
#endif
        }

        MessageModel GetMessage(MessageModel dbMsg)
        {
            Database.Activate(dbMsg, 10);
            var msg = new MessageModel(dbMsg);
            Database.Deactivate(dbMsg, 10);
            return msg;
        }

        MessageModel GetMessage(Int64 dbId)
        {
            var dbMsg = (MessageModel) Database.Ext().GetByID(dbId);
            return GetMessage(dbMsg);
        }

        void InitIndex()
        {
            if (f_Index != null) {
                return;
            }

            if (IsEmptyDatabase) {
#if LOG4NET
                Logger.Debug("InitIndex(): Creating initial index...");
#endif
                f_Index = new List<Int64>(MaxCapacity);
                return;
            }

            var index = FetchIndex();
            if (index == null) {
#if LOG4NET
                Logger.Info("InitIndex(): No index found, building...");
#endif
                RebuildIndex();
                return;
            }

            f_Index = index;
        }

        List<Int64> FetchIndex()
        {
            DateTime start = DateTime.UtcNow, stop;
            var indexes = Database.Query<List<Int64>>();
            if (indexes.Count == 0) {
                return null;
            }
            if (indexes.Count > 1) {
#if LOG4NET
                Logger.Warn(
                    "FetchIndex(): found multiple indexes, dropping them..."
                );
#endif
                // we can't deal with multiple indexes, so drop them all
                foreach (var idx in indexes) {
                    Database.Delete(idx);
                }
                Database.Commit();
                return null;
            }

            var index = indexes[0];
            Database.Activate(index, 10);
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

        List<Int64> BuildIndex()
        {
            DateTime start = DateTime.UtcNow, stop;
            var query = Database.Query();
            query.Constrain(typeof(MessageModel));
            query.Descend("f_TimeStamp").OrderAscending();
            var dbIndex = query.Execute();
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "BuildIndex(): query took: {0:0.00} ms, items: {1}",
                    (stop - start).TotalMilliseconds, dbIndex.Count
                )
            );
#endif
            start = DateTime.UtcNow;
            var indexCapacity = Math.Max(dbIndex.Count, MaxCapacity);
            var index = new List<Int64>(indexCapacity);
            int purgeCounter = 0;
            int purgeInterval = 1000;
            foreach (var dbMsg in dbIndex) {
                var dbId = Database.Ext().GetID(dbMsg);
                index.Add(dbId);
                if (purgeCounter++ >= purgeInterval) {
                    purgeCounter = 0;
                    if (AggressiveGC) {
                        Database.Ext().Purge();
                    }
                }
            }
            Database.Ext().Purge();
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "BuildIndex(): building index took: {0:0.00} ms",
                    (stop - start).TotalMilliseconds
                )
            );
#endif
            return index;
        }

        void RebuildIndex()
        {
            var indexes = Database.Query<List<Int64>>();
            if (indexes.Count > 0) {
                // we can't deal with multiple indexes, so drop them all
                foreach (var idx in indexes) {
                    Database.Delete(idx);
                }
                Database.Commit();
            }

            f_Index = BuildIndex();
            FlushIndex();
        }

        void ResetIndex()
        {
            f_Index = null;
        }

        void FlushIndex()
        {
            if (f_Index == null || f_Index.Count == 0) {
                // don't waste our time
                return;
            }

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
            var counter = FlushCounter;
            DateTime start = DateTime.UtcNow, stop;
            FlushCounter = 0;
            Database.Commit();
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "Flush(): flushing {0} items took: {1} ms",
                    counter, (stop - start).TotalMilliseconds
                )
            );
#endif
        }

        static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, LibraryTextDomain);
        }
    }

    [Flags]
    public enum Db4oMessageBufferOptimizationTypes : ushort {
        None   = ushort.MinValue,
        Defrag = 0x1,
        Index  = 0x2,
        All    = ushort.MaxValue
    }
}
