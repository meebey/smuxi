/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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
using System.ComponentModel;
using Meebey.Smuxi.Engine;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class GtkGnomeUI : PermanentRemoteObject, IFrontendUI
    {
        private int _ThreadId;
        private int _Version = 0;
        
        public int Version
        {
            get {
                return _Version;
            }
        }
        
        public bool InvokeRequired
        {
            get {
                if (AppDomain.GetCurrentThreadId() == _ThreadId) {
                    return true;
                } else {
                    return false;
                }
            }
        }
        
        public GtkGnomeUI()
        {
            _ThreadId = AppDomain.GetCurrentThreadId();
        }
        
        public void AddPage(Engine.Page epage)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("AddPage()");
#endif
            Page newpage = null;
            switch (epage.PageType) {
                case PageType.Server:
                    newpage = new ServerPage(epage);
                    break;
                case PageType.Channel:
                    newpage = new ChannelPage(epage);
                    break;
                case PageType.Query:
                    newpage = new QueryPage(epage);
                    break;
                default:
#if LOG4NET
                    Logger.UI.Fatal("AddPage() Unknown PageType: "+epage.PageType);
#endif
                    Gdk.Threads.Leave();
                    throw new ApplicationException("Unknown PageType: "+epage.PageType);
            }
            Frontend.MainWindow.Notebook.AppendPage(newpage, newpage.LabelEventBox);
            newpage.ShowAll();
            
            Gdk.Threads.Leave();
        }
        
        public void AddTextToPage(Engine.Page epage, string text)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("AddTextToPage() "+
                " epage: "+(epage != null ? epage.GetType().ToString() : "(null)")+
                " epage.Name: "+(epage.Name != null ? epage.Name : "(null)")+
                " text: '"+(text != null ? text : "(null)")+"'");
#endif
            Page page = Frontend.MainWindow.Notebook.GetPage(epage);
            Gtk.TextIter iter = page.OutputTextBuffer.EndIter;
#if GTK_SHARP_1
            page.OutputTextBuffer.Insert(iter, text+"\n");
#elif GTK_SHARP_2
            page.OutputTextBuffer.Insert(ref iter, text+"\n");
#endif
            
            if (Frontend.FrontendManager.CurrentPage != epage) {
                page.Label.Markup = "<span foreground=\"blue\">"+page.Name+"</span>";
            }
            
            Gdk.Threads.Leave();
        }
        
        public void RemovePage(Engine.Page epage)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("RemovePage()");
#endif
            Page page = Frontend.MainWindow.Notebook.GetPage(epage);
            Frontend.MainWindow.Notebook.RemovePage(
                Frontend.MainWindow.Notebook.PageNum(page)
            );
            
            Gdk.Threads.Leave();
        }
        
        public void SyncPage(Engine.Page epage)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("SyncPage()");
#endif

            Page page = Frontend.MainWindow.Notebook.GetPage(epage);
            if (epage.PageType == PageType.Channel) {
               ChannelPage cpage = (ChannelPage)page;
               Engine.ChannelPage ecpage = (Engine.ChannelPage)epage;
               
               // sync userlist
               Gtk.TreeView tv  = cpage.UserListTreeView;
               if (tv != null) {
                   //Gtk.ListStore ls = (Gtk.ListStore)tv.Model;
                   // cleanup, be sure the list is empty
                   Gtk.ListStore ls = new Gtk.ListStore(typeof(string), typeof(string));
                   // detach the model (less CPU load)
                   tv.Model = new Gtk.ListStore(typeof(string), typeof(string));
                   foreach (User user in ecpage.Users.Values) {
                       if (user is Engine.IrcChannelUser) { 
                           IrcChannelUser icuser = (IrcChannelUser)user;
                           if (icuser.IsOp) {
                               ls.AppendValues("@", icuser.Nickname);
                           } else if (icuser.IsVoice) {
                               ls.AppendValues("+", icuser.Nickname);
                           } else {
                               ls.AppendValues(String.Empty, icuser.Nickname);
                           }
                       }
                   }
                   // attach the model again
                   tv.Model = ls;
               
                   tv.GetColumn(1).Title = "Users ("+ls.IterNChildren()+")";
               }
               
               // sync topic
               if ((cpage.TopicEntry != null) &&
                   (ecpage.Topic != null)) {
                   cpage.TopicEntry.Text = ecpage.Topic;
               }
            }
            
            // sync messages
            // cleanup, be sure the output is empty
            // Gtk# bug?
            page.OutputTextBuffer.Clear();
            //page.OutputTextBuffer.Text = null;
            if (epage.Buffer.Count > 0) {
                foreach (string line in epage.Buffer) {
                    Gtk.TextIter iter = page.OutputTextBuffer.EndIter;
#if GTK_SHARP_1
                    page.OutputTextBuffer.Insert(iter, line+"\n");
#elif GTK_SHARP_2
                    page.OutputTextBuffer.Insert(ref iter, line+"\n");
#endif
                    
                    if (Frontend.FrontendManager.CurrentPage != epage) {
                        page.Label.Markup = "<span foreground=\"blue\">"+page.Name+"</span>";
                    }
                }
            }

            Gdk.Threads.Leave();
        }
        
        public void AddUserToChannel(Engine.ChannelPage ecpage, User user)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("AddUserToChannel()");
#endif
            ChannelPage cpage = (ChannelPage)Frontend.MainWindow.Notebook.GetPage(ecpage);
            Gtk.TreeView  treeview  = cpage.UserListTreeView;
            if (treeview == null) {
                // no treeview, nothing todo
                Gdk.Threads.Leave();
                return;
            }
            
            IrcChannelUser icuser = (IrcChannelUser)user;
            Gtk.ListStore liststore = (Gtk.ListStore)treeview.Model;
            if (icuser.IsOp) {
                liststore.AppendValues("@", icuser.Nickname);
            } else if (icuser.IsVoice) {
                liststore.AppendValues("+", icuser.Nickname);
            } else {
                liststore.AppendValues(String.Empty, icuser.Nickname);
            }

            treeview.GetColumn(1).Title = "Users ("+liststore.IterNChildren()+")";
            
            Gdk.Threads.Leave();
        }
        
        public void UpdateUserInChannel(Engine.ChannelPage ecpage, User olduser, User newuser)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("UpdateUserInChannel()");
#endif
            ChannelPage cpage = (ChannelPage)Frontend.MainWindow.Notebook.GetPage(ecpage);
            Gtk.TreeView  treeview  = cpage.UserListTreeView;
            if (treeview == null) {
                // no treeview, nothing todo
                Gdk.Threads.Leave();
                return;
            } 
            
            Gtk.ListStore liststore = (Gtk.ListStore)treeview.Model;
            Gtk.TreeIter iter;
            liststore.GetIterFirst(out iter);
            do {
                if ((string)liststore.GetValue(iter, 1) == olduser.Nickname) {
                    IrcChannelUser newcuser = (IrcChannelUser)newuser;
                    string mode;
                    if (newcuser.IsOp) {
                        mode = "@";
                    } else if (newcuser.IsVoice) {
                        mode = "+";
                    } else {
                        mode = String.Empty;
                    }
                    
                    // update the mode of the current row
                    liststore.SetValue(iter, 0, mode);
                    // update the nickname of the current row
                    liststore.SetValue(iter, 1, newuser.Nickname);
                    break;
                }
            } while (liststore.IterNext(ref iter));
            
            Gdk.Threads.Leave();
        }
        
        public void UpdateTopicInChannel(Engine.ChannelPage ecpage, string topic)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("UpdateTopicInChannel()");
#endif
            ChannelPage cpage = (ChannelPage)Frontend.MainWindow.Notebook.GetPage(ecpage);
            if (cpage.TopicEntry != null) {
                cpage.TopicEntry.Text = topic;
            }
            
            Gdk.Threads.Leave();
        }
        
        public void RemoveUserFromChannel(Engine.ChannelPage ecpage, User user)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("RemoveUserFromChannel()");
#endif
            ChannelPage cpage = (ChannelPage)Frontend.MainWindow.Notebook.GetPage(ecpage);
            Gtk.TreeView  treeview  = cpage.UserListTreeView;
            if (treeview == null) {
                // no treeview, nothing todo
                Gdk.Threads.Leave();
                return;
            } 
            
            Gtk.ListStore liststore = (Gtk.ListStore)treeview.Model;
            Gtk.TreeIter iter;
            liststore.GetIterFirst(out iter);
            do {
                if ((string)liststore.GetValue(iter, 1) == user.Nickname) {
                    liststore.Remove(ref iter);
                    break;
                }
            } while (liststore.IterNext(ref iter));
            
            treeview.GetColumn(1).Title = "Users ("+liststore.IterNChildren()+")";
            
            Gdk.Threads.Leave();
        }
        
        public void SetNetworkStatus(string status)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("SetNetworkStatus()");
#endif
#if UI_GNOME
            Frontend.MainWindow.NetworkStatusbar.Push(status);
#elif UI_GTK
            Frontend.MainWindow.NetworkStatusbar.Push(0, status);
#endif
            
            Gdk.Threads.Leave();
        }
        
        public void SetStatus(string status)
        {
            Gdk.Threads.Enter();
            
#if LOG4NET
            Logger.UI.Debug("SetStatus()");
#endif
#if UI_GNOME
            Frontend.MainWindow.Statusbar.Push(status);
#elif UI_GTK
            Frontend.MainWindow.Statusbar.Push(0, status);
#endif
            
            Gdk.Threads.Leave();
        }
    }
}
