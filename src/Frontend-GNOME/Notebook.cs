/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007-2013 Mirco Bauer <meebey@meebey.net>
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
using System.Threading;
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
        private bool      f_IsBrowseModeEnabled;

        public ChatView CurrentChatView {
            get {
                return (ChatView) base.CurrentPageWidget;
            }
            set {
                if (value == null) {
                    CurrentPage = 0;
                    return;
                }
                CurrentPage = GetPageNumber(value);
            }
        }

        public bool IsBrowseModeEnabled {
            get {
                return f_IsBrowseModeEnabled;
            }
            set {
                f_IsBrowseModeEnabled = value;
                if (value) {
#if LOG4NET
                    f_Logger.Debug("set_IsBrowseModeEnabled(): enabling browse mode");
#endif
                } else {
#if LOG4NET
                    f_Logger.Debug("set_IsBrowseModeEnabled(): disabling browse mode");
#endif
                    // HACK: force switch page event to clear activity and
                    // update the markerline
                    var chat = CurrentChatView;
                    CurrentChatView = null;
                    CurrentChatView = chat;
                }
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
            SwitchPage += OnBeforeSwitchPage;
            PageReordered += OnPageReordered;
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
            ShowTabs = false;
            
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

        public int GetPageNumber(ChatView chatView)
        {
            for (int i = 0; i < NPages; i++) {
                ChatView page = (ChatView) GetNthPage(i);
                if (page == chatView) {
                    return i;
                }
            }

            return -1;
        }

        public void SyncPagePositions()
        {
            Trace.Call();

            if (Frontend.EngineProtocolVersion >= new Version("0.8.1.2")) {
                // no need to sync chat positions with 0.8.1.2 as they get
                // updated via Session.MoveChat()
                return;
            }

            for (int i = 0; i < NPages; i++) {
                var chatView = (ChatView) GetNthPage(i);
                chatView.ChatModel.Position = i;
            }
        }

        public void ClearAllActivity()
        {
            Trace.Call();
            
            int npages = NPages;
            for (int i = 0; i < npages; i++) {
                ChatView chat = GetChat(i);
                chat.HasActivity = false;
                chat.HasEvent = false;
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
            f_Logger.Fatal("OnSwitchPageQueueAbortedEvent(): task queue aborted!");
#endif
        }

        [GLib.ConnectBefore]
        protected virtual void OnBeforeSwitchPage(object sender, Gtk.SwitchPageArgs e)
        {
            if (f_IsBrowseModeEnabled) {
                return;
            }

            var chatView = CurrentChatView;
            chatView.UpdateLastSeenMessage();
        }

        protected virtual void OnSwitchPage(object sender, Gtk.SwitchPageArgs e)
        {
            Trace.Call(sender, e);

            if (f_IsBrowseModeEnabled) {
                return;
            }

            // synchronize FrontManager.CurrenPage
            ChatView chatView = GetChat((int)e.PageNum);
            if (chatView == null) {
                return;
            }

            ChatModel chatModel = chatView.ChatModel;

            // clear activity and highlight
            chatView.HasHighlight = false;
            chatView.HasActivity = false;
            chatView.HasEvent = false;
            var lastMsg = chatView.OutputMessageTextView.LastMessage;

            var method = Trace.GetMethodBase();
            f_SwitchPageQueue.Queue(delegate {
                // HACK: don't pass the real parameters are it's unsafe from
                // a non-main (GUI) thread!
                Trace.Call(method, null, null);

                try {
                    DateTime start = DateTime.UtcNow, stop;
                    var fm = Frontend.FrontendManager;
                    if (fm == null) {
                        // in case we are currently reconnecting to a remote engine
                        return;
                    }

                    // OPT-TODO: we could use here a TaskStack instead which
                    // would make sure only the newest task gets executed
                    // instead of every task in the FIFO sequence!
                    var nmanager = chatView.ProtocolManager;

                    // TODO: only set the protocol manager and update network
                    // status if the protocol manager differs from the old one

                    // REMOTING CALL 2
                    fm.CurrentChat = chatModel;
                    if (nmanager != null) {
                        // REMOTING CALL 3
                        fm.CurrentProtocolManager = nmanager;
                    }

                    // even when we have no network manager, we still want to update
                    // the network status and title
                    // REMOTING CALL 4
                    fm.UpdateNetworkStatus();

                    // update last seen highlight
                    // REMOTING CALL 5
                    if (lastMsg != null && !Frontend.UseLowBandwidthMode) {
                        chatModel.LastSeenHighlight = lastMsg.TimeStamp;
                    }

                    stop = DateTime.UtcNow;
#if LOG4NET
                    f_Logger.Debug("OnSwitchPage(): task took: " + (stop - start).Milliseconds + " ms");
#endif
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error("OnSwitchPage(): Exception", ex);
#endif
                    Frontend.ShowException(ex);
                }
            });
        }

        protected virtual void OnPageReordered(object sender, Gtk.PageReorderedArgs e)
        {
            Trace.Call(sender, e);

            if (Frontend.EngineProtocolVersion < new Version("0.8.1.2")) {
                // Session.MoveChat() was added in >= 0.8.1.2
                return;
            }

            var chatView = (ChatView) e.P0;
            var newPosition = (int) e.P1;
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    Frontend.Session.MoveChat(chatView.ChatModel, newPosition);
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error("OnPageReordered(): Exception", ex);
#endif
                }
            });
        }
    }
}
