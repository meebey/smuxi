/*
 * $Id: ChannelPage.cs 137 2006-11-06 18:49:57Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/ChannelPage.cs $
 * $Rev: 137 $
 * $Author: meebey $
 * $Date: 2006-11-06 19:49:57 +0100 (Mon, 06 Nov 2006) $
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
using System.Collections.Specialized;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class GroupChatModel : ChatModel
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        //private Hashtable _Persons = Hashtable.Synchronized(new Hashtable());
        // shouldn't need threadsafe wrapper, only the "owning" IRC thread should write to it
        private IDictionary<string, PersonModel> _Persons = new Dictionary<string, PersonModel>();
        private bool                             _IsSynced;
        // IRC specific?
        private string    _Topic;
        
        public override bool IsEnabled {
            get {
                return base.IsEnabled;
            }
            internal set {
                base.IsEnabled = value;
                if (!value) {
                    _Topic = null;
                    _Persons.Clear();
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
        
        // safe version
        public IDictionary<string, PersonModel> Persons {
            get {
                // during cloning, someone could modify it and break the enumerator
                lock (_Persons) {
                    return new Dictionary<string, PersonModel>(_Persons);
                }
            }
        }
        
        // ProtocolManagers need access to this
        public IDictionary<string, PersonModel> UnsafePersons {
            get {
                lock (_Persons) {
                    return _Persons;
                }
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
        
        public GroupChatModel(string id, string name, IProtocolManager networkManager) :
                         base(id, name, ChatType.Group, networkManager)
        {
        }
        
        public PersonModel GetPerson(string id)
        {
            if (id == null) {
                throw new ArgumentNullException("id");
            }
            
            PersonModel personModel;
            _Persons.TryGetValue(id.ToLower(), out personModel);
            return personModel;
        }
        
        public PersonModel PersonLookup(string identityName)
        {
            Trace.Call(identityName);
            
#if LOG4NET
            _Logger.Debug("PersonLookup(): GroupChatModel.Name: " + Name);
#endif
            int identityNameLength = identityName.Length; 
            // must use a safe version (copy) here of Users, public method which can be used by a frontend (or many)
            foreach (PersonModel person in Persons.Values) {
                if ((person.IdentityName.Length >= identityNameLength) &&
                    (person.IdentityName.Substring(0, identityNameLength).ToLower() == identityName.ToLower())) {
#if LOG4NET
                    _Logger.Debug("PersonLookup(): found: " + person.IdentityName);
#endif
                    return person;
                }   
            }
            
#if LOG4NET
            _Logger.Debug("PersonLookup() no matching identityName found");
#endif
            return null;
        }

        public IList<string> PersonLookupAll(string identityName)
        {
            Trace.Call(identityName);
            
            //IList<PersonModel> foundPersons = new List<PersonModel>();
            IList<string> foundIdentityNames = new List<string>();
            int identityNameLength = identityName.Length;
            string longestIdentityName = String.Empty;
            // must use a copy here of Users, public method which can be used by a frontend (or many)
            foreach (PersonModel person in Persons.Values) {
                if ((person.IdentityName.Length >= identityNameLength) &&
                    (person.IdentityName.Substring(0, identityNameLength).ToLower() == identityName.ToLower())) {
                    foundIdentityNames.Add(person.IdentityName);
                    if (person.IdentityName.Length > longestIdentityName.Length) {
                        longestIdentityName = person.IdentityName; 
                    }
                }
            }
            
            // guess the common part of the found nicknames
            string common_nick = identityName;
            bool match = true;
            while (match) {
                if (common_nick.Length >= longestIdentityName.Length) {
                    break;
                }
                
                common_nick += longestIdentityName[common_nick.Length];
                foreach (string name in foundIdentityNames) {
                    if (!name.ToLower().StartsWith(common_nick.ToLower())) {
                        common_nick = common_nick.Substring(0, common_nick.Length - 1);
                        match = false;
                     }
                }
            }

            if (foundIdentityNames.Count == 0) {
#if LOG4NET
                _Logger.Debug("PersonLookupAll(): no matching identityName found");
#endif
            } else if (foundIdentityNames.Count == 1) {
#if LOG4NET
                _Logger.Debug("PersonLookupAll(): found exact match: " + foundIdentityNames[0]);
#endif
            } else {
#if LOG4NET
                _Logger.Debug("PersonLookupAll(): found " + foundIdentityNames.Count + " matches");
#endif
                foundIdentityNames.Insert(0, common_nick);
            }
            return foundIdentityNames;
        }
    }
}
