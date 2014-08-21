// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2011, 2013-2014 Mirco Bauer <meebey@meebey.net>
// Copyright (c) 2014 Oliver Schneider <mail@oli-obk.de>
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

using System;
using System.Threading;
using System.Runtime.Remoting;
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend
{
    public class ChatViewSyncManager
    {
#if LOG4NET
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif

        /*
         * TODO DisableChat is not in this system
         * TODO DisableChat should SyncState ---Disable---> WaitingForSyncState
         *
         * InitialState ---Add---> AddedState
         * AddedState ---Sync---> SyncQueuedState
         *                  ---ReadyToSync---> WaitingForSyncState
         * SyncQueuedState ---ReadyToSync---> SyncingState
         * WaitingForSyncState ---Sync---> SyncingState
         * SyncingState ---SyncFinished---> SyncState
         * SyncState ---Sync---> SyncingState
         *
         * AddedState ---Remove---> RemovingState
         * SyncQueuedState ---Remove---> RemovingState
         * WaitingForSyncState ---Remove---> RemovingState
         * SyncingState ---Remove---> RemovingState
         * SyncState ---Remove---> RemovingState
         * RemovingState ---RemoveFinished---> KILL IT WITH FIRE
         */

        abstract class State
        {
            protected SyncInfo SyncInfo { get; private set; }

            protected State(SyncInfo chat)
            {
                if (chat == null) {
                    throw new ArgumentNullException("chat");
                }
                SyncInfo = chat;
            }

            public virtual void Init()
            {
            }

            public virtual void ExecuteAdd()
            {
                throw new InvalidStateException("could not add in " + this.GetType().Name);
            }

            public virtual void ExecuteRemove()
            {
                throw new InvalidStateException("could not remove in " + this.GetType().Name);
            }

            public virtual void ExecuteRemoveFinished()
            {
                throw new InvalidStateException("could not remove in " + this.GetType().Name);
            }

            public virtual void ExecuteSync()
            {
                throw new InvalidStateException("could not sync in " + this.GetType().Name);
            }

            public virtual void ExecuteReadyToSync()
            {
                throw new InvalidStateException("could not be ready to sync in " + this.GetType().Name);
            }

            public virtual void ExecuteSyncFinished()
            {
                throw new InvalidStateException("could not finish sync in " + this.GetType().Name);
            }
        }

        class InitialState : State
        {
            public InitialState(SyncInfo chat) : base(chat)
            {
            }

            public override void ExecuteAdd()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new AddedState(SyncInfo);
            }
        }

        class AddedState : State
        {
            public AddedState(SyncInfo chat) : base(chat)
            {
            }

            public override void Init()
            {
#if LOG4NET
                DateTime start = DateTime.UtcNow;
#endif
                // REMOTING CALL 1
                var chatId = SyncInfo.ChatModel.ID;
                // REMOTING CALL 2
                var chatType = SyncInfo.ChatModel.ChatType;
                // REMOTING CALL 3
                var chatPosition = SyncInfo.ChatModel.Position;
                // REMOTING CALL 4
                var protocolManager = SyncInfo.ChatModel.ProtocolManager;
                Type protocolManagerType = null;
                if (protocolManager != null) {
                    protocolManagerType = protocolManager.GetType();
                }
#if LOG4NET
                DateTime stop = DateTime.UtcNow;
                double duration = stop.Subtract(start).TotalMilliseconds;
                Logger.Debug("Add() done, syncing took: " +
                             Math.Round(duration) + " ms");
#endif
                SyncInfo.Manager.OnChatAdded(SyncInfo.ChatModel,
                                             chatId,
                                             chatType,
                                             chatPosition,
                                             protocolManager,
                                             protocolManagerType);
            }

            public override void ExecuteReadyToSync()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new WaitingForSyncState(SyncInfo);
            }

            public override void ExecuteSync()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new SyncQueuedState(SyncInfo);
            }

            public override void ExecuteRemove()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new RemovingState(SyncInfo);
            }
        }

        class SyncQueuedState : State
        {
            public SyncQueuedState(SyncInfo chat) : base(chat)
            {
            }

            public override void ExecuteReadyToSync()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new SyncingState(SyncInfo);
            }
        }

        class WaitingForSyncState : State
        {
            public WaitingForSyncState(SyncInfo chat) : base(chat)
            {
            }

            public override void ExecuteSync()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new SyncingState(SyncInfo);
            }

            public override void ExecuteRemove()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new RemovingState(SyncInfo);
            }
        }

        class SyncingState : State
        {
            public SyncingState(SyncInfo chat) : base(chat)
            {
            }

            public override void Init()
            {
#if LOG4NET
                DateTime start = DateTime.UtcNow;
#endif
                SyncInfo.ChatView.Sync();
#if LOG4NET
                DateTime stop = DateTime.UtcNow;
                double duration = stop.Subtract(start).TotalMilliseconds;
                Logger.Debug("Sync() <" + SyncInfo.ChatView.ID + ">.Sync() done, " +
                             " syncing took: " + Math.Round(duration) + " ms");
#endif
                SyncInfo.Manager.OnChatSynced(SyncInfo.ChatView);
            }

            public override void ExecuteSyncFinished()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new SyncState(SyncInfo);
            }
        }

        class SyncState : State
        {
            public SyncState(SyncInfo chat) : base(chat)
            {
            }

            public override void ExecuteRemove()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new RemovingState(SyncInfo);
            }

            public override void ExecuteSync()
            {
                // this happens for example in /rejoin
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.State = new SyncingState(SyncInfo);
            }
        }

        class RemovingState : State
        {
            public RemovingState(SyncInfo chat) : base(chat)
            {
            }

            public override void Init()
            {
                SyncInfo.Manager.OnChatRemoved(SyncInfo.ChatView);
            }

            public override void ExecuteRemoveFinished()
            {
                Trace.Call(SyncInfo.ChatModel);
                SyncInfo.Manager.Remove(SyncInfo.ChatModel);
            }

            public override void ExecuteReadyToSync()
            {
                // no-op
                // this can happen when you add and remove very fast after each other.
                // the add callback might be in a different thread and therefore be delayed
            }
        }

        class SyncInfo
        {
            State f_State;
            object SyncRoot { get; set; }
            internal ChatViewSyncManager Manager { get; set; }
            internal ChatModel ChatModel { get; set; }
            internal IChatView ChatView { get; set; }

            internal State State {
                get {
                    return f_State;
                }
                set {
                    f_State = value;
                    f_State.Init();
                }
            }

            public SyncInfo(ChatViewSyncManager manager, ChatModel chatModel)
            {
                Manager = manager;
                ChatModel = chatModel;
                SyncRoot = new object();
                State = new InitialState(this);
            }

            public void ExecuteAdd()
            {
                lock (SyncRoot) {
                    State.ExecuteAdd();
                }
            }

            public void ExecuteRemove()
            {
                lock (SyncRoot) {
                    State.ExecuteRemove();
                }
            }

            public void ExecuteRemoveFinished()
            {
                lock (SyncRoot) {
                    State.ExecuteRemoveFinished();
                }
            }

            public void ExecuteSync()
            {
                lock (SyncRoot) {
                    State.ExecuteSync();
                }
            }

            public void ExecuteReadyToSync()
            {
                lock (SyncRoot) {
                    State.ExecuteReadyToSync();
                }
            }

            public void ExecuteSyncFinished()
            {
                lock (SyncRoot) {
                    State.ExecuteSyncFinished();
                }
            }
        }

        ThreadPoolQueue WorkerQueue { set; get; }
        Dictionary<object, SyncInfo> SyncInfos { set; get; }

        public event EventHandler<ChatViewAddedEventArgs>  ChatAdded;
        public event EventHandler<ChatViewSyncedEventArgs> ChatSynced;
        public event EventHandler<ChatViewRemovedEventArgs> ChatRemoved;
        public event EventHandler<WorkerExceptionEventArgs> WorkerException;

        public ChatViewSyncManager()
        {
            WorkerQueue = new ThreadPoolQueue() {
                MaxWorkers = 4
            };
            SyncInfos = new Dictionary<object, SyncInfo>();
        }

        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        void Remove(ChatModel chatModel)
        {
            Trace.Call(chatModel);

            if (chatModel == null) {

                throw new ArgumentNullException("chatModel");
            }

            var chatKey = GetChatKey(chatModel);
#if LOG4NET
            Logger.DebugFormat("Remove() <{0}> removing from release queue",
                               chatKey);
#endif
            lock (SyncInfos) {
                SyncInfos.Remove(chatKey);
            }
        }

        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        public void QueueAdd(ChatModel chatModel)
        {
            Trace.Call(chatModel);

            if (chatModel == null) {
                throw new ArgumentNullException("chatModel");
            }

            var chat = GetOrCreateChat(chatModel);
            WorkerQueue.Enqueue(delegate {
                try {
                    chat.ExecuteAdd();
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("QueueAdd(): ExecuteAdd() threw exception!" , ex);
#endif
                    OnWorkerException(chat.ChatModel, ex);
                }
            });
        }

        public void QueueRemove(ChatModel chatModel)
        {
            Trace.Call(chatModel);

            if (chatModel == null) {
                throw new ArgumentNullException("chatModel");
            }

            SyncInfo chat;
            if (!TryGetChat(chatModel, out chat)) {
#if LOG4NET
                Logger.WarnFormat("QueueRemove() <{0}> already removed or " +
                                  "never existed", chatModel);
#endif
                return;
            }
            WorkerQueue.Enqueue(delegate {
                try {
                    chat.ExecuteRemove();
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("QueueRemove(): ExecuteRemove() threw " +
                                 "exception!", ex);
#endif
                    OnWorkerException(chat.ChatModel, ex);
                }
            });
        }

        public void QueueRemoveFinished(ChatModel chatModel)
        {
            Trace.Call(chatModel);

            if (chatModel == null) {
                throw new ArgumentNullException("chatModel");
            }

            SyncInfo chat;
            if (!TryGetChat(chatModel, out chat)) {
#if LOG4NET
                Logger.WarnFormat("QueueRemoveFinished() <{0}> already " +
                                  "removed or never existed", chatModel);
#endif
                return;
            }

            WorkerQueue.Enqueue(delegate {
                try {
                    chat.ExecuteRemoveFinished();
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("QueueRemoveFinished(): " +
                                 "ExecuteRemoveFinished() threw exception!", ex);
#endif
                    OnWorkerException(chat.ChatModel, ex);
                }
            });
        }

        bool TryGetChat(ChatModel chatModel, out SyncInfo chat)
        {
            var key = GetChatKey(chatModel);
            lock (SyncInfos) {
                return SyncInfos.TryGetValue(key, out chat);
            }
        }

        SyncInfo GetOrCreateChat(ChatModel chatModel)
        {
            var key = GetChatKey(chatModel);
            lock (SyncInfos) {
                SyncInfo chat;
                if (!SyncInfos.TryGetValue(key, out chat)) {
                    chat = new SyncInfo(this, chatModel);
                    SyncInfos.Add(key, chat);
                }
                return chat;
            }
        }

        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        public void QueueSync(ChatModel chatModel)
        {
            Trace.Call(chatModel);

            if (chatModel == null) {
                throw new ArgumentNullException("chatModel");
            }

            SyncInfo chat;
            if (!TryGetChat(chatModel, out chat)) {
#if LOG4NET
                Logger.WarnFormat("QueueSync() <{0}> unknow chat, cannot sync",
                                  chatModel);
#endif
                return;
            }

            WorkerQueue.Enqueue(delegate {
                try {
                    chat.ExecuteSync();
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("QueueSync(): ExecuteSync() threw exception!", ex);
#endif
                    OnWorkerException(chat.ChatModel, ex);
                }
            });
        }

        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        public void QueueReadyToSync(IChatView chatView)
        {
            Trace.Call(chatView);

            if (chatView == null) {
                throw new ArgumentNullException("chatView");
            }

            SyncInfo chat;
            if (!TryGetChat(chatView.ChatModel, out chat)) {
#if LOG4NET
                Logger.WarnFormat("QueueReadyToSync() <{0}> unknow chat, " +
                                  "something is wrong", chatView.ChatModel);
#endif
                return;
            }
            chat.ChatView = chatView;

            WorkerQueue.Enqueue(delegate {
                try {
                    chat.ExecuteReadyToSync();
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("QueueReadyToSync(): ExecuteReadyToSync() " +
                                 "threw exception!", ex);
#endif
                    OnWorkerException(chat.ChatModel, ex);
                }
            });
        }

        public void QueueSyncFinished(IChatView chatView)
        {
            Trace.Call(chatView);

            if (chatView == null) {
                throw new ArgumentNullException("chatView");
            }

            SyncInfo chat;
            if (!TryGetChat(chatView.ChatModel, out chat)) {
#if LOG4NET
                Logger.WarnFormat(
                    "QueueSyncFinished() <{0}> unknow chat, something is wrong",
                    chatView.ChatModel
                );
#endif
                return;
            }

            WorkerQueue.Enqueue(delegate {
                try {
                    chat.ExecuteSyncFinished();
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("QueueSyncFinished(): ExecuteSyncFinished() " +
                                 "threw exception!", ex);
#endif
                    OnWorkerException(chat.ChatModel, ex);
                }
            });
        }

        public void Clear()
        {
            Trace.Call();

            lock (SyncInfos) {
                SyncInfos.Clear();
            }
        }

        object GetChatKey(ChatModel chatModel)
        {
            if (RemotingServices.IsTransparentProxy(chatModel)) {
                // HACK: we can't use ChatModel as Dictionary as it is
                // a remoting object
                return RemotingServices.GetObjectUri(chatModel);
            }
            return chatModel;
        }

        void OnChatAdded(ChatModel chatModel, string chatId,
                         ChatType chatType, int chatPosition,
                         IProtocolManager protocolManager,
                         Type protocolManagerType)
        {
            if (ChatAdded != null) {
                ChatAdded(this,
                          new ChatViewAddedEventArgs(chatModel, chatId,
                                                     chatType, chatPosition,
                                                     protocolManager,
                                                     protocolManagerType));
            }
        }

        void OnChatSynced(IChatView chatView)
        {
            if (ChatSynced != null) {
                ChatSynced(this, new ChatViewSyncedEventArgs(chatView));
            }
        }

        void OnChatRemoved(IChatView chatView)
        {
            if (ChatRemoved != null) {
                ChatRemoved(this, new ChatViewRemovedEventArgs(chatView));
            }
        }

        void OnWorkerException(ChatModel chatModel, Exception ex)
        {
            if (WorkerException != null) {
                WorkerException(
                    this,
                    new WorkerExceptionEventArgs(chatModel, ex)
                );
            }
        }
    }

    public class ChatViewAddedEventArgs : EventArgs
    {
        public ChatModel ChatModel { get; private set; }
        public string ChatID { get; private set; }
        public ChatType ChatType { get; private set; }
        public int ChatPosition { get; private set; }
        public IProtocolManager ProtocolManager { get; private set; }
        public Type ProtocolManagerType { get; private set; }

        public ChatViewAddedEventArgs(ChatModel chatModel, string chatId,
                                      ChatType chatType, int chatPosition,
                                      IProtocolManager protocolManager,
                                      Type protocolManagerType)
        {
            ChatModel = chatModel;
            ChatID = chatId;
            ChatType = chatType;
            ChatPosition = chatPosition;
            ProtocolManager = protocolManager;
            ProtocolManagerType = protocolManagerType;
        }
    }

    public class ChatViewSyncedEventArgs : EventArgs
    {
        public IChatView ChatView { get; private set; }

        public ChatViewSyncedEventArgs(IChatView chatView)
        {
            ChatView = chatView;
        }
    }

    public class ChatViewRemovedEventArgs : EventArgs
    {
        public IChatView ChatView { get; private set; }

        public ChatViewRemovedEventArgs(IChatView chatView)
        {
            ChatView = chatView;
        }
    }

    public class WorkerExceptionEventArgs : EventArgs
    {
        public ChatModel ChatModel { get; private set; }
        public Exception Exception { get; private set; }

        public WorkerExceptionEventArgs(ChatModel chat, Exception ex)
        {
            ChatModel = chat;
            Exception = ex;
        }
    }

    public class InvalidStateException : Exception
    {
        internal InvalidStateException(string msg) :
                                  base(msg)
        {
        }
    }
}
