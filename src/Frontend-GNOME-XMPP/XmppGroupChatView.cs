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
    [ChatViewInfo(ChatType = ChatType.Group, ProtocolManagerType = typeof(XmppProtocolManager))]
    public class XmppGroupChatView : GroupChatView
    {
        private static readonly string _LibraryTextDomain = "smuxi-frontend-gnome-xmpp";
        private XmppProtocolManager XmppProtocolManager { get; set; }

        public bool IsContactList {
            get {
                return ID == "Contacts";
            }
        }

        public XmppGroupChatView(GroupChatModel chat) : base(chat)
        {
            Trace.Call(chat);
        }

        protected override void OnMessageTextViewMessageAdded(object sender, MessageTextViewMessageAddedEventArgs e)
        {
            if (!IsActive) {
                switch (e.Message.MessageType) {
                    case MessageType.PresenceStateOffline:
                    case MessageType.PresenceStateAway:
                    case MessageType.PresenceStateOnline:
                        HasEvent = true;
                        break;
                }
            }
            base.OnMessageTextViewMessageAdded(sender, e);
        }

        void OnPersonRenameEditingStarted(object o, Gtk.EditingStartedArgs e)
        {
            Trace.Call(o, e);

            // only allow editing once from the context menu
            IdentityNameCellRenderer.Editable = false;

            Gtk.TreeIter iter;
            if (!PersonTreeView.Model.GetIterFromString(out iter, e.Path)) {
                return;
            }
            var person = (PersonModel) PersonTreeView.Model.GetValue(iter, 0);
            var entry = (Gtk.Entry) e.Editable;
            entry.Text = person.IdentityName;
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }

        public override void Sync(int msgCount)
        {
            Trace.Call(msgCount);

            base.Sync(msgCount);

            XmppProtocolManager = (XmppProtocolManager) ProtocolManager;
        }

        void _OnUserListMenuWhoisActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            IList<PersonModel> persons = GetSelectedPersons();
            if (persons == null) {
                return;
            }

            foreach (PersonModel person in persons) {
                var per = person;
            
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        XmppProtocolManager.CommandWhoIs(
                            new CommandModel(
                                Frontend.FrontendManager,
                                ChatModel,
                                per.ID
                            )
                         );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        void OnUserListMenuRemoveActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            IList<PersonModel> persons = GetSelectedPersons();
            if (persons == null) {
                return;
            }

            foreach (PersonModel person in persons) {
                var per = person;

                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        XmppProtocolManager.CommandContact(
                            new CommandModel(
                                Frontend.FrontendManager,
                                ChatModel,
                                "remove " + per.ID
                            )
                        );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        void _OnUserListMenuQueryActivated (object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            IList<PersonModel> persons = GetSelectedPersons();
            if (persons == null) {
                return;
            }

            foreach (PersonModel person in persons) {
                var per = person;
            
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        XmppProtocolManager.CommandMessageQuery(
                            new CommandModel(
                                Frontend.FrontendManager,
                                ChatModel,
                                per.ID
                            )
                         );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        void _OnMenuAddToContactsItemActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            IList<PersonModel> persons = GetSelectedPersons();
            if (persons == null) {
                return;
            }

            foreach (PersonModel person in persons) {
                var per = person;

                // is this a groupchat contact whose real id is unknown
                if (person.ID.StartsWith(ID)) {
                    continue;
                }

                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        XmppProtocolManager.CommandContact(
                            new CommandModel(
                                Frontend.FrontendManager,
                                ChatModel,
                                "add " + per.ID
                            )
                         );
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            }
        }

        void OnPersonRenameEdited(object o, Gtk.EditedArgs e)
        {
            Trace.Call(o, e);

            Gtk.TreeIter iter;
            if (!PersonTreeView.Model.GetIterFromString(out iter, e.Path)) {
                return;
            }
            PersonModel person = (PersonModel) PersonTreeView.Model.GetValue(iter, 0);

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    XmppProtocolManager.CommandContact(
                        new CommandModel(
                            Frontend.FrontendManager,
                            ChatModel,
                            "rename " + person.ID + " " + e.NewText
                        )
                    );
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            });
        }

        protected override void OnPersonMenuShown(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            foreach (var child in PersonMenu.Children) {
                PersonMenu.Remove(child);
            }

            base.OnPersonMenuShown(sender, e);

            // minimum version of any command below
            if (Frontend.EngineVersion < new Version(0, 8, 9)) {
                return;
            }

            if (Frontend.EngineVersion >= new Version(0, 8, 9)) {
                Gtk.ImageMenuItem query_item = new Gtk.ImageMenuItem(_("Query"));
                query_item.Activated += _OnUserListMenuQueryActivated;
                PersonMenu.Append(query_item);
            }

            PersonMenu.Append(new Gtk.SeparatorMenuItem());

            if (Frontend.EngineVersion >= new Version(0, 8, 12)) {
                Gtk.ImageMenuItem whois_item = new Gtk.ImageMenuItem(_("Whois"));
                whois_item.Activated += _OnUserListMenuWhoisActivated;
                PersonMenu.Append(whois_item);
            }

            if (!IsContactList && Frontend.EngineVersion >= new Version(0, 8, 11)) {
                var add_to_contacts_item = new Gtk.ImageMenuItem(_("Add To Contacts"));
                add_to_contacts_item.Activated += _OnMenuAddToContactsItemActivated;
                PersonMenu.Append(add_to_contacts_item);
            }

            if (Frontend.EngineVersion >= new Version(0, 8, 12)) {
                Gtk.MenuItem invite_to_item = new Gtk.MenuItem(_("Invite to"));
                Gtk.Menu invite_to_menu_item = new InviteToMenu(
                    XmppProtocolManager,
                    Frontend.MainWindow.ChatViewManager,
                    GetSelectedPersons()
                );
                invite_to_item.Submenu = invite_to_menu_item;
                PersonMenu.Append(invite_to_item);
            }

            if (IsContactList && Frontend.EngineVersion >= new Version(0, 8, 11)) {
                // cleanup old handlers
                IdentityNameCellRenderer.EditingStarted -= OnPersonRenameEditingStarted;
                IdentityNameCellRenderer.Edited -= OnPersonRenameEdited;

                IdentityNameCellRenderer.EditingStarted += OnPersonRenameEditingStarted;
                IdentityNameCellRenderer.Edited += OnPersonRenameEdited;

                var rename_item = new Gtk.ImageMenuItem(_("Rename"));
                rename_item.Activated += (o, args) => {
                    var paths = PersonTreeView.Selection.GetSelectedRows();
                    if (paths == null || paths.Length == 0) {
                        return;
                    }
                    var path = paths[0];
                    IdentityNameCellRenderer.Editable = true;
                    PersonTreeView.SetCursor(path, IdentityNameColumn, true);
                };
                PersonMenu.Append(rename_item);

                Gtk.ImageMenuItem remove_item = new Gtk.ImageMenuItem(_("Remove"));
                remove_item.Activated += OnUserListMenuRemoveActivated;
                PersonMenu.Append(remove_item);
            }

            PersonMenu.ShowAll();
        }
    }
}

