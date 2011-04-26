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
        private bool Seek(NodePin thisLock, TKey key, out NodePin pin, out int offset)
        {
            NodePin myPin = thisLock, nextPin = null;
            try
            {
                while (myPin != null)
                {
                    Node me = myPin.Ptr;

                    bool isValueNode = me.IsLeaf;
                    int ordinal;
                    if (me.BinarySearch(_itemComparer, new Element(key), out ordinal) && isValueNode)
                    {
                        pin = myPin;
                        myPin = null;
                        offset = ordinal;
                        return true;
                    }
                    if (isValueNode)
                        break; // not found.

                    nextPin = _storage.Lock(myPin, me[ordinal].ChildNode);
                    myPin.Dispose();
                    myPin = nextPin;
                    nextPin = null;
                }
            }
            finally
            {
                if (myPin != null) myPin.Dispose();
                if (nextPin != null) nextPin.Dispose(); 
            }

            pin = null;
            offset = -1;
            return false;
        }

        private bool Search(NodePin thisLock, TKey key, ref TValue value)
        {
            NodePin pin;
            int offset;
            if (Seek(thisLock, key, out pin, out offset))
                using (pin)
                {
                    value = pin.Ptr[offset].Payload;
                    return true;
                }
            return false;
        }

        private bool Update(NodePin thisLock, TKey key, TValue value)
        {
            NodePin pin;
            int offset;
            if (Seek(thisLock, key, out pin, out offset))
                using (pin)
                {
                    using (NodeTransaction trans = _storage.BeginTransaction())
                    {
                        trans.BeginUpdate(pin);
                        pin.Ptr.SetValue(offset, key, value, _keyComparer);
                        trans.Commit();
                        return true;
                    }
                }
            return false;
        }

        private bool Update(NodePin thisLock, TKey key, Converter<TValue, TValue> fnUpdate)
        {
            NodePin pin;
            int offset;
            if (Seek(thisLock, key, out pin, out offset))
                using (pin)
                {
                    TValue original = pin.Ptr[offset].Payload;
                    TValue value = Check.NotNull(fnUpdate)(original);
                    if ((value == null && original == null) ||
                        (value != null && value.Equals(original)))
                        return false;

                    using (NodeTransaction trans = _storage.BeginTransaction())
                    {
                        trans.BeginUpdate(pin);
                        pin.Ptr.SetValue(offset, key, value, _keyComparer);
                        trans.Commit();
                        return true;
                    }
                }
            return false;
        }

        private int CountValues(NodePin thisLock)
        {
            if (thisLock.Ptr.IsLeaf)
                return thisLock.Ptr.Count;

            int count = 0;
            for (int i = 0; i < thisLock.Ptr.Count; i++)
            {
                using (NodePin child = _storage.Lock(thisLock, thisLock.Ptr[i].ChildNode))
                    count += CountValues(child);
            }
            return count;
        }
    }
}
