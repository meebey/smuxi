// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2011 Mirco Bauer <meebey@meebey.net>
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

namespace Smuxi.Engine
{
    public class XmppServerModel : ServerModel
    {
        public string Resource { get; set; }
        public Dictionary<PresenceStatus, int> Priorities { get; private set; }

        public void InitDefaults()
        {
            Priorities = new Dictionary<PresenceStatus, int>();
            // choose somewhat reasonable defaults
            Priorities[PresenceStatus.Online] = 5;
            Priorities[PresenceStatus.Away] = 0;
            Protocol = "XMPP";
        }
        
        public virtual void Load(UserConfig config, string id)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            if (String.IsNullOrEmpty(id)) {
                throw new ArgumentNullException("id");
            }
            Load(config, Protocol, id);
        }
        
        public XmppServerModel()
        {
            InitDefaults();
        }

        public override void Load(UserConfig config, string protocol, string id)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            if (String.IsNullOrEmpty(protocol)) {
                throw new ArgumentNullException("protocol");
            }
            if (String.IsNullOrEmpty(id)) {
                throw new ArgumentNullException("id");
            }
            base.Load(config, protocol, id);
            
            var obj = config[ConfigKeyPrefix + "PriorityOnline"];
            if (obj != null) {
                Priorities[PresenceStatus.Online] = (int) obj;
            }
            obj = config[ConfigKeyPrefix + "PriorityAway"];
            if (obj != null) {
                Priorities[PresenceStatus.Away] = (int) obj;
            }
            obj = config[ConfigKeyPrefix + "Resource"];
            if (obj != null) {
                Resource = (string) obj;
            }
        }

        public override void Save(UserConfig config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            base.Save(config);
            config[ConfigKeyPrefix + "PriorityOnline"] = Priorities[PresenceStatus.Online];
            config[ConfigKeyPrefix + "PriorityAway"] = Priorities[PresenceStatus.Away];
            config[ConfigKeyPrefix + "Resource"] = Resource;
        }
    }
}
