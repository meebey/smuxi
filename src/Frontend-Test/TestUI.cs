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

using System;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.FrontendTest
{
    public class TestUI : PermanentRemoteObject, IFrontendUI 
    {
        private int _Version = 0;
        
        public int Version
        {
            get {
                return _Version;
            }
        }
        
        public void AddChat(ChatModel page)
        {
            Trace.Call(page);
            
            Console.WriteLine("New page: "+page.Name+ " type: "+page.ChatType);
            Frontend.ChangeActiveChat(page);
        }
        
        public void AddMessageToChat(ChatModel page, MessageModel msg)
        {
            Trace.Call(page, msg);

            string finalMsg = String.Empty;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
                // TODO: implement other types
                if (msgPart is TextMessagePartModel) {
                    TextMessagePartModel fmsgti = (TextMessagePartModel) msgPart;
                    finalMsg += fmsgti.Text;
                } 
            }
            
            string timestamp;
            try {
                timestamp = msg.TimeStamp.ToLocalTime().ToString((string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"]);
            } catch (FormatException e) {
                timestamp = "Timestamp Format ERROR: " + e.Message;
            }
            finalMsg = timestamp + " " + page.Name + " " + finalMsg;
            
            Console.WriteLine(finalMsg);
        }
        
        public void RemoveChat(ChatModel page)
        {
            Trace.Call(page);
            
            Console.WriteLine("Removed page: "+page.Name+" type: "+page.ChatType);
        }
        
        public void EnableChat(ChatModel page)
        {
            Trace.Call(page);
        }
        
        public void DisableChat(ChatModel page)
        {
            Trace.Call(page);
        }
        
        public void SyncChat(ChatModel page)
        {
            Trace.Call(page);
            
            Console.WriteLine("Synced page: "+page.Name+" type: "+page.ChatType);
            
            Frontend.FrontendManager.AddSyncedChat(page);
        }
        
        public void AddPersonToGroupChat(GroupChatModel cpage, PersonModel user)
        {
            Trace.Call(cpage, user);
        }
        
        public void UpdatePersonInGroupChat(GroupChatModel cpage, PersonModel olduser, PersonModel newuser)
        {
            Trace.Call(cpage, olduser, newuser);
        }
    
        public void UpdateTopicInGroupChat(GroupChatModel cpage, MessageModel topic)
        {
            Trace.Call(cpage, topic);
            
            Console.WriteLine("Topic changed to: " + topic.ToString() +  " on " + cpage.Name);
        }
        
        public void RemovePersonFromGroupChat(GroupChatModel cpage, PersonModel user)
        {
            Trace.Call(cpage, user);
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
