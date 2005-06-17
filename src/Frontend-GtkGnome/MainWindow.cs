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

namespace Meebey.Smuxi.FrontendGtkGnome
{
#if UI_GNOME
    public class MainWindow : Gnome.App
#elif UI_GTK
	public class MainWindow : Gtk.Window
#endif
	{
#if UI_GNOME
        private Gnome.AppBar     _NetworkStatusbar;
        private Gnome.AppBar     _Statusbar;
#elif UI_GTK
        private Gtk.Statusbar    _NetworkStatusbar;
        private Gtk.Statusbar    _Statusbar;
#endif
        private Entry            _Entry;
        private Notebook         _Notebook;
        
        public Notebook Notebook
        {
            get {
#if LOG4NET
                Logger.UI.Debug("MainWindow.Notebook called");
#endif
                return _Notebook;
            }
        }

#if UI_GNOME
        public new Gnome.AppBar NetworkStatusbar
#elif UI_GTK
        public new Gtk.Statusbar NetworkStatusbar
#endif
        {
            get {
#if LOG4NET
                Logger.UI.Debug("MainWindow.NetworkStatusbar called");
#endif
                return _NetworkStatusbar;
            }
        } 

#if UI_GNOME
        public new Gnome.AppBar Statusbar
#elif UI_GTK
        public new Gtk.Statusbar Statusbar
#endif
        {
            get {
#if LOG4NET
                Logger.UI.Debug("MainWindow.Statusbar called");
#endif
                return _Statusbar;
            }
        } 

        public Entry Entry
        {
            get {
#if LOG4NET
                Logger.UI.Debug("MainWindow.Entry called");
#endif
                return _Entry;
            }
        }
     
#if UI_GNOME
		public MainWindow() : base ("smuxi", "smuxi - Smart MUtipleXed Irc")
#elif UI_GTK
		public MainWindow() : base ("smuxi - Smart MUtipleXed Irc")
#endif
		{
            SetDefaultSize(800, 600);
            Destroyed += new EventHandler(_OnDestroyed);
            
            Gtk.AccelGroup agrp = new Gtk.AccelGroup();
            AddAccelGroup(agrp);
            
            // Menu
            Gtk.MenuBar mb = new Gtk.MenuBar();
            Gtk.Menu menu;
            Gtk.MenuItem item;
            Gtk.ImageMenuItem image_item;
            
            // Menu - File
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem("_File");
            item.Submenu = menu;
            mb.Append(item);
            
            item = new Gtk.ImageMenuItem(Gtk.Stock.Preferences, agrp);
            item.Activated += new EventHandler(_OnPreferencesButtonClicked);
            menu.Append(item);
            
            menu.Append(new Gtk.SeparatorMenuItem());
            
            item = new Gtk.ImageMenuItem(Gtk.Stock.Quit, agrp);
            item.Activated += new EventHandler(_OnQuitButtonClicked);
            menu.Append(item);
            
            // Menu - Help
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem("_Help");
            item.Submenu = menu;
            mb.Append(item);
            
#if UI_GNOME
            image_item = new Gtk.ImageMenuItem(Gnome.Stock.About, agrp);
#elif UI_GTK
            image_item = new Gtk.ImageMenuItem("_About", agrp);
#endif
            image_item.Activated += new EventHandler(_OnAboutButtonClicked);
            menu.Append(image_item);

            _Notebook = new Notebook();
            _Entry = new Entry();
            
#if UI_GNOME
            Menus = mb;
            mb.ShowAll();
            Gtk.VBox vbox = new Gtk.VBox(false, 0);
            vbox.PackStart(_Notebook, true, true, 0);
            vbox.PackStart(_Entry, false, false, 0);
            Contents = vbox;
            
            _NetworkStatusbar = new Gnome.AppBar(false, true, Gnome.PreferencesType.Never);
            _NetworkStatusbar.WidthRequest = 200;
            _Statusbar = new Gnome.AppBar(false, true, Gnome.PreferencesType.Never);
            Gtk.HBox sb_hbox = new Gtk.HBox(false, 0);
            sb_hbox.PackStart(_NetworkStatusbar, false, true, 0);
            sb_hbox.PackStart(_Statusbar, true, true, 0);
            base.Statusbar = sb_hbox;
#elif UI_GTK
            Gtk.VBox vbox = new Gtk.VBox(false, 0);
            vbox.PackStart(mb, false, false, 0);
            vbox.PackStart(_Notebook, true, true, 0);
            vbox.PackStart(_Entry, false, false, 0);

            _NetworkStatusbar = new Gtk.Statusbar();
            _NetworkStatusbar.WidthRequest = 200;
            _NetworkStatusbar.HasResizeGrip = false;
            _Statusbar = new Gtk.Statusbar();
            Gtk.HBox sb_hbox = new Gtk.HBox(false, 0);
            sb_hbox.PackStart(_NetworkStatusbar, false, true, 0);
            sb_hbox.PackStart(_Statusbar, true, true, 0);
            vbox.PackStart(sb_hbox, false, false, 0);
            Add(vbox);
#endif
		}

        private void _OnQuitButtonClicked(object obj, EventArgs args)
        {
            Frontend.Quit();
        }

        private void _OnDestroyed(object obj, EventArgs args)
        {
            Frontend.Quit();
        }
    
        private void _OnAboutButtonClicked(object obj, EventArgs args)
        {
            AboutDialog ad =new AboutDialog();
            ad.ShowAll();
        }
        
        private void _OnPreferencesButtonClicked(object obj, EventArgs args)
        {
            PreferencesDialog pd = new PreferencesDialog();
            pd.ShowAll();
        }
	}
}
