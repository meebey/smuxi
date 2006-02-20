/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
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
using Meebey.Smuxi.Engine;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class QueryPage : Page
    {
        private Gtk.Menu     _TabMenu;

        public Gtk.Menu TabMenu
        {
            get {
                return _TabMenu;
            }
        }
        
        public QueryPage(Engine.Page epage) : base(epage)
        {
            Label = new Gtk.Label(epage.Name);
            _LabelEventBox.Add(_Label);
            _Label.Show();
            
            Add(_OutputScrolledWindow);
            
            // popup menu
            Gtk.AccelGroup agrp = new Gtk.AccelGroup();
            Frontend.MainWindow.AddAccelGroup(agrp);
            _TabMenu = new Gtk.Menu();
            Gtk.ImageMenuItem image_item = new Gtk.ImageMenuItem(Gtk.Stock.Close, agrp);
            image_item.Activated += new EventHandler(_OnTabMenuCloseActivated);  
            _TabMenu.Append(image_item);
            
            _LabelEventBox.ButtonPressEvent += new Gtk.ButtonPressEventHandler(_OnTabButtonPress);
        }
        
        private void _OnTabButtonPress(object obj, Gtk.ButtonPressEventArgs args)
        {
#if LOG4NET
            Logger.UI.Debug("_OnTabButtonPress triggered");
#endif

            if (args.Event.Button == 3) {
#if GTK_1
                _TabMenu.Popup(null, null, null, IntPtr.Zero, args.Event.Button, args.Event.Time);
#elif GTK_2
                _TabMenu.Popup(null, null, null, args.Event.Button, args.Event.Time);
#endif
                _TabMenu.ShowAll();
            }
        }
        
        private void _OnTabMenuCloseActivated(object sender, EventArgs e)
        {
            Frontend.Session.RemovePage(EnginePage);
        }
    }
}
