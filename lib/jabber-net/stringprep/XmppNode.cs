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
    /// A stringprep profile for draft-ietf-xmpp-nodeprep-02, for Jabber nodes (the "user" part).
    /// </summary>
    public class XmppNode : Profile
    {
        private static readonly ProhibitStep XmppNodeprepProhibit =
            new ProhibitStep(new char[][]
                {   // note: these *must* be sorted by code.
                    new char[] {'"', '\x0000'},
                    new char[] {'&', '\x0000'},
                    new char[] {'\'', '\x0000'},
                    new char[] {'/', '\x0000'},
                    new char[] {':', '\x0000'},
                    new char[] {'<', '\x0000'},
                    new char[] {'>', '\x0000'},
                    new char[] {'@', '\x0000'},
                }, "XMPP Node");

        /// <summary>
        /// Create a new XmppNode profile instance.
        /// </summary>
        public XmppNode() :
            base( new ProfileStep[] {   B_1, B_2, NFKC,
                                        C_1_1, C_1_2, C_2_1, C_2_2,
                                        C_3, C_4, C_5, C_6, C_7, C_8, C_9,
                                        XmppNodeprepProhibit,
                                        BIDI, UNASSIGNED} )
        {
        }
    }
}
