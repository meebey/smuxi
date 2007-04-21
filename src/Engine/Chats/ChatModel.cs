/*
 * $Id: Page.cs 138 2006-12-23 17:11:57Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/Page.cs $
 * $Rev: 138 $
 * $Author: meebey $
 * $Date: 2006-12-23 18:11:57 +0100 (Sat, 23 Dec 2006) $
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
using System.Collections;
using System.Collections.Generic;
using Meebey.Smuxi.Common;

namespace Meebey.Smuxi.Engine
{
    public abstract class ChatModel : PermanentRemoteObject, ITraceable
    {
        private string               _ID;
        private string               _Name;
        private ChatType             _ChatType;
        private INetworkManager      _NetworkManager;
        private List<MessageModel>   _Messages = new List<MessageModel>();
        private bool                 _IsEnabled = true;
        
        public string ID {
            get {
                return _ID;
            }
        }
        
        public string Name {
            get {
                return _Name;
            }
        }
        
        public ChatType ChatType {
            get {
                return _ChatType;
            }
        }
        
        public INetworkManager NetworkManager {
            get {
                return _NetworkManager;
            }
        }
        
        public IList<MessageModel> Messages {
            get {
                lock (_Messages) {
                    // during cloning, someone could modify it and break the enumerator
                    return new List<MessageModel>(_Messages);
                }
            }
        }
        
        internal IList<MessageModel> UnsafeMessages {
            get {
                lock (_Messages) {
                    return _Messages;
                }
            }
        }
        
        public virtual bool IsEnabled {
        	get {
        		return _IsEnabled;
        	}
        	internal set {
        	    _IsEnabled = value;
        	}
        }
        
        public ChatModel(string id, string name, ChatType chatType, INetworkManager networkManager)
        {
            _ID = id;
            _Name = name;
            _ChatType = chatType;
            _NetworkManager = networkManager;
        }
        
        public string ToTraceString()
        {
        	string nm = (_NetworkManager != null) ? _NetworkManager.ToString() : "(null)";  
        	return  nm + "/" + _Name; 
        }
    }
}
