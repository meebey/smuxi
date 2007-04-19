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
using System.Collections;

namespace jabber.protocol
{
    /// <summary>
    /// Namespace stack.
    /// </summary>
    public class NS
    {
        private Stack m_stack = new Stack();

        /// <summary>
        /// Create a new stack, primed with xmlns and xml as prefixes.
        /// </summary>
        public NS()
        {
            PushScope();
            AddNamespace("xmlns", "http://www.w3.org/2000/xmlns/");
            AddNamespace("xml", "http://www.w3.org/XML/1998/namespace");
        }

        /// <summary>
        /// Declare a new scope, typically at the start of each element
        /// </summary>
        public void PushScope()
        {
            m_stack.Push(new Hashtable());
        }

        /// <summary>
        /// Pop the current scope off the stack.  Typically at the end of each element.
        /// </summary>
        public void PopScope()
        {
            m_stack.Pop();
        }

        /// <summary>
        /// Add a namespace to the current scope.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="uri"></param>
        public void AddNamespace(string prefix, string uri)
        {
            ((Hashtable)m_stack.Peek()).Add(prefix, uri);
        }

        /// <summary>
        /// Lookup a prefix to find a namespace.  Searches down the stack, starting at the current scope.
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public string LookupNamespace(string prefix)
        {
            foreach (Hashtable ht in m_stack)
            {
                if ((ht.Count > 0) && (ht.ContainsKey(prefix)))
                    return (string)ht[prefix];
            }
            return "";
        }

        /// <summary>
        /// The current default namespace.
        /// </summary>
        public string DefaultNamespace
        {
            get { return LookupNamespace(string.Empty); }
        }

        /// <summary>
        /// Debug output only.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (Hashtable ht in m_stack)
            {
                sb.Append("---\n");
                foreach (string k in ht.Keys)
                {
                    sb.Append(string.Format("{0}={1}\n", k, ht[k]));
                }
            }
            return sb.ToString();
        }
    }
}
