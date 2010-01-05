/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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

using NDesk.Options;

using Smuxi.Common;
using Smuxi.Engine;

using System;
using System.IO;
using System.Reflection;

namespace Smuxi.Server
{
    public class MainClass
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly string _LibraryTextDomain = "smuxi-server";

        public static void Main(string[] args)
        {
            bool addUser    = false;
            bool delUser    = false;
            bool modUser    = false;
            bool debug      = false;

            string username = null;
            string password = null;

#if LOG4NET
            // initialize log level
            log4net.Repository.ILoggerRepository repo = log4net.LogManager.GetRepository();
            if (debug) {
                repo.Threshold = log4net.Core.Level.Debug;
            } else {
                repo.Threshold = log4net.Core.Level.Info;
            }
#endif

            InitLocale();

            OptionSet parser = new OptionSet();

            parser.Add(
                "a|add-user",
                _("Add user to Smuxi"),
                delegate(string val) {
                    addUser = true;
                }
            );

            parser.Add(
                "m|modify-user",
                _("Modify existing user of Smuxi"),
                delegate(string val) {
                    modUser = true;
                }
            );

            parser.Add(
                "D|delete-user",
                _("Delete user of Smuxi"),
                delegate(string val) {
                    delUser = true;
                }
            );

            parser.Add(
                "u|username=",
                _("User to create/modifiy/delete"),
                delegate(string val) {
                    username = val;
                }
            );

            parser.Add(
                "p|password=",
                _("Password to create/modify for user"),
                delegate(string val) {
                    password = val;
                }
            );

            parser.Add(
                "d|debug",
                _("Enable debug output"),
                delegate (string v) {
                    debug = true;
                }
            );

            parser.Add(
                 "h|help",
                 _("Show this help"),
                 delegate(string val) {
                    Console.WriteLine(_("Usage: smuxi-server [options]"));
                    Console.WriteLine();
                    Console.WriteLine(_("Options:"));
                    parser.WriteOptionDescriptions(Console.Out);
                    Environment.Exit(0);
                 }
            );

            try {
                parser.Parse(args);

            } catch (OptionException) {
                // Exception details will be shown and explained later to a more
                // understandable form than NDesk.Options gives
            }

            ManageUsers (addUser, delUser, modUser, username, password);

            try {
                Server.Init(args);
            } catch (Exception e) {
#if LOG4NET
                _Logger.Fatal(e);
#endif
                // rethrow the exception for console output
                throw;
            }
        }

        private static void InitLocale ()
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
        
        private static void ManageUsers(bool addUser, bool delUser, bool modUser, string username, string password)
        {
            if ((addUser || modUser) && delUser) {
                Console.Error.WriteLine(_("Error: -a|--add-user or -m|--modify-user and -d|-delete-user options can't be used together"));
                Environment.Exit(1);
            }

            Config config = new Config();
            config.Load();

            UserListController controller =
                new UserListController(config);

            if (addUser || modUser) {
                if (modUser && addUser) {
                   Console.Error.WriteLine(_("Error: -a|--add-user and -m|--modify-user options can't be used together"));
                   Environment.Exit(1);
                }

                if (username == null || password == null) {
                   Console.Error.WriteLine(_("Error: -a|--add-user and -m|--modify-user options require -u|--username=VALUE and -p|--password=VALUE options to be set"));
                   Environment.Exit(1);
                }

                if (addUser) {
                    try {
                        controller.AddUser(username, password);
                        config.Save();
                        Console.WriteLine(
                             _("User: \"{0}\" successfully added to configuration with password: \"{1}\""),
                             username,
                             password
                        );
                        Environment.Exit(0);

                    } catch(ArgumentException ex) {
                        Console.Error.WriteLine("Error: " + ex.Message);
                        Environment.Exit(1);
                    }

                } else {
                    try {
                        controller.ModifyUser(username, password);
                        config.Save();
                        Console.WriteLine(
                             _("User: \"{0}\" password successfully changed to: \"{1}\""),
                             username,
                             password
                        );
                        Environment.Exit(0);

                    } catch(ArgumentException ex) {
                        Console.Error.WriteLine("Error: " + ex.Message);
                        Environment.Exit(1);
                    }
                }

            } else if(delUser) {
                if(username == null) {
                    Console.Error.WriteLine(_("Error: -D|--delete-user option require -u|--username=VALUE option"));
                    Environment.Exit(1);
                }

                try {
                    controller.DelUser(username);
                    config.Save();
                    Console.WriteLine(_("User: \"{0}\" successfully removed from configuration"), username);
                    Environment.Exit(0);

                } catch(ArgumentException ex) {
                    Console.Error.WriteLine("Error: " + ex.Message);
                    Environment.Exit(1);
                }
            }
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
