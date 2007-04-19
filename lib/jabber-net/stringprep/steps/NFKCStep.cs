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


/*
 * Assumption: UCS2.  The astral planes don't exist.  At least according to windows?
 *
 * Look over here.  Something shiny!
 */
using System;
using System.Text;
using stringprep.unicode;

namespace stringprep.steps
{
    /// <summary>
    /// Perform Unicode Normalization Form KC.
    /// </summary>
    public class NFKCStep : ProfileStep
    {
        /// <summary>
        /// Create an NFKC step.
        /// </summary>
        public NFKCStep() : base("NFKC")
        {
        }

        /// <summary>
        /// Perform NFKC.  General overview: Decompose, Reorder, Compose
        /// </summary>
        /// <param name="result"></param>
        public override void Prepare(StringBuilder result)
        {
            // From Unicode TR15: (http://www.unicode.org/reports/tr15)
            // R1. Normalization Form C
            // The Normalization Form C for a string S is obtained by applying the following process,
            // or any other process that leads to the same result:
            //
            // 1) Generate the canonical decomposition for the source string S according to the
            // decomposition mappings in the latest supported version of the Unicode Character Database.
            //
            // 2) Iterate through each character C in that decomposition, from first to last.
            // If C is not blocked from the last starter L, and it can be primary combined with L,
            // then replace L by the composite L-C, and remove C.
            Decomp(result);

            if (result.Length > 0)
            {
                CanonicalOrdering(result);
                Comp(result);
            }
        }


        private void Decomp(StringBuilder result)
        {
            int len;
            string ex;

            // Decompose
            for (int i=0; i< result.Length; i++)
            {
                ex = Decompose.Find(result[i]);
                if (ex == null)
                    continue;

                result[i] = ex[0];
                len = ex.Length - 1;
                if (len > 0)
                {
                    result.Insert(i+1, ex.ToCharArray(1, ex.Length-1));
                    i += len;
                }
            }
        }

        /// <summary>
        /// Reorder characters in the given range into their correct cannonical ordering with
        /// respect to combining class.
        /// </summary>
        /// <param name="buf">Buffer to reorder</param>
        private void CanonicalOrdering(StringBuilder buf)
        {
            int i, j;
            bool swap = false;
            int p_a, p_b;
            char t;
            int start = 0;
            int stop = buf.Length - 1;

            // From Unicode 3.0, section 3.10
            // R1 For each character x in D, let p(x) be the combining class of x
            // R2 Wenever any pair (A, B) of adjacent characters in D is such that p(B)!=0 and
            //    p(A)>p(B), exchange those characters
            // R3 Repeat step R2 until no exchanges can be made among any of the characters in D
            do
            {
                swap = false;
                p_a = Combining.Class(buf[start]);

                for (i = start; i < stop; i++)
                {
                    p_b = Combining.Class(buf[i + 1]);
                    if ((p_b != 0) && (p_a > p_b))
                    {
                        for (j = i; j > 0; j--)
                        {
                            if (Combining.Class(buf[j]) <= p_b)
                                break;

                            t = buf[j + 1];
                            buf[j + 1] = buf[j];
                            buf[j] = t;
                            swap = true;
                        }
                        /* We're re-entering the loop looking at the old
                           character again.  Don't reset p_a.*/
                        continue;
                    }
                    p_a = p_b;

                    // once we get to a start character without any swaps,
                    // there can be no further changes.  No sense constantly
                    // rechecking stuff we've already checked.
                    if (!swap && (p_a == 0))
                        start = i;
                }
            } while (swap);
        }

        private void Comp(StringBuilder result)
        {
            // All decomposed and reordered.
            // Combine all combinable characters.
            int cc;
            int last_cc = 0;
            char c;
            int last_start = 0;

            for (int i=0; i<result.Length; i++)
            {
                cc = Combining.Class(result[i]);
                if ((i > 0) &&
                    ((last_cc == 0) || (last_cc != cc)) &&
                    Compose.Combine(result[last_start], result[i], out c))
                {
                    result[last_start] = c;
                    result.Remove(i, 1);
                    i--;

                    if (i == last_start)
                        last_cc = 0;
                    else
                        last_cc = Combining.Class(result[i - 1]);

                    continue;
                }

                if (cc == 0)
                    last_start = i;

                last_cc = cc;
            }
        }
    }
}
