/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006, 2010-2011, 2013 Mirco Bauer <meebey@meebey.net>
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
using System.Runtime.Remoting;
using System.Collections;
using System.Collections.Generic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class UserConfig : PermanentRemoteObject, IEnumerable<KeyValuePair<string, object>>
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Config    _Config;
        private string    _UserPrefix;
        private string    _DefaultPrefix = "Engine/Users/DEFAULT/";
        private Hashtable _Cache; 
        
        public event EventHandler<ConfigChangedEventArgs> Changed;

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
                obj = _Config[_UserPrefix + key];
                if (obj != null) {
                    if (IsCaching) {
                        _Cache.Add(key, obj);
                    }
                    return obj;
                }
                
                obj = _Config[_DefaultPrefix + key];
#if LOG4NET
                if (obj == null) {
                    _Logger.Error("get_Item[]: default value is null for key: " + key);
                }
#endif
                if (IsCaching) {
                    _Cache.Add(key, obj);
                }

                return obj;
            }
            set {
                // TODO: make remoting calls after a timeout and batch the update
                _Config[_UserPrefix + key] = value;
                
                // update entry in cache
                if (IsCaching) {
                    _Cache[key] = value;
                }
            }
        }
        
        public UserConfig(Config config, string username)
        {
            _Config = config;
            // HACK: The Changed event was introduced in 0.7.2, for backwards
            // compatibility with 0.7.x server we need to suppress remoting
            // exceptions here
            try {
                // we can't use events over remoting
                if (!RemotingServices.IsTransparentProxy(config)) {
                    _Config.Changed += OnConfigChanged;
                }
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Warn(
                    "UserConfig() registration of Config.Changed event failed, " +
                    "ignoring for backwards compatibility with 0.7.x servers...",
                    ex
                );
#endif
            }
            _UserPrefix = "Engine/Users/"+username+"/";
        }

        public void ClearCache()
        {
            if (IsCaching) {
#if LOG4NET
                _Logger.Debug("Clearing cache");
#endif
                _Cache.Clear();
            }
        }
        
        public void Remove(string key)
        {
            _Config.Remove(_UserPrefix + key);

            if (!IsCaching) {
                return;
            }
            if (key.EndsWith("/")) {
                // invalidate all cache keys of this section
                var cachedKeys = new List<string>();
                foreach (string cacheKey in _Cache.Keys) {
                    if (cacheKey.StartsWith(key)) {
                        cachedKeys.Add(cacheKey);
                    }
                }
                foreach (string cacheKey in cachedKeys) {
                    _Cache.Remove(cacheKey);
                }
            } else {
                // deleting the single entry is enough
                _Cache.Remove(key);
            }
        }
        
        public void Save()
        {
            _Config.Save();
        }

        public void SyncCache()
        {
            Trace.Call();

            if (!IsCaching) {
                return;
            }

            var start = DateTime.UtcNow;
            var conf = _Config.GetAll();
            var cache = new Hashtable(conf.Count);
            foreach (var entry in conf) {
                if (!entry.Key.StartsWith(_UserPrefix)) {
                    // no need to cache values of other users
                    continue;
                }
                // remove user prefix from key
                var userKey = entry.Key.Substring(_UserPrefix.Length);
                cache.Add(userKey, entry.Value);
            }
            var stop = DateTime.UtcNow;
#if LOG4NET
            _Logger.Debug(
                String.Format(
                    "SyncCache(): syncing config took: {0:0.00} ms",
                    (stop - start).TotalMilliseconds
                )
            );
#endif
            _Cache = cache;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var entry in _Config.GetAll()) {
                if (!entry.Key.StartsWith(_UserPrefix)) {
                    continue;
                }
                // remove user prefix from key
                var userKey = entry.Key.Substring(_UserPrefix.Length);
                yield return new KeyValuePair<string, object>(userKey, entry.Value);
            }
        }

        void OnConfigChanged(object sender, ConfigChangedEventArgs e)
        {
            if (Changed == null) {
                // no listeners
                return;
            }

            if (!e.Key.StartsWith(_UserPrefix)) {
                // setting for some other user has changed
                return;
            }

            var key = e.Key.Substring(_UserPrefix.Length);
            Changed(this, new ConfigChangedEventArgs(key, e.Value));
        }
    }
}
