/**
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
    public class IrcUser : User
    {
        private string _Realname;
        private string _Ident;
        private string _Host;
        
        public string Realname
        {
            get {
                return _Realname;
            }
        }
        
        public string Ident
        {
            get {
                return _Ident;
            }
        }
        
        public string Host
        {
            get {
                return _Host;
            }
        }
        
        public IrcUser(string nickname, string realname, string ident, string host) : base(nickname, NetworkType.Irc)
        {
            _Realname = realname;
            _Ident = ident;
            _Host = host;
        }
    }
}
