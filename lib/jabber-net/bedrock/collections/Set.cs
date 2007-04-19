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
    /// The different ways a set can be implemented.
    /// </summary>
    [SVN(@"$Id: Set.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum SetImplementation
    {
        /// <summary>
        /// Hash table.
        /// </summary>
        Hashtable,
        /// <summary>
        /// Red/Black tree.
        /// </summary>
        Tree,
        /// <summary>
        /// Skip List.
        /// </summary>
        SkipList
    }

    /// <summary>
    /// Set backed into a Tree.
    /// </summary>
    [SVN(@"$Id: Set.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Set : ISet
    {
        private static readonly object s_nothing = new object();

        private IDictionary m_dict;

        /// <summary>
        /// Create a new, empty Set backed into a hash table.
        /// </summary>
        public Set()
        {
            m_dict = new Hashtable();
        }

        /// <summary>
        /// Create a set with the given back-end implementation.
        /// </summary>
        /// <param name="impl">How to implement the set.</param>
        public Set(SetImplementation impl)
        {
            switch (impl)
            {
                case SetImplementation.Hashtable:
                    m_dict = new Hashtable();
                    break;
                case SetImplementation.Tree:
                    m_dict = new Tree();
                    break;
                case SetImplementation.SkipList:
                    m_dict = new SkipList();
                    break;
                default:
                    throw new NotImplementedException("Unknown SetImplementation");
            }
        }

        #region Implementation of ISet
        /// <summary>
        /// Add an object to the set
        /// </summary>
        /// <param name="o">The object to add</param>
        /// <exception cref="ArgumentException">object was already in the set.</exception>
        public void Add(object o)
        {
            if (!m_dict.Contains(o))
                m_dict.Add(o, s_nothing);
        }

        /// <summary>
        /// Remove the given object from the set.  If the object is not in the set, this is a no-op.
        /// </summary>
        /// <param name="o">The object to remove.</param>
        public void Remove(object o)
        {
            m_dict.Remove(o);
        }

        /// <summary>
        /// Remove all items from the set.
        /// </summary>
        public void Clear()
        {
            m_dict.Clear();
        }

        /// <summary>
        /// Is the given object in the set?
        /// </summary>
        /// <param name="o">The object to search for.</param>
        /// <returns>True if the object is in the set.</returns>
        public bool Contains(object o)
        {
            return m_dict.Contains(o);
        }

        /// <summary>
        /// Return a new collection that contains all of the items that
        /// are in this set or the other set.
        /// </summary>
        public bedrock.collections.ISet Union(bedrock.collections.ISet other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return a new collection that contains all of the items that
        /// are in this list *and* the other set.
        /// </summary>
        public bedrock.collections.ISet Intersection(bedrock.collections.ISet other)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Implementation of ICollection
        /// <summary>
        /// Copies the elements of the ICollection to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">The array to copy</param>
        /// <param name="index">The index to start at</param>
        public void CopyTo(System.Array array, int index)
        {
            int count = index;
            foreach (object o in this)
            {
                array.SetValue(o, count);
                count++;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the ICollection is synchronized (thread-safe).
        /// </summary>
        public bool IsSynchronized
        {
            get { return m_dict.IsSynchronized; }
        }

        /// <summary>
        /// Get the number of items in the set.
        /// </summary>
        public int Count
        {
            get { return m_dict.Count; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the ICollection.
        /// </summary>
        public object SyncRoot
        {
            get { return m_dict.SyncRoot; }
        }
        #endregion

        #region Implementation of IEnumerable
        /// <summary>
        /// Enumerate over all items in the set.
        /// </summary>
        /// <returns></returns>
        public System.Collections.IEnumerator GetEnumerator()
        {
            return new TreeSetEnumerator(m_dict);
        }
        #endregion

        private class TreeSetEnumerator : IEnumerator
        {
            private IEnumerator m_enum;

            public TreeSetEnumerator(IDictionary e)
            {
                m_enum = e.GetEnumerator();
            }

            #region Implementation of IEnumerator
            public void Reset()
            {
                m_enum.Reset();
            }

            public bool MoveNext()
            {
                return m_enum.MoveNext();
            }

            public object Current
            {
                get
                {
                    DictionaryEntry entry = (DictionaryEntry) m_enum.Current;
                    return entry.Key;
                }
            }
            #endregion
        }
    }
}
