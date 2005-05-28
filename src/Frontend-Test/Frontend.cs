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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using Meebey.Smuxi;
using Meebey.Smuxi.Engine;

namespace Meebey.Smuxi.FrontendTest
{
    public class Frontend
    {
        public static FrontendConfig FrontendConfig;
        
        public static void Init(string[] args)
        {
            if (!(args.Length >= 1)) {
                Console.WriteLine("Usage: smuxi-test.exe profile");
                return;
            }
            System.Threading.Thread.CurrentThread.Name = "Main";
#if LOG4NET
            Logger.Init();
            Logger.Main.Info("smuxi-test starting");
            Engine.Logger.Init();
#endif
            FrontendConfig = new FrontendConfig("Test");
            FrontendConfig.Load();
            
            string profile = args[0];
            string username = (string)FrontendConfig["Engines/"+profile+"/Username"];
            string password = (string)FrontendConfig["Engines/"+profile+"/Password"];
            string hostname = (string)FrontendConfig["Engines/"+profile+"/Hostname"];
            int port = (int)FrontendConfig["Engines/"+profile+"/Port"];
            string protocol = (string)FrontendConfig["Engines/"+profile+"/Protocol"];
            IFrontendUI ui = new TestUI();
            try {
                SessionManager sessm = null;
                switch (protocol) {
                    case "TCP":
                        ChannelServices.RegisterChannel(new TcpChannel());
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            "tcp://"+hostname+":"+port+"/SessionManager");
                        break;
                    case "HTTP":
                        ChannelServices.RegisterChannel(new HttpChannel());
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            "http://"+hostname+":"+port+"/SessionManager");
                        break;
                    case "LOCAL":
                        Engine.Engine.Init();
                        sessm = Engine.Engine.SessionManager;
                        break;
                    default:
                        Console.WriteLine("Unknown protocol ("+protocol+"), aborting...");
                        Environment.Exit(1);
                        break;
                }
                
                Session sess = sessm.Register(username, password, ui);
                FrontendManager fm = sess.GetFrontendManager(ui);
                string line = string.Empty;
                bool handled = false;
                while (true) {
                    line = Console.ReadLine();
                    switch (line) {
                        case "/quit":
                            return;
                    }
                    
                    if (!handled) {
                        sess.Command(fm, line);
                    }
                }
            } catch (Exception e) {
#if LOG4NET
                Logger.Main.Fatal("Exception: "+e.Message, e);
                Logger.Main.Fatal("Type: "+e.GetType());
                Logger.Main.Fatal("StackTrace: "+e.StackTrace);
#endif
            }
            
#if LOG4NET
            Logger.Main.Info("smuxi-test ended");
#endif
       }
    }
}
