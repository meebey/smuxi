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

#if INDICATE_SHARP
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Indicate;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class IndicateManager : IDisposable
    {
#if LOG4NET
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static string PersonChatIconBase64 { get; set; }
        private static string GroupChatIconBase64  { get; set; }
        Server Server { get; set; }
        MainWindow MainWindow { get; set; }
        ChatViewManager ChatViewManager { get; set; }
        Dictionary<ChatView, Indicator> Indicators { get; set; }
        Dictionary<ChatView, MessageTextViewMessageHighlightedEventHandler> HighlightEventHandlers { get; set; }

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

            Init();
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

            if ((bool) userConfig["Interface/Notification/MessagingMenuEnabled"]) {
                Server.Show();
            } else {
                Server.Hide();
            }
        }

        void Init()
        {
            Trace.Call();

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

            var desktopFile = Path.Combine(Defines.InstallPrefix, "share");
            desktopFile = Path.Combine(desktopFile, "applications");
            desktopFile = Path.Combine(desktopFile, "smuxi-frontend-gnome.desktop");

            Server.SetType("message.im");
            Server.DesktopFile(desktopFile);
            Server.ServerDisplay += OnServerServerDisplay;

            MainWindow.FocusInEvent += OnMainWindowFocusInEvent;
            MainWindow.Notebook.SwitchPage += OnMainWindowNotebookSwitchPage;

            ChatViewManager.ChatAdded   += OnChatViewManagerChatAdded;
            ChatViewManager.ChatRemoved += OnChatViewManagerChatRemoved;
        }

        void OnMainWindowFocusInEvent(object sender, Gtk.FocusInEventArgs e)
        {
            Trace.Call(sender, e);

            var currentChatView = MainWindow.Notebook.CurrentChatView;
            if (currentChatView == null) {
                return;
            }
            DisposeIndicator(currentChatView);
        }

        void OnMainWindowNotebookSwitchPage(object sender, Gtk.SwitchPageArgs e)
        {
            Trace.Call(sender, e);

            var currentChatView = MainWindow.Notebook.CurrentChatView;
            if (currentChatView == null) {
                return;
            }
            DisposeIndicator(currentChatView);
        }

        void OnServerServerDisplay(object sender, ServerDisplayArgs e)
        {
            Trace.Call(sender, e);

            MainWindow.PresentWithTime(
                (uint) (DateTime.UtcNow - UnixEpoch).TotalSeconds
            );
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

            if (MainWindow.HasToplevelFocus) {
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
                    MainWindow.PresentWithTime(
                        (uint) (DateTime.UtcNow - UnixEpoch).TotalSeconds
                    );
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
            Trace.Call(chatView);

            Indicator indicator;
            if (!Indicators.TryGetValue(chatView, out indicator)) {
                return;
            }

            indicator.Hide();
            Indicators.Remove(chatView);
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
