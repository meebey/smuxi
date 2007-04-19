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
    /// A Trie that is searchable for substrings.  Uses a separate set of indexes
    /// to allow entry into the Trie at any point.  Yes, this
    /// </summary>
    [SVN(@"$Id: IndexedTrie.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class IndexedTrie : Trie
    {
        private Tree m_indexes    = new Tree();
        private int  m_maxResults = 100;

        /// <summary>
        ///
        /// </summary>
        public IndexedTrie()  {}

        /// <summary>
        ///
        /// </summary>
        /// <param name="maxResults"></param>
        public IndexedTrie(int maxResults)
        {
            m_maxResults = maxResults;
        }

        /// <summary>
        /// The maximum number of results to return from any query.  This is an approximate number.
        /// </summary>
        public int MaxResults
        {
            get
            {
                return m_maxResults;
            }
            set
            {
                m_maxResults = value;
            }
        }
        /// <summary>
        /// Find the index for the given byte.
        /// </summary>
        protected ArrayList this[byte b]
        {
            get
            {
                return (ArrayList) m_indexes[b];
            }
        }
        /// <summary>
        /// Traverse the trie, computing indexes.
        /// </summary>
        /// <param name="n"> </param>
        /// <param name="data"> </param>
        private bool IndexWalker(TrieNode n, object data)
        {
            if (n.Parent != null)
            {
                 this[n.Byte].Add(new WeakReference(n));
            }
            return true;
        }
        /// <summary>
        /// Compute the index.
        /// </summary>
        public void Index()
        {
            Traverse(new TrieWalker(IndexWalker), null);
            foreach (ArrayList al in m_indexes)
            {
                al.TrimToSize();
            }
        }
        /// <summary>
        /// Copy the keys from the sub-tree into an ArrayList.
        /// </summary>
        /// <param name="n"> </param>
        /// <param name="data"> </param>
        /// <param name="key"> </param>
        private bool CopyWalker(TrieNode n, object data, ByteStack key)
        {
            if (n.Value != null)
            {
                ArrayList al = (ArrayList) data;
                al.Add((byte[]) key);
                if (al.Count >= m_maxResults)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Return a list of keys that contain the given substring.
        /// </summary>
        /// <param name="lookFor">The substring to search for.</param>
        public ArrayList SubString(byte[] lookFor)
        {
            ArrayList starts = (ArrayList) m_indexes[lookFor[0]];
            ArrayList finds = new ArrayList();
            byte[] nBuf = new byte[lookFor.Length - 1];
            Buffer.BlockCopy(lookFor, 1, nBuf, 0, lookFor.Length - 1);
            TrieKeyWalker w = new TrieKeyWalker(CopyWalker);
            foreach (WeakReference wref in starts)
            {
                if (finds.Count >= m_maxResults)
                {
                    break;
                }
                TrieNode first = (TrieNode) wref.Target;
                if (first == null)
                {
                    // node got removed out from underneath.
                    starts.Remove(wref);
                }
                else
                {
                    TrieNode last = FindNode(nBuf, first, false);
                    if (last != null)
                    {
                        Traverse(w, finds, last, new ByteStack(last.Key));
                    }
                }
            }
            return finds;
        }
    }
}
