/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2008 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
using System.Reflection;
using Mono.Unix;
#if UI_GNOME
using GNOME = Gnome;
#endif
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
#if UI_GNOME
    public class MainWindow : GNOME.App
#elif UI_GTK
    public class MainWindow : Gtk.Window
#endif
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
#if UI_GNOME
        private GNOME.AppBar     _NetworkStatusbar;
        private GNOME.AppBar     _Statusbar;
#elif UI_GTK
        private Gtk.Statusbar    _NetworkStatusbar;
        private Gtk.Statusbar    _Statusbar;
#endif
        private Gtk.ProgressBar  _ProgressBar;
        private Entry            _Entry;
        private Notebook         _Notebook;
        private bool             _CaretMode;
        private ChatViewManager  _ChatViewManager;
        private IFrontendUI      _UI;
        private EngineManager    _EngineManager;
        private Gtk.MenuItem     _CloseChatMenuItem;
        
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
        
        public IFrontendUI UI {
            get {
                return _UI;
            }
        }
        
        public EngineManager EngineManager {
            get {
                return _EngineManager;
            }
        }
        
        public ChatViewManager ChatViewManager {
            get {
                return _ChatViewManager;
            }
        }
        
#if UI_GNOME
        public new GNOME.AppBar NetworkStatusbar {
#elif UI_GTK
        public new Gtk.Statusbar NetworkStatusbar {
#endif
            get {
                return _NetworkStatusbar;
            }
        } 

#if UI_GNOME
        public new GNOME.AppBar Statusbar {
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
            // restore window size / position
            int width, heigth;
            if (Frontend.FrontendConfig[Frontend.UIName + "/Interface/Width"] != null) {
                width  = (int) Frontend.FrontendConfig[Frontend.UIName + "/Interface/Width"];
            } else {
                width = 800;
            }
            if (Frontend.FrontendConfig[Frontend.UIName + "/Interface/Heigth"] != null) {
                heigth = (int) Frontend.FrontendConfig[Frontend.UIName + "/Interface/Heigth"];
            } else {
                heigth = 600;
            }
            SetDefaultSize(width, heigth);
            
            int x, y;
            if (Frontend.FrontendConfig[Frontend.UIName + "/Interface/XPosition"] != null) {
                x = (int) Frontend.FrontendConfig[Frontend.UIName + "/Interface/XPosition"];
            } else {
                x = 0;
            }
            if (Frontend.FrontendConfig[Frontend.UIName + "/Interface/YPosition"] != null) {
                y = (int) Frontend.FrontendConfig[Frontend.UIName + "/Interface/YPosition"];
            } else {
                y = 0;
            }
            if (x == 0 && y == 0) {
                SetPosition(Gtk.WindowPosition.Center);
            } else {
                Move(x, y);
            }
            
            Destroyed += new EventHandler(_OnDestroyed);
            FocusInEvent += new Gtk.FocusInEventHandler(_OnFocusInEvent);
            
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
            
            // Menu - Server
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_Server"));
            item.Submenu = menu;
            mb.Append(item);
            
            image_item = new Gtk.ImageMenuItem(_("_Quick Connect"));
            image_item.Image = new Gtk.Image(Gtk.Stock.Connect, Gtk.IconSize.Menu);
            image_item.Activated += OnServerQuickConnectButtonClicked;
            menu.Append(image_item);
            
            menu.Append(new Gtk.SeparatorMenuItem());
                    
            image_item = new Gtk.ImageMenuItem(Gtk.Stock.Add, agrp);
            image_item.Activated += OnServerAddButtonClicked;
            menu.Append(image_item);
            
            image_item = new Gtk.ImageMenuItem(_("_Manage"));
            image_item.Image = new Gtk.Image(Gtk.Stock.Edit, Gtk.IconSize.Menu);
            image_item.Activated += OnServerManageServersButtonClicked;
            menu.Append(image_item);
            
            // Menu - Chat
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_Chat"));
            item.Submenu = menu;
            mb.Append(item);
            
            image_item = new Gtk.ImageMenuItem(_("_Find Group Chat"));
            image_item.Image = new Gtk.Image(Gtk.Stock.Find, Gtk.IconSize.Menu);
            image_item.Activated += OnChatFindGroupChatButtonClicked;
            menu.Append(image_item);
            
            menu.Append(new Gtk.SeparatorMenuItem());
                    
            _CloseChatMenuItem = new Gtk.ImageMenuItem(Gtk.Stock.Close, agrp);
            _CloseChatMenuItem.Activated += OnCloseChatMenuItemActivated;
            menu.Append(_CloseChatMenuItem);

            // Menu - Engine
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_Engine"));
            item.Submenu = menu;
            mb.Append(item);

            item = new Gtk.MenuItem(_("_Use Local Engine"));
            item.Activated += new EventHandler(_OnUseLocalEngineButtonClicked);
            menu.Append(item);
            
            menu.Append(new Gtk.SeparatorMenuItem());
                    
            image_item = new Gtk.ImageMenuItem(_("_Add Remote Engine"));
            image_item.Image = new Gtk.Image(Gtk.Stock.Add, Gtk.IconSize.Menu);
            image_item.Activated += new EventHandler(_OnAddRemoteEngineButtonClicked);
            menu.Append(image_item);
            
            image_item = new Gtk.ImageMenuItem(_("_Switch Remote Engine"));
            image_item.Image = new Gtk.Image(Gtk.Stock.Refresh, Gtk.IconSize.Menu);
            image_item.Activated += new EventHandler(_OnSwitchRemoteEngineButtonClicked);
            menu.Append(image_item);
            
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
            
            // Menu - Help
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_Help"));
            item.Submenu = menu;
            mb.Append(item);
            
            image_item = new Gtk.ImageMenuItem(Gtk.Stock.About, agrp);
            image_item.Activated += new EventHandler(_OnAboutButtonClicked);
            menu.Append(image_item);
            
            // TODO: network treeview
            _Notebook = new Notebook();
            _Notebook.SwitchPage += OnNotebookSwitchPage;
            
            _ChatViewManager = new ChatViewManager(_Notebook, null);
            Assembly asm = Assembly.GetExecutingAssembly();
            _ChatViewManager.Load(asm);
            _ChatViewManager.LoadAll(System.IO.Path.GetDirectoryName(asm.Location),
                                     "smuxi-frontend-gnome-*.dll");
            
            _UI = new GnomeUI(_ChatViewManager);
            
            // HACK: Frontend.FrontendConfig out of scope
            _EngineManager = new EngineManager(Frontend.FrontendConfig, _UI);

            _Entry = new Entry(_Notebook);
            
            _ProgressBar = new Gtk.ProgressBar();
            
#if UI_GNOME
            Menus = mb;
            mb.ShowAll();
            Gtk.VBox vbox = new Gtk.VBox();
            vbox.PackStart(_Notebook, true, true, 0);
            vbox.PackStart(_Entry, false, false, 0);
            Contents = vbox;
            
            _NetworkStatusbar = new GNOME.AppBar(false, true, GNOME.PreferencesType.Never);
            _NetworkStatusbar.WidthRequest = 300;
            
            _Statusbar = new GNOME.AppBar(false, true, GNOME.PreferencesType.Never);
            
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

        public void ApplyConfig(UserConfig userConfig)
        {
            Trace.Call(userConfig);
            
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }
                    
            _Entry.ApplyConfig(userConfig);
            _Notebook.ApplyConfig(userConfig);
            _ChatViewManager.ApplyConfig(userConfig);
        }
        
        private void _OnQuitButtonClicked(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
                Frontend.Quit();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        private void _OnDestroyed(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
                Frontend.Quit();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
    
        private void _OnFocusInEvent(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
                UrgencyHint = false;
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnServerQuickConnectButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                QuickConnectDialog dialog = new QuickConnectDialog();
                dialog.Load();
                int res = dialog.Run();
                ServerModel server = dialog.Server;
                dialog.Destroy();
                if (res != (int) Gtk.ResponseType.Ok) {
                    return;
                }
                if (server == null) {
#if LOG4NET
                    f_Logger.Error("OnServerQuickConnectButtonClicked(): server is null!");
                    return;
#endif
                }
                
                CommandModel cmd = new CommandModel(
                    Frontend.FrontendManager,
                    Frontend.Session.SessionChat,
                    "/",
                    String.Format(
                        "/connect {0} {1} {2} {3}",
                        server.Protocol,
                        server.Hostname,
                        server.Port,
                        server.Username,
                        server.Password
                    )
                );
                Frontend.Session.CommandConnect(cmd);
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnServerAddButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ServerListController controller = new ServerListController(Frontend.UserConfig);
                ServerView serverView = new ServerView(null,
                                                       Frontend.Session.GetSupportedProtocols(),
                                                       controller.GetNetworks());
                int res = serverView.Run();
                serverView.Destroy();
                if (res != (int) Gtk.ResponseType.Ok) {
                    return;
                }
                
                controller.AddServer(serverView.Server);
                controller.Save();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnServerManageServersButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                PreferencesDialog dialog = new PreferencesDialog();
                dialog.CurrentPage = PreferencesDialog.Page.Servers;
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnChatFindGroupChatButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                IProtocolManager manager = Frontend.FrontendManager.CurrentProtocolManager;
                FindGroupChatDialog dialog = new FindGroupChatDialog(
                    this, manager
                );
                int res = dialog.Run();
                GroupChatModel groupChat = dialog.GroupChat;
                dialog.Destroy();
                if (res != (int) Gtk.ResponseType.Ok) {
                    return;
                }
                
                manager.OpenChat(Frontend.FrontendManager, groupChat);
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnCloseChatMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                _Notebook.CurrentChatView.Close();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnNotebookSwitchPage(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                _CloseChatMenuItem.Sensitive = !(_Notebook.CurrentChatView is SessionChatView);
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        private void _OnAboutButtonClicked(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
                AboutDialog ad = new AboutDialog();
                ad.Run();
                ad.Destroy();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        private void _OnPreferencesButtonClicked(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
                new PreferencesDialog();
                /*
                SteticPreferencesDialog dialog = new SteticPreferencesDialog();
                dialog.Run();
                */
            } catch (Exception e) {
#if LOG4NET
                f_Logger.Error(e);
#endif
                Frontend.ShowException(this, e);
            }
        }
        
        private void _OnUseLocalEngineButtonClicked(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
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
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        private void _OnAddRemoteEngineButtonClicked(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
                new EngineDruid(Frontend.FrontendConfig);
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        private void _OnSwitchRemoteEngineButtonClicked(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
                Gtk.MessageDialog md = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal,
                    Gtk.MessageType.Warning, Gtk.ButtonsType.YesNo,
                    _("Switching the remote engine will disconnect you from the current engine!\n"+
                      "Are you sure you want to do this?"));
                int result = md.Run();
                md.Destroy();
                if ((Gtk.ResponseType)result == Gtk.ResponseType.Yes) {
                    Frontend.DisconnectEngineFromGUI();
                    Frontend.ShowEngineManagerDialog();
                }
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        private void _OnCaretModeButtonClicked(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
                _CaretMode = !_CaretMode;
                
                for (int i = 0; i < _Notebook.NPages; i++) {
                    ChatView chatView = _Notebook.GetChat(i);
                    chatView.OutputTextView.CursorVisible = _CaretMode;
                }
                
                if (_CaretMode) {
                    _Notebook.CurrentChatView.OutputTextView.HasFocus = true;
                } else {
                    _Entry.HasFocus = true;
                }
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
