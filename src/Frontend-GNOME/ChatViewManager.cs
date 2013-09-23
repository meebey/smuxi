/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2013 Mirco Bauer <meebey@meebey.net>
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
    public class ChatViewManager : ChatViewManagerBase<ChatView>
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private List<ChatView> f_Chats = new List<ChatView>();
        public  IList<ChatView> SyncedChats { get; private set; }
        private Notebook       f_Notebook;
        private Gtk.TreeView   f_TreeView;
        private UserConfig     f_Config;
        ChatViewSyncManager    SyncManager { get; set; }
        bool AutoSwitchPersonChats { get; set; }
        bool AutoSwitchGroupChats { get; set; }

        public override ChatView ActiveChat {
            get {
                return CurrentChatView;
            }
            set {
                CurrentChatView = value as ChatView;
            }
        }

        public ChatView CurrentChatView {
            get {
                return f_Notebook.CurrentChatView;
            }
            set {
                if (value == null) {
                    return;
                }
                f_Notebook.CurrentChatView = value;
            }
        }

        public int CurrentChatNumber {
            get {
                if (CurrentChatView == null) {
                    return -1;
                }
                return f_Notebook.CurrentPage;
            }
            set {
                if (value < 0) {
                    value = f_Notebook.NPages + value;
                } else if (value >= f_Notebook.NPages) {
                    value = value - f_Notebook.NPages;
                }

                f_Notebook.CurrentPage = value;
            }
        }

        public IList<ChatView> Chats {
            get {
                return f_Chats;
            }
        }

        public bool IsSensitive {
            set {
                f_Notebook.Sensitive = value;
                Frontend.MainWindow.MenuWidget.Sensitive = value;
                Frontend.MainWindow.Entry.Sensitive = value;
            }
            get {
                return f_Notebook.Sensitive;
            }
        }

        public ChatViewManager(Notebook notebook, Gtk.TreeView treeView)
        {
            f_Notebook = notebook;
            f_TreeView = treeView;
            SyncedChats = new List<ChatView>();
            SyncManager = new ChatViewSyncManager();
            SyncManager.ChatAdded += OnChatAdded;
            SyncManager.ChatSynced += OnChatSynced;
            SyncManager.WorkerException += OnWorkerException;
        }

        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        public override void AddChat(ChatModel chat)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }

            SyncManager.QueueAdd(chat);
        }

        public override void RemoveChat(ChatModel chat)
        {
            ChatView chatView = GetChat(chat);
            if (chatView == null) {
 #if LOG4NET
                f_Logger.Warn("RemoveChat(): chatView is null!");
#endif
                return;
            }

            f_Notebook.RemovePage(f_Notebook.PageNum(chatView));
            f_Chats.Remove(chatView);
            SyncManager.Remove(chat);
            SyncedChats.Remove(chatView);

            OnChatRemoved(new ChatViewManagerChatRemovedEventArgs<ChatView>(chatView));

            chatView.Dispose();
        }

        public override void EnableChat(ChatModel chat)
        {
            ChatView chatView = GetChat(chat);
            if (chatView == null) {
 #if LOG4NET
                f_Logger.Warn("EnableChat(): chatView is null!");
#endif
                return;
            }

            chatView.Enable();
        }

        public override void DisableChat(ChatModel chat)
        {
            ChatView chatView = GetChat(chat);
            if (chatView == null) {
 #if LOG4NET
                f_Logger.Warn("DisableChat(): chatView is null!");
#endif
                return;
            }

            SyncedChats.Remove(chatView);
            chatView.Disable();
        }

        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        public void SyncChat(ChatModel chat)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }

            SyncManager.QueueSync(chat);
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

            var prefix = "Interface/Notebook/";
            AutoSwitchPersonChats =
                config[prefix + "AutoSwitchPersonChats"] as bool? ?? false;
            AutoSwitchGroupChats =
                config[prefix + "AutoSwitchGroupChats"] as bool? ?? true;

            f_Config = config;
            foreach (ChatView chat in f_Chats) {
                chat.ApplyConfig(f_Config);
            }
        }

        public void Clear()
        {
            Trace.Call();

            var chats = new List<ChatView>(f_Chats);
            foreach (var chat in chats) {
                // clean up ChatView and all notify related resources like
                // StatusIconManager, NotifyManager and IndicateManager
                RemoveChat(chat.ChatModel);
            }

            f_Config = null;
            SyncedChats.Clear();
            SyncManager.Clear();
        }

        public void ClearAllActivity()
        {
            Trace.Call();

            f_Notebook.ClearAllActivity();
        }

        public void Minimize()
        {
            Trace.Call();

            Frontend.MainWindow.Iconify();
        }

        void OnChatAdded(object sender, ChatViewAddedEventArgs e)
        {
            Trace.Call(sender, e);

            GLib.Idle.Add(delegate {
                var chatView = (ChatView) CreateChatView(e.ChatModel,
                                                         e.ChatType,
                                                         e.ProtocolManagerType);
                chatView.ID = e.ChatID;
                chatView.Name = e.ChatID;
                chatView.Position = e.ChatPosition;
                chatView.ProtocolManager = e.ProtocolManager;
                f_Chats.Add(chatView);

                if (f_Config != null) {
                    chatView.ApplyConfig(f_Config);
                }

                // POSSIBLE REMOTING CALL
                int idx = GetSortedChatPosition(chatView);
#if LOG4NET
                f_Logger.Debug("OnChatAdded(): adding " +
                               "<" + chatView.ID + "> at: " + idx);
#endif
                if (idx == -1) {
                    f_Notebook.AppendPage(chatView, chatView.LabelWidget);
                } else {
                    f_Notebook.InsertPage(chatView, chatView.LabelWidget, idx);
                }

                // notify the sync manager that the ChatView is ready to be synced
                SyncManager.ReleaseSync(chatView);

#if GTK_SHARP_2_10
                f_Notebook.SetTabReorderable(chatView, true);
#endif

                // we can't do auto switch for remote engines for now as that
                // kills all unseen highlights, see:
                // https://www.smuxi.org/issues/show/866
                if (Frontend.IsLocalEngine) {
                    if ((chatView is PersonChatView && AutoSwitchPersonChats) ||
                        (chatView is GroupChatView && AutoSwitchGroupChats)) {
                        CurrentChatNumber = idx;
                    }
                }

                OnChatAdded(new ChatViewManagerChatAddedEventArgs<ChatView>(chatView));
                return false;
            });
        }

        void OnChatSynced(object sender, ChatViewSyncedEventArgs e)
        {
            Trace.Call(sender, e);

            // FIXME: should we tell the FrontendManager before we sync?
            // no problem making remoting calls here as this event is called
            // from worker threads
            // REMOTING CALL 1
            Frontend.FrontendManager.AddSyncedChat(e.ChatView.ChatModel);

            GLib.Idle.Add(delegate {
                var chatView = (ChatView) e.ChatView;

                // we need to bailt out in case the chat was closed during the sync
                // else chatView.Populate() will die hard, see #635
                if (!Chats.Contains(chatView)) {
#if LOG4NET
                    f_Logger.Debug("OnChatSynced(): detected closed chat: " +
                                   chatView.ID + " during sync, bailing out...");
#endif
                    return false;
                }

                // HACK: patch chat position as OnChatAdded is not honoring the
                // AddChat order nor the complete range of chats
                if (chatView.Position != -1) {
                    f_Notebook.ReorderChild(chatView, chatView.Position);
                }

#if LOG4NET
                DateTime start = DateTime.UtcNow;
#endif
                chatView.Populate();
#if LOG4NET
                DateTime stop = DateTime.UtcNow;
                double duration = stop.Subtract(start).TotalMilliseconds;
                f_Logger.Debug("OnChatSynced(): " +
                               "<" + chatView.ID + ">.Populate() " +
                               "Position: " + chatView.Position +
                               " done, took: " + Math.Round(duration) + " ms");
#endif

                chatView.ScrollToEnd();

                SyncedChats.Add(chatView);
                OnChatSynced(new ChatViewManagerChatSyncedEventArgs<ChatView>(chatView));
                return false;
            });
        }

        void OnWorkerException(object sender, WorkerExceptionEventArgs e)
        {
            Trace.Call(sender, e);

            Frontend.ShowException(e.Exception);
        }

        int GetSortedChatPosition(ChatView chatView)
        {
            // starting with > 0.8 the Engine supplies ChatModel.Position for us
            if (Frontend.EngineVersion > new Version("0.8")) {
                return chatView.Position;
            }

            // COMPAT: Engine <= 0.8 doesn't populate ChatModel.Position thus
            // _we_ have to find a good position
            var chat = chatView.ChatModel;
            // REMOTING CALL 1
            int idx = chat.Position;
            // REMOTING CALL 2
            ChatType type = chat.ChatType;
            // new group person and group chats behind their protocol chat
            if (idx == -1 &&
                (type == ChatType.Person ||
                 type == ChatType.Group)) {
                // REMOTING CALL 3
                IProtocolManager pm = chat.ProtocolManager;
                for (int i = 0; i < f_Notebook.NPages; i++) {
                    ChatView page = (ChatView) f_Notebook.GetNthPage(i);
                    ChatModel pageChat = page.ChatModel;
                    // REMOTING CALL 4 and 5
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
                        // REMOTING CALL 6
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

            return idx;
        }
    }
}
