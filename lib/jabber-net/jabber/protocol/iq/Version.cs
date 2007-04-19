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
    /// <summary>
    /// IQ packet with a version query element inside.
    /// </summary>
    [SVN(@"$Id: Version.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class VersionIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create a version IQ
        /// </summary>
        /// <param name="doc"></param>
        public VersionIQ(XmlDocument doc) : base(doc)
        {
            this.Query = new Version(doc);
        }
    }

    /// <summary>
    /// A time query element.
    /// </summary>
    [SVN(@"$Id: Version.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Version : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Version(XmlDocument doc) : base("query", URI.VERSION, doc)
        {
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Version(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Name of the entity.
        /// </summary>
        public string EntityName
        {
            get { return GetElem("name"); }
            set { SetElem("name", value); }
        }

        /// <summary>
        /// Enitity version.  (Version was a keyword, or something)
        /// </summary>
        public string Ver
        {
            get { return GetElem("version"); }
            set { SetElem("version", value); }
        }

        /// <summary>
        /// Operating system of the entity.
        /// </summary>
        public string OS
        {
            get { return GetElem("os"); }
            set { SetElem("os", value); }
        }
    }
}
