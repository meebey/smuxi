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
using System.Xml;

using xpnet;

using bedrock.io;
using bedrock.util;

namespace jabber.protocol
{
    /// <summary>
    /// Summary description for AsynchElementStream.
    /// TODO: combine with ElementStream, since there's only one impl now.
    /// </summary>
    [SVN(@"$Id: AsynchElementStream.cs 342 2007-03-06 00:15:44Z hildjj $")]
    public class AsynchElementStream : ElementStream
    {
        private static System.Text.Encoding utf = System.Text.Encoding.UTF8;

        private BufferAggregate m_buf   = new BufferAggregate();
        private Encoding m_enc   = new UTF8Encoding();
        private NS m_ns    = new NS();
        private XmlElement m_elem  = null;
        private XmlElement m_root  = null;
        private bool m_cdata = false;

        /// <summary>
        /// Create an instance.
        /// </summary>
        public AsynchElementStream()
        {
        }

        /// <summary>
        /// Put bytes into parser.  Used by test routines, only, for convenience.
        /// </summary>
        /// <param name="buf"></param>
        public void Push(byte[] buf)
        {
            Push(buf, 0, buf.Length);
        }

        /// <summary>
        /// Put bytes into the parser.
        /// </summary>
        /// <param name="buf">The bytes to put into the parse stream</param>
        /// <param name="offset">Offset into buf to start at</param>
        /// <param name="length">Number of bytes to write</param>
        public void Push(byte[] buf, int offset, int length)
        {
            // or assert, really, but this is a little nicer.
            if (length == 0)
                return;

            // No locking is required.  Read() won't get called again
            // until this method returns.  Keep in mind that we're
            // already on a thread in a ThreadPool, which is created
            // and managed by System.IO at the end of the day.

            // TODO: only do this copy if we have a partial token at the
            // end of parsing.
            byte[] copy = new byte[length];
            System.Buffer.BlockCopy(buf, offset, copy, 0, length);
            m_buf.Write(copy);

            byte[] b = m_buf.GetBuffer();
            int off = 0;
            TOK tok = TOK.END_TAG;
            ContentToken ct = new ContentToken();

            try
            {
                while (off < b.Length)
                {


                    if (m_cdata)
                        tok = m_enc.tokenizeCdataSection(b, off, b.Length, ct);
                    else
                        tok = m_enc.tokenizeContent(b, off, b.Length, ct);

                    switch (tok)
                    {
                    case TOK.EMPTY_ELEMENT_NO_ATTS:
                    case TOK.EMPTY_ELEMENT_WITH_ATTS:
                        StartTag(b, off, ct, tok);
                        EndTag(b, off, ct, tok);
                        break;
                    case TOK.START_TAG_NO_ATTS:
                    case TOK.START_TAG_WITH_ATTS:
                        StartTag(b, off, ct, tok);
                        break;
                    case TOK.END_TAG:
                        EndTag(b, off, ct, tok);
                        break;
                    case TOK.DATA_CHARS:
                    case TOK.DATA_NEWLINE:
                        AddText(utf.GetString(b, off, ct.TokenEnd - off));
                        break;
                    case TOK.CHAR_REF:
                    case TOK.MAGIC_ENTITY_REF:
                        AddText(new string(new char[] { ct.RefChar1 }));
                        break;
                    case TOK.CHAR_PAIR_REF:
                        AddText(new string(new char[] {ct.RefChar1,
                                                              ct.RefChar2}));
                        break;
                    case TOK.COMMENT:
                        if (m_elem != null)
                        {
                            // <!-- 4
                            //  --> 3
                            int start = off + 4*m_enc.MinBytesPerChar;
                            int end = ct.TokenEnd - off -
                                    7*m_enc.MinBytesPerChar;
                            string text = utf.GetString(b, start, end);
                            m_elem.AppendChild(m_doc.CreateComment(text));
                        }
                        break;
                    case TOK.CDATA_SECT_OPEN:
                        m_cdata = true;
                        break;
                    case TOK.CDATA_SECT_CLOSE:
                        m_cdata = false;
                        break;
                    case TOK.XML_DECL:
                        // thou shalt use UTF8, and XML version 1.
                        // i shall ignore evidence to the contrary...

                        // TODO: Throw an exception if these assuptions are
                        // wrong
                        break;
                    case TOK.ENTITY_REF:
                    case TOK.PI:
                        throw new System.NotImplementedException("Token type not implemented: " + tok);
                    }
                    off = ct.TokenEnd;
                    ct.clearAttributes();
                }
            }
            catch (PartialTokenException)
            {
                // ignored;
            }
            catch (ExtensibleTokenException)
            {
                // ignored;
            }
            catch (Exception e)
            {
                throw new XMLParseException(e, this, buf, offset, length);
            }
            finally
            {
                m_buf.Clear(off);
                ct.clearAttributes();
            }
        }

        private void StartTag(byte[] buf, int offset,
                              ContentToken ct, TOK tok)
        {
            int colon;
            string name;
            string prefix;
            Hashtable ht = new Hashtable();

            m_ns.PushScope();

            // if i have attributes
            if ((tok == TOK.START_TAG_WITH_ATTS) ||
                (tok == TOK.EMPTY_ELEMENT_WITH_ATTS))
            {
                int start;
                int end;
                string val;
                for (int i=0; i<ct.getAttributeSpecifiedCount(); i++)
                {
                    start = ct.getAttributeNameStart(i);
                    end = ct.getAttributeNameEnd(i);
                    name = utf.GetString(buf, start, end - start);

                    start = ct.getAttributeValueStart(i);
                    end =  ct.getAttributeValueEnd(i);
                    val = utf.GetString(buf, start, end - start);

                    // <foo b='&amp;'/>
                    // <foo b='&amp;amp;'
                    // TODO: if val includes &amp;, it gets double-escaped
                    if (name.StartsWith("xmlns:"))
                    {
                        colon = name.IndexOf(':');
                        prefix = name.Substring(colon+1);
                        m_ns.AddNamespace(prefix, val);
                    }
                    else if (name == "xmlns")
                    {
                        m_ns.AddNamespace(string.Empty, val);
                    }
                    ht.Add(name, val);
                }
            }

            name = utf.GetString(buf,
                                 offset + m_enc.MinBytesPerChar,
                                 ct.NameEnd - offset - m_enc.MinBytesPerChar);
            colon = name.IndexOf(':');
            string ns = "";
            prefix = "";
            if (colon > 0)
            {
                prefix = name.Substring(0, colon);
                name = name.Substring(colon + 1);
                ns = m_ns.LookupNamespace(prefix);
            }
            else
            {
                ns = m_ns.DefaultNamespace;
            }

            XmlQualifiedName q = new XmlQualifiedName(name, ns);
            XmlElement elem = m_factory.GetElement(prefix, q, m_doc);


            foreach (string attrname in ht.Keys)
            {
                colon = attrname.IndexOf(':');
                if (colon > 0)
                {
                    prefix = attrname.Substring(0, colon);
                    name = attrname.Substring(colon+1);

                    XmlAttribute attr = m_doc.CreateAttribute(prefix,
                                                              name,
                                                              m_ns.LookupNamespace(prefix));
                    attr.InnerXml = (string)ht[attrname];
                    elem.SetAttributeNode(attr);
                }
                else
                {
                    XmlAttribute attr = m_doc.CreateAttribute(attrname);
                    attr.InnerXml = (string)ht[attrname];
                    elem.SetAttributeNode(attr);
                }
            }


            if (m_root == null)
            {
                m_root = elem;
                FireOnDocumentStart(m_root);
            }
            else
            {
                if (m_elem != null)
                    m_elem.AppendChild(elem);
                m_elem = elem;
            }
        }

        private void EndTag(byte[] buf, int offset,
                            ContentToken ct, TOK tok)
        {
            m_ns.PopScope();

            if (m_elem == null)
            {// end of doc
                FireOnDocumentEnd();
                return;
            }

            string name = null;

            if ((tok == TOK.EMPTY_ELEMENT_WITH_ATTS) ||
                (tok == TOK.EMPTY_ELEMENT_NO_ATTS))
                name = utf.GetString(buf,
                                     offset + m_enc.MinBytesPerChar,
                                     ct.NameEnd - offset -
                                     m_enc.MinBytesPerChar);
            else
                name = utf.GetString(buf,
                                     offset + m_enc.MinBytesPerChar*2,
                                     ct.NameEnd - offset -
                                     m_enc.MinBytesPerChar*2);


            if (m_elem.Name != name)
                throw new XmlException("Invalid end tag: " + name +
                                       " != " + m_elem.Name);

            XmlElement parent = (XmlElement)m_elem.ParentNode;
            if (parent == null)
            {
                FireOnElement(m_elem);
            }
            m_elem = parent;
        }

        private void AddText(string text)
        {
            if (m_elem != null)
            {
                m_elem.AppendChild(m_doc.CreateTextNode(text));
            }
        }

        /// <summary>
        /// There was an error parsing XML.  What was the context?
        /// </summary>
        public class XMLParseException : Exception
        {
            private string m_context = null;

            /// <summary>
            /// Some XML parsing error occurred.  Wrap it, and generate a little more context, so that we can try
            /// to figure out where the actual error happened.
            /// </summary>
            /// <param name="innerException"></param>
            /// <param name="stream"></param>
            /// <param name="buf"></param>
            /// <param name="offset"></param>
            /// <param name="length"></param>
            public XMLParseException(Exception innerException, AsynchElementStream stream, byte[] buf, int offset, int length)
                : base("Parsing exception", innerException)
            {
                XmlElement e = stream.m_elem;
                XmlElement last = null;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                
                while (e != null)
                {
                    last = e;
                    e = e.ParentNode as XmlElement;
                }

                if (last != null)
                {
                    sb.Append("Outer element: ");
                    sb.Append(last.OuterXml);
                    sb.Append("\n");
                }
                else
                {
                    sb.Append("Root stanza\n");
                }

                sb.Append("New text (note: it's normal to see what looks like extra close tags here): ");
                try
                {
                    sb.Append(AsynchElementStream.utf.GetString(buf, offset, length));
                }
                catch (Exception)
                {
                    sb.Append("Error in UTF8 decode: ");
                    sb.Append(Element.HexString(buf, offset, length));
                }
                m_context = sb.ToString();
            }
            /// <summary>
            /// More context of where the error ocurred
            /// </summary>
            public string Context
            {
                get { return m_context; }
            }

            /// <summary>
            /// String representation.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return base.ToString() + "\n----------\n\nContext:\n" + m_context;
            }
        }
    }
}
