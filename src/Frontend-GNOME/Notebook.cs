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

using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class Notebook : Gtk.Notebook
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        //private Gtk.Menu     _QueryTabMenu;
        private TaskQueue f_SwitchPageQueue;

        public ChatView CurrentChatView {
            get {
                return (ChatView) base.CurrentPageWidget;
            }
        }
        
        public Notebook() : base ()
        {
            Trace.Call();
            
            f_SwitchPageQueue = new TaskQueue("SwitchPage");
            f_SwitchPageQueue.AbortedEvent += OnSwitchPageQueueAbortedEvent;
            f_SwitchPageQueue.ExceptionEvent += OnSwitchPageQueueExceptionEvent;

            Scrollable = true;
            SwitchPage += OnSwitchPage;
        }
        
        public void ApplyConfig(UserConfig userConfig)
        {
            switch ((string) userConfig["Interface/Notebook/TabPosition"]) {
                case "top":
                    TabPos = Gtk.PositionType.Top;
                    ShowTabs = true;
                    break;
                case "bottom":
                    TabPos = Gtk.PositionType.Bottom;
                    ShowTabs = true;
                    break;
                case "left":
                    TabPos = Gtk.PositionType.Left;
                    ShowTabs = true;
                    break;
                case "right":
                    TabPos = Gtk.PositionType.Right;
                    ShowTabs = true;
                    break;
                case "none":
                    ShowTabs = false;
                    break;
            }
            
            // TODO: Homogeneous = true;
        }
        
        public ChatView GetChat(ChatModel chat)
        {
            for (int i=0; i < NPages; i++) {
                ChatView chatView = (ChatView) GetNthPage(i);
                if (chatView.ChatModel == chat) {
                    return chatView;
                }
            }
            
            return null;
        }
        
        public ChatView GetChat(int pageNumber)
        {
            return (ChatView) base.GetNthPage(pageNumber);
        }
        
        public void RemoveAllPages()
        {
            Trace.Call();

            // OPT: don't trigger lots of SwitchPage events while we remove all pages
            // this also breaks the Frontend.ReconnectEngineToGUI() as that one
            // has to cleanup all chats regardless of a working network
            // connection
            SwitchPage -= OnSwitchPage;

            int npages = NPages;
            CurrentPage = 0;
            for (int i = 0; i < npages; i++) {
                // *doh* this would be too easy, ugly Gtk.Notebook doesn't
                // like it though, index based vs array based?
                //RemovePage(i);

                NextPage();
                RemovePage(CurrentPage);
            }

            // reconnect the event handler
            SwitchPage += OnSwitchPage;
        }
        
        public void ClearAllActivity()
        {
            Trace.Call();
            
            int npages = NPages;
            for (int i = 0; i < npages; i++) {
                ChatView chat = GetChat(i);
                chat.HasActivity = false;
            }
        }
        
        protected virtual void OnSwitchPageQueueExceptionEvent(object sender, TaskQueueExceptionEventArgs e)
        {
            Trace.Call(sender, e);

#if LOG4NET
            f_Logger.Error("Exception in TaskQueue: ", e.Exception);
            f_Logger.Error("Inner-Exception: ", e.Exception.InnerException);
#endif
            Frontend.ShowException(e.Exception);
        }

        protected virtual void OnSwitchPageQueueAbortedEvent(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

#if LOG4NET
            f_Logger.Debug("OnSwitchPageQueueAbortedEvent(): task queue aborted!");
#endif
        }

        protected virtual void OnSwitchPage(object sender, Gtk.SwitchPageArgs e)
        {
            Trace.Call(sender, e);
            
            // synchronize FrontManager.CurrenPage
            ChatView chatView = GetChat((int)e.PageNum);
            if (chatView == null) {
                return;
            }

            ChatModel chatModel = chatView.ChatModel;

            // clear activity and highlight
            chatView.HasHighlight = false;
            chatView.HasActivity = false;

            var method = Trace.GetMethodBase();
            f_SwitchPageQueue.Queue(delegate {
                // HACK: don't pass the real parameters are it's unsafe from
                // a non-main (GUI) thread!
                Trace.Call(method, null, null);

                DateTime start = DateTime.UtcNow, stop;
                // OPT-TODO: we could use here a TaskStack instead which
                // would make sure only the newest task gets executed
                // instead of every task in the FIFO sequence!
                // REMOTING CALL 1
                IProtocolManager nmanager = chatModel.ProtocolManager;

                // REMOTING CALL 2
                Frontend.FrontendManager.CurrentChat = chatModel;
                if (nmanager != null) {
                    // REMOTING CALL 3
                    Frontend.FrontendManager.CurrentProtocolManager = nmanager;
                }

                // even when we have no network manager, we still want to update the state
                // REMOTING CALL 4
                Frontend.FrontendManager.UpdateNetworkStatus();

                // sync title
                // REMOTING CALL 5
                string networkStatus = nmanager == null ? null :
                                        nmanager.ToString();
                Gtk.Application.Invoke(delegate {
                    if (Frontend.MainWindow == null) {
                        return;
                    }

                    // update window title
                    string title;
                    if (chatView is SessionChatView) {
                        title = String.Empty;
                    } else if (chatView is ProtocolChatView) {
                        title = networkStatus;
                    } else {
                        title = String.Format("{0} @ {1}",
                                              chatView.Name,
                                              networkStatus);
                    }
                    if (!String.IsNullOrEmpty(title)) {
                        title += " - ";
                    }
                    title += "Smuxi";

                    Frontend.MainWindow.Title = title;
                });

                // update last seen highlight
                // REMOTING CALL 6
                chatModel.LastSeenHighlight = DateTime.UtcNow;

                stop = DateTime.UtcNow;
#if LOG4NET
                f_Logger.Debug("OnSwitchPage(): task took: " + (stop - start).Milliseconds + " ms");
#endif
            });
        }
    }
}
