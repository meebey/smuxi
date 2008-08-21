/*
 * $Id: ChannelPage.cs 138 2006-12-23 17:11:57Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/ChannelPage.cs $
 * $Rev: 138 $
 * $Author: meebey $
 * $Date: 2006-12-23 18:11:57 +0100 (Sat, 23 Dec 2006) $
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
using System.Collections.Generic;
using System.Globalization;

using Mono.Unix;

using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;

namespace Smuxi.Frontend.Gnome
{
    public class ChatViewManager : ChatViewManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private List<ChatView> f_Chats = new List<ChatView>();
        private Notebook       f_Notebook;
        private Gtk.TreeView   f_TreeView;
        private UserConfig     f_Config;
        
        public override IChatView ActiveChat {
            get {
                return f_Notebook.CurrentChatView;
            }
        }
        
        public IList<ChatView> Chats {
            get {
                return f_Chats;
            }
        }
        
        public ChatViewManager(Notebook notebook, Gtk.TreeView treeView)
        {
            f_Notebook = notebook;
            f_TreeView = treeView;
        }
        
        public override void AddChat(ChatModel chat)
        {
            ChatView chatView = (ChatView) CreateChatView(chat);
            f_Chats.Add(chatView);
            
            if (f_Config != null) {
                chatView.ApplyConfig(f_Config);
            }
            
            int idx = -1;
            ChatType type = chat.ChatType;
            // group person and group chats behind their protocol chat
            if (type == ChatType.Person ||
                type == ChatType.Group) {
                IProtocolManager pm = chat.ProtocolManager;
                for (int i = 0; i < f_Notebook.NPages; i++) {
                    ChatView page = (ChatView) f_Notebook.GetNthPage(i);
                    ChatModel pageChat = page.ChatModel;
                    if (pageChat.ChatType == ChatType.Protocol &&
                        pageChat.ProtocolManager == pm) {
                        idx = i + 1;
                        break;
                    }
                }
                
                if (idx != -1) {
                    // now find the first chat with a different protocol manager
                    bool found = false;
                    for (int i = idx; i < f_Notebook.NPages; i++) {
                        ChatView page = (ChatView) f_Notebook.GetNthPage(i);
                        ChatModel pageChat = page.ChatModel;
                        if (pageChat.ProtocolManager != pm) {
                            found = true;
                            idx = i;
                            break;
                        }
                    }
                    if (!found) {
                        // if there was no next protocol manager, simply append 
                        // the chat way to the end
                        idx = -1;
                    }
                }
            }
            
            if (idx == -1) {
                f_Notebook.AppendPage(chatView, chatView.LabelWidget);
            } else {
#if LOG4NET
                f_Logger.Debug("AddChat(): adding chat at: " + idx); 
#endif
                f_Notebook.InsertPage(chatView, chatView.LabelWidget, idx);
            }
            
#if GTK_SHARP_2_10
            f_Notebook.SetTabReorderable(chatView, true);
#endif
            chatView.ShowAll();
        }
        
        public override void RemoveChat(ChatModel chat)
        {
            ChatView chatView = f_Notebook.GetChat(chat);
            f_Notebook.RemovePage(f_Notebook.PageNum(chatView));
            f_Chats.Remove(chatView);
        }
        
        public override void EnableChat(ChatModel chat)
        {
            ChatView chatView = f_Notebook.GetChat(chat);
            chatView.Enable();
        }
        
        public override void DisableChat(ChatModel chat)
        {
            ChatView chatView = f_Notebook.GetChat(chat);
            chatView.Disable();
        }
        
        public ChatView GetChat(ChatModel chatModel)
        {
            return f_Notebook.GetChat(chatModel);
        }
        
        public virtual void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);

            if (config == null) {
                throw new ArgumentNullException("config");
            }
            
            f_Config = config;
            foreach (ChatView chat in f_Chats) {
                chat.ApplyConfig(f_Config);
            }
        }
    }   
}
