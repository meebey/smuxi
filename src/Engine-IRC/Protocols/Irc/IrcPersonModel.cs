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
using System.Runtime.Serialization;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    public class IrcPersonModel : PersonModel
    {
        private string _RealName;
        private string _Ident;
        private string _Host;
        
        public string NickName {
            get {
                return IdentityName;
            }
            internal set {
                IdentityName = value;
            }
        }
        
        public string RealName {
            get {
                return _RealName;
            }
            internal set {
                _RealName = value;
            }
        }
        
        public string Ident {
            get {
                return _Ident;
            }
            internal set {
                _Ident = value;
            }
        }
        
        public string Host {
            get {
                return _Host;
            }
            internal set {
                _Host = value;
            }
        }
        
        internal IrcPersonModel(string nickName, string realName, string ident,
                                string host, string networkID,
                                IProtocolManager networkManager) :
                           base(nickName, nickName, networkID, "IRC", networkManager)
        {
            if (realName == null) {
                throw new ArgumentNullException("realName");
            }
            if (ident == null) {
                throw new ArgumentNullException("ident");
            }
            if (host == null) {
                throw new ArgumentNullException("host");
            }

            _RealName = realName;
            _Ident = ident;
            _Host = host;
        }

        internal protected IrcPersonModel(string nickName, string networkID, 
                                          IProtocolManager networkManager) :
                                     base(nickName, nickName, networkID, "IRC",
                                          networkManager)
        {
        }
        
        internal protected IrcPersonModel(SerializationInfo info,
                                          StreamingContext ctx) :
                                     base(info, ctx)
        {
        }
        
        protected override void GetObjectData(SerializationWriter sw) 
        {
            if (sw == null) {
                throw new ArgumentNullException("sw");
            }
            
            base.GetObjectData(sw);
            
            sw.Write(_RealName);
            sw.Write(_Ident);
            sw.Write(_Host);
        }
        
        protected override void SetObjectData(SerializationReader sr)
        {
            if (sr == null) {
                throw new ArgumentNullException("sr");
            }
            
            base.SetObjectData(sr);
            
            _RealName = sr.ReadString();
            _Ident    = sr.ReadString();
            _Host     = sr.ReadString();
        }
    }
}
