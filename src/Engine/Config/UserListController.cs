// $Id$
// 
// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Clement Bourgeois <moonpyk@gmail.com>
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
        private Config _Config;
        private string _Prefix;
        private static readonly string  _LibraryTextDomain = "smuxi-engine";

        public UserListController(Config config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            _Config = config;
            _Prefix = "Engine/Users";
        }

        public void AddUser(string username, string password)
        {
            if (username == null) {
                throw new ArgumentNullException("username");

            } else if (password == null) {
                throw new ArgumentNullException("password");
            }

            if (UserExists(username)) {
                throw new ArgumentException(
                     String.Format(
                         _("User named: \"{0}\" already exists !"),
                         username
                     )
                );
            }

            List<string> userList = UserList();

            userList.Add(username);

            _Config[_Prefix + "/Users"] = userList.ToArray();
            _Config[_Prefix + "/" + username + "/Password"] = password;
        }

        public void ModifyUser(string username, string password)
        {
            if (username == null) {
                throw new ArgumentNullException("username");

            } else if (password == null) {
                throw new ArgumentNullException("password");
            }

            if (!UserExists(username)) {
                throw new ArgumentException(
                     String.Format(
                        _("User named: \"{0}\" doesn't exists !"),
                        username
                     )
                );
            }

            _Config[_Prefix + "/" + username + "/Password"] = password;
        }

        public void DelUser(string username)
        {
            if (username == null) {
                throw new ArgumentNullException("username");
            }

            if (!UserExists(username)) {
                throw new ArgumentException(
                     String.Format(
                        _("User named: \"{0}\" doesn't exists !"),
                        username
                     )
                );
            }

            List<string> userList = UserList();

            userList.Remove(username);

            _Config[_Prefix + "/Users"] = userList.ToArray();
            _Config.Remove(_Prefix + "/" + username + "/");
        }

        public bool UserExists(string username)
        {
            if (username == null) {
                throw new ArgumentNullException("username");
            }

            List<string> usersList = UserList();

            return usersList.Contains(username);
        }

        protected List<string> UserList()
        {
            return new List<string>((string[])_Config[_Prefix + "/Users"]);
        }

        private static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, _LibraryTextDomain);
        }
    }
}
