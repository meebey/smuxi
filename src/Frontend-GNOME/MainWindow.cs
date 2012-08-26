/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Linq;
using System.Threading;
using System.Reflection;
using SysDiag = System.Diagnostics;
using Mono.Unix;
using IgeMacIntegration;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class MainWindow : Gtk.Window
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private bool             _IsFullscreen;

        Gtk.Statusbar NetworkStatusbar { get; set; }
        Gtk.Statusbar Statusbar { get; set; }
        public Gtk.ProgressBar ProgressBar { get; private set; }
        public Gtk.MenuBar MenuBar { get; private set; }
        Gtk.HBox MenuHBox { get; set; }
        Gtk.HBox StatusHBox { get; set; }
        JoinWidget JoinWidget { get; set; }

        public IFrontendUI UI { get; private set; }
        public Entry Entry { get; private set; }
        public Notebook Notebook { get; private set; }
        public ChatViewManager ChatViewManager { get; private set; }
        public EngineManager EngineManager { get; private set; }
#if GTK_SHARP_2_10
        StatusIconManager StatusIconManager { get; set; }
#endif
#if INDICATE_SHARP
        IndicateManager IndicateManager { get; set; }
#endif
#if NOTIFY_SHARP
        NotifyManager NotifyManager { get; set; }
#endif
#if IPC_DBUS
        NetworkManager NetworkManager { get; set; }
#endif

        Gtk.Menu AppMenu { get; set; }
        Gtk.ImageMenuItem PreferencesMenuItem { get; set; }
        Gtk.ImageMenuItem QuitMenuItem { get; set; }
        Gtk.ImageMenuItem OpenChatMenuItem { get; set; }
        Gtk.MenuItem CloseChatMenuItem { get; set; }
        Gtk.ImageMenuItem OpenLogChatMenuItem { get; set; }
        Gtk.ImageMenuItem FindGroupChatMenuItem { get; set; }

        Gtk.CheckMenuItem ShowQuickJoinMenuItem { get; set; }
        Gtk.CheckMenuItem ShowMenuBarMenuItem  { get; set; }
        Gtk.CheckMenuItem ShowStatusBarMenuItem  { get; set; }

        public NotificationAreaIconMode NotificationAreaIconMode { get; set; }
        public bool CaretMode { get; private set; }
        public bool IsMinimized { get; private set; }
        public bool IsMaximized { get; private set; }

        public bool ShowMenuBar {
            get {
                return MenuBar.Visible;
            }
            set {
                ShowMenuBarMenuItem.Active = value;
            }
        }

        public string NetworkStatus {
            set {
                if (value == null) {
                    value = String.Empty;
                }
                NetworkStatusbar.Pop(0);
                NetworkStatusbar.Push(0, value);
            }
        } 

        public string Status {
            set {
                if (value == null) {
                    value = String.Empty;
                }
                Statusbar.Pop(0);
                Statusbar.Push(0, value);
            }
        }

        public bool IsFullscreen {
            get {
                return _IsFullscreen;
            }
            set {
                _IsFullscreen = value;
                if (value) {
                    Fullscreen();
                } else {
                    Unfullscreen();
                }
            }
        }

        public EventHandler Minimized;
        public EventHandler Unminimized;

        public MainWindow() : base("Smuxi")
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
            if (width < -1 || heigth < -1) {
                width = -1;
                heigth = -1;
            }
            if (width == -1 && heigth == -1) {
                SetDefaultSize(800, 600);
                Maximize();
            } else if (width == 0 && heigth == 0) {
                // HACK: map 0/0 to default size as it crashes on Windows :/
                SetDefaultSize(800, 600);
            } else {
                SetDefaultSize(width, heigth);
            }
            
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
            if (x < 0 || y < 0) {
                x = 0;
                y = 0;
            }
            if (x == 0 && y == 0) {
                SetPosition(Gtk.WindowPosition.Center);
            } else {
                Move(x, y);
            }

            DeleteEvent += OnDeleteEvent;
            FocusInEvent += OnFocusInEvent;
            FocusOutEvent += OnFocusOutEvent;
            WindowStateEvent += OnWindowStateEvent;

            Gtk.AccelGroup agrp = new Gtk.AccelGroup();
            Gtk.AccelKey   akey;
            AddAccelGroup(agrp);
            
            // Menu
            MenuBar = new Gtk.MenuBar();
            Gtk.Menu menu;
            Gtk.MenuItem item;
            Gtk.ImageMenuItem image_item;
            
            // Menu - Smuxi
            AppMenu = new Gtk.Menu();
            item = new Gtk.MenuItem("_Smuxi");
            item.Submenu = AppMenu;
            if (!Frontend.IsMacOSX) {
                MenuBar.Append(item);
            }

            PreferencesMenuItem = new Gtk.ImageMenuItem(Gtk.Stock.Preferences, agrp);
            PreferencesMenuItem.Activated += new EventHandler(_OnPreferencesButtonClicked);
            PreferencesMenuItem.AccelCanActivate += AccelCanActivateSensitive;
            AppMenu.Append(PreferencesMenuItem);
            
            AppMenu.Append(new Gtk.SeparatorMenuItem());
            
            QuitMenuItem = new Gtk.ImageMenuItem(Gtk.Stock.Quit, agrp);
            QuitMenuItem.Activated += new EventHandler(_OnQuitButtonClicked);
            QuitMenuItem.AccelCanActivate += AccelCanActivateSensitive;
            AppMenu.Append(QuitMenuItem);
            
            // Menu - Server
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_Server"));
            item.Submenu = menu;
            MenuBar.Append(item);
            
            image_item = new Gtk.ImageMenuItem(_("_Connect"));
            image_item.Image = new Gtk.Image(Gtk.Stock.Connect, Gtk.IconSize.Menu);
            image_item.Activated += OnServerConnectButtonClicked;
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
            MenuBar.Append(item);
            
            OpenChatMenuItem = new Gtk.ImageMenuItem(_("Open / Join Chat"));
            OpenChatMenuItem.Image = new Gtk.Image(Gtk.Stock.Open, Gtk.IconSize.Menu);
            OpenChatMenuItem.Activated += OnOpenChatMenuItemActivated;
            akey = new Gtk.AccelKey();
            akey.AccelFlags = Gtk.AccelFlags.Visible;
            akey.AccelMods = Gdk.ModifierType.ControlMask;
            akey.Key = Gdk.Key.L;
            OpenChatMenuItem.AddAccelerator("activate", agrp, akey);
            OpenChatMenuItem.AccelCanActivate += AccelCanActivateSensitive;
            menu.Append(OpenChatMenuItem);
                    
            FindGroupChatMenuItem = new Gtk.ImageMenuItem(_("_Find Group Chat"));
            FindGroupChatMenuItem.Image = new Gtk.Image(Gtk.Stock.Find, Gtk.IconSize.Menu);
            FindGroupChatMenuItem.Activated += OnChatFindGroupChatButtonClicked;
            FindGroupChatMenuItem.Sensitive = false;
            menu.Append(FindGroupChatMenuItem);
            
            image_item = new Gtk.ImageMenuItem(_("C_lear All Activity"));
            image_item.Image = new Gtk.Image(Gtk.Stock.Clear, Gtk.IconSize.Menu);
            image_item.Activated += OnChatClearAllActivityButtonClicked;
            menu.Append(image_item);
            
            menu.Append(new Gtk.SeparatorMenuItem());
                    
            image_item = new Gtk.ImageMenuItem(_("_Next Chat"));
            image_item.Image = new Gtk.Image(Gtk.Stock.GoForward, Gtk.IconSize.Menu);
            image_item.Activated += OnNextChatMenuItemActivated;
            akey = new Gtk.AccelKey();
            akey.AccelFlags = Gtk.AccelFlags.Visible;
            akey.AccelMods = Gdk.ModifierType.ControlMask;
            akey.Key = Gdk.Key.Page_Down;
            image_item.AddAccelerator("activate", agrp, akey);
            image_item.AccelCanActivate += AccelCanActivateSensitive;
            menu.Append(image_item);
            
            image_item = new Gtk.ImageMenuItem(_("_Previous Chat"));
            image_item.Image = new Gtk.Image(Gtk.Stock.GoBack, Gtk.IconSize.Menu);
            image_item.Activated += OnPreviousChatMenuItemActivated;
            akey = new Gtk.AccelKey();
            akey.AccelFlags = Gtk.AccelFlags.Visible;
            akey.AccelMods = Gdk.ModifierType.ControlMask;
            akey.Key = Gdk.Key.Page_Up;
            image_item.AddAccelerator("activate", agrp, akey);
            image_item.AccelCanActivate += AccelCanActivateSensitive;
            menu.Append(image_item);
            
            menu.Append(new Gtk.SeparatorMenuItem());
                    
            /*
            // TODO: make a radio item for each chat hotkey
            Gtk.RadioMenuItem radio_item;
            radio_item = new Gtk.RadioMenuItem();
            radio_item = new Gtk.RadioMenuItem(radio_item);
            radio_item = new Gtk.RadioMenuItem(radio_item);
                    
            menu.Append(new Gtk.SeparatorMenuItem());
            */
            
            /*
            image_item = new Gtk.ImageMenuItem(Gtk.Stock.Find, agrp);
            image_item.Activated += OnFindChatMenuItemActivated;
            menu.Append(image_item);
            
            item = new Gtk.MenuItem(_("Find _Next"));
            item.Activated += OnFindNextChatMenuItemActivated;
            akey = new Gtk.AccelKey();
            akey.AccelFlags = Gtk.AccelFlags.Visible;
            akey.AccelMods = Gdk.ModifierType.ControlMask;
            akey.Key = Gdk.Key.G;
            item.AddAccelerator("activate", agrp, akey);
            menu.Append(item);
            
            item = new Gtk.MenuItem(_("Find _Previous"));
            item.Activated += OnFindPreviousChatMenuItemActivated;
            akey = new Gtk.AccelKey();
            akey.AccelFlags = Gtk.AccelFlags.Visible;
            akey.AccelMods = Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask;
            akey.Key = Gdk.Key.G;
            item.AddAccelerator("activate", agrp, akey);
            menu.Append(item);
            */

            // ROFL: the empty code statement below is needed to keep stupid
            // gettext away from using all the commented code from above as
            // translator comment
            ;
            OpenLogChatMenuItem = new Gtk.ImageMenuItem(_("Open Log"));
            OpenLogChatMenuItem.Image = new Gtk.Image(Gtk.Stock.Open,
                                                       Gtk.IconSize.Menu);
            OpenLogChatMenuItem.Activated += OnOpenLogChatMenuItemActivated;
            OpenLogChatMenuItem.Sensitive = false;
            OpenLogChatMenuItem.NoShowAll = true;
            menu.Append(OpenLogChatMenuItem);

            CloseChatMenuItem = new Gtk.ImageMenuItem(Gtk.Stock.Close, agrp);
            CloseChatMenuItem.Activated += OnCloseChatMenuItemActivated;
            CloseChatMenuItem.AccelCanActivate += AccelCanActivateSensitive;
            menu.Append(CloseChatMenuItem);

            // Menu - Engine
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_Engine"));
            item.Submenu = menu;
            MenuBar.Append(item);

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
            MenuBar.Append(item);
            
            item = new Gtk.CheckMenuItem(_("_Caret Mode"));
            item.Activated += new EventHandler(_OnCaretModeButtonClicked);
            akey = new Gtk.AccelKey();
            akey.AccelFlags = Gtk.AccelFlags.Visible;
            akey.Key = Gdk.Key.F7;
            item.AddAccelerator("activate", agrp, akey);
            item.AccelCanActivate += AccelCanActivateSensitive;
            menu.Append(item);
            
            item = new Gtk.CheckMenuItem(_("_Browse Mode"));
            item.Activated += delegate {
                try {
                    Notebook.IsBrowseModeEnabled = !Notebook.IsBrowseModeEnabled;
                } catch (Exception ex) {
                    Frontend.ShowException(this, ex);
                }
            };
            akey = new Gtk.AccelKey();
            akey.AccelFlags = Gtk.AccelFlags.Visible;
            akey.Key = Gdk.Key.F8;
            item.AddAccelerator("activate", agrp, akey);
            item.AccelCanActivate += AccelCanActivateSensitive;
            menu.Append(item);

            ShowMenuBarMenuItem = new Gtk.CheckMenuItem(_("Show _Menubar"));
            ShowMenuBarMenuItem.Active = (bool) Frontend.FrontendConfig["ShowMenuBar"];
            ShowMenuBarMenuItem.Activated += OnShowMenuBarMenuItemActivated;
            menu.Append(ShowMenuBarMenuItem);

            ShowStatusBarMenuItem = new Gtk.CheckMenuItem(_("Show _Status Bar"));
            ShowStatusBarMenuItem.Active = (bool) Frontend.FrontendConfig["ShowStatusBar"];
            ShowStatusBarMenuItem.Activated += OnShowStatusBarMenuItemActivated;
            menu.Append(ShowStatusBarMenuItem);

            JoinWidget = new JoinWidget();
            JoinWidget.NoShowAll = true;
            JoinWidget.Visible = (bool) Frontend.FrontendConfig["ShowQuickJoin"];
            JoinWidget.Activated += OnJoinWidgetActivated;

            ShowQuickJoinMenuItem = new Gtk.CheckMenuItem(_("Show _Quick Join"));
            ShowQuickJoinMenuItem.Active = JoinWidget.Visible;
            ShowQuickJoinMenuItem.Activated += OnShowQuickJoinMenuItemActivated;
            menu.Append(ShowQuickJoinMenuItem);

            item = new Gtk.ImageMenuItem(Gtk.Stock.Fullscreen, agrp);
            item.Activated += delegate {
                try {
                    IsFullscreen = !IsFullscreen;
                } catch (Exception ex) {
                    Frontend.ShowException(this, ex);
                }
            };
            akey = new Gtk.AccelKey();
            akey.AccelFlags = Gtk.AccelFlags.Visible;
            akey.Key = Gdk.Key.F11;
            item.AddAccelerator("activate", agrp, akey);
            item.AccelCanActivate += AccelCanActivateSensitive;
            menu.Append(item);

            // Menu - Help
            menu = new Gtk.Menu();
            item = new Gtk.MenuItem(_("_Help"));
            item.Submenu = menu;
            MenuBar.Append(item);
            
            image_item = new Gtk.ImageMenuItem(Gtk.Stock.About, agrp);
            image_item.Activated += new EventHandler(_OnAboutButtonClicked);
            menu.Append(image_item);

            MenuBar.ShowAll();
            MenuBar.NoShowAll = true;
            MenuBar.Visible = ShowMenuBarMenuItem.Active;

            // TODO: network treeview
            Notebook = new Notebook();
            Notebook.SwitchPage += OnNotebookSwitchPage;
            Notebook.FocusInEvent += OnNotebookFocusInEvent;

            ChatViewManager = new ChatViewManager(Notebook, null);
            Assembly asm = Assembly.GetExecutingAssembly();
            ChatViewManager.Load(asm);
            ChatViewManager.LoadAll(System.IO.Path.GetDirectoryName(asm.Location),
                                     "smuxi-frontend-gnome-*.dll");
            ChatViewManager.ChatAdded += OnChatViewManagerChatAdded;
            ChatViewManager.ChatSynced += OnChatViewManagerChatSynced;
            ChatViewManager.ChatRemoved += OnChatViewManagerChatRemoved;
            
#if GTK_SHARP_2_10
            StatusIconManager = new StatusIconManager(this, ChatViewManager);
#endif
#if INDICATE_SHARP
            IndicateManager = new IndicateManager(this, ChatViewManager);
#endif
#if NOTIFY_SHARP
            NotifyManager = new NotifyManager(this, ChatViewManager);
#endif
#if IPC_DBUS
            NetworkManager = new NetworkManager(ChatViewManager);
#endif

            UI = new GnomeUI(ChatViewManager);
            
            // HACK: Frontend.FrontendConfig out of scope
            EngineManager = new EngineManager(Frontend.FrontendConfig, UI);

            Entry = new Entry(ChatViewManager);
            var entryScrolledWindow = new Gtk.ScrolledWindow();
            entryScrolledWindow.ShadowType = Gtk.ShadowType.EtchedIn;
            entryScrolledWindow.HscrollbarPolicy = Gtk.PolicyType.Never;
            entryScrolledWindow.SizeRequested += delegate(object o, Gtk.SizeRequestedArgs args) {
                // predict and set useful heigth
                var layout = Entry.CreatePangoLayout("Qp");
                int lineWidth, lineHeigth;
                layout.GetPixelSize(out lineWidth, out lineHeigth);
                var text = Entry.Text;
                var newLines = text.Count(f => f == '\n');
                // cap to 1-3 lines
                if (text.Length > 0) {
                    newLines++;
                    newLines = Math.Max(newLines, 1);
                    newLines = Math.Min(newLines, 3);
                } else {
                    newLines = 1;
                }
                // use text heigth + a bit extra
                var bestSize = new Gtk.Requisition() {
                    Height = (lineHeigth * newLines) + 5
                };
                args.Requisition = bestSize;
            };
            entryScrolledWindow.Add(Entry);

            ProgressBar = new Gtk.ProgressBar();

            MenuHBox = new Gtk.HBox();
            MenuHBox.PackStart(MenuBar, false, false, 0);
            MenuHBox.PackEnd(JoinWidget, false, false, 0);

            Gtk.VBox vbox = new Gtk.VBox();
            vbox.PackStart(MenuHBox, false, false, 0);
            vbox.PackStart(Notebook, true, true, 0);
            vbox.PackStart(entryScrolledWindow, false, false, 0);

            NetworkStatusbar = new Gtk.Statusbar();
            NetworkStatusbar.WidthRequest = 300;
            NetworkStatusbar.HasResizeGrip = false;
            
            Statusbar = new Gtk.Statusbar();
            Statusbar.HasResizeGrip = false;
            
            Gtk.HBox status_bar_hbox = new Gtk.HBox();
            status_bar_hbox.Homogeneous = true;
            status_bar_hbox.PackStart(NetworkStatusbar, false, true, 0);
            status_bar_hbox.PackStart(Statusbar, true, true, 0);

            StatusHBox = new Gtk.HBox();
            StatusHBox.PackStart(status_bar_hbox);
            StatusHBox.PackStart(ProgressBar, false, false, 0);
            StatusHBox.ShowAll();
            StatusHBox.NoShowAll = true;
            StatusHBox.Visible = ShowStatusBarMenuItem.Active;

            vbox.PackStart(StatusHBox, false, false, 0);
            Add(vbox);

            if (Frontend.IsMacOSX) {
                IgeMacMenu.GlobalKeyHandlerEnabled = true;
                IgeMacMenu.MenuBar = MenuBar;
                ShowMenuBar = false;

                var appGroup = IgeMacMenu.AddAppMenuGroup();
                appGroup.AddMenuItem(PreferencesMenuItem, _("Preferences"));
                IgeMacMenu.QuitMenuItem = QuitMenuItem;
            }
        }

        public void ApplyConfig(UserConfig userConfig)
        {
            Trace.Call(userConfig);
            
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }
                    
            string modeStr = (string) userConfig["Interface/Notification/NotificationAreaIconMode"];
            NotificationAreaIconMode mode = (NotificationAreaIconMode) Enum.Parse(
                typeof(NotificationAreaIconMode),
                modeStr
            );
            NotificationAreaIconMode = mode;

            OpenLogChatMenuItem.Visible = Frontend.IsLocalEngine;

#if GTK_SHARP_2_10
            StatusIconManager.ApplyConfig(userConfig);
#endif
#if INDICATE_SHARP
            IndicateManager.ApplyConfig(userConfig);
#endif
#if NOTIFY_SHARP
            NotifyManager.ApplyConfig(userConfig);
#endif
            Entry.ApplyConfig(userConfig);
            Notebook.ApplyConfig(userConfig);
            ChatViewManager.ApplyConfig(userConfig);
            JoinWidget.ApplyConfig(userConfig);
        }

        public void UpdateTitle()
        {
            UpdateTitle(null, null);
        }

        public void UpdateTitle(ChatView chatView, string protocolStatus)
        {
            Trace.Call(chatView, protocolStatus);

            if (chatView == null) {
                chatView = Notebook.CurrentChatView;
            }
            if (chatView == null) {
                return;
            }

            string title;
            if (chatView is SessionChatView) {
                title = String.Empty;
            } else if (chatView is ProtocolChatView) {
                title = protocolStatus;
            } else {
                title = String.Format("{0} @ {1}",
                                      chatView.Name,
                                      protocolStatus);
            }
            if (!String.IsNullOrEmpty(title)) {
                title += " - ";
            }
            title += "Smuxi";

            Title = title;
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

        protected virtual void OnDeleteEvent(object sender, Gtk.DeleteEventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                if (NotificationAreaIconMode == NotificationAreaIconMode.Closed) {
                    // showing the tray icon is handled in OnWindowStateEvent
                    Hide();
                    
                    // don't destroy the window nor quit smuxi!
                    e.RetVal = true;
                    return;
                }
                
                Frontend.Quit();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
    
        protected virtual void OnFocusInEvent(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                UrgencyHint = false;
                if (Notebook.IsBrowseModeEnabled) {
                    return;
                }

                ChatView chatView = Notebook.CurrentChatView;
                if (chatView != null) {
                    // clear activity and highlight
                    chatView.HasHighlight = false;
                    chatView.HasActivity = false;
                    var lastMsg = chatView.OutputMessageTextView.LastMessage;
                    if (lastMsg == null || Frontend.UseLowBandwidthMode) {
                        return;
                    }
                    // update last seen highlight
                    ThreadPool.QueueUserWorkItem(delegate {
                        try {
                            // REMOTING CALL 1
                            chatView.ChatModel.LastSeenHighlight = lastMsg.TimeStamp;
                        } catch (Exception ex) {
#if LOG4NET
                            f_Logger.Error("OnFocusInEvent(): Exception", ex);
#endif
                        }
                    });
                }
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnFocusOutEvent(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                if (Notebook.IsBrowseModeEnabled) {
                    return;
                }

                var chatView = Notebook.CurrentChatView;
                if (chatView == null) {
                    return;
                }

                chatView.OutputMessageTextView.UpdateMarkerline();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnServerConnectButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                QuickConnectDialog dialog = new QuickConnectDialog(this);
                dialog.Load();
                int res = dialog.Run();
                ServerModel server = dialog.Server;
                dialog.Destroy();
                if (res != (int) Gtk.ResponseType.Ok) {
                    return;
                }
                if (server == null) {
#if LOG4NET
                    f_Logger.Error("OnServerConnectButtonClicked(): server is null!");
                    return;
#endif
                }
                
                // do connect as background task as it might take a while
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        Frontend.Session.Connect(server, Frontend.FrontendManager);
                    } catch (Exception ex) {
                        Frontend.ShowException(this, ex);
                    }
                });
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

#region Item Event Handlers
        protected virtual void OnServerAddButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            ServerDialog dialog = null;
            try {
                ServerListController controller = new ServerListController(Frontend.UserConfig);
                dialog = new ServerDialog(this, null,
                                          Frontend.Session.GetSupportedProtocols(),
                                          controller.GetNetworks());
                int res = dialog.Run();
                ServerModel server = dialog.GetServer();
                if (res != (int) Gtk.ResponseType.Ok) {
                    return;
                }
                
                controller.AddServer(server);
                controller.Save();
            } catch (InvalidOperationException ex) {
                Frontend.ShowError(this, _("Unable to add server: "), ex);
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            } finally {
                if (dialog != null) {
                    dialog.Destroy();
                }
            }
        }

        protected virtual void OnServerManageServersButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                PreferencesDialog dialog = new PreferencesDialog(this);
                dialog.CurrentPage = PreferencesDialog.Page.Servers;
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnChatFindGroupChatButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            try {
                OpenFindGroupChatWindow();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        public void OpenFindGroupChatWindow()
        {
            OpenFindGroupChatWindow(null);
        }

        public void OpenFindGroupChatWindow(string searchKey)
        {
            var chatView = Notebook.CurrentChatView;
            if (chatView == null) {
                return;
            }

            var manager = chatView.ProtocolManager;
            if (manager == null) {
                return;
            }

            FindGroupChatDialog dialog = new FindGroupChatDialog(
                this, manager
            );
            if (!String.IsNullOrEmpty(searchKey)) {
                dialog.NameEntry.Text = searchKey;
                dialog.FindButton.Click();
            }
            int res = dialog.Run();
            GroupChatModel groupChat = dialog.GroupChat;
            dialog.Destroy();
            if (res != (int) Gtk.ResponseType.Ok) {
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    manager.OpenChat(Frontend.FrontendManager, groupChat);
                } catch (Exception ex) {
                    Frontend.ShowException(this, ex);
                }
            });
        }
        
        protected virtual void OnChatClearAllActivityButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                Notebook.ClearAllActivity();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnFindChatMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnFindNextChatMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnFindPreviousChatMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnCloseChatMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                Notebook.CurrentChatView.Close();
                if (Frontend.IsMacOSX && ChatViewManager.Chats.Count == 1) {
                    Iconify();
                }
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnOpenLogChatMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        SysDiag.Process.Start(Notebook.CurrentChatView.ChatModel.LogFile);
                    } catch (Exception ex) {
                        Frontend.ShowError(this, ex);
                    }
                });
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnNextChatMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ChatViewManager.CurrentChatNumber++;
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnPreviousChatMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ChatViewManager.CurrentChatNumber--;
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
                
        protected virtual void OnWindowStateEvent(object sender, Gtk.WindowStateEventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                // handle minimize / un-minimize
                if ((e.Event.ChangedMask & Gdk.WindowState.Iconified) != 0) {
                    IsMinimized = (e.Event.NewWindowState & Gdk.WindowState.Iconified) != 0;
#if LOG4NET
                    f_Logger.Debug("OnWindowStateEvent(): IsMinimized: " + IsMinimized);
#endif
                    #if DISABLED
                    // BUG: metacity is not allowing us to use the minimize state
                    // to hide and enable the notfication area icon as switching
                    // to a different workspace sets WindowState.Iconified on all
                    // windows, thus this code is disabled. For more details see:
                    // http://projects.qnetp.net/issues/show/158
                    Hide();
                    #endif
                    if (IsMinimized) {
                        if (Minimized != null) {
                            Minimized(this, EventArgs.Empty);
                        }
                    } else {
                        if (Unminimized != null) {
                            Unminimized(this, EventArgs.Empty);
                        }
                    }
                }

                // handle maximize / un-maximize
                if ((e.Event.ChangedMask & Gdk.WindowState.Maximized) != 0) {
                    IsMaximized = (e.Event.NewWindowState & Gdk.WindowState.Maximized) != 0;
#if LOG4NET
                    f_Logger.Debug("OnWindowStateEvent(): IsMaximized: " + IsMaximized);
#endif
                }
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
        
        protected virtual void OnNotebookSwitchPage(object sender, EventArgs e)
        {
            try {
                var chatView = Notebook.CurrentChatView;
                if (chatView == null) {
                    return;
                }

                if (!Frontend.IsMacOSX) {
                    CloseChatMenuItem.Sensitive = !(chatView is SessionChatView);
                }
                FindGroupChatMenuItem.Sensitive = !(chatView is SessionChatView);
                if (Frontend.IsLocalEngine) {
                    OpenLogChatMenuItem.Sensitive =
                        File.Exists(chatView.ChatModel.LogFile);
                }

                // HACK: Gtk.Notebook moves the focus to the child after the
                // page has been switched, so move the focus back to the entry
                GLib.Idle.Add(delegate {
                    Entry.GrabFocus();
                    return false;
                });
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnNotebookFocusInEvent(object sender, Gtk.FocusInEventArgs e)
        {
            // HACK: having the focus in the notebook doesn't make any sense,
            // so move focus back to the entry
            Entry.GrabFocus();
        }

        protected virtual void OnJoinWidgetActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                var chatLink = JoinWidget.GetChatLink();
                Frontend.OpenChatLink(chatLink);
                JoinWidget.Clear();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnShowQuickJoinMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                JoinWidget.Visible = !JoinWidget.Visible;
                Frontend.FrontendConfig["ShowQuickJoin"] = JoinWidget.Visible;
                Frontend.FrontendConfig.Save();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnShowMenuBarMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                MenuBar.Visible = ShowMenuBarMenuItem.Active;
                Frontend.FrontendConfig["ShowMenuBar"] = MenuBar.Visible;
                Frontend.FrontendConfig.Save();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnShowStatusBarMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                StatusHBox.Visible = ShowStatusBarMenuItem.Active;
                Frontend.FrontendConfig["ShowStatusBar"] = StatusHBox.Visible;
                Frontend.FrontendConfig.Save();
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected virtual void OnOpenChatMenuItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                if (!ShowQuickJoinMenuItem.Active) {
                    ShowQuickJoinMenuItem.Activate();
                }
                JoinWidget.HasFocus = true;
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }

        protected static void AccelCanActivateSensitive(object sender, Gtk.AccelCanActivateArgs e)
        {
            var widget = sender as Gtk.Widget;
            if (widget != null && !widget.Sensitive) {
                e.RetVal = false;
                return;
            }

            // allow the accelerator to be used even when the menu bar is hidden
            e.RetVal = true;
        }

        private void _OnAboutButtonClicked(object obj, EventArgs args)
        {
            Trace.Call(obj, args);
            
            try {
                AboutDialog ad = new AboutDialog(this);
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
                new PreferencesDialog(this);
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
                EngineAssistant assistant = new EngineAssistant(
                    this,
                    Frontend.FrontendConfig
                );
                assistant.Cancel += delegate {
                    assistant.Destroy();
                };
                assistant.Close += delegate {
                    assistant.Destroy();
                };
                assistant.ShowAll();
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
                CaretMode = !CaretMode;
                
                for (int i = 0; i < Notebook.NPages; i++) {
                    ChatView chatView = Notebook.GetChat(i);
                    chatView.OutputMessageTextView.CursorVisible = CaretMode;
                }
                
                if (CaretMode) {
                    Notebook.CurrentChatView.OutputMessageTextView.HasFocus = true;
                } else {
                    Entry.HasFocus = true;
                }
            } catch (Exception ex) {
                Frontend.ShowException(this, ex);
            }
        }
#endregion
        
        protected void OnChatViewManagerChatAdded(object sender, ChatViewManagerChatAddedEventArgs e)
        {
            Trace.Call(sender, e);

            e.ChatView.MessageHighlighted += OnChatViewMessageHighlighted;
            e.ChatView.OutputMessageTextView.FocusInEvent += delegate {
                if (CaretMode) {
                    return;
                }
                Entry.GrabFocus();
            };
            UpdateProgressBar();
        }
        
        protected void OnChatViewManagerChatSynced(object sender, ChatViewManagerChatSyncedEventArgs e)
        {
            Trace.Call(sender, e);

            UpdateProgressBar();
        }

        protected void OnChatViewManagerChatRemoved(object sender, ChatViewManagerChatRemovedEventArgs e)
        {
            Trace.Call(sender, e);
            
            e.ChatView.MessageHighlighted -= OnChatViewMessageHighlighted;
        }
        
        protected void OnChatViewMessageHighlighted(object sender, ChatViewMessageHighlightedEventArgs e)
        {
#if MSG_DEBUG
            Trace.Call(sender, e);
#endif

            if (!HasToplevelFocus) {
                UrgencyHint = true;
            }
        }

        public void UpdateProgressBar()
        {
            var totalChatCount = ChatViewManager.Chats.Count;
            var syncedChatCount =  ChatViewManager.SyncedChats.Count;
            ProgressBar.Fraction = (double)syncedChatCount / totalChatCount;
            ProgressBar.Text = String.Format("{0} / {1}",
                                              syncedChatCount,
                                              totalChatCount);
            if (syncedChatCount >= totalChatCount) {
                ProgressBar.Hide();
            } else {
                ProgressBar.Show();
            }
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
