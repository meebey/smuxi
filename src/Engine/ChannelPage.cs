/**
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
using System.Collections;
using System.Collections.Specialized;

namespace Meebey.Smuxi.Engine
{
    public class ChannelPage : Page
    {
        private string    _Topic;
        private Hashtable _Users = Hashtable.Synchronized(new Hashtable());
        private bool      _IsSynced;
        
        public string Topic
        {
            get {
                return _Topic;
            }
            set {
                _Topic = value;
            }
        }
        
        public Hashtable Users
        {
            get {
                return _Users;
            }
        }
        
        public bool IsSynced
        {
            get {
                return _IsSynced;
            }
            set {
                _IsSynced = value;
            }
        }
        
        public ChannelPage(string name, NetworkType ntype, INetworkManager nm) : base(name, PageType.Channel, ntype, nm)
        {
        }
        
        public User GetUser(string nickname)
        {
            return (User)_Users[nickname.ToLower()];
        }
        
        public string NicknameLookup(string searchnick)
        {
#if LOG4NET
            Logger.NickCompletion.Debug("ChannelPage.Name: "+Name);
#endif
            int searchnicklength = searchnick.Length; 
            foreach (User user in _Users.Values) {
#if LOG4NET
                Logger.NickCompletion.Debug("user.Nickname: "+user.Nickname);
#endif
                if ((user.Nickname.Length >= searchnicklength) &&
                    (user.Nickname.Substring(0, searchnicklength).ToLower() == searchnick.ToLower())) {
#if LOG4NET
                    Logger.NickCompletion.Debug("found: "+user.Nickname);
#endif
                    return user.Nickname;
                }   
            }
        
#if LOG4NET
            Logger.NickCompletion.Debug("NicknameLookup() no matching nickname found");
#endif
            return null;
        }

        public string[] NicknameLookupAll(string searchnick)
        {
            StringCollection foundnicks = new StringCollection();
            int searchnicklength = searchnick.Length;
            foreach (User user in _Users.Values) {
                if ((user.Nickname.Length >= searchnicklength) &&
                    (user.Nickname.Substring(0, searchnicklength).ToLower() == searchnick.ToLower())) {
                    foundnicks.Add(user.Nickname);
                }   
            }
        
            string[] result;
            result = new string[foundnicks.Count];
            foundnicks.CopyTo(result, 0);
            return result;
        }
    }
}
