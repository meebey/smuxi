/*
 * $Id: MainWindow.cs 273 2008-07-12 17:00:51Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/MainWindow.cs $
 * $Rev: 273 $
 * $Author: meebey $
 * $Date: 2008-07-12 19:00:51 +0200 (Sat, 12 Jul 2008) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using Mono.Unix;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public partial class FindGroupChatDialog : Gtk.Dialog
    {
        private IProtocolManager f_ProtocolManager;
        private Gtk.ListStore    f_ListStore;
        private GroupChatModel   f_GroupChatModel;
        
        public GroupChatModel GroupChat {
            get {
                return f_GroupChatModel;
            }
        }
        
        public FindGroupChatDialog(Gtk.Window parent, IProtocolManager protocolManager) :
                              base(null, parent, Gtk.DialogFlags.DestroyWithParent)
        {
            Build();
            
            f_ProtocolManager = protocolManager;
            
            int columnID = 0;
            Gtk.TreeViewColumn column;
            
            columnID++;
            column = f_TreeView.AppendColumn(_("#"), new Gtk.CellRendererText(), "text", columnID);
            column.SortColumnId = columnID;
            
            columnID++;
            column = f_TreeView.AppendColumn(_("Name"), new Gtk.CellRendererText(), "text", columnID);
            column.SortColumnId = columnID;
            column.Resizable = true;
            
            columnID++;
            column = f_TreeView.AppendColumn(_("Topic"), new Gtk.CellRendererText(), "text", columnID);
            column.SortColumnId = columnID;
            column.Sizing = Gtk.TreeViewColumnSizing.Fixed;
            column.Resizable = true;
            
            f_ListStore = new Gtk.ListStore(
                typeof(GroupChatModel),
                typeof(int), // person count
                typeof(string), // name
                typeof(string) // topic
            );
            f_TreeView.RowActivated += OnTreeViewRowActivated;
            f_TreeView.Selection.Changed += OnTreeViewSelectionChanged;
            f_TreeView.Model = f_ListStore;
            f_TreeView.SearchColumn = 2; // name
        }
        
        protected virtual void OnFindButtonClicked(object sender, System.EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                string nameFilter = f_NameEntry.Text.Trim();
                if (String.IsNullOrEmpty(nameFilter)) {
                    Gtk.MessageDialog md = new Gtk.MessageDialog(
                        this,
                        Gtk.DialogFlags.Modal,
                        Gtk.MessageType.Warning,
                        Gtk.ButtonsType.YesNo,
                        _("Searching for group chats without a filter is not " + 
                          "recommended.  This may take a while, or may not " +
                          "work at all.\n" +
                          "Do you wish to continue?")
                    );
                    int result = md.Run();
                    md.Destroy();
                    if (result != (int) Gtk.ResponseType.Yes) {
                        return;
                    }
                }
                
                f_ListStore.Clear();
                GroupChatModel filter =  new GroupChatModel(null, nameFilter, null);
                // TODO: use extra thread and show progress dialog
                IList<GroupChatModel> chats = f_ProtocolManager.FindGroupChats(filter);
                foreach (GroupChatModel chat in chats) {
                    f_ListStore.AppendValues(chat, chat.PersonCount, chat.Name, chat.Topic);
                }
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual GroupChatModel GetCurrentGroupChat()
        {
            Trace.Call();
            
            Gtk.TreeIter iter;
            if (!f_TreeView.Selection.GetSelected(out iter)) {
                return null;
            }
            return (GroupChatModel) f_ListStore.GetValue(iter, 0);
        }
        
        protected virtual void OnTreeViewRowActivated(object sender, Gtk.RowActivatedArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                GroupChatModel chat = GetCurrentGroupChat();
                if (chat == null) {
                    return;
                }
                
                Respond(Gtk.ResponseType.Ok);
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnTreeViewSelectionChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                f_OKButton.Sensitive = GetCurrentGroupChat() != null;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        protected override void OnResponse(Gtk.ResponseType responseType)
        {
            Trace.Call(responseType);
            
            if (responseType == Gtk.ResponseType.Ok) {
                f_GroupChatModel = GetCurrentGroupChat();
            }
            
            base.OnResponse(responseType);
        }

        protected virtual void OnNameEntryActivated(object sender, System.EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                f_FindButton.Click();
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
