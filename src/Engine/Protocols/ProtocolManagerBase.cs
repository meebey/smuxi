/*
 * $Id: IrcProtocolManager.cs 149 2007-04-11 16:47:52Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/IrcProtocolManager.cs $
 * $Rev: 149 $
 * $Author: meebey $
 * $Date: 2007-04-11 18:47:52 +0200 (Wed, 11 Apr 2007) $
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
using System.Collections.Generic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public abstract class ProtocolManagerBase : PermanentRemoteObject, IProtocolManager
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Session         _Session;
        private string          _Host;
        private int             _Port;
        private bool            _IsConnected;
        
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        
        public virtual string Host {
            get {
                return _Host;
            }
            protected set {
                _Host = value; 
            }
        }
        
        public virtual int Port {
            get {
                return _Port;
            }
            protected set {
                _Port = value; 
            }
        }
        
        public virtual bool IsConnected {
            get {
                return _IsConnected;
            }
            protected set {
                _IsConnected = value; 
            }
        }
        
        public abstract string NetworkID {
            get;
        }
        
        public abstract string Protocol {
            get;
        }
        
        public abstract NetworkProtocol NetworkProtocol {
            get;
        }
        
        public abstract ChatModel Chat {
            get;
        }
        
        protected Session Session {
            get {
                return _Session;
            }
        }
        
        protected ProtocolManagerBase(Session session)
        {
            Trace.Call(session);
            
            _Session = session;
        }
        
        public virtual void Dispose()
        {
            Trace.Call();
            
            // we can't delete directly, it will break the enumerator, let's use a list
            List<ChatModel> removelist = new List<ChatModel>();
            foreach (ChatModel chat in _Session.Chats) {
                if (chat.ProtocolManager == this) {
                    removelist.Add(chat);
                }
            }
            
            // now we can delete
            foreach (ChatModel chat in removelist) {
                _Session.RemoveChat(chat);
            }
        }
        
        public abstract bool Command(CommandModel cmd);
        public abstract void Connect(FrontendManager fm,
                                     string hostname, int port,
                                     string username, string password);
        public abstract void Reconnect(FrontendManager fm);
        public abstract void Disconnect(FrontendManager fm);
        
        protected void NotConnected(CommandModel cmd)
        {
            cmd.FrontendManager.AddTextToCurrentChat("-!- " + _("Not connected to server"));
        }

        protected void NotEnoughParameters(CommandModel cmd)
        {
            cmd.FrontendManager.AddTextToCurrentChat(
                "-!- " + String.Format(_("Not enough parameters for {0} command"),
                cmd.Command));
        }
        
        protected virtual void OnConnected(EventArgs e)
        {
            Trace.Call(e);
            
            if (Connected != null) {
                Connected(this, e);
            }
        }
        
        protected virtual void OnDisconnected(EventArgs e)
        {
            Trace.Call(e);

            if (Disconnected != null) {
                Disconnected(this, e);
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
        
        protected ChatModel GetChat(string id, ChatType chatType)
        {
            return _Session.GetChat(id, chatType, this);
        }
    }
}
