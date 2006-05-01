/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using Mono.Unix;
using Meebey.Smuxi.Engine;
using Meebey.Smuxi.Common;

namespace Meebey.Smuxi.FrontendGnome
{
    public class GnomeUI : PermanentRemoteObject, IFrontendUI
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private int _Version = 0;
        
        public int Version
        {
            get {
                return _Version;
            }
        }
        
        public void AddPage(Engine.Page epage)
        {
            Trace.Call(epage);
            
            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, epage);
                
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
                        _Logger.Fatal("AddPage() Unknown PageType: "+epage.PageType);
#endif
                        throw new ApplicationException("Unknown PageType: "+epage.PageType);
                }
                Frontend.MainWindow.Notebook.AppendPage(newpage, newpage.LabelEventBox);
                newpage.ShowAll();
            });
        }
        
        private void _AddMessageToPage(Engine.Page epage, FormattedMessage fmsg)
        {
            string timestamp;
            try {
                timestamp = fmsg.Timestamp.ToLocalTime().ToString((string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"]);
            } catch (FormatException e) {
                timestamp = "Timestamp Format ERROR: "+e.Message;
            }
            
            Page page = Frontend.MainWindow.Notebook.GetPage(epage);
#if LOG4NET
            if (page == null) {
                _Logger.Fatal(String.Format("_AddMessageToPage(): Notebook.GetPage(epage) epage.Name: {0} returned null!", epage.Name));
            }
#endif
            Gtk.TextIter iter = page.OutputTextBuffer.EndIter;
            page.OutputTextBuffer.Insert(ref iter, timestamp + " ");
            
            //string msg = null;
            foreach (FormattedMessageItem item in fmsg.Items) {
#if LOG4NET
                _Logger.Debug("_AddMessageToPage(): item.Type: " + item.Type);
#endif
                switch (item.Type) {
                    // TODO: implement other ItemTypes
                    case FormattedMessageItemType.Text:
                        FormattedMessageTextItem fmsgti = (FormattedMessageTextItem)item.Value;
#if LOG4NET
                        _Logger.Debug("_AddMessageToPage(): fmsgti.Text: '" + fmsgti.Text + "'");
#endif
                        ArrayList tags = new ArrayList();
                        /*
                        if ((ftmsg.Color.HexCode != -1) ||
                            (ftmsg.BackgroundColor.HexCode != -1)) {
                            msg += "<span ";
                            if (ftmsg.Color.HexCode != -1) {
                                msg += "foreground=\"#"+ftmsg.Color.
                                    HexCode+"\" ";
                            }
                            if (ftmsg.BackgroundColor.HexCode != -1) {
                                msg += "background=\"#"+ftmsg.BackgroundColor.
                                    HexCode+"\" ";
                            }
                            msg += ">";
                        }
                        */
                        if (fmsgti.Underline) {
#if LOG4NET
                            _Logger.Debug("_AddMessageToPage(): fmsgti.Underline is true");
#endif
                            tags.Add("underline");
                        }
                        if (fmsgti.Bold) {
#if LOG4NET
                            _Logger.Debug("_AddMessageToPage(): fmsgti.Bold is true");
#endif
                            tags.Add("bold");
                        }
                        if (fmsgti.Italic) {
#if LOG4NET
                            _Logger.Debug("_AddMessageToPage(): fmsgti.Italic is true");
#endif
                            tags.Add("italic");
                        }
                        
                        //msg += ftmsg.Text;
                        page.OutputTextBuffer.InsertWithTagsByName(ref iter, fmsgti.Text, (string[])tags.ToArray(typeof(string)));
                        
                        /*
                        if (ftmsg.Bold) {
                            msg += "</b>";
                        }
                        if (ftmsg.Underline) {
                            msg += "</u>";
                        }
                        if ((ftmsg.Color.HexCode != -1) ||
                            (ftmsg.BackgroundColor.HexCode != -1)) {
                            msg += "</span>";
                        }
                        */
                        break;
                    case FormattedMessageItemType.Url:
                        FormattedMessageUrlItem fmsgui = (FormattedMessageUrlItem)item.Value;
                        page.OutputTextBuffer.InsertWithTagsByName(ref iter, fmsgui.Url, "url");
                        break;
                } 
            }
            page.OutputTextBuffer.Insert(ref iter, "\n");
            
            // we must use pango here!!!
            //page.OutputTextBuffer.Insert(ref iter, msg);
            //page.OutputTextBuffer.InsertWithTagsByName(ref iter, msg, "bold");

            if (Frontend.FrontendManager.CurrentPage != epage) {
                page.Label.Markup = "<span foreground=\"blue\">"+page.Name+"</span>";
            }
        }
        
        public void AddMessageToPage(Engine.Page epage, FormattedMessage fmsg)
        {
            Trace.Call(epage, fmsg);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, epage, fmsg);
                
                _AddMessageToPage(epage, fmsg);
            });
        }
        
        public void RemovePage(Engine.Page epage)
        {
            Trace.Call(epage);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, epage);
                
                Page page = Frontend.MainWindow.Notebook.GetPage(epage);
                Frontend.MainWindow.Notebook.RemovePage(
                    Frontend.MainWindow.Notebook.PageNum(page)
                );
            });
        }
        
        public void SyncPage(Engine.Page epage)
        {
            Trace.Call(epage);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, epage);

                Page page = Frontend.MainWindow.Notebook.GetPage(epage);
                if (epage.PageType == PageType.Channel) {
                    ChannelPage cpage = (ChannelPage)page;
                    Engine.ChannelPage ecpage = (Engine.ChannelPage)epage;
                   
#if LOG4NET
                    _Logger.Debug("SyncPage() syncing userlist");
#endif
                    // sync userlist
                    Gtk.TreeView tv  = cpage.UserListTreeView;
                    if (tv != null) {
                        int count = ecpage.Users.Count;
                        if (count > 1) {
                            Frontend.MainWindow.ProgressBar.DiscreteBlocks = (uint)count;
                        } else {
                            Frontend.MainWindow.ProgressBar.DiscreteBlocks = 2;
                        }
                        Frontend.MainWindow.ProgressBar.BarStyle = Gtk.ProgressBarStyle.Continuous;
                        string status = String.Format(Catalog.GetString(
                                                      "Syncing Channel Users of {0}..."),
                                                      cpage.Name);
#if UI_GNOME
                        Frontend.MainWindow.Statusbar.Push(status);
#elif UI_GTK
                        Frontend.MainWindow.Statusbar.Push(0, status);
#endif
                        Gtk.ListStore ls = (Gtk.ListStore)tv.Model;
                        // cleanup, be sure the list is empty
                        ls.Clear();
                        // detach the model (less CPU load)
                        tv.Model = new Gtk.ListStore(typeof(string), typeof(string));
                        int i = 1;
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
                            Frontend.MainWindow.ProgressBar.Fraction = (double)i++ / count;
                            while (Gtk.Application.EventsPending()) {
                                Gtk.Application.RunIteration(false);
                            }
                        }
                        // attach the model again
                        tv.Model = ls;
                   
                        tv.GetColumn(1).Title = String.Format(Catalog.GetString(
                                                              "Users ({0})"),
                                                              ls.IterNChildren());
                       
                        Frontend.MainWindow.ProgressBar.Fraction = 0;
                        status += Catalog.GetString(" done.");
#if UI_GNOME
                        Frontend.MainWindow.Statusbar.Push(status);
#elif UI_GTK
                        Frontend.MainWindow.Statusbar.Push(0, status);
#endif
                   }
                   
#if LOG4NET
                   _Logger.Debug("SyncPage() syncing topic");
#endif
                   // sync topic
                   if ((cpage.TopicEntry != null) &&
                       (ecpage.Topic != null)) {
                       cpage.TopicEntry.Text = ecpage.Topic;
                   }
                }
                
#if LOG4NET
                _Logger.Debug("SyncPage() syncing messages");
#endif
                // sync messages
                // cleanup, be sure the output is empty
                page.OutputTextBuffer.Clear();
                if (epage.Buffer.Count > 0) {
                    foreach (FormattedMessage fm in epage.Buffer) {
                        _AddMessageToPage(epage, fm);
                    }
                }
                
                page.ScrollToEnd();
#if LOG4NET
                _Logger.Debug("SyncPage() done");
#endif
            });
        }
        
        public void AddUserToChannel(Engine.ChannelPage ecpage, User user)
        {
            Trace.Call(ecpage, user);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, ecpage, user);
                
                ChannelPage cpage = (ChannelPage)Frontend.MainWindow.Notebook.GetPage(ecpage);
                Gtk.TreeView  treeview  = cpage.UserListTreeView;
                if (treeview == null) {
                    // no treeview, nothing todo
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

                treeview.GetColumn(1).Title = String.Format(Catalog.GetString(
                                                            "Users ({0})"),
                                                            liststore.IterNChildren());
            });
        }
        
        public void UpdateUserInChannel(Engine.ChannelPage ecpage, User olduser, User newuser)
        {
            Trace.Call(ecpage, olduser, newuser);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, ecpage, olduser, newuser);
                
                ChannelPage cpage = (ChannelPage)Frontend.MainWindow.Notebook.GetPage(ecpage);
                Gtk.TreeView  treeview  = cpage.UserListTreeView;
                if (treeview == null) {
                    // no treeview, nothing todo
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
            });
        }
        
        public void UpdateTopicInChannel(Engine.ChannelPage ecpage, string topic)
        {
            Trace.Call(ecpage, topic);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, ecpage, topic);
                
                ChannelPage cpage = (ChannelPage)Frontend.MainWindow.Notebook.GetPage(ecpage);
                if (cpage.TopicEntry != null) {
                    cpage.TopicEntry.Text = topic;
                }
            });
        }
        
        public void RemoveUserFromChannel(Engine.ChannelPage ecpage, User user)
        {
            Trace.Call(ecpage, user);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, ecpage, user);
            
                ChannelPage cpage = (ChannelPage)Frontend.MainWindow.Notebook.GetPage(ecpage);
                Gtk.TreeView  treeview  = cpage.UserListTreeView;
                if (treeview == null) {
                    // no treeview, nothing todo
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
                
                treeview.GetColumn(1).Title = String.Format(Catalog.GetString(
                                                            "Users ({0})"),
                                                            liststore.IterNChildren());
            });
        }
        
        public void SetNetworkStatus(string status)
        {
            Trace.Call(status);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, status);
#if UI_GNOME
                Frontend.MainWindow.NetworkStatusbar.Push(status);
#elif UI_GTK
                Frontend.MainWindow.NetworkStatusbar.Push(0, status);
#endif
            });
        }
        
        public void SetStatus(string status)
        {
            Trace.Call(status);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, status);
#if UI_GNOME
                Frontend.MainWindow.Statusbar.Push(status);
#elif UI_GTK
                Frontend.MainWindow.Statusbar.Push(0, status);
#endif
            });
        }
    }
}
