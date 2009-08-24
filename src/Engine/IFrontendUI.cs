/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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

namespace Smuxi.Engine
{
    public interface IFrontendUI
    {
        int Version {
            get;
        }
        
        void AddChat(ChatModel chat);
        void EnableChat(ChatModel chat);
        void DisableChat(ChatModel chat);
        void AddMessageToChat(ChatModel chat, MessageModel msg);
        void RemoveChat(ChatModel chat);
        void SyncChat(ChatModel chat);
        
        void AddPersonToGroupChat(GroupChatModel groupChat, PersonModel person);
        void UpdatePersonInGroupChat(GroupChatModel groupChat, PersonModel oldPerson, PersonModel newPerson);
        void UpdateTopicInGroupChat(GroupChatModel groupChat, MessageModel topic);
        void RemovePersonFromGroupChat(GroupChatModel groupChat, PersonModel person);
        
        void SetNetworkStatus(string status);
        void SetStatus(string status);
        
        // Presence?
        // File Transfer?
    }
}
