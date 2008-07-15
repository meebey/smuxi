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
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private List<ChatView> f_Chats = new List<ChatView>();
        private Notebook       f_Notebook;
        private Gtk.TreeView   f_TreeView;
        //private UserConfig   _Config;
        
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
        
        public ChatViewManager(Notebook notebook, Gtk.TreeView treeView /*, UserConfig userConfig*/)
        {
            f_Notebook = notebook;
            f_TreeView = treeView;
            //_Config = userConfig;
        }
        
        /*
        protected override IChatView CreateChatView(ChatModel chat)
        {
            switch (chat.ChatType) {
                case ChatType.Network:
                    return new NetworkChatView(chat);
                case ChatType.Person:
                    return new PersonChatView(chat);
                case ChatType.Group:
                    return new GroupChatView(chat as GroupChatModel);
            }
            
            throw new ApplicationException("Unsupported ChatModel type: " + chat.GetType());
        }
        */
        
        public override void AddChat(ChatModel chat)
        {
            ChatView chatView = (ChatView) CreateChatView(chat);
            
            f_Chats.Add(chatView);
            //Frontend.UserConfig[""]
            //_Notebook.InsertPage(chatView, chatView.LabelEventBox, pos);
            f_Notebook.AppendPage(chatView, chatView.LabelWidget);
            //_Notebook.SetTabReorderable(chatView, true);
            // it's better to do automatic (re-)ordering
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
    }   
}
