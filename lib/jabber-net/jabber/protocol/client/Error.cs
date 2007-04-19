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
    /// Error codes for IQ and message
    /// </summary>
    [SVN(@"$Id: Error.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum ErrorCode
    {
        /// <summary>
        /// Bad request (400)
        /// </summary>
        BAD_REQUEST             = 400,
        /// <summary>
        /// Unauthorized (401)
        /// </summary>
        UNAUTHORIZED            = 401,
        /// <summary>
        /// Payment required (402)
        /// </summary>
        PAYMENT_REQUIRED        = 402,
        /// <summary>
        /// Forbidden (403)
        /// </summary>
        FORBIDDEN               = 403,
        /// <summary>
        /// Not found (404)
        /// </summary>
        NOT_FOUND               = 404,
        /// <summary>
        /// Not allowed (405)
        /// </summary>
        NOT_ALLOWED             = 405,
        /// <summary>
        /// Not acceptable (406)
        /// </summary>
        NOT_ACCEPTABLE          = 406,
        /// <summary>
        /// Registration required (407)
        /// </summary>
        REGISTRATION_REQUIRED   = 407,
        /// <summary>
        /// Request timeout (408)
        /// </summary>
        REQUEST_TIMEOUT         = 408,
        /// <summary>
        /// Conflict (409)
        /// </summary>
        CONFLICT                = 409,
        /// <summary>
        /// Internal server error (500)
        /// </summary>
        INTERNAL_SERVER_ERROR   = 500,
        /// <summary>
        /// Not implemented (501)
        /// </summary>
        NOT_IMPLEMENTED         = 501,
        /// <summary>
        /// Remote server error (502)
        /// </summary>
        REMOTE_SERVER_ERROR     = 502,
        /// <summary>
        /// Service unavailable (503)
        /// </summary>
        SERVICE_UNAVAILABLE     = 503,
        /// <summary>
        /// Remote server timeout (504)
        /// </summary>
        REMOTE_SERVER_TIMEOUT   = 504,
        /// <summary>
        /// Disconnected (510)
        /// </summary>
        DISCONNECTED            = 510
    }

    /// <summary>
    /// Error IQ
    /// </summary>
    [SVN(@"$Id: Error.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class IQError : IQ
    {
        /// <summary>
        /// Create an error IQ with the given code and message.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="code"></param>
        public IQError(XmlDocument doc, ErrorCode code) : base(doc)
        {
            XmlElement e = doc.CreateElement("error");
            this.Type = IQType.error;
            e.SetAttribute("code", ((int)code).ToString());
            e.InnerText = code.ToString();
            this.AppendChild(e);
        }
    }

    /// <summary>
    /// Error in a message or IQ.
    /// </summary>
    [SVN(@"$Id: Error.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Error : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Error(XmlDocument doc) : base("error", doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Error(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The error code, as an enumeration.
        /// </summary>
        public ErrorCode Code
        {
            get { return (ErrorCode) IntCode; }
            set { IntCode = (int) value; }
        }

        /// <summary>
        /// The error code, as an integer.
        /// </summary>
        public int IntCode
        {
            get { return GetIntAttr("code"); }
            set { this.SetAttribute("code", value.ToString()); }
        }

        /// <summary>
        /// The error message
        /// </summary>
        public string Message
        {
            get { return this.InnerXml; }
            set { this.InnerText = value; }
        }
    }
}
