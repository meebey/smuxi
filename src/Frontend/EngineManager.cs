/*
 * $Id: Frontend.cs 73 2005-06-27 12:42:06Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/Frontend.cs $
 * $Rev: 73 $
 * $Author: meebey $
 * $Date: 2005-06-27 14:42:06 +0200 (Mon, 27 Jun 2005) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
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
//using Smuxi.Channels.Tcp;
#if CHANNEL_TCPEX
using TcpEx;
#endif
#if CHANNEL_BIRDIRTCP
using DotNetRemotingCC.Channels.BidirectionalTCP;
#endif
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend
{
    public class EngineManager
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private SessionManager _SessionManager;
        private FrontendConfig _FrontendConfig;
        private IFrontendUI    _FrontendUI;
        private string         _Engine;
        private string         _EngineUrl;
        private Version        _EngineVersion;
        private UserConfig     _UserConfig;
        private Session        _Session;
        
        public SessionManager SessionManager {
            get {
                return _SessionManager;
            }
        }
        
        public string EngineUrl {
            get {
                return _EngineUrl;
            }
        }
        
        public Version EngineVersion {
            get {
                return _EngineVersion;
            }
        }
        
        public Session Session {
            get {
                return _Session;
            }
        }
        
        public UserConfig UserConfig {
            get {
                return _UserConfig;
            }
        }
        
        public EngineManager(FrontendConfig frontendConfig, IFrontendUI frontendUI)
        {
            Trace.Call(frontendConfig, frontendUI);
            
            if (frontendConfig == null) {
                throw new ArgumentNullException("frontendConfig");
            }
            if (frontendUI == null) {
                throw new ArgumentNullException("frontendUI");
            }
            
            _FrontendConfig = frontendConfig;
            _FrontendUI = frontendUI;
        }
        
        public void Connect(string engine)
        {
            Trace.Call(engine);
            
            _Engine = engine;
            string username = (string) _FrontendConfig["Engines/"+engine+"/Username"];
            string password = (string) _FrontendConfig["Engines/"+engine+"/Password"];
            string hostname = (string) _FrontendConfig["Engines/"+engine+"/Hostname"];
            string bindAddress = (string) _FrontendConfig["Engines/"+engine+"/BindAddress"];
            int port = (int) _FrontendConfig["Engines/"+engine+"/Port"];
            //string formatter = (string) _FrontendConfig["Engines/"+engine+"/Formatter"];
            string channel = (string) _FrontendConfig["Engines/"+engine+"/Channel"];
            
            IDictionary props = new Hashtable();
            props["port"] = "0";
            string error_msg = null;
            string connection_url = null;
            SessionManager sessm = null;
            switch (channel) {
                case "TCP":
                    if (ChannelServices.GetChannel("tcp") == null) {
                        // frontend -> engine
                        BinaryClientFormatterSinkProvider cprovider =
                            new BinaryClientFormatterSinkProvider();

                        // engine -> frontend (back-connection)
                        BinaryServerFormatterSinkProvider sprovider =
                            new BinaryServerFormatterSinkProvider();
                        // required for MS .NET 1.1
                        sprovider.TypeFilterLevel = TypeFilterLevel.Full;
                        
                        if (bindAddress != null) {
                            props["machineName"] = bindAddress;
                        }
                        ChannelServices.RegisterChannel(new TcpChannel(props, cprovider, sprovider));
                    }
                    connection_url = "tcp://"+hostname+":"+port+"/SessionManager"; 
#if LOG4NET
                    _Logger.Info("Connecting to: "+connection_url);
#endif
                    sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                        connection_url);
                    break;
#if CHANNEL_TCPEX
                case "TcpEx":
                    //props.Remove("port");
                    //props["name"] = "tcpex";
                    connection_url = "tcpex://"+hostname+":"+port+"/SessionManager"; 
                    if (ChannelServices.GetChannel("ExtendedTcp") == null) {
                        ChannelServices.RegisterChannel(new TcpExChannel(props, null, null));
                    }
    #if LOG4NET
                    _Logger.Info("Connecting to: "+connection_url);
    #endif
                    sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                        connection_url);
                    break;
#endif
#if CHANNEL_BIRDIRTCP
                case "BirDirTcp":
                    string ip = System.Net.Dns.Resolve(hostname).AddressList[0].ToString();
                    connection_url = "birdirtcp://"+ip+":"+port+"/SessionManager"; 
                    if (ChannelServices.GetChannel("birdirtcp") == null) {
                        ChannelServices.RegisterChannel(new BidirTcpClientChannel());
                    }
    #if LOG4NET
                    _Logger.Info("Connecting to: "+connection_url);
    #endif
                    sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                        connection_url);
                    break;
#endif
                case "HTTP":
                    connection_url = "http://"+hostname+":"+port+"/SessionManager"; 
                    if (ChannelServices.GetChannel("http") == null) {
                        ChannelServices.RegisterChannel(new HttpChannel());
                    }
#if LOG4NET
                    _Logger.Info("Connecting to: "+connection_url);
#endif
                    sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                        connection_url);
                    break;
                default:
                    throw new ApplicationException(String.Format(
                                    _("Unknown channel ({0}), "+
                                      "only following channel types are supported:"),
                                    channel) + " HTTP TCP");
            }
            _SessionManager = sessm;
            _EngineUrl = connection_url;
            
            _Session = sessm.Register(username, MD5.FromString(password), _FrontendUI);
            if (_Session == null) {
                throw new ApplicationException(_("Registration at engine failed, "+
                               "username and/or password was wrong, please verify them."));
            }
            
            _EngineVersion = sessm.EngineVersion;
            _UserConfig = new UserConfig(_Session.Config,
                                         username);
            _UserConfig.IsCaching = true;
        }

        public void Reconnect()
        {
            Connect(_Engine);
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
