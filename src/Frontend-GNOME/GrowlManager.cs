// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using Growl.Connector;
using Growl.CoreLibrary;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class GrowlManager
    {
        GrowlConnector Growl { get; set; }

        public GrowlManager()
        {
            Growl = new GrowlConnector("smuxi", "192.168.0.20", 23053);
            var app = new Application("Smuxi");
            app.Icon = new BinaryData(File.ReadAllBytes("/usr/share/icons/hicolor/48x48/apps/smuxi-frontend-gnome.png"));
            var type = new NotificationType("MSG_HIGHLIGHT", "Message Highlight");
            List<NotificationType> types = new List<NotificationType>();
            types.Add(type);
            Growl.Register(app, types.ToArray());
        }

        void OnChatViewMessageHighlighted(object sender,
                                          MessageTextViewMessageHighlightedEventArgs e,
                                          ChatView chatView)
        {
#if MSG_DEBUG
            Trace.Call(sender, e, chatView);
#endif
            ShowNotification(chatView, e.Message);
        }

        public void ShowNotification(ChatView chatView, MessageModel msg)
        {
            var notification = new Notification("Smuxi", "MSG_HIGHLIGHT", "",
                                                chatView.Name, msg.ToString());
            Growl.Notify(notification);
        }
    }
}
