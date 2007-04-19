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
using System.IO;
using System.Xml;

using jabber.protocol;
using bedrock.util;

using Org.System.Xml.Sax;
using Org.System.Xml.Sax.Helpers;

namespace jabber.protocol
{
    /// <summary>
    /// Summary description for SynchElementStream.
    /// </summary>
    [SVN(@"$Id: SynchElementStream.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class SynchElementStream : ElementStream, IContentHandler, IErrorHandler
    {
        private XmlElement m_stanza = null;

        /// <summary>
        /// Create a parser that reads from the input stream synchronously, in a single thread.
        /// </summary>
        /// <param name="input"></param>
        public SynchElementStream(Stream input) : base(input)
        {
        }

        /// <summary>
        /// Start parsing.  WARNING: this blocks until the stream is disconnected or end stream:stream is received.
        /// </summary>
        public void Start()
        {
            try
            {
                IXmlReader reader = new AElfred.SaxDriver();
                reader.SetFeature(Constants.NamespacesFeature, true);
                reader.SetFeature(AElfred.SaxDriver.FEATURE + "external-parameter-entities", false);
                reader.SetFeature(AElfred.SaxDriver.FEATURE + "external-general-entities", false);
                reader.ErrorHandler = this;
                reader.ContentHandler = this;
                InputSource inSource = new StreamInputSource(m_stream);
                inSource.Encoding = "UTF-8";
                reader.Parse(inSource);
            }
            catch (Exception e)
            {
                FireOnError(e);
            }
        }

        private string Prefix(string qName)
        {
            string[] parts = qName.Split(new char[] {':'});
            return (parts.Length == 2) ? parts[0] : "";
        }

        #region IContentHandler Members

        void IContentHandler.StartDocument()
        {
        }

        void IContentHandler.SkippedEntity(string name)
        {
        }

        void IContentHandler.StartElement(string uri, string localName, string qName, IAttributes atts)
        {
            XmlQualifiedName q  = new XmlQualifiedName(localName, uri);

            XmlElement elem = m_factory.GetElement(Prefix(qName), q, m_doc);
            for (int i=0; i<atts.Length; i++)
            {
                XmlAttribute a = m_doc.CreateAttribute(Prefix(atts.GetQName(i)),
                    atts.GetLocalName(i),
                    atts.GetUri(i));
                a.AppendChild(m_doc.CreateTextNode(atts.GetValue(i)));
                elem.SetAttributeNode(a);
            }

            if ((elem.LocalName != "stream") || (elem.NamespaceURI != URI.STREAM))
            {
                if (m_stanza != null)
                    m_stanza.AppendChild(elem);
                m_stanza = elem;
            }
            else
            {
                FireOnDocumentStart(elem);
            }
        }

        void IContentHandler.EndPrefixMapping(string prefix)
        {
            // TODO:  Add SynchElementStream.EndPrefixMapping implementation
        }

        void IContentHandler.SetDocumentLocator(ILocator locator)
        {
            // TODO:  Add SynchElementStream.SetDocumentLocator implementation
        }

        void IContentHandler.EndElement(string uri, string localName, string qName)
        {
            XmlElement last = m_stanza;
            if (last != null)
            {
                m_stanza = (XmlElement) m_stanza.ParentNode;
                if (m_stanza == null)
                    FireOnElement(last);
            }
        }

        void IContentHandler.EndDocument()
        {
            FireOnDocumentEnd();
        }

        void IContentHandler.Characters(char[] ch, int start, int length)
        {
            if (m_stanza != null)
            {
                m_stanza.AppendChild(
                    m_doc.CreateTextNode(new string(ch, start, length)));
            }
        }

        void IContentHandler.IgnorableWhitespace(char[] ch, int start, int length)
        {
        }

        void IContentHandler.StartPrefixMapping(string prefix, string uri)
        {
        }

        void IContentHandler.ProcessingInstruction(string target, string data)
        {
        }

        #endregion

        #region IErrorHandler Members

        void IErrorHandler.FatalError(ParseError error)
        {
            FireOnError(new SaxParseException(error));
        }

        void IErrorHandler.Warning(ParseError error)
        {
            Debug.WriteLine("XML parse warning: " + error.Message + " at line number: " + error.LineNumber);
        }

        void IErrorHandler.Error(ParseError error)
        {
            FireOnError(new SaxParseException(error));
        }

        #endregion
    }
}
