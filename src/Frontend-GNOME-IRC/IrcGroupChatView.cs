/*
 * $Id: GroupChatView.cs 188 2007-04-21 22:03:54Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/GroupChatView.cs $
 * $Rev: 188 $
 * $Author: meebey $
 * $Date: 2007-04-22 00:03:54 +0200 (Sun, 22 Apr 2007) $
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
        //private IrcGroupChatModel _IrcGroupChatModel; 
        
        public IrcGroupChatView(GroupChatModel groupChat) : base(groupChat)
        {
            Trace.Call(groupChat);
            
            //_IrcGroupChatModel = ircGroupChat;

            if (this.UserListMenu != null) {
                Gtk.ImageMenuItem op_item = new Gtk.ImageMenuItem(_("Op"));
                op_item.Activated += new EventHandler(_OnUserListMenuOpActivated);
                this.UserListMenu.Append(op_item);
                
                Gtk.ImageMenuItem deop_item = new Gtk.ImageMenuItem(_("Deop"));
                deop_item.Activated += new EventHandler(_OnUserListMenuDeopActivated);
                this.UserListMenu.Append(deop_item);
                
                Gtk.ImageMenuItem voice_item = new Gtk.ImageMenuItem(_("Voice"));
                voice_item.Activated += new EventHandler(_OnUserListMenuVoiceActivated);
                this.UserListMenu.Append(voice_item);
                
                Gtk.ImageMenuItem devoice_item = new Gtk.ImageMenuItem(_("Devoice"));
                devoice_item.Activated += new EventHandler(_OnUserListMenuDevoiceActivated);
                this.UserListMenu.Append(devoice_item);
                
                Gtk.ImageMenuItem kick_item = new Gtk.ImageMenuItem(_("Kick"));
                kick_item.Activated += new EventHandler(_OnUserListMenuKickActivated);
                this.UserListMenu.Append(kick_item);
            }
        }

        private void _OnUserListMenuOpActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            string whom = GetSelectedNode();
            if (whom == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandOp(new CommandModel(Frontend.FrontendManager, ChatModel,
                    whom));
            }
        } 
        
        private void _OnUserListMenuDeopActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            string whom = GetSelectedNode();
            if (whom == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandDeop(new CommandModel(Frontend.FrontendManager, ChatModel,
                    whom));
            }
        }
         
        private void _OnUserListMenuVoiceActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            string whom = GetSelectedNode();
            if (whom == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandVoice(new CommandModel(Frontend.FrontendManager, ChatModel,
                    whom));
            }
        }
        
        private void _OnUserListMenuDevoiceActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            string whom = GetSelectedNode();
            if (whom == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandDevoice(new CommandModel(Frontend.FrontendManager, ChatModel,
                    whom));
            }
        } 
        
        private void _OnUserListMenuKickActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            string victim = GetSelectedNode();
            if (victim == null) {
                return;
            }
            
            if (ChatModel.ProtocolManager is IrcProtocolManager) {
                IrcProtocolManager imanager = (IrcProtocolManager) ChatModel.ProtocolManager;
                imanager.CommandKick(new CommandModel(Frontend.FrontendManager, ChatModel,
                    victim));
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
