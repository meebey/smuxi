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
using System.IO;
using System.Collections;
#if CONFIG_NINI
using Nini.Ini;
#endif
using Meebey.Smuxi.Common;

namespace Meebey.Smuxi.Engine
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
        protected string        m_IniFilename;
#endif
        protected bool          m_IsCleanConfig;
        protected Hashtable     m_Preferences = Hashtable.Synchronized(new Hashtable());
        public event EventHandler Changed;
        
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
                m_Preferences[key] = value;
                if (Changed != null) {
                    Changed(this, EventArgs.Empty);
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
            
//            StreamReader sr = File.OpenText(m_IniFilename);
//            m_IniDocument = new IniDocument(sr);
            m_IniDocument = new IniDocument(m_IniFilename);
#endif
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
            string result_str = (string)Get(key, null);
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
#if LOG4NET
            _Logger.Info("Loading config");
#endif
            string prefix;
            
            // setting required default values
            prefix = "Server/";
            Get(prefix+"Port", 7689);
            Get(prefix+"Channel", "TCP");
            Get(prefix+"Formatter", "binary");
            
            prefix = "Engine/Users/DEFAULT/Interface/Notebook/";
            Get(prefix+"TimestampFormat", "HH:mm");
            Get(prefix+"TabPosition", "top");
            Get(prefix+"BufferLines", 100);
            Get(prefix+"EngineBufferLines", 100);
            Get(prefix+"StripColors", false);
            Get(prefix+"StripFormattings", false);
            
            prefix = "Engine/Users/DEFAULT/Interface/Notebook/Tab/";
            Get(prefix+"NoActivityColor", "#000000");
            Get(prefix+"ActivityColor",   "#0080FF");
            Get(prefix+"ModeColor",       "#2020C0");
            Get(prefix+"HighlightColor",  "#E80000");
            
            prefix = "Engine/Users/DEFAULT/Interface/Notebook/Channel/";
            Get(prefix+"UserListPosition", "left");
            Get(prefix+"TopicPosition", "top");

            prefix = "Engine/Users/DEFAULT/Interface/Entry/";
            Get(prefix+"CompletionCharacter", ":");
            Get(prefix+"CommandCharacter", "/");
            Get(prefix+"BashStyleCompletion", false);
            Get(prefix+"CommandHistorySize", 30);
            
            prefix = "Engine/Users/DEFAULT/Sound/";
            Get(prefix+"BeepOnHighlight", false);
            
            prefix = "Engine/Users/DEFAULT/Connection/";
            Get(prefix+"Encoding", String.Empty);
            
            prefix = "Engine/Users/";
            Get(prefix+"Users", new string[] {"local"});
            
            prefix = "Engine/Users/local/";
            Get(prefix+"Password", String.Empty);

            prefix = "Engine/Users/local/Servers/";
            Get(prefix+"Servers", new string[] {});
            
            prefix = "Server/";
            LoadEntry(prefix+"Port", 7689);
            LoadEntry(prefix+"Formatter", "binary");
            LoadEntry(prefix+"Channel", "TCP");
            LoadEntry(prefix+"BindAddress", null);

            // loading defaults
            LoadAllEntries("Engine/Users/DEFAULT");
            
            prefix = "Engine/Users/";
            string[] users = GetList(prefix+"Users");
            m_Preferences[prefix+"Users"] = users;
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
                    m_Preferences[prefix+user+"/Connection/Nicknames"] = new string[] {"Smuxi", "Smuxi_"};
                }
                
                LoadUserEntry(user, "Connection/Username", String.Empty);
                LoadUserEntry(user, "Connection/Realname", "http://smuxi.meebey.net");
                LoadUserEntry(user, "Connection/Encoding", String.Empty);
                
                string[] command_list = GetList(prefix+user+"/Connection/OnConnectCommands");
                if (command_list != null) {
                    m_Preferences[prefix+user+"/Connection/OnConnectCommands"] = command_list;
                } else {
                    m_Preferences[prefix+user+"/Connection/OnConnectCommands"] = new string[] {};
                }
                
                LoadUserEntry(user, "Interface/Notebook/TimestampFormat", null);
                LoadUserEntry(user, "Interface/Notebook/TabPosition", null);
                LoadUserEntry(user, "Interface/Notebook/BufferLines", null);
                LoadUserEntry(user, "Interface/Notebook/EngineBufferLines", null);
                LoadUserEntry(user, "Interface/Notebook/StripColors", null);
                LoadUserEntry(user, "Interface/Notebook/StripFormattings", null);
                LoadUserEntry(user, "Interface/Notebook/Channel/UserListPosition", null);
                LoadUserEntry(user, "Interface/Notebook/Channel/TopicPosition", null);
                LoadUserEntry(user, "Interface/Entry/CompletionCharacter", null);
                LoadUserEntry(user, "Interface/Entry/CommandCharacter", null);
                LoadUserEntry(user, "Interface/Entry/BashStyleCompletion", null);
                LoadUserEntry(user, "Interface/Entry/CommandHistorySize", null);
                
                LoadUserEntry(user, "Sound/BeepOnHighlight", null);
                
                string[] servers = null;
                string sprefix = prefix + user + "/Servers/";
                servers = GetList(sprefix + "Servers");
                if (servers == null) {
                    servers = new string[] {};
                }
                foreach (string server in servers) {
                    sprefix = prefix + user + "/Servers/" + server + "/";
                    LoadEntry(sprefix+"Hostname", null);
                    LoadEntry(sprefix+"Port", null);
                    LoadEntry(sprefix+"Network", null);
                    LoadEntry(sprefix+"Encoding", null);
                    LoadEntry(sprefix+"Username", null);
                    LoadEntry(sprefix+"Password", null);
                }

                string[] channelFilters = null;
                string cprefix = prefix + user + "/Filters/Channel/";
                channelFilters = GetList(cprefix + "Patterns");
                if (channelFilters == null) {
                    channelFilters = new string[] {};
                }
                foreach (string filter in channelFilters) {
                    cprefix = prefix + user + "/Filters/Channel/" + filter + "/";
                    LoadEntry(cprefix + "Pattern", null);
                    LoadEntry(cprefix + "FilterJoins", null);
                    LoadEntry(cprefix + "FilterParts", null);
                    LoadEntry(cprefix + "FilterQuits", null);
                }
            }
        }

        public void Save()
        {
#if LOG4NET
            _Logger.Info("Saving config");
#endif
            
            foreach (string key in m_Preferences.Keys) {
                object obj = m_Preferences[key];
                _Set(key, obj);
            }
            
            // BUG: we write all existing entries to the backends but when an
            // entry was removed, it will stay in the backend!
            // Probably need to explicit compare and hard remove from the
            // backends the removed entries. 
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
#if LOG4NET
            _Logger.Debug("Removing: "+key);
#endif
            m_Preferences.Remove(key);
            if (Changed != null) {
                Changed(this, EventArgs.Empty);
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
            
            object obj = Get(key, defaultvalue);
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
}
