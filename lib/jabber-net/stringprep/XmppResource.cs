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

using stringprep.steps;

namespace stringprep
{
    /// <summary>
    /// A stringprep profile for draft-ietf-xmpp-resourceprep-02, used for Jabber resources.
    /// </summary>
    public class XmppResource : Profile
    {
        /// <summary>
        /// Create an instance of an XmppResource
        /// </summary>
        public XmppResource() :
            base( new ProfileStep[] {   B_1, NFKC,
                                        C_1_2, C_2_1, C_2_2,
                                        C_3, C_4, C_5, C_6, C_7, C_8, C_9,
                                        BIDI, UNASSIGNED} )
        {
        }
    }
}
