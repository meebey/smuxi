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
        private SessionManager  f_SessionManager;
        private FrontendConfig  f_FrontendConfig;
        private IFrontendUI     f_FrontendUI;
        private string          f_Engine;
        private string          f_EngineUrl;
        private Version         f_EngineVersion;
        private UserConfig      f_UserConfig;
        private Session         f_Session;
        private SysDiag.Process f_SshTunnelProcess;
        
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
            string sshHostname = (string) f_FrontendConfig["Engines/"+engine+"/SshHostname"];
            int sshPort = -1;
            if (f_FrontendConfig["Engines/"+engine+"/SshPort"] != null) {
                sshPort = (int) f_FrontendConfig["Engines/"+engine+"/SshPort"];
            }
            string sshUsername = (string) f_FrontendConfig["Engines/"+engine+"/SshUsername"];
            string sshPassword = (string) f_FrontendConfig["Engines/"+engine+"/SshPassword"];
            
            int remotingPort = 0;
            if (useSshTunnel) {
                // find free remoting back-channel port
                TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                remotingPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
#if LOG4NET
                f_Logger.Debug("Connect(): found local free port for remoting back-channel: " + remotingPort);
#endif
                
                if (String.IsNullOrEmpty(sshProgram)) {
                    // TODO: find ssh
                    sshProgram = "/usr/bin/ssh";
                }
                if (!File.Exists(sshProgram)) {
                    throw new ApplicationException(_("SSH client application was not found: " + sshProgram));
                }
                if (sshProgram.ToLower().EndsWith("putty.exe")) {
                    throw new ApplicationException(_("SSH client must be either OpenSSH (ssh) or Plink (plink.exe, _not_ putty.exe)"));
                }
                
                bool isPutty = false;
                if (sshProgram.ToLower().EndsWith("plink.exe")) {
                    isPutty = true;
                }
                
                SysDiag.ProcessStartInfo psi; 
                if (isPutty) {
                    psi = CreatePlinkProcessStartInfo(hostname, port, remotingPort,
                                                      sshProgram, sshUsername,
                                                      sshPassword, null,
                                                      sshHostname, sshPort);
                } else {
                    psi = CreateOpenSshProcessStartInfo(hostname, port, remotingPort,
                                                        sshProgram, sshUsername,
                                                        sshPassword, null,
                                                        sshHostname, sshPort);
                }
                
#if LOG4NET
                f_Logger.Debug("Connect(): setting up ssh tunnel using command: " + psi.FileName + " " + psi.Arguments); 
#endif
                f_SshTunnelProcess = SysDiag.Process.Start(psi);

                // HACK: give the process some time to fail (exiting)
                System.Threading.Thread.Sleep(2000);
                
                /*
                // wait till the tunnels are ready and timeout after 30 seconds
                bool exited = f_SshTunnelProcess.WaitForExit(30 * 1000);
                
                if (!exited) {
                    string msg = String.Format(_("Timeout setting SSH tunnel up (30 seconds)."));
#if LOG4NET
                    f_Logger.Error("Connect(): " + msg);
                    f_Logger.Debug("Connect(): killing SSH tunnel process...");
#endif
                    f_SshTunnelProcess.Kill();
                    
                    throw new ApplicationException(msg);
                }
                */
                
                if (f_SshTunnelProcess.HasExited && f_SshTunnelProcess.ExitCode != 0) {
                    string output = f_SshTunnelProcess.StandardOutput.ReadToEnd();
                    string error = f_SshTunnelProcess.StandardError.ReadToEnd();
                    string msg = String.Format(
                        _("SSH tunnel setup failed with (exit code: {0})\n\n" +
                          "SSH program: {1}\n\n" +
                          "Program Error:\n" +
                          "{2}\n" +
                          "Prgram Output:\n" +
                          "{3}\n"),
                        f_SshTunnelProcess.ExitCode,
                        sshProgram,
                        error,
                        output
                    );
#if LOG4NET
                    f_Logger.Error("Connect(): " + msg);
#endif
                    throw new ApplicationException(msg);
                }
                
                // so we want connect to connect via the SSH tunnel now
                // (no need to override the port, as we use the same port as the smuxi-server)
                hostname = "127.0.0.1";
                
                // the smuxi-server has to connect via the SSH tunnel too
                bindAddress = "127.0.0.1";
            }
            
            IDictionary props = new Hashtable();
            // ugly remoting expects the port as string ;)
            props["port"] = remotingPort.ToString();
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
                        ChannelServices.RegisterChannel(new TcpChannel(props, cprovider, sprovider), false);
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
                                    _("Unknown channel ({0}), "+
                                      "only following channel types are supported:"),
                                    channel) + " HTTP TCP");
            }
            f_SessionManager = sessm;
            f_EngineUrl = connection_url;
            
            f_Session = sessm.Register(username, MD5.FromString(password), f_FrontendUI);
            if (f_Session == null) {
                throw new ApplicationException(_("Registration at engine failed, "+
                               "username and/or password was wrong, please verify them."));
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
            
            if (f_SshTunnelProcess != null && !f_SshTunnelProcess.HasExited) {
                f_SshTunnelProcess.Kill();
            }
        }
        
        private SysDiag.ProcessStartInfo CreateOpenSshProcessStartInfo(
                string smuxiHostname, int smuxiPort, int remotingPort,
                string sshProgram, string sshUsername,
                string sshPassword, string sshKeyfile,
                string sshHostname, int sshPort)
        {
            Trace.Call(sshProgram, sshUsername, "XXX", sshKeyfile, sshHostname, sshPort);
        
            string sshArguments = String.Empty;

            // don't ask for SSH key fingerprints
            // TOO NASTY!
            //sshCommand += " -o StrictHostKeyChecking=no";
            // exit if the tunnel setup didn't work somehow
            sshArguments += " -o ExitOnForwardFailure=yes";
            
            // run in the background (detach)
            // plink doesn't support this and we can't control the process this way!
            //sshArguments += " -f";
            
            // don't execute a remote command
            sshArguments += " -N";
            
            // HACK: force SSH to always flush the send buffer, as needed by
            // .NET remoting just like the X11 protocol
            sshArguments += " -X";
            
            if (!String.IsNullOrEmpty(sshUsername)) {
                sshArguments += String.Format(" -l {0}", sshUsername);
            }
            if (!String.IsNullOrEmpty(sshPassword)) {
                // TODO: pass password,  but how?
            }
            if (sshPort != -1) {
                sshArguments += String.Format(" -p {0}", sshPort);
            }
            
            // ssh tunnel
            sshArguments += String.Format(" -L 127.0.0.1:{0}:{1}:{2}", smuxiPort, smuxiHostname, smuxiPort);
            
            // ssh back tunnel
            sshArguments += String.Format(" -R {0}:127.0.0.1:{1}", remotingPort, remotingPort);
            
            // ssh host
            sshArguments += String.Format(" {0}", sshHostname);
        
            SysDiag.ProcessStartInfo psi = new SysDiag.ProcessStartInfo();
            psi.FileName = sshProgram;
            psi.Arguments = sshArguments;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            return psi;
        }
        
        private SysDiag.ProcessStartInfo CreatePlinkProcessStartInfo(
                string smuxiHostname, int smuxiPort, int remotingPort,
                string sshProgram, string sshUsername,
                string sshPassword, string sshKeyfile,
                string sshHostname, int sshPort)
        {
            Trace.Call(sshProgram, sshUsername, "XXX", sshKeyfile, sshHostname, sshPort);
            
            string sshArguments = String.Empty;
            
            // don't ask for SSH key fingerprints
            // TOO NASTY!
            //sshArguments += " -auto_store_key_in_cache";
            
            // no interactive mode please
            sshArguments += " -batch";
            // don't execute a remote command
            sshArguments += " -N";
            
            // HACK: force SSH to always flush the send buffer, as needed by
            // .NET remoting just like the X11 protocol
            sshArguments += " -X";
            
            if (String.IsNullOrEmpty(sshUsername)) {
                throw new ApplicationException(_("PuTTY / Plink requeries a username to be set."));
            }
            sshArguments += String.Format(" -l {0}", sshUsername);
            
            if (!String.IsNullOrEmpty(sshPassword)) {
                sshArguments += String.Format(" -pw {0}", sshPassword);
            }
            
            if (sshPort != -1) {
                sshArguments += String.Format(" -P {0}", sshPort);
            }
            
            // ssh tunnel
            sshArguments += String.Format(" -L 127.0.0.1:{0}:{1}:{2}", smuxiPort, smuxiHostname, smuxiPort);
            
            // ssh back tunnel
            sshArguments += String.Format(" -R {0}:127.0.0.1:{1}", remotingPort, remotingPort);
            
            // ssh host
            sshArguments += String.Format(" {0}", sshHostname);
        
            SysDiag.ProcessStartInfo psi = new SysDiag.ProcessStartInfo();
            psi.FileName = sshProgram;
            psi.Arguments = sshArguments;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            return psi;
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
