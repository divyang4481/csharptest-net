#region Copyright 2011-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Collections.Generic;
using CSharpTest.Net.Synchronization;

namespace CSharpTest.Net.Collections
{
    /// <summary>
    /// Represents a collection of objects that can be individually accessed by index.
    /// </summary>
    public class SynchronizedList<T> : IList<T>
    {
        IList<T> _store;
        readonly ILockStrategy _lock;
        
        /// <summary>
        /// Constructs a thread-safe generic collection of key/value pairs using exclusive locking.
        /// </summary>
        public SynchronizedList()
            : this(new List<T>(), new ExclusiveLocking())
        { }
        /// <summary>
        /// Constructs a thread-safe generic collection of key/value pairs using the lock provided.
        /// </summary>
        public SynchronizedList(ILockStrategy locking)
            : this(new List<T>(), locking)
        { }
        /// <summary>
        /// Constructs a thread-safe generic collection of T, wrapped around the instance in storage
        /// using the default locking type for exclusive access, akin to placing lock(this) around 
        /// each call.  If you want to allow reader/writer locking provide one of those lock types 
        /// from the Synchronization namespace.
        /// </summary>
        public SynchronizedList(IList<T> storage)
            : this(storage, new ExclusiveLocking())
        { }
        /// <summary>
        /// Constructs a thread-safe generic collection of T, wrapped around the instance in storage
        /// </summary>
        public SynchronizedList(IList<T> storage, ILockStrategy locking)
        {
            _store = Check.NotNull(storage);
            _lock = Check.NotNull(locking);
        }

        /// <summary>
        /// Defines a method to release allocated resources.
        /// </summary>
        public void Dispose()
        {
            _lock.Dispose();
        }

        ///<summary> Exposes the interal lock so that you can syncronize several calls </summary>
        public ILockStrategy Lock { get { return _lock; } }
        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        public bool IsReadOnly { get { return _store.IsReadOnly; } }
        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        public int Count { get { using (_lock.Read()) return _store.Count; } }
        /// <summary>
        /// Locks the collection and replaces the underlying storage dictionary.
        /// </summary>
        public IList<T> ReplaceStorage(IList<T> newStorage)
        {
            using (_lock.Write())
            {
                IList<T> storage = _store;
                _store = Check.NotNull(newStorage);
                return storage;
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                using (_lock.Read())
                    return _store[index];
            }
            set
            {
                using (_lock.Write())
                    _store[index] = value;
            }
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        public int Add(T item)
        {
            using (_lock.Write())
            {
                _store.Add(item);
                return _store.Count - 1;
            }
        }

        /// <summary>
        /// Added the public version to return the ordinal since you cannot depend upon the collection being 
        /// unmodified to determine the index either before or after the Add() call.
        /// </summary>
        void ICollection<T>.Add(T item)
        {
            using (_lock.Write())
                _store.Add(item);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
        /// </summary>
        public void Insert(int index, T item)
        {
            using (_lock.Write())
                _store.Insert(index, item);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        public bool Remove(T item)
        {
            using (_lock.Write())
                return _store.Remove(item);
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            using (_lock.Write())
                _store.RemoveAt(index);
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        public void Clear()
        {
            using (_lock.Write())
                _store.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IList`1"/> contains the element specified.
        /// </summary>
        public bool Contains(T item)
        {
            using (_lock.Read())
                return _store.Contains(item);
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        public int IndexOf(T item)
        {
            using (_lock.Read())
                return _store.IndexOf(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            using (_lock.Read())
                _store.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            using (_lock.Read())
            {
                foreach (T value in _store)
                    yield return value;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }
    }
}
