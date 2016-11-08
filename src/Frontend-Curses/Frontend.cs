/*
 * $Id: Frontend.cs 192 2007-04-22 11:48:12Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/Frontend.cs $
 * $Rev: 192 $
 * $Author: meebey $
 * $Date: 2007-04-22 13:48:12 +0200 (Sun, 22 Apr 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007 Mirco Bauer <meebey@meebey.net>
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
using Mono.Terminal;
using Smuxi.Engine;

namespace Smuxi.Frontend.Curses
{
    public class Frontend
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string    _Name = "smuxi";
        private static readonly string    _UIName = "Curses";
        private static Version            _Version;
        private static string             _VersionString;
        private static Version            _EngineVersion;
        private static MainWindow         _MainWindow;
        private static FrontendConfig     _FrontendConfig;
        private static Session            _Session;
        private static UserConfig         _UserConfig;
        private static FrontendManager    _FrontendManager;
        
        public static string Name {
            get {
                return _Name;
            }
        }
        
        public static string UIName {
            get {
                return _UIName;
            }
        }
        
        public static Version Version {
            get {
                return _Version;
            }
        }
        
        public static Version EngineVersion {
            get {
                return _EngineVersion;
            }
            set {
                _EngineVersion = value;
            }
        }
        
        public static string VersionString {
            get {
                return _VersionString;
            }
        }
    
        public static MainWindow MainWindow {
            get {
                return _MainWindow;
            }
        }

        public static Session Session {
            get {
                return _Session;
            }
            set {
                _Session = value;
            }
        }
        
        public static FrontendManager FrontendManager {
            get {
                return _FrontendManager;
            }
        }
        
        public static Config Config {
            get {
                return _Session.Config;
            }
        }
        
        public static UserConfig UserConfig {
            get {
                return _UserConfig;
            }
            set {
                _UserConfig = value;
            }
        }
        
        public static FrontendConfig FrontendConfig {
            get {
                return _FrontendConfig;
            }
        }
        
        public static void Init(string[] args)
        {
            System.Threading.Thread.CurrentThread.Name = "Main";
           
            Assembly asm = Assembly.GetAssembly(typeof(Frontend));
            AssemblyName asm_name = asm.GetName(false);
            AssemblyProductAttribute pr = (AssemblyProductAttribute)asm.
                GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
            _Version = asm_name.Version;
            _VersionString = pr.Product + " - " + _UIName + " frontend " + _Version;

#if LOG4NET
            _Logger.Info(_VersionString + " starting");
#endif
            Application.Init(false);

            _MainWindow = new MainWindow();
            
            _FrontendConfig = new FrontendConfig(UIName);
            // loading and setting defaults
            _FrontendConfig.Load();
            _FrontendConfig.Save();
           
            if (_FrontendConfig.IsCleanConfig) {
            } else {
                if (((string)FrontendConfig["Engines/Default"]).Length == 0) {
                    InitLocalEngine();
                } else {
                    // there is a default engine set, means we want a remote engine
                    //new EngineManagerDialog();
                    InitLocalEngine();
                }
            }
            
            Application.Timeout = 100;
        	Application.Iteration += delegate {
        		Application.Refresh ();
    		};
            
		    Application.Run(_MainWindow);
#if LOG4NET
           _Logger.Warn("Application.Run() returned!");
#endif
        }
        
        public static void InitLocalEngine()
        {
            Engine.Engine.Init();
            _EngineVersion = Engine.Engine.Version;
            _Session = new Engine.Session(Engine.Engine.Config,
                                          Engine.Engine.ProtocolManagerFactory,
                                          "local");
            _Session.ExecuteOnStartupCommands();
            _Session.ProcessAutoConnect();
            _Session.RegisterFrontendUI(_MainWindow.UI);
            _UserConfig = _Session.UserConfig;
            ConnectEngineToGUI();
        }
        
        public static void ConnectEngineToGUI()
        {
            _FrontendManager = _Session.GetFrontendManager(_MainWindow.UI);
            _FrontendManager.Sync();
            
            if (_UserConfig.IsCaching) {
                // when our UserConfig is cached, we need to invalidate the cache
                _FrontendManager.ConfigChangedDelegate = new SimpleDelegate(_UserConfig.ClearCache);
            }
            
            // make sure entry got attention :-P
            // BUG: MonoCurses
            //_MainWindow.Entry.HasFocus = true;
        }
        
        public static void DisconnectEngineFromGUI()
        {
            _FrontendManager.IsFrontendDisconnecting = true;
            //_Session.DeregisterFrontendUI(_MainWindow.UI);
            //_MainWindow.Hide();
            //_MainWindow.Notebook.RemoveAllPages();
            _FrontendManager = null;
            _Session = null;
        }
        
        public static void Quit()
        {
	        Mono.Terminal.Curses.endwin();
	        Environment.Exit(0);

            if (_FrontendManager != null) {
                _FrontendManager.IsFrontendDisconnecting = true;
            }
            
            /*
            BUG: don't do this, the access to config is lost and the entry will
            throw an exception then.
            if (_FrontendManager != null) {
                DisconnectEngineFromGUI();
            }
            */
        }
        
        public static void ShowException(Exception ex)
        {
            Application.Error("Error occurred!", ex.ToString());
        }
    }
}
