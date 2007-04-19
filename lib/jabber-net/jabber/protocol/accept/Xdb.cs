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
    /// The type attribute
    /// </summary>
    [SVN(@"$Id: Xdb.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum XdbType
    {
        /// <summary>
        /// None specified
        /// </summary>
        NONE = -1,
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
    /// The action attribute.
    /// </summary>
    [SVN(@"$Id: Xdb.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum XdbAction
    {
        /// <summary>
        /// None specified
        /// </summary>
        NONE = -1,
        /// <summary>
        /// action='check'
        /// </summary>
        check,
        /// <summary>
        /// action='insert'
        /// </summary>
        insert
    }

    /// <summary>
    /// The XDB packet.
    /// </summary>
    [SVN(@"$Id: Xdb.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Xdb : jabber.protocol.Packet
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Xdb(XmlDocument doc) : base("xdb", doc)
        {
            ID = NextID();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Xdb(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The contents of the XDB packet
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
        public XdbType Type
        {
            get { return (XdbType) GetEnumAttr("type", typeof(XdbType)); }
            set
            {
                XdbType cur = this.Type;
                if (cur == value)
                    return;
                if (value == XdbType.NONE)
                {
                    RemoveAttribute("type");
                }
                else
                {
                    SetAttribute("type", value.ToString());
                }
            }
        }

        /// <summary>
        /// The action attribute
        /// </summary>
        public XdbAction Action
        {
            get { return (XdbAction) GetEnumAttr("action", typeof(XdbAction)); }
            set
            {
                XdbAction cur = this.Action;
                if (cur == value)
                    return;
                if (value == XdbAction.NONE)
                {
                    RemoveAttribute("action");
                }
                else
                {
                    SetAttribute("action", value.ToString());
                }
            }
        }

        /// <summary>
        /// The namespace to store/retrive from.
        /// </summary>
        public string NS
        {
            get { return GetAttribute("ns"); }
            set { SetAttribute("ns", value); }
        }
    }
}
