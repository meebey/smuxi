// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010-2011 Mirco Bauer <meebey@meebey.net>
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

#if INDICATE_SHARP
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Indicate;
#if IPC_DBUS
    #if DBUS_SHARP
using DBus;
    #else
using NDesk.DBus;
    #endif
#endif
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class IndicateManager : IDisposable
    {
#if LOG4NET
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        const string BusName = "com.canonical.indicate";
        private static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static string PersonChatIconBase64 { get; set; }
        private static string GroupChatIconBase64  { get; set; }
        Server Server { get; set; }
        MainWindow MainWindow { get; set; }
        ChatViewManager ChatViewManager { get; set; }
        Dictionary<ChatView, Indicator> Indicators { get; set; }
        Dictionary<ChatView, MessageTextViewMessageHighlightedEventHandler> HighlightEventHandlers { get; set; }
        bool IsInitialized { get; set; }
        bool IsEnabled { get; set; }

        static IndicateManager()
        {
            PersonChatIconBase64 = Convert.ToBase64String(
                PersonChatView.IconPixbuf.SaveToBuffer("png")
            );
            GroupChatIconBase64 = Convert.ToBase64String(
                GroupChatView.IconPixbuf.SaveToBuffer("png")
            );
        }

        public IndicateManager(MainWindow mainWindow,
                               ChatViewManager chatViewManager)
        {
            Trace.Call(mainWindow, chatViewManager);

            if (mainWindow == null) {
                throw new ArgumentNullException("mainWindow");
            }
            if (chatViewManager == null) {
                throw new ArgumentNullException("chatViewManager");
            }

            MainWindow = mainWindow;
            ChatViewManager = chatViewManager;
            Indicators = new Dictionary<ChatView, Indicator>();
            HighlightEventHandlers = new Dictionary
                <ChatView,
                 MessageTextViewMessageHighlightedEventHandler>();

            try {
                Init();
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("IndicateManager(): initialization failed: ", ex);
#endif
            }
        }
        
        public void Dispose()
        {
            Trace.Call();

            MainWindow.FocusInEvent -= OnMainWindowFocusInEvent;
            MainWindow.Notebook.SwitchPage -= OnMainWindowNotebookSwitchPage;

            ChatViewManager.ChatAdded   -= OnChatViewManagerChatAdded;
            ChatViewManager.ChatRemoved -= OnChatViewManagerChatRemoved;

            Server.Hide();
            Server.Dispose();
        }

        public void ApplyConfig(UserConfig userConfig)
        {
            Trace.Call(userConfig);
            
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }

            if (!IsInitialized) {
                return;
            }

            if ((bool) userConfig["Interface/Notification/MessagingMenuEnabled"]) {
                Server.Show();
                IsEnabled = true;
                // hide the main window instead of closing it
                MainWindow.NotificationAreaIconMode = NotificationAreaIconMode.Closed;
            } else {
                Server.Hide();
                IsEnabled = false;
            }
        }

        void Init()
        {
            Trace.Call();

#if IPC_DBUS
            if (!Bus.Session.NameHasOwner(BusName)) {
    #if LOG4NET
                Logger.Info("Init(): no DBus provider for messaging menu found, " +
                            "disabling...");
                return;
            }
    #endif
#endif

            Server = Server.RefDefault();
            if (Server == null) {
                // just in case
                return;
            }
            // all checks return false for some reason
            /*
            if (!Server.CheckInterest(Interests.ServerDisplay)) {
#if LOG4NET
                Logger.Info("Init() the indicate server is not interested in " +
                            "us, thus no messaging menu :/");
#endif
                return;
            }
            */

            var partialPath = "share";
            partialPath = Path.Combine(partialPath, "applications");
            partialPath = Path.Combine(partialPath, "smuxi-frontend-gnome.desktop");

            var insDesktopFile = Path.Combine(Defines.InstallPrefix, partialPath);
            var sysDesktopFile = Path.Combine("/usr", partialPath);
            string desktopFile = null;
            if (File.Exists(insDesktopFile)) {
                desktopFile = insDesktopFile;
            } else if (File.Exists(sysDesktopFile)) {
                desktopFile = sysDesktopFile;
            } else {
#if LOG4NET
                Logger.Error("Init(): smuxi-frontend-gnome.desktop could not " +
                             " be found, thus no messaging menu :/");
#endif
                return;
            }

            Server.SetType("message.im");
            Server.DesktopFile(desktopFile);
            Server.ServerDisplay += OnServerServerDisplay;

            MainWindow.FocusInEvent += OnMainWindowFocusInEvent;
            MainWindow.Notebook.SwitchPage += OnMainWindowNotebookSwitchPage;

            ChatViewManager.ChatAdded   += OnChatViewManagerChatAdded;
            ChatViewManager.ChatRemoved += OnChatViewManagerChatRemoved;

            IsInitialized = true;
        }

        void OnMainWindowFocusInEvent(object sender, Gtk.FocusInEventArgs e)
        {
            if (MainWindow.Notebook.IsBrowseModeEnabled) {
                return;
            }

            var currentChatView = MainWindow.Notebook.CurrentChatView;
            if (currentChatView == null) {
                return;
            }
            DisposeIndicator(currentChatView);
        }

        void OnMainWindowNotebookSwitchPage(object sender, Gtk.SwitchPageArgs e)
        {
            if (MainWindow.Notebook.IsBrowseModeEnabled) {
                return;
            }

            var currentChatView = MainWindow.Notebook.CurrentChatView;
            if (currentChatView == null) {
                return;
            }
            DisposeIndicator(currentChatView);
        }

        void OnServerServerDisplay(object sender, ServerDisplayArgs e)
        {
            Trace.Call(sender, e);

            MainWindow.PresentWithTime(e.Timestamp);
        }

        void OnChatViewManagerChatAdded(object sender, ChatViewManagerChatAddedEventArgs e)
        {
            // we are only interested in highlights on person and group chats
            if (!(e.ChatView is PersonChatView) &&
                !(e.ChatView is GroupChatView)) {
                return;
            }

            MessageTextViewMessageHighlightedEventHandler handler =
            delegate(object o, MessageTextViewMessageHighlightedEventArgs args) {
                    OnChatViewMessageHighlighted(o, args, e.ChatView);
            };
            e.ChatView.OutputMessageTextView.MessageHighlighted += handler;

            // keep a reference to the handler so we can cleanup it up later
            // in OnChatViewManagerChatRemoved()
            HighlightEventHandlers.Add(e.ChatView, handler);
        }

        void OnChatViewManagerChatRemoved(object sender, ChatViewManagerChatRemovedEventArgs e)
        {
            MessageTextViewMessageHighlightedEventHandler handler;
            if (!HighlightEventHandlers.TryGetValue(e.ChatView, out handler)) {
                return;
            }

            e.ChatView.OutputMessageTextView.MessageHighlighted -= handler;

            // close possibly active indicator
            DisposeIndicator(e.ChatView);
        }

        void OnChatViewMessageHighlighted(object sender,
                                          MessageTextViewMessageHighlightedEventArgs e,
                                          ChatView chatView)
        {
            if (!IsEnabled ||
                e.Message.TimeStamp <= chatView.SyncedLastSeenHighlight ||
                MainWindow.HasToplevelFocus) {
                return;
            }

            Indicator indicator;
            if (Indicators.TryGetValue(chatView, out indicator)) {
                // update time of existing indicator
                indicator.SetProperty(
                    "time",
                    e.Message.TimeStamp.ToLocalTime().ToString("s")
                );
                return;
            }
            
            indicator = new Indicator();
            indicator.SetProperty("subtype", "im");
            if (chatView is PersonChatView) {
                indicator.SetProperty("icon", PersonChatIconBase64);
                indicator.SetProperty("sender", chatView.Name);
            }
            if (chatView is GroupChatView) {
                indicator.SetProperty("icon", GroupChatIconBase64);
                var nick = GetNick(e.Message);
                if (nick == null) {
                    indicator.SetProperty("sender", chatView.Name);
                } else {
                    indicator.SetProperty("sender",
                        String.Format(
                            "{0} ({1})",
                            chatView.Name, nick
                        )
                    );
                }
            }
            indicator.SetProperty(
                "time",
                e.Message.TimeStamp.ToLocalTime().ToString("s")
            );
            indicator.SetPropertyBool("draw-attention", true);
            indicator.UserDisplay += delegate {
                try {
                    MainWindow.Present();
                    MainWindow.Notebook.CurrentChatView = chatView;
                    DisposeIndicator(chatView);
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("OnChatViewMessageHighlighted() " +
                                 "indicator.UserDisplay threw exception", ex);
#endif
                }
            };
            try {
                indicator.Show();
            } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("OnChatViewMessageHighlighted() " +
                                 "indicator.Show() thew exception", ex);
#endif
            }

            Indicators.Add(chatView, indicator);
        }

        void DisposeIndicator(ChatView chatView)
        {
            Indicator indicator;
            if (!Indicators.TryGetValue(chatView, out indicator)) {
                return;
            }
#if LOG4NET
            Logger.Debug("DisposeIndicator(): disposing indicator for: " +
                         chatView.Name);
#endif

            try {
                indicator.Hide();
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("DisposeIndicator(): " +
                             "indicator.Hide() thew exception", ex);
#endif
            } finally {
                Indicators.Remove(chatView);
            }
        }

        string GetNick(MessageModel msg)
        {
            // HACK: try to obtain the nickname from the message
            // TODO: extend MessageModel with Origin property
            var msgText = msg.ToString();
            var match = Regex.Match(msgText, "^<([^ ]+)>");
            if (match.Success && match.Groups.Count >= 2) {
                return match.Groups[1].Value;
            }

            return null;
        }
    }
}
#endif
