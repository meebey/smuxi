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
using System.Reflection;
#if UI_GNOME
using GNOME = Gnome;
#endif
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    public class Frontend
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string    _Name = "smuxi";
        private static readonly string    _GladeFilename = "smuxi-frontend-gnome.glade";
#if UI_GTK
        private static readonly string    _UIName = "GTK+";
#elif UI_GNOME
        private static readonly string    _UIName = "GNOME";
        private static GNOME.Program      _Program;
#endif
        private static Version            _Version;
        private static string             _VersionNumber;
        private static string             _VersionString;
        private static Version            _EngineVersion;
        private static SplashScreenWindow _SplashScreenWindow;
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
        
        public static string GladeFilename {
            get {
                return _GladeFilename;
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
    
#if UI_GNOME
        public static GNOME.Program Program {
            get {
                return _Program;
            }
        }
#endif
 
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
            
#if GTK_SHARP_2_8 || GTK_SHARP_2_10
            if (!GLib.Thread.Supported) {
                GLib.Thread.Init();
            }
#else
            // with GTK# 2.8 we can do this better, see above
            // GTK# 2.7.1 for MS .NET doesn't support that though.
            if (Type.GetType("Mono.Runtime") == null) {
                // when we don't run on Mono, we need to initialize glib ourself
                GLib.Thread.Init();
            }
#endif

            string appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string localeDir = Path.Combine(appDir, "locale");
            if (!Directory.Exists(localeDir)) {
                localeDir = Path.Combine(Defines.InstallPrefix, "share");
                localeDir = Path.Combine(localeDir, "locale");
            }

            LibraryCatalog.Init("smuxi-frontend-gnome", localeDir);
#if LOG4NET
            _Logger.Debug("Using locale data from: " + localeDir);
#endif
            
#if UI_GNOME
            _Program = new GNOME.Program(Name, Version.ToString(), GNOME.Modules.UI, args);
#elif UI_GTK
            Gtk.Application.Init();
#endif
#if GTK_SHARP_2_10
            GLib.ExceptionManager.UnhandledException += _OnUnhandledException;
#endif           
            //_SplashScreenWindow = new SplashScreenWindow();

            _FrontendConfig = new FrontendConfig(UIName);
            // loading and setting defaults
            _FrontendConfig.Load();
            _FrontendConfig.Save();
 
            _MainWindow = new MainWindow();

            if (String.IsNullOrEmpty((string) FrontendConfig["Engines/Default"])) {
                InitLocalEngine();
            } else {
                // there is a default engine set, means we want a remote engine
                //_SplashScreenWindow.Destroy();
                _SplashScreenWindow = null;
                ShowEngineManagerDialog();
            }
            
            if (_SplashScreenWindow != null) {
                _SplashScreenWindow.Destroy();
            }
#if UI_GNOME
            _Program.Run();
    #if LOG4NET
            _Logger.Warn("_Program.Run() returned!");
    #endif
#elif UI_GTK
            Gtk.Application.Run();
    #if LOG4NET
            _Logger.Warn("Gtk.Application.Run() returned!");
    #endif
#endif
        }
        
        public static void InitLocalEngine()
        {
            Engine.Engine.Init();
            _EngineVersion = Engine.Engine.Version;
            _Session = new Engine.Session(Engine.Engine.Config,
                                          Engine.Engine.ProtocolManagerFactory,
                                          "local");
            _Session.RegisterFrontendUI(_MainWindow.UI);
            _UserConfig = _Session.UserConfig;
            ConnectEngineToGUI();
        }
        
        public static void ConnectEngineToGUI()
        {
            _FrontendManager = _Session.GetFrontendManager(_MainWindow.UI);
            _FrontendManager.Sync();
            
            /*
            // clear all current highlights, annoying to see them each time you
            // connect to the engine
            // BUG: this is useless here, the Sync() call triggers only a queue, so
            // at this point we don't have any chat views yet :(
            // this was an attempt to fix trac bug #50
            foreach (ChatView chat in _MainWindow.ChatViewManager.Chats) {
                chat.HasHighlight = false;
            }
            */
            
            // MS .NET doesn't like this?
            if (Type.GetType("Mono.Runtime") != null) {
                // when are running on Mono, all should be good
                if (_UserConfig.IsCaching) {
                    // when our UserConfig is cached, we need to invalidate the cache
                    _FrontendManager.ConfigChangedDelegate = new SimpleDelegate(_UserConfig.ClearCache);
                }
            }
            
            _MainWindow.ShowAll();
            _MainWindow.ApplyConfig(_UserConfig);
            // make sure entry got attention :-P
            _MainWindow.Entry.HasFocus = true;
            
            // check once per minute the status of the frontend manager
            GLib.Timeout.Add(60 * 1000, _CheckFrontendManagerStatus);
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
            if (_FrontendManager != null) {
                _FrontendManager.IsFrontendDisconnecting = true;
            }
            
            /*
            // BUG: don't do this, the access to config is lost and the entry will
            // throw an exception then.
            if (_FrontendManager != null) {
                DisconnectEngineFromGUI();
            }
            */
            
#if UI_GNOME
            _Program.Quit();
#elif UI_GTK
            Gtk.Application.Quit();
#endif
            
            Environment.Exit(0);
        }
        
        public static void ShowError(string msg)
        {
            Gtk.MessageDialog md = new Gtk.MessageDialog(_MainWindow,
                Gtk.DialogFlags.Modal, Gtk.MessageType.Error,
                Gtk.ButtonsType.Ok, msg);
            md.Run();
            md.Destroy();
        }
        
        public static void ShowException(Gtk.Window parent, Exception ex)
        {
            CrashDialog cd = new CrashDialog(parent, ex);
            cd.Run();
            cd.Destroy();
            
            Quit();
        }
        
        public static void ShowException(Exception ex)
        {
            ShowException(null, ex);
        }
        
        public static void ShowEngineManagerDialog()
        {
            Trace.Call();
            
            EngineManagerDialog diag = new EngineManagerDialog(_MainWindow.EngineManager);
            diag.Run();
            diag.Destroy();
        }
        
#if GTK_SHARP_2_10
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
#endif
        
        private static bool _CheckFrontendManagerStatus()
        {
            Trace.Call();
            
            try {
                if (_FrontendManager.IsAlive) {
                    return true;
                }
                
#if LOG4NET
                _Logger.Error("_CheckFrontendManagerStatus(): frontend manager is not alive anymore!");
#endif
                Gtk.MessageDialog md = new Gtk.MessageDialog(_MainWindow,
                    Gtk.DialogFlags.Modal, Gtk.MessageType.Error,
                    Gtk.ButtonsType.OkCancel, _("The server has lost the connection to the frontend.\n Do you want to reconnect now?"));
                Gtk.ResponseType res = (Gtk.ResponseType) md.Run();
                md.Destroy();
                
                if (res != Gtk.ResponseType.Ok) {
                    return false;
                }
                
                Frontend.DisconnectEngineFromGUI();
                _MainWindow.EngineManager.Reconnect();
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }

            return false;
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
