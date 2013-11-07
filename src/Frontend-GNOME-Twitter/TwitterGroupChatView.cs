// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Andrés G. Aragoneses <knocte@gmail.com>
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
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Group, ProtocolManagerType = typeof(TwitterProtocolManager))]
    public class TwitterGroupChatView : GroupChatView
    {
        static readonly string LibraryTextDomain = "smuxi-frontend-gnome-twitter";
        TwitterProtocolManager TwitterProtocolManager { get; set; }

        public TwitterGroupChatView(GroupChatModel groupChat) : base(groupChat)
        {
        }

        protected override void OnPersonMenuShown(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            foreach (var child in PersonMenu.Children) {
                PersonMenu.Remove(child);
            }

            base.OnPersonMenuShown(sender, e);

            if (ID == TwitterChatType.FriendsTimeline.ToString() && Frontend.EngineVersion >= new Version(0, 10)) {
                Gtk.ImageMenuItem query_item = new Gtk.ImageMenuItem(_("Unfollow"));
                query_item.Activated += OnUserListMenuUnfollowActivated;
                PersonMenu.Append(query_item);

                PersonMenu.ShowAll();
            }
        }

        void OnUserListMenuUnfollowActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            var persons = GetSelectedPersons();
            if (persons == null) {
                return;
            }

            foreach (var person in persons) {
                var per = person;
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        TwitterProtocolManager.CommandUnfollow(
                            new CommandModel(
                                Frontend.FrontendManager,
                                ChatModel,
                                per.ID
                            )
                        );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        public override void Sync()
        {
            Trace.Call();

            base.Sync();

            TwitterProtocolManager = (TwitterProtocolManager) ProtocolManager;
        }

        static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, LibraryTextDomain);
        }
    }
}

