/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007, 2010-2013 Mirco Bauer <meebey@meebey.net>
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
using System.Runtime.InteropServices;
using Smuxi.Engine;
using Smuxi.Common;
using Stfl;

namespace Smuxi.Frontend.Stfl
{
    public class Frontend
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string    _Name = "smuxi";
        private static readonly string    _UIName = "STFL";
        private static Version            _Version;
        private static string             _VersionString;
        private static Version            _EngineVersion;
        private static MainWindow         _MainWindow;
        private static FrontendConfig     _FrontendConfig;
        private static Session            _LocalSession;
        private static Session            _Session;
        private static UserConfig         _UserConfig;
        private static FrontendManager    _FrontendManager;
        
        public static event EventHandler SessionPropertyChanged;

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

                if (SessionPropertyChanged != null) {
                    SessionPropertyChanged(value, EventArgs.Empty);
                }
            }
        }
        
        public static bool IsLocalEngine {
            get {
                return _LocalSession != null && _Session == _LocalSession;
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
        
        public static void Init(string engine)
        {
            System.Threading.Thread.CurrentThread.Name = "Main";
            Trace.Call(engine);
           
            Assembly asm = Assembly.GetAssembly(typeof(Frontend));
            AssemblyName asm_name = asm.GetName(false);
            AssemblyProductAttribute pr = (AssemblyProductAttribute)asm.
                GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
            _Version = asm_name.Version;
            _VersionString = pr.Product + " - " + _UIName + " frontend " + _Version;

            // this always calls abort() :(((
            //StflApi.stfl_error_action("print");
            
#if LOG4NET
            _Logger.Info(_VersionString + " starting");
#endif
            _MainWindow = new MainWindow();
            
            _FrontendConfig = new FrontendConfig(UIName);
            // loading and setting defaults
            _FrontendConfig.Load();
            _FrontendConfig.Save();

            if (_FrontendConfig.IsCleanConfig) {
                // first start assistant
            } else {
                if (String.IsNullOrEmpty(engine) || engine == "local") {
                    InitLocalEngine();
                } else {
                    InitRemoteEngine(engine);
                }
            }
            
            while (true) {
                // wait maximum for 500ms, to force a refresh even when
                // not hitting a key
                _MainWindow.Run(500);
            }
        }
        
        public static void InitLocalEngine()
        {
            Engine.Engine.Init();
            _EngineVersion = Engine.Engine.Version;
            _LocalSession = new Engine.Session(Engine.Engine.Config,
                                         Engine.Engine.ProtocolManagerFactory,
                                         "local");
            Session = _LocalSession;
            Session.RegisterFrontendUI(_MainWindow.UI);
            _UserConfig = Session.UserConfig;
            ConnectEngineToGUI();
        }
        
        public static void InitRemoteEngine(string engine)
        {
            var manager = new EngineManager(_FrontendConfig,
                                            _MainWindow.UI);
            try {
                try {
                    Console.WriteLine(
                        _("Connecting to remote engine '{0}'..."), engine
                    );
                    manager.Connect(engine);
                    Console.WriteLine(_("Connection established"));
                } catch (Exception ex) {
#if LOG4NET
                    _Logger.Error(ex);
#endif
                    Console.WriteLine(
                        _("Connection failed! Error: {1}"),
                        engine,
                        ex.Message
                    );
                    Environment.Exit(1);
                }

                Session = manager.Session;
                _UserConfig = manager.UserConfig;
                _EngineVersion = manager.EngineVersion;
                ConnectEngineToGUI();
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
                manager.Disconnect();
                throw;
            }
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
        
        public static void ApplyConfig(UserConfig userConfig)
        {
            Trace.Call(userConfig);

            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }

            _MainWindow.ApplyConfig(userConfig);
        }

        public static void Quit()
        {
            if (_FrontendManager != null) {
                _FrontendManager.IsFrontendDisconnecting = true;
                if (IsLocalEngine) {
                    try {
                        // we don't shutdown the remote session
                        Session.Shutdown();
                    } catch (Exception ex) {
#if LOG4NET
                        _Logger.Error("Quit(): Exception", ex);
#endif
                    }
                }
            }
            
            /*
            BUG: don't do this, the access to config is lost and the entry will
            throw an exception then.
            if (_FrontendManager != null) {
                DisconnectEngineFromGUI();
            }
            */

            MainWindow.Reset();
            Environment.Exit(0);
        }
        
        public static void ShowException(Exception ex)
        {
            //Application.Error("Error occurred!", ex.ToString());
        }

        static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
