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
        IObjectSet       f_Index;
        IObjectContainer Database { get; set; }
        string           DatabaseFile { get; set; }
        string           SessionUsername { get; set; }
#if DB4O_8_0
        IEmbeddedConfiguration DatabaseConfiguration { get; set; }
#else
        IConfiguration         DatabaseConfiguration { get; set; }
#endif

        private IObjectSet Index {
            get {
                if (f_Index == null) {
                    BuildIndex();
                }
                return f_Index;
            }
            set {
                f_Index = value;
            }
        }

        public override MessageModel this[int index] {
            get {
                //return (MessageModel) Database.Ext().GetByID(index);
                return (MessageModel) Index[index];
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

            DatabaseFile = GetDatabaseFile();
#if DB4O_8_0
            DatabaseConfiguration = Db4oEmbedded.NewConfiguration();
            DatabaseConfiguration.Common.AllowVersionUpdates = true;
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
            var db = Database;
            if (db == null) {
                return;
            }
            Database = null;
            Index = null;

            db.Commit();
            db.Close();
            db.Dispose();
        }

        public override void Add(MessageModel item)
        {
            if (MaxCapacity > 0 && Count >= MaxCapacity) {
                RemoveAt(0);
            }

            // TODO: commit every 60 seconds
            Database.Store(item);

            ResetIndex();
        }

        public override void Clear()
        {
            foreach (var msg in this) {
                Database.Delete(msg);
            }

            ResetIndex();
        }

        public override bool Contains(MessageModel item)
        {
            // TODO: benchmark me!
            //return Database.Query<MessageModel>().Contains(item);
            return IndexOf(item) != -1;
        }

        public override void CopyTo(MessageModel[] array, int arrayIndex)
        {
            int i = arrayIndex;
            foreach (var msg in this) {
                array[i++] = msg;
            }
        }

        public override IEnumerator<MessageModel> GetEnumerator()
        {
            foreach (var msg in Index) {
               yield return (MessageModel) msg;
            }
        }

        public override int IndexOf(MessageModel item)
        {
            // TODO: benchmark me!
            /*
            var res = Database.Query<MessageModel>(delegate(MessageModel match) {
                return match.Equals(item);
            });
            */
            // TODO: use TimeStamp based hashtable as optimization?
            var res = Database.QueryByExample(item);
            // return -1 if not found
            if (res.Count == 0) {
                return -1;
            }
            return Index.IndexOf(res[0]);
        }

        public override void Insert(int index, MessageModel item)
        {
            throw new NotSupportedException();
        }

        public override void RemoveAt(int index)
        {
            Database.Delete(Index[index]);
        }

        public override bool Remove(MessageModel item)
        {
            if (!Contains(item)) {
                return false;
            }

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

        void BuildIndex()
        {
            DateTime start = DateTime.UtcNow, stop;
            // EXTREMELY SLOW (probably evaluated)
            /*
            var msgs = Database.Query<MessageModel>(
                delegate(MessageModel msg) {
                    return true;
                },
                delegate(MessageModel first, MessageModel second) {
                    return first.TimeStamp.CompareTo(second.TimeStamp);
                }
            );
            */
            var query = Database.Query();
            query.Constrain(typeof(MessageModel));
            query.Descend("f_TimeStamp");
            var index = query.Execute();
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.Debug(
                String.Format(
                    "BuildIndex(): query took: {0:0.00} ms",
                    (stop - start).TotalMilliseconds
                )
            );
#endif
            Index = index;
        }

        void ResetIndex()
        {
            Index = null;
        }
    }
}
