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

#if NOTIFY_SHARP
using System;
using System.IO;
using System.Collections.Generic;
using Notifications;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class NotifyManager
    {
#if LOG4NET
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static Gdk.Pixbuf PersonChatIconPixbuf { get; set; }
        private static string     PersonChatIconName { get; set; }
        private static Gdk.Pixbuf GroupChatIconPixbuf { get; set; }
        private static string     GroupChatIconName { get; set; }
        private static List<string> Capabilites { get; set; }
        private static string SoundFile { get; set; }
        private static Version SpecificationVersion { get; set; }
        Dictionary<ChatView, Notification> Notifications { get; set; }
        MainWindow MainWindow { get; set; }
        ChatViewManager ChatViewManager { get; set; }
        Dictionary<ChatView, MessageTextViewMessageHighlightedEventHandler> HighlightEventHandlers { get; set; }
        bool IsInitialized { get; set; }
        bool IsEnabled { get; set; }

        static NotifyManager()
        {
            // image size >= 128 pixels as per notify-osd guidelines:
            // https://wiki.ubuntu.com/NotificationDevelopmentGuidelines
            PersonChatIconPixbuf = Frontend.LoadIcon(
                "smuxi-person-chat", 256, "person-chat_256x256.png"
            );
            GroupChatIconPixbuf = Frontend.LoadIcon(
                "smuxi-group-chat", 256, "group-chat_256x256.png"
            );

            var partialPath = "share";
            partialPath = Path.Combine(partialPath, "sounds");
            partialPath = Path.Combine(partialPath, "freedesktop");
            partialPath = Path.Combine(partialPath, "stereo");
            partialPath = Path.Combine(partialPath, "message-new-instant.oga");
            var soundFile = Path.Combine(Defines.InstallPrefix, partialPath);
            var sysSoundFile = Path.Combine("/usr", partialPath);
            if (File.Exists(soundFile)) {
                SoundFile = soundFile;
            } else if (File.Exists(sysSoundFile)) {
                // fallback to system-wide install
                SoundFile = sysSoundFile;
            }
        }

        public NotifyManager(MainWindow mainWindow,
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
            Notifications = new Dictionary<ChatView, Notification>();
            HighlightEventHandlers = new Dictionary
                <ChatView,
                 MessageTextViewMessageHighlightedEventHandler>();

            try {
                Init();
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("NotifyManager(): initialization failed: ", ex);
#endif
            }
        }
        
        public void Dispose()
        {
            Trace.Call();
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

            IsEnabled = (bool) userConfig["Interface/Notification/PopupsEnabled"];
        }

        void Init()
        {
            Trace.Call();

            Capabilites = new List<string>(Global.Capabilities);
            var version = Global.ServerInformation.SpecVersion;
            try {
                SpecificationVersion = new Version(version);
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("Init(): couldn't parse specification version: " +
                             "'" + version + "'", ex);
#endif
            }

#if LOG4NET
            Logger.Debug(
                String.Format(
                    "Init(): Name: '{0}' Vendor: '{1}' Version: '{2}' " +
                        "SpecVersion: '{3}' Capabilities: '{4}'",
                    Global.ServerInformation.Name,
                    Global.ServerInformation.Vendor,
                    Global.ServerInformation.Version,
                    Global.ServerInformation.SpecVersion,
                    String.Join(", ", Global.Capabilities)
                )
            );
#endif

            // HACK: a bug in notification-daemon-xfce skips the reason field
            // in NotificationClosed which leads to an exception:
            // System.IndexOutOfRangeException: Array index is out of range.
            //  at NDesk.DBus.MessageReader.MarshalUInt (byte*) <IL 0x00024, 0x00030>
            // see: http://bugzilla.xfce.org/show_bug.cgi?id=5339
            if (Global.ServerInformation.Name == "Notification Daemon" &&
                Global.ServerInformation.Vendor == "Galago Project") {
#if LOG4NET
                Logger.Warn("Init(): detected buggy Xfce notification daemon, " +
                            "suppressing notifications...");
#endif
                return;
            }

            MainWindow.FocusInEvent += OnMainWindowFocusInEvent;
            MainWindow.Notebook.SwitchPage += OnMainWindowNotebookSwitchPage;

            ChatViewManager.ChatAdded   += OnChatViewManagerChatAdded;
            ChatViewManager.ChatRemoved += OnChatViewManagerChatRemoved;

            IsInitialized = true;
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
        }

        void OnChatViewMessageHighlighted(object sender,
                                          MessageTextViewMessageHighlightedEventArgs e,
                                          ChatView chatView)
        {
            Trace.Call(sender, e, chatView);

            if (!IsEnabled ||
                e.Message.TimeStamp <= chatView.SyncedLastSeenHighlight ||
                MainWindow.HasToplevelFocus) {
                return;
            }

            try {
                ShowNotification(chatView, e.Message);
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("OnChatViewMessageHighlighted(): " +
                             "ShowNotification() threw exception", ex);
#endif
            }
        }

        void ShowNotification(ChatView chatView, MessageModel msg)
        {
            Notification notification;
            if (!Capabilites.Contains("append") &&
                Notifications.TryGetValue(chatView, out notification)) {
                // no support for append, update the existing notification
                notification.Body = GLib.Markup.EscapeText(
                    msg.ToString()
                );
                return;
            }

            notification = new Notification() {
                Summary = chatView.Name,
                Category = "im.received"
            };
            if (Capabilites.Contains("body")) {
                // notify-osd doesn't like unknown tags when appending
                notification.Body = GLib.Markup.EscapeText(
                    msg.ToString()
                );
            }
            //notification.IconName = "notification-message-im";
            if (Capabilites.Contains("icon-static")) {
                if (chatView is PersonChatView) {
                    notification.Icon = PersonChatIconPixbuf;
                }
                if (chatView is GroupChatView) {
                    notification.Icon = GroupChatIconPixbuf;
                }
            }
            if (Capabilites.Contains("actions")) {
                notification.AddAction("show", _("Show"), delegate {
                    try {
                        MainWindow.Present();
                        MainWindow.Notebook.CurrentChatView = chatView;
                        notification.Close();
                    } catch (Exception ex) {
#if LOG4NET
                        Logger.Error("OnChatViewMessageHighlighted() " +
                                     "notification.Show threw exception", ex);
#endif
                    }
                });
            }
            if (Capabilites.Contains("append")) {
                notification.AddHint("append", String.Empty);
            }
            if (Capabilites.Contains("sound")) {
                // DNS 0.9 only supports sound-file which is a file path
                // http://www.galago-project.org/specs/notification/0.9/x344.html
                // DNS 1.1 supports sound-name which is an id, see:
                // http://people.canonical.com/~agateau/notifications-1.1/spec/ar01s08.html
                // http://0pointer.de/public/sound-naming-spec.html
                // LAMESPEC: We can't tell which of those are actually
                // supported by this version as hint are totally optional :/
                // HACK: always pass both hints when possible
                notification.AddHint("sound-name", "message-new-instant");
                if (SoundFile != null) {
                    notification.AddHint("sound-file", SoundFile);
                }
            }
            notification.Closed += delegate {
                try {
#if LOG4NET
                    Logger.Debug("OnChatViewMessageHighlighted(): received " +
                                 "notification.Closed signal for: " +
                                 chatView.Name);
#endif
                    Notifications.Remove(chatView);
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("OnChatViewMessageHighlighted(): " +
                                 "Exception in notification.Closed handler",
                                 ex);
#endif
                }
            };
            notification.Show();

            if (!Notifications.ContainsKey(chatView)) {
                Notifications.Add(chatView, notification);
            }
        }

        void OnMainWindowFocusInEvent(object sender, Gtk.FocusInEventArgs e)
        {
            Trace.Call(sender, e);

            if (MainWindow.Notebook.IsBrowseModeEnabled) {
                return;
            }

            var currentChatView = MainWindow.Notebook.CurrentChatView;
            if (currentChatView == null) {
                return;
            }
            DisposeNotification(currentChatView);
        }

        void OnMainWindowNotebookSwitchPage(object sender, Gtk.SwitchPageArgs e)
        {
            Trace.Call(sender, e);

            if (MainWindow.Notebook.IsBrowseModeEnabled) {
                return;
            }

            var currentChatView = MainWindow.Notebook.CurrentChatView;
            if (currentChatView == null) {
                return;
            }
            DisposeNotification(currentChatView);
        }

        void DisposeNotification(ChatView chatView)
        {
            Notification notification;
            if (!Notifications.TryGetValue(chatView, out notification)) {
                return;
            }
#if LOG4NET
            Logger.Debug("DisposeNotification(): disposing notification for: " +
                         chatView.Name);
#endif

            try {
                // don't try to close already closed notifications (timeout)
                if (notification.Id == 0) {
#if LOG4NET
                    Logger.Debug("DisposeNotification(): notification already " +
                                 "closed for: " + chatView.Name);
#endif
                    return;
                }

                notification.Close();
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("DisposeNotification(): " +
                             "notification.Close() thew exception", ex);
#endif
            } finally {
                Notifications.Remove(chatView);
            }
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
#endif
