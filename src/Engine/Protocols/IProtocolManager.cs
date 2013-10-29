/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2008, 2010, 2011, 2013 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System;
using System.Collections.Generic;

namespace Smuxi.Engine
{
    public interface IProtocolManager : IDisposable
    {
        Session Session {
            get;
        }

        string NetworkID {
            get;
        }
        
        string Protocol {
            get;
        }
        
        string Host {
            get;
        }
        
        int Port {
            get;
        }
        
        bool IsConnected {
            get;
        }

        PersonModel Me {
            get;
        }

        ChatModel Chat {
            get;
        }
        
        IList<ChatModel> Chats {
            get;
        }
        
        PresenceStatus PresenceStatus {
            get;
        }

        void Connect(FrontendManager frontendManager, ServerModel server);
        void Disconnect(FrontendManager frontendManager);
        void Reconnect(FrontendManager frontendManager);
        
        bool Command(CommandModel command);
        string ToString();
        
        event EventHandler Connected;
        event EventHandler Disconnected;
        
        IList<GroupChatModel> FindGroupChats(GroupChatModel filter);
        void OpenChat(FrontendManager fm, ChatModel chat);
        void CloseChat(FrontendManager fm, ChatModel chat);

        void SetPresenceStatus(PresenceStatus status, string message);
    }
}
