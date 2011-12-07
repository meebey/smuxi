/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
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
using System.IO;
using System.Collections;
using Mono.Unix.Native;
#if CONFIG_NINI
using Nini.Config;
using Nini.Ini;
#endif
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class Config : PermanentRemoteObject
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        //protected int           m_PreferencesVersion = 0;
#if CONFIG_GCONF
        private   GConf.Client  _GConf = new GConf.Client();
        private   string        _GConfPrefix = "/apps/smuxi/";
#elif CONFIG_NINI
        protected string        m_ConfigPath;
        protected IniDocument   m_IniDocument;
        //protected IConfigSource m_IniConfigSource;
        //protected IConfig       m_IniConfig;
        protected string        m_IniFilename;
#endif
        protected bool          m_IsCleanConfig;
        protected Hashtable     m_Preferences = Hashtable.Synchronized(new Hashtable());
        public event EventHandler<ConfigChangedEventArgs> Changed;
        
        public object this[string key] {
            get {
                return m_Preferences[key];
            }
            set {
                if (value == null) {
#if LOG4NET
                    _Logger.Error("Passed null to indexer with key: " + key + ", ignored.");
#endif
                    return;
                }
                var oldValue = m_Preferences[key];
                m_Preferences[key] = value;

                // only raise event if the value changed
                if (!value.Equals(oldValue)) {
                    if (Changed != null) {
                        Changed(this, new ConfigChangedEventArgs(key, value));
                    }
                }
            }
        }
        
        public bool IsCleanConfig {
            get {
                return m_IsCleanConfig;
            }
        }

        public Config()
        {
#if CONFIG_NINI
            m_ConfigPath = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "smuxi");
            
            if (!Directory.Exists(m_ConfigPath)) {
                Directory.CreateDirectory(m_ConfigPath);
            }
            
            m_IniFilename = Path.Combine(m_ConfigPath, "smuxi-engine.ini");
            if (!File.Exists(m_IniFilename)) {
#if LOG4NET
                _Logger.Debug("creating file: "+m_IniFilename);
#endif
                File.Create(m_IniFilename).Close();
                m_IsCleanConfig = true;
            }
            
            m_IniDocument = new IniDocument(m_IniFilename);
            //m_IniConfigSource = new IniConfigSource(m_IniFilename);
#endif
        }
        
        protected T Get<T>(string key, T defaultvalue)
        {
            Trace.Call(key, defaultvalue);
            
            string inisection = _IniGetSection(key);
            string inikey = _IniGetKey(key);
            IniSection section = m_IniDocument.Sections[inisection];
            if ((section == null) ||
                (!section.Contains(inikey))) {
                if (defaultvalue != null) {
                    _Set(key, defaultvalue);
                }
                return defaultvalue;
            } else {
                // the section and key exist
                string strValue = section.GetValue(inikey);
                Type targetType = typeof(T);
                if (targetType == typeof(string)) {
                    return (T)(object) strValue;
                }
                if (targetType == typeof(string[])) {
                    return (T)(object) GetList(key);
                }
                // handle empty booleans and integers
                if (targetType.IsValueType && String.IsNullOrEmpty(strValue)) {
                    return default(T);
                }

                return (T) Convert.ChangeType(strValue, targetType);
            }
       }

        protected object Get(string key, object defaultvalue)
        {
            Trace.Call(key, defaultvalue);

#if CONFIG_GCONF
            try {
                return _GConf.Get(_GConfPrefix+key);
            } catch (GConf.NoSuchKeyException) {
                if (defaultvalue != null) {
                    _Set(key, defaultvalue);
                }
                return defaultvalue;
            }
#elif CONFIG_NINI
            string inisection = _IniGetSection(key);
            string inikey = _IniGetKey(key);
            IniSection section = m_IniDocument.Sections[inisection];
            if ((section == null) ||
                (!section.Contains(inikey))) {
                if (defaultvalue != null) {
                    _Set(key, defaultvalue);
                }
                return defaultvalue;
            } else {
                // the section and key exist
                return _Parse(section.GetValue(inikey));
            }
#endif
        }

        protected string[] GetList(string key)
        {
            string[] result = null;
#if CONFIG_GCONF
            // Gconf# bug, it doesn't like empty string lists.
            result = (string[])Get(key, new string[] { String.Empty });
            if (result.Length == 1 && result[0] == String.Empty) {
                // don't return workaround list, instead a clean empty list
                result = new string[] {};
            }
            
#elif CONFIG_NINI
            // Nini does not support native string lists, have to emulate them
            string result_str = Get<string>(key, null);
            if (result_str != null) {
                if (result_str.Length > 0) {
                    result = result_str.Split('|');
                } else {
                    result = new string[] {};
                }
            }
#endif
            return result;
        }
        
        private void _Set(string key, object valueobj)
        {
            Trace.Call(key, valueobj);
            
#if CONFIG_GCONF
            _GConf.Set(_GConfPrefix+key, valueobj);
#elif CONFIG_NINI
            string inisection = _IniGetSection(key);
            string inikey = _IniGetKey(key);
            IniSection section = m_IniDocument.Sections[inisection];
            if (section == null) {
                m_IniDocument.Sections.Add(new IniSection(inisection));
                section = m_IniDocument.Sections[inisection];
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
            Trace.Call();

#if LOG4NET
            _Logger.Debug("Loading config");
#endif
            string prefix;
            
            // setting required default values
            prefix = "Server/";
            Get(prefix+"BindAddress", "127.0.0.1");
            Get(prefix+"Port", 7689);
            Get(prefix+"Channel", "TCP");
            Get(prefix+"Formatter", "binary");
            
            prefix = "Engine/Users/DEFAULT/Interface/";
            Get(prefix+"ShowAdvancedSettings", false);

            prefix = "Engine/Users/DEFAULT/Interface/Notebook/";
            Get(prefix+"TimestampFormat", "HH:mm");
            Get(prefix+"TabPosition", "top");
            Get(prefix+"BufferLines", 500);
            Get(prefix+"EngineBufferLines", 100);
            Get(prefix+"StripColors", false);
            Get(prefix+"StripFormattings", false);
            
            prefix = "Engine/Users/DEFAULT/Interface/Notebook/Tab/";
            Get(prefix+"NoActivityColor", "#000000");
            Get(prefix+"ActivityColor",   "#0080FF");
            Get(prefix+"EventColor",      "#2020C0");
            Get(prefix+"HighlightColor",  "#E80000");
            
            prefix = "Engine/Users/DEFAULT/Interface/Notebook/Channel/";
            Get(prefix+"UserListPosition", "left");
            Get(prefix+"TopicPosition", "top");
            Get(prefix+"NickColors", true);

            prefix = "Engine/Users/DEFAULT/Interface/Chat/";
            Get(prefix+"BackgroundColor", String.Empty);
            Get(prefix+"ForegroundColor", String.Empty);
            Get(prefix+"FontFamily", String.Empty);
            Get(prefix+"FontStyle",  String.Empty);
            Get(prefix+"FontSize",   0);
            Get(prefix+"WrapMode",   "Word");
            
            prefix = "Engine/Users/DEFAULT/Interface/Entry/";
            Get(prefix+"CompletionCharacter", ":");
            Get(prefix+"CommandCharacter", "/");
            Get(prefix+"BashStyleCompletion", false);
            Get(prefix+"CommandHistorySize", 30);

            prefix = "Engine/Users/DEFAULT/Interface/Notification/";
            Get(prefix+"NotificationAreaIconMode", "Never");
            Get(prefix+"MessagingMenuEnabled", true);
            Get(prefix+"PopupsEnabled", true);

            prefix = "Engine/Users/DEFAULT/Sound/";
            Get(prefix+"BeepOnHighlight", false);
            
            prefix = "Engine/Users/DEFAULT/Connection/";
            Get(prefix+"Encoding", String.Empty);
            Get(prefix+"ProxyType", "System");
            Get(prefix+"ProxyHostname", String.Empty);
            Get(prefix+"ProxyPort", -1);
            Get(prefix+"ProxyUsername", String.Empty);
            Get(prefix+"ProxyPassword", String.Empty);

            prefix = "Engine/Users/DEFAULT/Logging/";
            Get(prefix+"Enabled", false);
            Get(prefix+"LogFilteredMessages", false);

            prefix = "Engine/Users/DEFAULT/MessageBuffer/";
            Get(prefix+"PersistencyType", "Volatile");
            prefix = "Engine/Users/DEFAULT/MessageBuffer/Volatile/";
            Get(prefix+"MaxCapacity", 200);
            prefix = "Engine/Users/DEFAULT/MessageBuffer/Persistent/";
            Get(prefix+"MaxCapacity", 50 * 1000);

            prefix = "Engine/Users/DEFAULT/Servers/";
            Get(prefix + "Servers", new string[] {
                "IRC/irc.oftc.net",
                "IRC/irc.gimp.org",
                "IRC/irc.efnet.org",
                "IRC/irc.ircnet.org",
                "IRC/irc.freenode.net"
            });
            
            prefix = "Engine/Users/DEFAULT/Servers/IRC/irc.oftc.net/";
            Get(prefix + "Hostname", "irc.oftc.net");
            Get(prefix + "Port", 6667);
            Get(prefix + "Network", "OFTC");
            Get(prefix + "Username", String.Empty);
            Get(prefix + "Password", String.Empty);
            Get(prefix + "UseEncryption", false);
            Get(prefix + "ValidateServerCertificate", false);
            Get(prefix + "OnStartupConnect", true);
            Get(prefix + "OnConnectCommands",
                new string[] {
                    "/join #smuxi",
                }
            );
            
            prefix = "Engine/Users/DEFAULT/Servers/IRC/irc.gimp.org/";
            Get(prefix + "Hostname", "irc.gimp.org");
            Get(prefix + "Port", 6667);
            Get(prefix + "Network", "GIMPNet");
            Get(prefix + "Username", String.Empty);
            Get(prefix + "Password", String.Empty);
            Get(prefix + "UseEncryption", false);
            Get(prefix + "ValidateServerCertificate", false);

            prefix = "Engine/Users/DEFAULT/Servers/IRC/irc.efnet.org/";
            Get(prefix + "Hostname", "irc.efnet.org");
            Get(prefix + "Port", 6667);
            Get(prefix + "Network", "EFnet");
            Get(prefix + "Username", String.Empty);
            Get(prefix + "Password", String.Empty);
            Get(prefix + "UseEncryption", false);
            Get(prefix + "ValidateServerCertificate", false);
                
            prefix = "Engine/Users/DEFAULT/Servers/IRC/irc.ircnet.org/";
            Get(prefix + "Hostname", "irc.ircnet.org");
            Get(prefix + "Port", 6667);
            Get(prefix + "Network", "IRCnet");
            Get(prefix + "Username", String.Empty);
            Get(prefix + "Password", String.Empty);
            Get(prefix + "UseEncryption", false);
            Get(prefix + "ValidateServerCertificate", false);
                
            prefix = "Engine/Users/DEFAULT/Servers/IRC/irc.freenode.net/";
            Get(prefix + "Hostname", "irc.freenode.net");
            Get(prefix + "Port", 6667);
            Get(prefix + "Network", "freenode");
            Get(prefix + "Username", String.Empty);
            Get(prefix + "Password", String.Empty);
            Get(prefix + "UseEncryption", false);
            Get(prefix + "ValidateServerCertificate", false);
            
            prefix = "Engine/Users/";
            Get(prefix+"Users", new string[] { "local" });
            
            /*
            prefix = "Engine/Users/local/";
            Get(prefix+"Password", String.Empty);

            prefix = "Engine/Users/local/Servers/";
            Get(prefix+"Servers", new string[] {});
            */
            
            prefix = "Server/";
            LoadEntry(prefix+"Port", 7689);
            LoadEntry(prefix+"Formatter", "binary");
            LoadEntry(prefix+"Channel", "TCP");
            LoadEntry(prefix+"BindAddress", null);

            // loading defaults
            LoadAllEntries("Engine/Users/DEFAULT");
                    
            prefix = "Engine/Users/";
            string[] users = GetList(prefix+"Users");
            m_Preferences[prefix + "Users"] = users;
            foreach (string user in users) {
                LoadUserEntry(user, "Password", "smuxi");
                
                string[] startup_commands = GetList(prefix+user+"/OnStartupCommands");
                if (startup_commands != null) {
                    m_Preferences[prefix+user+"/OnStartupCommands"] = startup_commands;
                } else {
                    m_Preferences[prefix+user+"/OnStartupCommands"] = new string[] {};
                }
                
                string[] nick_list = GetList(prefix+user+"/Connection/Nicknames");
                if (nick_list != null) {
                    m_Preferences[prefix+user+"/Connection/Nicknames"] = nick_list;
                } else {
                    string nick = Environment.UserName;
                    // clean typical disallowed characters
                    nick = nick.Replace(" ", String.Empty);
                    if (String.IsNullOrEmpty(nick)) {
                        nick = "Smuxi";
                    }
                    m_Preferences[prefix+user+"/Connection/Nicknames"] = new string[] { nick };
                }
                
                LoadUserEntry(user, "Connection/Username", String.Empty);
                string realname = null;
                try {
                    string gecos = Mono.Unix.UnixUserInfo.GetRealUser().RealName;
                    if (gecos == null) {
                        gecos = String.Empty;
                    }
                    int pos = gecos.IndexOf(",");
                    if (pos != -1) {
                        realname = gecos.Substring(0, pos);
                    } else {
                        realname = gecos;
                    }
                } catch (Exception ex) {
#if LOG4NET
                    _Logger.Warn("Load(): error getting realname from gecos (ignoring)", ex);
#endif
                }
                if (String.IsNullOrEmpty(realname)) {
                    realname = "http://www.smuxi.org/";
                }
                LoadUserEntry(user, "Connection/Realname", realname);
                LoadUserEntry(user, "Connection/Encoding", String.Empty);

                LoadUserEntry(user, "Connection/ProxyType", "System");
                LoadUserEntry(user, "Connection/ProxyHostname", String.Empty);
                LoadUserEntry(user, "Connection/ProxyPort", -1);
                LoadUserEntry(user, "Connection/ProxyUsername", null);
                LoadUserEntry(user, "Connection/ProxyPassword", null);

                string[] command_list = GetList(prefix+user+"/Connection/OnConnectCommands");
                if (command_list != null) {
                    m_Preferences[prefix+user+"/Connection/OnConnectCommands"] = command_list;
                } else {
                    m_Preferences[prefix+user+"/Connection/OnConnectCommands"] = new string[] {};
                }
                
                string[] highlight_words = GetList(prefix+user+"/Interface/Chat/HighlightWords");
                if (highlight_words != null) {
                    m_Preferences[prefix+user+"/Interface/Chat/HighlightWords"] = highlight_words;
                } else {
                    m_Preferences[prefix+user+"/Interface/Chat/HighlightWords"] = new string[] {};
                }

                LoadUserEntry(user, "Interface/ShowAdvancedSettings", null);
                LoadUserEntry(user, "Interface/Notebook/TimestampFormat", null);
                LoadUserEntry(user, "Interface/Notebook/TabPosition", null);
                LoadUserEntry(user, "Interface/Notebook/BufferLines", null);
                LoadUserEntry(user, "Interface/Notebook/EngineBufferLines", null);
                LoadUserEntry(user, "Interface/Notebook/StripColors", null);
                LoadUserEntry(user, "Interface/Notebook/StripFormattings", null);
                LoadUserEntry(user, "Interface/Notebook/Tab/NoActivityColor", null);
                LoadUserEntry(user, "Interface/Notebook/Tab/ActivityColor", null);
                LoadUserEntry(user, "Interface/Notebook/Tab/EventColor", null);
                LoadUserEntry(user, "Interface/Notebook/Tab/HighlightColor", null);
                LoadUserEntry(user, "Interface/Notebook/Channel/UserListPosition", null);
                LoadUserEntry(user, "Interface/Notebook/Channel/TopicPosition", null);
                LoadUserEntry(user, "Interface/Notebook/Channel/NickColors", null);
                LoadUserEntry(user, "Interface/Chat/ForegroundColor", null);
                LoadUserEntry(user, "Interface/Chat/BackgroundColor", null);
                LoadUserEntry(user, "Interface/Chat/FontFamily", null);
                LoadUserEntry(user, "Interface/Chat/FontStyle", null);
                LoadUserEntry(user, "Interface/Chat/FontSize", null);
                LoadUserEntry(user, "Interface/Chat/WrapMode", null);
                LoadUserEntry(user, "Interface/Entry/CompletionCharacter", null);
                LoadUserEntry(user, "Interface/Entry/CommandCharacter", null);
                LoadUserEntry(user, "Interface/Entry/BashStyleCompletion", null);
                LoadUserEntry(user, "Interface/Entry/CommandHistorySize", null);
                LoadUserEntry(user, "Interface/Notification/NotificationAreaIconMode", null);
                LoadUserEntry(user, "Interface/Notification/MessagingMenuEnabled", null);
                LoadUserEntry(user, "Interface/Notification/PopupsEnabled", null);
                
                LoadUserEntry(user, "Sound/BeepOnHighlight", null);
                
                LoadUserEntry(user, "Logging/Enabled", null);
                LoadUserEntry(user, "Logging/LogFilteredMessages", null);

                LoadUserEntry(user, "MessageBuffer/PersistencyType", null);
                LoadUserEntry(user, "MessageBuffer/Volatile/MaxCapacity", null);
                LoadUserEntry(user, "MessageBuffer/Persistent/MaxCapacity", null);

                string[] servers = null;
                string sprefix = prefix + user + "/Servers/";
                servers = GetList(sprefix + "Servers");
                if (servers == null) {
                    // this user has no servers
                    string dprefix = prefix + "DEFAULT/Servers/";
                    servers = GetList(dprefix + "Servers");
                    if (servers == null) {
                        // no default servers, use empty list
                        servers = new string[] {};
                    } else {
                        // we have default servers, so lets copy them
                        foreach (string server in servers) {
                            LoadEntry(sprefix + server + "/Hostname",
                                      Get(dprefix + server + "/Hostname", null));
                            LoadEntry(sprefix + server + "/Port",
                                      Get(dprefix + server + "/Port", null));
                            LoadEntry(sprefix + server + "/Network",
                                      Get(dprefix + server + "/Network", null));
                            LoadEntry(sprefix + server + "/Encoding",
                                      Get(dprefix + server + "/Encoding", null));
                            LoadEntry(sprefix + server + "/Username",
                                      Get(dprefix + server + "/Username", null));
                            LoadEntry(sprefix + server + "/Password",
                                      Get(dprefix + server + "/Password", null));
                            LoadEntry(sprefix + server + "/UseEncryption",
                                      Get(dprefix + server + "/UseEncryption", null));
                            LoadEntry(sprefix + server + "/ValidateServerCertificate",
                                      Get(dprefix + server + "/ValidateServerCertificate", null));
                            LoadEntry(sprefix + server + "/OnStartupConnect",
                                      Get(dprefix + server + "/OnStartupConnect", null));
                            LoadEntry(sprefix + server + "/OnConnectCommands",
                                      Get(dprefix + server + "/OnConnectCommands", null));
                        }
                    }
                    m_Preferences[sprefix + "Servers"] = servers;
                } else {
                    // this user has servers
                    m_Preferences[sprefix + "Servers"] = servers;
                }
                foreach (string server in servers) {
                    sprefix = prefix + user + "/Servers/" + server + "/";
                    LoadEntry(sprefix+"Hostname", null);
                    LoadEntry(sprefix+"Port", null);
                    LoadEntry(sprefix+"Network", String.Empty);
                    LoadEntry(sprefix+"Encoding", null);
                    LoadEntry(sprefix+"Username", String.Empty);
                    LoadEntry(sprefix+"Password", String.Empty);
                    LoadEntry(sprefix+"UseEncryption", false);
                    LoadEntry(sprefix+"ValidateServerCertificate", false);
                    LoadEntry(sprefix+"OnStartupConnect", false);
                    string[] commands = GetList(sprefix + "OnConnectCommands");
                    if (commands == null) {
                        commands = new string[] {};
                        m_Preferences[sprefix + "OnConnectCommands"] = new string[] {};
                    } else {
                        m_Preferences[sprefix + "OnConnectCommands"] = commands;
                    }
                }

                string[] filters = null;
                string cprefix = "Filters/";
                filters = GetList(prefix + user + "/" + cprefix + "Filters");
                if (filters == null) {
                    filters = new string[] {};
                    m_Preferences[prefix + user + "/" + cprefix + "Filters"] = new string[] {};
                } else {
                    m_Preferences[prefix + user + "/" + cprefix + "Filters"] = filters;
                }
                foreach (string filter in filters) {
                    cprefix = "Filters/" + filter + "/";
                    LoadUserEntry(user, cprefix + "Protocol", null);
                    LoadUserEntry(user, cprefix + "ChatType", null);
                    LoadUserEntry(user, cprefix + "ChatID", null);
                    LoadUserEntry(user, cprefix + "MessageType", null);
                    LoadUserEntry(user, cprefix + "MessagePattern", null);
                }
            }
        }

        public void Save()
        {
            Trace.Call();

#if LOG4NET
            _Logger.Debug("Saving config");
#endif
            
            // update values in backend
            foreach (string key in m_Preferences.Keys) {
                object obj = m_Preferences[key];
                _Set(key, obj);
            }
            
#if CONFIG_GCONF
            _GConf.SuggestSync();
#elif CONFIG_NINI
//            StreamWriter sr = File.CreateText(m_IniFilename);
//            m_IniDocument.Save(sr);
            m_IniDocument.Save(m_IniFilename);
#endif
        }
        
        public void Remove(string key)
        {
            Trace.Call(key);
            
            bool isSection = false;
            if (key.EndsWith("/")) {
                isSection = true;
                ArrayList keys = new ArrayList(m_Preferences.Keys);
                foreach (string pkey in keys) {
                    if (pkey.StartsWith(key)) {
                        m_Preferences.Remove(pkey);
                    }
                }
            } else {
                m_Preferences.Remove(key);
            }
#if CONFIG_GCONF
            //_GConf.
#elif CONFIG_NINI
            string iniSection = _IniGetSection(key);
            string iniKey = _IniGetKey(key);
            if (isSection) {
                m_IniDocument.Sections.Remove(iniSection);
            } else {
                if (m_IniDocument.Sections[key] == null) {
                    return;
                }
                m_IniDocument.Sections[key].Remove(key);
            }
#endif
            
            if (Changed != null) {
                Changed(this, new ConfigChangedEventArgs(key, null));
            }
        }

        protected void LoadUserEntry(string user, string key, object defaultvalue)
        {
            Trace.Call(user, key, defaultvalue);
            
            string prefix = "Engine/Users/";
            string ukey = prefix+user+"/"+key;
            object obj = Get(ukey, defaultvalue);
            if (obj != null) {
                m_Preferences[ukey] = obj;
            }
        }
        
        protected void LoadEntry(string key, object defaultvalue)
        {
            Trace.Call(key, defaultvalue);
            
            object obj;
            if (defaultvalue is string) {
                obj = Get<string>(key, (string) defaultvalue);
            } else {
                obj = Get(key, defaultvalue);
            }
            if (obj != null) {
                m_Preferences[key] = obj;             
            }
        }
        
        protected void LoadAllEntries(string basepath)
        {
            Trace.Call(basepath);
            
#if CONFIG_GCONF
            // TODO: GConf# has no way yet to get the sub-paths of a given path!
            // So we have to use Nini as primary config backend for now...
#elif CONFIG_NINI
            foreach (DictionaryEntry dec in m_IniDocument.Sections) {
                IniSection inisection = (IniSection)dec.Value;
                if (inisection.Name.StartsWith(basepath)) {
                    foreach (string key in inisection.GetKeys()) {
                        m_Preferences[inisection.Name+"/"+key] = _Parse(inisection.GetValue(key));
                    }
                }
            }
#endif
        }
        
#if CONFIG_NINI
        private object _Parse(string data)
        {
            // since INI files are plain text, all data will be string,
            // must convert here when possible (via guessing)
            try {
                int number = Int32.Parse(data);
                return number;
            } catch (FormatException) {
            }

            try {
                bool boolean = Boolean.Parse(data);
                return boolean;
            } catch (FormatException) {
            }

            // no convert worked, let's leave it as string
            return data;
        }

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

    public class ConfigChangedEventArgs : EventArgs
    {
        public string Key { get; private set; }
        public object Value { get; private set; }

        public ConfigChangedEventArgs(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}
