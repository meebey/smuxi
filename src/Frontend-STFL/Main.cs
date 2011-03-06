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
using Mono.Unix;
using NDesk.Options;
using Smuxi.Common;

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
            string engine = "local";

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

            try {
                Frontend.Init(engine);
            } catch (Exception e) {
#if LOG4NET
                _Logger.Fatal(e);
#endif
                throw;
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

            LibraryCatalog.Init("smuxi-server", localeDir);
#if LOG4NET
            _Logger.Debug("Using locale data from: " + localeDir);
#endif
        }

        private static string _(string msg)
        {
            return Catalog.GetString(msg);
        }
    }
}
