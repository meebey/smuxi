/*
 * $Id: AboutDialog.cs 122 2006-04-26 19:31:42Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/AboutDialog.cs $
 * $Rev: 122 $
 * $Author: meebey $
 * $Date: 2006-04-26 21:31:42 +0200 (Wed, 26 Apr 2006) $
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
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend
{
    public abstract class ChatViewManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private IDictionary<ChatViewInfoAttribute, Type> _ChatViewTypes = new Dictionary<ChatViewInfoAttribute, Type>();
        
        public abstract IChatView ActiveChat {
            get;
        }
        
        protected ChatViewManagerBase()
        {
        }
        
        public abstract void AddChat(ChatModel chat);
        public abstract void RemoveChat(ChatModel chat);
        public abstract void EnableChat(ChatModel chat);
        public abstract void DisableChat(ChatModel chat);
        
        private Type _GetChatViewType(ChatType chatType, Type protocolManagerType)
        {
            foreach (ChatViewInfoAttribute info in _ChatViewTypes.Keys) {
                if (info.ChatType == chatType &&
                    info.ProtocolManagerType == protocolManagerType) {
                    return _ChatViewTypes[info];
                }
            }
            return null;
        }
        
        protected IChatView CreateChatView(ChatModel chat)
        {
            Trace.Call(chat);
            
            Type type;
            type = _GetChatViewType(chat.ChatType, chat.ProtocolManager != null ?
                                                   chat.ProtocolManager.GetType() : null);
            if (type == null) {
                type = _GetChatViewType(chat.ChatType, null);
            }
            
            if (type == null) {
                throw new ApplicationException("Unsupported ChatModel type: " + chat.GetType());
            }
            
            return (IChatView) Activator.CreateInstance(type, chat);
        }
        
        public void LoadAll(string path, string pattern)
        {
            Trace.Call(path, pattern);
            
            string[] filenames = Directory.GetFiles(path, pattern);
            foreach (string filename in filenames) {
                Load(filename);
            }
        }
        
        public void Load(string filename)
        {
            Trace.Call(filename);
            
            Load(Assembly.LoadFile(filename));
        }
        
        public void Load(Assembly assembly)
        {
            Trace.Call(assembly);
            
            Type[] types = assembly.GetTypes();
            
            foreach (Type type in types) {
                Type foundType = null;
                Type[] interfaceTypes = type.GetInterfaces();
                foreach (Type interfaceType in interfaceTypes) {
                    if (interfaceType == typeof(IChatView)) {
#if LOG4NET
                        _Logger.Debug("Load(): found " + type);
#endif
                        foundType = type;
                        break;
                    }
                }
                
                if (foundType == null) {
                    continue;
                }
                
                // let's get the info attribute
                object[] attrs = foundType.GetCustomAttributes(typeof(ChatViewInfoAttribute), true);
                if (attrs == null || attrs.Length == 0) {
                    continue;
                }
                
                foreach (ChatViewInfoAttribute attr in attrs) {
                    _ChatViewTypes.Add(attr, foundType);
                }
            }
        }
    }
}
