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
            f_Keyfile = keyfile;
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
            
            if (f_Process != null) {
                f_Process.Dispose();
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
                    var msg = String.Format(
                        _("The local SSH forwarding port {0} is already in " +
                          "use. Is there an old SSH tunnel still active?"),
                        f_ForwardBindPort
                    );
                    throw new ApplicationException(msg);
                } catch (SocketException) {
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

            Version sshVersion = GetOpenSshVersion();
            // starting with OpenSSH version 4.4p1 we can use the
            // ExitOnForwardFailure option for detecting tunnel issues better
            // as the process will quit nicely, for more details see:
            // http://projects.qnetp.net/issues/show/145
            // NOTE: the patch level is mapped to the micro component
            if (sshVersion >= new Version("4.4.1")) {
                // exit if the tunnel setup didn't work somehow
                sshArguments += " -o ExitOnForwardFailure=yes";
            }

            // with OpenSSH 3.8 we can use the keep-alive feature of SSH that
            // will check the remote peer in defined intervals and kills the
            // tunnel if it reached the max value
            if (sshVersion >= new Version("3.8")) {
                // exit if the peer can't be reached for more than 90 seconds
                sshArguments += " -o ServerAliveInterval=30 -o ServerAliveCountMax=3";
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
            if (!String.IsNullOrEmpty(f_Keyfile)) {
                if (!File.Exists(f_Keyfile)) {
                    throw new ApplicationException(_("SSH keyfile not found."));
                }
                try {
                    using (File.OpenRead(f_Keyfile)) {}
                } catch (Exception ex) {
                    throw new ApplicationException(
                        _("SSH keyfile could not be read."), ex
                    );
                }
                sshArguments += String.Format(" -i \"{0}\"", f_Keyfile);
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

            string error;
            string output;
            int exitCode;
            using (var process = SysDiag.Process.Start(psi)) {
                error = process.StandardError.ReadToEnd();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

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
                exitCode,
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
            
            var sshVersion = GetPlinkVersionString();
            // Smuxi by default ships Plink of Quest PuTTY which allows to
            // accept any fingerprint but does _not_ work with pagent thus we
            // need to also support the regular plink if the user wants
            // ssh key authentication instead
            if (sshVersion.EndsWith("_q1.129")) {
                // HACK: don't ask for SSH key fingerprints
                // this is nasty but plink.exe can't ask for fingerprint
                // confirmation and thus the connect would always fail
                sshArguments += " -auto_store_key_in_cache";
            }

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
            if (!String.IsNullOrEmpty(f_Keyfile)) {
                if (!File.Exists(f_Keyfile)) {
                    throw new ApplicationException(_("SSH keyfile not found."));
                }
                try {
                    using (File.OpenRead(f_Keyfile)) {}
                } catch (Exception ex) {
                    throw new ApplicationException(
                        _("SSH keyfile could not be read."), ex
                    );
                }
                sshArguments += String.Format(" -i \"{0}\"", f_Keyfile);
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

        private string GetPlinkVersionString()
        {
            var startInfo = new SysDiag.ProcessStartInfo() {
                FileName = f_Program,
                Arguments = "-V",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            string error;
            string output;
            int exitCode;
            using (var process = SysDiag.Process.Start(startInfo)) {
                error = process.StandardError.ReadToEnd();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            Match match = Regex.Match(output, @"[0-9]+\.[0-9a-zA-Z_\.]+");
            if (match.Success) {
                var version = match.Value;
#if LOG4NET
                f_Logger.Debug("GetPlinkVersionString(): found version: " + version);
#endif
                return version;
            }

            string msg = String.Format(
                _("Plink version number not found (exit code: {0})\n\n" +
                  "SSH program: {1}\n\n" +
                  "Program Error:\n" +
                  "{2}\n" +
                  "Program Output:\n" +
                  "{3}\n"),
                exitCode,
                f_Program,
                error,
                output
            );
#if LOG4NET
            f_Logger.Error("GetPlinkVersionString(): " + msg);
#endif
            throw new ApplicationException(msg);
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }
    }
}
