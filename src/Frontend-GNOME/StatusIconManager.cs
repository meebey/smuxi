// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
#if GTK_SHARP_2_10
    public class StatusIconManager
    {
        Gtk.StatusIcon           f_StatusIcon;
        MainWindow               f_MainWindow;
        ChatViewManager          f_ChatViewManager;
        NotificationAreaIconMode f_NotificationAreaIconMode;

        public StatusIconManager(MainWindow mainWindow, ChatViewManager chatViewManager)
        {
            if (mainWindow == null) {
                throw new ArgumentNullException("mainWindow");
            }
            if (chatViewManager == null) {
                throw new ArgumentNullException("chatViewManager");
            }

            f_MainWindow               = mainWindow;
            f_MainWindow.FocusInEvent += OnMainWindowFocusInEvent;
            f_MainWindow.Minimized    += delegate {
                CheckMainWindowState();
            };
            f_MainWindow.Unminimized  += delegate {
                CheckMainWindowState();
            };
            f_MainWindow.Hidden       += delegate {
                CheckMainWindowState();
            };
            f_MainWindow.Shown        += delegate {
                CheckMainWindowState();
            };

            f_ChatViewManager              = chatViewManager;
            f_ChatViewManager.ChatAdded   += OnChatViewManagerChatAdded;
            f_ChatViewManager.ChatRemoved += OnChatViewManagerChatRemoved;
        }

        public void ApplyConfig(UserConfig userConfig)
        {
            Trace.Call(userConfig);

            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }

            string modeStr = (string) userConfig["Interface/Notification/NotificationAreaIconMode"];
            f_NotificationAreaIconMode = (NotificationAreaIconMode) Enum.Parse(
                typeof(NotificationAreaIconMode),
                modeStr
            );

            // initialize status icon for the first time
            if (f_NotificationAreaIconMode != NotificationAreaIconMode.Never &&
                f_StatusIcon == null) {
                f_StatusIcon = new Gtk.StatusIcon();
                if (Frontend.HasSystemIconTheme) {
                    f_StatusIcon.IconName = Frontend.IconName;
                } else {
                    f_StatusIcon.Pixbuf = Frontend.LoadIcon(
                        Frontend.IconName, 256, "icon_256x256.png"
                    );
                }
                f_StatusIcon.Activate += OnStatusIconActivated;
                f_StatusIcon.PopupMenu += OnStatusIconPopupMenu;
                f_StatusIcon.Tooltip = "Smuxi";
            }
            if (f_NotificationAreaIconMode == NotificationAreaIconMode.Never &&
                !f_MainWindow.Visible) {
                // force window unhide as the user would not be able to bring
                // it back without a notification icon!
                f_MainWindow.Visible = true;
            }

            CheckMainWindowState();
        }

        private void CheckMainWindowState()
        {
            Trace.Call();

            if (f_StatusIcon == null) {
                return;
            }

            switch (f_NotificationAreaIconMode) {
                case NotificationAreaIconMode.Never:
                    f_StatusIcon.Visible = false;
                    break;
                case NotificationAreaIconMode.Always:
                    f_StatusIcon.Visible = true;
                    break;
                case NotificationAreaIconMode.Minimized:
                    f_StatusIcon.Visible = f_MainWindow.IsMinimized;
                    break;
                case NotificationAreaIconMode.Closed:
                    f_StatusIcon.Visible = !f_MainWindow.Visible;
                    break;
            }
        }

        private void OnStatusIconActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_StatusIcon == null) {
                return;
            }

            try {
                if (f_StatusIcon.Blinking) {
                    f_MainWindow.Present();
                    return;
                }
                // not everyone uses a window list applet thus we have to
                // restore from minimized state here, see:
                // http://projects.qnetp.net/issues/show/159
                if (f_MainWindow.IsMinimized) {
                    f_MainWindow.Present();
                    return;
                }
                f_MainWindow.Visible = !f_MainWindow.Visible;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        private void OnStatusIconPopupMenu(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            Gtk.Menu menu = new Gtk.Menu();

            Gtk.ImageMenuItem preferencesItem = new Gtk.ImageMenuItem(
                Gtk.Stock.Preferences, null
            );
            preferencesItem.Activated += delegate {
                try {
                    PreferencesDialog dialog = new PreferencesDialog(f_MainWindow);
                    dialog.CurrentPage = PreferencesDialog.Page.Interface;
                    dialog.CurrentInterfacePage = PreferencesDialog.InterfacePage.Notification;
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            };
            menu.Add(preferencesItem);
            
            menu.Add(new Gtk.SeparatorMenuItem());
            
            Gtk.ImageMenuItem quitItem = new Gtk.ImageMenuItem(
                Gtk.Stock.Quit, null
            );
            quitItem.Activated += delegate {
                try {
                    Frontend.Quit();
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            };
            menu.Add(quitItem);
            
            menu.ShowAll();
            menu.Popup();
        }

        private void OnMainWindowFocusInEvent(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_StatusIcon == null) {
                return;
            }

            f_StatusIcon.Blinking = false;
        }

        protected void OnChatViewManagerChatAdded(object sender, ChatViewManagerChatAddedEventArgs e)
        {
            e.ChatView.OutputMessageTextView.MessageHighlighted += OnChatViewMessageHighlighted;
        }

        protected void OnChatViewManagerChatRemoved(object sender, ChatViewManagerChatRemovedEventArgs e)
        {
            e.ChatView.OutputMessageTextView.MessageHighlighted -= OnChatViewMessageHighlighted;
        }

        private void OnChatViewMessageHighlighted(object sender, MessageTextViewMessageHighlightedEventArgs e)
        {
            Trace.Call(sender, e);

            if (f_StatusIcon == null) {
                return;
            }

            if (!f_MainWindow.HasToplevelFocus) {
                f_StatusIcon.Blinking = true;
            }
        }
    }
#endif
}
