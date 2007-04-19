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

using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml;

using bedrock.util;

namespace jabber.protocol.iq
{
    /// <summary>
    /// An browse IQ.
    /// </summary>
    [SVN(@"$Id: Browse.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class BrowseIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create a Browse IQ.
        /// </summary>
        /// <param name="doc"></param>
        public BrowseIQ(XmlDocument doc) : base(doc)
        {
            this.Query = new Browse(doc);
        }
    }

    /// <summary>
    /// Browse IQ query.
    /// </summary>
    [SVN(@"$Id: Browse.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Browse : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Browse(XmlDocument doc) :
            base("query", URI.BROWSE, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Browse(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The full JabberID of the entity described.
        /// </summary>
        public JID JID
        {
            get { return new JID(GetAttribute("jid")); }
            set { SetAttribute("jid", value.ToString()); }
        }

        /// <summary>
        /// One of the categories from the category list, or a non-standard category prefixed with the string "x-".
        /// </summary>
        public string Category
        {
            get { return GetAttribute("category"); }
            set { SetAttribute("category", value); }
        }

        /// <summary>
        /// One of the official types from the specified category, or a non-standard type prefixed with the string "x-".
        /// </summary>
        public string Type
        {
            get { return GetAttribute("type"); }
            set { SetAttribute("type", value); }
        }

        /// <summary>
        /// A friendly name that may be used in a user interface.
        /// </summary>
        public string BrowseName
        {
            get { return GetAttribute("name"); }
            set { SetAttribute("name", value); }
        }

        /// <summary>
        /// A string containing the version of the node, equivalent to the response provided to a
        /// query in the 'jabber:iq:version' namespace. This is useful for servers, especially for lists of services
        /// (see the 'service/serverlist' category/type above).
        /// </summary>
        public string Version
        {
            get { return GetAttribute("version"); }
            set { SetAttribute("version", value); }
        }

        /// <summary>
        /// Sub-items of this item
        /// </summary>
        /// <returns></returns>
        public Browse[] GetItems()
        {
            XmlNodeList nl = GetElementsByTagName("item", URI.BROWSE);
            Browse[] items = new Browse[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                items[i] = (Browse) n;
                i++;
            }
            return items;
        }

        /// <summary>
        /// Add an item to the sub-item list.
        /// </summary>
        /// <returns></returns>
        public Browse AddItem()
        {
            Browse b = new Browse(this.OwnerDocument);
            this.AppendChild(b);
            return b;
        }

        /// <summary>
        /// The namespaces advertised by this item.
        /// </summary>
        /// <returns></returns>
        public string[] GetNamespaces()
        {
            XmlNodeList nl = GetElementsByTagName("ns", URI.BROWSE);
            string[] items = new string[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                items[i] = n.InnerText;
                i++;
            }
            return items;
        }

        /// <summary>
        /// Add a namespace to the namespaces supported by this item.
        /// </summary>
        /// <param name="ns"></param>
        public void AddNamespace(string ns)
        {
            XmlElement e = this.OwnerDocument.CreateElement(null, "ns", URI.BROWSE);
            e.InnerText = ns;
            this.AppendChild(e);
        }
    }
}
