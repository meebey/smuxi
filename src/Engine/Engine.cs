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
using System.Reflection;

namespace Meebey.Smuxi.Engine
{
    public class Engine
    {
        private static string           _Version;
        private static string           _VersionString;
        private static Config           _Config;
        private static SessionManager   _SessionManager;
        
        public static string Version {
            get {
                return _Version;
            }
        }
    
        public static string VersionString {
            get {
                return _VersionString;
            }
        }
        
        public static Config Config {
            get {
                return _Config;
            } 
        }
        
        public static SessionManager SessionManager {
            get {
                return _SessionManager;
            } 
        }
        
        public static void Init()
        {
#if LOG4NET
            Logger.Init();
#endif
            Assembly assembly = Assembly.GetAssembly(typeof(Engine));
            AssemblyName assembly_name = assembly.GetName(false);
            AssemblyProductAttribute pr = (AssemblyProductAttribute)assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
            _Version = assembly_name.Version.ToString();
            _VersionString = pr.Product+" "+_Version;
                
            _Config = new Config(); 
            _Config.Load();
            _Config.Save();
            _SessionManager = new SessionManager();
        }
    }
}
