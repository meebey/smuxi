/*
 * $Id: IrcChannelUser.cs 142 2007-01-02 22:19:08Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/IrcChannelUser.cs $
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
            set {
                _IsOp = value;
            }
        }

        public bool IsVoice {
            get {
                return _IsVoice;
            }
            set {
                _IsVoice = value;
            }
        }
        
        public IrcGroupPersonModel(string nickname, string realname, string ident, string host,
                                   string networkID, IProtocolManager networkManager) :
                              base(nickname, realname, ident, host,
                                   networkID, networkManager)
        {
        }
        
        public IrcGroupPersonModel(string nickname, string networkID, IProtocolManager networkManager) :
                              base(nickname, null, null, null, networkID, networkManager)
        {
        }
        
        protected IrcGroupPersonModel(SerializationInfo info, StreamingContext ctx) :
                                 base(info, ctx)
        {
        }
        
        protected override void GetObjectData(SerializationWriter sw) 
        {
            base.GetObjectData(sw);

            sw.Write(_IsOp);
            sw.Write(_IsVoice);
        }

        protected override void SetObjectData(SerializationReader sr)
        {
            base.SetObjectData(sr);
            
            _IsOp    = sr.ReadBoolean();
            _IsVoice = sr.ReadBoolean();
        }
    }
}
