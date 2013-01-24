/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2013 Mirco Bauer <meebey@meebey.net>
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
using System.Reflection;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class Engine
    {
        private static bool             _IsInitialized;
        private static Version          _Version;
        private static string           _VersionNumber;
        private static string           _VersionString;
        private static Config           _Config;
        private static SessionManager   _SessionManager;
        private static ProtocolManagerFactory _ProtocolManagerFactory;
        
        public static Version Version {
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
        
        public static ProtocolManagerFactory ProtocolManagerFactory {
            get {
                return _ProtocolManagerFactory;
            } 
        }
        
        public static SessionManager SessionManager {
            get {
                return _SessionManager;
            } 
        }

        public static bool IsInitialized {
            get {
                return _IsInitialized;
            }
        }
        
        public static void Init()
        {
            if (_IsInitialized) {
                return;
            }
            _IsInitialized = true;
            
            var asm = Assembly.GetEntryAssembly();
            if (asm == null) {
                asm = Assembly.GetAssembly(typeof(Engine));
            }
            var asm_name = asm.GetName(false);
            _Version = asm_name.Version;
            _VersionNumber = asm_name.Version.ToString();

            var distVersion = Defines.DistVersion;
            if (!String.IsNullOrEmpty(distVersion)) {
                distVersion = String.Format(" ({0})", distVersion);
            }
            _VersionString = String.Format(
                "{0} {1}{2} - running on {3} {4}",
                Path.GetFileNameWithoutExtension(asm_name.Name),
                _Version,
                distVersion,
                Platform.OperatingSystem,
                Platform.Architecture
            );

            _Config = new Config();
            _Config.Load();
            _Config.Save();
            
            string location = Assembly.GetExecutingAssembly().Location;
            _ProtocolManagerFactory = new ProtocolManagerFactory();
            _ProtocolManagerFactory.LoadAllProtocolManagers(Path.GetDirectoryName(location));
            
            _SessionManager = new SessionManager(_Config, _ProtocolManagerFactory);
        }
    }
}
