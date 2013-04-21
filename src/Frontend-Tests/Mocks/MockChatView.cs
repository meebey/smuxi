// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer
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
using Smuxi.Engine;

namespace Smuxi.Frontend
{
    public class MockChatView : IChatView
    {
        public IList<PersonModel> Participants { get; private set; }
        private IList<MessageModel> Messages { get; set; }
        private IProtocolManager ProtocolManager { get; set; }

        public void ScrollUp() {}
        public void ScrollDown() {}
        public void ScrollToStart() {}
        public void ScrollToEnd() {}
        public void Enable() {}
        public void Disable() {}
        public void Sync() {}
        public void Populate() {}
        public string ID {
            get {
                return "MagicalFakeChatView";
            }
        }
        public int Position {
            get {
                return 0;
            }
        }
        public ChatModel ChatModel {
            get {
                return null;
            }
        }

        public MockChatView()
        {
            Participants = new List<PersonModel>();
            Messages = new List<MessageModel>();
            ProtocolManager = new MockProtocolManager();
        }

        public void AddParticipant(string nick)
        {
            PersonModel pm = new PersonModel(nick, nick, "fakeNetwork", "fakeProto", ProtocolManager);
            Participants.Add(pm);
        }

        public IList<string> ParticipantNicks()
        {
            var ret = new List<string>();
            foreach (PersonModel person in Participants) {
                ret.Add(person.IdentityName);
            }
            return ret;
        }

        public void AddMessage(MessageModel msg)
        {
            Messages.Add(msg);
        }

        public int MessageCount()
        {
            return Messages.Count;
        }

        public MessageModel GetMessage(int i)
        {
            return Messages [i];
        }

        public MessageModel GetLastMessage()
        {
            return Messages [Messages.Count - 1];
        }
    }
}

