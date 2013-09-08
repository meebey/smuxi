// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012-2013 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using JabbR.Client;
using JabbR.Client.Models;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Client.Http;
using Smuxi.Common;

namespace Smuxi.Engine
{
    // https://github.com/davidfowl/Jabbot/blob/master/Jabbot/Bot.cs
    // https://github.com/davidfowl/JabbR/blob/master/JabbR/Hubs/Chat.cs
    [ProtocolManagerInfo(Name = "JabbR", Description = "JabbR Chat", Alias = "jabbr")]
    public class JabbrProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        const string LibraryTextDomain = "smuxi-engine-jabbr";
        ChatModel ProtocolChat { get; set; }
        JabbRClient Client { get; set; }
        string Username { get; set; }
        ServerModel Server { get; set; }

        public override string NetworkID {
            get {
                if (Server == null) {
                    return Protocol;
                }
                return Server.Hostname;
            }
        }

        public override string Protocol {
            get {
                return "JabbR";
            }
        }

        public override ChatModel Chat {
            get {
                return ProtocolChat;
            }
        }

        public JabbrProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);
        }

        public override bool Command(CommandModel cmd)
        {
            Trace.Call(cmd);

            if (cmd.IsCommand) {
                var handled = false;
                switch (cmd.Command) {
                    case "help":
                        CommandHelp(cmd);
                        handled = true;
                        break;
                    case "j":
                    case "join":
                        CommandJoin(cmd);
                        handled = true;
                        break;
                }
                return handled;
            } else {
                CommandMessage(cmd);
            }
            return true;
        }

        public void CommandHelp(CommandModel cmd)
        {
            Trace.Call(cmd);

            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            var builder = CreateMessageBuilder().
                AppendEventPrefix().
                AppendHeader(_("JabbR Commands"));
            cmd.FrontendManager.AddMessageToChat(cmd.Chat, builder.ToMessage());

            string[] help = {
                "connect jabbr username password",
                "join"
            };

            foreach (string line in help) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                cmd.FrontendManager.AddMessageToChat(cmd.Chat, builder.ToMessage());
            }
        }

        public void CommandJoin(CommandModel cmd)
        {
            Trace.Call(cmd);

            if (String.IsNullOrEmpty(cmd.Parameter)) {
                NotEnoughParameters(cmd);
                return;
            }

            try {
                Client.JoinRoom(cmd.Parameter);
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error(ex);
#endif
                var msg = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendErrorText(_("Joining room failed. Reason: {0}"),
                                    ex.Message).
                    ToMessage();
                cmd.FrontendManager.AddMessageToChat(cmd.Chat, msg);
            }
        }

        public void CommandMessage(CommandModel cmd)
        {
            Trace.Call(cmd);

            try {
                switch (cmd.Chat.ChatType) {
                    case ChatType.Group:
                        Client.Send(cmd.Data, cmd.Chat.ID).Wait();
                        break;
                    case ChatType.Person:
                        Client.SendPrivateMessage(cmd.Chat.ID, cmd.Data).Wait();
                        break;
                }
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error(ex);
#endif
                var msg = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendErrorText(_("Sending message failed. Reason: {0}"),
                                    ex.Message).
                    ToMessage();
                cmd.FrontendManager.AddMessageToChat(cmd.Chat, msg);
            }
        }

        public override void Connect(FrontendManager fm, ServerModel server)
        {
            Trace.Call(fm, server);

            Server = server;
            Username = server.Username;
            var chatName = String.Format("{0} {1}", Protocol, NetworkID);
            ProtocolChat = new ProtocolChatModel(NetworkID, chatName, this);
            ProtocolChat.InitMessageBuffer(MessageBufferPersistencyType.Volatile);
            Session.AddChat(Chat);
            Session.SyncChat(Chat);

            try {
                string url;
                if (server.Hostname.StartsWith("http://") ||
                    server.Hostname.StartsWith("https://")) {
                    url = server.Hostname;
                } else {
                    if (server.UseEncryption && server.Port == 443) {
                        url = String.Format("https://{0}", server.Hostname);
                    } else if (server.UseEncryption) {
                        url = String.Format("https://{0}:{1}",
                                            server.Hostname, server.Port);
                    } else if (!server.UseEncryption && server.Port == 80) {
                        url = String.Format("http://{0}", server.Hostname);
                    } else {
                        url = String.Format("http://{0}:{1}",
                                            server.Hostname, server.Port);
                    }
                }
                // HACK: SignalR's ServerSentEventsTransport times out on Mono
                // for some reason and then fallbacks to LongPollingTransport
                // this takes 10 seconds though, so let's go LP directly
                Func<IClientTransport> transport = null;
                if (Type.GetType("Mono.Runtime") == null) {
                    transport = () => new AutoTransport(new DefaultHttpClient());
                } else {
                    transport = () => new LongPollingTransport();
                }
                var authProvider = new DefaultAuthenticationProvider(url);
                Client = new JabbRClient(url, authProvider, transport);
                Client.AutoReconnect = true;
                Client.MessageReceived += OnMessageReceived;
                Client.MeMessageReceived += OnMeMessageReceived;
                Client.UserLeft += OnUserLeft;
                Client.UserJoined += OnUserJoined;
                Client.JoinedRoom += OnJoinedRoom;
                Client.PrivateMessage += OnPrivateMessage;

                Me = CreatePerson(Username);
                Me.IdentityNameColored.ForegroundColor = new TextColor(0, 0, 255);
                Me.IdentityNameColored.BackgroundColor = TextColor.None;
                Me.IdentityNameColored.Bold = true;

                Connect();
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error(ex);
#endif
                var msg = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendErrorText(_("Connection failed! Reason: {0}"),
                                    ex.Message).
                    ToMessage();
                Session.AddMessageToChat(ProtocolChat, msg);
            }
        }

        void Connect()
        {
            Trace.Call();

            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                    AppendText(_("Connecting to {0}..."), Client.SourceUrl).
                    ToMessage();
            Session.AddMessageToChat(ProtocolChat, msg);

            var res = Client.Connect(Server.Username, Server.Password);
            res.Wait();
            // HACK: this event can only be subscribed if we have made an
            // actual connection o_O
            Client.Disconnected += OnDisconnected;
            IsConnected = true;
            OnConnected(EventArgs.Empty);
            OnLoggedOn(res.Result.Rooms);
        }

        void OnPrivateMessage(string fromUserName, string toUserName, string message)
        {
            Trace.Call(fromUserName, toUserName, message);

            string targetChat;
            string targetUser;
            if (fromUserName == Username) {
                targetChat = toUserName;
                targetUser = toUserName;
            } else {
                targetChat = fromUserName;
                targetUser = fromUserName;
            }
            var chat = (PersonChatModel) GetChat(targetChat, ChatType.Person);
            if (chat == null) {
                var person = CreatePerson(targetUser);
                chat = new PersonChatModel(person, targetUser, targetUser, this);
                chat.InitMessageBuffer(MessageBufferPersistencyType.Volatile);
                Session.AddChat(chat);
                Session.SyncChat(chat);
            }
            var builder = CreateMessageBuilder<JabbrMessageBuilder>();
            if (fromUserName == Username) {
                builder.AppendSenderPrefix(Me);
            } else {
                builder.AppendSenderPrefix(chat.Person, true);
            }
            builder.AppendMessage(message);
            Session.AddMessageToChat(chat, builder.ToMessage());
        }

        void OnDisconnected()
        {
            Trace.Call();

            foreach (var chat in Chats) {
                // don't disable the protocol chat, else the user loses all
                // control for the protocol manager! e.g. after a manual
                // reconnect or server-side disconnect
                if (chat.ChatType == ChatType.Protocol) {
                    continue;
                }

                Session.DisableChat(chat);
            }

            IsConnected = false;
            OnDisconnected(EventArgs.Empty);
        }

        void OnJoinedRoom(Room room)
        {
            Trace.Call(room);

            var groupChat = new GroupChatModel(room.Name, room.Name, this);
            groupChat.InitMessageBuffer(MessageBufferPersistencyType.Volatile);

            var task = Client.GetRoomInfo(room.Name);
            task.Wait();
            // check task.Exception
            var roomInfo = task.Result;

            groupChat.Topic = CreateMessageBuilder<JabbrMessageBuilder>().
                AppendMessage(roomInfo.Topic).
                ToMessage();
            foreach (var user in roomInfo.Users) {
                groupChat.UnsafePersons.Add(user.Name,
                                            CreatePerson(user));
            }
            // add ourself if needed
            if (!groupChat.UnsafePersons.ContainsKey(Username)) {
                groupChat.UnsafePersons.Add(Username, Me);
            }
            Session.AddChat(groupChat);
            Session.SyncChat(groupChat);
        }

        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            var msg = CreateMessageBuilder().
                AppendEventPrefix().
                    AppendText(_("Reconnecting to {0}..."), Server.Hostname).
                    ToMessage();
            Session.AddMessageToChat(Chat, msg);
            try {
                Client.Disconnect();
                Connect();
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("Reconnect(): Exception during reconnect", ex);
#endif
            }
        }

        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);

            Client.Disconnect();
        }

        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);

            var res = Client.GetRooms();
            res.Wait();
            // res.Exception
            var groupChats = new List<GroupChatModel>();
            foreach (var room in res.Result) {
                var groupChat = new GroupChatModel(room.Name, room.Name, this);
                groupChat.InitMessageBuffer(MessageBufferPersistencyType.Volatile);
                groupChat.PersonCount = room.Count;
                groupChats.Add(groupChat);
            }
            return groupChats;
        }

        public override void OpenChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);

            switch (chat.ChatType) {
                case ChatType.Person:
                    var personChat = (PersonChatModel) GetChat(chat.ID, ChatType.Person);
                    if (personChat != null) {
                        return;
                    }
                    var person = CreatePerson(chat.ID);
                    personChat = new PersonChatModel(person, chat.ID, chat.ID, this);
                    personChat.InitMessageBuffer(MessageBufferPersistencyType.Volatile);
                    Session.AddChat(personChat);
                    Session.SyncChat(personChat);
                    break;
                case ChatType.Group:
                    Client.JoinRoom(chat.ID);
                    break;
            }
        }

        public override void CloseChat(FrontendManager fm, ChatModel chatInfo)
        {
            Trace.Call(fm, chatInfo);

            // get real chat object from session
            var chat = GetChat(chatInfo.ID, chatInfo.ChatType);
            if (chat == null) {
#if LOG4NET
                Logger.Error("CloseChat(): Session.GetChat(" +
                             chatInfo.ID + ", " + chatInfo.ChatType + ")" +
                             " returned null!");
#endif
                return;
            }

            switch (chat.ChatType) {
                case ChatType.Person:
                    Session.RemoveChat(chat);
                    break;
                case ChatType.Group:
                    Client.LeaveRoom(chat.ID);
                    break;
            }
        }

        public override void SetPresenceStatus(PresenceStatus status, string message)
        {
            //throw new NotImplementedException();
        }

        public override string ToString()
        {
            string result = Chat.Name;
            if (!IsConnected) {
                result += " (" + _("not connected") + ")";
            }
            return result;
        }

        void OnMessageReceived(Message message, string room)
        {
            Trace.Call(message, room);

            var chat = GetChat(room, ChatType.Group) ?? ProtocolChat;
            AddMessage(chat, message);
        }

        void AddMessage(ChatModel chat, Message msg)
        {
            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            string content = msg.Content;
            string name = msg.User.Name;

            var builder = CreateMessageBuilder<JabbrMessageBuilder>();
            if (msg.When != default(DateTimeOffset)) {
                builder.TimeStamp = msg.When.UtcDateTime;
            }
            var sender = name == Username ? Me : CreatePerson(name);
            builder.AppendSenderPrefix(sender);
            builder.AppendMessage(content);
            if (sender != Me) {
                builder.MarkHighlights();
            }
            Session.AddMessageToChat(chat, builder.ToMessage());
        }

        void OnMeMessageReceived(string userName, string content, string roomName)
        {
            Trace.Call(userName, content, roomName);

            var chat = GetChat(roomName, ChatType.Group) ?? ProtocolChat;
            var builder = CreateMessageBuilder<JabbrMessageBuilder>().
                AppendActionPrefix().
                AppendIdendityName(GetPerson<PersonModel>(chat, userName)).
                AppendSpace().
                AppendMessage(content);
            if (userName != Username) {
                builder.MarkHighlights();
            }
            var msg = builder.ToMessage();
            Session.AddMessageToChat(chat, msg);
        }

        void OnUserJoined(User user, string room, bool isOwner)
        {
            Trace.Call(user, room, isOwner);

            var chat = (GroupChatModel) GetChat(room, ChatType.Group);
            if (chat == null) {
                return;
            }

            var person = CreatePerson(user.Name);
            lock (chat) {
                if (chat.Persons.ContainsKey(person.ID)) {
#if LOG4NET
                    Logger.Warn("OnUserJoined(): person already on chat, ignoring...");
#endif
                    return;
                }
                Session.AddPersonToGroupChat(chat, person);
            }
        }

        void OnUserLeft(User user, string room)
        {
            Trace.Call(user, room);

            var chat = (GroupChatModel) GetChat(room, ChatType.Group);
            if (chat == null) {
                return;
            }

            if (user.Name == Username) {
                Session.RemoveChat(chat);
                return;
            }

            PersonModel person = null;
            if (chat.Persons.TryGetValue(user.Name, out person)) {
                Session.RemovePersonFromGroupChat(chat, person);
            }
        }

        void OnLoggedOn(IEnumerable<Room> rooms)
        {
            Trace.Call(rooms);

            try {
                foreach (var room in rooms) {
                    var groupChat = (GroupChatModel) GetChat(room.Name, ChatType.Group);
                    bool newChat;
                    if (groupChat == null) {
                        groupChat = new GroupChatModel(room.Name, room.Name, this);
                        groupChat.InitMessageBuffer(MessageBufferPersistencyType.Volatile);
                        newChat = true;
                    } else {
                        groupChat.UnsafePersons.Clear();
                        newChat = false;
                    }

                    var task = Client.GetRoomInfo(room.Name);
                    task.Wait();
                    // check task.Exception
                    var roomInfo = task.Result;
                    groupChat.Topic = CreateMessageBuilder<JabbrMessageBuilder>().
                        AppendMessage(roomInfo.Topic).
                        ToMessage();
                    foreach (var user in roomInfo.Users) {
                        groupChat.UnsafePersons.Add(user.Name,
                                                    CreatePerson(user));
                    }
                    // add ourself if needed
                    if (!groupChat.UnsafePersons.ContainsKey(Username)) {
                        groupChat.UnsafePersons.Add(Username, Me);
                    }
                    foreach (var msg in roomInfo.RecentMessages) {
                        AddMessage(groupChat, msg);
                    }
                    if (newChat) {
                        Session.AddChat(groupChat);
                    } else {
                        Session.EnableChat(groupChat);
                    }
                    Session.SyncChat(groupChat);
                }
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error(ex);
#endif
                var msg = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendErrorText(_("Retrieving chat information failed " +
                                      "Reason: {0}"),
                                    ex.Message).
                    ToMessage();
                Session.AddMessageToChat(ProtocolChat, msg);
            }
        }

        PersonModel CreatePerson(User user)
        {
            return CreatePerson(user.Name);
        }

        PersonModel CreatePerson(string username)
        {
            return new PersonModel(username, username, NetworkID, Protocol, this);
        }

        static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, LibraryTextDomain);
        }
    }
}
