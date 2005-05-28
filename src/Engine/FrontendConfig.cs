/**
 * $Id: AssemblyInfo.cs 34 2004-09-05 14:46:59Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/Gnosmirc/trunk/src/AssemblyInfo.cs $
 * $Rev: 34 $
 * $Author: meebey $
 * $Date: 2004-09-05 16:46:59 +0200 (Sun, 05 Sep 2004) $
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

namespace Meebey.Smuxi.Engine
{
    public class FrontendConfig : Config
    {
        private string _Prefix;
        private string _UI;
        
        public new object this[string key]
        {
            get {
                return base[_Prefix+key];
            }
            set {
                base[_Prefix+key] = value;
            }
        }
        
        public FrontendConfig(string ui)
        {
            _UI = ui;
            _Prefix = "Frontend/";
        }
        
        public new void Load()
        {
            string prefix;
#if LOG4NET
            Logger.Config.Info("Loading config (FrontendConfig)");
#endif
            
            prefix = "Frontend/";
            _LoadEntry(prefix+"Engines/Default", "");

            prefix = "Frontend/Engines/";
            string[] engines_list = _GetList(prefix+"EnginesList");
            _Preferences[prefix+"EnginesList"] = engines_list;
            foreach (string engine in engines_list) {
                if (engine == "") {
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
            
            _LoadAllEntries("Frontend/"+_UI);
        }
    }
}
