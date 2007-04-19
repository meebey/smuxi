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
    /// Message type attribute
    /// </summary>
    [SVN(@"$Id: Message.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum MessageType
    {
        /// <summary>
        /// Normal message
        /// </summary>
        normal = -1,
        /// <summary>
        /// Error message
        /// </summary>
        error,
        /// <summary>
        /// Chat (one-to-one) message
        /// </summary>
        chat,
        /// <summary>
        /// Groupchat
        /// </summary>
        groupchat,
        /// <summary>
        /// Headline
        /// </summary>
        headline
    }
    /// <summary>
    /// A client-to-client message.
    /// TODO: Some XHTML is supported by setting the .Html property,
    /// but extra xmlns="" get put everywhere at the moment.
    /// </summary>
    [SVN(@"$Id: Message.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Message : Packet
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Message(XmlDocument doc) : base("message", doc)
        {
            ID = NextID();
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Message(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(qname.Name, doc)  // Note:  *NOT* base(prefix, qname, doc), so that xpath matches are easier
        {
        }

        /// <summary>
        /// The message type attribute
        /// </summary>
        public MessageType Type
        {
            get { return (MessageType) GetEnumAttr("type", typeof(MessageType)); }
            set
            {
                if (value == MessageType.normal)
                    RemoveAttribute("type");
                else
                    SetAttribute("type", value.ToString());
            }
        }

        private void NormalizeHtml(XmlElement body, string html)
        {
            XmlDocument d = new XmlDocument();
            d.LoadXml("<html xmlns='" + URI.XHTML + "'>" + html + "</html>");
            foreach (XmlNode node in d.DocumentElement.ChildNodes)
            {
                body.AppendChild(this.OwnerDocument.ImportNode(node, true));
            }
        }

        /// <summary>
        /// On set, creates both an html element, and a body element, which will
        /// have the de-html'd version of the html element.
        /// </summary>
        public string Html
        {
            get
            {
                // Thanks, Mr. Postel.
                XmlElement h = this["html"];
                if (h == null)
                    return "";
                XmlElement b = h["body"];
                if (b == null)
                    return "";
                string xml = b.InnerXml;
                // HACK: yeah, yeah, I know.
                return xml.Replace(" xmlns=\"" + URI.XHTML + "\"", "");
            }
            set
            {
                XmlElement old = this["html"];
                if (old != null)
                    this.RemoveChild(old);
                XmlElement html = this.OwnerDocument.CreateElement(null, "html", URI.XHTML_IM);
                XmlElement body = this.OwnerDocument.CreateElement(null, "body", URI.XHTML);
                NormalizeHtml(body, value);
                html.AppendChild(body);
                this.AppendChild(html);
                this.Body = body.InnerText;
            }
        }

        /// <summary>
        /// The message body
        /// </summary>
        public string Body
        {
            get { return GetElem("body"); }
            set { SetElem("body", value); }
        }

        /// <summary>
        /// The message thread
        /// TODO: some help to generate these, please.
        /// </summary>
        public string Thread
        {
            get { return GetElem("thread"); }
            set { SetElem("thread", value); }
        }
        /// <summary>
        /// The message subject
        /// </summary>
        public string Subject
        {
            get { return GetElem("subject"); }
            set { SetElem("subject", value); }
        }
        /// <summary>
        /// The first x tag, regardless of namespace.
        /// </summary>
        public XmlElement X
        {
            get { return this["x"]; }
            set { this.AddChild(value); }
        }

        /// <summary>
        /// Message error.
        /// </summary>
        public Error Error
        {
            get { return (Error) this["error"]; }
            set
            {
                this.Type = MessageType.error;
                ReplaceChild(value);
            }
        }
    }
}
