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
    /// A node in a Graph, such as a Tree
    /// </summary>
    [SVN(@"$Id: GraphNode.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class GraphNode : IEnumerable
    {
        private object      m_key      = null;
        private object      m_data     = null;
        private GraphNode   m_parent   = null;
        private IDictionary m_children = null;
        private bool        m_sorted   = true;
        /// <summary>
        /// Create a new node, with key and data
        /// </summary>
        /// <param name="key">The key used to retrieve the data</param>
        /// <param name="data">The data in the node</param>
        public GraphNode(object key, object data) : this(key, data, true)
        {
        }
        /// <summary>
        /// Create a new node, with key and data, possibly having sorted children.
        /// </summary>
        /// <param name="key">The key used to retrieve the data</param>
        /// <param name="data">The data in the node</param>
        /// <param name="sorted">Should the children be sorted?</param>
        public GraphNode(object key, object data, bool sorted)
        {
            m_key    = key;
            m_data   = data;
            m_sorted = sorted;
            if (m_sorted)
            {
                m_children = new SortedList();
            }
            else
            {
                m_children = new Hashtable();
            }
        }
        /// <summary>
        /// Add a new child node
        /// </summary>
        /// <param name="key">The key for the child</param>
        /// <param name="data">The data for the child</param>
        /// <returns></returns>
        public GraphNode Add(object key, object data)
        {
            GraphNode n = new GraphNode(key, data, m_sorted);
            n.m_parent = this;
            m_children.Add(key, n);
            return n;
        }
        /// <summary>
        /// Retrieve a child node, based on the key.
        /// </summary>
        public object this[object key]
        {
            get
            {
                return ((GraphNode)m_children[key]).m_data;
            }
        }
        /// <summary>
        /// Is this the root node?
        /// </summary>
        public bool IsRoot
        {
            get
            {
                return m_parent == null;
            }
        }
        #region IEnumerable

        /// <summary>
        /// Iterate over the child nodes
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return new GraphNodeEnumerator(this);
        }

        private class GraphNodeEnumerator : IEnumerator
        {
            private IEnumerator m_arrayEnumerator;
            public GraphNodeEnumerator(GraphNode n)
            {
                m_arrayEnumerator = n.m_children.GetEnumerator();
            }

            public object Current
            {
                get
                {
                    return ((GraphNode)m_arrayEnumerator.Current).m_data;
                }
            }

            public bool MoveNext()
            {
                return m_arrayEnumerator.MoveNext();
            }

            public void Reset()
            {
                m_arrayEnumerator.Reset();
            }
        }
        #endregion
    }
}
