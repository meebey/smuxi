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

using System.Text;
using System.Diagnostics;
using stringprep.steps;

namespace stringprep
{

    /// <summary>
    /// Summary description for Prep.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// RFC 3454, Appendix B.1
        /// </summary>
        public static readonly MapStep B_1 = new MapStep(RFC3454.B_1, "RFC3454.B_1");
        /// <summary>
        /// RFC 3454, Appendix B.2
        /// </summary>
        public static readonly MapStep B_2 = new MapStep(RFC3454.B_2, "RFC3454.B_2");
        /// <summary>
        /// RFC 3454, Appendix B.3
        /// </summary>
        public static readonly MapStep B_3 = new MapStep(RFC3454.B_3, "RFC3454.B_3");

        /// <summary>
        /// RFC 3454, Appendix C.1.1
        /// </summary>
        public static readonly ProhibitStep C_1_1 = new ProhibitStep(RFC3454.C_1_1, "RFC3454.C_1_1");
        /// <summary>
        /// RFC 3454, Appendix C.1.2
        /// </summary>
        public static readonly ProhibitStep C_1_2 = new ProhibitStep(RFC3454.C_1_2, "RFC3454.C_1_2");
        /// <summary>
        /// RFC 3454, Appendix C.2.1
        /// </summary>
        public static readonly ProhibitStep C_2_1 = new ProhibitStep(RFC3454.C_2_1, "RFC3454.C_2_1");
        /// <summary>
        /// RFC 3454, Appendix C.2.2
        /// </summary>
        public static readonly ProhibitStep C_2_2 = new ProhibitStep(RFC3454.C_2_2, "RFC3454.C_2_2");
        /// <summary>
        /// RFC 3454, Appendix C.3
        /// </summary>
        public static readonly ProhibitStep C_3   = new ProhibitStep(RFC3454.C_3, "RFC3454.C_3");
        /// <summary>
        /// RFC 3454, Appendix C.4
        /// </summary>
        public static readonly ProhibitStep C_4   = new ProhibitStep(RFC3454.C_4, "RFC3454.C_4");
        /// <summary>
        /// RFC 3454, Appendix C.5
        /// </summary>
        public static readonly ProhibitStep C_5   = new ProhibitStep(RFC3454.C_5, "RFC3454.C_5");
        /// <summary>
        /// RFC 3454, Appendix C.6
        /// </summary>
        public static readonly ProhibitStep C_6   = new ProhibitStep(RFC3454.C_6, "RFC3454.C_6");
        /// <summary>
        /// RFC 3454, Appendix C.7
        /// </summary>
        public static readonly ProhibitStep C_7   = new ProhibitStep(RFC3454.C_7, "RFC3454.C_7");
        /// <summary>
        /// RFC 3454, Appendix C.8
        /// </summary>
        public static readonly ProhibitStep C_8   = new ProhibitStep(RFC3454.C_8, "RFC3454.C_8");
        /// <summary>
        /// RFC 3454, Appendix C.9
        /// </summary>
        public static readonly ProhibitStep C_9   = new ProhibitStep(RFC3454.C_9, "RFC3454.C_9");

        /// <summary>
        /// RFC 3454, Section 4
        /// </summary>
        public static readonly NFKCStep NFKC = new NFKCStep();
        /// <summary>
        /// RFC 3454, Section 6
        /// </summary>
        public static readonly BidiStep BIDI = new BidiStep();
        /// <summary>
        /// RFC 3454, Section 7
        /// </summary>
        public static readonly ProhibitStep UNASSIGNED = new ProhibitStep(RFC3454.A_1, "RFC3454.A_1");

        private ProfileStep[] m_profile;

        /// <summary>
        /// Create a new profile, with the given steps.
        /// </summary>
        /// <param name="profile">The steps to perform</param>
        public Profile(ProfileStep[] profile)
        {
            m_profile = profile;
        }

        /// <summary>
        /// Prepare a string, according to the specified profile.
        /// </summary>
        /// <param name="input">The string to prepare</param>
        /// <returns>The prepared string</returns>
        public string Prepare(string input)
        {
            StringBuilder result = new StringBuilder(input);
            Prepare(result);
            return result.ToString();
        }

        /// <summary>
        /// Prepare a string, according to the specified profile, in place.
        /// Not thread safe; make sure the input is locked, if appropriate.
        /// (this is the canonical version, that should be overriden by
        /// subclasses if necessary)
        /// </summary>
        /// <param name="result">The string to prepare in place</param>
        public virtual void Prepare(StringBuilder result)
        {
            foreach (ProfileStep step in m_profile)
            {
                step.Prepare(result);
            }
        }
    }
}
