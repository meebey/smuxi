// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 oliver
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
using Smuxi.Common;
using Smuxi.Engine;
using System.Threading;
using System.Collections.Generic;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Person, ProtocolManagerType = typeof(XmppProtocolManager))]
    public class XmppPersonChatView : PersonChatView
    {
        private static readonly string _LibraryTextDomain = "smuxi-frontend-gnome-xmpp";
        private XmppProtocolManager XmppProtocolManager { get; set; }

        public XmppPersonChatView(PersonChatModel personChat) : base(personChat)
        {
            Trace.Call(personChat);

            OutputMessageTextView.PopulatePopup += _OnOutputMessageTextViewPopulatePopup;
        }

        private void _OnOutputMessageTextViewPopulatePopup (object o, Gtk.PopulatePopupArgs args)
        {
            if (OutputMessageTextView.IsAtUrlTag) {
                return;
            }

            Gtk.Menu popup = args.Menu;

            popup.Append(new Gtk.SeparatorMenuItem());

            Gtk.ImageMenuItem whois_item = new Gtk.ImageMenuItem(_("Whois"));
            whois_item.Activated += _OnMenuWhoisItemActivated;
            popup.Append(whois_item);

            Gtk.ImageMenuItem add2contacts_item = new Gtk.ImageMenuItem(_("Add To Contacts"));
            add2contacts_item.Activated += _OnMenuAdd2ContactsItemActivated;
            popup.Append(add2contacts_item);
            
            Gtk.ImageMenuItem invite_to_item = new Gtk.ImageMenuItem(_("Invite to"));
            Gtk.Menu invite_to_menu_item = new InviteToMenu(XmppProtocolManager,
                                                            Frontend.MainWindow.ChatViewManager,
                                                            PersonModel);
            invite_to_item.Submenu = invite_to_menu_item;
            popup.Append(invite_to_item);

            popup.ShowAll();
        }

        void _OnMenuWhoisItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            Command(String.Format("/whois", PersonModel.ID));
        }

        void _OnMenuAdd2ContactsItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
   
            Command("/contact add " + PersonModel.ID);
        }
        
        void Command(string cmd)
        {
            Trace.Call(cmd);
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    XmppProtocolManager.CommandContact(
                        new CommandModel(
                            Frontend.FrontendManager,
                            ChatModel,
                            ChatModel.ID,
                            cmd
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

        public override void Sync()
        {
            Trace.Call();

            base.Sync();

            XmppProtocolManager = (XmppProtocolManager) ProtocolManager;
        }
    }
}

