/*
 * $Id: Frontend.cs 213 2007-09-10 21:25:36Z meebey $
 * $URL: svn+ssh://SmuxiSVN/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/Frontend.cs $
 * $Rev: 213 $
 * $Author: meebey $
 * $Date: 2007-09-10 16:25:36 -0500 (Mon, 10 Sep 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2007 Mirco Bauer <meebey@meebey.net>
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
using System.Windows.Forms;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Swf
{
    public class Frontend
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string    _Name = "smuxi";
        private static readonly string    _UIName = "SWF (WinForms)";
        private static Version            _Version;
        private static string             _VersionNumber;
        private static string             _VersionString;
        private static Version            _EngineVersion;
        private static MainWindow         _MainWindow;
        private static FrontendConfig     _FrontendConfig;
        private static Session            _Session;
        private static UserConfig         _UserConfig;
        private static FrontendManager    _FrontendManager;
        private static object             _UnhandledExceptionSyncRoot = new Object();
        
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
            _VersionNumber = asm_name.Version.ToString();
            _VersionString = pr.Product + " - " + _UIName + " frontend " + _Version;

#if LOG4NET
            _Logger.Info(_VersionString + " starting");
#endif
           
            // We don't want to put any XP/Vista users by using the dull ugly
            // unthemed interface.  Application.EnableVisualStyles() should be
            // called before any form is displayed.
            Application.EnableVisualStyles();

            _MainWindow = new MainWindow();
            // HACK: force creation of window handle, else the engine will have problems adding stuff
            IntPtr handle = _MainWindow.Handle;
            
            _FrontendConfig = new FrontendConfig(UIName);
            // loading and setting defaults
            _FrontendConfig.Load();
            _FrontendConfig.Save();
           
            if (_FrontendConfig.IsCleanConfig) {
                /*TODO: Create and show first run wizard*/
            } else {
                if (((string)FrontendConfig["Engines/Default"]).Length == 0) {
                    InitLocalEngine();
                } else {
                    // there is a default engine set, means we want a remote engine
                    /*TODO: Create and show Engine Manager Dialog*/
                    
                    // HACK: for now always use local engine
                    InitLocalEngine();
                }
            }
             /*TODO: Set the main message loop*/
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
            
            _MainWindow.Show();
            _MainWindow.ApplyConfig(_UserConfig);
            // make sure entry got attention :-P
            _MainWindow.Entry.Select();
        }
        
        public static void DisconnectEngineFromGUI()
        {
            Trace.Call();
            
            _FrontendManager.IsFrontendDisconnecting = true;
            _Session.DeregisterFrontendUI(_MainWindow.UI);
            _MainWindow.Hide();
            _MainWindow.Notebook.RemoveAllPages();
            _FrontendManager = null;
            _Session = null;
        }
        
        public static void Quit()
        {
            _MainWindow.Close();
            Application.Exit();

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
        
        public static void ShowException(Form parent, Exception ex)
        {
            /* TODO
            CrashDialog cd = new CrashDialog(parent, ex);
            cd.Run();
            cd.Destroy();
             */
        }
        
        public static void ShowException(Exception ex)
        {
            ShowException(null, ex);
        }
        
        /*
        private static void _OnUnhandledException(GLib.UnhandledExceptionArgs e)
        {
            Trace.Call(e);
            
            lock (_UnhandledExceptionSyncRoot) {
                if (e.ExceptionObject is Exception) {
                    ShowException((Exception) e.ExceptionObject);
                    Quit();
                }
            }
        }
        */
    }
}
