/*
 * $Id: Frontend.cs 73 2005-06-27 12:42:06Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/Frontend.cs $
 * $Rev: 73 $
 * $Author: meebey $
 * $Date: 2005-06-27 14:42:06 +0200 (Mon, 27 Jun 2005) $
 *
 * Smuxi - Smart MUltipleXed Irc
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using SysDiag = System.Diagnostics;
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
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string f_LibraryTextDomain = "smuxi-frontend";
        private SessionManager  f_SessionManager;
        private FrontendConfig  f_FrontendConfig;
        private IFrontendUI     f_FrontendUI;
        private string          f_Engine;
        private string          f_EngineUrl;
        private Version         f_EngineVersion;
        private UserConfig      f_UserConfig;
        private Session         f_Session;
        private SshTunnelManager f_SshTunnelManager;
        private string          f_ChannelName;

        public SessionManager SessionManager {
            get {
                return f_SessionManager;
            }
        }
        
        public string EngineUrl {
            get {
                return f_EngineUrl;
            }
        }
        
        public Version EngineVersion {
            get {
                return f_EngineVersion;
            }
        }
        
        public Session Session {
            get {
                return f_Session;
            }
        }
        
        public UserConfig UserConfig {
            get {
                return f_UserConfig;
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
            
            f_FrontendConfig = frontendConfig;
            f_FrontendUI = frontendUI;
        }
        
        public void Connect(string engine)
        {
            Trace.Call(engine);

            if (engine == null) {
                throw new ArgumentNullException("engine");
            }
            if (engine.Length == 0) {
                throw new ArgumentException(_("Engine must not be empty."), "engine");
            }

            bool engineFound = false;
            foreach (var entry in (string[]) f_FrontendConfig["Engines/Engines"]) {
                if (entry == engine) {
                    engineFound = true;
                    break;
                }
            }
            if (!engineFound) {
                throw new ArgumentException(_("Engine does not exist."), "engine");
            }

            f_Engine = engine;
            string username = (string) f_FrontendConfig["Engines/"+engine+"/Username"];
            string password = (string) f_FrontendConfig["Engines/"+engine+"/Password"];
            string hostname = (string) f_FrontendConfig["Engines/"+engine+"/Hostname"];
            string bindAddress = (string) f_FrontendConfig["Engines/"+engine+"/BindAddress"];
            int port = (int) f_FrontendConfig["Engines/"+engine+"/Port"];
            //string formatter = (string) _FrontendConfig["Engines/"+engine+"/Formatter"];
            string channel = (string) f_FrontendConfig["Engines/"+engine+"/Channel"];
            
            // SSH tunnel support
            bool useSshTunnel = false;
            if (f_FrontendConfig["Engines/"+engine+"/UseSshTunnel"] != null) {
                useSshTunnel = (bool) f_FrontendConfig["Engines/"+engine+"/UseSshTunnel"];
            }
            string sshProgram = (string) f_FrontendConfig["Engines/"+engine+"/SshProgram"];
            string sshParameters = (string) f_FrontendConfig["Engines/"+engine+"/SshParameters"];
            string sshHostname = (string) f_FrontendConfig["Engines/"+engine+"/SshHostname"];
            int sshPort = -1;
            if (f_FrontendConfig["Engines/"+engine+"/SshPort"] != null) {
                sshPort = (int) f_FrontendConfig["Engines/"+engine+"/SshPort"];
            }
            string sshUsername = (string) f_FrontendConfig["Engines/"+engine+"/SshUsername"];
            string sshPassword = (string) f_FrontendConfig["Engines/"+engine+"/SshPassword"];
            var sshKeyfile = (string) f_FrontendConfig["Engines/"+engine+"/SshKeyfile"];

            // OPT: always use SSH compression (both openssh and plink support it)
            // this reduces the .NET remoting traffic by about 75%
            if (String.IsNullOrEmpty(sshParameters) ||
                !sshParameters.Contains(" -C")) {
                sshParameters += " -C";
            }

            int remotingPort = 0;
            if (useSshTunnel) {
                // find free remoting back-channel port
                TcpListener remotingPortListener = new TcpListener(IPAddress.Loopback, 0);
                remotingPortListener.Start();
                remotingPort = ((IPEndPoint)remotingPortListener.LocalEndpoint).Port;
                
                // find free local forward port
                TcpListener localForwardListener = new TcpListener(IPAddress.Loopback, 0);
                localForwardListener.Start();
                int localForwardPort = ((IPEndPoint)localForwardListener.LocalEndpoint).Port;
                
                // only stop the listeners after we got all ports we need
                // else it might re-use a port!
                remotingPortListener.Stop();
                localForwardListener.Stop();
#if LOG4NET
                f_Logger.Debug("Connect(): found free local backward port (for remoting back-channel): " + remotingPort);
                f_Logger.Debug("Connect(): found free local forward port: " + localForwardPort);
#endif

                // HACK: we can't use localForwardPort here as .NET remoting
                // will announce the server port in the server Session object
                // thus the client will try to reach it using the original
                // server port :(
                f_SshTunnelManager = new SshTunnelManager(
                    sshProgram, sshParameters, sshUsername, sshPassword,
                    sshKeyfile, sshHostname, sshPort,
                    //"127.0.0.1", localForwardPort, "127.0.0.1", port,
                    "127.0.0.1", port, "127.0.0.1", port,
                    "127.0.0.1", remotingPort, "127.0.0.1", remotingPort
                );
                f_SshTunnelManager.Setup();
                f_SshTunnelManager.Connect();
                
                // so we want to connect via the SSH tunnel now
                hostname = "127.0.0.1";
                // HACK: see above
                //port = localForwardPort;
                
                // the smuxi-server has to connect to us via the SSH tunnel too
                bindAddress = "127.0.0.1";
            }
            
            IDictionary props = new Hashtable();
            // ugly remoting expects the port as string ;)
            props["port"] = remotingPort.ToString();
            string connection_url = null;
            SessionManager sessm = null;
            switch (channel) {
                case "TCP":
                    // Make sure the channel is really using our random
                    // remotingPort. Already registered channel will for sure
                    // not to that and thus the back-connection fails!
                    if (f_ChannelName != null) {
                        IChannel oldChannel = ChannelServices.GetChannel(f_ChannelName);
                        if (oldChannel != null) {
#if LOG4NET
                            f_Logger.Debug("Connect(): found old remoting channel, unregistering...");
#endif
                            ChannelServices.UnregisterChannel(oldChannel);
                        }
                    }

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
                    var tcpChannel = new TcpChannel(props, cprovider, sprovider);
                    f_ChannelName = tcpChannel.ChannelName;
                    ChannelServices.RegisterChannel(tcpChannel, false);

                    // make sure the listen port of channel is ready before we
                    // connect to the engine, as it will make a call back!
                    while (true) {
                        using (TcpClient tcpClient = new TcpClient()) {
                            try {
                                tcpClient.Connect(hostname, port);
#if LOG4NET
                                f_Logger.Debug("Connect(): listen port of remoting channel is ready");
#endif
                                break;
                            } catch (SocketException ex) {
#if LOG4NET
                                f_Logger.Debug("Connect(): listen port of remoting channel is not reading yet, retrying...", ex);
#endif
                            }
                            System.Threading.Thread.Sleep(1000);
                        }
                    }

                    connection_url = "tcp://"+hostname+":"+port+"/SessionManager";
#if LOG4NET
                    f_Logger.Info("Connecting to: "+connection_url);
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
                        ChannelServices.RegisterChannel(new HttpChannel(), false);
                    }
#if LOG4NET
                    f_Logger.Info("Connecting to: "+connection_url);
#endif
                    sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                        connection_url);
                    break;
                default:
                    throw new ApplicationException(String.Format(
                                    _("Unknown channel ({0}) - "+
                                      "only the following channel types are supported:"),
                                    channel) + " HTTP TCP");
            }
            f_SessionManager = sessm;
            f_EngineUrl = connection_url;
            
            f_Session = sessm.Register(username, MD5.FromString(password), f_FrontendUI);
            if (f_Session == null) {
                throw new ApplicationException(_("Registration with engine failed!  "+
                               "The username and/or password were wrong - please verify them."));
            }
            
            f_EngineVersion = sessm.EngineVersion;
            f_UserConfig = new UserConfig(f_Session.Config,
                                         username);
            f_UserConfig.IsCaching = true;
        }

        public void Reconnect()
        {
            Trace.Call();
            
            Disconnect();
            Connect(f_Engine);
        }
        
        public void Disconnect()
        {
            Trace.Call();

            // HACK: the transparent proxy object is not automatically updating
            // changed channel data and thus will re-use the obsolete TCP port
            // for the next remoting back connection, thus we have to destroy
            // the proxy object here!
            RemotingServices.Disconnect((MarshalByRefObject) f_FrontendUI);

            if (f_SshTunnelManager != null) {
                f_SshTunnelManager.Disconnect();
                f_SshTunnelManager.Dispose();
                f_SshTunnelManager = null;
            }
        }
        
        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }
    }
}
