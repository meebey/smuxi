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
    /// A doubly-linked list implementation, with a sentinal wrap-around
    /// m_header.  Yes, it <b>does</b> seem like this should have been included
    /// in System.Collections.  This may be a nicer implementation of Queue
    /// than the one in System.Collections, which uses an array.  YMMV.
    /// </summary>
    [SVN(@"$Id: LinkedList.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class LinkedList : IList
    {
        private Node      m_header     = new Node(null, null, null);
        private int       m_size       = 0;
        private int       m_modCount   = 0;
        private IComparer m_comparator = null;
        private bool      m_readOnly   = false;
        private bool      m_synch      = false;
        /// <summary>
        /// Create an empty list
        /// </summary>
        public LinkedList()
        {
            m_header.next = m_header.previous = m_header;
        }
        /// <summary>
        /// Create a list with the targets of the given
        /// enumeration copied into it.
        /// </summary>
        /// <param name="e"></param>
        public LinkedList(IEnumerable e) : this()
        {
            foreach (object o in e)
            {
                Add(o);
            }
        }
        #region IEnumerable
        /// <summary>
        /// Iterate over the list.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return new ListEnumerator(this);
        }
        #endregion
        #region ICollection
        /// <summary>
        /// How many elements in the list?
        /// </summary>
        public int Count
        {
            get
            {
                return m_size;
            }
        }

        /// <summary>
        /// Is the list read-only?
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return m_readOnly;
            }
            set
            {
                m_readOnly = value;
            }
        }

        /// <summary>
        /// Is the list thread-safe?
        /// TODO: implement thread-safe
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return m_synch;
            }
            set
            {
                m_synch = value;
            }
        }

        /// <summary>
        /// The object to synchronize on.
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
        /// Copy this list to the given array.
        /// </summary>
        /// <param name="array">Array to copy into</param>
        /// <param name="index">Index to start copying at</param>
        public void CopyTo(Array array, int index)
        {
            int i = index;
            foreach (object o in this)
            {
                array.SetValue(o, i++);
            }
        }
        #endregion
        #region IList
        /// <summary>
        /// Walk the list to get the index'th element
        /// </summary>
        public object this[int index]
        {
            get
            {
                return GetNode(index).element;
            }
            set
            {
                GetNode(index).element = value;
            }
        }

        /// <summary>
        /// Insert an element at the end of the list
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int Add(object value)
        {
            AddBefore(value, m_header);
            return m_size-1;
        }

        /// <summary>
        /// Remove all of the elements.
        /// </summary>
        public void Clear()
        {
            m_modCount++;
            m_header.next = m_header.previous = m_header;
            m_size = 0;
        }

        /// <summary>
        /// Is the given object in the list?
        /// </summary>
        /// <param name="value">The object to find</param>
        /// <returns>True if the object is in the list</returns>
        public bool Contains(object value)
        {
            return IndexOf(value) != -1;
        }

        /// <summary>
        /// Where is the given object?
        /// </summary>
        /// <param name="value">The object to find</param>
        /// <returns>The position of the object in the list, or -1 if not found</returns>
        public int IndexOf(object value)
        {
            int index = 0;
            if (value == null)
            {
                for (Node e = m_header.next; e != m_header; e = e.next)
                {
                    if (e.element == null)
                        return index;
                    index++;
                }
            }
            else
            {
                for (Node e = m_header.next; e != m_header; e = e.next)
                {
                    if (value.Equals(e.element))
                        return index;
                    index++;
                }
            }
            return -1;
        }

        /// <summary>
        /// Insert an item into the list at the given index
        /// </summary>
        /// <param name="index">The position to insert before</param>
        /// <param name="value">The object to insert</param>
        public void Insert(int index, object value)
        {
            if (index >= m_size)
            {
                AddBefore(value, m_header);
            }
            else
            {
                Node n = GetNode(index);
                AddBefore(value, n);
            }
        }

        /// <summary>
        /// Always returns "false" for now.
        /// TODO: implement fixed size
        /// </summary>
        public bool IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Finds the first matching object, and removes it from the list.
        /// </summary>
        /// <param name="value">The object to remove</param>
        /// <exception cref="System.ArgumentException">Object not found</exception>
        public void Remove(object value)
        {
            if (value == null)
            {
                for (Node e = m_header.next; e != m_header; e = e.next)
                {
                    if (e.element == null)
                    {
                        Remove(e);
                        return;
                    }
                }
            }
            else
            {
                for (Node e = m_header.next; e != m_header; e = e.next)
                {
                    if (value.Equals(e.element))
                    {
                        Remove(e);
                        return;
                    }
                }
            }
            throw new ArgumentException("Object not found", "value");
        }

        /// <summary>
        /// Remove the index'th element from the list
        /// </summary>
        /// <param name="index">The index of the element to delete</param>
        public void RemoveAt(int index)
        {
            Node e = GetNode(index);
            Remove(e);
        }
        #endregion

        #region Queue
        /// <summary>
        /// Insert an element at the end of the list
        /// </summary>
        /// <param name="value"></param>
        public void Enqueue(object value)
        {
            AddBefore(value, m_header);
        }

        /// <summary>
        /// Remove and return the element at the front of the list
        /// </summary>
        /// <returns>The element at the end of the list</returns>
        public object Dequeue()
        {
            object value = m_header.next.element;
            Remove(m_header.next);
            return value;
        }

        /// <summary>
        /// Retrieve the element at the front of the list, without removing it.
        /// </summary>
        /// <returns></returns>
        public object Peek()
        {
            return m_header.next.element;
        }
        #endregion
        #region Stack
        /// <summary>
        /// Add an element to the front of the list.
        /// </summary>
        /// <param name="value"></param>
        public void Push(object value)
        {
            AddBefore(value, m_header.next);
        }

        /// <summary>
        /// Retrieve and remove the element at the front of the list.
        /// </summary>
        /// <returns></returns>
        public object Pop()
        {
            return Dequeue();
        }
        #endregion Stack
        #region private
        private Node GetNode(int index)
        {
            if ((index < 0) || (index >= m_size))
            {
                throw new IndexOutOfRangeException("Must choose index between 0 and " +
                                                   (m_size-1));
            }

            Node e = m_header;
            // start from end if closer
            if (index < m_size/2)
            {
                for (int i = 0; i <= index; i++)
                    e = e.next;
            }
            else
            {
                for (int i = m_size; i > index; i--)
                    e = e.previous;
            }
            return e;
        }

        private Node AddBefore(object value, Node n)
        {
            if (m_readOnly)
            {
                throw new InvalidOperationException("Can not add to read-only list");
            }

            if (m_synch)
            {
                lock (this)
                {
                    return UncheckedAdd(value, n);
                }
            }
            else
            {
                return UncheckedAdd(value, n);
            }
        }

        private Node UncheckedAdd(object value, Node n)
        {
            Node newNode = new Node(value, n, n.previous);
            newNode.previous.next = newNode;
            newNode.next.previous = newNode;
            m_size++;
            m_modCount++;
            return newNode;
        }

        private void Remove(Node n)
        {
            if (n == m_header)
                throw new InvalidOperationException("Deleting from an empty list");
            if (m_readOnly)
            {
                throw new InvalidOperationException("Can not add to read-only list");
            }

            if (m_synch)
            {
                lock (this)
                {
                    UncheckedRemove(n);
                }
            }
            else
            {
                UncheckedRemove(n);
            }
        }

        private void UncheckedRemove(Node n)
        {
            n.previous.next = n.next;
            n.next.previous = n.previous;
            m_size--;
            m_modCount++;
        }
        #endregion
        #region Object
        /// <summary>
        /// Comma-separated list of element.ToString()'s.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool first = true;
            foreach (object o in this)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                else
                {
                    first = false;
                }

                if (o == null)
                    sb.Append("null");
                else
                    sb.Append(o.ToString());
            }
            return sb.ToString();
        }
        #endregion
        #region newstuff
        /// <summary>
        /// Insert in order.  Returns the position of the insertion point.
        /// </summary>
        /// <param name="value"> </param>
        public int Insert(object value)
        {
            if (m_comparator == null)
            {
                m_comparator = Comparer.Default;
            }

            // null less than anything
            if (value == null)
            {
                AddBefore(null, m_header.next);
                return 0;
            }

            int index=0;
            int c;
            for (Node n = m_header.next; n != m_header; n = n.next, index++)
            {
                c = m_comparator.Compare(value, n.element);
                if (c < 0)
                {
                    AddBefore(value, n);
                    return index;
                }
                else if (c == 0) // equal.  replace
                {
                    n.element = value;
                    return index;
                }
            }

            // got to the end without inserting.  Put it on the end.
            AddBefore(value, m_header);
            return m_size-1;
        }

        /// <summary>
        /// The object to use for comparisons.
        /// </summary>
        public IComparer Comparator
        {
            get
            {
                return m_comparator;
            }

            set
            {
                m_comparator = value;
            }
        }

        /// <summary>
        /// Return a read-only linked list from the given enumeration.
        /// This doesn't seem all that useful to me.  An array might be
        /// a better choice.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static LinkedList ReadOnly(IEnumerable e)
        {
            LinkedList ll = new LinkedList(e);
            ll.m_readOnly = true;
            return ll;
        }
        #endregion
        #region enumerator
        private class ListEnumerator : IEnumerator
        {
            private LinkedList  list    = null;
            private Node        current = null;
            private int         mods    = -1;

            public ListEnumerator(LinkedList ll)
            {
                list    = ll;
                current = list.m_header;
                mods    = list.m_modCount;
            }

            public object Current
            {
                get
                {
                    if (list.m_modCount != mods)
                    {
                        throw new InvalidOperationException("Changed list during iterator");
                    }
                    return current.element;
                }
            }

            public bool MoveNext()
            {
                current = current.next;
                return current != list.m_header;
            }

            public void Reset()
            {
                current = list.m_header;
                mods = list.m_modCount;
            }
        }
        #endregion
        #region Node
        private class Node
        {
            public object element;
            public Node  next;
            public Node  previous;

            public Node(object element, Node next, Node previous)
            {
                this.element = element;
                this.next = next;
                this.previous = previous;
            }
        }
        #endregion
    }
}
