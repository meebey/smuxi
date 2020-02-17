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
using System.Threading;

namespace Smuxi.Frontend.Gnome
{
    public class InviteToMenu : Gtk.Menu
    {
        XmppProtocolManager ProtocolManager { get; set; }
        ChatViewManager    ChatViewManager { get; set; }
        IList<PersonModel> Invitees { get; set; }
        bool               IsPopulated { get; set; }

        public InviteToMenu(XmppProtocolManager protocolManager,
                            ChatViewManager chatViewManager,
                            PersonModel invitee) :
                       this(protocolManager,
                            chatViewManager,
                            new [] { invitee })
        {
        }

        public InviteToMenu(XmppProtocolManager protocolManager,
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
                    if (!(chatView is XmppGroupChatView)) {
                        // only invite to group chats
                        continue;
                    }
                    if (chatView == ChatViewManager.ActiveChat) {
                        // don't need to add current chat to invite list
                        continue;
                    }
                    if (chatView.ProtocolManager != ProtocolManager) {
                        // only add chats from current server
                        continue;
                    }
                    var groupChatView = (XmppGroupChatView) chatView;
                    if (groupChatView.IsContactList) {
                        // ignore our abused groupchatview
                        continue;
                    }

                    var item = new Gtk.ImageMenuItem(chatView.Name);
                    item.Image = new Gtk.Image(GroupChatView.IconPixbuf);
                    var chatid = chatView.ID;
                    item.Activated += delegate {
                        var inviteFromChatModel = ChatViewManager.ActiveChat.ChatModel;
                        ThreadPool.QueueUserWorkItem(delegate {
                            try {
                                for (int i = 0; i < Invitees.Count; i++) {
                                    ProtocolManager.CommandInvite(
                                        new CommandModel(
                                            Frontend.FrontendManager,
                                            inviteFromChatModel,
                                            chatid + " " + Invitees[i].ID
                                        )
                                     );
                                }
                            } catch (Exception ex) {
                                Frontend.ShowException(ex);
                            }
                        });
                    };
                    item.Show();
                    Append(item);
                }
            }

            base.OnShown();
        }
    }
}
