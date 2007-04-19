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

namespace jabber.protocol.iq
{
    /*
     * <iq type="set" to="horatio@denmark" from="sailor@sea" id="i_oob_001">
     *   <query xmlns="jabber:iq:oob">
     *     <url>http://denmark/act4/letter-1.html</url>
     *     <desc>There's a letter for you sir.</desc>
     *   </query>
     * </iq>
     */
    /// <summary>
    /// IQ packet with an oob query element inside.
    /// </summary>
    [SVN(@"$Id: OOB.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class OobIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create an OOB IQ.
        /// </summary>
        /// <param name="doc"></param>
        public OobIQ(XmlDocument doc) : base(doc)
        {
            this.Query = new OOB(doc);
        }
    }

    /// <summary>
    /// An oob query element for file transfer.
    /// </summary>
    [SVN(@"$Id: OOB.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class OOB : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public OOB(XmlDocument doc) : base("query", URI.OOB, doc)
        {
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public OOB(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// URL to send/receive from
        /// </summary>
        public string Url
        {
            get { return GetElem("url"); }
            set { SetElem("url", value); }
        }

        /// <summary>
        /// File description
        /// </summary>
        public string Desc
        {
            get { return GetElem("desc"); }
            set { SetElem("desc", value); }
        }
    }
}
