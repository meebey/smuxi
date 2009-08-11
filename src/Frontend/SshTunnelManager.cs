// $Id$
//
// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2009 Mirco Bauer <meebey@meebey.net>
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using SysDiag = System.Diagnostics;
using Smuxi.Common;

namespace Smuxi.Frontend
{
    public class SshTunnelManager : IDisposable
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private SysDiag.Process          f_Process;
        private SysDiag.ProcessStartInfo f_ProcessStartInfo;
        private int                      f_RemotingBackChannelPort;
        private string                   f_Program;
        private string                   f_Username;
        private string                   f_Password;
        private string                   f_Keyfile;
        private string                   f_Hostname;
        private int                      f_Port = -1;
        private string                   f_ForwardBindAddress;
        private int                      f_ForwardBindPort;
        private string                   f_ForwardHostName;
        private int                      f_ForwardHostPort;
        private string                   f_BackwardBindAddress;
        private int                      f_BackwardBindPort;
        private string                   f_BackwardHostName;
        private int                      f_BackwardHostPort;
        
        public SshTunnelManager(string program, string username,
                                string password, string keyfile,
                                string hostname, int port,
                                string forwardBindAddress, int forwardBindPort,
                                string forwardHostName, int forwardHostPort,
                                string backwardBindAddress, int backwardBindPort,
                                string backwardHostName, int backwardHostPort)
        {
            Trace.Call(program, username, "XXX", keyfile, hostname, port,
                       forwardBindAddress, forwardBindPort,
                       forwardHostName, forwardHostPort,
                       backwardBindAddress, backwardBindPort,
                       backwardHostName, backwardHostPort);
            
            if (hostname == null) {
                throw new ArgumentNullException("hostname");
            }
            if (forwardBindAddress == null) {
                throw new ArgumentNullException("forwardBindAddress");
            }
            if (forwardHostName == null) {
                throw new ArgumentNullException("forwardHostName");
            }
            if (backwardBindAddress == null) {
                throw new ArgumentNullException("backwardBindAddress");
            }
            if (backwardHostName == null) {
                throw new ArgumentNullException("backwardHostName");
            }
            
            f_Program = program;
            f_Username = username;
            f_Hostname = hostname;
            f_Port = port;
            
            f_ForwardBindAddress = forwardBindAddress;
            f_ForwardBindPort    = forwardBindPort;
            f_ForwardHostName    = forwardHostName;
            f_ForwardHostPort    = forwardHostPort;
            
            f_BackwardBindAddress = backwardBindAddress;
            f_BackwardBindPort    = backwardBindPort;
            f_BackwardHostName    = backwardHostName;
            f_BackwardHostPort    = backwardHostPort;
        }
        
        ~SshTunnelManager()
        {
            Trace.Call();
            
            Dispose(false);
        }
        
        public void Dispose()
        {
            Trace.Call();
            
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            Trace.Call(disposing);
            
            if (disposing) {
                if (f_Process != null) {
                    f_Process.Dispose();
                }
            }
        }
        
        public void Setup()
        {
            if (String.IsNullOrEmpty(f_Program)) {
                // TODO: find ssh
                f_Program = "/usr/bin/ssh";
            }
            if (!File.Exists(f_Program)) {
                throw new ApplicationException(_("SSH client application was not found: " + f_Program));
            }
            if (f_Program.ToLower().EndsWith("putty.exe")) {
                throw new ApplicationException(_("SSH client must be either OpenSSH (ssh) or Plink (plink.exe, _not_ putty.exe)"));
            }
            
            bool isPutty = false;
            if (f_Program.ToLower().EndsWith("plink.exe")) {
                isPutty = true;
            }
            
            if (isPutty) {
                f_ProcessStartInfo = CreatePlinkProcessStartInfo();
            } else {
                f_ProcessStartInfo = CreateOpenSshProcessStartInfo();
            }
        }
        
        public void Connect()
        {
#if LOG4NET
            f_Logger.Debug("Connect(): setting up ssh tunnel using command: " +
                           f_ProcessStartInfo.FileName + " " +
                           f_ProcessStartInfo.Arguments); 
#endif
            f_Process = SysDiag.Process.Start(f_ProcessStartInfo);

            // lets assume the tunnel didn't fail yet as long as the process is
            // still running and keep checking if the port is ready during that
            bool connected = false;
            while (!connected) {
                if (f_Process.HasExited && f_Process.ExitCode != 0) {
                    string output = f_Process.StandardOutput.ReadToEnd();
                    string error = f_Process.StandardError.ReadToEnd();
                    string msg = String.Format(
                        _("SSH tunnel setup failed with (exit code: {0})\n\n" +
                          "SSH program: {1}\n\n" +
                          "Program Error:\n" +
                          "{2}\n" +
                          "Prgram Output:\n" +
                          "{3}\n"),
                        f_Process.ExitCode,
                        f_Program,
                        error,
                        output
                    );
#if LOG4NET
                    f_Logger.Error("Connect(): " + msg);
#endif
                    throw new ApplicationException(msg);
                }

                using (TcpClient tcpClient = new TcpClient()) {
                    try {
                        tcpClient.Connect(f_ForwardBindAddress, f_ForwardBindPort);
                        connected = true;
                    } catch (SocketException ex) {
#if LOG4NET
                        f_Logger.Info("Connect(): ssh tunnel is not reading yet, retrying...", ex);
#endif
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
        }
        
        public void Disconnect()
        {
            if (f_Process != null && !f_Process.HasExited) {
                f_Process.Kill();
            }
        }
        
        private SysDiag.ProcessStartInfo CreateOpenSshProcessStartInfo()
        {
            string sshArguments = String.Empty;

            // exit if the tunnel setup didn't work somehow
            sshArguments += " -o ExitOnForwardFailure=yes";
            
            // run in the background (detach)
            // plink doesn't support this and we can't control the process this way!
            //sshArguments += " -f";
            
            // don't execute a remote command
            sshArguments += " -N";
            
            // HACK: force SSH to always flush the send buffer, as needed by
            // .NET Remoting just like the X11 protocol
            sshArguments += " -X";
            
            if (!String.IsNullOrEmpty(f_Username)) {
                sshArguments += String.Format(" -l {0}", f_Username);
            }
            if (!String.IsNullOrEmpty(f_Password)) {
                // TODO: pass password,  but how?
            }
            if (f_Port != -1) {
                sshArguments += String.Format(" -p {0}", f_Port);
            }
            
            // ssh tunnel
            sshArguments += String.Format(
                " -L {0}:{1}:{2}:{3}",
                f_ForwardBindAddress,
                f_ForwardBindPort,
                f_ForwardHostName,
                f_ForwardHostPort
            );
            
            // ssh back tunnel
            sshArguments += String.Format(
                " -R {0}:{1}:{2}:{3}",
                f_BackwardBindAddress,
                f_BackwardBindPort,
                f_BackwardHostName,
                f_BackwardHostPort
            );
            
            // ssh host
            sshArguments += String.Format(" {0}", f_Hostname);
        
            SysDiag.ProcessStartInfo psi = new SysDiag.ProcessStartInfo();
            psi.FileName = f_Program;
            psi.Arguments = sshArguments;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            return psi;
        }
        
        private SysDiag.ProcessStartInfo CreatePlinkProcessStartInfo()
        {
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
            
            if (String.IsNullOrEmpty(f_Username)) {
                throw new ApplicationException(_("PuTTY / Plink requeries a username to be set."));
            }
            sshArguments += String.Format(" -l {0}", f_Username);
            
            if (!String.IsNullOrEmpty(f_Password)) {
                sshArguments += String.Format(" -pw {0}", f_Password);
            }
            
            if (f_Port != -1) {
                sshArguments += String.Format(" -P {0}", f_Port);
            }
            
            // ssh tunnel
            sshArguments += String.Format(
                " -L {0}:{1}:{2}:{3}",
                f_ForwardBindAddress,
                f_ForwardBindPort,
                f_ForwardHostName,
                f_ForwardHostPort
            );

            // ssh back tunnel
            sshArguments += String.Format(
                " -R {0}:{1}:{2}:{3}",
                f_BackwardBindAddress,
                f_BackwardBindPort,
                f_BackwardHostName,
                f_BackwardHostPort
            );

            // ssh host
            sshArguments += String.Format(" {0}", f_Hostname);
        
            SysDiag.ProcessStartInfo psi = new SysDiag.ProcessStartInfo();
            psi.FileName = f_Program;
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
