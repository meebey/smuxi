// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2012 Mirco Bauer <meebey@meebey.net>
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
using System;
using System.Threading;
using SysDiag = System.Diagnostics;
using IgeMacIntegration;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class MenuWidget : Gtk.Bin
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        new Gtk.Window Parent { get; set; }
        MainWindow MainWindow { get; set; }
        public JoinWidget JoinWidget { get; private set; }
        ChatViewManager ChatViewManager { get; set; }

        public bool CaretMode {
            get {
                return f_CaretModeAction.Active;
            }
        }

        public Gtk.MenuBar MenuBar {
            get {
                return f_MenuBar;
            }
        }

        public Gtk.ToggleAction ShowMenubarAction {
            get {
                return f_ShowMenubarAction;
            }
        }

        public Gtk.Action OpenLogAction {
            get {
                return f_OpenLogAction;
            }
        }

        public Gtk.Action OpenLogToolAction {
            get {
                return f_OpenLogToolAction;
            }
        }

        public Gtk.Action CloseChatAction {
            get {
                return f_CloseChatAction;
            }
        }

        public Gtk.Action FindGroupChatAction {
            get {
                return f_FindGroupChatAction;
            }
        }

        public MenuWidget(Gtk.Window parent, ChatViewManager chatViewManager)
        {
            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            if (chatViewManager == null) {
                throw new ArgumentNullException("chatViewManager");
            }

            Parent = parent;
            MainWindow = parent as MainWindow;
            ChatViewManager = chatViewManager;

            Build();

            // Smuxi Menu
            f_QuitAction.IconName = Gtk.Stock.Quit;

            // Chat
            f_JoinChatAction.IconName = Gtk.Stock.Open;
            f_FindGroupChatAction.IconName = Gtk.Stock.Find;
            f_OpenLogAction.IconName = Gtk.Stock.Open;
            f_CloseChatAction.IconName = Gtk.Stock.Close;

            // Engine
            f_AddRemoteEngineAction.IconName = Gtk.Stock.Add;
            f_SwitchRemoteEngineAction.IconName = Gtk.Stock.Refresh;

            // Toolbar
            f_ConnectToolAction.IconName = Gtk.Stock.Network;
            f_OpenLogToolAction.IconName = Gtk.Stock.Open;
            f_FindGroupChatToolAction.IconName = Gtk.Stock.Find;

            f_MenuToolbar.ShowAll();
            f_MenuToolbar.NoShowAll = true;
            f_MenuToolbar.Visible = (bool) Frontend.FrontendConfig["ShowToolBar"];

            f_MenuBar.ShowAll();
            f_MenuBar.NoShowAll = true;
            f_MenuBar.Visible = (bool) Frontend.FrontendConfig["ShowMenuBar"];

            JoinWidget = new JoinWidget();
            JoinWidget.NoShowAll = true;
            JoinWidget.Activated += OnJoinWidgetActivated;

            var joinToolItem = new Gtk.ToolItem();
            joinToolItem.Add(JoinWidget);
            f_JoinToolbar.Add(joinToolItem);
            f_JoinToolbar.ShowAll();
            f_JoinToolbar.NoShowAll = true;
            f_JoinToolbar.Visible = f_MenuToolbar.Visible;

            f_ShowMenubarAction.Active = (bool) Frontend.FrontendConfig["ShowMenuBar"];
            f_ShowToolbarAction.Active = (bool) Frontend.FrontendConfig["ShowToolBar"];
            f_ShowStatusbarAction.Active = (bool) Frontend.FrontendConfig["ShowStatusBar"];

            if (Frontend.IsMacOSX) {
                // Smuxi menu is already shown as app menu
                f_SmuxiAction.Visible = false;
                // About item is already shown in app menu
                f_AboutAction.Visible = false;

                IgeMacMenu.GlobalKeyHandlerEnabled = true;
                IgeMacMenu.MenuBar = f_MenuBar;
                f_ShowMenubarAction.Active = false;
                // no need for the menu bar as have the app menu
                f_ShowMenubarAction.Visible = false;

                var appGroup = IgeMacMenu.AddAppMenuGroup();
                appGroup.AddMenuItem(
                    (Gtk.MenuItem) f_AboutAction.CreateMenuItem(),
                    _("About Smuxi")
                );
                var prefItem = (Gtk.MenuItem) f_PreferencesAction.CreateMenuItem();
                // TODO: add cmd+, accelerator
                appGroup.AddMenuItem(prefItem, _("Preferences"));
                IgeMacMenu.QuitMenuItem = (Gtk.MenuItem)
                    f_QuitAction.CreateMenuItem();
            }
        }

        protected void OnAboutActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                var dialog = new AboutDialog(Parent);
                dialog.Run();
                dialog.Destroy();
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnPreferencesActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                var dialog = new PreferencesDialog(Parent);
                dialog.Show();
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnQuitActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                Frontend.Quit();
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnConnectActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                var dialog = new QuickConnectDialog(Parent);
                dialog.Load();
                int res = dialog.Run();
                var server = dialog.Server;
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
                        if (Frontend.EngineVersion < new Version(0, 8, 11)) {
                            // HACK: Smuxi < 0.8.11 used auto serialization for
                            // ServerModel and thus breaks on unknown fields,
                            // which we skip by setting this to null, see:
                            // ServerModel.GetObjectData()
                            server.ServerID = null;
                        }
                        Frontend.Session.Connect(server, Frontend.FrontendManager);
                    } catch (Exception ex) {
                        Frontend.ShowException(Parent, ex);
                    }
                });
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnAddServerActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            ServerDialog dialog = null;
            try {
                var controller = new ServerListController(Frontend.UserConfig);
                dialog = new ServerDialog(Parent, null,
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
                Frontend.ShowError(Parent, _("Unable to add server: "), ex);
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            } finally {
                if (dialog != null) {
                    dialog.Destroy();
                }
            }
        }

        protected void OnManageServerActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                var dialog = new PreferencesDialog(Parent);
                dialog.CurrentPage = PreferencesDialog.Page.Servers;
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnJoinChatActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                if (!f_ShowToolbarAction.Active) {
                    f_ShowToolbarAction.Activate();
                }
                JoinWidget.HasFocus = true;
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnFindGroupChatActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                Frontend.OpenFindGroupChatWindow();
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnClearAllActivityActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ChatViewManager.ClearAllActivity();
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnNextChatActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ChatViewManager.CurrentChatNumber++;
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnPreviousChatActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ChatViewManager.CurrentChatNumber--;
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnOpenLogActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        SysDiag.Process.Start(
                            ChatViewManager.CurrentChatView.ChatModel.LogFile
                        );
                    } catch (Exception ex) {
                        Frontend.ShowError(Parent, ex);
                    }
                });
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnCloseChatActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ChatViewManager.CurrentChatView.Close();
                if (Frontend.IsMacOSX && ChatViewManager.Chats.Count == 1) {
                    ChatViewManager.Minimize();
                }
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnUseLocalEngineActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                var dialog = new Gtk.MessageDialog(
                    Parent, Gtk.DialogFlags.Modal,
                    Gtk.MessageType.Warning, Gtk.ButtonsType.YesNo,
                    _("Switching to local engine will disconnect you from the current engine!\n"+
                      "Are you sure you want to do this?")
                );
                int result = dialog.Run();
                dialog.Destroy();
                if ((Gtk.ResponseType)result == Gtk.ResponseType.Yes) {
                    Frontend.DisconnectEngineFromGUI();
                    Frontend.InitLocalEngine();
                    Frontend.ConnectEngineToGUI();
                }
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnAddRemoteEngineActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                var assistant = new EngineAssistant(
                    Parent,
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
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnSwitchRemoteEngineActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                var dialog = new Gtk.MessageDialog(
                    Parent, Gtk.DialogFlags.Modal,
                    Gtk.MessageType.Warning, Gtk.ButtonsType.YesNo,
                    _("Switching the remote engine will disconnect you from the current engine!\n"+
                      "Are you sure you want to do this?")
                );
                int result = dialog.Run();
                dialog.Destroy();
                if ((Gtk.ResponseType)result == Gtk.ResponseType.Yes) {
                    Frontend.DisconnectEngineFromGUI();
                    Frontend.ShowEngineManagerDialog();
                }
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnCaretModeActionToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                var caretMode = f_CaretModeAction.Active;

                foreach (var chatView in ChatViewManager.Chats) {
                    chatView.OutputMessageTextView.CursorVisible = caretMode;
                }
                
                if (caretMode) {
                    ChatViewManager.CurrentChatView.OutputMessageTextView.HasFocus = true;
                } else {
                    MainWindow.Entry.HasFocus = true;
                }
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnBrowseModeActionToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                var browseMode = f_BrowseModeAction.Active;
                MainWindow.Notebook.IsBrowseModeEnabled = browseMode;
                if (browseMode) {
                    MainWindow.ChatTreeView.HasFocus = true;
                } else {
                    MainWindow.Entry.HasFocus = true;
                }
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnShowMenubarActionToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                var active = f_ShowMenubarAction.Active;
                f_MenuBar.Visible = active;
                Frontend.FrontendConfig["ShowMenuBar"] = active;
                Frontend.FrontendConfig.Save();
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnShowToolbarActionToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                var active = f_ShowToolbarAction.Active;
                f_MenuToolbar.Visible = active;
                // also hide/show join bar
                f_JoinToolbar.Visible = active;
                Frontend.FrontendConfig["ShowToolBar"] = active;
                Frontend.FrontendConfig.Save();
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnShowStatusbarActionToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                var active = f_ShowStatusbarAction.Active;
                MainWindow.ShowStatusbar = active;
                Frontend.FrontendConfig["ShowStatusBar"] = active;
                Frontend.FrontendConfig.Save();
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected virtual void OnJoinWidgetActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                var chatLink = JoinWidget.GetChatLink();
                Frontend.OpenChatLink(chatLink);
                JoinWidget.Clear();
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnFullscreenActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                MainWindow.IsFullscreen = !MainWindow.IsFullscreen;
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        protected void OnWebsiteActionActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                Frontend.OpenLink(new Uri("http://www.smuxi.org/"));
            } catch (Exception ex) {
                Frontend.ShowException(Parent, ex);
            }
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
