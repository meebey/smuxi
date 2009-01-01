/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2008 Mirco Bauer <meebey@meebey.net>
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
                return  !f_TaskQueue.Disposed;
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
            // BUG: when the frontend disconnects there are dangling methods registered!
            //_Session.Config.Changed += new EventHandler(_OnConfigChanged);
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
            }
        }
        
        public void Sync()
        {
            Trace.Call();
            
            // TODO: sort pages network tabs then channel tabs (alphabeticly)
            // sync pages            
            foreach (ChatModel chat in _Session.Chats) {
                _AddChat(chat);
            }
            
            // sync current network manager (if any exists)
            if (_Session.ProtocolManagers.Count > 0) {
                IProtocolManager nm = _Session.ProtocolManagers[0];
                CurrentProtocolManager = nm;
            }
            
            // sync current page
            _CurrentChat = _Session.Chats[0];
            
            // sync content of pages
            foreach (ChatModel chat in _Session.Chats) {
                _SyncChat(chat);
            }
            
            _IsFrontendSynced = true;
        }
        
        public void AddSyncedChat(ChatModel chatModel)
        {
            Trace.Call(chatModel);
            
            _SyncedChats.Add(chatModel);
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
                SetNetworkStatus(String.Format("({0})", _("no network connections")));
            }
        }
        
        public void AddChat(ChatModel chat)
        {
            if (!_SyncedChats.Contains(chat) && _IsFrontendSynced) {
                _AddChat(chat);
            }
        }
        
        private void _AddChat(ChatModel chat)
        {
            f_TaskQueue.Queue(delegate {
                _UI.AddChat(chat);
            });
        }
        
        public void AddTextToChat(ChatModel chat, string text)
        {
            AddMessageToChat(chat, new MessageModel(text));
        }
        
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
            if (_SyncedChats.Contains(chat)) {
                _SyncedChats.Remove(chat);
            }
            f_TaskQueue.Queue(delegate {
                _UI.DisableChat(chat);
            });
        }
        
        public void AddMessageToChat(ChatModel chat, MessageModel msg)
        {
            if (!_SyncedChats.Contains(chat)) {
#if LOG4NET
                _Logger.Warn("AddMessageToChat(): chat: " + chat + " is not synced yet, ignoring call...");
#endif
                return;
            }
            
            _AddMessageToChat(chat, msg);
        }
        
        private void _AddMessageToChat(ChatModel chat, MessageModel msg)
        {
            f_TaskQueue.Queue(delegate {
                _UI.AddMessageToChat(chat, msg);
            });
        }
        
        public void AddMessageToCurrentChat(MessageModel msg)
        {
            AddMessageToChat(CurrentChat, msg);
        }
        
        public void RemoveChat(ChatModel chat)
        {
            f_TaskQueue.Queue(delegate {
                _UI.RemoveChat(chat);
            });
        }
        
        public void SyncChat(ChatModel chat)
        {
            if (!_SyncedChats.Contains(chat) && _IsFrontendSynced) {
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
            if (!_SyncedChats.Contains(groupChat)) {
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
            if (!_SyncedChats.Contains(groupChat)) {
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
    
        public void UpdateTopicInGroupChat(GroupChatModel groupChat, string topic)
        {
            if (!_SyncedChats.Contains(groupChat)) {
                return;
            }
            
            _UpdateTopicInGroupChat(groupChat, topic);
        }
        
        private void _UpdateTopicInGroupChat(GroupChatModel groupChat, string topic)
        {
            f_TaskQueue.Queue(delegate {
                _UI.UpdateTopicInGroupChat(groupChat, topic);
            });
        }
    
        public void RemovePersonFromGroupChat(GroupChatModel groupChat, PersonModel person)
        {
            if (!_SyncedChats.Contains(groupChat)) {
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
            
            // BUG: we should use some timeout here and only call the delegate
            // when the timeout is reached, else we flood the frontend for each
            // changed value in the config!
            try {
                if (_ConfigChangedDelegate != null) {
                    _ConfigChangedDelegate();
                }
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
            }
        }
        
        protected virtual void OnTaskQueueExceptionEvent(object sender, TaskQueueExceptionEventArgs e)
        {
            Trace.Call(sender, e);
            
            if (e.Exception is System.Runtime.Remoting.RemotingException) {
#if LOG4NET
                if (!_IsFrontendDisconnecting) {
                    // we didn't expect this problem
                    _Logger.Error("RemotingException in TaskQueue, aborting thread...", e.Exception);
                    _Logger.Error("Inner-Exception: ", e.Exception.InnerException);
                }
#endif
                // TODO: setup a timer and wait up to 10 minutes to let
                // the frontend resume the session, after that timeout
                // clean it good
            } else {
#if LOG4NET
                _Logger.Error("Exception in TaskQueue, aborting thread...", e.Exception);
                _Logger.Error("Inner-Exception: ", e.Exception.InnerException);
#endif
            }
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
