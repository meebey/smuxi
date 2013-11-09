// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
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
using System.Linq;
using System.Text;
using System.Collections.Generic;
using SysDiag = System.Diagnostics;

namespace Smuxi.Engine
{
    public class HookRunner
    {
#if LOG4NET
        static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        public Dictionary<string, string> EnvironmentVariables { get; private set; }
        public List<HookEnvironment> Environments { get; private set; }
        public List<HookCommand> Commands { get; set; }
        public List<string> Arguments { get; set; }
        List<string> Hooks { get; set; }
        string[] PathElements { get; set; }
        string StateBasePath { get; set; }

        public bool HasHooks {
            get {
                return Hooks.Count > 0;
            }
        }

        public HookRunner(params string[] path)
        {
            if (path == null) {
                throw new ArgumentNullException("path");
            }

            PathElements = path;
            EnvironmentVariables = new Dictionary<string, string>();
            Hooks = new List<string>();
            Environments = new List<HookEnvironment>();
            Commands = new List<HookCommand>();
        }

        public void Init()
        {
            var appData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            );
            StateBasePath = Path.Combine(appData, "smuxi");
            StateBasePath = Path.Combine(StateBasePath, "hook-state");
            foreach (var path in PathElements) {
                StateBasePath = Path.Combine(StateBasePath, path);
            }

            var hookPath = Path.Combine(appData, "smuxi");
            hookPath = Path.Combine(hookPath, "hooks");
            foreach (var path in PathElements) {
                hookPath = Path.Combine(hookPath, path);
            }
            if (!Directory.Exists(hookPath)) {
                return;
            }
            foreach (var file in Directory.GetFiles(hookPath).OrderBy(x => x)) {
                try {
                    File.OpenRead(file).Close();
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("Init(): error opening " + file, ex);
#endif
                    continue;
                }
                Hooks.Add(file);
            }


            if (!HasHooks) {
                return;
            }

            var env = EnvironmentVariables;
            if (Engine.Version != null) {
                env.Add("ENGINE_VERSION", Engine.Version.ToString());
            }

            foreach (var environment in Environments) {
                foreach (var entry in environment) {
                    env.Add(entry.Key, entry.Value);
                }
            }
        }

        public void Run()
        {
            if (!HasHooks) {
                return;
            }

            foreach (var hook in Hooks) {
                RunHook(hook);
            }
        }

        void RunHook(string hookPath)
        {
            var hookFilename = Path.GetFileName(hookPath);
            var statePath = Path.Combine(StateBasePath, hookFilename);
            if (!Directory.Exists(statePath)) {
                Directory.CreateDirectory(statePath);
            }

            string hookArgs = null;
            if (Arguments != null && Arguments.Count > 0) {
                var args = new StringBuilder(256);
                foreach (var arg in Arguments) {
                    // quote because of potential spaces and retarded Process API
                    args.AppendFormat(@"""{0}"" ", arg);
                }
                // remove trailing space
                args.Length--;
                hookArgs = args.ToString();
            }
            var startInfo = new SysDiag.ProcessStartInfo() {
                FileName = hookPath,
                Arguments = hookArgs,
                WorkingDirectory = statePath,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            // HACK: retarded API doesn't allow us to set the dictionary,
            // thus we have to copy all key/values into it
            var startEnv = startInfo.EnvironmentVariables;
            foreach (var entry in EnvironmentVariables) {
                startEnv.Add(String.Concat("SMUXI_", entry.Key), entry.Value);
            }
#if LOG4NET
            Logger.Debug("Run(): executing " + hookPath);
#endif
            var process = SysDiag.Process.Start(startInfo);
            while (!process.HasExited) {
                var line = process.StandardOutput.ReadLine();
                if (String.IsNullOrEmpty(line)) {
                    continue;
                }
                try {
                    // find matching hook command
                    foreach (var cmd in Commands) {
                        if (!line.StartsWith(cmd.CommandName + " ")) {
                            continue;
                        }
                        var cmdLine = line.Substring(cmd.CommandName.Length + 1);
                        cmd.Run(cmdLine);
                        break;
                    }
                } catch (Exception ex) {
#if LOG4NET
                    Logger.Error("Run(): error processing " + line, ex);
#endif
                }
            }
            process.WaitForExit();
        }
    }
}
