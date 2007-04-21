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
using System.Collections.Generic;
using System.Globalization; 
using System.ComponentModel;
using Mono.Unix;
using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;

namespace Smuxi.Frontend.Gnome
{
    public class GnomeUI : PermanentRemoteObject, IFrontendUI
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private int _Version = 0;
        private ChatViewManagerBase _ChatViewManager;
        private IList<ChatView>     _SyncedChatViews;
        
        public int Version
        {
            get {
                return _Version;
            }
        }
        
        public IList<ChatView> SyncedChatViews {
            get {
                return _SyncedChatViews;
            }
        }
        
        public GnomeUI(ChatViewManagerBase chatViewManager)
        {
            _ChatViewManager = chatViewManager;
        }
        
        public void AddChat(ChatModel chat)
        {
            Trace.Call(chat);
            
            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, chat);
                
                _ChatViewManager.AddChat(chat);
            });
        }
        
        private void _AddMessageToChat(ChatModel epage, MessageModel msg)
        {
            string timestamp;
            try {
                timestamp = msg.TimeStamp.ToLocalTime().ToString((string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"]);
            } catch (FormatException e) {
                timestamp = "Timestamp Format ERROR: " + e.Message;
            }
            
            ChatView chatView = Frontend.MainWindow.Notebook.GetChat(epage);
#if LOG4NET
            if (chatView == null) {
                _Logger.Fatal(String.Format("_AddMessageToChat(): Notebook.GetPage(epage) epage.Name: {0} returned null!", epage.Name));
            }
#endif
            Gtk.TextIter iter = chatView.OutputTextView.Buffer.EndIter;
            chatView.OutputTextView.Buffer.Insert(ref iter, timestamp + " ");
            
            bool hasHighlight = false;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
#if LOG4NET
                _Logger.Debug("_AddMessageToChat(): msgPart.GetType(): " + msgPart.GetType());
#endif
                if (msgPart.IsHighlight) {
                    hasHighlight = true;
                }
                
                // TODO: implement all types
                if (msgPart is UrlMessagePartModel) {
                    UrlMessagePartModel fmsgui = (UrlMessagePartModel) msgPart;
                    chatView.OutputTextView.Buffer.InsertWithTagsByName(ref iter, fmsgui.Url, "url");
                } else if (msgPart is TextMessagePartModel) {
                    TextMessagePartModel fmsgti = (TextMessagePartModel) msgPart;
#if LOG4NET
                    _Logger.Debug("_AddMessageToChat(): fmsgti.Text: '" + fmsgti.Text + "'");
#endif
                    ArrayList tags = new ArrayList();
                    
                    if (fmsgti.ForegroundColor.HexCode != -1) {
                        string tagname = _GetTextTagName(chatView, fmsgti.ForegroundColor, null);
                        tags.Add(tagname);
                    }
                    if (fmsgti.BackgroundColor.HexCode != -1) {
                        string tagname = _GetTextTagName(chatView, null, fmsgti.BackgroundColor);
                        tags.Add(tagname);
                    }
                    
                    if (fmsgti.Underline) {
#if LOG4NET
                        _Logger.Debug("_AddMessageToChat(): fmsgti.Underline is true");
#endif
                        tags.Add("underline");
                    }
                    if (fmsgti.Bold) {
#if LOG4NET
                        _Logger.Debug("_AddMessageToChat(): fmsgti.Bold is true");
#endif
                        tags.Add("bold");
                    }
                    if (fmsgti.Italic) {
#if LOG4NET
                        _Logger.Debug("_AddMessageToChat(): fmsgti.Italic is true");
#endif
                        tags.Add("italic");
                    }
                    
                    chatView.OutputTextView.Buffer.InsertWithTagsByName(ref iter, fmsgti.Text, (string[])tags.ToArray(typeof(string)));
                    
                    /*
                    page.OutputTextBuffer.Insert(ref iter, fmsgti.Text);
                    Gtk.TextIter end_iter = iter;
                    Gtk.TextIter start_iter = page.OutputTextBuffer.GetIterAtOffset(end_iter.Offset - fmsgti.Text.Length);
                    if (fg_color_tt != null) {
                        page.OutputTextBuffer.ApplyTag(fg_color_tt, start_iter, end_iter);
                    }
                    */
                } 
            }
            chatView.OutputTextView.Buffer.Insert(ref iter, "\n");
            
            if (hasHighlight && !Frontend.MainWindow.HasToplevelFocus) {
                Frontend.MainWindow.UrgencyHint = true;
                if (Frontend.UserConfig["Sound/BeepOnHighlight"] != null &&
                    (bool)Frontend.UserConfig["Sound/BeepOnHighlight"]) {
                    Frontend.MainWindow.Display.Beep();
                }
            }
            
            if (Frontend.MainWindow.Notebook.CurrentChatView != chatView) {
                string color = null;
                if (hasHighlight) {
                    chatView.HasHighlight = hasHighlight;
                    color = (string) Frontend.UserConfig["Interface/Notebook/Tab/HighlightColor"];
                } else if (!chatView.HasHighlight) {
                    color = (string) Frontend.UserConfig["Interface/Notebook/Tab/ActivityColor"];
                }
                
                if (color != null) {
                    chatView.Label.Markup = String.Format("<span foreground=\"{0}\">{1}</span>", color, chatView.Name);
                }
            }
        }
        
        private string _GetTextTagName(ChatView page, TextColor fg_color, TextColor bg_color)
        {
             string hexcode;
             string tagname;
             if (fg_color != null) {
                hexcode = fg_color.HexCode.ToString("X6");
                tagname = "fg_color:" + hexcode;
             } else if (bg_color != null) {
                hexcode = bg_color.HexCode.ToString("X6");
                tagname = "bg_color:" + hexcode;
             } else {
                return null;
             }
             
             if (page.OutputTextTagTable.Lookup(tagname) == null) {
                 int red   = Int16.Parse(hexcode.Substring(0, 2), NumberStyles.HexNumber);
                 int green = Int16.Parse(hexcode.Substring(2, 2), NumberStyles.HexNumber);
                 int blue  = Int16.Parse(hexcode.Substring(4, 2), NumberStyles.HexNumber);
                 Gdk.Color c = new Gdk.Color((byte)red, (byte)green, (byte)blue);
                 Gtk.TextTag tt = new Gtk.TextTag(tagname);
                 if (fg_color != null) {
                    tt.ForegroundGdk = c;
                 } else if (bg_color != null) {
                    tt.BackgroundGdk = c;
                 }
#if LOG4NET
                 _Logger.Debug("_GetTextTagName(): adding: " + tagname + " to page.OutputTextTagTable");
#endif
                 page.OutputTextTagTable.Add(tt);
             }
             return tagname;
        }
        
        public void AddMessageToChat(ChatModel epage, MessageModel fmsg)
        {
            Trace.Call(epage, fmsg);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, epage, fmsg);
                
                _AddMessageToChat(epage, fmsg);
            });
        }
        
        public void RemoveChat(ChatModel chat)
        {
            Trace.Call(chat);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, chat);
                
                _ChatViewManager.RemoveChat(chat);
            });
        }
        
        public void EnableChat(ChatModel chat)
        {
        	Trace.Call(chat);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
            	Trace.Call(mb, chat);
                
                _ChatViewManager.EnableChat(chat);
        	});
        }
        
        public void DisableChat(ChatModel chat)
        {
        	Trace.Call(chat);
        	
            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
            	Trace.Call(mb, chat);
            	
                _ChatViewManager.DisableChat(chat);
        	});
        }
        
        public void SyncChat(ChatModel chatModel)
        {
            Trace.Call(chatModel);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, chatModel);

#if LOG4NET
                DateTime syncStart = DateTime.UtcNow;
#endif
                ChatView chatView = Frontend.MainWindow.Notebook.GetChat(chatModel);
                if (chatModel.ChatType == ChatType.Group) {
                    GroupChatView cpage = (GroupChatView)chatView;
                    GroupChatModel ecpage = (GroupChatModel)chatModel;
                    IDictionary<string, PersonModel> users = ecpage.Persons; 
#if LOG4NET
                    _Logger.Debug("SyncChat() syncing userlist");
#endif
                    // sync userlist
                    Gtk.TreeView tv  = cpage.UserListTreeView;
                    if (tv != null) {
                        int count = users.Count;
                        /*
                        if (count > 1) {
                            Frontend.MainWindow.ProgressBar.DiscreteBlocks = (uint)count;
                        } else {
                            Frontend.MainWindow.ProgressBar.DiscreteBlocks = 2;
                        }
                        Frontend.MainWindow.ProgressBar.BarStyle = Gtk.ProgressBarStyle.Continuous;
                        */
                        string status = String.Format(
                                            _("Syncing Channel PersonModels of {0}..."),
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
                        foreach (PersonModel user in users.Values) {
                            /*
                            if (user is Engine.IrcGroupPersonModel) {
                                IrcGroupPersonModel icuser = (IrcGroupPersonModel)user;
                                if (icuser.IsOp) {
                                    ls.AppendValues("@", icuser.NickName);
                                } else if (icuser.IsVoice) {
                                    ls.AppendValues("+", icuser.NickName);
                                } else {
                                    ls.AppendValues(String.Empty, icuser.NickName);
                                }
                            }
                            */
                            
                            //Frontend.MainWindow.ProgressBar.Fraction = (double)i++ / count;
                            /*
                            // this seems to break the sync when it's remote engine is used,
                            // guess it does some other GUI processing, like removing users from
                            // the userlist....
                            while (Gtk.Application.EventsPending()) {
                                Gtk.Application.RunIteration(false);
                            }
                            */
                        }
                        // attach the model again
                        tv.Model = ls;
                   
                        cpage.UpdateUsersCount(); 
                       
                        Frontend.MainWindow.ProgressBar.Fraction = 0;
                        status += _(" done.");
#if UI_GNOME
                        Frontend.MainWindow.Statusbar.Push(status);
#elif UI_GTK
                        Frontend.MainWindow.Statusbar.Push(0, status);
#endif
                   }
                   
#if LOG4NET
                   _Logger.Debug("SyncChat() syncing topic");
#endif
                   // sync topic
                   string topic = ecpage.Topic;
                   if ((cpage.TopicEntry != null) &&
                       (topic != null)) {
                       cpage.TopicEntry.Text = topic;
                   }
                }
                
#if LOG4NET
                _Logger.Debug("SyncChat() syncing messages");
#endif
                // sync messages
                // cleanup, be sure the output is empty
                chatView.OutputTextView.Buffer.Clear();
                IList<MessageModel> messages = chatModel.Messages;
                if (messages.Count > 0) {
                    foreach (MessageModel fm in messages) {
                        _AddMessageToChat(chatModel, fm);
                    }
                }
                
                // maybe a BUG here? should be tell the FrontendManager before we sync?
                Frontend.FrontendManager.AddSyncedChat(chatModel);
#if LOG4NET
                DateTime syncStop = DateTime.UtcNow;
                double duration = syncStop.Subtract(syncStart).TotalMilliseconds;
                _Logger.Debug("SyncChat() done, syncing took: " + Math.Round(duration) + " ms");
#endif
                //_SyncedChats.Add(chatView);

                // BUG: doesn't work?!?
                chatView.ScrollToEnd();
            });
        }
        
        public void AddPersonToGroupChat(GroupChatModel ecpage, PersonModel user)
        {
            Trace.Call(ecpage, user);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, ecpage, user);
                
                GroupChatView cpage = (GroupChatView)Frontend.MainWindow.Notebook.GetChat(ecpage);
                Gtk.TreeView  treeview  = cpage.UserListTreeView;
                if (treeview == null) {
                    // no treeview, nothing todo
                    return;
                }
                
                Gtk.ListStore liststore = (Gtk.ListStore)treeview.Model;
                /*
                IrcGroupPersonModel icuser = (IrcGroupPersonModel)user;
                if (icuser.IsOp) {
                    liststore.AppendValues("@", icuser.NickName);
                } else if (icuser.IsVoice) {
                    liststore.AppendValues("+", icuser.NickName);
                } else {
                    liststore.AppendValues(String.Empty, icuser.NickName);
                }
                */
                
                treeview.GetColumn(1).Title = String.Format(
                                                _("PersonModels ({0})"),
                                                liststore.IterNChildren());
            });
        }
        
        public void UpdatePersonInGroupChat(GroupChatModel ecpage, PersonModel olduser, PersonModel newuser)
        {
            Trace.Call(ecpage, olduser, newuser);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, ecpage, olduser, newuser);
                
                GroupChatView cpage = (GroupChatView)Frontend.MainWindow.Notebook.GetChat(ecpage);
                Gtk.TreeView  treeview  = cpage.UserListTreeView;
                if (treeview == null) {
                    // no treeview, nothing todo
                    return;
                } 
                
                Gtk.ListStore liststore = (Gtk.ListStore)treeview.Model;
                Gtk.TreeIter iter;
                bool res = liststore.GetIterFirst(out iter);
                if (!res) {
#if LOG4NET
                    _Logger.Error("UpdatePersonModelInChannel(): liststore.GetIterFirst() returned false, ignoring update...");
#endif
                    return;
                }
                
                do {
                    if ((string)liststore.GetValue(iter, 1) == olduser.IdentityName) {
                        //IrcGroupPersonModel newcuser = (IrcGroupPersonModel)newuser;
                        string mode = String.Empty;
                        /*
                        if (newcuser.IsOp) {
                            mode = "@";
                        } else if (newcuser.IsVoice) {
                            mode = "+";
                        } else {
                            mode = String.Empty;
                        }
                        */
                        
                        // update the mode of the current row
                        liststore.SetValue(iter, 0, mode);
                        // update the nickname of the current row
                        liststore.SetValue(iter, 1, newuser.IdentityName);
                        break;
                    }
                } while (liststore.IterNext(ref iter));
            });
        }
        
        public void UpdateTopicInGroupChat(GroupChatModel ecpage, string topic)
        {
            Trace.Call(ecpage, topic);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, ecpage, topic);
                
                GroupChatView cpage = (GroupChatView)Frontend.MainWindow.Notebook.GetChat(ecpage);
                if (cpage.TopicEntry != null) {
                    cpage.TopicEntry.Text = topic;
                }
            });
        }
        
        public void RemovePersonFromGroupChat(GroupChatModel ecpage, PersonModel user)
        {
            Trace.Call(ecpage, user);

            MethodBase mb = Trace.GetMethodBase();
            Gtk.Application.Invoke(delegate {
                Trace.Call(mb, ecpage, user);
            
                GroupChatView cpage = (GroupChatView)Frontend.MainWindow.Notebook.GetChat(ecpage);
                Gtk.TreeView  treeview  = cpage.UserListTreeView;
                if (treeview == null) {
                    // no treeview, nothing todo
                    return;
                } 
                
                Gtk.ListStore liststore = (Gtk.ListStore)treeview.Model;
                Gtk.TreeIter iter;
                liststore.GetIterFirst(out iter);
                do {
                    if ((string)liststore.GetValue(iter, 1) == user.IdentityName) {
                        liststore.Remove(ref iter);
                        break;
                    }
                } while (liststore.IterNext(ref iter));
                
                treeview.GetColumn(1).Title = String.Format(
                                                _("PersonModels ({0})"),
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
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
