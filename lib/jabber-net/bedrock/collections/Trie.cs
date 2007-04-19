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
using bedrock.util;

namespace bedrock.collections
{
    /// <summary>
    /// A method to get called for each key in the trie
    /// </summary>
    public delegate bool TrieKeyWalker(TrieNode e, object data, ByteStack key);
    /// <summary>
    /// A method to get called for each node in the tree
    /// </summary>
    public delegate bool TrieWalker(TrieNode e, object data);
    /// <summary>
    /// A trie is a tree structure that implements a radix search.  Each node of the tree has a
    /// sub-node for each possible next byte.
    /// </summary>
    [SVN(@"$Id: Trie.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Trie : IDictionary
    {
        private static readonly System.Text.Encoding ENCODING = System.Text.Encoding.Default;

        /// <summary>
        /// The root node of the trie.
        /// </summary>
        private TrieNode m_root  = new TrieNode(null, 0);

        /// <summary>
        /// The number of nodes are in the trie
        /// </summary>
        private int      m_count = 0;

        /// <summary>
        /// Create an empty trie
        /// </summary>
        public Trie() {}

        /// <summary>
        /// Find a node for a given key, somewhere under the root.
        /// </summary>
        /// <param name="key">The bytes to search for, where key[0] corresponds to a child
        /// node of the root.</param>
        /// <param name="create">Create nodes that don't exist, while searching.</param>
        protected virtual TrieNode FindNode(byte[] key, bool create)
        {
            return FindNode(key, m_root, create);
        }
        /// <summary>
        /// Find a node in the given sub-tree.
        /// </summary>
        /// <param name="key">The key to search on, where key[0] corresponds to a child of startAt.</param>
        /// <param name="startAt">The node to search under</param>
        /// <param name="create">Create nodes that don't exist, while searching.</param>
        protected virtual TrieNode FindNode(byte[] key, TrieNode startAt, bool create)
        {
            TrieNode current = startAt;
            byte b;

            for (int i=0; (i<key.Length) && (current != null); i++)
            {
                b = key[i];
                current = current[b, create];
            }
            return current;
        }
        /// <summary>
        /// Compute the byte array corresping to the given object.
        /// This is likely to cause problems for non 7-bit ASCII text.
        /// </summary>
        /// <param name="key"> </param>
        protected static byte[] KeyBytes(object key)
        {
            if (key is byte[])
            {
                return (byte[]) key;
            }

            return ENCODING.GetBytes(key.ToString());
        }
        /// <summary>
        /// Extra functionality for trie's whose values are integers.
        /// Increment the value corresponding to the key.  If
        /// the key doesn't exist, put in a value of '1'.
        /// </summary>
        /// <param name="key"> </param>
        public virtual void Increment(object key)
        {
            TrieNode e = FindNode(KeyBytes(key), true);
            if (e.Value == null)
            {
                e.Value = 1;
            }
            else
            {
                e.Value = ((int) e.Value) + 1;
            }
        }
        /// <summary>
        /// Perform the given function on every element of the trie.  Perl's map() operator.
        /// </summary>
        /// <param name="w">The function to call</param>
        /// <param name="data">Extra data to pass along to the function.</param>
        public void Traverse(TrieKeyWalker w, object data)
        {
            Traverse(w, data, m_root, new ByteStack());
        }

        /// <summary>
        /// Perform the given function on every element of the trie.  Perl's map() operator.
        /// </summary>
        /// <param name="w">The function to call</param>
        /// <param name="data">Extra data to pass along to the function.</param>
        /// <param name="current">What node are we currently on?</param>
        /// <param name="key">A stack holding the current key value</param>
        protected void Traverse(TrieKeyWalker w, object data, TrieNode current, ByteStack key)
        {
            if (!w(current, data, key))
            {
                return;
            }
            foreach (TrieNode e in current)
            {
                key.Push(e.Byte);
                Traverse(w, data, e, key);
                key.Pop();
            }
        }
        /// <summary>
        /// Perform the given function on every element of the trie.  Perl's map() operator.
        /// Don't keep track of the keys (slightly faster than the other Traverse() method).
        /// </summary>
        /// <param name="w">The function to call</param>
        /// <param name="data">Extra data to pass along to the function.</param>
        public void Traverse(TrieWalker w, object data)
        {
            Traverse(w, data, m_root);
        }
        /// <summary>
        /// Perform the given function on every element of the trie.  Perl's map() operator.
        /// </summary>
        /// <param name="w">The function to call</param>
        /// <param name="data">Extra data to pass along to the function.</param>
        /// <param name="current">What node are we currently on?</param>
        protected void Traverse(TrieWalker w, object data, TrieNode current)
        {
            if (! w(current, data))
            {
                return;
            }
            foreach (TrieNode e in current)
            {
                Traverse(w, data, e);
            }
        }
        #region System.Collections.IDictionary

        /// <summary>
        /// Retrieve the value associated with the given key.
        /// </summary>
        public virtual object this[object key]
        {
            get
            {
                TrieNode e = FindNode(KeyBytes(key), false);
                return (e == null) ? null : e.Value;
            }
            set
            {
                TrieNode e = FindNode(KeyBytes(key), true);
                e.Value = value;
                m_count++;
            }
        }

        /// <summary>
        /// Always "false" for now.
        /// </summary>
        public bool IsFixedSize
        {
            get { return false; }
        }


        /// <summary>
        /// Find all of the keys.
        /// </summary>
        /// <param name="n"> </param>
        /// <param name="data"> </param>
        /// <param name="key"> </param>
        private bool KeyWalker(TrieNode n, object data, ByteStack key)
        {
            if (n.Value != null)
            {
                ArrayList al = (ArrayList) data;
                al.Add((byte[]) key);
            }
            return true;
        }

        /// <summary>
        /// Get a list of all of the keys.  Hope this doesn't get called often, since it has to make copies
        /// of all of the possible keys.
        /// </summary>
        public ICollection Keys
        {
            get
            {
                ArrayList al = new ArrayList(m_count);
                Traverse(new TrieKeyWalker(KeyWalker), al);
                al.TrimToSize();
                return al;
            }
        }

        /// <summary>
        /// Return a collection containing all of the values.
        /// </summary>
        public ICollection Values
        {
            get
            {
                // pretty easy, since we implement ICollection.
                return new ArrayList(this);
            }
        }

        /// <summary>
        /// Remove the node associated with the given key, along with all newly empty ancestors.
        /// </summary>
        /// <param name="key"> </param>
        public void Remove(object key)
        {
            TrieNode current = FindNode(KeyBytes(key), false);
            if (current == null)
            {
                return;
            }

            current.Value = null;
            m_count--;

            if (m_count == 0)
            {
                Clear();
                return;
            }

            // prune
            byte last;
            while (current.IsEmpty)
            {
                last = current.Byte;
                current = current.Parent;
                if (current == null)
                {
                    // all the way empty.
                    break;
                }
                current.Remove(last);
            }
        }

        /// <summary>
        /// Iterate the dictionary way.
        /// </summary>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new TrieEnumerator(this);
        }

        /// <summary>
        /// Delete all nodes.
        /// </summary>
        public void Clear()
        {
            m_root  = new TrieNode(null, 0);
            m_count = 0;
        }

        /// <summary>
        /// Add a new key/value pair.
        /// </summary>
        /// <param name="key"> </param>
        /// <param name="value"> </param>
        public void Add(object key, object value)
        {
            this[key] = value;
        }

        /// <summary>
        /// Is the given key in the trie?
        /// </summary>
        /// <param name="key"> </param>
        public bool Contains(object key)
        {
            TrieNode current = FindNode(KeyBytes(key), false);
            return current != null;
        }

        #endregion
        #region System.Collections.ICollection

        /// <summary>
        /// How many values are stored?  Note: NOT how many nodes.
        /// </summary>
        public int Count
        {
            get
            {
                return m_count;
            }
        }

        /// <summary>
        /// Object to synchronize on, if in thread-safe mode.
        /// TODO: implement settable SyncRoot
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Always "false" for now
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Always "false" for now
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Copy into an array.
        /// </summary>
        /// <param name="array"> </param>
        /// <param name="index"> </param>
        public void CopyTo(Array array, int index)
        {
            int i = index;
            foreach (object o in this)
            {
                array.SetValue(o, i);
                i++;
            }
        }

        #endregion
        #region System.Collections.IEnumerable

        /// <summary>
        /// Iterate over the keys.  Each key will be a byte[].
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new TrieEnumerator(this);
        }

        #endregion

        private class TrieEnumerator : IDictionaryEnumerator
        {
            protected Trie     m_trie;
            protected Stack    m_pos     = new Stack();
            protected TrieNode m_current = null;
            public TrieEnumerator(Trie t)
            {
                m_trie = t;
                m_pos.Push(m_trie.m_root);
            }

            public object Current
            {
                get
                {
                    return Entry;
                }
            }

            public void Reset()
            {
                m_pos.Clear();
                m_pos.Push(m_trie.m_root);
            }

            public bool MoveNext()
            {
                if (m_pos.Count <= 0)
                {
                    return false;
                }

                m_current = (TrieNode) m_pos.Pop();

                foreach (TrieNode e in m_current)
                {
                    m_pos.Push(e);
                }

                if (m_current.Value != null)
                {
                    return true;
                }

                return MoveNext();
            }

            public object Key
            {
                get
                {
                    return m_current.Key;
                }
            }

            public object Value
            {
                get
                {
                    return m_current.Value;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry(m_current.Key, m_current.Value);
                }
            }
        }
    }
}
