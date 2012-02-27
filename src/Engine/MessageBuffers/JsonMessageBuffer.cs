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
                throw new NotImplementedException();
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

        string GetChunkFileName(Int64 index)
        {
            return null;
        }

        void ScanChunks()
        {
            if (!Directory.Exists(ChunkBasePath)) {
                Directory.CreateDirectory(ChunkBasePath);
            }
            foreach (var filename in Directory.GetFiles(ChunkBasePath, "*.json")) {
                var strNumber = filename.Substring(0, filename.IndexOf("."));
                var strStartNumber = strNumber.Split('-')[0];
                var strEndNumber = strNumber.Split('-')[1];
                var intStartNumber = 0L;
                var intEndNumber = 0L;
                // find first chunk
                Int64.TryParse(strStartNumber, out intStartNumber);
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
        }

        void RefreshCurrentChunkPath()
        {
            CurrentChunkPath = Path.Combine(
                ChunkBasePath,
                String.Format(
                    "{0}-{1}.json",
                    CurrentChunkOffset,
                    CurrentChunkOffset + MaxChunkSize - 1
                )
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
            JsonSerializer.SerializeToWriter(chunk, writer);
        }

        List<MessageDtoModelV1> DeserializeChunk(Stream chunkStream)
        {
            return JsonSerializer.DeserializeFromStream<List<MessageDtoModelV1>>(chunkStream);
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
            // TODO: use compression?
            lock (CurrentChunk) {
                using (var writer = File.OpenWrite(CurrentChunkPath))
                using (var textWriter = new StreamWriter(writer, Encoding.UTF8)) {
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
