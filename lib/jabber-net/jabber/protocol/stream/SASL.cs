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
using System.Xml;

using bedrock.util;

namespace jabber.protocol.stream
{
    /// <summary>
    /// SASL mechanisms registered with IANA as of 5/16/2004.
    /// </summary>
    [SVN(@"$Id: SASL.cs 339 2007-03-02 19:40:49Z hildjj $")]
    [Flags]
    public enum MechanismType
    {
        /// <summary>
        ///
        /// </summary>
        NONE = 0,
        /// <summary>
        /// LIMITED  [RFC2222]   IESG &lt;iesg@ietf.org&gt;
        /// </summary>
        KERBEROS_V4 = (1 << 0),
        /// <summary>
        /// COMMON   [RFC2222]   IESG &lt;iesg@ietf.org&gt;
        /// </summary>
        GSSAPI = (1 << 1),
        /// <summary>
        /// OBSOLETE [RFC2444]   IESG &lt;iesg@ietf.org&gt;
        /// </summary>
        SKEY  = (1 << 2),
        /// <summary>
        /// COMMON   [RFC2222]   IESG &lt;iesg@ietf.org&gt;
        /// </summary>
        EXTERNAL = (1 << 3),
        /// <summary>
        /// LIMITED  [RFC2195]   IESG &lt;iesg@ietf.org&gt;
        /// </summary>
        CRAM_MD5 = (1 << 4),
        /// <summary>
        /// COMMON   [RFC2245]   IESG &lt;iesg@ietf.org&gt;
        /// </summary>
        ANONYMOUS = (1 << 5),
        /// <summary>
        /// COMMON   [RFC2444]   IESG &lt;iesg@ietf.org&gt;
        /// </summary>
        OTP = (1 << 6),
        /// <summary>
        /// LIMITED  [Leach]     Paul Leach &lt;paulle@microsoft.com&gt;
        /// </summary>
        GSS_SPNEGO = (1 << 7),
        /// <summary>
        /// COMMON   [RFC2595]   IESG &lt;iesg@ietf.org&gt;
        /// </summary>
        PLAIN = (1 << 8),
        /// <summary>
        /// COMMON   [RFC2808]   Magnus Nystrom &lt;magnus@rsasecurity.com&gt;
        /// </summary>
        SECURID = (1 << 9),
        /// <summary>
        /// LIMITED  [Leach]     Paul Leach &lt;paulle@microsoft.com&gt;
        /// </summary>
        NTLM = (1 << 10),
        /// <summary>
        /// LIMITED  [Gayman]    Mark G. Gayman &lt;mgayman@novell.com&gt;
        /// </summary>
        NMAS_LOGIN = (1 << 11),
        /// <summary>
        /// LIMITED  [Gayman]    Mark G. Gayman &lt;mgayman@novell.com&gt;
        /// </summary>
        NMAS_AUTHEN = (1 << 12),
        /// <summary>
        /// COMMON   [RFC2831]   IESG &lt;iesg@ietf.org&gt;
        /// </summary>
        DIGEST_MD5 = (1 << 13),
        /// <summary>
        /// [RFC3163]  robert.zuccherato@entrust.com
        /// </summary>
        ISO_9798_U_RSA_SHA1_ENC = (1 << 14),
        /// <summary>
        /// COMMON   [RFC3163]   robert.zuccherato@entrust.com
        /// </summary>
        ISO_9798_M_RSA_SHA1_ENC = (1 << 15),
        /// <summary>
        /// COMMON   [RFC3163]   robert.zuccherato@entrust.com
        /// </summary>
        ISO_9798_U_DSA_SHA1 = (1 << 16),
        /// <summary>
        /// COMMON   [RFC3163]   robert.zuccherato@entrust.com
        /// </summary>
        ISO_9798_M_DSA_SHA1 = (1 << 17),
        /// <summary>
        /// COMMON   [RFC3163]   robert.zuccherato@entrust.com
        /// </summary>
        ISO_9798_U_ECDSA_SHA1 = (1 << 18),
        /// <summary>
        /// COMMON   [RFC3163]   robert.zuccherato@entrust.com
        /// </summary>
        ISO_9798_M_ECDSA_SHA1 = (1 << 19),
        /// <summary>
        /// COMMON   [Josefsson] Simon Josefsson &lt;simon@josefsson.org&gt;
        /// </summary>
        KERBEROS_V5 = (1 << 20),
        /// <summary>
        /// LIMITED  [Brimhall]  Vince Brimhall &lt;vbrimhall@novell.com&gt;
        /// </summary>
        NMAS_SAMBA_AUTH = (1 << 21)
    }

    /// <summary>
    /// SASL mechanisms in stream features.
    /// </summary>
    [SVN(@"$Id: SASL.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Mechanisms : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Mechanisms(XmlDocument doc) :
            base("", new XmlQualifiedName("mechanisms", jabber.protocol.URI.SASL), doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Mechanisms(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The SASL mechanisms valid for this stream.
        /// </summary>
        /// <returns></returns>
        public Mechanism[] GetMechanisms()
        {
            XmlNodeList nl = GetElementsByTagName("mechanism", URI.SASL);
            Mechanism[] items = new Mechanism[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                items[i] = (Mechanism) n;
                i++;
            }
            return items;
        }

        /// <summary>
        /// A bitmap of all of the implemented types.
        /// </summary>
        public MechanismType Types
        {
            get
            {
                MechanismType ret = MechanismType.NONE;
                foreach (Mechanism m in GetMechanisms())
                {
                    ret |= m.MechanismType;
                }
                return ret;
            }
        }
    }

    /// <summary>
    /// SASL mechanisms in stream features.
    /// </summary>
    [SVN(@"$Id: SASL.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Mechanism : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Mechanism(XmlDocument doc) :
            base("", new XmlQualifiedName("mechanism", jabber.protocol.URI.SASL), doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Mechanism(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The IANA-registered SASL mechanism name.
        /// </summary>
        public string MechanismName
        {
            get { return this.InnerText; }
            set { this.InnerText = value; }
        }

        /// <summary>
        /// SASL mechanism, as an enum
        /// </summary>
        public MechanismType MechanismType
        {
            get { return GetMechanismType(MechanismName); }
            set { MechanismName = GetMechanism(value); }
        }

        /// <summary>
        /// The SASL mechanism, as an enum.
        /// </summary>
        public static MechanismType GetMechanismType(string name)
        {
            switch (name)
            {
                case "KERBEROS_V4":
                    return MechanismType.KERBEROS_V4;
                case "GSSAPI":
                    return MechanismType.GSSAPI;
                case "SKEY":
                    return MechanismType.SKEY;
                case "EXTERNAL":
                    return MechanismType.EXTERNAL;
                case "CRAM-MD5":
                    return MechanismType.CRAM_MD5;
                case "ANONYMOUS":
                    return MechanismType.ANONYMOUS;
                case "OTP":
                    return MechanismType.OTP;
                case "GSS-SPNEGO":
                    return MechanismType.GSS_SPNEGO;
                case "PLAIN":
                    return MechanismType.PLAIN;
                case "SECURID":
                    return MechanismType.SECURID;
                case "NTLM":
                    return MechanismType.NTLM;
                case "NMAS_LOGIN":
                    return MechanismType.NMAS_LOGIN;
                case "NMAS_AUTHEN":
                    return MechanismType.NMAS_AUTHEN;
                case "DIGEST-MD5":
                    return MechanismType.DIGEST_MD5;
                case "9798-U-RSA-SHA1-ENC":
                    return MechanismType.ISO_9798_U_RSA_SHA1_ENC;
                case "9798-M-RSA-SHA1-ENC":
                    return MechanismType.ISO_9798_M_RSA_SHA1_ENC;
                case "9798-U-DSA-SHA1":
                    return MechanismType.ISO_9798_U_DSA_SHA1;
                case "9798-M-DSA-SHA1":
                    return MechanismType.ISO_9798_M_DSA_SHA1;
                case "9798-U-ECDSA-SHA1":
                    return MechanismType.ISO_9798_U_ECDSA_SHA1;
                case "9798-M-ECDSA-SHA1":
                    return MechanismType.ISO_9798_M_ECDSA_SHA1;
                case "KERBEROS_V5":
                    return MechanismType.KERBEROS_V5;
                case "NMAS-SAMBA-AUTH":
                    return MechanismType.NMAS_SAMBA_AUTH;
                default:
                    return MechanismType.NONE;
            }
        }

        /// <summary>
        /// The SASL mechanism, as a string.
        /// </summary>
        public static string GetMechanism(MechanismType type)
        {
            switch (type)
            {
                case MechanismType.KERBEROS_V4:
                    return "KERBEROS_V4";
                case MechanismType.GSSAPI:
                    return "GSSAPI";
                case MechanismType.SKEY:
                    return "SKEY";
                case MechanismType.EXTERNAL:
                    return "EXTERNAL";
                case MechanismType.CRAM_MD5:
                    return "CRAM-MD5";
                case MechanismType.ANONYMOUS:
                    return "ANONYMOUS";
                case MechanismType.OTP:
                    return "OTP";
                case MechanismType.GSS_SPNEGO:
                    return "GSS-SPNEGO";
                case MechanismType.PLAIN:
                    return "PLAIN";
                case MechanismType.SECURID:
                    return "SECURID";
                case MechanismType.NTLM:
                    return "NTLM";
                case MechanismType.NMAS_LOGIN:
                    return "NMAS_LOGIN";
                case MechanismType.NMAS_AUTHEN:
                    return "NMAS_AUTHEN";
                case MechanismType.DIGEST_MD5:
                    return "DIGEST-MD5";
                case MechanismType.ISO_9798_U_RSA_SHA1_ENC:
                    return "9798-U-RSA-SHA1-ENC";
                case MechanismType.ISO_9798_M_RSA_SHA1_ENC:
                    return "9798-M-RSA-SHA1-ENC";
                case MechanismType.ISO_9798_U_DSA_SHA1:
                    return "9798-U-DSA-SHA1";
                case MechanismType.ISO_9798_M_DSA_SHA1:
                    return "9798-M-DSA-SHA1";
                case MechanismType.ISO_9798_U_ECDSA_SHA1:
                    return "9798-U-ECDSA-SHA1";
                case MechanismType.ISO_9798_M_ECDSA_SHA1:
                    return "9798-M-ECDSA-SHA1";
                case MechanismType.KERBEROS_V5:
                    return "KERBEROS_V5";
                case MechanismType.NMAS_SAMBA_AUTH:
                    return "NMAS-SAMBA-AUTH";
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Auth, Challenge, and Response.
    /// </summary>
    public abstract class Step : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Step(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The innards of the step.  If it is "=", it
        /// means an intentionally blank response, not one waiting for a challenge.
        /// </summary>
        public byte[] Bytes
        {
            get
            {
                string it = this.InnerText;
                if (it == "")
                    return null;
                if (it == "=")
                    return new byte[0];
                return Convert.FromBase64String(it);
            }
            set
            {
                if (value == null)
                    this.InnerText = "";
                else if (value.Length == 0)
                    this.InnerText = "=";
                else
                    this.InnerText = Convert.ToBase64String(value);
            }
        }
    }

    /// <summary>
    /// First phase of SASL auth.
    /// </summary>
    [SVN(@"$Id: SASL.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Auth : Step
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Auth(XmlDocument doc) :
            base("", new XmlQualifiedName("auth", jabber.protocol.URI.SASL), doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Auth(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The chosen mechanism
        /// </summary>
        public MechanismType Mechanism
        {
            get
            {
                string m = GetAttribute("mechanism");
                return jabber.protocol.stream.Mechanism.GetMechanismType(m);
            }
            set
            {
                string m = jabber.protocol.stream.Mechanism.GetMechanism(value);
                SetAttribute("mechanism", m);
            }
        }
    }
    /// <summary>
    /// Subsequent phases of SASL auth sent by server.
    /// </summary>
    [SVN(@"$Id: SASL.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Challenge : Step
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Challenge(XmlDocument doc) :
            base("", new XmlQualifiedName("challenge", jabber.protocol.URI.SASL), doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Challenge(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }
    }

    /// <summary>
    /// First phase of SASL auth.
    /// </summary>
    [SVN(@"$Id: SASL.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Response : Step
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Response(XmlDocument doc) :
            base("", new XmlQualifiedName("response", jabber.protocol.URI.SASL), doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Response(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }
    }

    /// <summary>
    /// SASL auth failed.
    /// </summary>
    [SVN(@"$Id: SASL.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class SASLFailure : Step
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public SASLFailure(XmlDocument doc) :
            base("", new XmlQualifiedName("failure", jabber.protocol.URI.SASL), doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public SASLFailure(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }
    }

    /// <summary>
    /// Abort SASL auth.
    /// </summary>
    [SVN(@"$Id: SASL.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Abort : Step
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Abort(XmlDocument doc) :
            base("", new XmlQualifiedName("abort", jabber.protocol.URI.SASL), doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Abort(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }
    }

    /// <summary>
    /// SASL auth successfult.
    /// </summary>
    [SVN(@"$Id: SASL.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Success : Step
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Success(XmlDocument doc) :
            base("", new XmlQualifiedName("success", jabber.protocol.URI.SASL), doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Success(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }
    }
}
