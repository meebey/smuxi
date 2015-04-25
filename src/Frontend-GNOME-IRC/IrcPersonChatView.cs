/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008, 2010-2011 Mirco Bauer <meebey@meebey.net>
 * Copyright (c) 2013 Andrés G. Aragoneses <knocte@gmail.com>
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
using System.Collections.Generic;
using System.Threading;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Person, ProtocolManagerType = typeof(IrcProtocolManager))]
    public class IrcPersonChatView : PersonChatView
    {
        private static readonly string _LibraryTextDomain = "smuxi-frontend-gnome-irc";
        IrcProtocolManager IrcProtocolManager;

        public IrcPersonChatView(PersonChatModel personChat) : base(personChat)
        {
            Trace.Call(personChat);

            OutputMessageTextView.PopulatePopup += OnOutputMessageTextViewPopulatePopup;
        }

        public override void Sync(int msgCount)
        {
            Trace.Call(msgCount);

            base.Sync(msgCount);

            IrcProtocolManager = (IrcProtocolManager) ProtocolManager;
        }

        protected override void OnTabMenuShown(object sender, EventArgs e)
        {
            base.OnTabMenuShown(sender, e);

            var stack = new Stack<Gtk.MenuItem>();
            foreach (var menu_item in CreateContextMenuItems()) {
                stack.Push(menu_item);
            }
            TabMenu.Prepend(new Gtk.SeparatorMenuItem());
            while (stack.Count != 0) {
                TabMenu.Prepend(stack.Pop());
            }
            TabMenu.ShowAll();
        }

        void OnOutputMessageTextViewPopulatePopup(object o, Gtk.PopulatePopupArgs args)
        {
            if (OutputMessageTextView.IsAtUrlTag) {
                return;
            }

            Gtk.Menu popup = args.Menu;

            popup.Append(new Gtk.SeparatorMenuItem());
            foreach (var menu_item in CreateContextMenuItems()) {
                popup.Append(menu_item);
            }

            popup.ShowAll();
        }

        IEnumerable<Gtk.MenuItem> CreateContextMenuItems()
        {
            if (IrcProtocolManager == null) {
                // we are not synced yet
                yield break;
            }

            Gtk.ImageMenuItem whois_item = new Gtk.ImageMenuItem(_("Whois"));
            whois_item.Activated += OnMenuWhoisItemActivated;
            yield return whois_item;

            Gtk.ImageMenuItem ctcp_item = new Gtk.ImageMenuItem(_("CTCP"));
            Gtk.Menu ctcp_menu_item = new CtcpMenu(IrcProtocolManager,
                                                   Frontend.MainWindow.ChatViewManager,
                                                   PersonModel);
            ctcp_item.Submenu = ctcp_menu_item;
            yield return ctcp_item;

            Gtk.ImageMenuItem invite_to_item = new Gtk.ImageMenuItem(_("Invite to"));
            Gtk.Menu invite_to_menu_item = new InviteToMenu(IrcProtocolManager,
                                                            Frontend.MainWindow.ChatViewManager,
                                                            PersonModel);
            invite_to_item.Submenu = invite_to_menu_item;
            yield return invite_to_item;
        }

        void OnMenuWhoisItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    IrcProtocolManager.CommandWhoIs(
                        new CommandModel(
                            Frontend.FrontendManager,
                            ChatModel,
                            ID
                        )
                     );
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            });
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
