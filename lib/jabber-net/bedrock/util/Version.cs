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
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
namespace bedrock.util
{
    /// <summary>
    /// Make source code versions available at runtime.  Use the appropriate
    /// subclass for your CM system.
    /// </summary>
    /// <see cref="StarTeamAttribute"/>
    /// <see cref="SourceSafeAttribute"/>
    /// <see cref="RCSAttribute"/>
    //    [SVN(@"$Id: Version.cs 339 2007-03-02 19:40:49Z hildjj $")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct,
                    AllowMultiple = false,
                    Inherited     = false)]
    public abstract class SourceVersionAttribute : Attribute
    {
        /// <summary>
        /// The entire header
        /// </summary>
        protected string   m_header  = null;
        /// <summary>
        /// The directory it's stored in
        /// </summary>
        protected string   m_archive = null;
        /// <summary>
        /// Last check-in author
        /// </summary>
        protected string   m_author  = null;
        /// <summary>
        /// Last check-in version
        /// </summary>
        protected string   m_version = null;
        /// <summary>
        /// Last check-in date
        /// </summary>
        protected DateTime m_date    = DateTime.MinValue;
        /// <summary>
        /// Have we parsed the header, yet?
        /// </summary>
        private   bool     m_parsed  = false;
        // TODO: replace all of the subclasses with a single, uber-regex.
        private static readonly Regex REGEX =
            new Regex(@"^(\$(?<field>[a-z]+): *(?<value>.+) *\$)|( *(?<value>.+) *)$",
                      RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Construct the attribute.  Parsing is delayed until needed.
        /// </summary>
        /// <param name="header">the Header keyword for your CM system.
        /// Usually &#36;Header&#36;</param>
        public SourceVersionAttribute(string header)
        {
            m_header = header;
        }
        /// <summary>
        /// You could use this one, and pass the keywords in individually.
        /// </summary>
        public SourceVersionAttribute()
        {

        }
        /// <summary>
        /// Give back the header string.
        /// </summary>
        public override string ToString()
        {
            return m_header;
        }
        /// <summary>
        /// Have we parsed yet?
        /// </summary>
        protected void CheckParse()
        {
            if (m_parsed)
            {
                return;
            }
            lock(this)
            {
                if (m_parsed)
                {
                    return;
                }
                Parse();
                m_parsed = true;
            }
        }
        /// <summary>
        /// We have done a parse, now
        /// </summary>
        protected void SetParse()
        {
            if (!m_parsed)
            {
                lock(this)
                {
                    m_parsed = true;
                }
            }
        }
        /// <summary>
        /// Parse data into internal fields
        /// </summary>
        protected abstract void Parse();
        /// <summary>
        /// Do a regex match on src
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        protected string GetField(string src)
        {
            Match m = REGEX.Match(src);
            if (!m.Success)
            {
                throw new FormatException("Bad header format: " + src + " != " + REGEX.ToString());
            }
            if (!m.Groups["value"].Success)
            {
                throw new FormatException("Value not found in: " + src);
            }
            return m.Groups["value"].ToString();
        }
        /// <summary>
        /// The last checked-in version
        /// </summary>
        public string Revision
        {
            get
            {
                CheckParse();
                return m_version;
            }
            set
            {
                SetParse();
                m_version = GetField(value);
            }
        }
        /// <summary>
        /// The last checked-in version, in perhaps more useful format
        /// </summary>
        public Version Version
        {
            get
            {
                CheckParse();
                if (m_version == null)
                {
                    return null;
                }
                if (m_version.IndexOf('.') == -1)
                {
                    return new Version(1, Int32.Parse(m_version));
                }
                return new Version(m_version);
            }
        }
        /// <summary>
        /// Retrive the binary date/time of last check-in
        /// </summary>
        public DateTime Date
        {
            get
            {
                CheckParse();
                return m_date;
            }
            set
            {
                SetParse();
                m_date = value;
            }
        }
        /// <summary>
        /// Retrieve the string representation of the date of last check-in.
        /// </summary>
        public string DateString
        {
            get
            {
                CheckParse();
                return m_date.ToString();
            }
            set
            {
                SetParse();
                m_date = DateTime.Parse(GetField(value));
            }
        }
        /// <summary>
        /// Retrive the name of the last person to check in
        /// </summary>
        public string Author
        {
            get
            {
                CheckParse();
                return m_author;
            }
            set
            {
                SetParse();
                m_author = GetField(value);
            }
        }
        /// <summary>
        /// Retrieve the archive name from the header
        /// </summary>
        public string Archive
        {
            get
            {
                CheckParse();
                return m_archive;
            }
            set
            {
                SetParse();
                m_archive = GetField(value);
            }
        }
        /// <summary>
        /// Get the version information for the given type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static SourceVersionAttribute GetVersion(Type t)
        {
            object[] sta = t.GetCustomAttributes(typeof(SourceVersionAttribute), true);
            if (sta.Length == 0)
            {
                // throw exception?  Null seems nicer.
                return null;
            }
            return (SourceVersionAttribute) sta[0];
        }
        /// <summary>
        /// Get the version information for the class of the given object.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static SourceVersionAttribute GetVersion(object o)
        {
            // Well, someone used it wrong, but who am I to complain?
            if (o is Type)
            {
                return GetVersion((Type) o);
            }
            return GetVersion(o.GetType());
        }
        /// <summary>
        /// Get all of the versioned classes currently in the working set.
        /// </summary>
        /// <returns></returns>
        public static SourceVersionCollection GetVersion()
        {
            SourceVersionCollection tv = new SourceVersionCollection();
            Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
            SourceVersionAttribute sta;
            foreach (Assembly a in assems)
            {
                Type[] ts = a.GetTypes();
                foreach (Type t in ts)
                {
                    sta = GetVersion(t);
                    if (sta != null)
                    {
                        tv.Add(t.FullName, sta);
                    }
                }
            }
            return tv;
        }
    }
    /// <summary>
    /// Make StarTeam versoning available at run-time.
    ///
    /// </summary>
    /// <example>
    /// [StarTeam(@"&#36;Header&#36;")]
    /// public class foo {}
    ///
    /// SourceVersionAttribute sta = SourceVersionAttribute.GetVersion(typeof(foo));
    /// </example>
    [SVN(@"$Id: Version.cs 339 2007-03-02 19:40:49Z hildjj $")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct,
                    AllowMultiple=false, Inherited=false)]
    public class StarTeamAttribute : SourceVersionAttribute
    {
        // Dammit gumby.  Don't mess up my regex.
        private static readonly Regex REGEX =
            new Regex(@"^\$" + @"Header(: (?<archive>[^,]+), (?<version>[0-9.]+), (?<date>[^,]+), (?<author>[^$]+))?" + @"\$$");
        /// <summary>
        /// Normal usage
        /// </summary>
        /// <param name="header"></param>
        public StarTeamAttribute(string header) : base(header)
        {
        }
        /// <summary>
        /// Not useful
        /// </summary>
        public StarTeamAttribute() : base()
        {
        }

        /// <summary>
        /// Return normalized header
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string s = base.ToString();
            if (s != null)
            {
                return s;
            }

            return String.Format("{0}Header: {1}, {2}, {3:MM/dd/yyyy h:mm:ss tt}, {4}{5}",
                                 new object[] {"$", m_archive, m_version, m_date, m_author, "$"});
        }
        /// <summary>
        /// Parse the header
        /// </summary>
        protected override void Parse()
        {
            Match m = REGEX.Match(m_header);
            if (!m.Success)
            {
                throw new FormatException("Bad header format: " + m_header + " != " + REGEX.ToString());
            }
            if (m.Groups["archive"].Success)
            {
                m_archive = m.Groups["archive"].ToString();
                m_version = m.Groups["version"].ToString();
                m_date    = DateTime.Parse(m.Groups["date"].ToString());
                m_author  = m.Groups["author"].ToString();
            }
        }
    }
    /// <summary>
    /// Version control attribute for RCS and CVS.
    /// </summary>
    [SVN(@"$Id: Version.cs 339 2007-03-02 19:40:49Z hildjj $")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct,
                    AllowMultiple=false, Inherited=false)]
    public class RCSAttribute : SourceVersionAttribute
    {
        // Header: /u1/html/cvsroot/www.cyclic.com/RCS-html/info-ref.html,v 1.1 1999/04/14 19:04:02 kingdon Exp
        private static readonly Regex REGEX =
            new Regex(@"^\$" + @"Header(: +(?<archive>[^ ]+) +(?<version>[0-9.]+) +(?<date>[0-9/]+ [0-9:]+) +(?<author>[^ ]+) +(?<state>[^ ]+) *)?" + @"\$$");
        private string m_state = null;
        /// <summary>
        /// The most common.  Pass in @"$ Header $" (without the spaces).
        /// </summary>
        /// <param name="header"></param>
        public RCSAttribute(string header) : base(header)
        {
        }
        /// <summary>
        /// Null constructor.  This is rarely right.
        /// </summary>
        public RCSAttribute() : base()
        {
        }
        /// <summary>
        /// Parse the header string.
        /// </summary>
        protected override void Parse()
        {
            Match m = REGEX.Match(m_header);
            if (!m.Success)
            {
                throw new FormatException("Bad header format: " + m_header + " != " + REGEX.ToString());
            }
            if (m.Groups["archive"].Success)
            {
                m_archive = m.Groups["archive"].ToString();
                m_version = m.Groups["version"].ToString();
                m_date    = DateTime.Parse(m.Groups["date"].ToString());
                m_author  = m.Groups["author"].ToString();
                m_state   = m.Groups["state"].ToString();
            }
        }
        /// <summary>
        /// Hm.  Wish I remembered what this was for.  :)
        /// </summary>
        public string State
        {
            get
            {
                CheckParse();
                return m_state;
            }
            set
            {
                SetParse();
                m_state  = GetField(value);
            }
        }
    }
    /// <summary>
    /// Version control attribute for SourceSafe.
    /// I don't use this any more, so someone tell me if it breaks with
    /// some new release.
    /// </summary>
    [SVN(@"$Id: Version.cs 339 2007-03-02 19:40:49Z hildjj $")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct,
                    AllowMultiple=false, Inherited=false)]
    public class SourceSafeAttribute : SourceVersionAttribute
    {
        // Header: /t.cs 1     2/14/01 3:57p Hildebzj
        private static readonly Regex REGEX =
            new Regex(@"^\$" + @"Header(: +(?<archive>[^ ]+) +(?<version>[0-9.]+) +(?<date>[0-9/]+ [0-9:]+)(?<ampm>[ap]) +(?<author>[^ ]+) *)?" + @"\$$");
        //private string m_state = null;
        /// <summary>
        /// The normal use.  Pass in @"$ Header $" (without the spaces).
        /// </summary>
        /// <param name="header"></param>
        public SourceSafeAttribute(string header) : base(header)
        {
        }
        /// <summary>
        /// Not usually useful.
        /// </summary>
        public SourceSafeAttribute() : base()
        {
        }
        /// <summary>
        /// Parse the header.
        /// </summary>
        protected override void Parse()
        {
            Match m = REGEX.Match(m_header);
            if (!m.Success)
            {
                throw new FormatException("Bad header format: " + m_header + " != " + REGEX.ToString());
            }
            if (m.Groups["archive"].Success)
            {
                m_archive = m.Groups["archive"].ToString();
                m_version = m.Groups["version"].ToString();
                m_date    = DateTime.Parse(m.Groups["date"].ToString());
                if (m.Groups["ampm"].ToString() == "p")
                {
                    m_date = m_date.AddHours(12);
                }
                m_author  = m.Groups["author"].ToString();
            }
        }
    }
    /// <summary>
    /// A collection of SourceVersionAttributes, so that we can
    /// return a list of all of the versioned classes in the
    /// current working set.
    /// </summary>
    [SVN(@"$Id: Version.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class SourceVersionCollection : NameObjectCollectionBase
    {
        /// <summary>
        /// Add an attribute to the list
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Add(string type, SourceVersionAttribute value)
        {
            BaseAdd(type, value);
        }
        /// <summary>
        /// Remove all of the attributes from the list
        /// </summary>
        public void Clear()
        {
            BaseClear();
        }
        /// <summary>
        /// Get the index'th attribute
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public SourceVersionAttribute Get(int index)
        {
            return (SourceVersionAttribute) BaseGet(index);
        }
        /// <summary>
        /// Get the attribute associated with a give type name
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public SourceVersionAttribute Get(string type)
        {
            return (SourceVersionAttribute) BaseGet(type);
        }
        /// <summary>
        /// Set the attribute associated with a given type name
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void Set(string type, SourceVersionAttribute value)
        {
            BaseSet(type, value);
        }
        /// <summary>
        /// Set the index'th attribute
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void Set(int index, SourceVersionAttribute value)
        {
            BaseSet(index, value);
        }
        /// <summary>
        /// Remove the index'th attribute
        /// </summary>
        /// <param name="index"></param>
        public void Remove(int index)
        {
            BaseRemoveAt(index);
        }
        /// <summary>
        /// Remove the attribute associated with the given type name
        /// </summary>
        /// <param name="type"></param>
        public void Remove(string type)
        {
            BaseRemove(type);
        }
        /// <summary>
        /// Retrieve the index'th attribute
        /// </summary>
        public string this[int index]
        {
            get
            {
                return BaseGetKey(index);
            }
        }
        /// <summary>
        /// Retrieve/set the attriubute associated with the given type name.
        /// </summary>
        public SourceVersionAttribute this[string type]
        {
            get
            {
                return Get(type);
            }
            set
            {
                Set(type, value);
            }
        }
        /// <summary>
        /// Retrieve/set the attribute associated with the given type.
        /// </summary>
        public SourceVersionAttribute this[Type type]
        {
            get
            {
                return Get(type.FullName);
            }
            set
            {
                Set(type.FullName, value);
            }
        }
    }

    /// <summary>
    /// Version control attribute for Subversion.
    /// </summary>
    [SVN(@"$Id: Version.cs 339 2007-03-02 19:40:49Z hildjj $")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct,
                    AllowMultiple=false, Inherited=false)]
    public class SVNAttribute : SourceVersionAttribute
    {
        // Header: /u1/html/cvsroot/www.cyclic.com/RCS-html/info-ref.html,v 1.1 1999/04/14 19:04:02 kingdon Exp
        // Id: calc.c 148 2002-07-28 21:30:43Z sally
        private static readonly Regex REGEX =
            new Regex(@"^\$" + @"Id(: +(?<archive>[^ ]+) +(?<version>[0-9.]+) +(?<date>[0-9-]+ [0-9:]+)Z +(?<author>[^ ]+) *)?\$$");
        /// <summary>
        /// The most common.  Pass in @"$ Id $" (without the spaces).
        /// </summary>
        /// <param name="header"></param>
        public SVNAttribute(string header)
            : base(header)
        {
        }
        /// <summary>
        /// Null constructor.  This is rarely right.
        /// </summary>
        public SVNAttribute()
            : base()
        {
        }
        /// <summary>
        /// Parse the header string.
        /// </summary>
        protected override void Parse()
        {
            Match m = REGEX.Match(m_header);
            if (!m.Success)
            {
                throw new FormatException("Bad header format: " + m_header + " != " + REGEX.ToString());
            }
            if (m.Groups["archive"].Success)
            {
                m_archive = m.Groups["archive"].ToString();
                m_version = m.Groups["version"].ToString();
                m_date    = DateTime.Parse(m.Groups["date"].ToString());
                m_author  = m.Groups["author"].ToString();
            }
        }
    }
}
