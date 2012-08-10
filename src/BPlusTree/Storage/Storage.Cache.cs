#region Copyright 2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Threading;
using CSharpTest.Net.Storage;
using CSharpTest.Net.Interfaces;
using CSharpTest.Net.Serialization;
using CSharpTest.Net.Synchronization;

namespace CSharpTest.Net.Collections
{
    partial class BPlusTree<TKey, TValue>
    {
        class StorageCache : INodeStorage, ITransactable
        {
            enum StoreAction { None, Write }
            class StorageInfo
            {
                private readonly IStorageHandle _handle;
                public StorageInfo(IStorageHandle handle, Node node)
                {
                    _handle = handle;
                    Node = node;
                    Action = StoreAction.None;
                    RefCount = 0;
                }

                public int RefCount;
                public Node Node;
                public StoreAction Action;
                public IStorageHandle Handle { get { return _handle; } }
            }

            private readonly INodeStorage _store;
            private readonly int _sizeLimit;
            private ILockStrategy _lock;
            private Dictionary<IStorageHandle, StorageInfo> _cache;
            private Queue<IStorageHandle> _ordered;

            ISerializer<Node> _serializer;

            public StorageCache(INodeStorage store, int sizeLimit)
            {
                _store = store;
                _sizeLimit = sizeLimit;
                _lock = new SimpleReadWriteLocking();
                _cache = new Dictionary<IStorageHandle, StorageInfo>();
                _ordered = new Queue<IStorageHandle>();
            }

            public void Dispose()
            {
                _store.Dispose();
            }

            public void Commit()
            {
                Flush();
                ITransactable tstore = _store as ITransactable;
                if (tstore != null)
                    tstore.Commit();
            }

            public void Rollback()
            {
                ITransactable tstore = _store as ITransactable;
                if (tstore != null)
                {
                    using (_lock.Write())
                    {
                        _ordered.Clear();
                        _cache.Clear();
                        tstore.Rollback();
                    }
                }
            }

            public void Reset()
            {
                using (_lock.Write())
                {
                    _ordered.Clear();
                    _cache.Clear();
                    _store.Reset();
                }
            }

            public IStorageHandle OpenRoot(out bool isNew)
            {
                return _store.OpenRoot(out isNew);
            }

            public IStorageHandle Create()
            {
                return _store.Create();
            }

            private void Flush()
            {
                using (_lock.Write())
                {
                    Flush(0);
                    _ordered.Clear();
                    foreach (StorageInfo item in _cache.Values)
                    {
                        if (item.Action == StoreAction.Write)
                            _store.Update(item.Handle, _serializer, item.Node);
                    }
                    _cache.Clear();
                }
            }

            private void Flush(int maxBacklog)
            {
                while (_cache.Count >= maxBacklog && _ordered.Count > 0)
                {
                    IStorageHandle hremove = _ordered.Dequeue();
                    StorageInfo remove;
                    if (!_cache.TryGetValue(hremove, out remove))
                        continue;
                    int refCount = Interlocked.Decrement(ref remove.RefCount);
                    if (refCount == 0)
                    {
                        _cache.Remove(hremove);
                        if (remove.Action == StoreAction.Write)
                            _store.Update(remove.Handle, _serializer, remove.Node);
                    }
                }
            }

            private void CacheAdd(IStorageHandle handle, StorageInfo storageInfo)
            {
                Flush(_sizeLimit);
                _cache.Add(handle, storageInfo);
            }

            public bool TryGetNode<TNode>(IStorageHandle handle, out TNode tnode, ISerializer<TNode> serializer)
            {
                if (_serializer == null) _serializer = (ISerializer<Node>) serializer;
                StorageInfo info;
                Node node;

                using (_lock.Read())
                {
                    if (_cache.TryGetValue(handle, out info))
                    {
                        tnode = (TNode)(object)info.Node;
                        return true;
                    }
                }
                using (_lock.Write())
                {
                    if (_cache.TryGetValue(handle, out info))
                    {
                        tnode = (TNode) (object) info.Node;
                        Interlocked.Increment(ref info.RefCount);
                        _ordered.Enqueue(handle);
                        return true;
                    }

                    if (!_store.TryGetNode(handle, out tnode, serializer))
                        return false;
                    node = (Node)(object)tnode;
                    CacheAdd(handle, info = new StorageInfo(handle, node));

                    tnode = (TNode)(object)info.Node;
                    Interlocked.Increment(ref info.RefCount);
                    _ordered.Enqueue(handle);
                }
                return true;
            }

            public void Update<TNode>(IStorageHandle handle, ISerializer<TNode> serializer, TNode tnode)
            {
                if (_serializer == null) _serializer = (ISerializer<Node>)serializer;
                StorageInfo info;
                Node node = (Node)(object)tnode;

                using (_lock.Write())
                {
                    if (_cache.TryGetValue(handle, out info))
                    {
                        Interlocked.Increment(ref info.RefCount);
                        info.Action = StoreAction.Write;
                        info.Node = node;
                        Interlocked.Increment(ref info.RefCount);
                        _ordered.Enqueue(handle);
                        return;
                    }

                    CacheAdd(handle, info = new StorageInfo(handle, node));
                    info.Action = StoreAction.Write;
                    info.Node = node;
                    Interlocked.Increment(ref info.RefCount);
                    _ordered.Enqueue(handle);
                }
            }

            public void Destroy(IStorageHandle handle)
            {
                using (_lock.Write())
                {
                    _cache.Remove(handle);
                }
                _store.Destroy(handle);
            }

            public IStorageHandle ReadFrom(System.IO.Stream stream)
            {
                return _store.ReadFrom(stream);
            }

            public void WriteTo(IStorageHandle value, System.IO.Stream stream)
            {
                _store.WriteTo(value, stream);
            }
        }
    }
}
