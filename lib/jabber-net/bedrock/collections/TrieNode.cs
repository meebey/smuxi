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

using System.IO;
using System.Collections;
using bedrock.util;
namespace bedrock.collections
{
    /// <summary>
    /// A node in a Trie.  This class is public to support traversal via Trie.Traverse().
    /// </summary>
    [SVN(@"$Id: TrieNode.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class TrieNode : IEnumerable
    {
        // Warning: Assumption of 7-bit ASCII encoding!
        // TODO: replace with GraphNode
        //public const byte MIN_CHAR  = (byte) ' ';
        //public const byte MAX_CHAR  = (byte) '~';
        //public const byte NUM_CHARS = MAX_CHAR - MIN_CHAR + 1;

        //private TrieNode[] m_children = new TrieNode[NUM_CHARS];
        private Tree       m_children = new Tree();
        private TrieNode   m_parent   = null;
        private Object     m_value    = null;
        private byte       m_key      = 0;

        /// <summary>
        /// Create a new node
        /// </summary>
        /// <param name="parent">The parent of the new node</param>
        /// <param name="key">The byte for this node</param>
        public TrieNode(TrieNode parent, byte key)
        {
            m_parent = parent;
            m_key    = key;
        }

        /// <summary>
        /// Add a child to this node
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual TrieNode Add(byte key)
        {
            TrieNode e = new TrieNode(this, key);
            this[key]  = e;
            return e;
        }
        /// <summary>
        /// Are there children of this node?
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                if (m_value != null)
                    return false;
                foreach (object key in m_children)
                {
                    if (m_children[key] != null)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Get the parent of this node
        /// </summary>
        public TrieNode Parent
        {
            get
            {
                return m_parent;
            }
        }
        /// <summary>
        /// Get the byte associated with this node
        /// </summary>
        public byte Byte
        {
            get
            {
                return m_key;
            }
        }

        /// <summary>
        /// Retrive the full key for this node, traversing parent-ward toward the root.
        /// </summary>
        public byte[] Key
        {
            get
            {
                MemoryStream ms = new MemoryStream();
                TrieNode current = this;
                while (current.Parent != null)
                {
                    ms.WriteByte(current.Byte);
                    current = current.Parent;
                }
                byte[] buf = ms.ToArray();
                Array.Reverse(buf);
                return buf;
            }
        }
        /// <summary>
        /// The value associated with this node
        /// </summary>
        public Object Value
        {
            get
            {
                return m_value;
            }
            set
            {
                m_value = value;;
            }
        }
        /// <summary>
        /// Get the child associated with the given byte, or null if one doesn't exist.
        /// </summary>
        public TrieNode this[byte key]
        {
            get
            {
                return (TrieNode) m_children[key];
            }
            set
            {
                m_children[key] = value;
            }
        }
        /// <summary>
        /// Get the child associated with the given byte, or null if one doesn't exist.
        /// If create is true, a node will be added with a null value
        /// if a node does not already exist, so that this can be used
        /// as an lvalue.
        /// </summary>
        public TrieNode this[byte key, bool create]
        {
            get
            {
                TrieNode e = this[key];
                if (e != null)
                {
                    return e;
                }
                if (create)
                {
                    return Add(key);
                }
                return null;
            }
        }
        /// <summary>
        /// Is there a child at the given byte?
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasChild(byte key)
        {
            return this[key] != null;
        }
        /// <summary>
        /// Remove the child at the given byte
        /// </summary>
        /// <param name="key"></param>
        public void Remove(byte key)
        {
            this[key] = null;
        }
        /// <summary>
        /// Compares the specified object with this entry for equality.
        /// Returns <tt>true</tt> if the given object is also a map entry and
        /// the two entries represent the same mapping.  More formally, two
        /// entries <tt>e1</tt> and <tt>e2</tt> represent the same mapping
        /// if<pre>
        ///     (e1.getKey()==null ?
        ///      e2.getKey()==null : e1.getKey().equals(e2.getKey()))  &amp;&amp;
        ///     (e1.getValue()==null ?
        ///      e2.getValue()==null : e1.getValue().equals(e2.getValue()))
        /// </pre>
        /// This ensures that the <tt>equals</tt> method works properly across
        /// different implementations of the <tt>Map.Entry</tt> interface.
        /// </summary>
        /// <param name="o">object to be compared for equality with this map entry</param>
        /// <returns><tt>true</tt> if the specified object is equal to this map
        ///         entry.</returns>
        public override bool Equals(Object o)
        {
            if (o == null)
                return false;
            if (o == this)
                return true;

            if (! (o is TrieNode))
                return false;
            TrieNode e = (TrieNode) o;

            return ((Key == null)    ? (e.Key   == null) : Key.Equals(e.Key))  &&
                ((Value == null) ? (e.Value == null) : Value.Equals(e.Value));
        }
        /// <summary>
        /// Returns the hash code value for this map entry.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ((Key==null)    ? 0 : Key.GetHashCode()) ^
                    ((Value==null) ? 0 : Value.GetHashCode());
        }
        #region Implementation of IEnumerable
        /// <summary>
        /// Iterate over the children
        /// </summary>
        /// <returns></returns>
        public System.Collections.IEnumerator GetEnumerator()
        {
            return new TrieNodeEnumerator(this);
        }
        #endregion
        private class TrieNodeEnumerator : IEnumerator
        {
            private TrieNode    m_node;
            private IEnumerator m_enum;
            public TrieNodeEnumerator(TrieNode n)
            {
                m_node = n;
                Reset();
            }
            #region Implementation of IEnumerator
            public void Reset()
            {
                // yeah, I know.  but I want to go to bed.
                m_enum  = m_node.m_children.Values.GetEnumerator();
            }

            public bool MoveNext()
            {
                return m_enum.MoveNext();
            }

            public object Current
            {
                get
                {
                    return m_enum.Current;
                }
            }
            #endregion
        }
    }
}
