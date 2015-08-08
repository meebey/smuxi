// This file is part of Smuxi and is licensed under the terms of MIT/X11
// 
// Copyright (c) 2010-2012 Mirco Bauer <meebey@meebey.net>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
                if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                    return Environment.OSVersion.Platform.ToString();
                }

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
                // Cygwin
                var info = new ProcessStartInfo("uname", "-o");
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                var process = Process.Start(info);
                process.WaitForExit();
                if (process.ExitCode == 0) {
                    os = process.StandardOutput.ReadLine();
                    // HACK: if Cygwin was installed on Windows and is in PATH
                    // we should not trust uname and ask the runtime instead
                    if (os == "Cygwin") {
                        return Environment.OSVersion.Platform.ToString();
                    }
                }

                if (String.IsNullOrEmpty(os)) {
                    // not all operating systems support -o so lets fallback to -s
                    // Linux
                    // FreeBSD
                    // Darwin
                    info = new ProcessStartInfo("uname", "-s");
                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;
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

                // uname present?
                try {
                    var pinfo = new ProcessStartInfo("uname");
                    pinfo.UseShellExecute = false;
                    pinfo.RedirectStandardOutput = true;
                    Process.Start(pinfo).WaitForExit();
                } catch (Exception) {
                    // no uname, fall back to pointer size
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
