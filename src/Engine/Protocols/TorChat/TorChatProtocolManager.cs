// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Smuxi.Common;

#if TOR
namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "TorChat", Description = "TorChat", Alias = "tor")]
    public class TorChatProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        const string LibraryTextDomain = "smuxi-engine";
        ChatModel ProtocolChat { get; set; }

        const char MSG_SEPARATOR = '\n';
        byte[] ReceiveBuffer { get; set; }

        public override string NetworkID {
            get {
                return "TorChat";
            }
        }

        public override string Protocol {
            get {
                return "TorChat";
            }
        }

        public override ChatModel Chat {
            get {
                return ProtocolChat;
            }
        }

        public TorChatProtocolManager(Session session) : base(session)
        {
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
                }
                return handled;
            } else {
                //CommandMessage(cmd);
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
                AppendHeader(_("TorChat Commands"));
            Session.AddMessageToFrontend(cmd, builder.ToMessage());

            string[] help = {
                "connect tor",
            };

            foreach (string line in help) {
                builder = CreateMessageBuilder();
                builder.AppendEventPrefix();
                builder.AppendText(line);
                Session.AddMessageToFrontend(cmd, builder.ToMessage());
            }
        }

        public override void Connect(FrontendManager fm, ServerModel server)
        {
            Trace.Call(fm, server);
        }

        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }

        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }

        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);

            return new List<GroupChatModel>();
        }

        public override void OpenChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);
        }

        public override void CloseChat(FrontendManager fm, ChatModel chatInfo)
        {
            Trace.Call(fm, chatInfo);
        }

        public override void SetPresenceStatus(PresenceStatus status, string message)
        {
            Trace.Call(status, message);
        }

        void OnReceived(byte[] buffer)
        {
            var msgs = new List<List<byte>>();
            var msg = new List<byte>(buffer.Length);
            for (int i = 0; i < buffer.Length; i++) {
                var value = buffer[i];
                if (value == MSG_SEPARATOR) {
                    msgs.Add(msg);
                    msg = new List<byte>(buffer.Length);
                    continue;
                }
                msg.Add(value);
            }
            // remaining unfinished message
            ReceiveBuffer = msg.ToArray();
            /*
            byte[] msg;
            do {
                msg = buffer.TakeWhile((@byte) => @byte != MSG_SEPARATOR);
                buffer = buffer.Skip(msg.Length);
            } while (buffer.Length > 0);
            */
        }

        string DecodeMessage(string msg)
        {
            return msg.Replace(@"\r\n", @"\n").Replace(@"\n", "\n");
        }

        static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, LibraryTextDomain);
        }
    }
}
#endif