/*
 * $Id: User.cs 142 2007-01-02 22:19:08Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/User.cs $
 * $Rev: 142 $
 * $Author: meebey $
 * $Date: 2007-01-02 23:19:08 +0100 (Tue, 02 Jan 2007) $
 *
 * smuxi - Smart MUltipleXed Irc
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

using System;

namespace Smuxi.Engine
{
    [Serializable]
    public class ContactModel
    {
        private string          _ID;
        private string          _IdentityName;
        private string          _NetworkID;
        private NetworkProtocol _NetworkProtocol;
        
        public string ID {
            get {
                return _ID;
            }
        }
        
        public string IdentityName {
            get {
                return _IdentityName;
            }
            set {
                _IdentityName = value;
            }
        }
        
        public string NetworkID {
            get {
                return _NetworkID;
            }
        }
        
        public NetworkProtocol NetworkProtocol {
            get {
                return _NetworkProtocol;
            }
        }
        
        public ContactModel(string id, string identityName,
                            string networkID, NetworkProtocol networkProtocol)
        {
            _ID = id;
            _IdentityName = identityName;
            _NetworkID = networkID;
            _NetworkProtocol = networkProtocol;
        }
    }
}
