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
#if CONFIG_NINI
using Nini.Ini;
#endif

namespace Smuxi.Engine
{
    public class FrontendConfig : Config
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private string _Prefix;
        private string _UIName;
        
        public new object this[string key]
        {
            get {
                var value = base["Engine/" + key];
                if (value != null) {
                    return value;
                }
                return base[_Prefix+key];
            }
            set {
                base[_Prefix+key] = value;
            }
        }
        
        public FrontendConfig(string uiName) : base()
        {
            _UIName = uiName;
            _Prefix = "Frontend/";
            
#if CONFIG_NINI
            m_IniFilename = m_ConfigPath+"/smuxi-frontend.ini";
            if (!File.Exists(m_IniFilename)) {
    #if LOG4NET
                _Logger.Debug("creating file: "+m_IniFilename);
    #endif
                File.Create(m_IniFilename).Close();
                m_IsCleanConfig = true;
            }
            
            m_IniDocument = new IniDocument(m_IniFilename);
#endif
        }

        public new void Load()
        {
            string prefix;
#if LOG4NET
            _Logger.Info("Loading config (FrontendConfig)");
#endif
            
            // setting required default values
            prefix = "Frontend/";
            LoadEntry(prefix+"UseLowBandwidthMode", false);
            LoadEntry(prefix+"ShowQuickJoin", true);
            LoadEntry(prefix+"ShowMenuBar", true);
            LoadEntry(prefix+"ShowStatusBar", true);

            prefix = "Frontend/Engines/";
            Get<string[]>(prefix+"Engines", new string[] {});
            Get(prefix+"Default", String.Empty);
            
            prefix = "Frontend/Engines/";
            LoadEntry(prefix+"Default", String.Empty);
            
            string[] engines = GetList(prefix+"Engines");
            m_Preferences[prefix+"Engines"] = engines;
            foreach (string engine in engines) {
                string eprefix = prefix+engine+"/"; 
                LoadEntry(eprefix+"Username", String.Empty);
                LoadEntry(eprefix+"Password", String.Empty);
                LoadEntry(eprefix+"Hostname", String.Empty);
                LoadEntry(eprefix+"BindAddress", null);
                LoadEntry(eprefix+"Port", null);
                LoadEntry(eprefix+"Channel", null);
                LoadEntry(eprefix+"Formatter", null);
                LoadEntry(eprefix+"UseSshTunnel", false);
                LoadEntry(eprefix+"SshProgram", null);
                LoadEntry(eprefix+"SshParameters", null);
                LoadEntry(eprefix+"SshHostname", String.Empty);
                LoadEntry(eprefix+"SshPort", 22);
                LoadEntry(eprefix+"SshUsername", String.Empty);
                LoadEntry(eprefix+"SshPassword", String.Empty);
                LoadEntry(eprefix+"SshKeyfile", String.Empty);
            }
            
            LoadAllEntries("Frontend/"+_UIName);
            LoadAllEntries("Engine");
        }
        
        public new void Remove(string key)
        {
            base.Remove(_Prefix+key);
        }
    }
}
