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
using jabber;
using jabber.protocol;
using jabber.protocol.client;

namespace Smuxi.Engine
{
    /*
     * <iq from="chat.facebook.com" type="set" id="fbiq4B035BDF6E005" to="username@chat.facebook.com/resource" xmlns="jabber:client">
     *   <own-message xmlns="http://www.facebook.com/xmpp/messages" to="user_id@chat.facebook.com" self="false">
     *     <body>message goes here</body>
     *   </own-message>
     * </iq>
     */
    internal class OwnMessageIQ : TypedIQ<OwnMessageQuery>
    {
        public OwnMessageIQ(XmlDocument doc) : base(doc)
        {
        }
    }

    internal class OwnMessageQuery : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public OwnMessageQuery(XmlDocument doc)
                        : base("own-message",
                               "http://www.facebook.com/xmpp/messages",
                               doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public OwnMessageQuery(string prefix, XmlQualifiedName qname, XmlDocument doc) :
                          base(prefix, qname, doc)
        {
        }

        public JID To {
            get {
                return (JID) GetAttr("to");
            }
            set {
                SetAttr("to", value);
            }
        }

        public bool Self {
            get {
                var value = true;
                Boolean.TryParse(GetAttr("self"), out value);
                return value;
            }
            set {
                SetAttr("self", value.ToString());
            }
        }

        /// <summary>
        /// Message body
        /// </summary>
        public string Body {
            get {
                return GetElem("body");
            }
            set {
                SetElem("body", value);
            }
        }
    }
}
