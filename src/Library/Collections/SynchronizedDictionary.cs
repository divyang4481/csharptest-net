#region Copyright 2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Collections.Generic;
using CSharpTest.Net.Synchronization;

namespace CSharpTest.Net.Collections
{
    /// <summary>
    /// Represents a thread-safe generic collection of key/value pairs.
    /// </summary>
    public class SynchronizedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        IDictionary<TKey, TValue> _store;
        readonly ILockStrategy _lock;

        /// <summary>
        /// Constructs a thread-safe generic collection of key/value pairs using exclusive locking.
        /// </summary>
        public SynchronizedDictionary()
            : this(new Dictionary<TKey, TValue>(), new ExclusiveLocking())
        { }
        /// <summary>
        /// Constructs a thread-safe generic collection of key/value pairs using exclusive locking.
        /// </summary>
        public SynchronizedDictionary(IEqualityComparer<TKey> comparer)
            : this(new Dictionary<TKey, TValue>(comparer), new ExclusiveLocking())
        { }
        /// <summary>
        /// Constructs a thread-safe generic collection of key/value pairs using the lock provided.
        /// </summary>
        public SynchronizedDictionary(IEqualityComparer<TKey> comparer, ILockStrategy locking)
            : this(new Dictionary<TKey, TValue>(comparer), locking)
        { }
        /// <summary>
        /// Constructs a thread-safe generic collection of key/value pairs using the lock provided.
        /// </summary>
        public SynchronizedDictionary(ILockStrategy locking)
            : this(new Dictionary<TKey,TValue>(), locking)
        { }
        /// <summary>
        /// Constructs a thread-safe generic collection of key/value pairs using the default locking
        /// type for exclusive access, akin to placing lock(this) around each call.  If you want to
        /// allow reader/writer locking provide one of those lock types from the Synchronization
        /// namespace.
        /// </summary>
        public SynchronizedDictionary(IDictionary<TKey, TValue> storage)
            : this(storage, new ExclusiveLocking())
        { }

        /// <summary>
        /// Constructs a thread-safe generic collection of key/value pairs.
        /// </summary>
        public SynchronizedDictionary(IDictionary<TKey, TValue> storage, ILockStrategy locking)
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
        public IDictionary<TKey, TValue> ReplaceStorage(IDictionary<TKey, TValue> newStorage)
        {
            using (_lock.Write())
            {
                IDictionary<TKey, TValue> storage = _store;
                _store = Check.NotNull(newStorage);
                return storage;
            }
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                using (_lock.Read())
                    return _store[key];
            }
            set
            {
                using (_lock.Write())
                    _store[key] = value;
            }
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            using (_lock.Write())
                _store.Add(key, value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            using (_lock.Write())
                _store.Add(item);
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public bool Remove(TKey key)
        {
            using (_lock.Write())
                return _store.Remove(key);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            using (_lock.Write())
                return _store.Remove(item);
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        public void Clear()
        {
            using (_lock.Write())
                _store.Clear();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            using (_lock.Read())
                return _store.Contains(item);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            using (_lock.Read())
                return _store.ContainsKey(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            using (_lock.Read())
                return _store.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get
            {
                using (_lock.Read())
                    return new List<TKey>(_store.Keys);
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {
                using (_lock.Read())
                    return new List<TValue>(_store.Values);
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            using (_lock.Read())
                _store.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            using (_lock.Read())
            {
                foreach (KeyValuePair<TKey, TValue> kv in _store)
                    yield return kv;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }
    }
}