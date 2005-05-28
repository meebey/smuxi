/**
 * $Id: AssemblyInfo.cs 34 2004-09-05 14:46:59Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/Gnosmirc/trunk/src/AssemblyInfo.cs $
 * $Rev: 34 $
 * $Author: meebey $
 * $Date: 2004-09-05 16:46:59 +0200 (Sun, 05 Sep 2004) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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
using Meebey.Smuxi;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class Notebook : Gtk.Notebook
    {
        private Gtk.Menu     _ChannelTabMenu;
        private Gtk.Menu     _QueryTabMenu;
        private Gtk.Menu     _ChannelUserMenu;
    
        public Notebook() : base ()
        {
            SwitchPage += new Gtk.SwitchPageHandler(_OnSwitchPage);
            ApplyUserConfig();
        }
        
        public void ApplyUserConfig()
        {
            switch ((string)Frontend.UserConfig["Interface/Notebook/TabPosition"]) {
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
        }
        
        public Page GetPage(Engine.Page epage)
        {
            for (int i=0; i < NPages; i++) {
                Page page = (Page)GetNthPage(i);
                if (page.EnginePage == epage) {
                    return page;
                }
            }
            
            return null;
        }
        
        // events
        private void _OnTabButtonPress(object obj, Gtk.ButtonPressEventArgs args)
        {
#if LOG4NET
            Logger.UI.Debug("_OnTabButtonPress triggered");
#endif

            if (args.Event.Button == 3) {
                _ChannelTabMenu.Popup(null, null, null, IntPtr.Zero, args.Event.Button, args.Event.Time);
                _ChannelTabMenu.ShowAll();
            }
        }

        private void _OnChannelUserListButtonPress(object obj, Gtk.ButtonPressEventArgs args)
        {
#if LOG4NET
            Logger.UI.Debug("_OnChannelUserListButtonPress triggered");
#endif

            if (args.Event.Button == 3) {
                _ChannelUserMenu.Popup(null, null, null, IntPtr.Zero, args.Event.Button, args.Event.Time);
                _ChannelUserMenu.ShowAll();
            }
        }

        private void _OnSwitchPage(object obj, Gtk.SwitchPageArgs args)
        {
#if LOG4NET
            Logger.UI.Debug("_OnPageSwitched triggered");
#endif

            // synchronize FrontManager.CurrenPage
            Page npage = (Page)GetNthPage((int)args.PageNum);
            if (npage != null) {
                Frontend.FrontendManager.CurrentPage = npage.EnginePage;
                if (npage.EnginePage.NetworkManager != null) {
                    Frontend.FrontendManager.CurrentNetworkManager = npage.EnginePage.NetworkManager;
                }
                // even when we have no network manager, we still want to update the state
                Frontend.FrontendManager.UpdateNetworkStatus();

                // lets remove any markup
                Gtk.Label label = (Gtk.Label)GetTabLabel(npage);
                label.Markup = label.Text;
            }
        }
    }
}
