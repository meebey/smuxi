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

namespace Smuxi.Engine
{
    public class MessageHookEnvironment : HookEnvironment
    {
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0);

        public MessageHookEnvironment(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            var nick = msg.GetNick();
            var message = msg.ToString();
            if (String.IsNullOrEmpty(nick)) {
                this["MSG"] = message;
            } else {
                this["MSG"] = message.Substring(nick.Length + 3);
            }

            var timestamp = (Int64) (msg .TimeStamp - UnixEpoch).TotalSeconds;
            this["MSG_TIMESTAMP_UNIX"] = timestamp.ToString();
            this["MSG_TIMESTAMP_ISO_UTC"] = msg.TimeStamp.ToString("u").Replace('Z', ' ').TrimEnd();
            this["MSG_TIMESTAMP_ISO_LOCAL"] = msg.TimeStamp.ToLocalTime().ToString("u").Replace('Z', ' ').TrimEnd();
        }
    }
}
