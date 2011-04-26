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
#define DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using CSharpTest.Net.Synchronization;
using NUnit.Framework;

namespace CSharpTest.Net.BPlusTree.Test
{
    [TestFixture]
    public class BasicTests
    {
        protected Random Random = new Random();

        protected virtual BPlusTree<int, string>.Options Options
        {
            get
            {
                return new BPlusTree<int, string>.Options(new PrimitiveSerializer(), new PrimitiveSerializer())
                {
                    BTreeOrder = 4,
                };
            }
        }

        [Test]
        public void TestEnumeration()
        {
            using (BPlusTree<int, string> data = new BPlusTree<int, string>(Options))
            {
                data.EnableCount();

                data.DebugSetOutput(new StringWriter());
                data.DebugSetValidateOnCheckpoint(true);

                for (int id = 0; id < 10; id++)
                    Assert.IsTrue(data.Add(id, id.ToString()));

                using (IEnumerator<KeyValuePair<int, string>> enu = data.GetEnumerator())
                {
                    Assert.IsTrue(enu.MoveNext());
                    Assert.AreEqual(0, enu.Current.Key);

                    for (int id = 2; id < 10; id++)
                        Assert.IsTrue(data.Remove(id));
                    for (int id = 6; id < 11; id++)
                        Assert.IsTrue(data.Add(id, id.ToString()));

                    Assert.IsTrue(enu.MoveNext());
                    Assert.AreEqual(1, enu.Current.Key);
                    Assert.IsTrue(enu.MoveNext());
                    Assert.AreEqual(6, enu.Current.Key);
                    Assert.IsTrue(enu.MoveNext());
                    Assert.AreEqual(7, enu.Current.Key);
                    Assert.IsTrue(data.Remove(8));
                    Assert.IsTrue(data.Remove(9));
                    Assert.IsTrue(data.Remove(10));
                    Assert.IsTrue(data.Add(11, 11.ToString()));
                    Assert.IsTrue(enu.MoveNext());
                    Assert.AreEqual(11, enu.Current.Key);
                    Assert.IsTrue(false == enu.MoveNext());
                }
                data.Clear();
            }
        }

        [Test]
        public void TestCounts()
        {
            using (BPlusTree<int, string> data = new BPlusTree<int, string>(Options))
            {
                Assert.AreEqual(int.MinValue, data.Count);
                data.EnableCount();

                Assert.AreEqual(0, data.Count);
                Assert.IsTrue(data.Add(1, "test"));
                Assert.AreEqual(1, data.Count);
                Assert.IsTrue(data.Add(2, "test"));
                Assert.AreEqual(2, data.Count);

                Assert.IsFalse(data.Add(2, "test"));
                Assert.AreEqual(2, data.Count);
                Assert.IsTrue(data.Remove(1));
                Assert.AreEqual(1, data.Count);
                Assert.IsTrue(data.Remove(2));
                Assert.AreEqual(0, data.Count);
            }
        }

        [Test]
        public void TestInserts()
        {
            using (BPlusTree<int, string> data = new BPlusTree<int, string>(Options))
            {
                data.EnableCount();

                int[][] TestArrays = new int[][]
                {
                    new int[] { 10, 18, 81, 121, 76, 31, 250, 174, 24, 38, 246, 79 },
                    new int[] { 110,191,84,218,170,217,199,232,184,254,32,90,241,136,181,28,226,69,52 },
                };

                foreach (int[] arry in TestArrays)
                {
                    data.Clear();
                    Assert.AreEqual(0, data.Count);

                    int count = 0;
                    foreach (int id in arry)
                    {
                        Assert.IsTrue(data.Add(id, id.ToString()));
                        Assert.AreEqual(++count, data.Count);
                    }

                    Assert.AreEqual(arry.Length, data.Count);
                    data.UnloadCache();

                    foreach (int id in arry)
                    {
                        Assert.AreEqual(id.ToString(), data[id]);
                        data[id] = String.Empty;
                        Assert.AreEqual(String.Empty, data[id]);
                        
                        Assert.IsTrue(data.Remove(id));
                        Assert.AreEqual(--count, data.Count);
                    }

                    Assert.AreEqual(0, data.Count);
                }
            }
        }

        [Test]
        public void RandomSequenceTest()
        {
            int iterations = 5;
            int limit = 255;

            using (BPlusTree<int, string> data = new BPlusTree<int, string>(Options))
            {
                data.EnableCount();

                List<int> numbers = new List<int>();
                while (iterations-- > 0)
                {
                    data.Clear();
                    numbers.Clear();
                    data.DebugSetValidateOnCheckpoint(true);

                    for (int i = 0; i < limit; i++)
                    {
                        int id = Random.Next(limit);
                        if (!numbers.Contains(id))
                        {
                            numbers.Add(id);
                            Assert.IsTrue(data.Add(id, "V" + id));
                        }
                    }

                    Assert.AreEqual(numbers.Count, data.Count);

                    foreach (int number in numbers)
                        Assert.IsTrue(data.Remove(number));

                    Assert.AreEqual(0, data.Count);
                }
            }
        }

        [Test]
        public void ExplicitRangeAddRemove()
        {
            string test;
            using (BPlusTree<int, string> data = new BPlusTree<int, string>(Options))
            {
                data.Add(2, "v2");
                data.Add(1, "v1");

                int i = 0;
                for (; i < 8; i++)
                    data.Add(i, "v" + i);
                for (i = 16; i >= 8; i--)
                    data.Add(i, "v" + i);
                data.Add(13, "v" + i);

                for (i = 0; i <= 16; i++)
                {
                    if (!data.TryGetValue(i, out test))
                        throw new ApplicationException();
                    Assert.AreEqual("v" + i, test);
                }

                data.Remove(1);
                data.Remove(3);
                IEnumerator<KeyValuePair<int, string>> e = data.GetEnumerator();
                Assert.IsTrue(e.MoveNext());
                Assert.AreEqual(0, e.Current.Key);
                data.Add(1, "v1");
                Assert.IsTrue(e.MoveNext());
                data.Add(3, "v3");
                Assert.IsTrue(e.MoveNext());
                data.Remove(8);
                Assert.IsTrue(e.MoveNext());
                e.Dispose();
                data.Add(8, "v8");

                i = 0;
                foreach (KeyValuePair<int, string> pair in data)
                    Assert.AreEqual(pair.Key, i++);

                for (i = 0; i <= 16; i++)
                    Assert.IsTrue(data.Remove(i) && data.Add(i, "v" + i));

                for (i = 6; i <= 12; i++)
                    Assert.IsTrue(data.Remove(i));

                for (i = 6; i <= 12; i++)
                {
                    Assert.IsFalse(data.TryGetValue(i, out test));
                    Assert.IsNull(test);
                }

                for (i = 0; i <= 5; i++)
                {
                    Assert.IsTrue(data.TryGetValue(i, out test));
                    Assert.AreEqual("v" + i, test);
                }

                for (i = 13; i <= 16; i++)
                {
                    Assert.IsTrue(data.TryGetValue(i, out test));
                    Assert.AreEqual("v" + i, test);
                }

                for (i = 0; i <= 16; i++)
                    Assert.AreEqual(i < 6 || i > 12, data.Remove(i));
            }
        }

        [Test]
        public void TestRandomAddRemoveOrder4()
        {
            TestRandomAddRemove(1, 4, 1000);
        }

        [Test]
        public void TestRandomAddRemoveOrder16()
        {
            TestRandomAddRemove(1, 16, 1000);
        }

        [Test]
        public void TestRandomAddRemoveOrder64()
        {
            TestRandomAddRemove(1, 64, 1000);
        }

        void TestRandomAddRemove(int repeat, int nodesz, int size)
        {
            List<int> keysAdded = new List<int>(250000);
            BPlusTree<int, string>.Options options = Options;
            options.LockingFactory = new IgnoreLockFactory();

            Dictionary<int, string> keys = new Dictionary<int, string>();

            for (; repeat > 0; repeat--)
            {
                keys.Clear();
                options.BTreeOrder = nodesz;
                using (BPlusTree<int, string> data = new BPlusTree<int, string>(options))
                {
                    data.EnableCount();

                    AddRandomKeys(size, keys, data);
                    IsSameList(keys, data);
                    keysAdded.Clear();

                    for (int tc = 0; tc < 1; tc++)
                    {
                        int del = keys.Count/3 + Random.Next(keys.Count/3);
                        RemoveRandomKeys(del, keys, data);
                        IsSameList(keys, data);

                        data.Validate();

                        AddRandomKeys(del, keys, data);
                        IsSameList(keys, data);

                        data.Validate();
                    }

                    keysAdded.Clear();

                    foreach (KeyValuePair<int, string> kv in data)
                        keysAdded.Add(kv.Key);

                    foreach (int k in keysAdded)
                    {
                        Assert.IsTrue(data.Remove(k));
                        Assert.IsTrue(data.Add(k, k.ToString()));
                        Assert.IsTrue(data.Remove(k));
                        string test;
                        Assert.IsFalse(data.TryGetValue(k, out test));
                        Assert.IsNull(test);
                    }
                }
            }
        }

        void RemoveRandomKeys(int count, Dictionary<int, string> keys, BPlusTree<int, string> data)
        {
            Stopwatch time = new Stopwatch();
            time.Start();

            int ix = 0;
            int[] del = new int[count];
            foreach (int k in keys.Keys)
            {
                del[ix++] = k;
                if (ix == del.Length) break;
            }

            foreach (int k in del)
                keys.Remove(k);
            if (data != null)
            {
                for (int i = 0; i < count; i++)
                    data.Remove(del[i]);
                data.Remove(del[0]);
            }
            Trace.TraceInformation("Removed {0} in {1}", count, time.ElapsedMilliseconds);
        }

        void AddRandomKeys(int count, Dictionary<int, string> keys, BPlusTree<int, string> data)
        {
            Stopwatch time = new Stopwatch();
            time.Start();

            for (int i = 0; i < count; i++)
            {
                int key = Random.Next(int.MaxValue);
                if (data.Add(key, key.ToString()))
                    keys.Add(key, key.ToString());
            }

            Trace.TraceInformation("Added {0} in {1}", count, time.ElapsedMilliseconds);
        }

        void IsSameList(Dictionary<int, string> keys, BPlusTree<int, string> data)
        {
            Stopwatch time = new Stopwatch();
            time.Start();

            Assert.AreEqual(keys.Count, data.Count);

            int count = 0;
            string test;
            foreach (int key in keys.Keys)
            {
                count++;
                Assert.IsTrue(data.TryGetValue(key, out test));
                Assert.IsTrue(test == key.ToString());
            }

            Trace.TraceInformation("Seek {0} in {1}", count, time.ElapsedMilliseconds);
        }
    }
}
