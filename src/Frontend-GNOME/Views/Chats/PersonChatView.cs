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
using System.Collections.Generic;
using Smuxi.Engine;
using Smuxi.Common;
using System.Threading;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Person)]
    public class PersonChatView : ChatView
    {
        public static Gdk.Pixbuf IconPixbuf { get; private set; }
        public  PersonChatModel PersonChatModel { get; private set; }
        public  PersonModel     PersonModel { get; private set; }

        protected override Gtk.Image DefaultTabImage {
            get {
                return new Gtk.Image(IconPixbuf);
            }
        }

        static PersonChatView()
        {
            IconPixbuf = Frontend.LoadIcon(
                "smuxi-person-chat", 16, "person-chat_256x256.png"
            );
        }

        public override void AddMessage(MessageModel msg)
        {
            switch (msg.MessageType) {
                case MessageType.PersonChatPersonChanged:
                    ThreadPool.QueueUserWorkItem(delegate {
                        try {
                            // REMOTING CALL
                            PersonModel = PersonChatModel.Person;
                        } catch (Exception ex) {
                            Frontend.ShowException(ex);
                        }
                    });
                    return;
            }
            base.AddMessage(msg);
        }

        public PersonChatView(PersonChatModel chat) : base(chat)
        {
            Trace.Call(chat);
            
            PersonChatModel = chat;

            Add(OutputScrolledWindow);
            ShowAll();
        }

        protected PersonChatView(IntPtr handle) : base(handle)
        {
        }

        public override IList<PersonModel> Participants
        {
            get {
                var ret = new List<PersonModel>();
                ret.Add(PersonChatModel.Person);
                return ret;
            }
        }

        public override void Sync(int msgCount)
        {
            Trace.Call(msgCount);

            GLib.Idle.Add(delegate {
                TabImage.SetFromStock(Gtk.Stock.Refresh, Gtk.IconSize.Menu);
                OnStatusChanged(EventArgs.Empty);
                return false;
            });

            // REMOTING CALL 1
            PersonModel = PersonChatModel.Person;

            base.Sync(msgCount);
        }
    }
}
