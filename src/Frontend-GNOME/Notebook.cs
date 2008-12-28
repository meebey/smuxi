/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
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

using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class Notebook : Gtk.Notebook
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        //private Gtk.Menu     _QueryTabMenu;
        
        public ChatView CurrentChatView {
            get {
                return (ChatView) base.CurrentPageWidget;
            }
        }
        
        public Notebook() : base ()
        {
            Trace.Call();
            
            Scrollable = true;
            SwitchPage += new Gtk.SwitchPageHandler(_OnSwitchPage);
        }
        
        public void ApplyConfig(UserConfig userConfig)
        {
            switch ((string) userConfig["Interface/Notebook/TabPosition"]) {
                case "top":
                    TabPos = Gtk.PositionType.Top;
                    ShowTabs = true;
                    break;
                case "bottom":
                    TabPos = Gtk.PositionType.Bottom;
                    ShowTabs = true;
                    break;
                case "left":
                    TabPos = Gtk.PositionType.Left;
                    ShowTabs = true;
                    break;
                case "right":
                    TabPos = Gtk.PositionType.Right;
                    ShowTabs = true;
                    break;
                case "none":
                    ShowTabs = false;
                    break;
            }
            
            // TODO: Homogeneous = true;
        }
        
        public ChatView GetChat(ChatModel chat)
        {
            for (int i=0; i < NPages; i++) {
                ChatView chatView = (ChatView) GetNthPage(i);
                if (chatView.ChatModel == chat) {
                    return chatView;
                }
            }
            
            return null;
        }
        
        public ChatView GetChat(int pageNumber)
        {
            return (ChatView) base.GetNthPage(pageNumber);
        }
        
        public void RemoveAllPages()
        {
            Trace.Call();
            
            int npages = NPages;
            CurrentPage = 0;
            for (int i = 0; i < npages; i++) {
                // *doh* this would be too easy, ugly Gtk.Notebook doesn't
                // like it though, index based vs array based?
                //RemovePage(i);

                NextPage();
                RemovePage(CurrentPage);
            }
        }
        
        // events
        private void _OnSwitchPage(object sender, Gtk.SwitchPageArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                // synchronize FrontManager.CurrenPage
                ChatView chatView = GetChat((int)e.PageNum);
                if (chatView != null) {
                    ChatModel chatModel = chatView.ChatModel;
                    IProtocolManager nmanager = chatModel.ProtocolManager;
                    Frontend.FrontendManager.CurrentChat = chatModel;
                    if (nmanager != null) {
                        Frontend.FrontendManager.CurrentProtocolManager = nmanager;
                    }
                    // even when we have no network manager, we still want to update the state
                    Frontend.FrontendManager.UpdateNetworkStatus();

                    // update last seen highlight 
                    if (chatView.HasHighlight) {
                        chatModel.LastSeenHighlight = DateTime.UtcNow;
                    }
                    // clear activity and highlight
                    chatView.HasHighlight = false;
                    chatView.HasActivity = false;
                    
                    // sync title
                    if (Frontend.MainWindow != null) {
                        string network = nmanager != null ? nmanager.ToString() + " / " : "";
                        Frontend.MainWindow.Title = network + chatView.Name +
                                                    " - smuxi - Smart MUtipleXed Irc";
                    }
                }
            } catch (Exception ex) {
                Frontend.ShowException(null, ex);
            }
        }
    }
}
