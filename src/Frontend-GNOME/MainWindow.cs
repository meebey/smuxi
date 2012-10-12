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
        Gtk.HBox StatusHBox { get; set; }
        public MenuWidget MenuWidget { get; private set; }

        public IFrontendUI UI { get; private set; }
        public Entry Entry { get; private set; }
        public Notebook Notebook { get; private set; }
        public ChatViewManager ChatViewManager { get; private set; }
        public EngineManager EngineManager { get; private set; }
#if GTK_SHARP_2_10
        StatusIconManager StatusIconManager { get; set; }
#endif
#if INDICATE_SHARP || MESSAGING_MENU_SHARP
        IndicateManager IndicateManager { get; set; }
#endif
#if NOTIFY_SHARP
        NotifyManager NotifyManager { get; set; }
#endif
#if IPC_DBUS
        NetworkManager NetworkManager { get; set; }
#endif

        public NotificationAreaIconMode NotificationAreaIconMode { get; set; }
        public bool IsMinimized { get; private set; }
        public bool IsMaximized { get; private set; }

        public bool CaretMode {
            get {
                return MenuWidget.CaretMode;
            }
        }

        public bool ShowMenuBar {
            get {
                return MenuWidget.MenuBar.Visible;
            }
            set {
                MenuWidget.ShowMenubarAction.Active = value;
            }
        }

        public bool ShowStatusbar {
            get {
                return StatusHBox.Visible;
            }
            set {
                StatusHBox.Visible = value;
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
#if INDICATE_SHARP || MESSAGING_MENU_SHARP
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
            StatusHBox = new Gtk.HBox();

            MenuWidget = new MenuWidget(this, ChatViewManager);

            Gtk.VBox vbox = new Gtk.VBox();
            vbox.PackStart(MenuWidget, false, false, 0);
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

            StatusHBox.PackStart(status_bar_hbox);
            StatusHBox.PackStart(ProgressBar, false, false, 0);
            StatusHBox.ShowAll();
            StatusHBox.NoShowAll = true;
            StatusHBox.Visible = (bool) Frontend.FrontendConfig["ShowStatusBar"];

            vbox.PackStart(StatusHBox, false, false, 0);
            Add(vbox);
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

            MenuWidget.OpenLogAction.Visible = Frontend.IsLocalEngine;

#if GTK_SHARP_2_10
            StatusIconManager.ApplyConfig(userConfig);
#endif
#if INDICATE_SHARP || MESSAGING_MENU_SHARP
            IndicateManager.ApplyConfig(userConfig);
#endif
#if NOTIFY_SHARP
            NotifyManager.ApplyConfig(userConfig);
#endif
            Entry.ApplyConfig(userConfig);
            Notebook.ApplyConfig(userConfig);
            ChatViewManager.ApplyConfig(userConfig);
            MenuWidget.JoinWidget.ApplyConfig(userConfig);
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
                    MenuWidget.CloseChatAction.Sensitive = !(chatView is SessionChatView);
                }
                MenuWidget.FindGroupChatAction.Sensitive = !(chatView is SessionChatView);
                if (Frontend.IsLocalEngine) {
                    MenuWidget.OpenLogAction.Sensitive =
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
