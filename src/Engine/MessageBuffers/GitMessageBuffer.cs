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
#if LIBGIT2SHARP
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using ServiceStack.Text;
using LibGit2Sharp;
using Smuxi.Common;
using Smuxi.Engine.Dto;

namespace Smuxi.Engine
{
    public class GitMessageBuffer : MessageBufferBase
    {
#if LOG4NET
        static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        Int64 f_MessageNumber = -1;
        Repository Repository { get; set; }
        string RepositoryPath { get; set; }
        TimeSpan CommitInterval { get; set; }
        Timer CommitTimer { get; set; }
        StringBuilder CommitMessage { get; set; }

        Int64 MessageNumber {
            get {
                if (f_MessageNumber == -1) {
                    var msgNumber = 0L;
                    var reference = Repository.Refs["refs/heads/master"] as DirectReference;
                    if (reference != null) {
                        var commit = reference.Target as Commit;
                        foreach (var treeEntry in commit.Tree) {
                            var filename = treeEntry.Name;
                            var strNumber = filename.Substring(0, filename.IndexOf("."));
                            var intNumber = 0L;
                            Int64.TryParse(strNumber, out intNumber);
                            if (intNumber > msgNumber) {
                                msgNumber = intNumber;
                            }
                        }
                    }
                    f_MessageNumber = msgNumber;
                }
                return f_MessageNumber;
            }
            set {
                f_MessageNumber = value;
            }
        }

        static GitMessageBuffer() {
            JsConfig<MessagePartModel>.ExcludeTypeInfo = true;
        }

        public GitMessageBuffer(string sessionUsername, string protocol,
                                string networkId, string chatId) :
                           base(sessionUsername, protocol, networkId, chatId)
        {
            var bufferPath = GetBufferPath();
            RepositoryPath = bufferPath + ".git";
            if (!Directory.Exists(RepositoryPath)) {
                Repository.Init(RepositoryPath, false);
            }
            Repository = new Repository(RepositoryPath);

            CommitMessage = new StringBuilder(1024);
            CommitInterval = TimeSpan.FromMinutes(1);
            CommitTimer = new Timer(delegate { Flush(); }, null,
                                    CommitInterval, CommitInterval);
        }

        public override void Add(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            /*
            if (MaxCapacity > 0 && Index.Count >= MaxCapacity) {
                RemoveAt(0);
            }
            */

            var msgFileName = String.Format("{0}.v1.json", MessageNumber++);
            var msgFilePath = Path.Combine(RepositoryPath, msgFileName);
            using (var writer = File.OpenWrite(msgFilePath))
            using (var textWriter = new StreamWriter(writer, Encoding.UTF8)) {
                JsonSerializer.SerializeToWriter(msg, textWriter);
            }

            DateTime start, stop;
            lock (Repository) {
                start = DateTime.UtcNow;
                var index = Repository.Index;
                //index.Stage(msgFilePath);
                // OPT: Stage() writes the index to disk on each call
                index.AddToIndex(msgFileName);
                stop = DateTime.UtcNow;
            }
#if LOG4NET && MSGBUF_DEBUG
            f_Logger.DebugFormat("Add(): Index.AddToIndex() took: {0:0.00} ms",
                                 (stop - start).TotalMilliseconds);
#endif

            // FIXME: delete file when index was written to disk
            File.Delete(msgFilePath);

            lock (CommitMessage) {
                CommitMessage.Append(
                    String.Format("{0}: {1}\n", msgFileName, msg.ToString())
                );
            }

            // TODO: create tree, commit tree, repack repo reguraly (see rugged docs)
        }

        #region implemented abstract members of Smuxi.Engine.MessageBufferBase
        public override int Count {
            get {
                return Repository.Index.Count;
            }
        }

        public override MessageModel this[int index] {
            get {
                if (index < 0) {
                    throw new IndexOutOfRangeException();
                }
                var fileName = String.Format("{0}.v1.json", index);
                var entry = Repository.Index[fileName];
                if (entry == null) {
                    throw new IndexOutOfRangeException();
                }
                return GetMessage(entry.Id);
            }
            set {
                throw new NotImplementedException();
            }
        }

        MessageModel GetMessage(ObjectId id)
        {
            if (id == null) {
                throw new ArgumentNullException("id");
            }
            var blob = Repository.Lookup<Blob>(id);
            if (blob == null) {
                throw new ArgumentOutOfRangeException("id", id, "ObjectId not found");
            }
            var dto = JsonSerializer.DeserializeFromStream<MessageDtoModelV1>(blob.ContentStream);
            return dto.ToMessage();
        }

        public override void Clear ()
        {
            throw new NotImplementedException ();
        }

        public override bool Contains (MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override void CopyTo (MessageModel[] array, int arrayIndex)
        {
            throw new NotImplementedException ();
        }

        public override bool Remove (MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override IEnumerator<MessageModel> GetEnumerator()
        {
            foreach (var entry in Repository.Index) {
                yield return GetMessage(entry.Id);
            }
        }

        public override int IndexOf (MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override void Insert (int index, MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override void RemoveAt (int index)
        {
            throw new NotImplementedException ();
        }

        public override void Dispose()
        {
            Flush();

            var repo = Repository;
            if (repo != null) {
                Repository = null;
                repo.Dispose();
            }
        }
        #endregion

        public override void Flush()
        {
            Trace.Call();

            var repo = Repository;
            if (repo == null) {
                return;
            }
            lock (repo)
            lock (CommitMessage) {
                if (CommitMessage.Length == 0) {
                    // nothing to commit
                    return;
                }

                DateTime start, stop;

                start = DateTime.UtcNow;
                repo.Index.UpdatePhysicalIndex();
                stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
                f_Logger.DebugFormat("Commit(): Repository.Index.UpdatePhysicalIndex() took: {0:0.00} ms",
                                     (stop - start).TotalMilliseconds);
#endif

                start = DateTime.UtcNow;
                repo.Commit(CommitMessage.ToString(), false);
                stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
                f_Logger.DebugFormat("Commit(): Repository.Commit() took: {0:0.00} ms",
                                     (stop - start).TotalMilliseconds);
#endif

                CommitMessage.Clear();
            }
        }
    }
}
#endif
