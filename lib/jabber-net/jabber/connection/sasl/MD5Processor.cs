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
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Security.Cryptography;
using System.IO;
using System.Globalization;
using System.Xml;

using bedrock.util;
using jabber.protocol.stream;

namespace jabber.connection.sasl
{
    /// <summary>
    /// RFC2831 DIGEST-MD5 SASL mechanism
    /// </summary>
    [SVN(@"$Id: MD5Processor.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class MD5Processor : SASLProcessor
    {
        /// <summary>
        /// Private members
        /// </summary>
        private string  m_response;
        private string  m_realm;
        private string  m_username;
        private string  m_password;
        private string  m_nonce;
        private string  m_cnonce;
        private int     m_nc;
        private string  m_ncString;
        private string  m_qop;
        private string  m_charset;
        private string  m_algorithm;
        private string  m_authzid;


        /// <summary>
        /// DIGEST-MD5 Realm
        /// </summary>
        public const string REALM = "realm";
        /// <summary>
        /// DIGEST-MD5 nonce
        /// </summary>
        public const string NONCE = "nonce";
        /// <summary>
        /// DIGEST-MD5 qop
        /// </summary>
        public const string QOP = "qop";
        /// <summary>
        /// DIGEST-MD5 charset
        /// </summary>
        public const string CHARSET = "charset";
        /// <summary>
        /// DIGEST-MD5 algorithm
        /// </summary>
        public const string ALGORITHM = "algorithm";
        /// <summary>
        /// DIGEST-MD5 authorization id
        /// </summary>
        public const string AUTHZID = "authzid";

        /// <summary>
        /// The directives that are required to be set on the SASLProcessor in OnSASLStart
        /// </summary>
        public static readonly string[] s_requiredDirectives = {USERNAME, PASSWORD};

        private static readonly MD5CryptoServiceProvider MD5 = new MD5CryptoServiceProvider();
        private static readonly System.Text.Encoding     ENC = System.Text.Encoding.UTF8;

        /// <summary>
        ///
        /// </summary>
        public MD5Processor() : base()
        {
            m_nc = 0;
        }

        /// <summary>
        /// Process the next DIGEST-MD5 step.
        /// </summary>
        /// <param name="s">The previous step.  Null for the first step</param>
        /// <param name="doc">Document to create next step in.</param>
        /// <returns></returns>
        /// <exception cref="AuthenticationFailedException">Thrown if authentication fails</exception>
        public override Step step(Step s, XmlDocument doc)
        {
            Step resp = null;

            if (s == null)
            { // first step
                Auth a = new Auth(doc);
                a.Mechanism = MechanismType.DIGEST_MD5;
                return a;
            }

            Debug.Assert(s is Challenge);
            String decodedChallenge = ENC.GetString(s.Bytes);
            populateDirectives(decodedChallenge);
            validateStartDirectives();


            resp = new Response(doc);
            if (this["rspauth"] == null)  // we haven't authenticated yet
            {
                generateResponseString();
                resp.Bytes = generateResponse();
            }
            else // we have authenticated
            {
                // make sure what is in rspauth is correct
                if (!validateResponseAuth())
                {
                    throw new AuthenticationFailedException();
                }
            }
            return resp;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="decoded"></param>
        private void populateDirectives(string decoded)
        {
            string key = "";
            string data = "";
            string pDelimStr = ",";

            char[] pDelimiter = pDelimStr.ToCharArray();

            string[] split = null;
            split = decoded.Split(pDelimiter);
            foreach(string name_value in split)
            {
                parsePair(name_value, key, data);
            }
        }

        private void parsePair(string name_value, string key, string data)
        {
            int index = name_value.IndexOf("=");
            key = name_value.Substring(0,index);
            int start = index+1;
            int end = name_value.Length - start;
            if (name_value[start] == '\"')
            {
                start++;
                end = end-2;
            }
            data = name_value.Substring(start, end);
            this[key] = data;
        }

        /// <summary>
        ///
        /// </summary>
        ///
        private void validateStartDirectives()
        {
            Object n;
            ASCIIEncoding AE = new ASCIIEncoding();
            string temp;
            if ( (n = this[USERNAME]) != null)
            {
                temp = n.ToString();
                m_username = ENC.GetString(AE.GetBytes(temp));
            }
            else
            {
                throw new MissingDirectiveException("Missing SASL username directive");
            }
            if ( (n = this[PASSWORD]) != null)
            {
                temp = n.ToString();
                m_password = ENC.GetString(AE.GetBytes(temp));
            }
            else
            {
                throw new MissingDirectiveException("Missing SASL password directive");
            }

            if ( (n = this[REALM]) != null)
            {
                m_realm = n.ToString();
            }
            else
            {
                throw new InvalidServerChallengeException("Missing SASL realm");
            }
            if ( (n = this[NONCE]) != null)
            {
                m_nonce = n.ToString();
            }
            else
            {
                throw new InvalidServerChallengeException("Missing nonce directive");
            }
            if ( (n = this[QOP]) != null)
            {
                m_qop = n.ToString();
            }
            else
            {
                throw new InvalidServerChallengeException("Missing qop directive");
            }
            if ( (n = this[CHARSET]) != null)
            {
                m_charset = n.ToString();
            }
            if ( (n = this[ALGORITHM]) != null)
            {
                m_algorithm = n.ToString();
            }
            if ( (n = this[AUTHZID]) != null)
            {
                m_authzid = n.ToString();
            }
        }

        /// <summary>
        /// Generates the entrire response to send to the server
        /// </summary>
        /// <returns></returns>
        private byte[] generateResponse()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("username=\"");
            sb.Append(m_username);
            sb.Append("\",");
            sb.Append("realm=\"");
            sb.Append(m_realm);
            sb.Append("\",");
            sb.Append("nonce=\"");
            sb.Append(m_nonce);
            sb.Append("\",");
            sb.Append("cnonce=\"");
            sb.Append(m_cnonce);
            sb.Append("\",");
            sb.Append("nc=");
            sb.Append(m_ncString);
            sb.Append(",");
            sb.Append("qop=");
            sb.Append(m_qop);
            sb.Append(",");
            sb.Append("digest-uri=\"");
            sb.Append("xmpp/");
            sb.Append(m_realm);
            sb.Append("\",");
            sb.Append("response=");
            sb.Append(m_response);
            sb.Append(",");
            sb.Append("charset=");
            sb.Append(m_charset);
            return ENC.GetBytes(sb.ToString());
        }
        /// <summary>
        /// Generates the MD5 hash that goes in the response attribute of the
        /// response sent to the server.
        /// </summary>
        private void generateResponseString()
        {
            // here is where we do the md5 foo
            ASCIIEncoding AE = new ASCIIEncoding();
            byte[] H1, H2, H3, temp;
            string A1, A2, A3, uri, p1, p2;

            uri = "xmpp/" + m_realm;
            Random r = new Random();
            int v = r.Next(1024);

            StringBuilder sb = new StringBuilder();
            sb.Append(v.ToString());
            sb.Append(":");
            sb.Append(m_username);
            sb.Append(":");
            sb.Append(m_password);

            m_cnonce = HexString(AE.GetBytes(sb.ToString())).ToLower();

            m_nc++;
            m_ncString = m_nc.ToString().PadLeft(8,'0');

            sb.Remove(0,sb.Length);
            sb.Append(m_username);
            sb.Append(":");
            sb.Append(m_realm);
            sb.Append(":");
            sb.Append(m_password);
            H1 = MD5.ComputeHash(AE.GetBytes(sb.ToString()));

            sb.Remove(0, sb.Length);
            sb.Append(":");
            sb.Append(m_nonce);
            sb.Append(":");
            sb.Append(m_cnonce);

            if (m_authzid != null)
            {
                sb.Append(":");
                sb.Append(m_authzid);
            }
            A1 = sb.ToString();

            MemoryStream ms = new MemoryStream();
            ms.Write(H1,0,16);
            temp = AE.GetBytes(A1);
            ms.Write(temp,0,temp.Length);
            ms.Seek(0,System.IO.SeekOrigin.Begin);
            H1 = MD5.ComputeHash(ms);

            sb.Remove(0,sb.Length);
            sb.Append("AUTHENTICATE:");
            sb.Append(uri);
            if (m_qop.CompareTo("auth") != 0)
            {
                sb.Append(":00000000000000000000000000000000");
            }
            A2 = sb.ToString();
            H2 = AE.GetBytes(A2);
            H2 = MD5.ComputeHash(H2);

            // create p1 and p2 as the hex representation of H1 and H2
            p1 = HexString(H1).ToLower();
            p2 = HexString(H2).ToLower();

            sb.Remove(0, sb.Length);
            sb.Append(p1);
            sb.Append(":");
            sb.Append(m_nonce);
            sb.Append(":");
            sb.Append(m_ncString);
            sb.Append(":");
            sb.Append(m_cnonce);
            sb.Append(":");
            sb.Append(m_qop);
            sb.Append(":");
            sb.Append(p2);

            A3 = sb.ToString();
            H3 = MD5.ComputeHash(AE.GetBytes(A3));
            m_response = HexString(H3).ToLower();
        }

        private bool validateResponseAuth()
        {
            //TODO:  We need to validate the respauth's value by going through the responseString
            //function again
            return true;
        }
    }
}
