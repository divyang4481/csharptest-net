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

namespace CSharpTest.Net.Collections
{
    partial class BPlusTree<TKey, TValue>
    {
        enum InsertResult { Failed = 0, Inserted = 1, Updated = 2 }

        private InsertResult Insert(NodePin thisLock, TKey key, TValue value, bool allowUpdate)
        { return Insert(thisLock, key, value, allowUpdate, null, int.MinValue); }
        private InsertResult Insert(NodePin thisLock, TKey key, TValue value, bool allowUpdate, NodePin parent, int parentIx)
        {
            Node me = thisLock.Ptr;
            if (me.Count == me.Size && parent != null)
            {
                using (NodeTransaction trans = _storage.BeginTransaction())
                {
                    TKey splitAt;
                    if (parent.Ptr.IsRoot) //Is root node
                    {
                        Node rootNode = trans.BeginUpdate(parent);
                        using (NodePin newRoot = trans.Create(parent, false))
                        {
                            rootNode.ReplaceChild(0, thisLock.Handle, newRoot.Handle);

                            bool success = newRoot.Ptr.Insert(0, new Element(default(TKey), thisLock.Handle));
                            Assert(success, "Insertion to new root node failed.");

                            using (NodePin next = Split(trans, ref thisLock, newRoot, 0, out splitAt))
                            using (thisLock)
                            {
                                trans.Commit();
                                GC.KeepAlive(thisLock);
                                GC.KeepAlive(next);
                            }

                            return Insert(newRoot, key, value, allowUpdate, parent, parentIx);
                        }
                    }

                    trans.BeginUpdate(parent);
                    using (NodePin next = Split(trans, ref thisLock, parent, parentIx, out splitAt))
                    using (thisLock)
                    {
                        trans.Commit();

                        if (_keyComparer.Compare(key, splitAt) >= 0)
                        {
                            thisLock.Dispose();
                            return Insert(next, key, value, allowUpdate, parent, parentIx);
                        }
                        next.Dispose();
                        return Insert(thisLock, key, value, allowUpdate, parent, parentIx);
                    }
                }
            }
            if (parent != null)
                parent.Dispose();//done with the parent lock.

            int ordinal;
            if (me.BinarySearch(_itemComparer, new Element(key), out ordinal) && me.IsLeaf)
            {
                if (!allowUpdate)
                    return InsertResult.Failed;

                using (NodeTransaction trans = _storage.BeginTransaction())
                {
                    me = trans.BeginUpdate(thisLock);
                    me.SetValue(ordinal, key, value, _keyComparer);
                    trans.Commit();
                    return InsertResult.Updated;
                }
            }

            if (me.IsLeaf)
            {
                using (NodeTransaction trans = _storage.BeginTransaction())
                {
                    me = trans.BeginUpdate(thisLock);
                    if (me.Insert(ordinal, new Element(key, value)))
                    {
                        trans.Commit();
                        return InsertResult.Inserted;
                    }
                }
                return InsertResult.Failed;
            }

            if (ordinal >= me.Count) ordinal = me.Count - 1;
            using (NodePin child = _storage.Lock(thisLock, me[ordinal].ChildNode))
                return Insert(child, key, value, allowUpdate, thisLock, ordinal);
        }

        private NodePin Split(NodeTransaction trans, ref NodePin thisLock, NodePin parentLock, int parentIx, out TKey splitKey)
        {
            Node me = thisLock.Ptr;

            NodePin prev = trans.Create(parentLock, thisLock.Ptr.IsLeaf);
            NodePin next = trans.Create(parentLock, thisLock.Ptr.IsLeaf);
            try
            {
                int ix;
                int count = me.Count >> 1;

                for (ix = 0; ix < count; ix++)
                    prev.Ptr.Insert(prev.Ptr.Count, me[ix]);

                splitKey = me[count].Key;

                if (!thisLock.Ptr.IsLeaf)
                    next.Ptr.Insert(next.Ptr.Count, new Element(default(TKey), me[ix++].ChildNode));
                for (; ix < me.Count; ix++)
                    next.Ptr.Insert(next.Ptr.Count, me[ix]);

                parentLock.Ptr.ReplaceChild(parentIx, thisLock.Handle, prev.Handle);

                bool success = parentLock.Ptr.Insert(parentIx + 1, new Element(splitKey, next.Handle));
                Assert(success, "Insertion to parent failed.");

                trans.Destroy(thisLock);
                thisLock = prev;
                return next;
            }
            catch
            {
                prev.Dispose();
                next.Dispose();
                throw;
            }
        }
    }
}
