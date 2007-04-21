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
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Smuxi;
using Smuxi.Engine;

namespace Smuxi.FrontendTest
{
    public class Frontend
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        public  const  char            Escape = (char)27;
        private static FrontendManager _FrontendManager;
        private static FrontendConfig  _FrontendConfig;
        private static Session         _Session;
        private static UserConfig      _UserConfig;
        
        public static FrontendConfig FrontendConfig {
            get {
                return _FrontendConfig;
            }
        }
        
        public static FrontendManager FrontendManager {
            get {
                return _FrontendManager;
            }
        }
        
        public static Session Session {
            get {
                return _Session;
            }
        }
        
        public static UserConfig UserConfig {
            get {
                return _UserConfig;
            }
        }
        
        public static void Init(string[] args)
        {
            System.Threading.Thread.CurrentThread.Name = "Main";
            
            if (!(args.Length >= 1)) {
                Console.WriteLine("Usage: smuxi-test.exe profile");
                return;
            }
            
#if LOG4NET
            _Logger.Info("smuxi-test starting");
#endif

            _FrontendConfig = new FrontendConfig("Test");
            _FrontendConfig.Load();
            
            string profile = args[0];

            IFrontendUI ui = new TestUI();
            
            SessionManager sessionManager = null;
            Session session = null;
            if (profile == "local") {
                Engine.Engine.Init();
                sessionManager = Engine.Engine.SessionManager;
                session = new Engine.Session(Engine.Engine.Config, "local");
                session.RegisterFrontendUI(ui);
                _UserConfig = session.UserConfig;
            } else {
                // remote engine
                string username = (string)_FrontendConfig["Engines/"+profile+"/Username"];
                string password = (string)_FrontendConfig["Engines/"+profile+"/Password"];
                string hostname = (string)_FrontendConfig["Engines/"+profile+"/Hostname"];
                int    port     = (int)_FrontendConfig["Engines/"+profile+"/Port"];
                string channel  = (string)_FrontendConfig["Engines/"+profile+"/Channel"];
                
                IDictionary props = new Hashtable();
                props["port"] = 0;
                switch (channel) {
                    case "TCP":
                        BinaryClientFormatterSinkProvider cprovider =
                            new BinaryClientFormatterSinkProvider();

                        BinaryServerFormatterSinkProvider sprovider =
                            new BinaryServerFormatterSinkProvider();
                        // required for MS .NET 1.1
                        sprovider.TypeFilterLevel = TypeFilterLevel.Full;

                        ChannelServices.RegisterChannel(new TcpChannel(props, cprovider, sprovider));
                        sessionManager = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            "tcp://"+hostname+":"+port+"/SessionManager");
                        break;
                    case "HTTP":
                        ChannelServices.RegisterChannel(new HttpChannel());
                        sessionManager = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            "http://"+hostname+":"+port+"/SessionManager");
                        break;
                    default:
                        Console.WriteLine("Unknown channel ("+channel+"), aborting...");
                        Environment.Exit(1);
                        break;
                }
                session = sessionManager.Register(username, password, ui);
                
                // setup cached config
                _UserConfig = new UserConfig(session.Config, username);
                _UserConfig.IsCaching = true;
            }
            
            _Session = session;
            _FrontendManager = session.GetFrontendManager(ui);
            _FrontendManager.Sync();
            
            if (_UserConfig.IsCaching) {
                // when our UserConfig is cached, we need to invalidate the cache
                _FrontendManager.ConfigChangedDelegate = new SimpleDelegate(_UserConfig.ClearCache);
            }
            
            while (true) {
                string line = Console.ReadLine();
                // TODO: remove the entered line from output
                //Console.WriteLine(Escape+"M");
                
                _ExecuteCommand(line);
            }
        }
        
        public static void _ExecuteCommand(string cmd)
        {
            bool handled = false;
            CommandModel cd = new CommandModel(_FrontendManager, _FrontendManager.CurrentChat,
                                             (string)_UserConfig["Interface/Entry/CommandCharacter"],
                                             cmd);
            
            if (cd.IsCommand) {
                switch (cd.Command) {
                    case "window":
                        bool found = false;
                        foreach (ChatModel chatModel in _Session.Chats) {
                            if (chatModel.Name.ToLower() == cd.Parameter.ToLower()) {
                                found = true;
                                ChangeActiveChat(chatModel);
                                break;
                            }
                        }
                        if (!found) {
                            Console.WriteLine("-!- Unknown page: "+cd.Parameter);
                        }
                        handled = true;
                        break;
                    case "quit":
                        Environment.Exit(0);
                        handled = true;
                        break;
                }
            }
            
            if (!handled) {
                handled = _Session.Command(cd);
            }
            
            if (!handled) {
                // we may have no network manager yet
                if (_FrontendManager.CurrentProtocolManager != null) {
                    handled = _FrontendManager.CurrentProtocolManager.Command(cd);
                } else {
                    handled = true;
                }
            }
            
            if (!handled) {
               Console.WriteLine("-!- Unknown command");
            }
        }
        
        public static void ChangeActiveChat(ChatModel chatModel)
        {
            Console.WriteLine("Active chat: "+chatModel.Name);
            _FrontendManager.CurrentChat = chatModel;
        }
    }
}
