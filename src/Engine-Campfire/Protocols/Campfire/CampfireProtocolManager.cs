// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012 Carlos Martín Nieto <cmn@dwim.me>
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
using System.Web;
using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Smuxi.Common;
using Smuxi.Engine.Campfire;
using ServiceStack.ServiceClient.Web;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "Campfire", Description = "Campfire chat", Alias = "campfire")]
    public class CampfireProtocolManager : ProtocolManagerBase
    {
        static readonly string f_LibraryTextDomain = "smuxi-engine-campfire";

        Dictionary<ChatModel, CampfireEventStream> EventStreams { get; set; }
        int LastSentId { get; set; }
        IEnumerable<Room> Rooms { get; set; }
        DateTime RoomsUpdated { get; set; }
        TimeSpan RefreshInterval { get; set; }
        Dictionary<int, CampfirePersonModel>  Users { get; set; }
        string Key { get; set; }
        string Network { get; set; }
        Uri BaseUri { get; set; }
        ChatModel NetworkChat { get; set; }
        JsonServiceClient Client { get; set; }

        public override string Protocol {
            get {
                return "Campfire";
            }
        }

        public override string NetworkID {
            get {
                return Network;
            }
        }

        public override ChatModel Chat {
            get {
                return NetworkChat;
            }
        }

        static CampfireProtocolManager()
        {
        }

        private CampfirePersonModel CreatePerson(User user)
        {
            var person = new CampfirePersonModel(user, NetworkID, this);
            return person;
        }

        private void GetUserDetails(int id)
        {
            if (Users.ContainsKey(id) || id == 0)
                return;

            var u = Client.Get<UserResponse>(String.Format("/users/{0}.json", id)).User;
            Users[u.Id] = CreatePerson(u);
        }

        private void RefreshRooms()
        {
            if (Rooms == null ||
                RefreshInterval.CompareTo(RoomsUpdated - DateTime.Now) > 0)
                Rooms = Client.Get<RoomsResponse>("/rooms.json").Rooms;
        }

        public CampfireProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);
            RefreshInterval = TimeSpan.FromMinutes(5);
            RoomsUpdated = DateTime.MinValue;
            Users = new Dictionary<int, CampfirePersonModel>();
            EventStreams = new Dictionary<ChatModel, CampfireEventStream>();
        }

        private void FailedToConnect(string str, Exception e)
        {
            Session.AddMessageToChat(NetworkChat, CreateMessageBuilder()
                                     .AppendErrorText("{0}: {1}", str, e.Message)
                                     .ToMessage());
        }

        public override void Connect(FrontendManager fm, ServerModel server)
        {
            Trace.Call(fm, server);

            Network = server.Hostname.Substring(0, server.Hostname.IndexOf('.'));
            Host = server.Hostname;
            BaseUri = new Uri(String.Format("https://{0}", Host));

            NetworkChat = new ProtocolChatModel(Network, "Campfire " + Network, this);
            NetworkChat.InitMessageBuffer(MessageBufferPersistencyType.Volatile);
            NetworkChat.ApplyConfig(Session.UserConfig);
            Session.AddChat(NetworkChat);
            Session.SyncChat(NetworkChat);
            var msg = _("Connecting to campfire... ");
            fm.SetStatus(msg);
            var bld = CreateMessageBuilder().AppendEventPrefix().AppendText(msg);
            Session.AddMessageToChat(NetworkChat, bld.ToMessage());

            if (!server.ValidateServerCertificate) {
                var whitelist = Session.CertificateValidator.HostnameWhitelist;
                lock (whitelist) {
                    // needed for favicon
                    if (!whitelist.Contains("campfirenow.com")) {
                        whitelist.Add("campfirenow.com");
                    }
                    if (!whitelist.Contains(Host)) {
                        whitelist.Add(Host);
                    }
                }
            }

            Client = new JsonServiceClient(BaseUri.AbsoluteUri);
            var creds = new NetworkCredential(server.Username, server.Password);
            Client.Credentials = creds;

            try {
                var me = Client.Get<UserResponse>("/users/me.json").User;
                Key = me.Api_Auth_Token;
                Me = CreatePerson(me);
                // The blue color is hardcoded for now
                Me.IdentityNameColored.ForegroundColor = new TextColor(0x0000FF);
                Me.IdentityNameColored.BackgroundColor = TextColor.None;
                Me.IdentityNameColored.Bold = true;

            } catch (Exception e) {
                FailedToConnect("Failed to connect to Campfire", e);
                return;
            }

            Client.Credentials = new NetworkCredential(Key, "X");
            msg = _("Connected to campfire");
            fm.SetStatus(msg);
            bld = CreateMessageBuilder().AppendEventPrefix().AppendText(msg);
            Session.AddMessageToChat(NetworkChat, bld.ToMessage());

            // Campfire lets us know what channels the user is currently in, so
            // connect to those rooms automatically
            Rooms = Client.Get<RoomsResponse>("/rooms.json").Rooms;
            RoomsUpdated = DateTime.Now;

            var myrooms = Client.Get<RoomsResponse>("/presence.json").Rooms;
            if (myrooms.Length > 0) {
                bld = CreateMessageBuilder().
                    AppendEventPrefix().
                    AppendText("Present in {0}",
                        String.Join(", ", myrooms.Select(r => r.Name).ToArray())
                    );
                Session.AddMessageToChat(NetworkChat, bld.ToMessage());
            }

            foreach (var room in myrooms) {
                var chat = new GroupChatModel(room.Id.ToString(), room.Name, null);
                OpenChat(fm, chat);
            }
        }

        public void CommandHelp(CommandModel cd)
        {
            var builder = CreateMessageBuilder();
            builder.AppendEventPrefix();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            builder.AppendHeader(_("Campfire Commands"));
            cd.FrontendManager.AddMessageToChat(cd.Chat, builder.ToMessage());

            string[] help = {
                "connect campfire username password",
                "list",
                "uploads",
            };

            foreach (string line in help) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                cd.FrontendManager.AddMessageToChat(cd.Chat, builder.ToMessage());
            }
        }


        public void CommandJoin(CommandModel cmd)
        {
            Trace.Call(cmd);

            RefreshRooms();

            /*
             * cmd.DataArray is split at SP, but that's an allowed character
             * for Campfire. Instead of relying on that, we need to remove the "/join "
             * part and then split on ','
             */
            var chans = cmd.Parameter.Split(',');
            var list = Rooms.Where(r => chans.Any(r.Name.Equals));

            foreach(Room room in list) {
                var chat = new GroupChatModel(room.Id.ToString(), room.Name, null);
                OpenChat(cmd.FrontendManager, chat);
            }

        }

        public void CommandTopic(CommandModel cmd)
        {
            Trace.Call(cmd);

            var update = new UpdateTopicWrapper {
                room = new TopicChange {
                    topic = cmd.Parameter
                }
            };

            Client.Put<object>(String.Format("/room/{0}.json", cmd.Chat.ID), update);
        }

        public void CommandUploads(CommandModel cmd)
        {
            Trace.Call(cmd);

            var uploads = Client.Get<UploadsResponse>(String.Format("/room/{0}/uploads.json", cmd.Chat.ID)).Uploads;

            foreach (var upload in uploads) {
                var bld = CreateMessageBuilder();
                bld.AppendEventPrefix().AppendHeader(_("Upload")).AppendSpace();
                bld.AppendText(_("'{0}' ({1} B) {2}"), upload.Name, upload.Byte_Size, upload.Full_Url);
                Session.AddMessageToChat(cmd.Chat, bld.ToMessage());
            }
        }

        public void CommandSay(CommandModel cmd)
        {
            Trace.Call(cmd);
            SendMessage((GroupChatModel) cmd.Chat, cmd.Parameter);
        }

        public override bool Command(CommandModel command)
        {
            Trace.Call(command);

            bool handled = false;

            switch (command.Command) {
                case "j":
                case "join":
                    CommandJoin(command);
                    handled = true;
                    break;
                case "say":
                    CommandSay(command);
                    handled = true;
                    break;
                case "help":
                    CommandHelp(command);
                    handled = true;
                    break;
                case "topic":
                    CommandTopic(command);
                    handled = true;
                    break;
                case "uploads":
                    CommandUploads(command);
                    handled = true;
                    break;
                default: // nothing, normal chat
                    handled = true;
                    if (command.Chat is GroupChatModel)
                        SendMessage((GroupChatModel) command.Chat, command.Data);
                    break;
            }

            return handled;
        }

        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);

            RefreshRooms();
            string searchPattern = null;
            if (filter == null || String.IsNullOrEmpty(filter.Name)) {
                // full channel list
            } else {
                if (!filter.Name.StartsWith("*") && !filter.Name.EndsWith("*")) {
                    searchPattern = String.Format("*{0}*", filter.Name);
                } else {
                    searchPattern = filter.Name;
                }
            }

            List<GroupChatModel> chats = new List<GroupChatModel>(Rooms.Count());
            IEnumerable<Room> matching;

            matching = searchPattern == null ? Rooms :
                Rooms.Where(r => Pattern.IsMatch(r.Name, searchPattern));

            foreach (var room in matching) {
                GroupChatModel chat = new GroupChatModel(room.Id.ToString(), room.Name, null);
                var users = Client.Get<RoomResponse>(String.Format("/room/{0}.json", chat.ID)).Room.Users;

                /* Don't waste this data */
                lock (Users) {
                    foreach (var user in users) {
                        if (!Users.ContainsKey(user.Id))
                            Users[user.Id] = CreatePerson(user);
                    }
                }


                chat.PersonCount = users.Length;

                chat.Topic = CreateMessageBuilder().AppendMessage(room.Topic).ToMessage();
                lock (chat) {
                    chats.Add(chat);
                }
            }

            return chats;
        }

        void SendMessage(GroupChatModel chat, string text)
        {
            var message = new MessageSending { body = text, type = Campfire.MessageType.TextMessage};
            var wrapper = new MessageWrapper { message = message };
            var res = Client.Post<MessageResponse>(String.Format("/room/{0}/speak.json", chat.ID), wrapper).Message;
            ShowMessage(this, new MessageReceivedEventArgs(chat, res));
            LastSentId = res.Id;
        }

        void FormatUpload(MessageBuilder bld, PersonModel person, ChatModel chat, Message message)
        {
            // Figure out what the user uploaded, we need to issue another call for this
            var upload = Client.Get<UploadWrapper>(String.Format("/room/{0}/messages/{1}/upload.json", chat.ID, message.Id)).Upload;

            bld.AppendEventPrefix();
            bld.AppendIdendityName(person).AppendSpace();
            bld.AppendText(_("has uploaded '{0}' ({1} B) {2}"), upload.Name, upload.Byte_Size, upload.Full_Url);
        }

        void FormatEvent(MessageBuilder bld, PersonModel person, string action)
        {
            bld.AppendEventPrefix();
            bld.AppendIdendityName(person).AppendSpace();
            bld.AppendText(action);
        }

        void ShowMessage(object sender, MessageReceivedEventArgs args)
        {
            var message = args.Message;
            var chat = args.Chat;
            bool processed = true;

            if (message.Type == Campfire.MessageType.TimestampMessage)
                return;


            CampfirePersonModel person;
            lock (Users) {
                GetUserDetails(message.User_Id); /* Make sure we know who this is */
                person = Users[message.User_Id];
            }

            var bld = CreateMessageBuilder();
            bld.TimeStamp = message.Created_At.DateTime;

            switch (message.Type) {
                case Campfire.MessageType.EnterMessage:
                    // TRANSLATOR: {0} is the name of the room
                    FormatEvent(bld, person, String.Format(_("has joined {0}"), chat.Name));
                    lock (chat) {
                        if (chat.GetPerson(person.ID) == null)
                            Session.AddPersonToGroupChat(chat, person);
                    }
                    break;
                case Campfire.MessageType.KickMessage:
                case Campfire.MessageType.LeaveMessage:
                    // TRANSLATOR: {0} is the name of the room
                    FormatEvent(bld, person, String.Format(_("has left {0}"), chat.Name));
                    lock (chat) {
                        if (chat.GetPerson(person.ID) != null)
                            Session.RemovePersonFromGroupChat(chat, person);
                    }
                    break;
                case Campfire.MessageType.LockMessage:
                    // TRANSLATOR: {0} is the name of the room
                    FormatEvent(bld, person, String.Format(_("has locked {0}"), chat.Name));
                    break;
                case Campfire.MessageType.UnlockMessage:
                    // TRANSLATOR: {0} is the name of the room
                    FormatEvent(bld, person, String.Format(_("has unlocked {0}"), chat.Name));
                    break;
                case Campfire.MessageType.TopicChangeMessage:
                    var topic = CreateMessageBuilder().AppendMessage(message.Body);
                    Session.UpdateTopicInGroupChat(chat, topic.ToMessage());
                    FormatEvent(bld, person, _("has changed the topic"));
                    break;
                case Campfire.MessageType.UploadMessage:
                    FormatUpload(bld, person, chat, message);
                    break;
                case Campfire.MessageType.TextMessage:
                case Campfire.MessageType.PasteMessage:
                    processed = false;
                    break;
                default:
                    FormatEvent(bld, person, String.Format(_("has performed an unknown action"), chat.Name));
                    break;
            }

            if (processed) {
                Session.AddMessageToChat(chat, bld.ToMessage());
                return;
            }

            bool mine = person == Me;

            // Don't double-post the messages we've sent
            if (mine && message.Id <= LastSentId)
                return;

            if (mine)
                bld.AppendSenderPrefix(Me);
            else
                bld.AppendNick(person).AppendSpace();

            if (message.Type == Campfire.MessageType.TextMessage ||
                message.Type == Campfire.MessageType.TweetMessage) {
                bld.AppendMessage(message.Body);
            } else if (message.Type == Campfire.MessageType.PasteMessage) {
                bld.AppendText("\n");
                foreach (string part in message.Body.Split('\n')) {
                    bld.AppendText("    {0}\n", part);
                }
            }

            if (!mine)
                bld.MarkHighlights();

            Session.AddMessageToChat(chat, bld.ToMessage());
    }

        public override void OpenChat(FrontendManager fm, ChatModel chat_)
        {
            Trace.Call(fm, chat_);

            var room = Rooms.Single(r => r.Name.Equals(chat_.Name));
            Client.Post<object>(String.Format("/room/{0}/join.json", room.Id), null);
            room = Client.Get<RoomResponse>(String.Format("/room/{0}.json", room.Id)).Room;
            var chat = Session.GetChat(room.Name, ChatType.Group, this) as GroupChatModel;
            if (chat == null)
                chat = Session.CreateChat<GroupChatModel>(room.Id.ToString(), room.Name, this);

            var bld = CreateMessageBuilder();
            bld.AppendMessage(room.Topic);
            chat.Topic = bld.ToMessage();

            Session.AddChat(chat);

            /* Fill what we know about the users, this is only the currently-connected ones */
            lock (Users) {
                foreach (User user in room.Users) {
                    if (!Users.ContainsKey(user.Id))
                        Users[user.Id] = CreatePerson(user);
                    Session.AddPersonToGroupChat(chat, Users[user.Id]);
                }
            }

            /* Show the recent messages, then go live. FIXME: race condition */
            var recent = Client.Get<MessagesResponse>(String.Format("/room/{0}/recent.json", chat.ID)).Messages;
            foreach (Message message in recent)
                ShowMessage(this, new MessageReceivedEventArgs(chat, message));

            Session.SyncChat(chat);
            chat.IsSynced = true; // Let the part and join messages take affect

            var stream = new CampfireEventStream(chat, BaseUri, new NetworkCredential(Key, "X"));
            lock (EventStreams)
                EventStreams.Add(chat, stream);

            stream.MessageReceived += ShowMessage;
            stream.Start();
        }

        public override void CloseChat(FrontendManager fm, ChatModel ChatInfo)
        {
            var chat = GetChat(ChatInfo.ID, ChatType.Group);
            Client.Post<object>(String.Format("/room/{0}/leave.json", chat.ID), null);
            Session.RemoveChat(chat);
            lock (EventStreams) {
                var stream = EventStreams[chat];
                stream.Dispose();
                EventStreams.Remove(chat);
            }
        }

        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }

        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }

        public override void SetPresenceStatus(PresenceStatus status, string message)
        {
        }

        public override void Dispose()
        {
            Trace.Call();

            lock (EventStreams) {
                foreach (var stream in EventStreams.Values)
                    stream.Dispose();
            }

            base.Dispose();
        }

        public override string ToString()
        {
            return Network;
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }

    }
}

