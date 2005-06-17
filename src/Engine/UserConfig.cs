/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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

namespace Meebey.Smuxi.Engine
{
    public class UserConfig : PermanentRemoteObject
    {
        private Config _Config;
        private string _Username;
        private string _UserPrefix;
        private string _DefaultPrefix = "Engine/Users/DEFAULT/";
        
        public new object this[string key]
        {
            get {
                object obj;
                obj = _Config[_UserPrefix+key];
                if (obj != null) {
                    return obj;
                }
                return _Config[_DefaultPrefix+key];
            }
            set {
                _Config[_UserPrefix+key] = value;
            }
        }
        
        public UserConfig(Config config, string username)
        {
            _Config = config;
            _Username = username;
            _UserPrefix = "Engine/Users/"+username+"/";
        }
    }
}
