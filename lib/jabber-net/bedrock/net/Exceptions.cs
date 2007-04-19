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
using bedrock.util;

namespace bedrock.net
{
#if NET20
    using System.Security.Cryptography.X509Certificates;
    using System.Net.Security;

    /// <summary>
    /// Error connecting with certificate.
    /// </summary>
    [SVN(@"$Id: Exceptions.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class CertificateException : System.Exception
    {
        /// <summary>
        /// The certificate of the remote side.
        /// </summary>
        public string Certificate;
        /// <summary>
        /// The chain of certs that signed the remote cert.
        /// </summary>
        public string CertificateChain;
        /// <summary>
        /// The policies that were violated.
        /// </summary>
        public SslPolicyErrors PolicyErrors;

        /// <summary>
        /// Create a certificate exception.
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        public CertificateException(X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
            : base("Certificate does not comply with policy.")
        {
            Certificate = cert.ToString(true);
            CertificateChain = "";

            int count = 0;
            foreach (X509ChainElement c in chain.ChainElements)
            {
                CertificateChain += "------ Chain Cert " + count + "------\r\n" + c.Certificate.ToString(true) + "\r\n";
                count++;
            }
            PolicyErrors = errors;
        }

        /// <summary>
        /// More information in the stringified version.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return
                "Certificate: " + Certificate + "\r\n" +
                "Chain: " + CertificateChain + "\r\n" +
                "Errors: " + PolicyErrors + "\r\n" +
                base.ToString();
        }
    }
#endif

    /// <summary>
    /// Lame exception, since I couldn't find one I liked.
    /// </summary>
    [SVN(@"$Id: Exceptions.cs 339 2007-03-02 19:40:49Z hildjj $")]
    [Serializable]
    public class AsyncSocketConnectionException : System.SystemException
    {
        /// <summary>
        /// Create a new exception instance.
        /// </summary>
        /// <param name="description"></param>
        public AsyncSocketConnectionException(string description)
            : base(description)
        {
        }

        /// <summary>
        /// Create a new exception instance.
        /// </summary>
        public AsyncSocketConnectionException()
            : base()
        {
        }

        /// <summary>
        /// Create a new exception instance, wrapping another exception.
        /// </summary>
        /// <param name="description">Desecription of the exception</param>
        /// <param name="e">Inner exception</param>
        public AsyncSocketConnectionException(string description, Exception e)
            : base(description, e)
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// AsyncSocketConnectionException class with serialized
        /// data.
        /// </summary>
        /// <param name="info">The object that holds the serialized
        /// object data.</param>
        /// <param name="ctx">The contextual information about the
        /// source or destination.</param>
        protected AsyncSocketConnectionException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext ctx)
            :
            base(info, ctx)
        {
        }
    }
}
