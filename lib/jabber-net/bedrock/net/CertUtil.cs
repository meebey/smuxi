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

#if !NO_SSL && !NET20  && !__MonoCS__
using Org.Mentalis.Security.Certificates;
using bedrock.util;

namespace bedrock.net
{
    /// <summary>
    /// Utilities for creating certificates
    /// </summary>
    [SVN(@"$Id: CertUtil.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class CertUtil
    {
        /// <summary>
        /// Can this cert be used for server authentication?
        /// </summary>
        private const string OID_PKIX_KP_SERVER_AUTH = "1.3.6.1.5.5.7.3.1";
        /// <summary>
        /// Can this cert be used for client authentication?
        /// </summary>
        private const string OID_PKIX_KP_CLIENT_AUTH = "1.3.6.1.5.5.7.3.2";

        /// <summary>
        /// Find a server certificate in the given store.
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        public static Certificate FindServerCert(CertificateStore store)
        {
            // return store.FindCertificate(new string[] {OID_PKIX_KP_SERVER_AUTH});
            return store.FindCertificateByUsage(new string[] {OID_PKIX_KP_SERVER_AUTH});
        }

        /// <summary>
        /// Find a client certificate in the given store.
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        public static Certificate FindClientCert(CertificateStore store)
        {
            //return store.FindCertificate(new string[] {OID_PKIX_KP_CLIENT_AUTH});
            return store.FindCertificateByUsage(new string[] {OID_PKIX_KP_CLIENT_AUTH});
        }
    }
}
#endif
