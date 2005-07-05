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
using Meebey.Smuxi;
using Meebey.Smuxi.Engine;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class Frontend
    {
        private static string             _Name = "smuxi";
        private static string             _UIName = "GtkGnome";
        private static IFrontendUI        _UI;
        private static string             _Version;
        private static string             _VersionString;
        private static SplashScreenWindow _SplashScreenWindow;
        private static MainWindow         _MainWindow;
#if UI_GNOME
        private static Gnome.Program      _Program;
#endif
        private static FrontendConfig     _FrontendConfig;
        private static Session            _Session;
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
    
        public static IFrontendUI UI {
            get {
                return _UI;
            }
        }
        
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
    
#if UI_GNOME
        public static Gnome.Program Program {
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
                return Session.Config;
            }
        }
        
        public static UserConfig UserConfig {
            get {
                return Session.UserConfig;
            }
        }
        
        public static FrontendConfig FrontendConfig {
            get {
                return _FrontendConfig;
            }
        }
        
        public static void Init(string[] args)
        {
            try {
                System.Threading.Thread.CurrentThread.Name = "Main";
                
                Assembly assembly = Assembly.GetAssembly(typeof(Frontend));
                AssemblyName assembly_name = assembly.GetName(false);
                AssemblyProductAttribute pr = (AssemblyProductAttribute)assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
                _Version = assembly_name.Version.ToString();
                _VersionString = pr.Product+" "+_Version;
                
#if LOG4NET
                Logger.Init();
                Logger.Main.Info("smuxi-gtkgnome starting");
                Engine.Logger.Init();
#endif

#if GTK_1
                int os = (int)Environment.OSVersion.Platform;
                // 128 == Linux with Mono .NET 1.0
                // 4 == Linux with Mono .NET 2.0
                if ((os != 128) &&
                    (os != 4)) {
                    // this is not linux
                    GLib.Thread.Init(); // .NET needs that...
                }
#endif
                Gdk.Threads.Init();
#if UI_GNOME
                _Program = new Gnome.Program(Name, Version, Gnome.Modules.UI, args);
#elif UI_GTK
                Gtk.Application.Init();
#endif
                _SplashScreenWindow = new SplashScreenWindow();
                    
                _FrontendConfig = new FrontendConfig(UIName);
                _FrontendConfig.Load();
                _FrontendConfig.Save();
                
                _UI = new GtkGnomeUI();
                if (((string)Frontend.FrontendConfig["Engines/Default"]).Length == 0) {
                    Engine.Engine.Init();
                    _Session = new Engine.Session(Engine.Engine.Config, "local");
                    _Session.RegisterFrontendUI(Frontend.UI);
                } else {
                    // there is a default engine set, means we want a remote engine
                    new EngineManagerDialog();
                }

                if (_Session != null) {
                    InitGUI();
                }
                _SplashScreenWindow.Destroy();
                
#if UI_GNOME
                _Program.Run();
#elif UI_GTK
                Gtk.Application.Run();
#endif
            } catch (Exception e) {
                new CrashDialog(e);
                // rethrow the exception for console output
                throw e;
            }
        }
        
        public static void InitGUI()
        {
            _FrontendManager = _Session.GetFrontendManager(_UI);

            _MainWindow = new MainWindow();
            _MainWindow.ShowAll();

            // make sure entry got attention :-P
            _MainWindow.Entry.HasFocus = true;
        }
        
        public static void Quit()
        {
#if UI_GNOME
            _Program.Quit();
#elif UI_GTK
            Gtk.Application.Quit();
#endif
        }    
    }
}
