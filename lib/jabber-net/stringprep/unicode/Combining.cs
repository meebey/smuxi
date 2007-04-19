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
    /// Combining classes for Unicode characters.
    /// </summary>
    public class Combining
    {
        /// <summary>
        /// What is the combining class for the given character?
        /// </summary>
        /// <param name="c">Character to look up</param>
        /// <returns>Combining class for this character</returns>
        public static int Class(char c)
        {
            int page = c >> 8;
            if (CombiningData.Pages[page] == 255)
                return 0;
            else
                return CombiningData.Classes[CombiningData.Pages[page], c & 0xff];
        }
    }
}
