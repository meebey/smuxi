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
        private static int                _UIThreadID;
        private static GNOME.Program      _Program;
#endif
        private static Version            _Version;
        private static string             _VersionNumber;
        private static string             _VersionString;
        private static Version            _EngineVersion;
        private static SplashScreenWindow _SplashScreenWindow;
        private static MainWindow         _MainWindow;
#if GTK_SHARP_2_10
        private static Gtk.StatusIcon     _StatusIcon;
#endif
        private static FrontendConfig     _FrontendConfig;
        private static Session            _LocalSession;
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
        
#if GTK_SHARP_2_10
        public static Gtk.StatusIcon StatusIcon {
            get {
                return _StatusIcon;
            }
        }
#endif
        
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
            _UIThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
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

#if GTK_SHARP_2_10
            _StatusIcon = new Gtk.StatusIcon();
            _StatusIcon.Visible = false;
            _StatusIcon.Pixbuf = new Gdk.Pixbuf(null, "icon.svg");
            _StatusIcon.Activate += delegate {
                try {
                    if (_StatusIcon.Blinking) {
                        _MainWindow.Present();
                        return;
                    }
                    _MainWindow.Visible = !_MainWindow.Visible;
                } catch (Exception ex) {
                    ShowException(ex);
                }
            };
            _StatusIcon.PopupMenu += OnStatusIconPopupMenu;
            _StatusIcon.Tooltip = "Smuxi";
#endif
            
            if (String.IsNullOrEmpty((string) FrontendConfig["Engines/Default"])) {
                InitLocalEngine();
                ConnectEngineToGUI();
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
            if (!Engine.Engine.IsInitialized) {
                // only initialize a local engine once
                Engine.Engine.Init();
                _LocalSession = new Engine.Session(Engine.Engine.Config,
                                                   Engine.Engine.ProtocolManagerFactory,
                                                   "local");
            }
            _EngineVersion = Engine.Engine.Version;
            _Session = _LocalSession;
            _UserConfig = _Session.UserConfig;
        }
        
        public static void ConnectEngineToGUI()
        {
            _Session.RegisterFrontendUI(_MainWindow.UI);
            _FrontendManager = _Session.GetFrontendManager(_MainWindow.UI);
            _FrontendManager.Sync();
            
            // MS .NET doesn't like this with Remoting?
            if (Type.GetType("Mono.Runtime") != null) {
                // when are running on Mono, all should be good
                if (_UserConfig.IsCaching) {
                    // when our UserConfig is cached, we need to invalidate the cache
                    _FrontendManager.ConfigChangedDelegate = new SimpleDelegate(_UserConfig.ClearCache);
                }
            }
            
            ApplyConfig(_UserConfig);
            
            _MainWindow.ShowAll();
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
            Trace.Call();
            
            // save window size
            int width, heigth;
            _MainWindow.GetSize(out width, out heigth);
            _FrontendConfig[Frontend.UIName + "/Interface/Width"] = width;
            _FrontendConfig[Frontend.UIName + "/Interface/Heigth"] = heigth;
            
            int x, y;
            _MainWindow.GetPosition(out x, out y);
            _FrontendConfig[Frontend.UIName + "/Interface/XPosition"] = x;
            _FrontendConfig[Frontend.UIName + "/Interface/YPosition"] = y;
            _FrontendConfig.Save();
            
            if (_FrontendManager != null) {
                _FrontendManager.IsFrontendDisconnecting = true;
            }
            
            /*
            // WASBUG: don't do this, the access to config is lost and the entry will
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
        
        private static bool IsGuiThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == _UIThreadID;
        }
                
        public static void ShowError(string msg, Exception ex)
        {
            Trace.Call(msg, ex);
            
            if (!IsGuiThread()) {
                Gtk.Application.Invoke(delegate {
                    ShowError(msg, ex);
                });
            }
            
            if (ex != null) {
                msg += "\n" + String.Format(_("Cause: {0}"), ex.Message);
            }
               
            Gtk.MessageDialog md = new Gtk.MessageDialog(_MainWindow,
                Gtk.DialogFlags.Modal, Gtk.MessageType.Error,
                Gtk.ButtonsType.Ok, msg);
            md.Run();
            md.Destroy();
        }
        
        public static void ShowError(string msg)
        {
            Trace.Call(msg);
            
            ShowError(msg, null);
        }
        
        public static void ShowException(Gtk.Window parent, Exception ex)
        {
            Trace.Call(parent, ex);
            
            if (parent == null) {
                parent = _MainWindow;
            }
            
            if (!IsGuiThread()) {
                Gtk.Application.Invoke(delegate {
                    ShowException(parent, ex);
                });
            }
                
            if (ex is System.Runtime.Remoting.RemotingException) {
                Gtk.MessageDialog md = new Gtk.MessageDialog(_MainWindow,
                    Gtk.DialogFlags.Modal, Gtk.MessageType.Error,
                    Gtk.ButtonsType.OkCancel, _("The frontend has lost the connection to the server.\n Do you want to reconnect now?"));
                Gtk.ResponseType res = (Gtk.ResponseType) md.Run();
                md.Destroy();
                
                if (res == Gtk.ResponseType.Ok) {
                    Frontend.DisconnectEngineFromGUI();
                    _MainWindow.EngineManager.Reconnect();
                }
                return;
            }
            
            CrashDialog cd = new CrashDialog(parent, ex);
            cd.Run();
            cd.Destroy();
            
            Quit();
        }
        
        public static void ShowException(Exception ex)
        {
            Trace.Call(ex);
            
            ShowException(null, ex);
        }
        
        public static void ShowEngineManagerDialog()
        {
            Trace.Call();
            
            EngineManagerDialog diag = new EngineManagerDialog(_MainWindow.EngineManager);
            diag.Run();
            diag.Destroy();
        }
        
        public static void ApplyConfig(UserConfig userConfig)
        {
            Trace.Call(userConfig);
            
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }
            
#if GTK_SHARP_2_10
            string modeStr = (string) userConfig["Interface/Notification/NotificationAreaIconMode"];
            NotificationAreaIconMode mode = (NotificationAreaIconMode) Enum.Parse(
                typeof(NotificationAreaIconMode),
                modeStr
            );
            switch (mode) {
                case NotificationAreaIconMode.Never:
                    _StatusIcon.Visible = false;
                    break;
                case NotificationAreaIconMode.Always:
                    _StatusIcon.Visible = true;
                    break;
                case NotificationAreaIconMode.Minimized:
                case NotificationAreaIconMode.Closed:
                    // at application startup the main window is not visible but also not yet realized 
                    _StatusIcon.Visible = _MainWindow.IsRealized && !_MainWindow.Visible;
                    break;
            }
#endif
            
            _MainWindow.ApplyConfig(userConfig);
        }
        
        private static void OnStatusIconPopupMenu(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            Gtk.Menu menu = new Gtk.Menu();
            
            Gtk.ImageMenuItem preferencesItem = new Gtk.ImageMenuItem(Gtk.Stock.Preferences, null);
            preferencesItem.Activated += delegate {
                try {
                    PreferencesDialog dialog = new PreferencesDialog(_MainWindow);
                    dialog.CurrentPage = PreferencesDialog.Page.Interface;
                    dialog.CurrentInterfacePage = PreferencesDialog.InterfacePage.Notification;
                } catch (Exception ex) {
                    ShowException(ex);
                }
            };
            menu.Add(preferencesItem);
            
            menu.Add(new Gtk.SeparatorMenuItem());
            
            Gtk.ImageMenuItem quitItem = new Gtk.ImageMenuItem(Gtk.Stock.Quit, null);
            quitItem.Activated += delegate {
                try {
                    Quit();
                } catch (Exception ex) {
                    ShowException(ex);
                }
            };
            menu.Add(quitItem);
            
            menu.ShowAll();
            menu.Popup();
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
                if (_FrontendManager == null) {
                    // we lost the frontend manager, nothing to check
                    return false;
                }
                
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
