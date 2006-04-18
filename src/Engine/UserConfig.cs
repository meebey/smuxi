/*
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

namespace Meebey.Smuxi.Engine
{
    public class UserConfig : PermanentRemoteObject
    {
        private Config    _Config;
        private string    _UserPrefix;
        private string    _DefaultPrefix = "Engine/Users/DEFAULT/";
        private Hashtable _Cache; 
        
        public bool IsCaching
        {
            get {
                return _Cache != null;
            }
            set {
                if (value) {
                    _Cache = new Hashtable();
                } else {
                    _Cache = null;
                }
            }
        }
        
        public object this[string key]
        {
            get {
                if (IsCaching) {
                    if (_Cache.Contains(key)) {
                        return _Cache[key];
                    }
                }
                
                object obj;
                obj = _Config[_UserPrefix+key];
                if (obj != null) {
                    if (IsCaching) {
                        _Cache.Add(key, obj);
                    }
                    return obj;
                }
                
                obj = _Config[_DefaultPrefix+key];
#if LOG4NET
                if (obj == null) {
                    Logger.Config.Error("value is null for key: "+key);
                }
#endif
                if (IsCaching) {
                    _Cache.Add(key, obj);
                }

                return obj;
            }
            set {
                _Config[_UserPrefix+key] = value;
            }
        }
        
        public UserConfig(Config config, string username)
        {
            _Config = config;
            _UserPrefix = "Engine/Users/"+username+"/";
        }
        
        public void ClearCache()
        {
            if (IsCaching) {
#if LOG4NET
                Logger.Config.Debug("Clearing UserConfig cache");
#endif
                _Cache.Clear();
            }
        }
    }
}
