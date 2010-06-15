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
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class InviteToMenu : Gtk.Menu
    {
        IrcProtocolManager ProtocolManager { get; set; }
        ChatViewManager    ChatViewManager { get; set; }
        IList<PersonModel> Invitees { get; set; }
        bool               IsPopulated { get; set; }

        public InviteToMenu(IrcProtocolManager protocolManager,
                            ChatViewManager chatViewManager,
                            PersonModel invitee) :
                       this(protocolManager,
                            chatViewManager,
                            new [] { invitee })
        {
        }

        public InviteToMenu(IrcProtocolManager protocolManager,
                            ChatViewManager chatViewManager,
                            IList<PersonModel> invitees)
        {
            if (protocolManager == null) {
                throw new ArgumentNullException("protocolManager");
            }
            if (chatViewManager == null) {
                throw new ArgumentNullException("chatViewManager");
            }
            if (invitees == null) {
                throw new ArgumentNullException("invitees");
            }

            ProtocolManager = protocolManager;
            ChatViewManager = chatViewManager;
            Invitees = invitees;
        }

        protected override void OnShown()
        {
            Trace.Call();

            if (!IsPopulated) {
                IsPopulated = true;
                foreach (var chatView in ChatViewManager.Chats) {
                    if (!(chatView is GroupChatView)) {
                        continue;
                    }

                    var item = new Gtk.ImageMenuItem(chatView.Name);
                    item.Image = new Gtk.Image(GroupChatView.IconPixbuf);
                    // HACK: anonymous methods inside foreach loops needs this
                    var chat = chatView;
                    item.Activated += delegate {
                        foreach (var invitee in Invitees) {
                            ProtocolManager.CommandInvite(
                                new CommandModel(
                                    Frontend.FrontendManager,
                                    ChatViewManager.ActiveChat.ChatModel,
                                    String.Format("{0} {1}",
                                                  invitee.ID,
                                                  chat.ID)
                                )
                            );
                        }
                    };
                    item.Show();
                    Append(item);
                }
            }

            base.OnShown();
        }
    }
}
