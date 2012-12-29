/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public delegate void SimpleDelegate(); 
    
    public class FrontendManager : PermanentRemoteObject, IFrontendUI, IDisposable
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string       _LibraryTextDomain = "smuxi-engine";
        private Session          _Session;
        private IFrontendUI      _UI;
        private ChatModel        _CurrentChat;
        private IProtocolManager _CurrentProtocolManager;
        private bool             _IsFrontendDisconnecting;
        private SimpleDelegate   _ConfigChangedDelegate;
        private bool             _IsFrontendSynced;
        private IList<ChatModel> _SyncedChats = new List<ChatModel>();
        private TaskQueue        f_TaskQueue;

        DateTime LastConfigChange;

        public int Version {
            get {
                return 0;
            }
        }
        
        public SimpleDelegate ConfigChangedDelegate {
            set {
                _ConfigChangedDelegate = value;
            }
        }
        
        public ChatModel CurrentChat {
            get {
                return _CurrentChat;
            }
            set {
                _CurrentChat = value;
            }
        }
        
        public IProtocolManager CurrentProtocolManager {
            get {
                return _CurrentProtocolManager;
            }
            set {
                _CurrentProtocolManager = value;
            }
        }
        
        public bool IsFrontendDisconnecting {
            get {
                return _IsFrontendDisconnecting;
            }
            set {
                _IsFrontendDisconnecting = value;
            }
        }
        
        public bool IsAlive {
            get {
                return !f_TaskQueue.Disposed;
            }
        }
        
        public FrontendManager(Session session, IFrontendUI ui)
        {
            Trace.Call(session, ui);
            
            if (session == null) {
                throw new ArgumentNullException("session");
            }
            if (ui == null) {
                throw new ArgumentNullException("ui");
            }
            
            _Session = session;
            _UI = ui;
            f_TaskQueue = new TaskQueue("FrontendManager");
            f_TaskQueue.ExceptionEvent += OnTaskQueueExceptionEvent;
            f_TaskQueue.AbortedEvent   += OnTaskQueueAbortedEvent;

            // register event for config invalidation
            _Session.Config.Changed += _OnConfigChanged;
        }
        
        ~FrontendManager()
        {
            Trace.Call();
            
            Dispose(false);
        }
        
        public void Dispose()
        {
            Trace.Call();
            
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected void Dispose(bool disposing)
        {
            Trace.Call(disposing);
            
            if (disposing) {
                f_TaskQueue.Dispose();
                _Session.Config.Changed -= _OnConfigChanged;
            }
        }
        
        public void Sync()
        {
            Trace.Call();

            // sync current page
            List<ChatModel> chats;
            lock (_Session.Chats) {
                _CurrentChat = _Session.Chats[0];
                chats = new List<ChatModel>(_Session.Chats);
            }

            // restore page positions
            if (_CurrentChat.Position != -1) {
                // looks like the positions were synced, sort it good
                chats.Sort(
                    (a, b) => (a.Position.CompareTo(b.Position))
                );
            }

            // sync pages
            foreach (ChatModel chat in chats) {
                _AddChat(chat);
            }

            // sync current network manager (if any exists)
            if (_Session.ProtocolManagers.Count > 0) {
                IProtocolManager nm = _Session.ProtocolManagers[0];
                CurrentProtocolManager = nm;
            }

            // sync content of pages
            foreach (ChatModel chat in chats) {
                _SyncChat(chat);
            }

            _IsFrontendSynced = true;

            _Session.CheckPresenceStatus();
        }

        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        public void AddSyncedChat(ChatModel chatModel)
        {
            Trace.Call(chatModel);

            if (!chatModel.IsEnabled) {
                // The frontend synced a disabled chat, this means the content
                // was is not in a clean state and thus we need to ignore this
                // sync so that a "re-sync" will bring the chat into a clean
                // state again. If we would not do this a re-sync would be
                // ignored, see SyncChat() and
                // http://www.smuxi.org/issues/show/132
                return;
            }

            // this method must be thread-safe as the frontend might sync
            // multiple chats at the same time
            lock (_SyncedChats) {
                _SyncedChats.Add(chatModel);
            }
        }
        
        public void NextProtocolManager()
        {
            Trace.Call();
            
            if (_Session.ProtocolManagers.Count == 0) {
                CurrentProtocolManager = null;
            } else {
                int pos = 0;
                if (CurrentProtocolManager != null) {
                    pos = _Session.ProtocolManagers.IndexOf(CurrentProtocolManager);
                }
                if (pos < _Session.ProtocolManagers.Count - 1) {
                    pos++;
                } else {
                    pos = 0;
                }
                CurrentProtocolManager = _Session.ProtocolManagers[pos];
            }
            
            UpdateNetworkStatus();
        }
        
        public void UpdateNetworkStatus()
        {
            if (CurrentProtocolManager != null) {
                SetNetworkStatus(CurrentProtocolManager.ToString());
            } else {
                SetNetworkStatus(String.Format("({0})", _("No network connections")));
            }
        }
        
        public void AddChat(ChatModel chat)
        {
            if (!IsSynced(chat) && _IsFrontendSynced) {
                _AddChat(chat);
            }
        }
        
        private void _AddChat(ChatModel chat)
        {
            f_TaskQueue.Queue(delegate {
                _UI.AddChat(chat);
            });
        }
        
        [Obsolete("This method is deprecated, use AddMessageToChat(cmd.Chat, MessageModel) instead!")]
        public void AddTextToChat(ChatModel chat, string text)
        {
            AddMessageToChat(chat, new MessageModel(text));
        }

        [Obsolete("This method is unsafe, use AddMessageToChat(cmd.Chat, MessageModel) instead!", true)]
        public void AddTextToCurrentChat(string text)
        {
            AddTextToChat(CurrentChat, text);
        }
        
        public void EnableChat(ChatModel chat)
        {
            f_TaskQueue.Queue(delegate {
                _UI.EnableChat(chat);
            });
        }
        
        public void DisableChat(ChatModel chat)
        {
            lock (_SyncedChats) {
                _SyncedChats.Remove(chat);
            }
            f_TaskQueue.Queue(delegate {
                _UI.DisableChat(chat);
            });
        }
        
        public void AddMessageToChat(ChatModel chat, MessageModel msg)
        {
            if (!IsSynced(chat)) {
#if LOG4NET
                // too much logging noise
                //_Logger.Warn("AddMessageToChat(): chat: " + chat + " is not synced yet, ignoring call...");
#endif
                return;
            }
            // BUG: if the frontend is syncing this chat, he probably will lose
            // messages heres!
            _AddMessageToChat(chat, msg);
        }
        
        private void _AddMessageToChat(ChatModel chat, MessageModel msg)
        {
            f_TaskQueue.Queue(delegate {
                _UI.AddMessageToChat(chat, msg);
            });
        }
        
        [Obsolete("This method is unsafe, use AddMessageToChat(cmd.Chat, msg) instead!", true)]
        public void AddMessageToCurrentChat(MessageModel msg)
        {
            AddMessageToChat(CurrentChat, msg);
        }
        
        public void RemoveChat(ChatModel chat)
        {
            lock (_SyncedChats) {
                _SyncedChats.Remove(chat);
            }

            // switch to next protocol manager if the current one was closed
            if (chat is ProtocolChatModel &&
                chat.ProtocolManager == CurrentProtocolManager) {
                NextProtocolManager();
            }

            f_TaskQueue.Queue(delegate {
                _UI.RemoveChat(chat);
            });
        }
        
        public void SyncChat(ChatModel chat)
        {
            if (!IsSynced(chat) && _IsFrontendSynced) {
                _SyncChat(chat);
            }
        }
        
        private void _SyncChat(ChatModel chat)
        {
            f_TaskQueue.Queue(delegate {
                _UI.SyncChat(chat);
            });
        }
        
        public void AddPersonToGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            if (!IsSynced(groupChat)) {
                return;
            }
            
            _AddPersonToGroupChat(groupChat, person);
        }
        
        private void _AddPersonToGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            f_TaskQueue.Queue(delegate {
                _UI.AddPersonToGroupChat(groupChat, person);
            });
        }
        
        public void UpdatePersonInGroupChat(GroupChatModel groupChat, PersonModel oldPerson, PersonModel newPerson)
        {
            if (!IsSynced(groupChat)) {
                return;
            }
            
            _UpdatePersonInGroupChat(groupChat, oldPerson, newPerson);
        }
        
        private void _UpdatePersonInGroupChat(GroupChatModel groupChat, PersonModel oldPerson, PersonModel newPerson)
        {
            f_TaskQueue.Queue(delegate {
                _UI.UpdatePersonInGroupChat(groupChat, oldPerson, newPerson);
            });
        }
    
        public void UpdateTopicInGroupChat(GroupChatModel groupChat, MessageModel topic)
        {
            if (!IsSynced(groupChat)) {
                return;
            }
            
            _UpdateTopicInGroupChat(groupChat, topic);
        }
        
        private void _UpdateTopicInGroupChat(GroupChatModel groupChat, MessageModel topic)
        {
            f_TaskQueue.Queue(delegate {
                _UI.UpdateTopicInGroupChat(groupChat, topic);
            });
        }
    
        public void RemovePersonFromGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            if (!IsSynced(groupChat)) {
                return;
            }
            
            _RemovePersonFromGroupChat(groupChat, person);
        }
        
        private void _RemovePersonFromGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            f_TaskQueue.Queue(delegate {
                _UI.RemovePersonFromGroupChat(groupChat, person);
            });
        }
        
        public void SetNetworkStatus(string status)
        {
            f_TaskQueue.Queue(delegate {
                _UI.SetNetworkStatus(status);
            });
        }
        
        public void SetStatus(string status)
        {
            f_TaskQueue.Queue(delegate {
                _UI.SetStatus(status);
            });
        }
        
        private void _OnConfigChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            // only push config changes once per 30 seconds
            if ((DateTime.UtcNow - LastConfigChange).TotalSeconds < 30) {
                return;
            }

            try {
                // DISABLED: delegate is not reliable enough, this needs to be
                // replaced with an IChatConfig API
                /*
                if (_ConfigChangedDelegate != null) {
                    _ConfigChangedDelegate();
                }
                */
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
            }
            LastConfigChange = DateTime.UtcNow;
        }

        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        bool IsSynced(ChatModel chatModel)
        {
            if (chatModel == null) {
                throw new ArgumentNullException("chatModel");
            }

            lock (_SyncedChats) {
                return _SyncedChats.Contains(chatModel);
            }
        }

        protected virtual void OnTaskQueueExceptionEvent(object sender, TaskQueueExceptionEventArgs e)
        {
            Trace.Call(sender, e);
            
            if (e.Exception is System.Runtime.Remoting.RemotingException) {
#if LOG4NET
                if (!_IsFrontendDisconnecting) {
                    // we didn't expect this problem
                    _Logger.Error("RemotingException in TaskQueue: ", e.Exception);
                    _Logger.Error("Inner-Exception: ", e.Exception.InnerException);
                }
#endif
                // TODO: setup a timer and wait up to 10 minutes to let
                // the frontend resume the session, after that timeout
                // clean it good
            } else {
#if LOG4NET
                _Logger.Error("Exception in TaskQueue: ", e.Exception);
                _Logger.Error("Inner-Exception: ", e.Exception.InnerException);
#endif
            }

            // no need to remove us from the Session here as
            // OnTaskQueueAbortedEvent will be raised after this and handle it
        }
        
        protected virtual void OnTaskQueueAbortedEvent(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            // we can't rely on the UI (proxy) object here, the connection is probably
            // gone and doesn't come back
            //_Session.DeregisterFrontendUI(_UI);
            // thus we can deregister the hardway (by using our instance)
            _Session.DeregisterFrontendManager(this);            
        }
        
        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
