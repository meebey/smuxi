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

using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Xml;

using bedrock.util;

namespace jabber.protocol
{
    /// <summary>
    /// Packets that have to/from information.
    /// </summary>
    [SVN(@"$Id: Packet.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Packet : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Packet(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localName"></param>
        /// <param name="doc"></param>
        public Packet(string localName, XmlDocument doc) :
            base(localName, doc)
        {
        }

        /// <summary>
        /// The TO address
        /// </summary>
        public JID To
        {
            get { return new JID(this.GetAttribute("to")); }
            set
            {
                if (value == null)
                    this.RemoveAttribute("to");
                else
                    this.SetAttribute("to", value);
            }
        }

        /// <summary>
        ///  The FROM address
        /// </summary>
        public JID From
        {
            get { return new JID(this.GetAttribute("from")); }
            set
            {
                if (value == null)
                    this.RemoveAttribute("from");
                else
                    this.SetAttribute("from", value);
            }
        }

        /// <summary>
        /// The packet ID.
        /// </summary>
        public string ID
        {
            get { return this.GetAttribute("id"); }
            set { this.SetAttribute("id", value); }
        }

        /// <summary>
        /// Swap the To and the From addresses.
        /// </summary>
        public void Swap()
        {
            string tmp = this.GetAttribute("to");
            this.SetAttribute("to", this.GetAttribute("from"));
            this.SetAttribute("from", tmp);
        }
    }
}
