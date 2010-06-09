// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Clement Bourgeois <moonpyk@gmail.com>
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
using System.Collections.Generic;

using Smuxi.Common;

namespace Smuxi.Engine
{
    public class UserListController
    {
        private static readonly string f_LibraryTextDomain = "smuxi-engine";
        private Config f_Config;
        private string f_Prefix;

        public UserListController(Config config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            f_Config = config;
            f_Prefix = "Engine/Users";
        }

        public void AddUser(string username, string password)
        {
            if (username == null) {
                throw new ArgumentNullException("username");
            }
            if (password == null) {
                throw new ArgumentNullException("password");
            }

            CheckUsername(username);
            CheckPassword(password);
            CheckUserNotExists(username);

            List<string> userList = GetUsers();
            userList.Add(username);
            f_Config[f_Prefix + "/Users"] = userList.ToArray();
            f_Config[f_Prefix + "/" + username + "/Password"] = password;
        }

        public void ModifyUser(string username, string password)
        {
            if (username == null) {
                throw new ArgumentNullException("username");
            }
            if (password == null) {
                throw new ArgumentNullException("password");
            }

            CheckUsername(username);
            CheckPassword(password);
            CheckUserExists(username);

            f_Config[f_Prefix + "/" + username + "/Password"] = password;
        }

        public void DeleteUser(string username)
        {
            if (username == null) {
                throw new ArgumentNullException("username");
            }

            CheckUsername(username);
            CheckUserExists(username);

            List<string> userList = GetUsers();
            userList.Remove(username);
            f_Config[f_Prefix + "/Users"] = userList.ToArray();
            f_Config.Remove(f_Prefix + "/" + username + "/");
        }

        public bool UserExists(string username)
        {
            if (username == null) {
                throw new ArgumentNullException("username");
            }

            List<string> usersList = GetUsers();
            return usersList.Contains(username);
        }

        public List<string> GetUsers()
        {
            return new List<string>((string[]) f_Config[f_Prefix + "/Users"]);
        }

        protected void CheckUsername(string username)
        {
            if (String.IsNullOrEmpty(username) ||
                username.Trim().Length == 0) {
                throw new ApplicationException(
                     String.Format(_("Username must not be empty."), username)
                );
            }
        }

        protected void CheckPassword(string password)
        {
            if (String.IsNullOrEmpty(password) ||
                password.Trim().Length == 0) {
                throw new ApplicationException(
                     String.Format(_("Password must not be empty."), password)
                );
            }
        }

        protected void CheckUserExists(string username)
        {
            if (!UserExists(username)) {
                throw new ApplicationException(
                     String.Format(_("User \"{0}\" doesn't exist."), username)
                );
            }
        }

        protected void CheckUserNotExists(string username)
        {
            if (UserExists(username)) {
                throw new ApplicationException(
                    String.Format(_("User \"{0}\" already exists."), username)
                );
            }
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, f_LibraryTextDomain);
        }
    }
}
