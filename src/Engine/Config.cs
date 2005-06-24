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
using System.IO;
using System.Collections;
#if CONFIG_NINI
using Nini.Ini;
#endif

namespace Meebey.Smuxi.Engine
{
    public class Config : PermanentRemoteObject
    {
        //private   int           _PreferencesVersion = 0;
#if CONFIG_GCONF
        private   GConf.Client  _GConf     = new GConf.Client();
#elif CONFIG_NINI
        private   IniDocument   _IniDocument;
        protected string        _IniFilename = "smuxi.ini";
#endif
        protected Hashtable     _Preferences = Hashtable.Synchronized(new Hashtable());

        public object this[string key] {
            get {
                return _Preferences[key];
            }
            set {
                _Preferences[key] = value;
            }
        }

        public Config()
        {
#if CONFIG_NINI
            // doesn't work
            // using (FileInfo fi = new FileInfo(_IniFilename)) {
            FileInfo fi = new FileInfo(_IniFilename);
                if (!fi.Exists) {
                    fi.Create();
                }
            //}
            // bug here? when the file got created, we get a "Sharing Violation"
            _IniDocument = new IniDocument(_IniFilename);
#endif
       }

       protected object _Get(string key, object defaultvalue)
       {
#if LOG4NET
            Logger.Config.Debug("_Get() key: '"+key+"' defaultvalue: '"+(defaultvalue != null ? defaultvalue : "(null)")+"'");
#endif
#if CONFIG_GCONF
            try {
                return _GConf.Get("/apps/smuxi/"+key);
            } catch (GConf.NoSuchKeyException) {
                if (defaultvalue != null) {
                    _Set(key, defaultvalue);
                }
                return defaultvalue;
            }
#elif CONFIG_NINI
            string inisection = _IniGetSection(key);
            string inikey = _IniGetKey(key);
            IniSection section = _IniDocument.Sections[inisection];
            if ((section == null) ||
                (!section.Contains(inikey))) {
                if (defaultvalue != null) {
                    _Set(key, defaultvalue);
                }
               return defaultvalue;
            } else {
                // the section and key exist
                // since INI files are plain text, all data will be string,
                // must convert here when possible (via guessing)
                object obj = section.GetValue(inikey);
                try {
                    int number = Int32.Parse((string)obj);
                    return number;
                } catch (FormatException) {
                }
                
                try {
                    bool boolean = Boolean.Parse((string)obj);
                    return boolean;
                } catch (FormatException) {
                }
                
                // no convert worked, let's leave it as string
                return obj;
            }
#endif
       }

        protected string[] _GetList(string key)
        {
            string[] result = null;
#if CONFIG_GCONF
            // Gconf# bug, it doesn't like empty string lists.
            result = (string[])_Get(key, new string[] {""});
#elif CONFIG_NINI
            // Nini does not support native string lists, have to emulate them
            string result_str = (string)_Get(key, null);
            if (result_str != null) {
                result = result_str.Split('|');
            }
#endif
            return result;
        }
        
        private void _Set(string key, object valueobj)
        {
#if LOG4NET
            Logger.Config.Debug("_Set() key: '"+key+"' valueobj: '"+(valueobj != null ? valueobj : "(null)")+"'");
#endif
#if CONFIG_GCONF
            _GConf.Set("/apps/smuxi/"+key, valueobj);
#elif CONFIG_NINI
            string inisection = _IniGetSection(key);
            string inikey = _IniGetKey(key);
            IniSection section = _IniDocument.Sections[inisection];
            if (section == null) {
                _IniDocument.Sections.Add(new IniSection(inisection));
                section = _IniDocument.Sections[inisection];
            }

            if (valueobj is string[]) {
                // Nini does not support native string lists, have to emulate them
                section.Set(inikey, String.Join("|", (string[])valueobj));
            } else {
                section.Set(inikey, valueobj.ToString());
            } 
#endif
        }

        public void Load()
        {
#if LOG4NET
            Logger.Config.Info("Loading config (Config)");
#endif
            string prefix;
            
            prefix = "Server/";
            _LoadEntry(prefix+"Host", null);
            _LoadEntry(prefix+"Port", 7689);
            _LoadEntry(prefix+"Formatter", "binary");
            _LoadEntry(prefix+"Channel", "TCP");

            prefix = "Engine/Users/";
            string[] users = _GetList(prefix+"Users");
            if (users != null) {
                _Preferences[prefix+"Users"] = users;
            } else {
                users = new string[] {""};
            }
            foreach (string user in users) {
                if (user.Length == 0) {
                    continue;
                }
                
                _LoadUserEntry(user, "Password", "smuxi");
                string[] nick_list = _GetList(prefix+user+"/Connection/Nicknames");
                if (nick_list != null) {
                    _Preferences[prefix+user+"/Connection/Nicknames"] = nick_list;
                } else {
                    _Preferences[prefix+user+"/Connection/Nicknames"] = new string[] {"Smuxi", "Smuxi_"};
                }
                _LoadUserEntry(user, "Connection/Username", "");
                _LoadUserEntry(user, "Connection/Realname", "http://smuxi.meebey.net");
                string[] command_list = _GetList(prefix+user+"/Connection/OnConnectCommands");
                if (command_list != null) {
                    _Preferences[prefix+user+"/Connection/OnConnectCommands"] = command_list;
                } else {
                    _Preferences[prefix+user+"/Connection/OnConnectCommands"] = new string[] {""};
                }
                _LoadUserEntry(user, "Interface/Notebook/TimestampFormat", null);
                _LoadUserEntry(user, "Interface/Notebook/TabPosition", null);
                _LoadUserEntry(user, "Interface/Notebook/BufferLines", null);
                _LoadUserEntry(user, "Interface/Notebook/EngineBufferLines", null);
                _LoadUserEntry(user, "Interface/Notebook/Channel/UserListPosition", null);
                _LoadUserEntry(user, "Interface/Notebook/Channel/TopicPosition", null);
                _LoadUserEntry(user, "Interface/Entry/CompletionCharacter", null);
                _LoadUserEntry(user, "Interface/Entry/CommandCharacter", null);
                _LoadUserEntry(user, "Interface/Entry/BashStyleCompletion", null);
                _LoadUserEntry(user, "Interface/Entry/CommandHistorySize", null);
                
                string[] servers = null;
                servers = _GetList(prefix+"Servers/Servers");
                foreach (string server in servers) {
                    if (server.Length == 0) {
                        continue;
                    }
                    string sprefix = prefix+user+"/Servers/"+server+"/";
                    _LoadEntry(sprefix+"Hostname", null);
                    _LoadEntry(sprefix+"Port", null);
                    _LoadEntry(sprefix+"Network", null);
                    _LoadEntry(sprefix+"Username", null);
                    _LoadEntry(sprefix+"Password", null);
                }                
            }
        }

        public void Save()
        {
#if LOG4NET
            Logger.Config.Info("Saving config (Config)");
#endif

            foreach (string key in _Preferences.Keys) {
                object obj = _Preferences[key];
                _Set(key, obj);
            }

#if CONFIG_GCONF
            _GConf.SuggestSync();
#elif CONFIG_NINI
            _IniDocument.Save(_IniFilename);
#endif
        }

        private void _LoadUserEntry(string user, string key, object defaultvalue)
        {
#if LOG4NET
            Logger.Config.Debug("_LoadUserEntry() user: '"+user+"' key: '"+key+"' defaultvalue: '"+(defaultvalue != null ? defaultvalue : "(null)")+"'");
#endif
            string prefix = "Engine/Users/";
            string ukey = prefix+user+"/"+key;
            string dkey = prefix+"DEFAULT/"+key;
            object obj;
            if (defaultvalue == null) {
                // BUG: this will set the value of the Users/DEFAULT/* value!
                obj = _Get(ukey, _Get(dkey, null));
            } else {
                obj = _Get(ukey, defaultvalue);
            }
            
            if (obj != null) {
                _Preferences[ukey] = obj;
            }
        }
        
        protected void _LoadEntry(string key, object defaultvalue)
        {
#if LOG4NET
            Logger.Config.Debug("_LoadEntry() key: '"+key+"' defaultvalue: '"+(defaultvalue != null ? defaultvalue : "(null)")+"'");
#endif
            object obj = _Get(key, defaultvalue);
            if (obj != null) {
                _Preferences[key] = obj;             
            }
        }
        
        protected void _LoadAllEntries(string basepath)
        {
#if LOG4NET
            Logger.Config.Debug("_LoadAllEntries() basepath: '"+basepath+"'");
#endif
#if CONFIG_GCONF
#elif CONFIG_NINI
            foreach (DictionaryEntry dec in _IniDocument.Sections) {
                IniSection inisection = (IniSection)dec.Value;
                if (inisection.Name.StartsWith(basepath)) {
                    foreach (string key in inisection.GetKeys()) {
                        _Preferences[inisection.Name+"/"+key] = inisection.GetValue(key);
                    }
                }
            }
#endif
        }
        
#if CONFIG_NINI
        private string _IniGetKey(string key)
        {
            string[] keys = key.Split(new char[] {'/'});
            // nothing but the last part
            string inikey = String.Join("/", keys, keys.Length - 1, 1);
            return inikey;
        }

        private string _IniGetSection(string key)
        {
            string[] keys = key.Split(new char[] {'/'});
            // everything except the last part
            string inisection = String.Join("/", keys, 0, keys.Length - 1);
            return inisection;
        }
#endif
    }
}
