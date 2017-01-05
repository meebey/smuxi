/*
 * $Id: GnomeUI.cs 213 2007-09-10 21:25:36Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/GnomeUI.cs $
 * $Rev: 213 $
 * $Author: meebey $
 * $Date: 2007-09-10 23:25:36 +0200 (Mon, 10 Sep 2007) $
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
using System.Reflection;
using SysDiag = System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization; 
using System.ComponentModel;
using System.Windows.Forms;
using Mono.Unix;
using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;

namespace Smuxi.Frontend.Swf
{
    public class SwfUI : PermanentRemoteObject, IFrontendUI
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private int _Version = 0;
        private ChatViewManager _ChatViewManager;
        private Control         _Control;
        
        public int Version {
            get {
                return _Version;
            }
        }
        
        public SwfUI(ChatViewManager chatViewManager, Control control)
        {
            _ChatViewManager = chatViewManager;
            _Control = control;
        }
        
        public void AddChat(ChatModel chat)
        {
            TraceRemotingCall(chat);
            
            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, chat);
                
                _ChatViewManager.AddChat(chat);
            }));
        }
        
        private void _AddMessageToChat(ChatModel epage, MessageModel msg)
        {
            ChatView chatView = _ChatViewManager.GetChat(epage);
#if LOG4NET
            if (chatView == null) {
                _Logger.Fatal(String.Format("_AddMessageToChat(): Notebook.GetPage(epage) epage.Name: {0} returned null!", epage.Name));
                return;
            }
#endif
            chatView.AddMessage(msg);
        }
        
        public void AddMessageToChat(ChatModel epage, MessageModel fmsg)
        {
            TraceRemotingCall(epage, fmsg);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, epage, fmsg);
                
                _AddMessageToChat(epage, fmsg);
            }));
        }
        
        public void RemoveChat(ChatModel chat)
        {
            TraceRemotingCall(chat);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, chat);
                
                _ChatViewManager.RemoveChat(chat);
            }));
        }
        
        public void EnableChat(ChatModel chat)
        {
            TraceRemotingCall(chat);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, chat);
                
                _ChatViewManager.EnableChat(chat);
            }));
        }
        
        public void DisableChat(ChatModel chat)
        {
            TraceRemotingCall(chat);
            
            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, chat);
                
                _ChatViewManager.DisableChat(chat);
            }));
        }
        
        public void SyncChat(ChatModel chatModel)
        {
            TraceRemotingCall(chatModel);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, chatModel);

                ChatView chatView = _ChatViewManager.GetChat(chatModel);
#if LOG4NET
                DateTime syncStart = DateTime.UtcNow;
#endif
                chatView.Sync();
#if LOG4NET
                DateTime syncStop = DateTime.UtcNow;
                double duration = syncStop.Subtract(syncStart).TotalMilliseconds;
                _Logger.Debug("SyncChat() done, syncing took: " + Math.Round(duration) + " ms");
#endif
                
                // maybe a BUG here? should be tell the FrontendManager before we sync?
                Frontend.FrontendManager.AddSyncedChat(chatModel);
                //_SyncedChats.Add(chatView);

                // BUG: doesn't work?!?
                chatView.ScrollToEnd();
            }));
        }
        
        public void AddPersonToGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            TraceRemotingCall(groupChat, person);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, groupChat, person);
                
                GroupChatView groupChatView = (GroupChatView) _ChatViewManager.GetChat(groupChat);
                groupChatView.AddPerson(person);
            }));
        }
        
        public void UpdatePersonInGroupChat(GroupChatModel groupChat, PersonModel oldPerson, PersonModel newPerson)
        {
            TraceRemotingCall(groupChat, oldPerson, newPerson);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, groupChat, oldPerson, newPerson);
                
                GroupChatView groupChatView = (GroupChatView) _ChatViewManager.GetChat(groupChat);
                groupChatView.UpdatePerson(oldPerson, newPerson);
            }));
        }
        
        public void UpdateTopicInGroupChat(GroupChatModel ecpage, MessageModel topic)
        {
            TraceRemotingCall(ecpage, topic);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, ecpage, topic);
                
                /*
                GroupChatView cpage = (GroupChatView)Frontend.MainWindow.Notebook.GetChat(ecpage);
                if (cpage.TopicEntry != null) {
                    cpage.TopicEntry.Text = topic;
                }
                */
            }));
        }
        
        public void RemovePersonFromGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            TraceRemotingCall(groupChat, person);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, groupChat, person);
            
                GroupChatView groupChatView = (GroupChatView) _ChatViewManager.GetChat(groupChat);
                groupChatView.RemovePerson(person);
            }));
        }
        
        public void SetNetworkStatus(string status)
        {
            TraceRemotingCall(status);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, status);
                
                Frontend.MainWindow.NetworkStatusbar.Text = status;
            }));
        }
        
        public void SetStatus(string status)
        {
            TraceRemotingCall(status);

            MethodBase mb = Trace.GetMethodBase();
            _Control.Invoke(new MethodInvoker(delegate {
                TraceRemotingCall(mb, status);

                Frontend.MainWindow.Statusbar.Text = status;
            }));
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
        
        [SysDiag.Conditional("REMOTING_TRACE")]
        protected static void TraceRemotingCall(MethodBase mb, params object[] parameters)
        {
            Trace.Call(mb, parameters);
        }
        
        [SysDiag.Conditional("REMOTING_TRACE")]
        protected static void TraceRemotingCall(params object[] parameters)
        {
            TraceRemotingCall(Trace.GetMethodBase(), parameters);
        }
    }
}
