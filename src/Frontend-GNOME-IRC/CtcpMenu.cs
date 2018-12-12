// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2010, 2013 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class CtcpMenu : Gtk.Menu
    {
        private static readonly string _LibraryTextDomain = "smuxi-frontend-gnome-irc";
        IrcProtocolManager ProtocolManager { get; set; }
        ChatViewManager    ChatViewManager { get; set; }
        IList<PersonModel> Targets { get; set; }
        bool               IsPopulated { get; set; }

        public CtcpMenu(IrcProtocolManager protocolManager,
                        ChatViewManager chatViewManager,
                        PersonModel target) :
                   this(protocolManager,
                        chatViewManager,
                        new [] { target })
        {
        }

        public CtcpMenu(IrcProtocolManager protocolManager,
                        ChatViewManager chatViewManager,
                        IList<PersonModel> targets)
        {
            if (protocolManager == null) {
                throw new ArgumentNullException("protocolManager");
            }
            if (chatViewManager == null) {
                throw new ArgumentNullException("chatViewManager");
            }
            if (targets == null) {
                throw new ArgumentNullException("targets");
            }

            ProtocolManager = protocolManager;
            ChatViewManager = chatViewManager;
            Targets = targets;
        }

        protected override void OnShown()
        {
            Trace.Call();

            if (!IsPopulated) {
                IsPopulated = true;

                Gtk.MenuItem item;
                item = new Gtk.MenuItem(_("Ping"));
                item.Activated += OnPingItemActivated;
                item.Show();
                Append(item);

                item = new Gtk.MenuItem(_("Version"));
                item.Activated += OnVersionItemActivated;
                item.Show();
                Append(item);

                item = new Gtk.MenuItem(_("Time"));
                item.Activated += OnTimeItemActivated;
                item.Show();
                Append(item);

                item = new Gtk.MenuItem(_("Finger"));
                item.Activated += OnFingerItemActivated;
                item.Show();
                Append(item);

                item = new Gtk.MenuItem(_("Userinfo"));
                item.Activated += OnUserinfoItemActivated;
                item.Show();
                Append(item);
            }

            base.OnShown();
        }

        void OnPingItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            var sourceChatModel = ChatViewManager.ActiveChat.ChatModel;
            foreach (PersonModel target in Targets) {
                var targetId = target.ID;
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        ProtocolManager.CommandPing(
                            new CommandModel(
                                Frontend.FrontendManager,
                                sourceChatModel,
                                targetId
                            )
                        );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        void OnVersionItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            var sourceChatModel = ChatViewManager.ActiveChat.ChatModel;
            foreach (PersonModel target in Targets) {
                var targetId = target.ID;
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        ProtocolManager.CommandVersion(
                            new CommandModel(
                                Frontend.FrontendManager,
                                sourceChatModel,
                                targetId
                            )
                        );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        void OnTimeItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            var sourceChatModel = ChatViewManager.ActiveChat.ChatModel;
            foreach (PersonModel target in Targets) {
                var targetId = target.ID;
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        ProtocolManager.CommandTime(
                            new CommandModel(
                                Frontend.FrontendManager,
                                sourceChatModel,
                                targetId
                            )
                        );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        void OnFingerItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            var sourceChatModel = ChatViewManager.ActiveChat.ChatModel;
            foreach (PersonModel target in Targets) {
                var targetId = target.ID;
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        ProtocolManager.CommandFinger(
                            new CommandModel(
                                Frontend.FrontendManager,
                                sourceChatModel,
                                targetId
                            )
                        );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        void OnUserinfoItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            var sourceChatModel = ChatViewManager.ActiveChat.ChatModel;
            foreach (PersonModel target in Targets) {
                var targetId = target.ID;
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        ProtocolManager.CommandCtcp(
                            new CommandModel(
                                Frontend.FrontendManager,
                                sourceChatModel,
                                String.Format("{0} {1}", targetId, "USERINFO")
                            )
                        );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
