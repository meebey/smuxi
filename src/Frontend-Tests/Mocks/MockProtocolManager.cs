// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer
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
using Smuxi.Engine;

namespace Smuxi.Frontend
{
    public class MockProtocolManager : IProtocolManager
    {
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public Session Session {
            get {
                return null;
            }
        }

        public string NetworkID {
            get {
                return "MockProtocolManagerNetwork";
            }
        }

        public string Protocol {
            get {
                return "MockProtocolManagerProtocol";
            }
        }

        public string Host {
            get {
                return "MockProtocolManagerHost";
            }
        }

        public int Port {
            get {
                return 1337;
            }
        }

        public bool IsConnected {
            get {
                return true;
            }
        }

        public PersonModel Me {
            get {
                return null;
            }
        }

        public ChatModel Chat {
            get {
                return null;
            }
        }

        public IList<ChatModel> Chats {
            get {
                return new List<ChatModel>();
            }
        }

        public PresenceStatus PresenceStatus {
            get {
                return PresenceStatus.Online;
            }
        }

        public void Connect(FrontendManager frontman, ServerModel srv) {
        }

        public void Disconnect(FrontendManager frontman) {
        }

        public void Reconnect(FrontendManager frontman) {
        }

        public bool Command(CommandModel cmd) {
            return false;
        }

        public IList<GroupChatModel> FindGroupChats(GroupChatModel filter) {
            return new List<GroupChatModel>();
        }

        public void OpenChat(FrontendManager frontman, ChatModel chat) {
        }

        public void CloseChat(FrontendManager frontman, ChatModel chat) {
        }

        public void SetPresenceStatus(PresenceStatus status, string message) {
        }

        public void Dispose() {
        }
    }
}

