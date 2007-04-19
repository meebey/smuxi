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
using System.Collections;

namespace stringprep.steps
{
    /// <summary>
    /// A stringprep profile step to map one input character into 0 or
    /// more output characters.
    /// </summary>
    public class MapStep : ProfileStep
    {
        private string[] m_table = null;
        private static IComparer m_comp = new CharMapComparer();

        /// <summary>
        /// Map from one character to 0+
        /// </summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
        public MapStep(string[] table, string name): base(name)
        {
            m_table = table;
        }

        /// <summary>
        /// Perform mapping for each character of input.
        /// </summary>
        /// <param name="result">Result is modified in place.</param>
        public override void Prepare(System.Text.StringBuilder result)
        {
            // From RFC3454, section 3: Mapped characters are not
            // re-scanned during the mapping step.  That is, if
            // character A at position X is mapped to character B,
            // character B which is now at position X is not checked
            // against the mapping table.
            int pos;
            string map;
            int len;
            for (int i=0; i<result.Length; i++)
            {
                pos = Array.BinarySearch(m_table, result[i], m_comp);
                if (pos < 0)
                    continue;

                map = m_table[pos];
                len = map.Length;
                if (len == 1)
                {
                    result.Remove(i, 1);
                    i--;
                }
                else
                {
                    result[i] = map[1];
                    if (len > 2)
                    {
                        result.Insert(i+1, map.ToCharArray(2, len - 2));
                        i += len - 2;
                    }
                }
            }
        }

        private class CharMapComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                return ((string)x)[0].CompareTo(y);
            }
        }
    }
}
