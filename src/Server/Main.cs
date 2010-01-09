/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006, 2008, 2010 Mirco Bauer <meebey@meebey.net>
 * Copyright (c) 2010 Clement Bourgeois <moonpyk@gmail.com>
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
using NDesk.Options;
using Smuxi.Common;
using Smuxi.Engine;

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

            InitLocale();

            OptionSet parser = new OptionSet();

            parser.Add(
                "add-user",
                _("Add user to Server"),
                delegate(string val) {
                    addUser = true;
                    CheckExclusiveParameters(addUser, modUser, delUser);
                }
            );

            parser.Add(
                "modify-user",
                _("Modify existing user of Server"),
                delegate(string val) {
                    modUser = true;
                    CheckExclusiveParameters(addUser, modUser, delUser);
                }
            );

            parser.Add(
                "delete-user",
                _("Delete user from Server"),
                delegate(string val) {
                    delUser = true;
                    CheckExclusiveParameters(addUser, modUser, delUser);
                }
            );

            parser.Add(
                "username=",
                _("User to create, modify or delete"),
                delegate(string val) {
                    CheckUsernameParameter(val);
                    username = val;
                }
            );

            parser.Add(
                "password=",
                _("Password of the user when creating or modifying a user"),
                delegate(string val) {
                    CheckPasswordParameter(val);
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
                if (addUser || modUser) {
                    CheckUsernameParameter(username);
                    CheckPasswordParameter(password);
                }
                if (delUser) {
                    CheckUsernameParameter(username);
                }
                ManageUser(addUser, delUser, modUser, username, password);
            } catch (OptionException ex) {
                Console.Error.WriteLine(_("Command line error: {0}"), ex.Message);
                Environment.Exit(1);
            }

#if LOG4NET
            // initialize log level
            log4net.Repository.ILoggerRepository repo = log4net.LogManager.GetRepository();
            if (debug) {
                repo.Threshold = log4net.Core.Level.Debug;
            } else {
                repo.Threshold = log4net.Core.Level.Info;
            }
#endif

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

        private static void CheckExclusiveParameters(bool addUser, bool modUser, bool delUser)
        {
            int enabled = 0;
            if (addUser) {
                enabled++;
            }
            if (modUser) {
                enabled++;
            }
            if (delUser) {
                enabled++;
            }

            if (enabled <= 1 ) {
                return;
            }

            throw new OptionException(
                _("At most one of --add-user, --modify-user, and --delete-user " +
                  "may be used at a time."),
                String.Empty
            );
        }

        private static void CheckUsernameParameter(string username)
        {
            if (username == null) {
                throw new OptionException(
                    _("You must specify a username with the --username option."),
                    String.Empty
                );
            }
            if (username.Trim().Length == 0) {
                throw new OptionException(
                    _("Username must not be empty."),
                    String.Empty
                );
            }
        }

        private static void CheckPasswordParameter(string password)
        {
            if (password == null) {
                throw new OptionException(
                    _("You must specify a password with the --password option."),
                    String.Empty
                );
            }
            if (password.Trim().Length == 0) {
                throw new OptionException(
                    _("Password must not be empty."),
                    String.Empty
                );
            }
        }

        private static void ManageUser(bool addUser, bool delUser, bool modUser, string username, string password)
        {
            Config config = new Config();
            UserListController controller = new UserListController(config);
            if (addUser) {
                config.Load();
                controller.AddUser(username, password);
                config.Save();
                Console.WriteLine(
                     _("User \"{0}\" successfully added to server."),
                     username
                );
                Environment.Exit(0);
            } else if (modUser) {
                config.Load();
                controller.ModifyUser(username, password);
                config.Save();
                Console.WriteLine(
                     _("User \"{0}\" successfully modified."),
                     username
                );
                Environment.Exit(0);
            } else if (delUser) {
                config.Load();
                controller.DeleteUser(username);
                config.Save();
                Console.WriteLine(
                    _("User \"{0}\" successfully deleted from server."),
                    username
                );
                Environment.Exit(0);
            }
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
