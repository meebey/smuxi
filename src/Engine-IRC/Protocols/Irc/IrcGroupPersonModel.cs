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
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private bool _IsOp;
        private bool _IsVoice;
        
        public bool IsOp {
            get {
                return _IsOp;
            }
            internal set {
                _IsOp = value;
            }
        }

        public bool IsVoice {
            get {
                return _IsVoice;
            }
            internal set {
                _IsVoice = value;
            }
        }
        
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

            sw.Write(_IsOp);
            sw.Write(_IsVoice);
        }

        protected override void SetObjectData(SerializationReader sr)
        {
            if (sr == null) {
                throw new ArgumentNullException("sr");
            }
            
            base.SetObjectData(sr);
            
            _IsOp    = sr.ReadBoolean();
            _IsVoice = sr.ReadBoolean();
        }

        public override int CompareTo(ContactModel contact)
        {
            var ircContact = contact as IrcGroupPersonModel;
            if (ircContact == null) {
                return 1;
            }

            int status1 = 0;
            if (IsOp) {
                status1 += 2;
            } else if (IsVoice) {
                status1 += 1;
            }

            int status2 = 0;
            if (ircContact.IsOp) {
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
