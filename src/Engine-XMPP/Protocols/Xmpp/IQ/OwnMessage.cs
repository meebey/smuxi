// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2011 <meebey@meebey.net>
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
using System.Xml;


using agsXMPP;
using agsXMPP.protocol;
using agsXMPP.protocol.client;
using agsXMPP.Xml.Dom;

namespace Smuxi.Engine
{
    /*
     * <iq from="chat.facebook.com" type="set" id="fbiq4B035BDF6E005" to="username@chat.facebook.com/resource" xmlns="jabber:client">
     *   <own-message xmlns="http://www.facebook.com/xmpp/messages" to="user_id@chat.facebook.com" self="false">
     *     <body>message goes here</body>
     *   </own-message>
     * </iq>
     */


    internal class OwnMessageQuery : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public OwnMessageQuery()
        {
            base.Namespace = "http://www.facebook.com/xmpp/messages";
            base.TagName = "own-message";
        }

        public Jid To {
            get {
                return GetAttributeJid("to");
            }
            set {
                SetAttribute("to", value);
            }
        }

        public bool Self {
            get {
                var value = true;
                Boolean.TryParse(GetAttribute("self"), out value);
                return value;
            }
            set {
                SetAttribute("self", value.ToString());
            }
        }

        /// <summary>
        /// Message body
        /// </summary>
        public string Body {
            get {
                return GetTag("body");
            }
            set {
                SetTag("body", value);
            }
        }
    }
}
