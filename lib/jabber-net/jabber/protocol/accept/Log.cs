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
    /// The type field in a log tag.
    /// </summary>
    [SVN(@"$Id: Log.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum LogType
    {
        /// <summary>
        /// None specified
        /// </summary>
        NONE = -1,
        /// <summary>
        /// type='warn'
        /// </summary>
        warn,
        /// <summary>
        /// type='info'
        /// </summary>
        info,
        /// <summary>
        /// type='verbose'
        /// </summary>
        verbose,
        /// <summary>
        /// type='debug'
        /// </summary>
        debug
    }

    /// <summary>
    /// The log packet.
    /// </summary>
    [SVN(@"$Id: Log.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Log : jabber.protocol.Packet
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Log(XmlDocument doc) : base("log", doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Log(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The element inside the route tag.
        /// </summary>
        public XmlElement Element
        {
            get { return this["element"]; }
            set { AddChild(value); }
        }

        /// <summary>
        /// The type attribute
        /// </summary>
        public LogType Type
        {
            get { return (LogType) GetEnumAttr("type", typeof(LogType)); }
            set
            {
                LogType cur = this.Type;
                if (cur == value)
                    return;
                if (value == LogType.NONE)
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
        /// The namespace for logging
        /// </summary>
        public string NS
        {
            get { return GetAttribute("ns"); }
            set { SetAttribute("ns", value); }
        }

        /// <summary>
        /// The server thread this came from
        /// </summary>
        public string Thread
        {
            get { return GetAttribute("thread"); }
            set { SetAttribute("thread", value); }
        }

        /// <summary>
        /// Time sent.
        /// </summary>
        public DateTime Timestamp
        {
            get { return JabberDate(GetAttribute("timestamp")); }
            set { SetAttribute("timestamp", JabberDate(value)); }
        }

    }
}
