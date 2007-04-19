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


namespace stringprep.unicode
{
    /// <summary>
    /// Combine combining characters, where possible.
    /// Note: this is still Black Magic, as far as I can tell.
    /// </summary>
    public class Compose
    {
        private static int Index(char c)
        {
            int p = c >> 8;
            if (p >= ComposeData.Table.Length)
                return 0;
            if (ComposeData.Table[p] == 255)
                return 0;
            else
                return ComposeData.Data[ComposeData.Table[p], c & 0xff];
        }

        private static bool Between(int x, int start, int end)
        {
            return (x >= start) && (x < end);
        }

        /// <summary>
        /// Combine two characters together, if possible.
        /// </summary>
        /// <param name="a">First character to combine</param>
        /// <param name="b">Second character to combine</param>
        /// <param name="result">The combined character, if method returns true.  Otherwise, undefined.</param>
        /// <returns>True if combination occurred</returns>
        public static bool Combine(char a, char b, out char result)
        {

            // FIRST_START..FIRST_SINGLE_START:
            // FIRST_SINGLE_START..SECOND_START: look up a to see if b matches
            // SECOND_START..SECOND_SINGLE_START:
            // SECOND_SINGLE_START..: look up b to see if a matches

            int index_a = Index(a);
            // for stuff in this range, there is only one possible combination for the character
            // on the left
            if (Between(index_a, ComposeData.FIRST_SINGLE_START, ComposeData.SECOND_START))
            {
                int offset = index_a - ComposeData.FIRST_SINGLE_START;
                if (b == ComposeData.FirstSingle[offset, 0])
                {
                    result = ComposeData.FirstSingle[offset, 1];
                    return true;
                }
                else
                {
                    result = '\x0';
                    return false;
                }
            }

            int index_b = Index(b);
            // for this range, only one possible combination to the right.
            if (index_b >= ComposeData.SECOND_SINGLE_START)
            {
                int offset = index_b - ComposeData.SECOND_SINGLE_START;
                if (a == ComposeData.SecondSingle[offset,0])
                {
                    result = ComposeData.SecondSingle[offset, 1];
                    return true;
                }
                else
                {
                    result = '\x0';
                    return false;
                }
            }

            if (Between(index_a, ComposeData.FIRST_START, ComposeData.FIRST_SINGLE_START) &&
                Between(index_b, ComposeData.SECOND_START, ComposeData.SECOND_SINGLE_START))
            {
                char res = ComposeData.Array[index_a - ComposeData.FIRST_START, index_b - ComposeData.SECOND_START];

                if (res != '\x0')
                {
                    result = res;
                    return true;
                }
            }

            result = '\x0';
            return false;
        }
    }
}
