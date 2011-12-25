/*
 * $Id: MainWindow.cs 273 2008-07-12 17:00:51Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/MainWindow.cs $
 * $Rev: 273 $
 * $Author: meebey $
 * $Date: 2008-07-12 19:00:51 +0200 (Sat, 12 Jul 2008) $
 *
 * Smuxi - Smart MUltipleXed Irc
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
using System.Threading;
using System.Collections.Generic;
using Mono.Unix;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public partial class FindGroupChatDialog : Gtk.Dialog
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private IProtocolManager f_ProtocolManager;
        private Gtk.ListStore    f_ListStore;
        private GroupChatModel   f_GroupChatModel;
        private Thread           f_FindThread;
        
        public GroupChatModel GroupChat {
            get {
                return f_GroupChatModel;
            }
        }

        public Gtk.Entry NameEntry {
            get {
                return f_NameEntry;
            }
        }

        public Gtk.Button FindButton {
            get {
                return f_FindButton;
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
            column = f_TreeView.AppendColumn("#", new Gtk.CellRendererText(), "text", columnID);
            column.SortColumnId = columnID;
            
            columnID++;
            column = f_TreeView.AppendColumn(_("Name"), new Gtk.CellRendererText(), "text", columnID);
            column.SortColumnId = columnID;
            column.Resizable = true;
            
            columnID++;
            column = f_TreeView.AppendColumn(_("Topic"), new Gtk.CellRendererText(), "markup", columnID);
            column.SortColumnId = columnID;
            column.Resizable = true;

            f_ListStore = new Gtk.ListStore(
                typeof(GroupChatModel),
                typeof(int), // person count
                typeof(string), // name
                typeof(string) // topic pango markup
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
                if (!(Frontend.EngineVersion >= new Version("0.8.1")) &&
                    String.IsNullOrEmpty(nameFilter)) {
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
                CancelFindThread();
                
                GroupChatModel filter =  new GroupChatModel(null, nameFilter, null);
                f_FindThread = new Thread(new ThreadStart(delegate {
                    try {
                        Gtk.Application.Invoke(delegate {
                            GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
                        });
                        
                        IList<GroupChatModel> chats = f_ProtocolManager.FindGroupChats(filter);
                        
                        Gtk.Application.Invoke(delegate {
                            Gdk.Color bgColor = f_TreeView.Style.Background(Gtk.StateType.Normal);
                            foreach (GroupChatModel chat in chats) {
                                f_ListStore.AppendValues(
                                    chat,
                                    chat.PersonCount,
                                    chat.Name,
                                    PangoTools.ToMarkup(chat.Topic, bgColor)
                                );
                            }
                        });
                    } catch (ThreadAbortException ex) {
#if LOG4NET
                        f_Logger.Debug("FindThread aborted");
#endif
                        Thread.ResetAbort();
                    } catch (Exception ex) {
                        Frontend.ShowError(this, _("Error while fetching the list of group chats from the server."), ex);
                    } finally {
                        Gtk.Application.Invoke(delegate {
                            // if the dialog is gone the GdkWindow might be destroyed already
                            if (GdkWindow != null) {
                                GdkWindow.Cursor = null;
                            }
                        });
                    }
                }));
                f_FindThread.IsBackground = true;
                f_FindThread.Start();
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        private void CancelFindThread()
        {
            if (f_FindThread != null && f_FindThread.IsAlive) {
                try {
#if LOG4NET
                    f_Logger.Debug("Aborting FindThread...");
#endif
                    f_FindThread.Abort();
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error(ex);
#endif
                }
                f_FindThread = null;
                GdkWindow.Cursor = null;
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
            
            switch (responseType) {
                case Gtk.ResponseType.Ok:
                    f_GroupChatModel = GetCurrentGroupChat();
                    break;
                case Gtk.ResponseType.Cancel:
                    CancelFindThread();
                    break;
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
