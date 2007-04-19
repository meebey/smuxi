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

using System.Collections;
using System.Text;

namespace stringprep.steps
{
    /// <summary>
    /// A character that is forbidden by the current stringprep profile exists in the input.
    /// </summary>
    public class ProhibitedCharacterException : Exception
    {
        /// <summary>
        /// The character that was invalid.
        /// </summary>
        public char InvalidChar;

        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <param name="step">In which step did this occur?</param>
        /// <param name="c">The offending character</param>
        public ProhibitedCharacterException(ProfileStep step, char c) :
            base(string.Format("Step {0} prohibits string (character U+{1:x04}).", step.Name, (ushort) c))
        {
            InvalidChar = c;
        }
    }

    /// <summary>
    /// A stringprep profile step that checks for prohibited characters
    /// </summary>
    public class ProhibitStep : ProfileStep
    {
        private char[][] m_table = null;
        private ProhibitComparer m_comp = new ProhibitComparer();

        /*
        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <param name="tab">The prohibit table to be checked</param>
        /// <param name="name">The name of the step (for debugging purposes)</param>
        public ProhibitStep(string name) : base(name)
        {
        }
        */

        /// <summary>
        /// These characters are prohibited
        /// </summary>
        /// <param name="table"></param>
        /// <param name="name"></param>
        public ProhibitStep(char[][] table, string name): base(name)
        {
            m_table = table;
        }

        /// <summary>
        /// Does this step prohibit the given character?
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns>True if the character is prohibited</returns>
        protected bool Contains(char c)
        {
            return (Array.BinarySearch(m_table, c, m_comp) >= 0);
        }

        /// <summary>
        /// Check all of the characters for prohbition.
        /// </summary>
        /// <param name="s">String to check</param>
        /// <returns>If one of the characters is prohibited, returns the index of that character.
        /// If all are allowed, returns -1.</returns>
        public int FindStringInTable(StringBuilder s)
        {
            for (int j=0; j<s.Length; j++)
            {
                if (Contains(s[j]))
                {
                    return j;
                }
            }
            return -1;
        }

        /// <summary>
        /// Check for prohibited characters
        /// </summary>
        /// <param name="result">No modifications</param>
        /// <exception cref="ProhibitedCharacterException">Invalid character detected.</exception>
        public override void Prepare(System.Text.StringBuilder result)
        {
            int j = FindStringInTable(result);
            if (j >= 0)
                throw new ProhibitedCharacterException(this, result[j]);
        }

        private class ProhibitComparer : IComparer
        {
            #region IComparer Members

            public int Compare(object x, object y)
            {
                char[] bounds = (char[]) x;
                if (bounds[1] == '\x0000')
                    return bounds[0].CompareTo(y);

                char c = (char) y;
                if (c < bounds[0])
                    return 1;

                if (c > bounds[1])
                    return -1;

                return 0;
            }

            #endregion
        }

    }


}
