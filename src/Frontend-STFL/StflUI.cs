/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007, 2011, 2013 Mirco Bauer <meebey@meebey.net>
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
            
            try {
                _ChatViewManager.AddChat(chat);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
        }

        public void AddMessageToChat(ChatModel chat, MessageModel msg)
        {
            Trace.Call(chat, msg);

            try {
                ChatView chatView = _ChatViewManager.GetChat(chat);
                if (chatView == null) {
#if LOG4NET
                    _Logger.Fatal(String.Format("AddMessageToChat(): _ChatViewManager.GetChat(chat) chat.Name: {0} returned null!", chat.Name));
#endif
                    return;
                }

                // FIXME: this must be marshalled into the UI thread!
                chatView.AddMessage(msg);
                _ChatViewManager.UpdateNavigation();
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
        }
        
        public void RemoveChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            try {
                _ChatViewManager.RemoveChat(chat);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
        }
        
        public void EnableChat(ChatModel chat)
        {
            Trace.Call(chat);

            try {
                _ChatViewManager.EnableChat(chat);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
        }
        
        public void DisableChat(ChatModel chat)
        {
            Trace.Call(chat);

            try {
                _ChatViewManager.DisableChat(chat);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
        }
        
        public void SyncChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            try {
                var chatView = _ChatViewManager.GetChat(chat);
                if (chatView == null) {
#if LOG4NET
                    _Logger.Fatal(String.Format("SyncChat(): _ChatViewManager.GetChat(chat) chat.Name: {0} returned null!", chat.Name));
#endif
                    return;
                }
                chatView.Sync();
                if (_ChatViewManager.CurrentChat == chatView) {
                    _ChatViewManager.UpdateInput();
                }

                Frontend.FrontendManager.AddSyncedChat(chat);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
        }
        
        public void AddPersonToGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            Trace.Call(groupChat, person);

            try {
                var chatView = _ChatViewManager.GetChat(groupChat);
                if (chatView == null) {
#if LOG4NET
                    _Logger.Fatal(String.Format("AddPersonToGroupChat(): _ChatViewManager.GetChat(chat) chat.Name: {0} returned null!", groupChat.Name));
#endif
                    return;
                }

                lock (chatView.Participants) {
                    chatView.Participants.Add(person);
                }
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
        }
        
        public void UpdatePersonInGroupChat(GroupChatModel groupChat, PersonModel oldPerson, PersonModel newPerson)
        {
            Trace.Call(groupChat, oldPerson, newPerson);

            try {
                var chatView = _ChatViewManager.GetChat(groupChat);
                if (chatView == null) {
#if LOG4NET
                    _Logger.Fatal(String.Format("UpdatePersonInGroupChat(): _ChatViewManager.GetChat(groupChat) groupChat.Name: {0} returned null!", groupChat.Name));
#endif
                    return;
                }

                lock (chatView.Participants) {
                    chatView.Participants.Remove(oldPerson);
                    chatView.Participants.Add(newPerson);
                }
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
        }
    
        public void UpdateTopicInGroupChat(GroupChatModel groupChat, MessageModel topic)
        {
            Trace.Call(groupChat, topic);

            try {
                var chatView = _ChatViewManager.GetChat(groupChat);
                if (chatView == null) {
#if LOG4NET
                    _Logger.Fatal(String.Format("UpdateTopicInGroupChat(): _ChatViewManager.GetChat(groupChat) groupChat.Name: {0} returned null!", groupChat.Name));
#endif
                    return;
                }

                if (!(chatView is GroupChatView)) {
#if LOG4NET
                    _Logger.Fatal(String.Format("UpdateTopicInGroupChat(): _ChatViewManager.GetChat(groupChat) groupChat.Name: {0} returned something that isn't a group chat view!", groupChat.Name));
#endif
                    return;
                }

                var groupChatView = (GroupChatView) chatView;

                groupChatView.Topic = topic;

                _ChatViewManager.UpdateTopic();
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
        }
        
        public void RemovePersonFromGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            Trace.Call(groupChat, person);

            try {
                var chatView = _ChatViewManager.GetChat(groupChat);
                if (chatView == null) {
#if LOG4NET
                    _Logger.Fatal(String.Format("RemovePersonFromGroupChat(): _ChatViewManager.GetChat(groupChat) groupChat.Name: {0} returned null!", groupChat.Name));
#endif
                    return;
                }

                lock (chatView.Participants) {
                    chatView.Participants.Remove(person);
                }
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Fatal(ex);
#endif
            }
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
