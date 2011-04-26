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
using System.Diagnostics;
using System.Threading;
using CSharpTest.Net.Synchronization;

namespace CSharpTest.Net.Collections
{
    /// <summary>
    /// Implements an IDictionary interface for a simple file-based database
    /// </summary>
    public sealed partial class BPlusTree<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        readonly Options _options;
        readonly NodeCacheBase _storage;
        readonly ILockStrategy _selfLock;
        readonly IComparer<TKey> _keyComparer;
        readonly IComparer<Element> _itemComparer;

        private bool _hasCount;
        private int _count;
        
        /// <summary>
        /// Constructs a BPlusTree
        /// </summary>
        public BPlusTree(Options options)
        {
            _options = options.Clone();
            _selfLock = _options.CallLevelLock;
            _keyComparer = options.KeyComparer;
            _itemComparer = new ElementComparer(_keyComparer);

            switch (options.CachePolicy)
            {
                case CachePolicy.All: _storage = new NodeCacheFull(options); break;
                case CachePolicy.Recent: _storage = new NodeCacheNormal(_options); break;
                case CachePolicy.None: _storage = new NodeCacheNone(_options); break;
                default: throw new InvalidConfigurationValueException("CachePolicy");
            }

            try
            {
                _storage.Load();
            }
            catch
            {
                _selfLock.Dispose();
                _storage.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Closes the storage and clears memory used by the instance
        /// </summary>
        public void Dispose()
        {
            using(_selfLock.Write(LockTimeout))
                _storage.Dispose();
            _selfLock.Dispose();
        }

        /// <summary>
        /// Defines the lock used to provide tree-level exclusive operations.  This should be set at the time of construction, or not at all since
        /// operations depending on this (Clear, EnableCount, and UnloadCache) may behave poorly if operations that started prior to setting this
        /// value are still being processed.  Out of the locks I've tested the ReaderWriterLocking implementation performs best here since it is
        /// a highly read-intensive lock.  All public APIs that access tree content will aquire this lock as a reader except the three exclusive 
        /// operations mentioned above.  This allows you to gain exclusive access and perform mass updates, atomic enumeration, etc.
        /// </summary>
        public ILockStrategy CallLevelLock
        {
            get { return _selfLock; }
        }

        /// <summary> See comments on EnableCount() for usage of this property </summary>
        public int Count { get { return _hasCount ? _count : int.MinValue; } }

        int LockTimeout { get { return _options.LockTimeout; } }

        /// <summary> 
        /// Due to the cost of upkeep, this must be enable each time the object is created via a call to
        /// EnableCount() which itself must be done before any writer threads are active for it to be
        /// accurate.  This requires that the entire tree be loaded (sequentially) in order to build
        /// the initial working count.  Once completed, members like Add() and Remove() will keep the
        /// initial count accurate.
        /// </summary>
        public void EnableCount()
        {
            _count = 0;
            using (RootLock root = LockRoot(LockType.Read, "EnableCount", true))
                _count = CountValues(root.Pin);
            _hasCount = true;
        }

        /// <summary>
        /// Safely removes all items from the in-memory cache.
        /// </summary>
        public void UnloadCache()
        {
            using (_selfLock.Write(LockTimeout))
                _storage.ResetCache();
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                TValue result;
                if (!TryGetValue(key, out result))
                    throw new IndexOutOfRangeException();
                return result;
            }
            set 
            {
                if (!AddOrUpdate(key, value))
                    Assert(false, "Unable to add or modify the item.");
            }
        }

        struct RootLock : IDisposable
        {
            readonly BPlusTree<TKey, TValue> _tree;
            readonly bool _exclusive;
            readonly string _methodName;
            private bool _locked;
            private NodeVersion _version;
            public readonly NodePin Pin;

            public RootLock(BPlusTree<TKey, TValue> tree, LockType type, bool exclusiveTreeAccess, string methodName)
            {
                _tree = tree;
                _version = type == LockType.Read ? tree._storage.CurrentVersion : null;
                _methodName = methodName;
                _exclusive = exclusiveTreeAccess;
                _locked = _exclusive ? _tree._selfLock.TryWrite(tree._options.LockTimeout) : _tree._selfLock.TryRead(tree._options.LockTimeout);
                Assert(_locked);
                Pin = _tree._storage.LockRoot(type);
            }
            void IDisposable.Dispose()
            {
                Pin.Dispose();

                if (_locked && _exclusive)
                    _tree._selfLock.ReleaseWrite();
                else if (_locked && !_exclusive)
                    _tree._selfLock.ReleaseRead();

                _locked = false;
                _tree._storage.ReturnVersion(ref _version);
            }
        }

        RootLock LockRoot(LockType ltype, string methodName) { return new RootLock(this, ltype, false, methodName); }
        RootLock LockRoot(LockType ltype, string methodName, bool exclusive) { return new RootLock(this, ltype, exclusive, methodName); }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        { return ContainsKey(item.Key); }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
        /// </summary>
        public bool ContainsKey(TKey key)
        { 
            TValue value; 
            return TryGetValue(key, out value); 
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            bool result;
            value = default(TValue);
            using (RootLock root = LockRoot(LockType.Read, "TryGetValue"))
                result = Search(root.Pin, key, ref value);
            DebugComplete("Found({0}) = {1}", key, result);
            return result;
        }

        /// <summary>
        /// Modify the value associated with the specified key.
        /// </summary>
        public bool Update(TKey key, TValue value)
        {
            bool result;
            using (RootLock root = LockRoot(LockType.Update, "Update"))
                result = Update(root.Pin, key, value);
            DebugComplete("Updated({0}) = {1}", key, result);
            return result;
        }

        /// <summary>
        /// Modify the value associated with the result of the provided update method
        /// as an atomic operation, Allows for reading/writing a single record within
        /// the tree lock.  Be cautious about the behavior and performance of the code 
        /// provided as it can cause a dead-lock to occur.  If the method returns an
        /// instance who .Equals the original, no update is applied.
        /// </summary>
        public bool Update(TKey key, Converter<TValue, TValue> fnUpdate)
        {
            bool result;
            using (RootLock root = LockRoot(LockType.Update, "Update"))
                result = Update(root.Pin, key, fnUpdate);
            DebugComplete("Updated({0}) = {1}", key, result);
            return result;
        }

        void ICollection<KeyValuePair<TKey,TValue>>.Add(KeyValuePair<TKey, TValue> item)
        { DuplicateKeyException.Assert(Add(item.Key, item.Value)); }
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        { DuplicateKeyException.Assert(Add(key, value)); }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public bool Add(TKey key, TValue value)
        {
            InsertResult result;
            using (RootLock root = LockRoot(LockType.Insert, "Add"))
                result = Insert(root.Pin, key, value, false);
            if (result == InsertResult.Inserted && _hasCount)
                Interlocked.Increment(ref _count);
            DebugComplete("Added({0}) = {1}", key, result);
            return result != InsertResult.Failed;
        }

        /// <summary>
        /// Adds or modifies an element with the provided key and value.
        /// </summary>
        public bool AddOrUpdate(TKey key, TValue value)
        {
            InsertResult result;
            using (RootLock root = LockRoot(LockType.Insert, "AddOrUpdate"))
                result = Insert(root.Pin, key, value, true);
            if (result == InsertResult.Inserted && _hasCount)
                Interlocked.Increment(ref _count);
            DebugComplete("Added({0}) = {1}", key, result);
            return result != InsertResult.Failed;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        { return Remove(item.Key); }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public bool Remove(TKey key)
        {
            bool result;
            using (RootLock root = LockRoot(LockType.Delete, "Remove"))
                result = Delete(root.Pin, key);
            if (result && _hasCount)
                Interlocked.Decrement(ref _count);
            DebugComplete("Removed({0}) = {1}", key, result);
            return result;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        { return new Enumerator(this); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return new Enumerator(this); }

        #region ICollection Members

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get
            {
                List<TKey> items = new List<TKey>();
                foreach (KeyValuePair<TKey, TValue> kv in this)
                    items.Add(kv.Key);
                return new System.Collections.ObjectModel.ReadOnlyCollection<TKey>(items);
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        public ICollection<TValue> Values
        {
            get 
            {
                List<TValue> items = new List<TValue>();
                foreach (KeyValuePair<TKey, TValue> kv in this)
                    items.Add(kv.Value);
                return new System.Collections.ObjectModel.ReadOnlyCollection<TValue>(items);
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<TKey, TValue> kv in this)
                array[arrayIndex++] = kv;
        }

        /// <summary>
        /// Removes all items from the collection and permanently destroys all storage.
        /// </summary>
        public void Clear()
        {
            using (_selfLock.Write(LockTimeout))
            {
                _storage.DeleteAll();
                _count = 0;
            }
            DebugComplete("Clear()");
        }

        bool ICollection<KeyValuePair<TKey,TValue>>.IsReadOnly
        { get { return false; } }

        #endregion

        [DebuggerNonUserCode]
        static void Assert(bool condition)
        {
            if (!condition)
                throw new AssertionFailedException();
        }

        [DebuggerNonUserCode]
        static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new AssertionFailedException(message);
        }
    }
}