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
    /// Base class for other exceptions
    /// </summary>
    [SVN(@"$Id: Exceptions.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class TokenException : System.Exception
    {
    }

    /// <summary>
    /// An empty token was detected.  This only happens with a buffer of length 0 is passed in
    /// to the parser.
    /// </summary>
    [SVN(@"$Id: Exceptions.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class EmptyTokenException : TokenException
    {
    }

    /// <summary>
    /// End of prolog.
    /// </summary>
    [SVN(@"$Id: Exceptions.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class EndOfPrologException : TokenException
    {
    }
    /**
     * Thrown to indicate that the byte subarray being tokenized is a legal XML
     * token, but that subsequent bytes in the same entity could be part of
     * the token.  For example, <code>Encoding.tokenizeProlog</code>
     * would throw this if the byte subarray consists of a legal XML name.
     * @version $Revision: 340 $ $Date: 2007-03-02 21:35:59 +0100 (Fri, 02 Mar 2007) $
     */
    [SVN(@"$Id: Exceptions.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class ExtensibleTokenException : TokenException
    {
        private TOK tokType;

        /// <summary>
        ///
        /// </summary>
        /// <param name="tokType"></param>
        public ExtensibleTokenException(TOK tokType)
        {
            this.tokType = tokType;
        }

        /**
         * Returns the type of token in the byte subarrary.
         */
        public TOK TokenType
        {
            get { return tokType; }
        }
    }

    /// <summary>
    /// Several kinds of token problems.
    /// </summary>
    [SVN(@"$Id: Exceptions.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class InvalidTokenException : TokenException
    {
        private int offset;
        private byte type;

        /// <summary>
        /// An illegal character
        /// </summary>
        public const byte ILLEGAL_CHAR = 0;
        /// <summary>
        /// Doc prefix wasn't XML
        /// </summary>
        public const byte XML_TARGET = 1;
        /// <summary>
        /// More than one attribute with the same name on the same element
        /// </summary>
        public const byte DUPLICATE_ATTRIBUTE = 2;

        /// <summary>
        /// Some other type of bad token detected
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="type"></param>
        public InvalidTokenException(int offset, byte type)
        {
            this.offset = offset;
            this.type = type;
        }

        /// <summary>
        /// Illegal character detected
        /// </summary>
        /// <param name="offset"></param>
        public InvalidTokenException(int offset)
        {
            this.offset = offset;
            this.type = ILLEGAL_CHAR;
        }

        /// <summary>
        /// Offset into the buffer where the problem ocurred.
        /// </summary>
        public int Offset
        {
            get { return this.offset; }
        }

        /// <summary>
        /// Type of exception
        /// </summary>
        public int Type
        {
            get { return this.type; }
        }
    }

    /**
     * Thrown to indicate that the subarray being tokenized is not the
     * complete encoding of one or more characters, but might be if
     * more bytes were added.
     * @version $Revision: 340 $ $Date: 2007-03-02 21:35:59 +0100 (Fri, 02 Mar 2007) $
     */
    [SVN(@"$Id: Exceptions.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class PartialCharException : PartialTokenException
    {
        private int leadByteIndex;

        /// <summary>
        ///
        /// </summary>
        /// <param name="leadByteIndex"></param>
        public PartialCharException(int leadByteIndex)
        {
            this.leadByteIndex = leadByteIndex;
        }

        /**
         * Returns the index of the first byte that is not part of the complete
         * encoding of a character.
         */
        public int LeadByteIndex
        {
            get { return leadByteIndex; }
        }
    }

    /// <summary>
    /// A partial token was received.  Try again, after you add more bytes to the buffer.
    /// </summary>
    [SVN(@"$Id: Exceptions.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class PartialTokenException : TokenException
    {
    }
}
