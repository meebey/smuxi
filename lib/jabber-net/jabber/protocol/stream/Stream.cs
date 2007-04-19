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

using System.Security.Cryptography;
using System.Xml;
using bedrock.util;
using jabber.protocol;

namespace jabber.protocol.stream
{
    /// <summary>
    /// The fabled stream:stream packet.  Id's get assigned automatically on allocation.
    /// </summary>
    [SVN(@"$Id: Stream.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Stream : Packet
    {
        private static readonly RandomNumberGenerator RNG = RandomNumberGenerator.Create();

        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="xmlns"></param>
        public Stream(XmlDocument doc, string xmlns) :
            base("stream", new XmlQualifiedName("stream", jabber.protocol.URI.STREAM), doc)
        {
            byte[] buf = new byte[4];
            RNG.GetBytes(buf);
            ID = HexString(buf);
            NS = xmlns;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Stream(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Default stream namespace.  xmlns=''.
        /// </summary>
        public string NS
        {
            get { return this.GetAttribute("xmlns"); }
            set { this.SetAttribute("xmlns", value); }
        }

        /// <summary>
        /// The version attribute.  "1.0" for an XMPP-core-compliant stream.
        /// </summary>
        public string Version
        {
            get { return this.GetAttribute("version"); }
            set { this.SetAttribute("version", value); }
        }
    }
}
