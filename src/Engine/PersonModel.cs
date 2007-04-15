/*
 * $Id: NetworkType.cs 141 2006-12-31 02:09:01Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/NetworkType.cs $
 * $Rev: 141 $
 * $Author: meebey $
 * $Date: 2006-12-31 03:09:01 +0100 (Sun, 31 Dec 2006) $
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

namespace Meebey.Smuxi.Engine
{
    public class PersonModel : ContactModel
    {
        private INetworkManager _NetworkManager;
        
        public INetworkManager NetworkManager {
            get {
                return _NetworkManager;
            }
        }
        
        public PersonModel(string id, string displayName,
                           string networkID, NetworkProtocol networkProtocol, INetworkManager networkManager) :
                      base(id, displayName, networkID, networkProtocol)
        {
            _NetworkManager = networkManager;
        }
    }
}
