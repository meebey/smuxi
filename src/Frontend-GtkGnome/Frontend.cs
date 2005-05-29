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

using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using Meebey.Smuxi;
using Meebey.Smuxi.Engine;
#if CHANNEL_TCPEX
using TcpEx;
#endif

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class Frontend
    {
        private static string             _Name = "smuxi";
        private static string             _UI = "GtkGnome";
        private static string             _Version;
        private static string             _VersionString;
        private static MainWindow         _MainWindow;
#if UI_GNOME
        private static Gnome.Program      _Program;
#endif
        private static FrontendConfig     _FrontendConfig;
        private static Session            _Session;
        private static FrontendManager    _FrontendManager;
        
        public static string Name
        {
            get {
                return _Name;
            }
        }
    
        public static string UI
        {
            get {
                return _UI;
            }
        }
    
        public static string Version
        {
            get {
                return _Version;
            }
        }
    
        public static string VersionString
        {
            get {
                return _VersionString;
            }
        }
    
#if UI_GNOME
        public static Gnome.Program Program
        {
            get {
                return _Program;
            }
        }
#endif
 
        public static MainWindow MainWindow
        {
            get {
                return _MainWindow;
            }
        }
    
        public static Session Session
        {
            get {
                return _Session;
            }
        }
        
        public static FrontendManager FrontendManager
        {
            get {
                return _FrontendManager;
            }
        }
        
        public static Config Config
        {
            get {
                return Session.Config;
            }
        }
        
        public static UserConfig UserConfig
        {
            get {
                return Session.UserConfig;
            }
        }
        
        public static FrontendConfig FrontendConfig
        {
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
                SplashScreenWindow ssw = new SplashScreenWindow();
                    
                _FrontendConfig = new FrontendConfig(UI);
                _FrontendConfig.Load();
                _FrontendConfig.Save();
                
                // setup the session
                IFrontendUI ui = new GtkGnomeUI();
                if (((string)FrontendConfig["Engines/Default"]).Length == 0) {
                    Engine.Engine.Init();
                    _Session = new Session(Engine.Engine.Config, "local");
                    _Session.RegisterFrontendUI(ui);
                } else {
                    // there is a default engine set, means we want a remote engine
                    // TODO: write engine manager dialog!
                    //EngineManagerDialog emd = new EngineManagerDialog();
                    //emd.Run();
                    string engine = (string)FrontendConfig["Engines/Default"];
                    string username = (string)FrontendConfig["Engines/"+engine+"/Username"];
                    string password = (string)FrontendConfig["Engines/"+engine+"/Password"];
                    string hostname = (string)FrontendConfig["Engines/"+engine+"/Hostname"];
                    int port = (int)FrontendConfig["Engines/"+engine+"/Port"];
                    string formatter = (string)FrontendConfig["Engines/"+engine+"/Formatter"];
                    string channel = (string)FrontendConfig["Engines/"+engine+"/Channel"];
                    
                    SessionManager sessm = null;
                    string connection_url;
                    switch (channel) {
                        case "TCP":
                            connection_url = "tcp://"+hostname+":"+port+"/SessionManager"; 
#if LOG4NET
                            Logger.Main.Info("Connecting to: "+connection_url);
#endif
                            ChannelServices.RegisterChannel(new TcpChannel());
                            sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                                connection_url);
                            break;
#if CHANNEL_TCPEX
                        case "TcpEx":
                            connection_url = "tcpex://"+hostname+":"+port+"/SessionManager"; 
#if LOG4NET
                            Logger.Main.Info("Connecting to: "+connection_url);
#endif
                            ChannelServices.RegisterChannel(new TcpExChannel());
                            sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                                connection_url);
                            break;
#endif
                        case "HTTP":
                            connection_url = "http://"+hostname+":"+port+"/SessionManager"; 
#if LOG4NET
                            Logger.Main.Info("Connecting to: "+connection_url);
#endif
                            ChannelServices.RegisterChannel(new HttpChannel());
                            sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                                connection_url);
                            break;
                        default:
                            Console.WriteLine("Unknown channel ("+channel+"), aborting...");
                            Environment.Exit(1);
                            break;
                    }
                    _Session = sessm.Register(username, password, ui);
                }
                _FrontendManager = _Session.GetFrontendManager(ui);
                
                _MainWindow = new MainWindow();
                
                ssw.Destroy();
                _MainWindow.ShowAll();
                // make sure entry got attention :-P
                _MainWindow.Entry.HasFocus = true;
                
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
        
        static public void Quit()
        {
#if UI_GNOME
            _Program.Quit();
#elif UI_GTK
            Gtk.Application.Quit();
#endif
        }    
    }
}
