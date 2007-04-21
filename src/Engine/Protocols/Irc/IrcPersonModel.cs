/*
 * $Id: IrcUser.cs 142 2007-01-02 22:19:08Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/IrcUser.cs $
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

namespace Meebey.Smuxi.Engine
{
    [Serializable]
    public class IrcPersonModel : PersonModel
    {
        private string _RealName;
        private string _Ident;
        private string _Host;
        
        public string NickName {
            get {
                return base.IdentityName;
            }
            set {
                base.IdentityName = value;
            }
        }
        
        public string RealName {
            get {
                return _RealName;
            }
            set {
                _RealName = value;
            }
        }
        
        public string Ident {
            get {
                return _Ident;
            }
            set {
                _Ident = value;
            }
        }
        
        public string Host {
            get {
                return _Host;
            }
            set {
                _Host = value;
            }
        }
        
        public IrcPersonModel(string nickName, string realName, string ident, string host,
                              string networkID, INetworkManager networkManager) :
                         base(nickName, nickName, networkID, NetworkProtocol.Irc, networkManager)
        {
            _RealName = realName;
            _Ident = ident;
            _Host = host;
        }
    }
}
