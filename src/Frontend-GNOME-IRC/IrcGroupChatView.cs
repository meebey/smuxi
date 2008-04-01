/*
 * $Id: GroupChatView.cs 188 2007-04-21 22:03:54Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/GroupChatView.cs $
 * $Rev: 188 $
 * $Author: meebey $
 * $Date: 2007-04-22 00:03:54 +0200 (Sun, 22 Apr 2007) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2007 Mirco Bauer <meebey@meebey.net>
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
using System.Globalization;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Group, ProtocolManagerType = typeof(IrcProtocolManager))]
    public class IrcGroupChatView : GroupChatView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        //private IrcGroupChatModel  _IrcGroupChatModel; 
        private IrcProtocolManager _IrcProtocolManager;
        
        public IrcGroupChatView(GroupChatModel groupChat) : base(groupChat)
        {
            Trace.Call(groupChat);
            
            //_IrcGroupChatModel = ircGroupChat;
            _IrcProtocolManager = (IrcProtocolManager) groupChat.ProtocolManager;
            
            if (PersonMenu != null) {
                Gtk.ImageMenuItem op_item = new Gtk.ImageMenuItem(_("Op"));
                op_item.Activated += new EventHandler(_OnUserListMenuOpActivated);
                PersonMenu.Append(op_item);
                
                Gtk.ImageMenuItem deop_item = new Gtk.ImageMenuItem(_("Deop"));
                deop_item.Activated += new EventHandler(_OnUserListMenuDeopActivated);
                PersonMenu.Append(deop_item);
                
                Gtk.ImageMenuItem voice_item = new Gtk.ImageMenuItem(_("Voice"));
                voice_item.Activated += new EventHandler(_OnUserListMenuVoiceActivated);
                PersonMenu.Append(voice_item);
                
                Gtk.ImageMenuItem devoice_item = new Gtk.ImageMenuItem(_("Devoice"));
                devoice_item.Activated += new EventHandler(_OnUserListMenuDevoiceActivated);
                PersonMenu.Append(devoice_item);
                
                Gtk.ImageMenuItem kick_item = new Gtk.ImageMenuItem(_("Kick"));
                kick_item.Activated += new EventHandler(_OnUserListMenuKickActivated);
                PersonMenu.Append(kick_item);

                Gtk.ImageMenuItem ban_item = new Gtk.ImageMenuItem(_("Ban"));
                ban_item.Activated += new EventHandler(_OnUserListMenuBanActivated);
                PersonMenu.Append(ban_item);

                Gtk.ImageMenuItem unban_item = new Gtk.ImageMenuItem(_("Unban"));
                unban_item.Activated += new EventHandler(_OnUserListMenuUnbanActivated);
                PersonMenu.Append(unban_item);
                
                // TODO: add devider
                
                Gtk.ImageMenuItem query_item = new Gtk.ImageMenuItem(_("Query"));
                query_item.Activated += new EventHandler(_OnUserListMenuQueryActivated);
                PersonMenu.Append(query_item);
            }
            
            if (PersonTreeView != null) {
                Gtk.CellRendererText cellr = new Gtk.CellRendererText();
                cellr.WidthChars = 1;
                Gtk.TreeViewColumn column = new Gtk.TreeViewColumn(String.Empty, cellr);
                column.Spacing = 0;
                column.SortIndicator = false;
                column.Sizing = Gtk.TreeViewColumnSizing.GrowOnly;
                column.SetCellDataFunc(cellr, new Gtk.TreeCellDataFunc(_RenderIrcGroupPersonMode));
                
                PersonTreeView.AppendColumn(column);
                PersonTreeView.MoveColumnAfter(IdentityNameColumn, column);
            }
        }
        
        private void _RenderIrcGroupPersonMode(Gtk.TreeViewColumn column,
                                               Gtk.CellRenderer cellr,
                                               Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            IrcGroupPersonModel person = model.GetValue(iter, 0) as IrcGroupPersonModel;
            if (person == null) {
#if LOG4NET
                _Logger.Error("_RenderIrcGroupPersonMode(): person == null");
#endif
                return;
            }
            
            string mode;
            if (person.IsOp) {
                mode = "@";
            } else if (person.IsVoice) {
                mode = "+";
            } else {
                mode = String.Empty;
            }
            (cellr as Gtk.CellRendererText).Text = mode;
        }
        
        private void _OnUserListMenuOpActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            PersonModel person = GetSelectedPerson();
            if (person == null) {
                return;
            }

            _IrcProtocolManager.CommandOp(new CommandModel(Frontend.FrontendManager, ChatModel,
                person.ID));
        } 
        
        private void _OnUserListMenuDeopActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            PersonModel person = GetSelectedPerson();
            if (person == null) {
                return;
            }
            
            _IrcProtocolManager.CommandDeop(new CommandModel(Frontend.FrontendManager, ChatModel,
                person.ID));
        }
         
        private void _OnUserListMenuVoiceActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            PersonModel person = GetSelectedPerson();
            if (person == null) {
                return;
            }
            
            _IrcProtocolManager.CommandVoice(new CommandModel(Frontend.FrontendManager, ChatModel,
                    person.ID));
        }
        
        private void _OnUserListMenuDevoiceActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            PersonModel person = GetSelectedPerson();
            if (person == null) {
                return;
            }
            
            _IrcProtocolManager.CommandDevoice(new CommandModel(Frontend.FrontendManager,
                                                                ChatModel, person.ID));
        } 
        
        private void _OnUserListMenuKickActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            PersonModel person = GetSelectedPerson();
            if (person == null) {
                return;
            }
            
            _IrcProtocolManager.CommandKick(new CommandModel(Frontend.FrontendManager, ChatModel,
                    person.ID));
        }
        
        private void _OnUserListMenuBanActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            PersonModel person = GetSelectedPerson();
            if (person == null) {
                return;
            }
            
            _IrcProtocolManager.CommandBan(new CommandModel(Frontend.FrontendManager, ChatModel,
                    person.ID));
        }
        
        private void _OnUserListMenuUnbanActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            PersonModel person = GetSelectedPerson();
            if (person == null) {
                return;
            }
            
            _IrcProtocolManager.CommandUnban(new CommandModel(Frontend.FrontendManager, ChatModel,
                    person.ID));
        }
        
        private void _OnUserListMenuQueryActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            PersonModel person = GetSelectedPerson();
            if (person == null) {
                return;
            }
            
            _IrcProtocolManager.CommandMessageQuery(new CommandModel(Frontend.FrontendManager, ChatModel,
                    person.ID));
        }
        
        protected override void OnTabMenuCloseActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            base.OnTabMenuCloseActivated(sender, e);
            
            _IrcProtocolManager.CommandPart(new CommandModel(Frontend.FrontendManager,
                                                      ChatModel,
                                                      ChatModel.ID));
        }
        
        protected override void OnPersonsRowActivated(object sender, Gtk.RowActivatedArgs e)
        {
            Trace.Call(sender, e);
            
            base.OnPersonsRowActivated(sender, e);
            
            PersonModel person = GetSelectedPerson();
            if (person == null) {
                return;
            }
            
            _IrcProtocolManager.CommandMessageQuery(new CommandModel(Frontend.FrontendManager,
                                                          ChatModel, person.ID));
        }

        protected override int SortPersonListStore(Gtk.TreeModel model,
                                                   Gtk.TreeIter iter1,
                                                   Gtk.TreeIter iter2)
        {
            Gtk.ListStore liststore = (Gtk.ListStore) model;
            
            IrcGroupPersonModel person1 = (IrcGroupPersonModel) liststore.GetValue(iter1, 0);
            IrcGroupPersonModel person2 = (IrcGroupPersonModel) liststore.GetValue(iter2, 0);
            
            int status1 = 0;
            if (person1.IsOp) {
                status1 += 1;
            }
            if (person1.IsVoice) {
                status1 += 2;
            }
            if (status1 == 0) {
                status1 = 4;
            }
            
            int status2 = 0;
            if (person2.IsOp) {
                status2 += 1;
            }
            if (person2.IsVoice) {
                status2 += 2;
            }
            if (status2 == 0) {
                status2 = 4;
            }
            
            int mode_res = 0;
            if (status1 > status2) {
                mode_res = 1;
            }
            if (status1 < status2) {
                mode_res = -1;
            }
            
            if (mode_res == 0 ) {
                // the mode is equal, so the name decides
                return base.SortPersonListStore(model, iter1, iter2);
            }
            
            return mode_res;
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
