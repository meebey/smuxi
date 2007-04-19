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
    /// A basic balanced tree implementation.  Yes, it seems like
    /// this might have been something nice to have been in
    /// System.Collections.  Not yet complete, but the algorithmic
    /// stuff is here.
    /// </summary>
    [SVN(@"$Id: Tree.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Tree : IEnumerable, IDictionary
    {
        private Node      root       = null;
        private int       size       = 0;
        private int       modCount   = 0;
        private IComparer comparator = System.Collections.Comparer.Default;
        //private bool      readOnly   = false;
        //private bool      synch      = false;
        /// <summary>
        /// Construct an empty tree
        /// </summary>
        public Tree()
        {
            //
            // TODO: Add Constructor Logic here
            //
        }
        #region IEnumerable
        /// <summary>
        /// Iterate over the tree
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new TreeEnumerator(this);
        }
        #endregion
        #region ICollection
        /// <summary>
        /// The number of items in the tree.
        /// </summary>
        public int Count
        {
            get
            {
                return size;
            }
        }

        /// <summary>
        /// Is the tree synchronized.  Always false for now.
        /// </summary>
        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///  Copy the values from the tree to the specified array in the order of the keys.
        /// </summary>
        /// <param name="array">The array to copy into</param>
        /// <param name="index">The index to start at</param>
        public void CopyTo(System.Array array, int index)
        {
            int i = index;
            foreach (DictionaryEntry o in this)
            {
                array.SetValue(o.Value, i++);
            }
        }

        /// <summary>
        /// An object to synch on.  Always returns null for now.
        /// </summary>
        public object SyncRoot
        {
            get
            {
                return null;
            }
        }

        #endregion
        #region IDictionary
        /// <summary>
        /// Add an item to the tree
        /// </summary>
        /// <param name="key">The key for the item</param>
        /// <param name="value">The data to store with this key</param>
        /// <exception cref="ArgumentException">Thrown if the same key is added twice</exception>
        /// <exception cref="ArgumentNullException">Thrown if key is null</exception>
        public void Add(object key,object value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            Node n = root;

            if (n == null)
            {
                sizeUp();
                root = new Node(key, value, null);
                return;
            }

            while (true)
            {
                int cmp = comparator.Compare(key, n.key);
                if (cmp == 0)
                {
                    //n.value = value;
                    //return;
                    throw new ArgumentException("Can't add the same key twice", "key");
                }
                else if (cmp < 0)
                {
                    if (n.left != null)
                    {
                        n = n.left;
                    }
                    else
                    {
                        sizeUp();
                        n.left = new Node(key, value, n);
                        fixAfterInsertion(n.left);
                        return;
                    }
                }
                else // cmp > 0
                {
                    if (n.right != null) {
                        n = n.right;
                    }
                    else
                    {
                        sizeUp();
                        n.right = new Node(key, value, n);
                        fixAfterInsertion(n.right);
                        return;
                    }
                }
            }

        }

        /// <summary>
        /// Remove all values from the tree.
        /// </summary>
        public void Clear()
        {
            modCount++;
            size = 0;
            root = null;
        }

        /// <summary>
        /// Is the given key found in the tree?
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <returns></returns>
        public bool Contains(object key)
        {
            return getNode(key) != null;
        }

        /// <summary>
        /// Return a dictionary enumerator.
        /// </summary>
        /// <returns>a dictionary enumerator</returns>
        public IDictionaryEnumerator GetEnumerator()
        {
            return new TreeEnumerator(this);
        }

        /// <summary>
        /// Remove the element from the tree associated
        /// with this key, possibly rebalancing.
        /// </summary>
        /// <param name="key"></param>
        public void Remove(object key)
        {
            Node n = getNode(key);
            if (n == null)
            {
                return;
            }
            sizeDown();

            // If strictly internal, first swap position with successor.
            if ((n.left != null) && (n.right != null))
            {
                Node s = successor(n);
                swapPosition(s, n);
            }

            // Start fixup at replacement node, if it exists.
            Node replacement = ((n.left != null) ? n.left : n.right);

            if (replacement != null)
            {
                // Link replacement to parent
                replacement.parent = n.parent;
                if (n.parent == null)
                    root = replacement;
                else if (n == n.parent.left)
                    n.parent.left = replacement;
                else
                    n.parent.right = replacement;

                // Null out links so they are OK to use by fixAfterDeletion.
                n.left = n.right = n.parent = null;

                // Fix replacement
                if (n.color == NodeColor.BLACK)
                fixAfterDeletion(replacement);
            }
            else if (n.parent == null)
            {
                root = null;
            }
            else
            {
                if (n.color == NodeColor.BLACK)
                    fixAfterDeletion(n);

                if (n.parent != null)
                {
                    if (n == n.parent.left)
                        n.parent.left = null;
                    else if (n == n.parent.right)
                        n.parent.right = null;
                    n.parent = null;
                }
            }
        }

        /// <summary>
        /// Retrieve the value associated with the given key.
        /// </summary>
        public object this[object key]
        {
            get
            {
                Node n = getNode(key);
                if (n == null)
                    return null;
                return n.value;
            }
            set
            {
                Add(key, value);
            }
        }

        /// <summary>
        /// Retrieve a list of keys.
        /// </summary>
        public ICollection Keys
        {
            get
            {
                object[] keys = new object[Count];
                int i=0;
                if (root == null)
                    return keys;
                Node n = first(root);
                while (n != null)
                {
                    keys[i++] = n.key;
                    n = successor(n);
                }
                return keys;
            }
        }

        /// <summary>
        /// Retrieve a list of values.
        /// </summary>
        public ICollection Values
        {
            get
            {
                object[] vals = new object[Count];
                int i=0;
                Node n = first(root);
                while (n != null)
                {
                    vals[i++] = n.value;
                    n = successor(n);
                }
                return vals;
            }
        }

        /// <summary>
        /// Always returns false for now.
        /// </summary>
        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Always returns false for now.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        #endregion
        #region Object
        /// <summary>
        /// Retrieve a string representation of the tree
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool f = true;
            for (Node n = first(root); n != null; n = successor(n))
            {
                if (!f)
                {
                    sb.Append(", ");
                }
                else
                {
                    f = false;
                }

                sb.AppendFormat("{0}={1}", n.key, n.value);
            }
            return sb.ToString();
        }

        #endregion
        /// <summary>
        /// Retrieve a string representation of the tree.
        /// Nice for debugging, but otherwise useless.
        /// </summary>
        /// <returns></returns>
        public string Structure()
        {
            return root.ToString();
        }
        #region hidden
        private static Node first(Node n)
        {
            if (n != null)
            {
                while (n.left != null)
                    n = n.left;
            }
            return n;
        }

        private static Node successor(Node t)
        {
            if (t == null)
                return null;

            if (t.right != null)
            {
                Node n = t.right;
                while (n.left != null)
                    n = n.left;
                return n;
            }

            Node p = t.parent;
            Node ch = t;
            while (p != null && ch == p.right)
            {
                ch = p;
                p = p.parent;
            }
            return p;
        }

        private Node getNode(object key)
        {
            Node n = root;
            while (n != null)
            {
                int cmp = comparator.Compare(key, n.key);
                if (cmp == 0)
                    return n;
                else if (cmp < 0)
                    n = n.left;
                else
                    n = n.right;
            }
            return null;
        }

        private void swapPosition(Node x, Node y)
        {
            Node px = x.parent;
            Node lx = x.left;
            Node rx = x.right;
            Node py = y.parent;
            Node ly = y.left;
            Node ry = y.right;
            bool xWasLeftChild = (px != null) && (x == px.left);
            bool yWasLeftChild = (py != null) && (y == py.left);

            if (x == py)
            {
                x.parent = y;
                if (yWasLeftChild)
                {
                    y.left = x;
                    y.right = rx;
                }
                else
                {
                    y.right = x;
                    y.left = lx;
                }
            }
            else
            {
                x.parent = py;
                if (py != null)
                {
                    if (yWasLeftChild)
                        py.left = x;
                    else
                        py.right = x;
                }
                y.left = lx;
                y.right = rx;
            }

            if (y == px)
            {
                y.parent = x;
                if (xWasLeftChild)
                {
                    x.left = y;
                    x.right = ry;
                }
                else
                {
                    x.right = y;
                    x.left = ly;
                }
            }
            else
            {
                y.parent = px;
                if (px != null)
                {
                    if (xWasLeftChild)
                        px.left = y;
                    else
                        px.right = y;
                }
                x.left = ly;
                x.right = ry;
            }

            if (x.left != null)
                x.left.parent = x;
            if (x.right != null)
                x.right.parent = x;
            if (y.left != null)
                y.left.parent = y;
            if (y.right != null)
                y.right.parent = y;

            NodeColor c = x.color;
            x.color = y.color;
            y.color = c;

            if (root == x)
                root = y;
            else if (root == y)
                root = x;
        }

        private static NodeColor colorOf(Node n)
        {
            return (n == null ? NodeColor.BLACK : n.color);
        }

        private static Node  parentOf(Node n)
        {
            return (n == null ? null: n.parent);
        }

        private static void setColor(Node n, NodeColor c)
        {
            if (n != null)
                n.color = c;
        }

        private static Node leftOf(Node n)
        {
            return (n == null)? null: n.left;
        }

        private static Node rightOf(Node n)
        {
            return (n == null)? null: n.right;
        }

        private void rotateLeft(Node n)
        {
            Node r = n.right;
            n.right = r.left;
            if (r.left != null)
                r.left.parent = n;
            r.parent = n.parent;

            if (n.parent == null)
                root = r;
            else if (n.parent.left == n)
                n.parent.left = r;
            else
                n.parent.right = r;

            r.left   = n;
            n.parent = r;
        }

        private void rotateRight(Node n)
        {
            Node l = n.left;
            n.left = l.right;
            if (l.right != null)
                l.right.parent = n;
            l.parent = n.parent;

            if (n.parent == null)
                root = l;
            else if (n.parent.right == n)
                n.parent.right = l;
            else
                n.parent.left = l;

            l.right = n;
            n.parent = l;
        }


        private void fixAfterInsertion(Node n)
        {
            n.color = NodeColor.RED;

            while ((n != null) &&
                   (n != root) &&
                   (n.parent.color == NodeColor.RED))
            {
                if (parentOf(n) == leftOf(parentOf(parentOf(n))))
                {
                    Node y = rightOf(parentOf(parentOf(n)));
                    if (colorOf(y) == NodeColor.RED)
                    {
                        setColor(parentOf(n), NodeColor.BLACK);
                        setColor(y, NodeColor.BLACK);
                        setColor(parentOf(parentOf(n)), NodeColor.RED);
                        n = parentOf(parentOf(n));
                    }
                    else
                    {
                        if (n == rightOf(parentOf(n)))
                        {
                            n = parentOf(n);
                            rotateLeft(n);
                        }

                        setColor(parentOf(n), NodeColor.BLACK);
                        setColor(parentOf(parentOf(n)), NodeColor.RED);
                        if (parentOf(parentOf(n)) != null)
                            rotateRight(parentOf(parentOf(n)));
                    }
                }
                else
                {
                    Node y = leftOf(parentOf(parentOf(n)));
                    if (colorOf(y) == NodeColor.RED)
                    {
                        setColor(parentOf(n), NodeColor.BLACK);
                        setColor(y, NodeColor.BLACK);
                        setColor(parentOf(parentOf(n)), NodeColor.RED);
                        n = parentOf(parentOf(n));
                    }
                    else
                    {
                        if (n == leftOf(parentOf(n)))
                        {
                            n = parentOf(n);
                            rotateRight(n);
                        }
                        setColor(parentOf(n),  NodeColor.BLACK);
                        setColor(parentOf(parentOf(n)), NodeColor.RED);
                        if (parentOf(parentOf(n)) != null)
                            rotateLeft(parentOf(parentOf(n)));
                    }
                }
            }
            root.color = NodeColor.BLACK;
        }

        private void fixAfterDeletion(Node x)
        {
            while ((x != root) && (colorOf(x) == NodeColor.BLACK))
            {
                if (x == leftOf(parentOf(x)))
                {
                    Node sib = rightOf(parentOf(x));

                    if (colorOf(sib) == NodeColor.RED)
                    {
                        setColor(sib, NodeColor.BLACK);
                        setColor(parentOf(x), NodeColor.RED);
                        rotateLeft(parentOf(x));
                        sib = rightOf(parentOf(x));
                    }

                    if ((colorOf(leftOf(sib))  == NodeColor.BLACK) &&
                        (colorOf(rightOf(sib)) == NodeColor.BLACK))
                    {
                        setColor(sib,  NodeColor.RED);
                        x = parentOf(x);
                    }
                    else
                    {
                        if (colorOf(rightOf(sib)) == NodeColor.BLACK)
                        {
                            setColor(leftOf(sib), NodeColor.BLACK);
                            setColor(sib, NodeColor.RED);
                            rotateRight(sib);
                            sib = rightOf(parentOf(x));
                        }
                        setColor(sib, colorOf(parentOf(x)));
                        setColor(parentOf(x), NodeColor.BLACK);
                        setColor(rightOf(sib), NodeColor.BLACK);
                        rotateLeft(parentOf(x));
                        x = root;
                    }
                }
                else
                {
                    Node sib = leftOf(parentOf(x));

                    if (colorOf(sib) == NodeColor.RED)
                    {
                        setColor(sib, NodeColor.BLACK);
                        setColor(parentOf(x), NodeColor.RED);
                        rotateRight(parentOf(x));
                        sib = leftOf(parentOf(x));
                    }

                    if (colorOf(rightOf(sib)) == NodeColor.BLACK &&
                        colorOf(leftOf(sib)) == NodeColor.BLACK)
                    {
                        setColor(sib,  NodeColor.RED);
                        x = parentOf(x);
                    }
                    else
                    {
                        if (colorOf(leftOf(sib)) == NodeColor.BLACK) {
                        setColor(rightOf(sib), NodeColor.BLACK);
                        setColor(sib, NodeColor.RED);
                        rotateLeft(sib);
                        sib = leftOf(parentOf(x));
                        }
                        setColor(sib, colorOf(parentOf(x)));
                        setColor(parentOf(x), NodeColor.BLACK);
                        setColor(leftOf(sib), NodeColor.BLACK);
                        rotateRight(parentOf(x));
                        x = root;
                    }
                }
            }

            setColor(x, NodeColor.BLACK);
        }

        private void sizeUp()   { modCount++; size++; }
        private void sizeDown() { modCount++; size--; }
        #endregion
        #region node
        private enum NodeColor : byte
        {
            RED,
            BLACK
        }

        private class Node
        {
            public object key;
            public object value;
            public Node left = null;
            public Node right = null;
            public Node parent;
            public NodeColor color = NodeColor.BLACK;

            public Node(object key, object value, Node parent)
            {
                this.key = key;
                this.value = value;
                this.parent = parent;
            }

            public override bool Equals(object o)
            {
                Node n = o as Node;
                if (n == null)
                {
                    return false;
                }
                return (key == n.key) && (value == n.value);
            }

            public override int GetHashCode()
            {
                return key.GetHashCode();
            }

            public override string ToString()
            {
                //return key + "=" + value + " (" + color + ")";
                if ((left==null) && (right==null))
                {
                    return key.ToString();
                }
                return key + " (" +
                    ((left == null) ? "null" : left.ToString()) + "," +
                    ((right == null) ? "null" : right.ToString()) + ")";
            }

        }
        #endregion
        #region enumerator
        private class TreeEnumerator : IDictionaryEnumerator
        {
            private Tree  tree    = null;
            private Node  current = null;
            private int   mods    = -1;

            public TreeEnumerator(Tree t) : this(t, t.root)
            {
            }

            public TreeEnumerator(Tree t, Node n)
            {
                tree    = t;
                mods    = t.modCount;

                // foreach calls MoveNext before starting.  Create a dummy node before the first one.
                current = new Node(null, null, null);
                current.right = first(n);
            }

            public object Current
            {
                get
                {
                    if (tree.modCount != mods)
                    {
                        throw new InvalidOperationException("Changed list during iterator");
                    }
                    return Entry;
                }
            }

            public bool MoveNext()
            {
                current = successor(current);
                return current != null;
            }

            public void Reset()
            {
                current = first(tree.root);
                mods = tree.modCount;
            }

            public object Key
            {
                get
                {
                    if (tree.modCount != mods)
                    {
                        throw new InvalidOperationException("Changed list during iterator");
                    }
                    return current.key;
                }
            }

            public object Value
            {
                get
                {
                    if (tree.modCount != mods)
                    {
                        throw new InvalidOperationException("Changed list during iterator");
                    }
                    return current.value;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry(current.key, current.value);
                }
            }
        }
        #endregion
    }
}
