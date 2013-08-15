// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Oliver Schneider <smuxi@oli-obk.de>
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
using agsXMPP.protocol.client;
using System.Collections.Generic;
using agsXMPP;
using Smuxi.Common;
using agsXMPP.protocol.iq.roster;
using agsXMPP.protocol.iq.disco;

namespace Smuxi.Engine
{
    internal class XmppResourceModel
    {
        public Presence Presence { get; set; }
        public DiscoInfo Disco { get; set; }
        public string Name { get; set; }
    }
    
    internal class XmppPersonModel : PersonModel
    {
        public bool Temporary { get; set; }
        public Jid Jid { get; set; }
        public Dictionary<string, XmppResourceModel> Resources { get; private set; }
        public Dictionary<Jid, XmppResourceModel> MucResources { get; private set; }
        public SubscriptionType Subscription { get; set; }
        public AskType Ask { get; set; }
        public XmppPersonModel(Jid jid, string nick, XmppProtocolManager protocolManager)
            :base(jid, nick, protocolManager.NetworkID, protocolManager.Protocol, protocolManager)
        {
            Trace.Call(jid, nick, protocolManager);
            Jid = jid.Bare;
            Resources = new Dictionary<string, XmppResourceModel>();
            MucResources = new Dictionary<Jid, XmppResourceModel>();
            Ask = AskType.NONE;
            Subscription = SubscriptionType.none;
            Temporary = true;
            if (!String.IsNullOrEmpty(jid.Resource)) {
                GetOrCreateResource(jid);
            }
        }
        
        public XmppResourceModel GetOrCreateResource(Jid jid, out bool isNew)
        {
            XmppResourceModel ret;
            string res = jid.Resource ?? "";
            if (Resources.TryGetValue(res, out ret)) {
                isNew = false;
                return ret;
            }
            ret = new XmppResourceModel();
            ret.Name = res;
            Resources.Add(res, ret);
            isNew = true;
            return ret;
        }
        
        public XmppResourceModel GetOrCreateResource(Jid jid)
        {
            XmppResourceModel ret;
            string res = jid.Resource ?? "";
            if (Resources.TryGetValue(res, out ret)) {
                return ret;
            }
            ret = new XmppResourceModel();
            ret.Name = res;
            Resources.Add(res, ret);
            return ret;
        }

        public XmppResourceModel GetOrCreateMucResource(Jid jid)
        {
            Trace.Call(jid);
            XmppResourceModel ret;
            if (MucResources.TryGetValue(jid, out ret)) {
                return ret;
            }
            ret = new XmppResourceModel();
            ret.Name = jid;
            MucResources.Add(jid, ret);
            return ret;
        }
        
        public List<XmppResourceModel> GetResourcesWithHighestPriority()
        {
            List<XmppResourceModel> ret = new List<XmppResourceModel>();
            int prio = -99999;
            foreach (var res in Resources) {
                if (res.Value.Presence.Priority > prio) {
                    ret.Clear();
                    ret.Add(res.Value);
                    prio = res.Value.Presence.Priority;
                } else if (res.Value.Presence.Priority == prio) {
                    ret.Add(res.Value);
                }
            }
            return ret;
        }
        
        public void RemoveResource(Jid jid)
        {
            Resources.Remove(jid.Resource ?? "");
        }
        
        public PersonModel ToPersonModel()
        {
            return new PersonModel(
                base.ID,
                base.IdentityName,
                base.NetworkID,
                base.NetworkID,
                base.ProtocolManager
                );
        }
    }
}

