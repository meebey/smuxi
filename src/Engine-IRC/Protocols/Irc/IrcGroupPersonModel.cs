/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2007, 2011 Mirco Bauer <meebey@meebey.net>
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
    public class IrcGroupPersonModel : IrcPersonModel
    {
        public bool IsOwner { get; internal set; }
        public bool IsChannelAdmin { get; internal set; }
        public bool IsOp { get; internal set; }
        public bool IsHalfop { get; internal set; }
        public bool IsVoice { get; internal set; }

        internal IrcGroupPersonModel(string nickname, string realname,
                                     string ident, string host, string networkID,
                                     IProtocolManager networkManager) :
                                base(nickname, realname, ident, host,
                                     networkID, networkManager)
        {
        }
        
        internal IrcGroupPersonModel(string nickname, string networkID,
                                     IProtocolManager networkManager) :
                                base(nickname, networkID, networkManager)
        {
        }
        
        internal protected IrcGroupPersonModel(SerializationInfo info,
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

            sw.Write(IsOp);
            sw.Write(IsVoice);
            sw.Write(IsOwner);
            sw.Write(IsChannelAdmin);
            sw.Write(IsHalfop);
        }

        protected override void SetObjectData(SerializationReader sr)
        {
            if (sr == null) {
                throw new ArgumentNullException("sr");
            }
            
            base.SetObjectData(sr);
            
            IsOp = sr.ReadBoolean();
            IsVoice = sr.ReadBoolean();

            // backward compatibility
            if (sr.PeekChar() != -1) {
                IsOwner = sr.ReadBoolean();
                IsChannelAdmin = sr.ReadBoolean();
                IsHalfop = sr.ReadBoolean();
            }
        }

        public override int CompareTo(ContactModel contact)
        {
            var ircContact = contact as IrcGroupPersonModel;
            if (ircContact == null) {
                return 1;
            }

            int status1 = 0;
            if (IsOwner) {
                status1 += 5;
            } else if (IsChannelAdmin) {
                status1 += 4;
            } else if (IsOp) {
                status1 += 3;
            } else if (IsHalfop) {
                status1 += 2;
            } else if (IsVoice) {
                status1 += 1;
            }

            int status2 = 0;
            if (ircContact.IsOwner) {
                status2 += 5;
            } else if (ircContact.IsChannelAdmin) {
                status2 += 4;
            } else if (ircContact.IsOp) {
                status2 += 3;
            } else if (ircContact.IsHalfop) {
                status2 += 2;
            } else if (ircContact.IsVoice) {
                status2 += 1;
            }

            int res = status2.CompareTo(status1);
            if (res != 0 ) {
                return res;
            }

            // the status is equal, so the name decides
            return base.CompareTo(contact);
        }
    }
}
