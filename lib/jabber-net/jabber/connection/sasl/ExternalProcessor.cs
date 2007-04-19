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
    /// SASL Mechanism EXTERNAL as specified in XEP-0178.
    /// </summary>
    [SVN(@"$Id: ExternalProcessor.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class ExternalProcessor : SASLProcessor
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
            a.Mechanism = MechanismType.EXTERNAL;
            MemoryStream ms = new MemoryStream();
            return a;
        }
    }
}
