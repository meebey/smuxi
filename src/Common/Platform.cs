// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Smuxi.Common
{
    public static class Platform
    {
        public static string OperatingSystem {
            get {
                // uname present?
                try {
                    var pinfo = new ProcessStartInfo("uname");
                    pinfo.UseShellExecute = false;
                    pinfo.RedirectStandardOutput = true;
                    pinfo.RedirectStandardError = true;
                    Process.Start(pinfo).WaitForExit();
                } catch (Exception) {
                    // fall back to runtime detector
                    return Environment.OSVersion.Platform.ToString();
                }

                string os = null;
                // GNU/Linux
                // GNU/kFreeBSD
                var info = new ProcessStartInfo("uname", "-o");
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                var process = Process.Start(info);
                process.WaitForExit();
                if (process.ExitCode == 0) {
                    os = process.StandardOutput.ReadLine();
                }

                if (String.IsNullOrEmpty(os)) {
                    // not all operating systems support -o so lets fallback to -s
                    // Linux
                    // FreeBSD
                    // Darwin
                    info = new ProcessStartInfo("uname", "-s");
                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;
                    process = Process.Start(info);
                    process.WaitForExit();
                    if (process.ExitCode == 0) {
                        os = process.StandardOutput.ReadLine();
                    }
                }

                if (String.IsNullOrEmpty(os)) {
                    return "Unknown";
                }

                string distro = null;
                try {
                    info = new ProcessStartInfo("lsb_release", "-i");
                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;
                    process = Process.Start(info);
                    process.WaitForExit();
                    if (process.ExitCode == 0) {
                        distro = process.StandardOutput.ReadLine();
                        var match = Regex.Match(distro,
                                                @"^Distributor ID:\s+(.+)");
                        if (match.Success && match.Groups.Count > 1) {
                            distro = match.Groups[1].Value;
                        } else {
                            distro = null;
                        }
                    }
                } catch (Exception) {
                }

                if (String.IsNullOrEmpty(distro)) {
                    return os;
                }

                return String.Format("{0} ({1})", os, distro);
            }
        }
        
        public static string Architecture {
            get {
                // uname present?
                try {
                    var pinfo = new ProcessStartInfo("uname");
                    pinfo.UseShellExecute = false;
                    pinfo.RedirectStandardOutput = true;
                    Process.Start(pinfo).WaitForExit();
                } catch (Exception) {
                    // no uname
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                        // x86
                        // AMD64
                        var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
                        if (!String.IsNullOrEmpty(arch)) {
                            return arch;
                        }
                        arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                        if (!String.IsNullOrEmpty(arch)) {
                            return arch;
                        }
                    }

                    // fall back to pointer size
                    return String.Format("{0}-bit", IntPtr.Size * 8);
                }

                // i386
                // i686
                // x86_64
                var info = new ProcessStartInfo("uname", "-m");
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                var process = Process.Start(info);
                process.WaitForExit();
                if (process.ExitCode == 0) {
                    return process.StandardOutput.ReadLine();
                }

                return "Unknown";
            }
        }

        public static string LogPath {
            get {
                var logPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                );
                logPath = Path.Combine(logPath, "smuxi");
                // FIXME: include session username
                logPath = Path.Combine(logPath, "logs");
                return logPath;
            }
        }

        public static string CachePath {
            get {
                string cachePath = null;
                if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                    cachePath = Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData
                    );
                    cachePath = Path.Combine(cachePath, "smuxi");
                    cachePath = Path.Combine(cachePath, "cache");
                } else {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    var xdgCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
                    if (String.IsNullOrEmpty(xdgCache)) {
                        xdgCache = Path.Combine(home, ".cache");
                    }
                    cachePath = Path.Combine(xdgCache, "smuxi");
                }
                if (!Directory.Exists(cachePath)) {
                    Directory.CreateDirectory(cachePath);
                }
                return cachePath;
            }
        }

        public static string GetBuffersPath(string username)
        {
            var dbPath = GetBuffersBasePath();
            dbPath = Path.Combine(dbPath, IOSecurity.GetFilteredPath(username));
            return dbPath;
        }

        public static string GetBuffersBasePath()
        {
            var dbPath = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            );
            dbPath = Path.Combine(dbPath, "smuxi");
            dbPath = Path.Combine(dbPath, "buffers");
            return dbPath;
        }
    }
}
