/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Person)]
    public class PersonChatView : ChatView
    {
        public  PersonChatModel PersonChatModel { get; private set; }
        public  PersonModel     PersonModel { get; private set; }

        public PersonChatView(PersonChatModel chat) : base(chat)
        {
            Trace.Call(chat);
            
            PersonChatModel = chat;

            var tabImage = new Gtk.Image(
                new Gdk.Pixbuf(
                    null,
                    "person-chat.svg",
                    16,
                    16
                )
            );
            
            TabHBox.PackStart(tabImage, false, false, 2);
            TabHBox.ShowAll();
            
            Add(OutputScrolledWindow);
        }

        public override void Sync()
        {
            Trace.Call();

            PersonModel = PersonChatModel.Person;

            base.Sync();
        }
    }
}
