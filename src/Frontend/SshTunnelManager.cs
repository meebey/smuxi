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
using System.Text.RegularExpressions;
using SysDiag = System.Diagnostics;
using Smuxi.Common;

namespace Smuxi.Frontend
{
    public class SshTunnelManager : IDisposable
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string   f_LibraryTextDomain = "smuxi-frontend";
        private SysDiag.Process          f_Process;
        private SysDiag.ProcessStartInfo f_ProcessStartInfo;
        private int                      f_RemotingBackChannelPort;
        private string                   f_Program;
        private string                   f_Parameters;
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
        
        public SshTunnelManager(string program, string parameters,
                                string username, string password, string keyfile,
                                string hostname, int port,
                                string forwardBindAddress, int forwardBindPort,
                                string forwardHostName, int forwardHostPort,
                                string backwardBindAddress, int backwardBindPort,
                                string backwardHostName, int backwardHostPort)
        {
            Trace.Call(program, parameters, username, "XXX", keyfile, hostname,
                       port, forwardBindAddress, forwardBindPort,
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
            f_Parameters = parameters;
            f_Username = username;
            f_Password = password;
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
            Trace.Call();

            if (String.IsNullOrEmpty(f_Program)) {
                // use plink by default if it's there
                if (File.Exists("plink.exe")) {
                    f_Program = "plink.exe";
                } else {
                    // TODO: find ssh
                    f_Program = "/usr/bin/ssh";
                }
            }
            if (!File.Exists(f_Program)) {
                throw new ApplicationException(_("SSH client application was not found: " + f_Program));
            }
            if (f_Program.ToLower().EndsWith("putty.exe")) {
                throw new ApplicationException(_("SSH client must be either OpenSSH (ssh) or Plink (plink.exe, not putty.exe)"));
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

            // make sure the tunnel is killed when smuxi is quitting
            // BUG: this will not kill the tunnel if Smuxi was killed using a
            // process signal like SIGTERM! Not sure how to handle that case...
            System.AppDomain.CurrentDomain.ProcessExit += delegate {
#if LOG4NET
                f_Logger.Debug("Setup(): our process is exiting, let's dispose!");
#endif
                Dispose();
            };
        }
        
        public void Connect()
        {
            Trace.Call();

#if LOG4NET
            f_Logger.Debug("Connect(): checking if local forward port is free...");
#endif
            using (TcpClient tcpClient = new TcpClient()) {
                try {
                    tcpClient.Connect(f_ForwardBindAddress, f_ForwardBindPort);
                    // the connect worked, panic!
                    var msg = _("Local SSH forward port is already in use. "+
                                "Old SSH tunnel still active?");
                    throw new ApplicationException(msg);
                } catch (SocketException ex) {
                }
            }

#if LOG4NET
            f_Logger.Debug("Connect(): setting up ssh tunnel using command: " +
                           f_ProcessStartInfo.FileName + " " +
                           f_ProcessStartInfo.Arguments); 
#endif
            f_Process = SysDiag.Process.Start(f_ProcessStartInfo);

            // lets assume the tunnel didn't fail yet as long as the process is
            // still running and keep checking if the port is ready during that
            bool forwardPortReady = false, backwardPortReady  = false;
            while (!forwardPortReady || !backwardPortReady) {
                if (f_Process.HasExited) {
                    string output = f_Process.StandardOutput.ReadToEnd();
                    string error = f_Process.StandardError.ReadToEnd();
                    string msg = String.Format(
                        _("SSH tunnel setup failed (exit code: {0})\n\n" +
                          "SSH program: {1}\n" +
                          "SSH parameters: {2}\n\n" +
                          "Program Error:\n" +
                          "{3}\n" +
                          "Program Output:\n" +
                          "{4}\n"),
                        f_Process.ExitCode,
                        f_ProcessStartInfo.FileName,
                        f_ProcessStartInfo.Arguments,
                        error,
                        output
                    );
#if LOG4NET
                    f_Logger.Error("Connect(): " + msg);
#endif
                    throw new ApplicationException(msg);
                }

                // check forward port
                using (TcpClient tcpClient = new TcpClient()) {
                    try {
                        tcpClient.Connect(f_ForwardBindAddress, f_ForwardBindPort);
#if LOG4NET
                        f_Logger.Debug("Connect(): ssh tunnel's forward port is ready");
#endif
                        forwardPortReady = true;
                    } catch (SocketException ex) {
#if LOG4NET
                        f_Logger.Debug("Connect(): ssh tunnel's forward port is not reading yet, retrying...", ex);
#endif
                    }
                }

                backwardPortReady = true;
                // we can't test the back-port as the .NET remoting channel
                // would need to be ready at this point, which isn't
                /*
                // check backward port
                using (TcpClient tcpClient = new TcpClient()) {
                    try {
                        tcpClient.Connect(f_BackwardBindAddress, f_BackwardBindPort);
#if LOG4NET
                        f_Logger.Debug("Connect(): ssh tunnel's backward port is ready");
#endif
                        backwardPortReady = true;
                    } catch (SocketException ex) {
#if LOG4NET
                        f_Logger.Debug("Connect(): ssh tunnel's backward port is not reading yet, retrying...", ex);
#endif
                    }
                }
                */
#if LOG4NET
                f_Logger.Info("Connect(): ssh tunnel is not ready yet, retrying...");
#endif
                System.Threading.Thread.Sleep(1000);
            }
#if LOG4NET
            f_Logger.Info("Connect(): ssh tunnel is ready");
#endif
        }
        
        public void Disconnect()
        {
            Trace.Call();

            if (f_Process != null && !f_Process.HasExited) {
#if LOG4NET
                f_Logger.Debug("Disconnect(): killing ssh tunnel...");
#endif
                f_Process.Kill();
                f_Process.WaitForExit();
#if LOG4NET
                f_Logger.Debug("Disconnect(): ssh tunnel exited");
#endif
            }
        }
        
        private SysDiag.ProcessStartInfo CreateOpenSshProcessStartInfo()
        {
            string sshArguments = String.Empty;

            // starting with OpenSSH version 4.4p1 we can use the
            // ExitOnForwardFailure option for detecting tunnel issues better
            // as the process will quit nicely, for more details see:
            // http://projects.qnetp.net/issues/show/145
            // NOTE: the patch level is mapped to the micro component
            if (GetOpenSshVersion() >= new Version("4.4.1")) {
                // exit if the tunnel setup didn't work somehow
                sshArguments += " -o ExitOnForwardFailure=yes";
            }
            
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

            // custom ssh parameters
            sshArguments += String.Format(" {0}", f_Parameters);

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

        private Version GetOpenSshVersion()
        {
            SysDiag.ProcessStartInfo psi = new SysDiag.ProcessStartInfo();
            psi.FileName = f_Program;
            psi.Arguments = "-V";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            SysDiag.Process process = SysDiag.Process.Start(psi);
            string error = process.StandardError.ReadToEnd();
            string output = process.StandardOutput.ReadToEnd();
            string haystack;
            // we expect the version output on stderr
            if (error.Length > 0) {
                haystack = error;
            } else {
                haystack = output;
            }
            Match match = Regex.Match(haystack, @"OpenSSH[_\w](\d+).(\d+)(?:.(\d+))?");
            if (match.Success) {
                string major, minor, micro;
                string version = null;
                if (match.Groups.Count >= 3) {
                    major = match.Groups[1].Value;
                    minor = match.Groups[2].Value;
                    version = String.Format("{0}.{1}", major, minor);
                }
                if (match.Groups.Count >= 4) {
                    micro = match.Groups[3].Value;
                    version = String.Format("{0}.{1}", version, micro);
                }
#if LOG4NET
                f_Logger.Debug("GetOpenSshVersion(): found version: " + version);
#endif
                return new Version(version);
            }

            string msg = String.Format(
                _("OpenSSH version number not found (exit code: {0})\n\n" +
                  "SSH program: {1}\n\n" +
                  "Program Error:\n" +
                  "{2}\n" +
                  "Program Output:\n" +
                  "{3}\n"),
                f_Process.ExitCode,
                f_Program,
                error,
                output
            );
#if LOG4NET
            f_Logger.Error("GetOpenSshVersion(): " + msg);
#endif
            throw new ApplicationException(msg);
        }

        private SysDiag.ProcessStartInfo CreatePlinkProcessStartInfo()
        {
            string sshArguments = String.Empty;
            
            // HACK: don't ask for SSH key fingerprints
            // this is nasty but plink.exe can't ask for fingerprint
            // confirmation and thus the connect would always fail
            sshArguments += " -auto_store_key_in_cache";
            
            // no interactive mode please
            sshArguments += " -batch";
            // don't execute a remote command
            sshArguments += " -N";
            
            // HACK: force SSH to always flush the send buffer, as needed by
            // .NET remoting just like the X11 protocol
            sshArguments += " -X";
            
            if (String.IsNullOrEmpty(f_Username)) {
                throw new ApplicationException(_("PuTTY / Plink requires a username to be set."));
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

            // custom ssh parameters
            sshArguments += String.Format(" {0}", f_Parameters);

            // ssh host
            sshArguments += String.Format(" {0}", f_Hostname);

            SysDiag.ProcessStartInfo psi = new SysDiag.ProcessStartInfo();
            psi.FileName = f_Program;
            psi.Arguments = sshArguments;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            return psi;
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }
    }
}
