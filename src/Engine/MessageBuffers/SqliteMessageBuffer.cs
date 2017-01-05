// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014-2015 Mirco Bauer <meebey@meebey.net>
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
        Int64 MessageCount { get; set; }

        public override int Count {
            get {
                var connection = Connection;
                if (connection == null) {
                    return 0;
                }
                using (var cmd = connection.CreateCommand()) {
                    cmd.CommandText = "SELECT COUNT(*) FROM Messages";
                    return (int) Convert.ChangeType(cmd.ExecuteScalar(), typeof(int));
                }
            }
        }

        public override DateTime LastSeenMessage {
            get {
                var connection = Connection;
                if (connection == null) {
                    return DateTime.MinValue;
                }
                using (var cmd = connection.CreateCommand()) {
                    cmd.CommandText = "SELECT Value FROM Properties WHERE Key = 'LastSeenMessage'";
                    var value = cmd.ExecuteScalar();
                    if (value == null) {
                        return DateTime.MinValue;
                    }
                    return DateTime.Parse((string) value).ToUniversalTime();
                }
            }
            set {
                var connection = Connection;
                if (connection == null) {
                    return;
                }
                using (var cmd = connection.CreateCommand()) {
                    var sql = "INSERT OR REPLACE INTO Properties (Key, Value) " +
                              "VALUES('LastSeenMessage', @timestamp)";
                    cmd.CommandText = sql;
                    var param = cmd.CreateParameter();
                    param.ParameterName = "timestamp";
                    if (value.Kind == DateTimeKind.Unspecified) {
                        // HACK: on Mono the DateTimeKind gets lost during
                        // serialization of .NET remoting. When this happens we
                        // store the timestamp in local time instead. Otherwise
                        // the timezone offset will be applied _again_ leading
                        // to incorrect values, see:
                        // https://smuxi.im/issues/show/1058
                        param.Value = value.ToString("o");
                    } else {
                        param.Value = value.ToUniversalTime().ToString("o");
                    }
                    cmd.Parameters.Add(param);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override DateTime LastSeenHighlight {
            get {
                var connection = Connection;
                if (connection == null) {
                    return DateTime.MinValue;
                }
                using (var cmd = connection.CreateCommand()) {
                    cmd.CommandText = "SELECT Value FROM Properties WHERE Key = 'LastSeenHighlight'";
                    var value = cmd.ExecuteScalar();
                    if (value == null) {
                        return DateTime.MinValue;
                    }
                    return DateTime.Parse((string) value).ToUniversalTime();
                }
            }
            set {
                var connection = Connection;
                if (connection == null) {
                    return;
                }
                using (var cmd = connection.CreateCommand()) {
                    var sql = "INSERT OR REPLACE INTO Properties (Key, Value) " +
                              "VALUES('LastSeenHighlight', @timestamp)";
                    cmd.CommandText = sql;
                    var param = cmd.CreateParameter();
                    param.ParameterName = "timestamp";
                    if (value.Kind == DateTimeKind.Unspecified) {
                        // HACK: on Mono the DateTimeKind gets lost during
                        // serialization of .NET remoting. When this happens we
                        // store the timestamp in local time instead. Otherwise
                        // the timezone offset will be applied _again_ leading
                        // to incorrect values, see:
                        // https://smuxi.im/issues/show/1058
                        param.Value = value.ToString("o");
                    } else {
                        param.Value = value.ToUniversalTime().ToString("o");
                    }
                    cmd.Parameters.Add(param);
                    cmd.ExecuteNonQuery();
                }
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
            Init();
        }

        public SqliteMessageBuffer(string dbPath)
        {
            if (dbPath == null) {
                throw new ArgumentNullException("dbPath");
            }

            DBPath = dbPath;
            Init();
        }

        void Init()
        {
            Connection = new SqliteConnection(
                //extra double-quotes are needed to prevent conflicting connectionString chars in the path such as ','
                "Data Source=\"" + DBPath + "\";" +
                // enable Write-Ahead-Log (WAL)
                "Journal Mode=WAL"
            );
            Connection.Open();

            using (var cmd = Connection.CreateCommand()) {
                var sql = "CREATE TABLE IF NOT EXISTS Messages (" +
                              "ID INTEGER PRIMARY KEY," +
                              "JSON TEXT" +
                          ")";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            using (var cmd = Connection.CreateCommand()) {
                var sql = "CREATE TABLE IF NOT EXISTS Properties (" +
                              "Key TEXT PRIMARY KEY," +
                              "Value TEXT" +
                          ")";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            MessageCount = Count;
        }

        public override void Add(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            if (MaxCapacity > 0 && MessageCount >= MaxCapacity) {
                RemoveAt(0);
            }

            var dto = new MessageDtoModelV2(msg);
            var json = JsonSerializer.SerializeToString(dto);

            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "INSERT INTO Messages (JSON)" +
                                  " VALUES(@json)";
                var param = cmd.CreateParameter();
                param.ParameterName = "json";
                param.Value = json;
                cmd.Parameters.Add(param);

                cmd.ExecuteNonQuery();
            }
            MessageCount++;
        }

        public override IList<MessageModel> GetRange(int offset, int limit)
        {
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "SELECT JSON FROM Messages " +
                                  " ORDER BY ID " +
                                  " LIMIT @limit OFFSET @offset";

                var param = cmd.CreateParameter();
                param.ParameterName = "offset";
                param.Value = offset.ToString();
                cmd.Parameters.Add(param);

                param = cmd.CreateParameter();
                param.ParameterName = "limit";
                param.Value = limit.ToString();
                cmd.Parameters.Add(param);

                using (var reader = cmd.ExecuteReader()) {
                    var msgs = new List<MessageModel>(limit);
                    while (reader.Read()) {
                        var json = (string) reader["JSON"];
                        var dto = JsonSerializer.DeserializeFromString<MessageDtoModelV2>(json);
                        var msg = dto.ToMessage();
                        msgs.Add(msg);
                    }
                    return msgs;
                }
            }
        }

        public override void Clear()
        {
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "DELETE FROM Messages";
                cmd.ExecuteNonQuery();
            }
        }

        public override bool Contains(MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override bool Remove(MessageModel item)
        {
            throw new NotImplementedException ();
        }

        public override IEnumerator<MessageModel> GetEnumerator()
        {
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "SELECT JSON FROM Messages";

                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        var json = (string) reader["JSON"];
                        var dto = JsonSerializer.DeserializeFromString<MessageDtoModelV1>(json);
                        yield return dto.ToMessage();
                    }
                }
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

        public override void RemoveAt(int offset)
        {
            int id = -1;
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "SELECT ID FROM Messages " +
                                  " ORDER BY ID " +
                                  " LIMIT 1 OFFSET @offset";

                var param = cmd.CreateParameter();
                param.ParameterName = "offset";
                param.Value = offset;
                cmd.Parameters.Add(param);

                id = (int) Convert.ChangeType(cmd.ExecuteScalar(), typeof(int));
            }

            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = "DELETE FROM Messages WHERE ID = @id";

                var param = cmd.CreateParameter();
                param.ParameterName = "id";
                param.Value = id;
                cmd.Parameters.Add(param);

                cmd.ExecuteNonQuery();
            }
        }

        public override void Flush()
        {
        }

        public override void Dispose()
        {
            var connection = Connection;
            if (connection == null) {
                return;
            }

            Flush();
            Connection = null;

            connection.Close();
            connection.Dispose();
        }
    }
}
