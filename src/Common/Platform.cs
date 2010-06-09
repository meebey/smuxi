// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
                    Process.Start(pinfo);
                } catch (Exception) {
                    // fall back to runtime detector
                    return Environment.OSVersion.Platform.ToString();
                }

                // GNU/Linux
                // GNU/kFreeBSD
                var info = new ProcessStartInfo("uname", "-o");
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                var process = Process.Start(info);
                process.WaitForExit();
                if (process.ExitCode == 0) {
                    return process.StandardOutput.ReadLine();
                }

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
                    return process.StandardOutput.ReadLine();
                }

                return "Unknown";
            }
        }
        
        public static string Architecture {
            get {
                // uname present?
                try {
                    var pinfo = new ProcessStartInfo("uname");
                    pinfo.UseShellExecute = false;
                    Process.Start(pinfo);
                } catch (Exception) {
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
    }
}
