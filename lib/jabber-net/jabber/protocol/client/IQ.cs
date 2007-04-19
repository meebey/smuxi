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

namespace jabber.protocol.client
{
    /// <summary>
    /// IQ type attribute
    /// </summary>
    [SVN(@"$Id: IQ.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum IQType
    {
        /// <summary>
        /// type='get'
        /// </summary>
        get,
        /// <summary>
        /// type='set'
        /// </summary>
        set,
        /// <summary>
        /// type='result'
        /// </summary>
        result,
        /// <summary>
        /// type='error'
        /// </summary>
        error
    }

    /// <summary>
    /// All IQ packets start here.  The Query property holds the interesting part.
    /// There should usually be a convenience class next to the Query type, which
    /// creates an IQ with the appropriate type of query inside.
    /// </summary>
    [SVN(@"$Id: IQ.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class IQ : Packet
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public IQ(XmlDocument doc) : base("iq", doc)
        {
            ID   = NextID();
            Type = IQType.get;  // get better errors than when there is no type specified.
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public IQ(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(qname.Name, doc) // Note:  *NOT* base(prefix, qname, doc), so that xpath matches are easier
        {
        }

        /// <summary>
        ///
        /// </summary>
        public IQType Type
        {
            get { return (IQType) GetEnumAttr("type", typeof(IQType)); }
            set
            {
                IQType cur = this.Type;
                if (cur == value)
                    return;
                // FIXME: this should iterate through child elements, removing them, I think.
                if (value == IQType.error)
                {
                    this.InnerXml = "";
                    this.AppendChild(new Error(this.OwnerDocument));
                }
                SetAttribute("type", value.ToString());
            }
        }
        /// <summary>
        /// IQ error.
        /// </summary>
        public Error Error
        {
            get { return (Error) this["error"]; }
            set
            {
                this.Type = IQType.error;
                ReplaceChild(value);
            }
        }

        /// <summary>
        /// The query tag inside, regardless of namespace.
        /// If the iq contains something other than query,
        /// use normal XmlElement routines.
        /// </summary>
        public XmlElement Query
        {
            get { return this.GetFirstChildElement(); }
            set { this.InnerXml = ""; this.AddChild(value); }
        }
    }
}
