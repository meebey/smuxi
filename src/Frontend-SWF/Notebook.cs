/*
 * $Id: Notebook.cs 212 2007-08-23 21:36:44Z meebey $
 * $URL: svn+ssh://SmuxiSVN/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/Notebook.cs $
 * $Rev: 212 $
 * $Author: meebey $
 * $Date: 2007-08-23 16:36:44 -0500 (Thu, 23 Aug 2007) $
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
using Smuxi.Common;
using Smuxi.Engine;
using System.Windows.Forms;

namespace Smuxi.Frontend.Swf
{
    public class Notebook : TabControl
    {
        //private Gtk.Menu     _QueryTabMenu;
        
        public ChatView CurrentChatView {
            get {
                return (ChatView)SelectedTab;
            }
        }
        
        public Notebook() : base ()
        {
            Trace.Call();
            
            Selected += _OnSwitchPage;
        }
        
        public void ApplyConfig(UserConfig userConfig)
        {
            switch ((string) userConfig["Interface/Notebook/TabPosition"]) {
                case "top":
                    Alignment = TabAlignment.Top;
                    break;
                case "bottom":
                    Alignment = TabAlignment.Bottom;
                    break;
                case "left":
                    Alignment = TabAlignment.Left;
                    break;
                case "right":
                    Alignment = TabAlignment.Right;
                    break;
                case "none":
                    //ShowTabs = false;
                    break;
            }
        }
        
        // BUG: something fishy here, I don't believe the collection contains key
        // as string and int, see method below.
        public ChatView GetChat(ChatModel chat)
        {
            return (ChatView) Controls[chat.Name];
        }
        
        public ChatView GetChat(int pageNumber)
        {
            return (ChatView) Controls[pageNumber];
        }
        
        public void RemoveAllPages()
        {
            Controls.Clear();
        }
        
        // events
        private void _OnSwitchPage(object sender, TabControlEventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                // synchronize FrontManager.CurrenPage
                ChatView chatView = e.TabPage as ChatView;
                if (chatView != null) {
                    ChatModel chatModel = chatView.ChatModel;
                    IProtocolManager nmanager = chatModel.ProtocolManager;
                    Frontend.FrontendManager.CurrentChat = chatModel;
                    if (nmanager != null) {
                        Frontend.FrontendManager.CurrentProtocolManager = nmanager;
                    }
                    // even when we have no network manager, we still want to update the state
                    Frontend.FrontendManager.UpdateNetworkStatus();

                    // lets remove any markup / highlight
                    string color = (string) Frontend.UserConfig["Interface/Notebook/Tab/NoActivityColor"];
                    // TODO: apply color to tab
                    chatView.HasHighlight = false;
                    
                    // sync title
                    if (Frontend.MainWindow != null) {
                        string network = nmanager != null ? nmanager.ToString() + " / " : "";
                        Frontend.MainWindow.Text = network + chatView.Text +
                                                    " - Smuxi";
                    }
                }
            } catch (Exception ex) {
                Frontend.ShowException(null, ex);
            }
        }
    }
}
