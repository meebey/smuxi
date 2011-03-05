/*
 * $Id: ChannelPage.cs 138 2006-12-23 17:11:57Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/ChannelPage.cs $
 * $Rev: 138 $
 * $Author: meebey $
 * $Date: 2006-12-23 18:11:57 +0100 (Sat, 23 Dec 2006) $
 *
 * Smuxi - Smart MUltipleXed Irc
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
using System.Text;
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
        private MainWindow                      f_MainWindow;
        private ChatView                        f_CurrentChat;
        private Dictionary<ChatModel, ChatView> f_ChatViews = new Dictionary<ChatModel, ChatView>();
        private List<ChatView>                  f_ChatViewList = new List<ChatView>();

        public event ChatSwitchedEventHandler CurrentChatSwitched;

        public override IChatView ActiveChat {
            get {
                return CurrentChat;
            }
        }
        
        public ChatView CurrentChat {
            get {
                return f_CurrentChat;
            }
            set {
                if (f_CurrentChat != null) {
                    f_CurrentChat.IsVisible = false;
                }

                f_CurrentChat = value;
                if (f_CurrentChat != null) {
#if LOG4NET
                    _Logger.Debug("set_CurrentChat(): making " + value.ChatModel.ID + " visible");
#endif
                    f_CurrentChat.IsVisible = true;
                }

                if (CurrentChatSwitched != null) {
                    CurrentChatSwitched(this, new ChatSwitchedEventArgs(f_CurrentChat));
                }
            }
        }

        public int CurrentChatNumber {
            get {
                if (CurrentChat == null) {
                    return -1;
                }
                return f_ChatViewList.IndexOf(CurrentChat);
            }
            set {
                if (value < 0 || value >= f_ChatViewList.Count) {
                    return;
                }

                CurrentChat = f_ChatViewList[value];
            }
        }

        public ChatViewManager(MainWindow mainWindow)
        {
           if (mainWindow == null) {
                throw new ArgumentNullException("mainWindow");
           }

           f_MainWindow = mainWindow;
        }
        
        public override void AddChat(ChatModel chat)
        {
            ChatView chatView = (ChatView) CreateChatView(chat, f_MainWindow);
            f_ChatViews.Add(chat, chatView);
            f_ChatViewList.Add(chatView);

            if (CurrentChat == null) {
                CurrentChat = chatView;
            }

            UpdateNavigation();
        }

        public override void RemoveChat(ChatModel chat)
        {
            var chatView = GetChat(chat);
            chatView.IsVisible = false;

            if (CurrentChat == chatView) {
                CurrentChatNumber--;
            }

            chatView.Dispose();
            f_ChatViews.Remove(chat);
            f_ChatViewList.Remove(chatView);

            UpdateNavigation();
        }
        
        public override void EnableChat(ChatModel chat)
        {
           ChatView chatView = f_ChatViews[chat];
           chatView.Enable();
        }
        
        public override void DisableChat(ChatModel chat)
        {
           ChatView chatView = f_ChatViews[chat];
           chatView.Disable();
        }
        
        public ChatView GetChat(ChatModel chat)
        {
            return f_ChatViews[chat];
        }
        
        public ChatView GetChat(int chat)
        {
            if (chat < 0 || chat >= f_ChatViewList.Count) {
                return null;
            }
            return f_ChatViewList[chat];
        }

        private void UpdateNavigation()
        {
            var nav = new StringBuilder();
            foreach (var chat in f_ChatViewList) {
                nav.AppendFormat("[{0}] ", chat.ChatModel.Name);
            }
            nav.Remove(nav.Length - 1, 1);

            f_MainWindow.NavigationLabel = nav.ToString();
        }
    }

    public delegate void ChatSwitchedEventHandler(object sender, ChatSwitchedEventArgs e);
    
    public class ChatSwitchedEventArgs : EventArgs
    {
        public ChatView ChatView { get; set; }
        
        public ChatSwitchedEventArgs(ChatView chatView)
        {
            ChatView = chatView;
        }
    }
}
