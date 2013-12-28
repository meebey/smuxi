// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
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
using System.Text;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Smuxi.Engine
{
    public abstract class TorChatConnection
    {
        protected const char MSG_SEPARATOR = '\n';
        protected TcpClient TcpClient { get; set; }
        protected int SocketReceiveTimeout { get; set; }
        protected int SocketSendTimeout { get; set; }
        List<byte> IncompleteMessageBuffer { get; set; }

        public TorChatConnection()
        {
            SocketReceiveTimeout = 600;
            SocketSendTimeout = 600;
        }

        protected void BeginReadStream(NetworkStream stream)
        {
            var receiveBuffer = new byte[4096];
            stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length,
                             (ar) => {
                int bytesRead = stream.EndRead(ar);
                ReadBytes(receiveBuffer.Take((int) bytesRead));
            }, null);
        }

        protected void ReadBytes(IEnumerable<byte> receivedBytes)
        {
            var buffer = new List<byte>();
            if (IncompleteMessageBuffer != null) {
                buffer.AddRange(IncompleteMessageBuffer);
            }
            buffer.AddRange(receivedBytes);

            var msgs = new List<List<byte>>();
            var msg = new List<byte>(buffer.Count);
            for (int i = 0; i < buffer.Count; i++) {
                var value = buffer[i];
                if (value == MSG_SEPARATOR) {
                    msgs.Add(msg);
                    msg = new List<byte>(buffer.Count);
                    continue;
                }
                msg.Add(value);
            }
            // remaining unfinished message
            IncompleteMessageBuffer = msg;
            /*
            byte[] msg;
            do {
                msg = buffer.TakeWhile((@byte) => @byte != MSG_SEPARATOR);
                buffer = buffer.Skip(msg.Length);
            } while (buffer.Length > 0);
            */

            foreach (var cmdBytes in msgs) {
                ReadCommand(cmdBytes);
            }
        }

        void ReadCommand(IList<byte> bytes)
        {
            var cmdBytes = bytes.TakeWhile((arg) => arg != ' ').ToArray();
            var payloadBytes = bytes.Skip(cmdBytes.Length).ToArray();

            var cmd = DecodeString(cmdBytes);
            switch (cmd) {
                case "ping":
                    var payload = DecodeString(payloadBytes);
                    break;
            }
        }

        string DecodeString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        string DecodeMessage(string msg)
        {
            return msg.Replace(@"\r\n", @"\n").Replace(@"\n", "\n");
        }
    }
}
