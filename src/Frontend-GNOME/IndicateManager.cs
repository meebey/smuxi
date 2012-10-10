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

#if INDICATE_SHARP || MESSAGING_MENU_SHARP
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#if INDICATE_SHARP
using Indicate;
#elif MESSAGING_MENU_SHARP
using MessagingMenu;
#endif
#if IPC_DBUS
    #if DBUS_SHARP
using DBus;
    #else
using NDesk.DBus;
    #endif
#endif
using Gtk.Extensions;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class IndicateManager : IDisposable
    {
#if LOG4NET
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        const string BusName = "com.canonical.indicator.session";
        private static string PersonChatIconBase64 { get; set; }
        private static string GroupChatIconBase64  { get; set; }
#if INDICATE_SHARP
        Server Server { get; set; }
        Dictionary<ChatView, Indicator> Indicators { get; set; }
#elif MESSAGING_MENU_SHARP
        private static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
        App App { get; set; }
        Dictionary<ChatView, string> Sources { get; set; }
#endif
        MainWindow MainWindow { get; set; }
        ChatViewManager ChatViewManager { get; set; }
        Dictionary<ChatView, MessageTextViewMessageHighlightedEventHandler> HighlightEventHandlers { get; set; }
        bool IsInitialized { get; set; }
        bool IsEnabled { get; set; }
        string DesktopFile { get; set; }

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
#if INDICATE_SHARP
            Indicators = new Dictionary<ChatView, Indicator>();
#elif MESSAGING_MENU_SHARP
            Sources = new Dictionary<ChatView, string>();
#endif
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

#if INDICATE_SHARP
            Server.Hide();
#elif MESSAGING_MENU_SHARP
            App.Unregister();
            App.Dispose();
#endif
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

            var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var blacklistPath = Path.Combine(home, ".config");
            blacklistPath = Path.Combine(blacklistPath, "indicators");
            blacklistPath = Path.Combine(blacklistPath, "messages");
            blacklistPath = Path.Combine(blacklistPath, "applications-blacklist");
            if (!Directory.Exists(blacklistPath)) {
                Directory.CreateDirectory(blacklistPath);
            }
            blacklistPath = Path.Combine(blacklistPath, "smuxi-frontend-gnome");

            if ((bool) userConfig["Interface/Notification/MessagingMenuEnabled"]) {
                // persist in menu
                if (File.Exists(blacklistPath)) {
                    File.Delete(blacklistPath);
                }
                var path = Path.Combine(home, ".config");
                path = Path.Combine(path, "indicators");
                path = Path.Combine(path, "messages");
                path = Path.Combine(path, "applications");
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, "smuxi-frontend-gnome");
                File.WriteAllText(path, DesktopFile + "\n");

#if INDICATE_SHARP
                Server.Show();
#elif MESSAGING_MENU_SHARP
                App.Register();
#endif
                IsEnabled = true;
                // hide the main window instead of closing it
                MainWindow.NotificationAreaIconMode = NotificationAreaIconMode.Closed;
            } else {
                // non-persistent in menu using the blacklist as per
                // specification:
                // https://wiki.ubuntu.com/MessagingMenu/#Registration
                File.WriteAllText(blacklistPath, DesktopFile + "\n");

#if INDICATE_SHARP
                Server.Hide();
#elif MESSAGING_MENU_SHARP
                App.Unregister();
#endif
                IsEnabled = false;
            }
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
#if INDICATE_SHARP
            DisposeIndicator(currentChatView);
#elif MESSAGING_MENU_SHARP
            DisposeSource(currentChatView);
#endif
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
#if INDICATE_SHARP
            DisposeIndicator(currentChatView);
#elif MESSAGING_MENU_SHARP
            DisposeSource(currentChatView);
#endif
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
#if INDICATE_SHARP
            DisposeIndicator(e.ChatView);
#elif MESSAGING_MENU_SHARP
            DisposeSource(e.ChatView);
#endif
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

#if INDICATE_SHARP
            ShowIndicator(chatView, e.Message);
#elif MESSAGING_MENU_SHARP
            ShowSource(chatView, e.Message);
#endif
        }

#if INDICATE_SHARP
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
            if (File.Exists(insDesktopFile)) {
                DesktopFile = insDesktopFile;
            } else if (File.Exists(sysDesktopFile)) {
                DesktopFile = sysDesktopFile;
            } else {
#if LOG4NET
                Logger.Error("Init(): smuxi-frontend-gnome.desktop could not " +
                             " be found, thus no messaging menu :/");
#endif
                return;
            }

            Server.SetType("message.im");
            Server.DesktopFile(DesktopFile);
            Server.ServerDisplay += OnServerServerDisplay;

            MainWindow.FocusInEvent += OnMainWindowFocusInEvent;
            MainWindow.Notebook.SwitchPage += OnMainWindowNotebookSwitchPage;

            ChatViewManager.ChatAdded   += OnChatViewManagerChatAdded;
            ChatViewManager.ChatRemoved += OnChatViewManagerChatRemoved;

            IsInitialized = true;
        }

        void ShowIndicator(ChatView chatView, MessageModel msg)
        {
            Indicator indicator;
            if (Indicators.TryGetValue(chatView, out indicator)) {
                // update time of existing indicator
                indicator.SetProperty(
                    "time",
                    msg.TimeStamp.ToLocalTime().ToString("s")
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
                var nick = GetNick(msg);
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
                msg.TimeStamp.ToLocalTime().ToString("s")
            );
            indicator.SetPropertyBool("draw-attention", true);
            indicator.UserDisplay += delegate {
                try {
                    MainWindow.PresentWithServerTime();
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

        void OnServerServerDisplay(object sender, ServerDisplayArgs e)
        {
            Trace.Call(sender, e);

            MainWindow.PresentWithTime(e.Timestamp);
        }

#elif MESSAGING_MENU_SHARP
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

            App = new App("smuxi-frontend-gnome.desktop");
            App.ActivateSource += OnAppActivateSource;

            MainWindow.FocusInEvent += OnMainWindowFocusInEvent;
            MainWindow.Notebook.SwitchPage += OnMainWindowNotebookSwitchPage;

            ChatViewManager.ChatAdded   += OnChatViewManagerChatAdded;
            ChatViewManager.ChatRemoved += OnChatViewManagerChatRemoved;

            IsInitialized = true;
        }

        void ShowSource(ChatView chatView, MessageModel msg)
        {
            Trace.Call(chatView, msg);

            string sourceId;
            var time = (Int64) ((msg.TimeStamp - UnixEpoch).TotalMilliseconds * 1000L);
            if (Sources.TryGetValue(chatView, out sourceId)) {
                // update time of existing source
                App.SetSourceTime(sourceId, time);
                return;
            }

            // TODO: TEST ME!
            sourceId = chatView.ID;
            string iconName = null;
            string label = null;
            if (chatView is PersonChatView) {
                iconName = "smuxi-person-chat";
                label = chatView.Name;
            } else if (chatView is GroupChatView) {
                iconName = "smuxi-group-chat";
                var nick = GetNick(msg);
                if (nick == null) {
                    label = chatView.Name;
                } else {
                    label = String.Format("{0} ({1})", chatView.Name, nick);
                }
            }

            var theme = Gtk.IconTheme.Default;
            GLib.Icon icon = null;
            if (Frontend.HasSystemIconTheme &&
                iconName != null && theme.HasIcon(iconName)) {
                icon = new GLib.ThemedIcon(iconName);
            } else if (iconName != null && theme.HasIcon(iconName)) {
                // icon wasn't in the system icon theme
                var iconInfo = theme.LookupIcon(iconName, 256, Gtk.IconLookupFlags.UseBuiltin);
                if (!String.IsNullOrEmpty(iconInfo.Filename) &&
                    File.Exists(iconInfo.Filename)) {
                    icon = new GLib.FileIcon(
                        GLib.FileFactory.NewForPath(iconInfo.Filename)
                    );
                }
            }
            App.AppendSource(sourceId, icon, label);
            App.SetSourceTime(sourceId, time);
            App.DrawAttention(sourceId);
            Sources.Add(chatView, sourceId);
        }

        void DisposeSource(ChatView chatView)
        {
            Trace.Call(chatView);

            try {
                string sourceId;
                if (!Sources.TryGetValue(chatView, out sourceId)) {
                    return;
                }

                App.RemoveSource(sourceId);
            } finally {
                Sources.Remove(chatView);
            }
        }

        void OnAppActivateSource(object sender, ActivateSourceArgs e)
        {
            Trace.Call(sender, e);

            try {
                MainWindow.PresentWithServerTime();

                ChatView chatView = null;
                foreach (var kvp in Sources) {
                    if (kvp.Value != e.SourceId) {
                        continue;
                    }
                    chatView = kvp.Key;
                }
                if (chatView == null) {
                    return;
                }

                MainWindow.Notebook.CurrentChatView = chatView;
                DisposeSource(chatView);
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("OnAppActivateSource(): Exception", ex);
#endif
            }
        }
#endif

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
