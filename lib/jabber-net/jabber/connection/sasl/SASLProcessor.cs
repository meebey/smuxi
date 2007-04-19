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
using System.Xml;
using System.Text;

using bedrock.util;
using jabber.protocol.stream;

namespace jabber.connection.sasl
{
    /// <summary>
    /// A SASL processor instance has been created.  Fill it with information, like USERNAME and PASSWORD.
    /// </summary>
    public delegate void SASLProcessorHandler(Object sender, SASLProcessor proc);

    /// <summary>
    /// Some sort of SASL error
    /// </summary>
    [SVN(@"$Id: SASLProcessor.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class SASLException : ApplicationException
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public SASLException(string message) : base(message){}

        /// <summary>
        ///
        /// </summary>
        public SASLException() : base(){}
    }

    /// <summary>
    /// Authentication failed.
    /// </summary>
    [SVN(@"$Id: SASLProcessor.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class AuthenticationFailedException : SASLException
    {
        /// <summary>
        ///
        /// </summary>
        public AuthenticationFailedException() : base()
        {}

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public AuthenticationFailedException(string message) : base(message)
        {}
    }

    /// <summary>
    /// A required directive wasn't supplied.
    /// </summary>
    [SVN(@"$Id: SASLProcessor.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class MissingDirectiveException : SASLException
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public MissingDirectiveException(string message) : base(message)
        {}
    }

    /// <summary>
    /// Server sent an invalid challenge
    /// </summary>
    [SVN(@"$Id: SASLProcessor.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class InvalidServerChallengeException : SASLException
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public InvalidServerChallengeException(string message) : base(message)
        {}
    }
    /// <summary>
    /// Summary description for SASLProcessor.
    /// </summary>
    [SVN(@"$Id: SASLProcessor.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public abstract class SASLProcessor
    {
        /// <summary>
        /// SASL username
        /// </summary>
        public const string USERNAME = "username";
        /// <summary>
        /// SASL password
        /// </summary>
        public const string PASSWORD = "password";


        /// <summary>
        ///
        /// </summary>
        private Hashtable m_directives = new Hashtable();

        /// <summary>
        ///
        /// </summary>
        public SASLProcessor()
        {
        }

        /// <summary>
        /// Create a new SASLProcessor, of the best type possible
        /// </summary>
        /// <param name="mt">The types the server implements</param>
        /// <param name="plaintextOK">Is it ok to select insecure types?</param>
        /// <returns></returns>
        public static SASLProcessor createProcessor(MechanismType mt, bool plaintextOK)
        {
            if ((mt & MechanismType.EXTERNAL) == MechanismType.EXTERNAL)
            {
                return new ExternalProcessor();
            }
            if ((mt & MechanismType.DIGEST_MD5) == MechanismType.DIGEST_MD5)
            {
                return new MD5Processor();
            }
            else if (plaintextOK && ((mt & MechanismType.PLAIN) == MechanismType.PLAIN))
            {
                return new PlainProcessor();
            }
            return null;
        }

        /// <summary>
        /// Data for performing SASL challenges and responses.
        /// </summary>
        public string this[string directive]
        {
            get { return (string) m_directives[directive]; }
            set { m_directives[directive] = value; }
        }

        /// <summary>
        /// Perform the next step
        /// </summary>
        /// <param name="s">Null if it's the initial response</param>
        /// <param name="doc">Document to create Steps in</param>
        /// <returns></returns>
        public abstract Step step(Step s, XmlDocument doc);

        /// <summary>
        /// byte array as a hex string, two chars per byte.
        /// </summary>
        /// <param name="buf">Byte array</param>
        /// <returns></returns>
        protected static string HexString(byte[] buf)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in buf)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
