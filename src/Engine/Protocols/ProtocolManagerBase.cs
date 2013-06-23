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
using System.IO;
using SysDiag = System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public abstract class ProtocolManagerBase : PermanentRemoteObject, IProtocolManager
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string       _LibraryTextDomain = "smuxi-engine";
        private Session         _Session;
        private string          _Host;
        private int             _Port;
        private bool            _IsConnected;
        private PresenceStatus  _PresenceStatus;

        public PersonModel Me { get; protected set; }

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<MessageEventArgs> MessageSent;
        public event EventHandler<MessageEventArgs> MessageReceived;

        public virtual string Host {
            get {
                return _Host;
            }
            protected set {
                _Host = value; 
            }
        }
        
        public virtual int Port {
            get {
                return _Port;
            }
            protected set {
                _Port = value; 
            }
        }
        
        public virtual bool IsConnected {
            get {
                return _IsConnected;
            }
            protected set {
                _IsConnected = value; 
            }
        }
        
        public virtual PresenceStatus PresenceStatus {
            get {
                return _PresenceStatus;
            }
            set {
                _PresenceStatus = value;
                SetPresenceStatus(value, null);
            }
        }

        public abstract string NetworkID {
            get;
        }
        
        public abstract string Protocol {
            get;
        }
        
        public abstract ChatModel Chat {
            get;
        }

        public virtual IList<ChatModel> Chats {
            get {
                IList<ChatModel> chats = new List<ChatModel>();
                lock (_Session.Chats) {
                    foreach (ChatModel chat in _Session.Chats) {
                        if (chat.ProtocolManager == this) {
                            chats.Add(chat);
                        }
                    }
                }
                return chats;
            }
        }
        
        public virtual Session Session {
            get {
                return _Session;
            }
        }

        protected bool DebugProtocol {
            get {
#if LOG4NET
                var repo = log4net.LogManager.GetRepository();
                // info is higher than debug
                return repo.Threshold <= log4net.Core.Level.Debug;
#else
                return false;
#endif
            }
        }
        
        protected ProtocolManagerBase(Session session)
        {
            Trace.Call(session);
            
            if (session == null) {
                throw new ArgumentNullException("session");
            }
            
            _Session = session;
        }
        
        public virtual void Dispose()
        {
            Trace.Call();
            
            foreach (ChatModel chat in Chats) {
                _Session.RemoveChat(chat);
            }
        }
        
        public abstract bool Command(CommandModel cmd);
        public abstract void Connect(FrontendManager fm,
                                     ServerModel server);
        public abstract void Reconnect(FrontendManager fm);
        public abstract void Disconnect(FrontendManager fm);
        
        public abstract IList<GroupChatModel> FindGroupChats(GroupChatModel filter);
        public abstract void OpenChat(FrontendManager fm, ChatModel chat);
        public abstract void CloseChat(FrontendManager fm, ChatModel chat);

        public abstract void SetPresenceStatus(PresenceStatus status,
                                               string message);

        protected void NotConnected(CommandModel cmd)
        {
            var msg = CreateMessageBuilder();
            msg.AppendEventPrefix();
            msg.AppendText(_("Not connected to server"));
            Session.AddMessageToFrontend(cmd, msg.ToMessage());
        }

        protected void NotEnoughParameters(CommandModel cmd)
        {
            var msg = CreateMessageBuilder();
            msg.AppendEventPrefix();
            msg.AppendText(_("Not enough parameters for {0} command"),
                           cmd.Command);
            Session.AddMessageToFrontend(cmd, msg.ToMessage());
        }

        protected virtual void OnConnected(EventArgs e)
        {
            Trace.Call(e);

            var msg = CreateMessageBuilder();
            msg.AppendEventPrefix();
            msg.AppendText(_("Connected to {0}"), NetworkID);
            Session.AddMessageToChat(Chat, msg.ToMessage());

            _PresenceStatus = PresenceStatus.Online;

            Session.UpdateNetworkStatus();

            if (Connected != null) {
                Connected(this, e);
            }

            var hooks = new HookRunner("protocol-manager", "on-connected");
            hooks.Environments.Add(new ChatHookEnvironment(Chat));
            hooks.Environments.Add(new ProtocolManagerHookEnvironment(this));

            var cmdChar = (string) Session.UserConfig["Interface/Entry/CommandCharacter"];
            hooks.Commands.Add(new SessionHookCommand(Session, Chat, cmdChar));
            hooks.Commands.Add(new ProtocolManagerHookCommand(this, Chat, cmdChar));

            // show time
            hooks.Init();
            hooks.Run();
        }
        
        protected virtual void OnDisconnected(EventArgs e)
        {
            Trace.Call(e);

            var msg = CreateMessageBuilder();
            msg.AppendEventPrefix();
            msg.AppendText(_("Disconnected from {0}"), NetworkID);
            Session.AddMessageToChat(Chat, msg.ToMessage());

            _PresenceStatus = PresenceStatus.Offline;

            Session.UpdateNetworkStatus();
            
            if (Disconnected != null) {
                Disconnected(this, e);
            }

            var hooks = new HookRunner("protocol-manager", "on-disconnected");
            hooks.Environments.Add(new ChatHookEnvironment(Chat));
            hooks.Environments.Add(new ProtocolManagerHookEnvironment(this));

            var cmdChar = (string) Session.UserConfig["Interface/Entry/CommandCharacter"];
            hooks.Commands.Add(new SessionHookCommand(Session, Chat, cmdChar));
            hooks.Commands.Add(new ProtocolManagerHookCommand(this, Chat, cmdChar));

            // show time
            hooks.Init();
            hooks.Run();
        }
        
        protected virtual void OnMessageSent(MessageEventArgs e)
        {
            Trace.Call(e);

            if (MessageSent != null) {
                MessageSent(this, e);
            }

            var hooks = new HookRunner("protocol-manager", "on-message-sent");
            hooks.Environments.Add(new ChatHookEnvironment(e.Chat));
            hooks.Environments.Add(new MessageHookEnvironment(e.Message));
            hooks.Environments.Add(new ProtocolManagerHookEnvironment(this));
            if (String.IsNullOrEmpty(e.Sender)) {
                hooks.EnvironmentVariables.Add("SENDER", Me.ID);
            } else {
                hooks.EnvironmentVariables.Add("SENDER", e.Sender);
            }
            hooks.EnvironmentVariables.Add("RECEIVER", e.Receiver);

            var cmdChar = (string) Session.UserConfig["Interface/Entry/CommandCharacter"];
            hooks.Commands.Add(new SessionHookCommand(Session, e.Chat, cmdChar));
            hooks.Commands.Add(new ProtocolManagerHookCommand(this, e.Chat, cmdChar));

            // show time
            hooks.Init();
            hooks.Run();
        }

        protected virtual void OnMessageReceived(MessageEventArgs e)
        {
            Trace.Call(e);

            if (MessageReceived != null) {
                MessageReceived(this, e);
            }

            var hooks = new HookRunner("protocol-manager", "on-message-received");
            hooks.Environments.Add(new ChatHookEnvironment(e.Chat));
            hooks.Environments.Add(new MessageHookEnvironment(e.Message));
            hooks.Environments.Add(new ProtocolManagerHookEnvironment(this));
            hooks.EnvironmentVariables.Add("SENDER", e.Sender);
            if (String.IsNullOrEmpty(e.Receiver)) {
                hooks.EnvironmentVariables.Add("RECEIVER", Me.ID);
            } else {
                hooks.EnvironmentVariables.Add("RECEIVER", e.Receiver);
            }

            var cmdChar = (string) Session.UserConfig["Interface/Entry/CommandCharacter"];
            hooks.Commands.Add(new SessionHookCommand(Session, e.Chat, cmdChar));
            hooks.Commands.Add(new ProtocolManagerHookCommand(this, e.Chat, cmdChar));

            // show time
            hooks.Init();
            hooks.Run();
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
        
        protected ChatModel GetChat(string id, ChatType chatType)
        {
            return _Session.GetChat(id, chatType, this);
        }

        protected virtual T GetPerson<T>(ChatModel chat, string personId) where T : PersonModel
        {
            if (personId == null) {
                throw new ArgumentNullException("personId");
            }

            T person = null;
            if (chat is GroupChatModel) {
                var groupChat = (GroupChatModel) chat;
                person = (T) groupChat.GetPerson(personId);
            } else if (chat is PersonChatModel) {
                var personChat = (PersonChatModel) chat;
                if (personId == personChat.Person.ID) {
                    person = (T) personChat.Person;
                } else if (personId == Me.ID) {
                    person = (T) Me;
                }
            }

            return person;
        }

        protected MessageBuilder CreateMessageBuilder()
        {
            return CreateMessageBuilder<MessageBuilder>();
        }

        protected virtual T CreateMessageBuilder<T>() where T : MessageBuilder, new()
        {
            var builder = new T();
            builder.Me = Me;
            builder.ApplyConfig(Session.UserConfig);
            return builder;
        }

        protected virtual void DebugRead(string data)
        {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            if (Chat == null) {
                return;
            }
            if (!DebugProtocol) {
                return;
            }

            var msgBuilder = CreateMessageBuilder();
            msgBuilder.MessageType = MessageType.Event;
            // HACK: extra leading space to align with "-!- "
            // HACK: extra trailing space to align with "WRITE: "
            msgBuilder.AppendText("    READ:  ");
            msgBuilder.AppendText(data);
            Session.AddMessageToChat(Chat, msgBuilder.ToMessage());
        }

        protected virtual void DebugWrite(string data)
        {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            if (Chat == null) {
                return;
            }
            if (!DebugProtocol) {
                return;
            }

            var msgBuilder = CreateMessageBuilder();
            msgBuilder.MessageType = MessageType.Event;
            // HACK: extra leading space to align with "-!- "
            msgBuilder.AppendText("    WRITE: ");
            msgBuilder.AppendText(data);
            Session.AddMessageToChat(Chat, msgBuilder.ToMessage());
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public ChatModel Chat { get; protected set; }
        public MessageModel Message { get; protected set; }
        public string Sender { get; protected set; }
        public string Receiver { get; protected set; }

        public MessageEventArgs(ChatModel chat, MessageModel msg,
                                string sender, string receiver)
        {
            Chat = chat;
            Message = msg;
            Sender = sender;
            Receiver = receiver;
        }
    }
}
