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
using System.IO;
using System.Threading;
using System.Xml;

using bedrock.io;
using bedrock.util;
using jabber.protocol;

namespace jabber.protocol
{
    /// <summary>
    /// A packet was received.  The specified element will likely be a sub-class
    /// of XmlElement, if the packet is found in the packet factory.
    /// </summary>
    public delegate void ProtocolHandler(Object sender, System.Xml.XmlElement rp);

    /// <summary>
    /// Async XML parsing, according to jabber protocol rules of "interesting".
    /// The root node fires IElementStreamListener.OnDocumentStart(), and each
    /// direct child of the root fires IElementStreamListener.OnTag().
    ///
    /// TODO: Combine with AsyncElementStream, since there's only one impl.
    /// </summary>
    [SVN(@"$Id: ElementStream.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ElementStream
    {
        /// <summary>
        /// The document to create elements in
        /// </summary>
        protected XmlDocument m_doc;

        /// <summary>
        /// The element factory.
        /// </summary>
        protected ElementFactory m_factory = new ElementFactory();

        /// <summary>
        /// The document started.  This will have a full element, even
        /// though only the start tag has been received.
        /// </summary>
        public event ProtocolHandler       OnDocumentStart;

        /// <summary>
        /// The document has completed.
        /// TODO: This isn't fired as often as it needs to be, yet.
        /// </summary>
        public event bedrock.ObjectHandler OnDocumentEnd;

        /// <summary>
        /// A protocol element (child of the doc root) has been received.
        /// </summary>
        public event ProtocolHandler       OnElement;

        /// <summary>
        /// An XML parsing error occurred.
        /// </summary>
        public event bedrock.ExceptionHandler OnError;

        /// <summary>
        /// Create a parser that will report events to the listener.
        /// </summary>
        protected ElementStream()
        {
            m_doc = new XmlDocument();
            m_factory.AddType(new jabber.protocol.stream.Factory());
// TODO: Fix broken build.
//            m_factory.AddType(new jabber.protocol.httpbind.Factory());
        }

        /// <summary>
        /// The document being read into.  This document is used for creating nodes,
        /// but does not actually contain the nodes.
        /// </summary>
        public XmlDocument Document
        {
            get { return m_doc; }
        }

        /// <summary>
        /// Add PacketFactories to get XmlElements with type-safe accessors, for
        /// all of the namespaces you care about.
        /// </summary>
        /// <param name="pf"></param>
        public void AddFactory(IPacketTypes pf)
        {
            m_factory.AddType(pf);
        }

        /// <summary>
        /// Add a type to the packet factory.
        /// </summary>
        /// <param name="localName">Local Name (e.g. query)</param>
        /// <param name="ns">Namespace URI (e.g. jabber:iq:roster)</param>
        /// <param name="t">Type to create</param>
        public void AddType(string localName, string ns, Type t)
        {
            m_factory.AddType(localName, ns, t);
        }

        /// <summary>
        /// Fire the OnDocumentStart event
        /// </summary>
        /// <param name="stream"></param>
        protected void FireOnDocumentStart(XmlElement stream)
        {
            if (OnDocumentStart != null)
                OnDocumentStart(this, stream);
        }

        /// <summary>
        /// Fire the OnElement event
        /// </summary>
        /// <param name="elem"></param>
        protected void FireOnElement(XmlElement elem)
        {
            if (OnElement != null)
                OnElement(this, elem);
        }

        /// <summary>
        /// Fire the OnDocumentEnd event
        /// </summary>
        protected void FireOnDocumentEnd()
        {
            if (OnDocumentEnd != null)
                OnDocumentEnd(this);
        }

        /// <summary>
        /// Fire the OnError event
        /// </summary>
        /// <param name="ex">The exception that was thrown</param>
        protected void FireOnError(Exception ex)
        {
            if (OnError != null)
                OnError(this, ex);
        }
    }
}
