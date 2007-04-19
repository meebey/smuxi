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
using System.Diagnostics;

using bedrock.util;

namespace jabber
{
    /// <summary>
    /// An attempt was made to parse a badly-formatted JID.
    /// </summary>
    [SVN(@"$Id: JID.cs 338 2007-03-02 19:39:29Z hildjj $")]
    public class JIDFormatException : ApplicationException
    {
        /// <summary>
        /// Bad JID.
        /// </summary>
        /// <param name="badJid">The invalid JID</param>
        public JIDFormatException(string badJid) : base("Bad JID: (" + badJid + ")")
        {
        }

        /// <summary>
        /// Create a new exception instance.
        /// </summary>
        public JIDFormatException() : base()
        {
        }

        /// <summary>
        /// Create a new exception instance, wrapping another exception.
        /// </summary>
        /// <param name="badJid">Ill-formatted JID</param>
        /// <param name="e">Inner exception</param>
        public JIDFormatException(string badJid, Exception e) : base("Bad JID: (" + badJid + ")", e)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AsyncSocketConnectionException class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="ctx">The contextual information about the source or destination.</param>
        protected JIDFormatException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext ctx) :
            base(info, ctx)
        {
        }
    }

    /// <summary>
    /// Simple JID management.  There should be more, here, particularly
    /// with respect to interning.
    /// </summary>
    [SVN(@"$Id: JID.cs 338 2007-03-02 19:39:29Z hildjj $")]
    public class JID : IComparable
    {
#if !NO_STRINGPREP
        private static readonly stringprep.Profile s_nodeprep     = new stringprep.XmppNode();
        private static readonly stringprep.Profile s_nameprep     = new stringprep.Nameprep();
        private static readonly stringprep.Profile s_resourceprep = new stringprep.XmppResource();
#endif

        private string m_user     = null;
        private string m_server   = null;
        private string m_resource = null;
        private string m_JID      = null;

        /// <summary>
        /// Create a JID from a string.  This will parse and stringprep.
        /// </summary>
        /// <param name="jid">Jabber ID, in string form</param>
        public JID(string jid)
        {
            Debug.Assert(jid != null, "jid must be non-null");
            m_JID = jid;
            parse();
        }

        /// <summary>
        /// Build a new JID from the given components.
        /// </summary>
        /// <param name="user">The username</param>
        /// <param name="server">The server</param>
        /// <param name="resource">The current resource</param>
        public JID(string user, string server, string resource)
        {
            Debug.Assert(server != null, "server must be non-null");

#if !NO_STRINGPREP
            m_user     = (user == null) ? null : s_nodeprep.Prepare(user);
            m_server   = s_nameprep.Prepare(server);
            m_resource = (resource == null) ? null : s_resourceprep.Prepare(resource);
#else
            m_user     = (user == null) ? null : user.ToLower();
            m_server   = server.ToLower();
            m_resource = resource;
#endif
            m_JID      = build(m_user, m_server, m_resource);

        }

        private static string build(string user, string server, string resource)
        {
            Debug.Assert(server != null, "Server must be non-null");
            StringBuilder sb = new StringBuilder();
            if (user != null)
            {
                sb.Append(user);
                sb.Append("@");
            }
            sb.Append(server);
            if (resource != null)
            {
                sb.Append("/");
                sb.Append(resource);
            }
            return sb.ToString();
        }

        private void parse()
        {
            if (m_server != null)
                return; // already parsed

            string user = null;
            string server = null;
            string resource = null;

            int at = m_JID.IndexOf('@');
            int slash = m_JID.IndexOf('/');

            if (at == -1)
            {
                user = null;
                if (slash == -1)
                {
                    server = m_JID;
                    resource = null;
                }
                else
                {
                    server = m_JID.Substring(0, slash);
                    resource = m_JID.Substring(slash+1);
                }
            }
            else
            {
                if (slash == -1)
                {
                    user = m_JID.Substring(0, at);
                    server = m_JID.Substring(at + 1);
                }
                else
                {
                    if (at < slash)
                    { // normal case
                        user = m_JID.Substring(0, at);
                        server = m_JID.Substring(at+1, slash-at-1);
                        resource = m_JID.Substring(slash+1);
                    }
                    else
                    { // @ in a resource, with no user.  bastards.
                        user = null;
                        server = m_JID.Substring(0, slash);
                        resource = m_JID.Substring(slash+1);
                    }
                }
            }
            if (user != null)
            {
                if (user.IndexOf('@') != -1) throw new JIDFormatException(m_JID);
                if (user.IndexOf('/') != -1) throw new JIDFormatException(m_JID);
            }

            if ((server == null) || (server.Length == 0)) throw new JIDFormatException(m_JID);
            if (server.IndexOf('@') != -1) throw new JIDFormatException(m_JID);
            if (server.IndexOf('/') != -1) throw new JIDFormatException(m_JID);
            if ((resource != null) && (resource.Length == 0)) // null is ok, but "" is not.
                throw new JIDFormatException(m_JID);

#if !NO_STRINGPREP
            m_user = (user == null) ? null : s_nodeprep.Prepare(user);
            m_server = s_nameprep.Prepare(server);
            m_resource = (resource == null) ? null : s_resourceprep.Prepare(resource);
#else
            m_user = (user == null) ? null : user.ToLower();
            m_server = server.ToLower();
            m_resource = resource;
#endif
            // Make the case right, for fast equality comparisons
            m_JID = build(m_user, m_server, m_resource);
        }

        /// <summary>
        /// Hash the string version of the JID.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return m_JID.GetHashCode();
        }

        /// <summary>
        /// Return the string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_JID;
        }

        /// <summary>
        /// Equality of string representations.
        /// </summary>
        /// <param name="other">JID or string to compare against.</param>
        /// <returns></returns>
        public override bool Equals(object other)
        {
            if (other == null)
                return false;
            if (other is string)
                return m_JID.Equals(other);
            if (! (other is JID))
                return false;

            return m_JID.Equals(((JID)other).m_JID);
        }

        /// <summary>
        /// Two jids are equal?
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static bool operator==(JID one, JID two)
        {
            if ((object)one == null)
                return ((object)two == null);
            return one.Equals(two);
        }

        /// <summary>
        /// Is this string equal to that jid?
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static bool operator==(string one, JID two)
        {
            if ((object)two == null)
                return ((object)one == null);
            return two.Equals(one);
        }

        /// <summary>
        /// Is this string not equal to that jid?
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static bool operator!=(string one, JID two)
        {
            if ((object)two == null)
                return ((object)one != null);
            return !two.Equals(one);
        }

        /// <summary>
        /// Two jids are unequal?
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static bool operator!=(JID one, JID two)
        {
            if ((object)one == null)
                return ((object)two != null);
            return !one.Equals(two);
        }

        /// <summary>
        /// Convert string to JID implicitly (no cast needed).
        /// </summary>
        /// <param name="jid"></param>
        /// <returns></returns>
        public static implicit operator JID(string jid)
        {
            return new JID(jid);
        }

        /// <summary>
        /// Convert string to JID implicitly (no cast needed).
        /// </summary>
        /// <param name="jid"></param>
        /// <returns></returns>
        public static implicit operator string(JID jid)
        {
            return jid.m_JID;
        }

        /// <summary>
        /// Less than
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator<(JID left, JID right)
        {
            return left.CompareTo(right) == -1;
        }

        /// <summary>
        /// Greater than
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator>(JID left, JID right)
        {
            return left.CompareTo(right) == 1;
        }

        /// <summary>
        /// Less than or equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator<=(JID left, JID right)
        {
            return left.CompareTo(right) != 1;
        }

        /// <summary>
        /// Greater than or equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator>=(JID left, JID right)
        {
            return left.CompareTo(right) != -1;
        }

        /// <summary>
        /// Just the user field.  NULL if none.
        /// </summary>
        public string User
        {
            get
            {
                parse();
                return m_user;
            }
            set
            {
                parse();
                m_user = value;
                m_JID = build(m_user, m_server, m_resource);
            }
        }

        /// <summary>
        /// Just the server field.
        /// </summary>
        public string Server
        {
            get
            {
                parse();
                return m_server;
            }
            set
            {
                parse();
                m_server = value;
                m_JID = build(m_user, m_server, m_resource);
            }
        }

        /// <summary>
        /// Just the resource field.  NULL if none.
        /// </summary>
        public string Resource
        {
            get
            {
                parse();
                return m_resource;
            }
            set
            {
                parse();
                m_resource = value;
                m_JID = build(m_user, m_server, m_resource);
            }
        }

        /// <summary>
        /// User@host.
        /// </summary>
        public string Bare
        {
            get
            {
                parse();
                return build(m_user, m_server, null);
            }
        }

        #region Implementation of IComparable
        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the comparands. The return value has these meanings:
        /// Less than zero This instance is less than obj.
        /// Zero This instance is equal to obj.
        /// Greater than zero This instance is greater than obj.
        /// </returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;
            if (obj == (object)this)
                return 0;

            JID oj = obj as JID;
            if (oj == null)
                throw new ArgumentException("Comparison of JID to non-JID", "obj");

            // hm.  How tricky to get?
            // It could be that sorting by domain first is correct...
            //return this.m_JID.CompareTo(oj.m_JID);
            this.parse();
            oj.parse();

            int c = this.m_server.ToLower().CompareTo(oj.m_server.ToLower());
            if (c != 0) return c;

            if (this.m_user == null)
            {
                if (oj.m_user != null)
                    return -1;
            }
            else
            {
                if (oj.m_user == null)
                    return 1;

                c = this.m_user.ToLower().CompareTo(oj.m_user.ToLower());
                if (c != 0) return c;
            }

            if (this.m_resource == null)
            {
                return (oj.m_resource == null) ? 0 : -1;
            }
            return this.m_resource.CompareTo(oj.m_resource);
        }
        #endregion
    }
}
