/*
 * $Id: ChannelPage.cs 138 2006-12-23 17:11:57Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/ChannelPage.cs $
 * $Rev: 138 $
 * $Author: meebey $
 * $Date: 2006-12-23 18:11:57 +0100 (Sat, 23 Dec 2006) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007 Mirco Bauer <meebey@meebey.net>
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
using System.Globalization;
using System.Collections.Generic;
using Mono.Unix;
using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;

namespace Smuxi.Frontend.Stfl
{
    public class ChatViewManager : ChatViewManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private MainWindow                      _MainWindow;
        private Dictionary<ChatModel, ChatView> _ChatViews = new Dictionary<ChatModel, ChatView>();
        
        public ChatViewManager(MainWindow mainWindow)
        {
            _MainWindow = mainWindow;
        }
        
        public override void AddChat(ChatModel chat)
        {
            ChatView chatView = (ChatView) CreateChatView(chat);
            chatView.MainWindow = _MainWindow;
            _ChatViews.Add(chat, chatView);
        }
        
        public override void RemoveChat(ChatModel chat)
        {
            ChatView chatView = _ChatViews[chat];
            _ChatViews.Remove(chat);
        }
        
        public override void EnableChat(ChatModel chat)
        {
       	    ChatView chatView = _ChatViews[chat];
       	    chatView.Enable();
        }
        
        public override void DisableChat(ChatModel chat)
        {
       	    ChatView chatView = _ChatViews[chat];
       	    chatView.Disable();
        }
        
        public ChatView GetChat(ChatModel chat)
        {
            return _ChatViews[chat];
        }
    }   
}
