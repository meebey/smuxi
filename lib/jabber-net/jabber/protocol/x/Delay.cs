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

namespace jabber.protocol.x
{
    /// <summary>
    /// A delay x element.
    /// </summary>
    [SVN(@"$Id: Delay.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Delay : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Delay(XmlDocument doc) : base("x", URI.XDELAY, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Delay(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// From whom?
        /// </summary>
        public string From
        {
            get { return GetAttribute("from"); }
            set { SetAttribute("from", value); }
        }

        /// <summary>
        /// Date/time stamp.
        /// </summary>
        public DateTime Stamp
        {
            get { return JabberDate(GetAttribute("stamp")); }
            set { SetAttribute("stamp", JabberDate(value)); }
        }

        /// <summary>
        /// Description
        /// </summary>
        public string Desc
        {
            get { return this.InnerText; }
            set { this.InnerText = value; }
        }
    }
}
