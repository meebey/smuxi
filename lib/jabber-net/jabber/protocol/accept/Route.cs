/* --------------------------------------------------------------------------
 * Copyrights
 *
 * Portions created by or assigned to Cursive Systems, Inc. are
 * Copyright (c) 2002-2007 Cursive Systems, Inc.  All Rights Reserved.  Contact
 * information for Cursive Systems, Inc. is available at
 * http://www.cursive.net/.
 *
 * License
 *
 * Jabber-Net can be used under either JOSL or the GPL.
 * See LICENSE.txt for details.
 * --------------------------------------------------------------------------*/
using System;

using System.Xml;

using bedrock.util;

namespace jabber.protocol.accept
{
    /// <summary>
    /// The type field in a route tag.
    /// </summary>
    [SVN(@"$Id: Route.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum RouteType
    {
        /// <summary>
        /// None specified
        /// </summary>
        NONE = -1,
        /// <summary>
        /// type='error'
        /// </summary>
        error,
        /// <summary>
        /// type='auth'
        /// </summary>
        auth,
        /// <summary>
        /// type='session'
        /// </summary>
        session
    }

    /// <summary>
    /// The route packet.
    /// </summary>
    [SVN(@"$Id: Route.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Route : jabber.protocol.Packet
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Route(XmlDocument doc) : base("route", doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Route(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The element inside the route tag.
        /// </summary>
        public XmlElement Contents
        {
            get { return (XmlElement) this.FirstChild; }
            set
            {
                this.InnerXml = "";
                AddChild(value);
            }
        }

        /// <summary>
        /// The type attribute
        /// </summary>
        public RouteType Type
        {
            get { return (RouteType) GetEnumAttr("type", typeof(RouteType)); }
            set
            {
                RouteType cur = this.Type;
                if (cur == value)
                    return;
                if (value == RouteType.NONE)
                {
                    RemoveAttribute("type");
                }
                else
                {
                    SetAttribute("type", value.ToString());
                }
            }
        }
    }
}
