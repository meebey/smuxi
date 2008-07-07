/*
 * $Id: NetworkChatView.cs 218 2007-11-12 19:50:25Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/Chats/NetworkChatView.cs $
 * $Rev: 218 $
 * $Author: meebey $
 * $Date: 2007-11-12 20:50:25 +0100 (Mon, 12 Nov 2007) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
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
    [ChatViewInfo(ChatType = ChatType.Session)]
    public class SessionChatView : ChatView
    {
        private Gtk.Image   _TabImage;
        
        public SessionChatView(ChatModel chat) : base(chat)
        {
            Trace.Call(chat);
            
            _TabImage = new Gtk.Image(
                new Gdk.Pixbuf(
                    null,
                    "session-chat.svg",
                    16,
                    16
                )
            );
            
            TabHBox.PackStart(_TabImage, true, true, 2);
            TabHBox.ShowAll();
            
            Add(OutputScrolledWindow);
        }
        
        protected override void OnTabButtonPress(object sender, Gtk.ButtonPressEventArgs e)
        {
            Trace.Call(sender, e);
            
            // disable menu for session chats
        }
    }
}
