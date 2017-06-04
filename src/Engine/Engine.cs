/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2017 Mirco Bauer <meebey@meebey.net>
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
using System.Text;
using System.Reflection;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class Engine
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static bool             _IsInitialized;
        private static string           _VersionString;
        private static Config           _Config;
        private static SessionManager   _SessionManager;
        private static ProtocolManagerFactory _ProtocolManagerFactory;

        public static Version AssemblyVersion {
            get {
                var asm = Assembly.GetEntryAssembly();
                if (asm == null) {
                    asm = Assembly.GetAssembly(typeof(Engine));
                }
                var asm_name = asm.GetName(false);
                return asm_name.Version;
            }
        }

        [Obsolete("Use AssemblyVersion or ProtocolVersion instead.")]
        public static Version Version {
            get {
                return AssemblyVersion;
            }
        }
    
        public static string VersionString {
            get {
                return _VersionString;
            }
        }

        public static Version ProtocolVersion {
            get {
                // major == compatibility
                // minor == features
                return new Version("0.13");
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

            var distVersion = Defines.DistVersion;
            if (!String.IsNullOrEmpty(distVersion)) {
                distVersion = String.Format(" ({0})", distVersion);
            }
            _VersionString = String.Format(
                "{0} {1}{2} - running on {3} {4}",
                Path.GetFileNameWithoutExtension(asm_name.Name),
                AssemblyVersion,
                distVersion,
                Platform.OperatingSystem,
                Platform.Architecture
            );

            _Config = new Config();
            _Config.Load();

            // migration config settins from 1.0 or earlier to 1.1
            if (_Config.PreviousVersion == null ||
                _Config.PreviousVersion < new Version(1, 1)) {
                // migrate all existing IRC connections for Slack to the
                // SlackProtocolManager
                var users = (string[]) _Config["Engine/Users/Users"];
                if (users != null) {
                    foreach (var user in users) {
                        var userConfig = new UserConfig(_Config, user);
                        var serverController = new ServerListController(userConfig);
                        var servers = serverController.GetServerList();
                        foreach (var server in servers) {
                            if (server.Protocol != "IRC") {
                                continue;
                            }
                            if (!server.Hostname.EndsWith(".irc.slack.com")) {
                                continue;
                            }
#if LOG4NET
                            f_Logger.InfoFormat(
                                "Migrating Slack server '{0}' of user '{1}' " +
                                "from IRC to Slack protocol manager",
                                server,
                                user
                            );
#endif
                            // this is Slack IRC bridge connection
                            var migratedServer = new ServerModel(server);
                            migratedServer.ServerID = null;
                            migratedServer.Protocol = "Slack";
                            serverController.AddServer(migratedServer);
                            // remove old Slack server with IRC as protocol
                            serverController.RemoveServer(server.Protocol,
                                                          server.ServerID);
                        }
                    }
                }
                _Config["Engine/ConfigVersion"] = _Config.CurrentVersion.ToString();
            }

            _Config.Save();

            string location = Path.GetDirectoryName(asm.Location);
            if (String.IsNullOrEmpty(location) &&
                Environment.OSVersion.Platform == PlatformID.Unix) {
                // we are mkbundled
                var locationBuilder = new StringBuilder(8192);
                if (Mono.Unix.Native.Syscall.readlink("/proc/self/exe", locationBuilder) >= 0) {
                    location = Path.GetDirectoryName(locationBuilder.ToString());
                }
            }
            _ProtocolManagerFactory = new ProtocolManagerFactory();
            _ProtocolManagerFactory.LoadAllProtocolManagers(location);
        }

        public static void InitSessionManager()
        {
            if (_SessionManager != null) {
                return;
            }
            if (_Config == null || _ProtocolManagerFactory == null) {
                throw new InvalidOperationException("Init() must be called first!");
            }
            _SessionManager = new SessionManager(_Config, _ProtocolManagerFactory);
        }

        public static void Shutdown()
        {
            SessionManager.Shutdown();
            Environment.Exit(0);
        }
    }
}
