// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 Mirco Bauer <meebey@meebey.net>
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
using Mono.Data.Sqlite;
using ServiceStack.Text;
using Smuxi.Engine.Dto;

namespace Smuxi.Engine
{
    public class SqliteMessageBuffer : MessageBufferBase
    {
#if LOG4NET
        static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        string DBPath { get; set; }
        SqliteConnection Connection { get; set; }

        public override int Count {
            get {
                var cmd = Connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Messages";
                return (int) Convert.ChangeType(cmd.ExecuteScalar(), typeof(int));
            }
        }

        public override MessageModel this[int offset] {
            get {
                return GetRange(offset, 1).First();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public SqliteMessageBuffer(string sessionUsername, string protocol,
                                   string networkId, string chatId) :
                              base(sessionUsername, protocol, networkId, chatId)
        {
            DBPath = GetBufferPath() + ".sqlite3";
            Connection = new SqliteConnection("URI=file:" + DBPath);
            Connection.Open();

            var sql = "CREATE TABLE IF NOT EXISTS Messages (" +
                          "ID INTEGER PRIMARY KEY," +
                          "JSON TEXT" +
                      ")";
            var cmd = Connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public override void Add(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            var dto = new MessageDtoModelV1(msg);
            var json = JsonSerializer.SerializeToString(dto);

            var cmd = Connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Messages (JSON)" +
                              " VALUES(@json)";
            var param = cmd.CreateParameter();
            param.ParameterName = "json";
            param.Value = json;
            cmd.Parameters.Add(param);

            cmd.ExecuteNonQuery();
        }

        public override IList<MessageModel> GetRange(int offset, int limit)
        {
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "SELECT ID, JSON FROM Messages " +
                                  " ORDER BY ID" +
                                  " LIMIT @limit OFFSET @offset ";
                var param = cmd.CreateParameter();
                param.ParameterName = "offset";
                param.Value = offset.ToString();
                cmd.Parameters.Add(param);

                param = cmd.CreateParameter();
                param.ParameterName = "limit";
                param.Value = limit.ToString();
                cmd.Parameters.Add(param);

                var reader = cmd.ExecuteReader();
                var msgs = new List<MessageModel>(limit);
                while (reader.Read()) {
                    var json = (string) reader["JSON"];
                    var dto = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
                    var msg = dto.ToMessage();
                    msgs.Add(msg);
                }
                return msgs;
            }
        }

        public override void Clear()
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Messages";
            cmd.ExecuteNonQuery();
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
        }

        public override void Dispose()
        {
            Flush();
        }
    }
}
