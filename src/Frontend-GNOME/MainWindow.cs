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
using Mono.Unix;

namespace Meebey.Smuxi.FrontendGnome
{
#if UI_GNOME
    public class MainWindow : Gnome.App
#elif UI_GTK
	public class MainWindow : Gtk.Window
#endif
	{
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
#if UI_GNOME
        private Gnome.AppBar     _NetworkStatusbar;
        private Gnome.AppBar     _Statusbar;
#elif UI_GTK
        private Gtk.Statusbar    _NetworkStatusbar;
        private Gtk.Statusbar    _Statusbar;
#endif
        private Gtk.ProgressBar  _ProgressBar;
        private Entry            _Entry;
        private Notebook         _Notebook;
        private bool             _CaretMode;
        
        public bool CaretMode {
            get {
                return _CaretMode;
            }
        }
        
        public Notebook Notebook {
            get {
                return _Notebook;
            }
        }

#if UI_GNOME
        public new Gnome.AppBar NetworkStatusbar {
#elif UI_GTK
        public new Gtk.Statusbar NetworkStatusbar {
#endif
            get {
                return _NetworkStatusbar;
            }
        } 

#if UI_GNOME
        public new Gnome.AppBar Statusbar {
#elif UI_GTK
        public new Gtk.Statusbar Statusbar {
#endif
            get {
                return _Statusbar;
            }
        } 

        public Gtk.ProgressBar ProgressBar {
            get {
                return _ProgressBar;
            }
        }
        
        public Entry Entry {
            get {
                return _Entry;
            }
        }
     
#if UI_GNOME
		public MainWindow() : base("smuxi", "smuxi - Smart MUtipleXed Irc")
#elif UI_GTK
		public MainWindow() : base("smuxi - Smart MUtipleXed Irc")
#endif
		{
            SetDefaultSize(800, 600);
            Destroyed += new EventHandler(_OnDestroyed);
            
            Gtk.AccelGroup agrp = new Gtk.AccelGroup();
            Gtk.AccelKey   akey;
            AddAccelGroup(agrp);
            
            // Menu
            Gtk.MenuBar mb = new Gtk.MenuBar();
            Gtk.Menu menu;
            Gtk.MenuItem item;
            Gtk.ImageMenuItem image_item;
            
            // Menu - File
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_File"));
            item.Submenu = menu;
            mb.Append(item);
            
            item = new Gtk.ImageMenuItem(Gtk.Stock.Preferences, agrp);
            item.Activated += new EventHandler(_OnPreferencesButtonClicked);
            menu.Append(item);
            
            menu.Append(new Gtk.SeparatorMenuItem());
            
            item = new Gtk.ImageMenuItem(Gtk.Stock.Quit, agrp);
            item.Activated += new EventHandler(_OnQuitButtonClicked);
            menu.Append(item);
            
            // Menu - View
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_View"));
            item.Submenu = menu;
            mb.Append(item);
            
            item = new Gtk.CheckMenuItem(_("_Caret Mode"));
            item.Activated += new EventHandler(_OnCaretModeButtonClicked);
            akey = new Gtk.AccelKey();
            akey.AccelFlags = Gtk.AccelFlags.Visible;
            akey.Key = Gdk.Key.F7;
            item.AddAccelerator("activate", agrp, akey);
            menu.Append(item);
            
            // Menu - Engine
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_Engine"));
            item.Submenu = menu;
            mb.Append(item);

            item = new Gtk.MenuItem(_("_Use Local Engine"));
            item.Activated += new EventHandler(_OnUseLocalEngineButtonClicked);
            menu.Append(item);
            
            image_item = new Gtk.ImageMenuItem(_("_Add Remote Engine"));
            Gdk.Pixbuf pbuf = image_item.RenderIcon(Gtk.Stock.Add, Gtk.IconSize.Menu, null); 
            image_item.Image = new Gtk.Image(pbuf);
            image_item.Activated += new EventHandler(_OnAddRemoteEngineButtonClicked);
            menu.Append(image_item);
            
            item = new Gtk.MenuItem(_("_Switch Remote Engine"));
            item.Activated += new EventHandler(_OnSwitchRemoteEngineButtonClicked);
            menu.Append(item);
            
            // Menu - Help
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_Help"));
            item.Submenu = menu;
            mb.Append(item);
            
#if UI_GNOME
            image_item = new Gtk.ImageMenuItem(Gnome.Stock.About, agrp);
#elif UI_GTK
            image_item = new Gtk.ImageMenuItem(_("_About"), agrp);
#endif
            image_item.Activated += new EventHandler(_OnAboutButtonClicked);
            menu.Append(image_item);

            _Notebook = new Notebook();
            
            _Entry = new Entry(_Notebook);
            
            _ProgressBar = new Gtk.ProgressBar();
            
#if UI_GNOME
            Menus = mb;
            mb.ShowAll();
            Gtk.VBox vbox = new Gtk.VBox();
            vbox.PackStart(_Notebook, true, true, 0);
            vbox.PackStart(_Entry, false, false, 0);
            Contents = vbox;
            
            _NetworkStatusbar = new Gnome.AppBar(false, true, Gnome.PreferencesType.Never);
            _NetworkStatusbar.WidthRequest = 300;
            
            _Statusbar = new Gnome.AppBar(false, true, Gnome.PreferencesType.Never);
            
            Gtk.HBox sb_hbox = new Gtk.HBox();
            sb_hbox.PackStart(_NetworkStatusbar, false, true, 0);
            sb_hbox.PackStart(_Statusbar, true, true, 0);
            sb_hbox.PackStart(_ProgressBar, false, false, 0);
            base.Statusbar = sb_hbox;
#elif UI_GTK
            Gtk.VBox vbox = new Gtk.VBox();
            vbox.PackStart(mb, false, false, 0);
            vbox.PackStart(_Notebook, true, true, 0);
            vbox.PackStart(_Entry, false, false, 0);

            _NetworkStatusbar = new Gtk.Statusbar();
            _NetworkStatusbar.WidthRequest = 300;
            _NetworkStatusbar.HasResizeGrip = false;
            
            _Statusbar = new Gtk.Statusbar();
            
            Gtk.HBox sb_hbox = new Gtk.HBox();
            sb_hbox.PackStart(_NetworkStatusbar, false, true, 0);
            sb_hbox.PackStart(_Statusbar, true, true, 0);
            sb_hbox.PackStart(_ProgressBar, false, false, 0);
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
            AboutDialog ad = new AboutDialog();
            ad.ShowAll();
        }
        
        private void _OnPreferencesButtonClicked(object obj, EventArgs args)
        {
            try {
                new PreferencesDialog();
            } catch (Exception e) {
#if LOG4NET
                _Logger.Error(e);
#endif
                Frontend.ShowException(e);
            }
        }
        
        private void _OnUseLocalEngineButtonClicked(object obj, EventArgs args)
        {
            Gtk.MessageDialog md = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal,
                Gtk.MessageType.Warning, Gtk.ButtonsType.YesNo,
                _("Switching to local engine will disconnect you from the current engine!\n"+
                  "Are you sure you want to do this?"));
            int result = md.Run();
            md.Destroy();
            if ((Gtk.ResponseType)result == Gtk.ResponseType.Yes) {
                Frontend.DisconnectEngineFromGUI();
                Frontend.InitLocalEngine();
                Frontend.ConnectEngineToGUI();
            }
        }
        
        private void _OnAddRemoteEngineButtonClicked(object obj, EventArgs args)
        {
            new NewEngineDruid();
        }
        
        private void _OnSwitchRemoteEngineButtonClicked(object obj, EventArgs args)
        {
            Gtk.MessageDialog md = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal,
                Gtk.MessageType.Warning, Gtk.ButtonsType.YesNo,
                _("Switching the remote engine will disconnect you from the current engine!\n"+
                  "Are you sure you want to do this?"));
            int result = md.Run();
            md.Destroy();
            if ((Gtk.ResponseType)result == Gtk.ResponseType.Yes) {
                Frontend.DisconnectEngineFromGUI();
                EngineManagerDialog emd = new EngineManagerDialog();
                emd.Run();
            }
        }

        private void _OnCaretModeButtonClicked(object obj, EventArgs args)
        {
            _CaretMode = !_CaretMode;
            
            for (int i = 0; i < _Notebook.NPages; i++) {
                Page page = _Notebook.GetPage(i);
                page.OutputTextView.CursorVisible = _CaretMode;
            }
            
            if (_CaretMode) {
                _Notebook.CurrentFrontendPage.OutputTextView.HasFocus = true;
            } else {
                _Entry.HasFocus = true;
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
