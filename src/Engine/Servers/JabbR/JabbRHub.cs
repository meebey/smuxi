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

#if JABBR_SERVER
using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR;

namespace Smuxi.Engine.JabbR
{
    public class JabbRHub : Hub
    {
        public JabbRHub()
        {
        }

        public void Join()
        {
        }

        public void Join(bool reconnecting)
        {
        }

        // LogOut???
        /*
        public Task LogOut()
        {
            return _chat.Invoke("LogOut");
        }
        */

        public bool Send(string content, string roomName)
        {
            return false;
        }

        public bool Send(ClientMessage clientMessage)
        {
            return false;
        }

        //public UserViewModel GetUserInfo()
        public User GetUserInfo()
        {
            return null;
        }

        //public IEnumerable<LobbyRoomViewModel> GetRooms()
        public IEnumerable<Room> GetRooms()
        {
            return null;
        }

        //public RoomViewModel GetRoomInfo(string roomName)
        public Room GetRoomInfo(string roomName)
        {
            return null;
        }

        //public IEnumerable<MessageViewModel> GetPreviousMessages(string messageId)
        public IEnumerable<Message> GetPreviousMessages(string messageId)
        {
            return null;
        }

        public void PostNotification(ClientNotification notification)
        {
        }

        public void PostNotification(ClientNotification notification, bool executeContentProviders)
        {
        }

        private void CheckStatus()
        {
        }

        public void Typing(string roomName)
        {
        }
    }
}
#endif
