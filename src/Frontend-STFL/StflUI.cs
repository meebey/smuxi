/*
 * $Id: TestUI.cs 179 2007-04-21 15:01:29Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-Test/TestUI.cs $
 * $Rev: 179 $
 * $Author: meebey $
 * $Date: 2007-04-21 17:01:29 +0200 (Sat, 21 Apr 2007) $
 *
 * smuxi - Smart MUltipleXed Irc
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

using System;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Stfl
{
    public class StflUI : PermanentRemoteObject, IFrontendUI 
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private int             _Version = 0;
        private ChatViewManager _ChatViewManager;
        
        public int Version {
            get {
                return _Version;
            }
        }
        
        public StflUI(ChatViewManager chatViewManager)
        {
            _ChatViewManager = chatViewManager;
        }
        
        public void AddChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            _ChatViewManager.AddChat(chat);
        }
        
        public void AddMessageToChat(ChatModel chat, MessageModel msg)
        {
            Trace.Call(chat, msg);

            ChatView chatView = _ChatViewManager.GetChat(chat);
#if LOG4NET
            if (chatView == null) {
                _Logger.Fatal(String.Format("AddMessageToChat(): _ChatViewManager.GetChat(chat) chat.Name: {0} returned null!", chat.Name));
                return;
            }
#endif
            chatView.AddMessage(msg);
        }
        
        public void RemoveChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            //Console.WriteLine("Removed page: "+page.Name+" type: "+page.ChatType);
        }
        
        public void EnableChat(ChatModel chat)
        {
            Trace.Call(chat);
        }
        
        public void DisableChat(ChatModel chat)
        {
            Trace.Call(chat);
        }
        
        public void SyncChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            //Console.WriteLine("Synced page: "+page.Name+" type: "+page.ChatType);
            // HACK: fake that we synced the chat, else we get no messages
            Frontend.FrontendManager.AddSyncedChat(chat);
        }
        
        public void AddPersonToGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            Trace.Call(groupChat, person);
        }
        
        public void UpdatePersonInGroupChat(GroupChatModel groupChat, PersonModel olduser, PersonModel newuser)
        {
            Trace.Call(groupChat, olduser, newuser);
        }
    
        public void UpdateTopicInGroupChat(GroupChatModel groupChat, string topic)
        {
            Trace.Call(groupChat, topic);
            
            //Console.WriteLine("Topic changed to: "+topic+ " on "+cpage.Name);
        }
        
        public void RemovePersonFromGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            Trace.Call(groupChat, person);
        }

        public void SetNetworkStatus(string status)
        {
            Trace.Call(status);
        }

        public void SetStatus(string status)
        {
            Trace.Call(status);
        }
    }
}
