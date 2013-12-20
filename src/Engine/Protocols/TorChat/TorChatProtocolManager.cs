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
using System.Linq;
using System.IO;

#if TOR && DISABLED
namespace Smuxi.Engine
{
    public class TorChatProtocolManager : ProtocolManagerBase
    {
        const char MSG_SEPARATOR = '\n';

        public TorChatProtocolManager(Session session) : base(session)
        {
        }

        void OnReceived(byte[] buffer)
        {
            foreach (byte @byte in buffer) {
                byte[] msg;

            }
            do {
                msg = buffer.TakeWhile((@byte) => @byte != MSG_SEPARATOR);
            } while (msg.Length > 0);
        }

        void DecodeMessage(string msg)
        {
            return msg.Replace(@"\r\n", @"\n").Replace(@"\n", "\n");
        }
    }
}
#endif