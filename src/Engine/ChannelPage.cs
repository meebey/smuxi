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
using System.Collections;
using System.Collections.Specialized;
using Meebey.Smuxi.Common;

namespace Meebey.Smuxi.Engine
{
    public class ChannelPage : Page
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private string    _Topic;
        //private Hashtable _Users = Hashtable.Synchronized(new Hashtable());
        // shouldn't need threadsafe wrapper, only the IRC thread should write to it
        private Hashtable _Users = new Hashtable();
        private bool      _IsSynced;
        
        public override bool IsEnabled {
            get {
                return base.IsEnabled;
            }
            internal set {
                base.IsEnabled = value;
                if (!value) {
                    _Topic = null;
                    _Users.Clear();
                    _IsSynced = false;
                }
            }
        }
        
        public string Topic {
            get {
                return _Topic;
            }
            set {
                _Topic = value;
            }
        }
        
        public Hashtable Users {
            get {
                return (Hashtable)_Users.Clone();
            }
        }
        
        public Hashtable UnsafeUsers {
            get {
                return _Users;
            }
        }
        
        public bool IsSynced {
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
            Trace.Call(searchnick);
            
#if LOG4NET
            _Logger.Debug("NicknameLookup(): ChannelPage.Name: "+Name);
#endif
            int searchnicklength = searchnick.Length; 
            // must use a copy here of Users, public method which can be used by a frontend (or many)
            foreach (User user in Users.Values) {
#if LOG4NET
                _Logger.Debug("NicknameLookup(): user.Nickname: "+user.Nickname);
#endif
                if ((user.Nickname.Length >= searchnicklength) &&
                    (user.Nickname.Substring(0, searchnicklength).ToLower() == searchnick.ToLower())) {
#if LOG4NET
                    _Logger.Debug("NicknameLookup(): found: "+user.Nickname);
#endif
                    return user.Nickname;
                }   
            }
        
#if LOG4NET
            _Logger.Debug("NicknameLookup() no matching nickname found");
#endif
            return null;
        }

        public string[] NicknameLookupAll(string searchnick)
        {
            Trace.Call(searchnick);
            
            StringCollection foundnicks = new StringCollection();
            int searchnicklength = searchnick.Length;
            string longest_nickname = String.Empty;
            // must use a copy here of Users, public method which can be used by a frontend (or many)
            foreach (User user in Users.Values) {
                if ((user.Nickname.Length >= searchnicklength) &&
                    (user.Nickname.Substring(0, searchnicklength).ToLower() == searchnick.ToLower())) {
                    foundnicks.Add(user.Nickname);
                    if (user.Nickname.Length > longest_nickname.Length) {
                        longest_nickname = user.Nickname; 
                    }
                }
            }
            
            // guess the common part of the found nicknames
            string common_nick = searchnick;
            bool match = true;
            while (match) {
                if (common_nick.Length >= longest_nickname.Length) {
                    break;
                }
                
                common_nick += longest_nickname[common_nick.Length];
                foreach (string nick in foundnicks) {
                    if (!nick.ToLower().StartsWith(common_nick.ToLower())) {
                        common_nick = common_nick.Substring(0, common_nick.Length - 1);
                        match = false;
                     }
                }
            }

            string[] result = null;
            if (foundnicks.Count == 0) {
#if LOG4NET
                _Logger.Debug("NicknameLookupAll(): no matching nickname found");
#endif
            } else if (foundnicks.Count == 1) {
#if LOG4NET
                _Logger.Debug("NicknameLookupAll(): found exact match: "+foundnicks[0]);
#endif
                result = new string[] { foundnicks[0] };
            } else {
#if LOG4NET
                _Logger.Debug("NicknameLookupAll(): found "+foundnicks.Count+" matches");
#endif
                result = new string[foundnicks.Count+1];
                result[0] = common_nick;
                foundnicks.CopyTo(result, 1);
            }
            return result;
        }
    }
}
