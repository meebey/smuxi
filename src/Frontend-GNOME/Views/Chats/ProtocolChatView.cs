/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006, 2009-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Threading;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Protocol)]
    public class ProtocolChatView : ChatView
    {
        public static Gdk.Pixbuf IconPixbuf { get; private set; }
        
        protected override Gtk.Image DefaultTabImage {
            get {
                return new Gtk.Image(IconPixbuf);
            }
        }

        static ProtocolChatView()
        {
            IconPixbuf = Frontend.LoadIcon(
                "smuxi-protocol-chat", 16, "protocol-chat_256x256.png"
            );
        }

        public ProtocolChatView(ChatModel chat) : base(chat)
        {
            Trace.Call(chat);
            
            Add(OutputScrolledWindow);
            
            ShowAll();
        }
        
        public override void Close()
        {
            Trace.Call();
            
            // show warning if there are open chats (besides protocol chat)
            // FIXME: REMOTING CALL 1 + 2 + 3
            if (ChatModel.ProtocolManager.Chats.Count > 1) {
                Gtk.MessageDialog md = new Gtk.MessageDialog(
                    Frontend.MainWindow,
                    Gtk.DialogFlags.Modal,
                    Gtk.MessageType.Warning,
                    Gtk.ButtonsType.YesNo,
                    _("Closing the protocol chat will also close all open chats connected to it!\n"+
                      "Are you sure you want to do this?"));
                int result = md.Run();
                md.Destroy();
                if ((Gtk.ResponseType) result != Gtk.ResponseType.Yes) {
                    return;
                }
            }

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    // no need to call base.Close() as CommandNetwork() will
                    // deal with it
                    Frontend.Session.CommandNetwork(
                        new CommandModel(
                            Frontend.FrontendManager,
                            ChatModel,
                            "close"
                        )
                    );
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            });
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
