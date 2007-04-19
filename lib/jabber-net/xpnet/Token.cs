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
 *
 * xpnet is a deriviative of James Clark's XP.  See copying.txt for more info.
 * --------------------------------------------------------------------------*/
namespace xpnet
{
    using bedrock.util;

    /// <summary>
    /// A token that was parsed.
    /// </summary>
    [SVN(@"$Id: Token.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class Token
    {
        private int tokenEnd = -1;
        private int nameEnd = -1;
        private char refChar1 = (char)0;
        private char refChar2 = (char)0;

        /// <summary>
        /// The end of the current token, in relation to the beginning of the buffer.
        /// </summary>
        public int TokenEnd
        {
            get {return tokenEnd;}
            set {tokenEnd = value; }
        }

        /// <summary>
        /// The end of the current token's name, in relation to the beginning of the buffer.
        /// </summary>
        public int NameEnd
        {
            get {return nameEnd;}
            set {nameEnd = value;}
        }

        /// <summary>
        /// The parsed-out character. &amp; for &amp;amp;
        /// </summary>
        public char RefChar1
        {
            get {return refChar1;}
            set {refChar1 = value; }
        }
        /// <summary>
        /// The second of two parsed-out characters.  TODO: find example.
        /// </summary>
        public char RefChar2
        {
            get {return refChar2;}
            set {refChar2 = value; }
        }

        /*
        public void getRefCharPair(char[] ch, int off) {
            ch[off] = refChar1;
            ch[off + 1] = refChar2;
        }
        */
    }
}
