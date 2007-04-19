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
using System.IO;
using System.Diagnostics;
using System.Xml;

using bedrock.util;
using jabber.protocol.stream;

namespace jabber.connection.sasl
{
    /// <summary>
    /// SASL Mechanism PLAIN as specified in RFC 2595.
    /// </summary>
    [SVN(@"$Id: PlainProcessor.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class PlainProcessor : SASLProcessor
    {
        /// <summary>
        /// Perform the next step
        /// </summary>
        /// <param name="s">Null if it's the initial response</param>
        /// <param name="doc">Document to create Steps in</param>
        /// <returns></returns>
        public override Step step(Step s, XmlDocument doc)
        {
            Debug.Assert(s == null);
            Auth a = new Auth(doc);
            a.Mechanism = MechanismType.PLAIN;
            MemoryStream ms = new MemoryStream();

            // message = [authorize-id] NUL authenticate-id NUL password

            // Skip authzid.
            ms.WriteByte(0);
            string u = this[USERNAME];
            if ((u == null) || (u == ""))
                throw new SASLException("Username required");
            byte[] bu = System.Text.Encoding.UTF8.GetBytes(u);
            ms.Write(bu, 0, bu.Length);
            ms.WriteByte(0);
            string p = this[PASSWORD];
            if ((p == null) || (p == ""))
                throw new SASLException("Password required");
            byte[] pu = System.Text.Encoding.UTF8.GetBytes(p);
            ms.Write(pu, 0, pu.Length);

            a.Bytes = ms.ToArray();
            return a;
        }
    }
}
