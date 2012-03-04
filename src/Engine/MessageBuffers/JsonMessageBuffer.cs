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
using Smuxi.Engine.Dto;

namespace Smuxi.Engine
{
    public class JsonMessageBuffer : MessageBufferBase
    {
#if LOG4NET
        static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        int DefaultMaxChunkSize { get; set; }
        int MaxChunkSize { get; set; }
        string ChunkBasePath { get; set; }
        Int64 FirstChunkOffset { get; set; }
        Int64 CurrentChunkOffset { get; set; }
        string CurrentChunkPath { get; set; }
        List<MessageDtoModelV1> CurrentChunk { get; set; }

        public override int Count {
            get {
                return (int) CurrentChunkOffset + CurrentChunk.Count;
            }
        }

        public override MessageModel this[int index] {
            get {
                return GetRange(index, 1).First();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public JsonMessageBuffer(string sessionUsername, string protocol,
                                 string networkId, string chatId) :
                            base(sessionUsername, protocol, networkId, chatId)
        {
            DefaultMaxChunkSize = 1000;
            MaxChunkSize = DefaultMaxChunkSize;
            ChunkBasePath = GetBufferPath() + ".v1.json";
            ScanChunks();
            CurrentChunk = new List<MessageDtoModelV1>(MaxChunkSize);
        }

        internal string GetChunkFileName(Int64 index)
        {
            if (index < 0 || index > Int64.MaxValue) {
                throw new ArgumentOutOfRangeException("index");
            }
            var start = (index / MaxChunkSize) * MaxChunkSize;
            var end = checked(start - 1 + MaxChunkSize);
            return String.Format("{0}-{1}.json", start, end);
        }

        void ScanChunks()
        {
            if (!Directory.Exists(ChunkBasePath)) {
                Directory.CreateDirectory(ChunkBasePath);
            }
            foreach (var filePath in Directory.GetFiles(ChunkBasePath, "*.json")) {
                var fileName = Path.GetFileName(filePath);
                var strNumber = fileName.Substring(0, fileName.IndexOf("."));
                var strStartNumber = strNumber.Split('-')[0];
                var strEndNumber = strNumber.Split('-')[1];
                var intStartNumber = 0L;
                var intEndNumber = 0L;
                // find first chunk
                Int64.TryParse(strStartNumber, out intStartNumber);
                Int64.TryParse(strEndNumber, out intEndNumber);
                if (intStartNumber < FirstChunkOffset) {
                    FirstChunkOffset = intStartNumber;
                }
                // find current (newest) chunk
                if (intStartNumber > CurrentChunkOffset) {
                    CurrentChunkOffset = intStartNumber;
                    MaxChunkSize = (int) (intEndNumber - intStartNumber + 1L);
                }
            }
            RefreshCurrentChunkPath();
            // load latest chunk
            CurrentChunk = LoadDtoChunk(CurrentChunkOffset);
        }

        void RefreshCurrentChunkPath()
        {
            CurrentChunkPath = Path.Combine(
                ChunkBasePath,
                GetChunkFileName(CurrentChunkOffset)
            );
        }

        void NextChunk()
        {
            Flush();
            var chunk = new List<MessageDtoModelV1>(MaxChunkSize);
            CurrentChunk = chunk;
            CurrentChunkOffset += MaxChunkSize;
            RefreshCurrentChunkPath();
        }

        void SerializeChunk(List<MessageDtoModelV1> chunk, TextWriter writer)
        {
            DateTime start, stop;
            start = DateTime.UtcNow;
            JsonSerializer.SerializeToWriter(chunk, writer);
            stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
            f_Logger.DebugFormat("SerializeChunk(): {0} items took: {1:0.00} ms",
                                 chunk.Count,
                                 (stop - start).TotalMilliseconds);
#endif
        }

        List<MessageDtoModelV1> DeserializeChunk(TextReader reader)
        {
            DateTime start, stop;
            start = DateTime.UtcNow;
            var chunk = JsonSerializer.DeserializeFromReader<List<MessageDtoModelV1>>(reader);
            stop = DateTime.UtcNow;
#if LOG4NET && MSGBUF_DEBUG
            f_Logger.DebugFormat("DeserializeChunk(): {0} items took: {1:0.00} ms",
                                 chunk.Count,
                                 (stop - start).TotalMilliseconds);
#endif
            return chunk;
        }

        List<MessageModel> LoadChunk(Int64 offset)
        {
            var chunk = new List<MessageModel>(MaxChunkSize);
            var chunkFileName = GetChunkFileName(offset);
            var chunkFilePath = Path.Combine(ChunkBasePath, chunkFileName);
            if (!File.Exists(chunkFilePath)) {
                return chunk;
            }
            using (var reader = File.OpenRead(chunkFilePath))
            using (var textReader = new StreamReader(reader)) {
                var dtoMsgs = DeserializeChunk(textReader);
                foreach (var dtoMsg in dtoMsgs) {
                    chunk.Add(dtoMsg.ToMessage());
                }
            }
            return chunk;
        }

        List<MessageDtoModelV1> LoadDtoChunk(Int64 offset)
        {
            var chunkFileName = GetChunkFileName(offset);
            var chunkFilePath = Path.Combine(ChunkBasePath, chunkFileName);
            if (!File.Exists(chunkFilePath)) {
                return new List<MessageDtoModelV1>(0);
            }
            using (var reader = File.OpenRead(chunkFilePath))
            using (var textReader = new StreamReader(reader)) {
                return DeserializeChunk(textReader);
            }
        }

        public override void Add(MessageModel item)
        {
            var chunk = CurrentChunk;
            if (chunk.Count >= MaxChunkSize) {
                NextChunk();
                chunk = CurrentChunk;
            }
            chunk.Add(new MessageDtoModelV1(item));
        }

        public override IList<MessageModel> GetRange(int offset, int limit)
        {
            var chunkMessages = new List<MessageModel>(MaxChunkSize * 2);
            chunkMessages.AddRange(LoadChunk(offset));
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
            // TODO: only write if chunk actually changed!
            // TODO: use compression?
            lock (CurrentChunk) {
                if (CurrentChunk.Count == 0) {
                    // don't write empty chunks to disk
                    return;
                }

                using (var writer = File.Open(CurrentChunkPath, FileMode.Create, FileAccess.Write))
                using (var textWriter = new StreamWriter(writer)) {
                    SerializeChunk(CurrentChunk, textWriter);
                }
            }
        }

        public override void Dispose()
        {
            Flush();
        }
    }
}
