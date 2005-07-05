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

namespace Meebey.Smuxi.Engine
{
    public class FrontendConfig : Config
    {
        private string _Prefix;
        private string _UIName;
        
        public new object this[string key]
        {
            get {
                return base[_Prefix+key];
            }
            set {
                base[_Prefix+key] = value;
            }
        }
        
        public FrontendConfig(string uiName)
        {
            _UIName = uiName;
            _Prefix = "Frontend/";
        }
        
        public new void Load()
        {
            string prefix;
#if LOG4NET
            Logger.Config.Info("Loading config (FrontendConfig)");
#endif
            
            prefix = "Frontend/";
            _LoadEntry(prefix+"Engines/Default", String.Empty);
            
            prefix = "Frontend/Engines/";
            string[] engines = _GetList(prefix+"Engines");
            _Preferences[prefix+"Engines"] = engines;
            foreach (string engine in engines) {
                if (engine.Length == 0) {
                    continue;
                }
                string eprefix = prefix+engine+"/"; 
                _LoadEntry(eprefix+"Username", null);
                _LoadEntry(eprefix+"Password", null);
                _LoadEntry(eprefix+"Hostname", null);
                _LoadEntry(eprefix+"Port", null);
                _LoadEntry(eprefix+"Channel", null);
                _LoadEntry(eprefix+"Formatter", null);
            }
            
            _LoadAllEntries("Frontend/"+_UIName);
        }
        
        public new void Remove(string key)
        {
            base.Remove(_Prefix+key);
        }
    }
}
