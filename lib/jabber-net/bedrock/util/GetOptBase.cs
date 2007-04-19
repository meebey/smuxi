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
namespace bedrock.util

{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// GetOpt should be subclassed to create a class that handles
    /// command-line parameters.  The subclass should use fields or properties
    /// that have the CommandLine attribute set on them.  Fields and properties
    /// of type bool will be toggle flags, other types will take a value as
    /// either the next command-line parameter or following a colon.
    /// Also, now, you can create an instance of GetOpt, and pass in
    /// TODO: Give examples of sublcass and calling example.
    /// </summary>
    [SVN(@"$Id: GetOptBase.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class GetOpt
    {
        private object    m_obj   = null;
        private string[]  m_args  = null;
        private Hashtable m_flags =
#if NET20
            new Hashtable(StringComparer.InvariantCultureIgnoreCase);
#else
            new Hashtable(CaseInsensitiveHashCodeProvider.Default,
                          CaseInsensitiveComparer.Default);
#endif

        // Regular expression to parse these:
        // /a
        // /a:foo
        // -a
        // -a:foo
        private static readonly Regex FLAG_REGEX =
            new Regex("[/-]([a-z0-9_]+)([:=](.*))?", RegexOptions.IgnoreCase);
        /// <summary>
        /// Really only useful for subclasses, I think.
        /// </summary>
        public GetOpt()
        {
            // Debug.Assert(this.GetType() != typeof(GetOpt));
            m_obj = this;
        }
        /// <summary>
        /// Get ready to process command line parameters for the given target object.
        /// </summary>
        /// <param name="target">Object to set parameters on</param>
        public GetOpt(object target)
        {
            m_obj = (target == null) ? this : target;
        }
        /// <summary>
        /// Process command line parameters for the given target object, with the
        /// given arguments.
        /// </summary>
        /// <param name="target">Object to set parameters on</param>
        /// <param name="args">An array of arguments.  If null, use the environment's command line.</param>
        public GetOpt(object target, string[] args) : this(target)
        {
            Process(args);
        }
        /// <summary>
        /// Subclass interface, processing immediately.
        /// </summary>
        /// <param name="args">An array of arguments.  If null, use the environment's command line.</param>
        public GetOpt(string[] args) : this(null, args)
        {
        }
        /// <summary>
        /// Process the given command line parameters.
        /// </summary>
        /// <param name="args">An array of arguments.  If null, use the environment's command line.</param>
        public void Process(string[] args)
        {
            int        i;
            MemberInfo mi;
            Match      rm;
            Type       mit;

            SetFlags();
            if (args == null)
            {
                string[] e = Environment.GetCommandLineArgs();
                args = new string[e.Length - 1];
                Array.Copy(e, 1, args, 0, e.Length-1);
            }
            for (i=0; i<args.Length; i++)
            {
                rm = FLAG_REGEX.Match(args[i]);
                if (!rm.Success)   // no more flags
                {
                    break;
                }

                mi = (MemberInfo) m_flags[rm.Groups[1].ToString()];
                if (mi == null)
                {
                    throw new ArgumentException("Invalid command-line argument", args[i]);
                }

                mit = GetMemberType(mi);
                // methods return null types, for now.
                // TODO: should this be moved to SetValue?
                // Not sure what to do with bool params, then.
                if (mit == null)
                {
                    string old_flag = args[i];
                    MethodInfo meth = (MethodInfo) mi;
                    ParameterInfo[] pi = meth.GetParameters();
                    object[] parms = new object[pi.Length];
                    for (int j=0; j<pi.Length; j++)
                    {
                        if (i+1 >= args.Length)
                        {
                            throw new IndexOutOfRangeException("Not enough parameters for: " + old_flag);
                        }
                        parms[j] = ConvertValue(args[++i], pi[j].ParameterType);
                    }

                    meth.Invoke(m_obj, parms);
                }

                // bool flags act as toggles
                else if (mit == typeof(bool))
                {
                    SetValue(mi, ! (bool) GetValue(mi));
                }
                else
                {
                    // use the value after the colon, if it exists
                    if (rm.Groups[3].Success)
                    {
                        SetValue(mi, rm.Groups[3].ToString());
                    }
                    else
                    {
                        if (i+1 >= args.Length)
                        {
                            throw new IndexOutOfRangeException("Not enough parameters for: " + args[i]);
                        }
                        SetValue(mi, args[++i]);
                    }
                }
            }
            // copy the rest of the argument array (those after the flags)
            // into an array for later use.
            m_args = new string[args.Length - i];
            Array.Copy(args, i, m_args, 0, args.Length - i);
            CheckRequired();
        }
        /// <summary>
        /// Look at myself, to see if there are any command line
        /// parameter fields or properties.
        /// </summary>
        private void SetFlags()
        {
            if (m_flags.Count != 0)
                return;
            MemberInfo[] mis = GetCommandLineMembers();
            foreach (MemberInfo mi in mis)
            {
                CommandLineAttribute cla = GetOption(mi);
                string cf = cla.CommandFlag;
                // If no CommandFlag specified, use the member name.
                if (cf == null)
                {
                    cf = mi.Name;
                }
                // make sure required parameters are initialized to null.
                if (cla.Required && (GetValue(mi) != null))
                {
                    throw new ArgumentException("Must provide null initial value for required parameters: ", mi.Name);
                }
                m_flags[cf] = mi;
            }
        }
        /// <summary>
        /// Make sure all required fields got hit.
        /// </summary>
        private void CheckRequired()
        {
            MemberInfo[] mis = GetCommandLineMembers();
            foreach (MemberInfo mi in mis)
            {
                CommandLineAttribute cla = GetOption(mi);
                if (cla.Required && (GetValue(mi) == null))
                {
                    throw new ArgumentException("Did not provide required parameter: ", mi.Name);
                }
            }
        }
        /// <summary>
        /// Set the value of a field or property, depending on the kind of member.
        /// Coerce the type of the value passed in, as possible
        /// </summary>
        /// <param name="mi">The member to set</param>
        /// <param name="val">The value to set</param>
        private void SetValue(MemberInfo mi, object val)
        {
            switch (mi.MemberType)
            {
            case MemberTypes.Field:
                FieldInfo fi = (FieldInfo) mi;
                fi.SetValue(m_obj, ConvertValue(val, fi.FieldType));
                break;
            case MemberTypes.Property:
                PropertyInfo pi = (PropertyInfo) mi;
                pi.SetValue(m_obj, ConvertValue(val, pi.PropertyType), null);
                break;
            default:
                throw new ArgumentException("Invalid member type", "mi");
            }
        }
        /// <summary>
        /// Convert a field value representation to a value of the correct type.
        /// Enums need special handling, at least for now.
        /// </summary>
        /// <param name="val">The value to convert</param>
        /// <param name="TargetType">The type to convert it to</param>
        private object ConvertValue(object val, Type TargetType)
        {
            if (TargetType.IsEnum)
            {
                return Enum.Parse(TargetType, (string) val, true);
            }
            return Convert.ChangeType(val, TargetType);
        }
        /// <summary>
        /// Get the value from a field or property, depending on the type of member.
        /// </summary>
        /// <param name="mi"> </param>
        private object GetValue(MemberInfo mi)
        {
            object ret = null;
            switch (mi.MemberType)
            {
            case MemberTypes.Field:
                FieldInfo fi = (FieldInfo) mi;
                ret = fi.GetValue(m_obj);
                break;
            case MemberTypes.Property:
                PropertyInfo pi = (PropertyInfo) mi;
                ret = pi.GetValue(m_obj, null);
                break;
            default:
                throw new ArgumentException("Invalid member type", "mi");
            }
            return ret;
        }
        /// <summary>
        /// Get the type contained in the given member.
        /// </summary>
        /// <param name="mi">The member to check</param>
        private static Type GetMemberType(MemberInfo mi)
        {
            Type ret = null;
            switch (mi.MemberType)
            {
            case MemberTypes.Field:
                FieldInfo fi = (FieldInfo) mi;
                ret = fi.FieldType;
                break;
            case MemberTypes.Property:
                PropertyInfo pi = (PropertyInfo) mi;
                ret = pi.PropertyType;
                break;
            case MemberTypes.Method:
                ret = null;
                break;
            default:
                throw new ArgumentException("Invalid member type", "mi");
            }
            return ret;
        }
        /// <summary>
        /// Get all of the members that are tagged with the CommandLineAttribute.
        /// NOTE: this currently returns private members as well, but setting the
        /// BindingFlags to public doesn't return anything.  Could be a bug in the BCL?
        /// </summary>
        private MemberInfo[] GetCommandLineMembers()
        {
            Type t = m_obj.GetType();
            MemberInfo[] mis = t.FindMembers(MemberTypes.All,
                                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance,
                                             new MemberFilter(AttrMemberFilter),
                                             typeof(CommandLineAttribute));
            Debug.Assert(mis.Length > 0, "Must have at least one CommandLine attribute on class: " + t.FullName);
            return mis;
        }

        /// <summary>
        /// Filter proc for GetCommandLineMembers.  Returns true if the member
        /// implements a given attribute.
        /// </summary>
        /// <param name="m">The member to evaluate</param>
        /// <param name="filterCriteria">The attribute type to check for</param>
        private static bool AttrMemberFilter(MemberInfo m, object filterCriteria)
        {
            return m.GetCustomAttributes((Type)filterCriteria, true).Length > 0;
        }
        /// <summary>
        /// Get the CommandLineAttribute off of a member.  Assumes that the member implements
        ///<i>exactly</i> one instance of the attribute.
        /// </summary>
        /// <param name="mi">The member to retrieve from</param>
        private CommandLineAttribute GetOption(MemberInfo mi)
        {
            object[] o = mi.GetCustomAttributes(typeof(CommandLineAttribute), true);
            Debug.Assert(o.Length == 1);
            return ((CommandLineAttribute[]) o)[0];
        }
        /// <summary>
        /// The list of command-line arguments that were not associated with flags.
        /// </summary>
        public virtual string[] Args
        {
            get { return m_args; }
        }
        /// <summary>
        /// Get/Set a parameter on the managed object, using the flag.
        /// Warning: this will do an implicit conversion to the type of the
        /// field associated with the flag.
        /// If you're using this, you've probably got a design problem.
        /// </summary>
        public object this[string flag]
        {
            get
            {
                SetFlags();
                MemberInfo mi = (MemberInfo) m_flags[flag];
                return GetValue(mi);
            }
            set
            {
                SetFlags();
                MemberInfo mi = (MemberInfo) m_flags[flag];
                SetValue(mi, value);
            }
        }
        /// <summary>
        /// Get a usage description string from the object.
        /// Use the CommandLineAttribute descriptions wherever possible.
        /// </summary>
        public virtual string Usage
        {
            get
            {
                SetFlags();
                StringBuilder sb = new StringBuilder();
                // Gr.  this used to work, and I can't find the new API.
                //sb.Append(System.IO.File.GetFileNameFromPath(Environment.GetCommandLineArgs()[0]));
                sb.Append(Environment.GetCommandLineArgs()[0]);
                string[] keys = new string[m_flags.Count];
                m_flags.Keys.CopyTo(keys, 0);
                Array.Sort(keys);
                foreach (object key in keys)
                {
                    MemberInfo mi = (MemberInfo) m_flags[key];
                    CommandLineAttribute cla = GetOption(mi);
                    Type       mit = GetMemberType(mi);
                    sb.Append(" ");
                    if (!cla.Required)
                    {
                        sb.Append("[");
                    }

                    // method
                    if (mit == null)
                    {
                        MethodInfo meth = (MethodInfo) mi;
                        ParameterInfo[] pis = meth.GetParameters();
                        sb.AppendFormat("/{0}", key);
                        foreach (ParameterInfo pi in pis)
                        {
                            sb.Append(" ");
                            sb.Append(pi.ParameterType.Name);
                        }
                    }
                    else if (mit == typeof(bool))
                    {
                        sb.AppendFormat("/{0}", key);
                    }
                    else if (mit.IsEnum)
                    {
                        sb.AppendFormat("/{0} (", key);
                        string val = GetValue(mi).ToString();
                        string[] names = Enum.GetNames(mit);
                        bool first = true;
                        foreach (string n in names)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                sb.Append("|");
                            }
                            if (val == n)
                            {
                                sb.AppendFormat("*{0}*", n);
                            }
                            else
                            {
                                sb.Append(n);
                            }
                        }
                        sb.Append(")");
                    }
                    else
                    {
                        sb.AppendFormat("/{0} {1}", key, GetValue(mi));
                    }
                    if (!cla.Required)
                    {
                        sb.Append("]");
                    }
                }
                sb.Append(Environment.NewLine);
                foreach (object key in keys)
                {
                    sb.AppendFormat("\t/{0}: \t{1}", key, GetOption((MemberInfo)m_flags[key]).Description);
                    sb.Append(Environment.NewLine);
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// Print out the usage information on StdErr, and exit with code 64.
        /// </summary>
        public virtual void UsageExit()
        {
            Console.Error.WriteLine(Usage);
            Environment.Exit(64);
        }

        /// <summary>
        /// Echo Command-Line requirements for a GUI app via a MessageBox
        /// (since we do not have user-visible stdout)
        /// </summary>
        public virtual void UsageGUIExit()
        {
            /*
          MessageBox.Show
            (Usage, "Command-line argument usage",
             MessageBoxButtons.OK, MessageBoxIcon.Error);
          Environment.Exit(64);
             */
            throw new NotImplementedException("This is the only thing that requires Windows.Forms.  Removed.");
        }
    }
    /// <summary>
    /// Attribute to annotate subclasses of GetOpt.  Any field or property
    /// that gets this attribute is a possible command-line argument for the
    /// program containing the GetOpt subclass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method,
                    AllowMultiple=false)]
    [SVN(@"$Id: GetOptBase.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class CommandLineAttribute : Attribute
    {
        private string m_commandFlag = null;
        private string m_description = null;
        private bool   m_required    = false;
        /// <summary>
        /// Use the member name for the command-line parameter.
        /// </summary>
        public CommandLineAttribute()
        {
        }
        /// <summary>
        /// Use the given string as the command-line parameter.
        /// </summary>
        /// <param name="commandFlag"> </param>
        public CommandLineAttribute(string commandFlag)
        {
            m_commandFlag = commandFlag;
        }
        /// <summary>
        /// Use the given string as the command-line parameter.
        /// </summary>
        /// <param name="commandFlag"> </param>
        /// <param name="description"> </param>
        public CommandLineAttribute(string commandFlag, string description)
        {
            m_commandFlag = commandFlag;
            m_description = description;
        }
        /// <summary>
        /// Use the given string as the command-line parameter.
        /// </summary>
        /// <param name="commandFlag"> </param>
        /// <param name="description"> </param>
        /// <param name="required"> </param>
        public CommandLineAttribute(string commandFlag, string description, bool required)
        {
            m_commandFlag = commandFlag;
            m_description = description;
            m_required    = required;
        }
        /// <summary>
        /// Get the command-line flag.  If none was specified, returns null.
        /// </summary>
        public string CommandFlag
        {
            get
            {
                return m_commandFlag;
            }
        }
        /// <summary>
        /// Get the command-line description.  If none was specified, returns null.
        /// </summary>
        public string Description
        {
            get
            {
                return m_description;
            }
            set
            {
                m_description = value;
            }
        }
        /// <summary>
        /// Is the option required?  Defaults to false.
        /// </summary>
        public bool Required
        {
            get
            {
                return m_required;
            }
            set
            {
                m_required = value;
            }
        }
    }
}
