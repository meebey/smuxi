/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007, 2010-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Reflection;
using System.Threading;
using SysDiag = System.Diagnostics;
using NDesk.Options;
using Smuxi.Common;
using Smuxi.Engine;
using Mono.Unix;
using Mono.Unix.Native;

namespace Smuxi.Frontend.Stfl
{ 
    public class MainClass
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif

        public static void Main(string[] args)
        {
#if LOG4NET
            // initialize log level
            log4net.Repository.ILoggerRepository repo = log4net.LogManager.GetRepository();
            repo.Threshold = log4net.Core.Level.Error;
#endif

            bool debug = false;
            bool listEngines = false;
            string engine = "local";

            InitLocale();

            OptionSet parser = new OptionSet();

            parser.Add(
                "d|debug",
                _("Enable debug output"),
                delegate (string value) {
                    debug = true;
                }
            );

            parser.Add(
                "e|engine=",
                _("Engine to connect to"),
                delegate (string value) {
                    engine = value;
                }
            );

            parser.Add(
                "l|list-engines",
                _("List available engines"),
                delegate (string value) {
                    listEngines = true;
                }
            );

            parser.Add(
                 "h|help",
                 _("Show this help"),
                 delegate(string value) {
                    Console.WriteLine(_("Usage: smuxi-frontend-stfl [options]"));
                    Console.WriteLine();
                    Console.WriteLine(_("Options:"));
                    parser.WriteOptionDescriptions(Console.Out);
                    Environment.Exit(0);
                 }
            );

            parser.Add(
                 "<>",
                delegate(string value) {
                    throw new OptionException(
                        String.Format(
                            _("Unknown option: '{0}'"),
                            value
                        ),
                        value
                    );
                }
            );

            try {
                parser.Parse(args);
#if LOG4NET
                if (debug) {
                    repo.Threshold = log4net.Core.Level.Debug;
                }
#endif
            } catch (OptionException ex) {
                Console.Error.WriteLine(_("Command line error: {0}"), ex.Message);
                Environment.Exit(1);
            }

            if (listEngines) {
                Console.WriteLine(_("Available Engines:"));
                var config = new FrontendConfig(Frontend.UIName);
                config.Load();
                foreach (var entry in  (string[]) config["Engines/Engines"]) {
                    Console.WriteLine("\t{0}", entry);
                }
                return;
            }

            if ((Environment.OSVersion.Platform == PlatformID.Unix) ||
                (Environment.OSVersion.Platform == PlatformID.MacOSX)) {
                // Register shutdown handlers
#if LOG4NET
                _Logger.Info("Registering signal handlers");
#endif
                UnixSignal[] shutdown_signals = {
                    new UnixSignal(Signum.SIGINT),
                    new UnixSignal(Signum.SIGTERM),
                };
                Thread signal_thread = new Thread(() => {
                    var index = UnixSignal.WaitAny(shutdown_signals);
#if LOG4NET
                    _Logger.Info("Caught signal " + shutdown_signals[index].Signum.ToString() + ", shutting down");
#endif
                    Frontend.Quit();
                });
                signal_thread.Start();
            }

            try {
                Frontend.Init(engine);
            } catch (Exception e) {
#if LOG4NET
                _Logger.Fatal(e);
#endif
                if (SysDiag.Debugger.IsAttached) {
                    throw;
                }
            }
        }

        private static void InitLocale()
        {
            string appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string localeDir = Path.Combine(appDir, "locale");
            if (!Directory.Exists(localeDir)) {
                localeDir = Path.Combine(Defines.InstallPrefix, "share");
                localeDir = Path.Combine(localeDir, "locale");
            }

            LibraryCatalog.Init("smuxi-frontend-stfl", localeDir);
#if LOG4NET
            _Logger.Debug("Using locale data from: " + localeDir);
#endif
        }

        static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
