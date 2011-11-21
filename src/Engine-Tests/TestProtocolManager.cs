// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "Test", Description = "Protocol manager for test-cases", Alias = "test")]
    public class TestProtocolManager : ProtocolManagerBase
    {
        private string f_Protocol = "TestProt";
        private string f_NetworkID = "TESTnet";

        public override string NetworkID {
            get {
                return f_NetworkID;
            }
        }

        public override string Protocol {
            get {
                return f_Protocol;
            }
        }

        public override ChatModel Chat {
            get {
                throw new System.NotImplementedException();
            }
        }

        public TestProtocolManager(Session session) : base(session)
        {
        }

        public void SetProtocol(string value)
        {
            f_Protocol = value;
        }

        public void SetNetworkID(string value)
        {
            f_NetworkID = value;
        }

        public override bool Command (CommandModel cmd)
        {
            throw new System.NotImplementedException();
        }

        public override void Connect (FrontendManager fm, ServerModel server)
        {
            throw new System.NotImplementedException();
        }

        public override void Reconnect (FrontendManager fm)
        {
            throw new System.NotImplementedException();
        }

        public override void Disconnect (FrontendManager fm)
        {
           throw new System.NotImplementedException();
        }

        public override System.Collections.Generic.IList<GroupChatModel> FindGroupChats (GroupChatModel filter)
        {
            throw new System.NotImplementedException();
        }

        public override void OpenChat (FrontendManager fm, ChatModel chat)
        {
            throw new System.NotImplementedException();
        }

        public override void CloseChat (FrontendManager fm, ChatModel chat)
        {
            throw new System.NotImplementedException();
        }

        public override void SetPresenceStatus (PresenceStatus status, string message)
        {
            throw new System.NotImplementedException();
        }
    }
}

