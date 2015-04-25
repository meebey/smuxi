// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Oliver Schneider <smuxi@oli-obk.de>
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
using System.Threading;
using System.Collections.Generic;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Person, ProtocolManagerType = typeof(XmppProtocolManager))]
    public class XmppPersonChatView : PersonChatView
    {
        private static readonly string _LibraryTextDomain = "smuxi-frontend-gnome-xmpp";
        private XmppProtocolManager XmppProtocolManager { get; set; }

        // for finding the position of the dots and removing them
        Gtk.TextMark ChatStateStartPosition { get; set; }
        bool ChatStatePositionValid { get; set; }

        // for drawing the dots
        int NumberOfTypingDots { get; set; }
        bool IsDisposed { get; set; }
        bool IsComposing { get; set; }
        bool ChatStateTimeoutRunning { get; set; }
        MessageModel TypingDots { get; set; }

        // for remembering the presence state
        MessageModel LastPresenceMessage { get; set; }

        public XmppPersonChatView(PersonChatModel personChat) : base(personChat)
        {
            Trace.Call(personChat);

            OutputMessageTextView.PopulatePopup += _OnOutputMessageTextViewPopulatePopup;
            ChatStateStartPosition = new Gtk.TextMark("ChatStateStartPosition", true);
            IsDisposed = false;
        }

        void DeleteOldChatState()
        {
            if (!ChatStatePositionValid) {
                return;
            }
            var buffer = OutputMessageTextView.Buffer;
            var start = buffer.GetIterAtMark(ChatStateStartPosition);
            var end = buffer.EndIter;
            buffer.Delete(ref start, ref end);
            buffer.DeleteMark(ChatStateStartPosition);
            if (buffer.EndIter.Offset < OutputMessageTextView.MarkerlineBufferPosition) {
                // in the rare case that the markeline is below the dots, move it to the correct position
                OutputMessageTextView.UpdateMarkerline();
            }
            ChatStatePositionValid = false;
        }

        void UpdateChatState()
        {
            DeleteOldChatState();
            if (LastPresenceMessage == null && TypingDots == null) {
                // nothing to display
                return;
            }
            var buffer = OutputMessageTextView.Buffer;
            buffer.AddMark(ChatStateStartPosition, buffer.EndIter);

            if (TypingDots != null) {
                OutputMessageTextView.AddMessage(TypingDots, true, false);
            }
            if (LastPresenceMessage != null) {
                OutputMessageTextView.AddMessage(LastPresenceMessage, false);
            }

            ChatStatePositionValid = true;
        }

        void SetPresenceStateText(MessageModel msg)
        {
            LastPresenceMessage = msg;
            UpdateChatState();
        }

        void ClearPresenceStateText()
        {
            if (LastPresenceMessage == null) {
                // nothing to do, probably received duplicate available messages
                return;
            }
            LastPresenceMessage = null;
            UpdateChatState();
        }

        bool TypingDotsCallback()
        {
            if (IsDisposed) {
                return false;
            }
            if (IsComposing) {
                NumberOfTypingDots++;
                if (NumberOfTypingDots == 4) {
                    NumberOfTypingDots = 0;
                }
            } else {
                NumberOfTypingDots--;
                if (NumberOfTypingDots <= 0) {
                    // done
                    TypingDots = null;
                    UpdateChatState();
                    ChatStateTimeoutRunning = false;
                    return false;
                }
            }
            var builder = new MessageBuilder();
            builder.AppendText(new string('.', NumberOfTypingDots));
            TypingDots = builder.ToMessage();
            UpdateChatState();
            GLib.Timeout.Add(300, TypingDotsCallback);
            return false;
        }

        void StartMovingDots()
        {
            IsComposing = true;
            if (!ChatStateTimeoutRunning) {
                ChatStateTimeoutRunning = true;
                NumberOfTypingDots = 0;
                TypingDotsCallback();
            }
        }

        void StopMovingDots()
        {
            if (!ChatStateTimeoutRunning) {
                // already done
                return;
            }
            IsComposing = false;
        }

        void AbortMovingDots()
        {
            TypingDots = null;
            UpdateChatState();
            if (!ChatStateTimeoutRunning) {
                // already done
                return;
            }
            // will be removed on next call to UpdateChatState()
            NumberOfTypingDots = 0;
            IsComposing = false;
        }

        public override void AddMessage(MessageModel msg)
        {
            Trace.Call(msg);
            switch (msg.MessageType) {
                case MessageType.ChatStateComposing:
                    StartMovingDots();
                    break;
                case MessageType.ChatStatePaused:
                    StopMovingDots();
                    break;
                case MessageType.ChatStateReset:
                    AbortMovingDots();
                    break;
                case MessageType.PresenceStateOnline:
                    ClearPresenceStateText();
                    break;
                case MessageType.PresenceStateOffline:
                case MessageType.PresenceStateAway:
                    SetPresenceStateText(msg);
                    break;
                default:
                    AbortMovingDots();
                    DeleteOldChatState();
                    base.AddMessage(msg);
                    UpdateChatState();
                    break;
            }
        }

        private void _OnOutputMessageTextViewPopulatePopup (object o, Gtk.PopulatePopupArgs args)
        {
            if (OutputMessageTextView.IsAtUrlTag) {
                return;
            }

            Gtk.Menu popup = args.Menu;

            // minimum version of any command below
            if (Frontend.EngineVersion < new Version(0, 8, 11)) {
                return;
            }

            popup.Append(new Gtk.SeparatorMenuItem());

            if (Frontend.EngineVersion >= new Version(0, 8, 12)) {
                Gtk.ImageMenuItem whois_item = new Gtk.ImageMenuItem(_("Whois"));
                whois_item.Activated += _OnMenuWhoisItemActivated;
                popup.Append(whois_item);
            }

            if (Frontend.EngineVersion >= new Version(0, 8, 11)) {
                Gtk.ImageMenuItem AddToContacts_item = new Gtk.ImageMenuItem(_("Add To Contacts"));
                AddToContacts_item.Activated += _OnMenuAddToContactsItemActivated;
                popup.Append(AddToContacts_item);
            }

            if (Frontend.EngineVersion >= new Version(0, 8, 12)) {
                Gtk.ImageMenuItem invite_to_item = new Gtk.ImageMenuItem(_("Invite to"));
                Gtk.Menu invite_to_menu_item = new InviteToMenu(XmppProtocolManager,
                                                                Frontend.MainWindow.ChatViewManager,
                                                                PersonModel);
                invite_to_item.Submenu = invite_to_menu_item;
                popup.Append(invite_to_item);
            }

            popup.ShowAll();
        }

        void _OnMenuWhoisItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    XmppProtocolManager.CommandWhoIs(
                        new CommandModel(
                            Frontend.FrontendManager,
                            ChatModel,
                            PersonModel.ID
                        )
                     );
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            });
        }

        void _OnMenuAddToContactsItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    XmppProtocolManager.CommandContact(
                        new CommandModel(
                            Frontend.FrontendManager,
                            ChatModel,
                            "add " + PersonModel.ID
                        )
                     );
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            });
        }
        
        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }

        public override void Dispose()
        {
            Trace.Call();
            IsDisposed = true;
            base.Dispose();
        }

        public override void Sync(int msgCount)
        {
            Trace.Call(msgCount);

            base.Sync(msgCount);

            XmppProtocolManager = (XmppProtocolManager) ProtocolManager;
        }
    }
}

