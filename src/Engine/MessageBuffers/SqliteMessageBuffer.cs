// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using Mono.Data.Sqlite;

namespace Smuxi.Engine
{
    public class SqliteMessageBuffer
    {
        private SqliteConnection Connection { get; set; }
        public string Protocol { get; private set; }
        public string NetworkID { get; private set; }
        public string ChatID { get; private set; }

        public MessageModel this[int index] {
            get {
                throw new System.NotImplementedException();
            }
            set {
                throw new System.NotImplementedException();
            }
        }

        public int Count {
            get {
                var cmd = Connection.CreateCommand();
                cmd.CommandText =
                    "SELECT COUNT(*)" +
                    " FROM Messages" +
                    " WHERE" +
                    " Protocol = @protocol " +
                    " NetworkID = @network " +
                    " ChatID = @chat";

                var param = cmd.CreateParameter();
                param.ParameterName = "protocol";
                param.Value = Protocol;

                param = cmd.CreateParameter();
                param.ParameterName = "network";
                param.Value = NetworkID;

                param = cmd.CreateParameter();
                param.ParameterName = "chat";
                param.Value = ChatID;

                return (int) Convert.ChangeType(cmd.ExecuteScalar(), typeof(int));
            }
        }

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        public SqliteMessageBuffer(SqliteConnection connection)
        {
            if (connection == null) {
                throw new ArgumentNullException("connection");
            }

            Connection = connection;
        }

        public void Add(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            var cmd = Connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Messages (ID, Text)" +
                              " VALUES(DEFAULT, @text)";
            var param = cmd.CreateParameter();
            param.ParameterName = "text";
            param.Value = msg.ToString();

            cmd.ExecuteNonQuery();
        }

        public void Clear()
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Messages";
            cmd.ExecuteNonQuery();
        }

        public bool Contains(MessageModel msg)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(MessageModel[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove (MessageModel item)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<MessageModel> GetEnumerator ()
        {
            throw new System.NotImplementedException();
        }

        public int IndexOf (MessageModel item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert (int index, MessageModel item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt (int index)
        {
            throw new System.NotImplementedException();
        }
    }
}
