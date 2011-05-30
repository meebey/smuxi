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
            System.Threading.Thread.CurrentThread.Name = "Main";
#if LOG4NET
            // initialize log level
            log4net.Repository.ILoggerRepository repo = log4net.LogManager.GetRepository();
            repo.Threshold = log4net.Core.Level.Error;
#endif

            bool addUser    = false;
            bool delUser    = false;
            bool modUser    = false;
            bool listUsers  = false;
            bool debug      = false;
            bool optBuffers = false;

            string username = null;
            string password = null;

            InitLocale();

            OptionSet parser = new OptionSet();

            parser.Add(
                "add-user",
                _("Add user to Server"),
                delegate(string val) {
                    addUser = true;
                    CheckExclusiveParameters(addUser, modUser, delUser, listUsers);
                }
            );

            parser.Add(
                "modify-user",
                _("Modify existing user of Server"),
                delegate(string val) {
                    modUser = true;
                    CheckExclusiveParameters(addUser, modUser, delUser, listUsers);
                }
            );

            parser.Add(
                "delete-user",
                _("Delete user from Server"),
                delegate(string val) {
                    delUser = true;
                    CheckExclusiveParameters(addUser, modUser, delUser, listUsers);
                }
            );

            parser.Add(
                "list-users",
                _("List all existing users of Server"),
                delegate(string val) {
                    listUsers = true;
                    CheckExclusiveParameters(addUser, modUser, delUser, listUsers);
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
                "optimize-message-buffers",
                _("Optimize message buffers and exit"),
                delegate (string val) {
                    optBuffers = true;
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

            parser.Add(
                 "<>",
                delegate(string val) {
                    throw new OptionException(
                        String.Format(
                            _("Unknown option: '{0}'"),
                            val
                        ),
                        val
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
                if (optBuffers) {
                    OptimizeMessageBuffers();
                }
                if (addUser || modUser) {
                    CheckUsernameParameter(username);
                    CheckPasswordParameter(password);
                }
                if (delUser) {
                    CheckUsernameParameter(username);
                }
                ManageUser(addUser, delUser, modUser, listUsers, username, password);
            } catch (OptionException ex) {
                Console.Error.WriteLine(_("Command line error: {0}"), ex.Message);
                Environment.Exit(1);
            }

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

        private static void CheckExclusiveParameters(params bool[] parameters)
        {
            int enabled = 0;
            foreach (bool parameter in parameters) {
                if (parameter) {
                    enabled++;
                }
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

        private static void ManageUser(bool addUser, bool delUser, bool modUser,
                                       bool listUsers,
                                       string username, string password)
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
            } else if (listUsers) {
                config.Load();
                var users = controller.GetUsers();
                Console.WriteLine(_("Users:"));
                foreach (var user in users) {
                    if (user == "local") {
                        // is not a real user and could cause confusion
                        continue;
                    }
                    Console.WriteLine("\t{0}", user);
                }
                Environment.Exit(0);
            }
        }

        private static void OptimizeMessageBuffers()
        {
            var logRepo = log4net.LogManager.GetRepository();
            var origThreshold = logRepo.Threshold;
            // don't spew errors of Db4oMessageBuffer
            if (origThreshold == log4net.Core.Level.Error) {
                logRepo.Threshold = log4net.Core.Level.Fatal;
            }
            try {
                var bufferCount = Db4oMessageBuffer.OptimizeAllBuffers();
                Console.WriteLine(
                    String.Format(
                        _("Successfully optimized {0} message buffers."),
                        bufferCount
                    )
                );
                Environment.Exit(0);
            } catch (Exception ex) {
                string error = ex.Message;
                if (ex.InnerException != null) {
                    // inner-exceptio is more useful for some reason...
                    error = ex.InnerException.Message;
                }
                Console.WriteLine(
                    String.Format(
                        _("Failed to optimize message buffers: {0}"), error
                    )
                );
                Environment.Exit(1);
            }
            logRepo.Threshold = origThreshold;
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
