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
    /// An auth IQ.
    /// </summary>
    [SVN(@"$Id: Auth.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class AuthIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create an Auth IQ.
        /// </summary>
        /// <param name="doc"></param>
        public AuthIQ(XmlDocument doc) : base(doc)
        {
            this.Query = new Auth(doc);
        }
    }

    /// <summary>
    /// Client authentication, with digest support.  Call SetAuth() to compute
    /// the digest.
    /// </summary>
    [SVN(@"$Id: Auth.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Auth : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Auth(XmlDocument doc) :
            base("query", URI.AUTH, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Auth(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Set the authentication information.
        /// TODO: 0k
        /// </summary>
        /// <param name="username">The user name.  NOT the JID.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="StreamID">The id from the stream:stream
        /// that the server sent, or null for plaintext.</param>
        public void SetDigest(string username, string password, string StreamID)
        {
            Debug.Assert(username != null);
            Debug.Assert(password != null);
            Debug.Assert(StreamID != null);
            this.Username = username;
            this.Digest = ShaHash(StreamID, password);
        }
        /// <summary>
        /// Set the authentication information, for plaintext auth.
        /// </summary>
        /// <param name="username">The user name.  NOT the JID.</param>
        /// <param name="password">The user's password.</param>
        public void SetAuth(string username, string password)
        {
            Debug.Assert(username != null);
            Debug.Assert(password != null);
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Set the zero-knowledge information for this iq.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="token"></param>
        /// <param name="sequence"></param>
        public void SetZeroK(string username,
            string password,
            string token,
            int    sequence)
        {
            Debug.Assert(username != null);
            this.Username = username;
            this.Hash = Element.ZeroK(password, token, sequence);
        }

        /// <summary>
        /// The user's account name.  NOT the JID.
        /// </summary>
        public string Username
        {
            get { return GetElem("username"); }
            set { SetElem("username", value); }
        }

        /// <summary>
        /// The plaintext password.
        /// </summary>
        public string Password
        {
            get { return GetElem("password"); }
            set { SetElem("password", value); }
        }

        /// <summary>
        /// SHA1 hash of the StreamID and the password.
        /// </summary>
        public string Digest
        {
            get { return GetElem("digest"); }
            set { SetElem("digest", value); }
        }

        /// <summary>
        /// The resource to connect with.
        /// </summary>
        public string Resource
        {
            get { return GetElem("resource"); }
            set { SetElem("resource", value); }
        }

        /// <summary>
        /// Zero-k token
        /// </summary>
        public string Token
        {
            get { return GetElem("token"); }
            set { SetElem("token", value); }
        }

        /// <summary>
        /// Zero-k sequence
        /// </summary>
        public int Sequence
        {
            get { return Int32.Parse(GetElem("sequence")); }
            set { SetElem("sequence", value.ToString()); }
        }

        /// <summary>
        /// Zero-k hash.  NOT DIGEST!
        /// </summary>
        public string Hash
        {
            get { return GetElem("hash"); }
            set { SetElem("hash", value); }
        }
    }
}
