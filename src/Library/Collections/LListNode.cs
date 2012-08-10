#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;

namespace CSharpTest.Net.Collections
{
    /// <summary>
    /// A typed node, used similiarly to LinkedListNode&lt;T>.  Can belong to single list
    /// at a time, the type of list is usually LListNode&lt;T>.LList
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LListNode<T> : LListNode
    {
        /// <summary> Constructs a node without a value </summary>
        public LListNode() { }
        /// <summary> Constructs a node with a value </summary>
        public LListNode(T value) { Value = value; }

        /// <summary> Gets/Sets the value of this node </summary>
        public T Value;

        /// <summary> Returns the previous item in the list or null </summary>
        public new LListNode<T> Previous { get { return (LListNode<T>)base.Previous; } }
        /// <summary> Returns the next item in the list or null </summary>
        public new LListNode<T> Next { get { return (LListNode<T>)base.Next; } }

        /// <summary> Provides a linked list of nodes of type T </summary>
        public new class LList : LList<LListNode<T>>
        {
            /// <summary> Adds the node to the front of the list </summary>
            public void AddFirst(T value) { AddFirst(new LListNode<T>(value)); }

            /// <summary> Adds the node to the end of the list </summary>
            public void AddLast(T value) { AddLast(new LListNode<T>(value)); }
        }
    }

    /// <summary>
    /// The basic Linked-list node, untyped, use LListNode&lt;T> for strong typed nodes and lists.
    /// </summary>
    public class LListNode
    {
        private LListNode _prev, _next;

        /// <summary> 
        /// You may derive an instance class from LListNode and use it in a single list, or you
        /// may construct a node&lt;T> with the value that the node should contain.
        /// </summary>
        protected LListNode() { }

        /// <summary> Returns the previous item in the list or null </summary>
        public LListNode Previous { get { return _prev; } }
        /// <summary> Returns the next item in the list or null </summary>
        public LListNode Next { get { return _next; } }

        /// <summary> Provides a linked list of nodes of type T </summary>
        public class LList : LList<LListNode> { }

        /// <summary> Provides a linked list of nodes of type T </summary>
        public class LList<TNode> : System.Collections.Generic.ICollection<TNode>
            where TNode : LListNode
        {
            int _count;
            TNode _head, _tail;
            /// <summary> Returns the number of items in the list </summary>
            public int Count { get { return _count; } }
            /// <summary> Returns true if the list is empty </summary>
            public bool IsEmpty { get { return _head == null; } }
            /// <summary> Returns the first node in the list </summary>
            public TNode First { get { return _head; } }
            /// <summary> Returns the last node in the list </summary>
            public TNode Last { get { return _tail; } }

            /// <summary> Adds the node to the front of the list </summary>
            public void AddFirst(TNode value)
            {
                Check.Assert<InvalidOperationException>(value._prev == null && value._next == null);
                value._prev = null;
                value._next = _head;
                if (_head != null) _head._prev = value;
                _head = value;
                _tail = _tail ?? value;
                _count++;
            }

            /// <summary> Adds the node to the end of the list </summary>
            public void AddLast(TNode value)
            {
                Check.Assert<InvalidOperationException>(value._prev == null && value._next == null);
                value._next = null;
                value._prev = _tail;
                if (_tail != null) _tail._next = value;
                _tail = value;
                _head = _head ?? value;
                _count++;
            }

            /// <summary> Remvoes the node from the list </summary>
            public bool Remove(TNode value)
            {
                int removed = 0;
                if (ReferenceEquals(_head, value) && ++removed > 0)
                    _head = (TNode)value._next;
                if (value._next != null && ++removed > 0)
                    value._next._prev = value._prev;

                if (ReferenceEquals(_tail, value) && ++removed > 0)
                    _tail = (TNode)value._prev;
                if (value._prev != null && ++removed > 0)
                    value._prev._next = value._next;

                value._next = value._prev = null;
                _count--;
                return removed > 0;
            }

            /// <summary> Remvoes all the nodes from the list </summary>
            public void Clear()
            {
                foreach (TNode item in this)
                    item._next = item._prev = null;
                _head = _tail = null;
                _count = 0;
            }

            /// <summary> Returns an enumerator that iterates through the collection. </summary>
            public System.Collections.Generic.IEnumerator<TNode> GetEnumerator() { return new Enumerator<LList<TNode>, TNode>(this); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return new Enumerator<LList<TNode>, TNode>(this); }

            void System.Collections.Generic.ICollection<TNode>.Add(TNode item)
            { AddLast(item); }

            bool System.Collections.Generic.ICollection<TNode>.Contains(TNode item)
            {
                while (item._prev != null)
                    item = (TNode)item._prev;
                return item == _head;
            }

            void System.Collections.Generic.ICollection<TNode>.CopyTo(TNode[] array, int arrayIndex)
            {
                foreach (TNode node in this)
                    array[arrayIndex++] = node;
            }

            bool System.Collections.Generic.ICollection<TNode>.IsReadOnly
            {
                get { return false; }
            }
        }

        /// <summary> Provides a enumeration of the list where it is acceptable to remove current </summary>
        private class Enumerator<TList, TNode> : System.Collections.Generic.IEnumerator<TNode>
            where TList : LList<TNode>
            where TNode : LListNode
        {
            TList _list;
            TNode _current, _next;

            /// <summary> Creates the enumeration on a list </summary>
            public Enumerator(TList list)
            {
                _list = list;
                _next = _list.First;
                _current = null;
            }

            object System.Collections.IEnumerator.Current { get { return Current; } }
            /// <summary> Returns the current node </summary>
            public TNode Current
            {
                get
                {
                    if (_list == null) throw new ObjectDisposedException(GetType().FullName);
                    if (_current == null) throw new InvalidOperationException();
                    return _current;
                }
            }

            /// <summary> Disposes of the enumeration </summary>
            public void Dispose() { _list = null; _next = _current = null; }

            /// <summary> Moves to the next element or returns false </summary>
            public bool MoveNext()
            {
                if (_list == null) throw new ObjectDisposedException(GetType().FullName);
                _current = _next;
                if (_current == null) return false;
                _next = (TNode)_current._next;
                return true;
            }

            /// <summary> Resets the enumeration back to the beginning of the list </summary>
            public void Reset()
            {
                if (_list == null) throw new ObjectDisposedException(GetType().FullName);
                _next = _list.First;
                _current = null;
            }
        }
    }
}
