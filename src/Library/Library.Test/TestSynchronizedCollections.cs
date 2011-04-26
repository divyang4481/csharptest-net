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
using CSharpTest.Net.Collections;
using CSharpTest.Net.Synchronization;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestSynchronizedDictionary : TestGenericCollection<SynchronizedDictionary<int,string>, KeyValuePair<int,string>>
    {
        protected override KeyValuePair<int, string>[] GetSample()
        {
            return new[] 
            {
                new KeyValuePair<int,string>(1, "1"),
                new KeyValuePair<int,string>(3, "3"),
                new KeyValuePair<int,string>(5, "5"),
                new KeyValuePair<int,string>(7, "7"),
                new KeyValuePair<int,string>(9, "9"),
                new KeyValuePair<int,string>(11, "11"),
                new KeyValuePair<int,string>(13, "13"),
            };
        }

        [Test]
        public void TestAddRemoveByKey()
        {
            SynchronizedDictionary<int, string> test = new SynchronizedDictionary<int, string>(new IgnoreLocking());
            for (int i = 0; i < 10; i++)
                test.Add(i, i.ToString());
            
            for (int i = 0; i < 10; i++)
                Assert.IsTrue(test.ContainsKey(i));

            string cmp;
            for (int i = 0; i < 10; i++)
                Assert.IsTrue(test.TryGetValue(i, out cmp) && cmp == i.ToString());

            for (int i = 0; i < 10; i++)
                Assert.IsTrue(test.Remove(i));
        }

        [Test]
        public void TestComparer()
        {
            SynchronizedDictionary<string, string> test = new SynchronizedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            test["a"] = "b";
            Assert.IsTrue(test.ContainsKey("A"));

            test = new SynchronizedDictionary<string, string>(StringComparer.OrdinalIgnoreCase, new IgnoreLocking());
            test["a"] = "b";
            Assert.IsTrue(test.ContainsKey("A"));
        }

        [Test]
        public void TestKeys()
        {
            SynchronizedDictionary<string, string> test = new SynchronizedDictionary<string, string>(new Dictionary<string,string>(), new IgnoreLocking());
            test["a"] = "b";
            string all = String.Join("", new List<string>(test.Keys).ToArray());
            Assert.AreEqual("a", all);
        }
        [Test]
        public void TestValues()
        {
            SynchronizedDictionary<string, string> test = new SynchronizedDictionary<string, string>(new Dictionary<string, string>());
            test["a"] = "b";
            string all = String.Join("", new List<string>(test.Values).ToArray());
            Assert.AreEqual("b", all);
        }
        [Test]
        public void TestReplaceDictionary()
        {
            SynchronizedDictionary<string, string> test = new SynchronizedDictionary<string, string>(StringComparer.Ordinal, new IgnoreLocking());
            test["a"] = "b";
            Assert.AreEqual(1, test.Count);
            test.ReplaceStorage(new Dictionary<string, string>());
            Assert.AreEqual(0, test.Count);
        }

        [Test]
        public void TestLock()
        {
            LockCounterFactory<SimpleReadWriteLocking> factory = new LockCounterFactory<SimpleReadWriteLocking>();
            ILockStrategy lck = factory.Create();
            SynchronizedDictionary<int, string> test = new SynchronizedDictionary<int, string>(lck);

            Assert.IsTrue(ReferenceEquals(lck, test.Lock));
            test.Add(42, "42");
            Assert.AreEqual(1, factory.TotalWriterCount);
            Assert.AreEqual(0, factory.TotalReaderCount);

            test[42] = "51";
            Assert.AreEqual(2, factory.TotalWriterCount);

            test.Add(1, "52");
            Assert.AreEqual(3, factory.TotalWriterCount);

            test.Remove(-1);
            Assert.AreEqual(4, factory.TotalWriterCount);

            test.Remove(1);
            Assert.AreEqual(5, factory.TotalWriterCount);

            Assert.AreEqual("51", test[42]);
            Assert.AreEqual(1, factory.TotalReaderCount);

            foreach (KeyValuePair<int, string> i in test)
                GC.KeepAlive(i);
            Assert.AreEqual(2, factory.TotalReaderCount);

            Assert.AreEqual(false, test.ContainsKey(-1));
            Assert.AreEqual(3, factory.TotalReaderCount);

            Assert.AreEqual(1, test.Count);
            Assert.AreEqual(4, factory.TotalReaderCount);

            string cmp;
            Assert.IsTrue(test.TryGetValue(42, out cmp) && cmp == "51");
            Assert.AreEqual(5, factory.TotalReaderCount);
        }

        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposed()
        {
            SynchronizedDictionary<int, string> test = new SynchronizedDictionary<int, string>(new SimpleReadWriteLocking());
            test.Dispose();
            test.Add(1, "");
        }
    }
    [TestFixture]
    public class TestSynchronizedList : TestGenericCollection<SynchronizedList<int>, int>
    {
        protected override int[] GetSample()
        {
            return new[] { 1, 3, 5, 7, 9, 11, 13 };
        }

        [Test]
        public void TestAddWithOrdinal()
        {
            SynchronizedList<int> list = new SynchronizedList<int>(new IgnoreLocking());
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, list.Add(i));
        }
        [Test]
        public void TestIndexOf()
        {
            SynchronizedList<int> list = new SynchronizedList<int>(new List<int>(new[] { 1, 2, 3 }));
            Assert.AreEqual(2, list.IndexOf(3));
            Assert.AreEqual(1, list.IndexOf(2));
            Assert.AreEqual(0, list.IndexOf(1));
            Assert.AreEqual(-1, list.IndexOf(5));
        }
        [Test]
        public void TestInsert()
        {
            SynchronizedList<int> list = new SynchronizedList<int>(new List<int>(), new IgnoreLocking());
            list.Insert(0, 1);
            list.Insert(0, 2);
            list.Insert(0, 3);

            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(1, list[2]);
        }
        [Test]
        public void TestIndexer()
        {
            SynchronizedList<int> list = new SynchronizedList<int>(new List<int>(), new IgnoreLocking());
            list.Insert(0, 1);
            list.Insert(0, 2);
            list.Insert(0, 3);

            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(1, list[2]);

            list[2] ^= list[0];
            list[0] ^= list[2];
            list[2] ^= list[0];

            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }
        [Test]
        public void TestRemoveAt()
        {
            SynchronizedList<int> list = new SynchronizedList<int>(new List<int>(), new IgnoreLocking());
            list.Insert(0, 1);
            list.Insert(0, 2);
            list.Insert(0, 3);
            list.RemoveAt(1);
            list.RemoveAt(0);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }
        [Test]
        public void TestReplaceList()
        {
            SynchronizedList<int> list = new SynchronizedList<int>(new List<int>(), new IgnoreLocking());
            list.Insert(0, 1);
            Assert.AreEqual(1, list.Count);
            list.ReplaceStorage(new List<int>());
            Assert.AreEqual(0, list.Count);
        }
        [Test]
        public void TestLock()
        {
            LockCounterFactory<SimpleReadWriteLocking> factory = new LockCounterFactory<SimpleReadWriteLocking>();
            ILockStrategy lck = factory.Create();
            SynchronizedList<int> list = new SynchronizedList<int>(lck);

            Assert.IsTrue(ReferenceEquals(lck, list.Lock));
            list.Add(42);
            Assert.AreEqual(1, factory.TotalWriterCount);
            Assert.AreEqual(0, factory.TotalReaderCount);

            list[0] = 51;
            Assert.AreEqual(2, factory.TotalWriterCount);

            list.Insert(1, 52);
            Assert.AreEqual(3, factory.TotalWriterCount);

            list.RemoveAt(1);
            Assert.AreEqual(4, factory.TotalWriterCount);

            list.Remove(-1);
            Assert.AreEqual(5, factory.TotalWriterCount);

            Assert.AreEqual(51, list[0]);
            Assert.AreEqual(1, factory.TotalReaderCount);

            foreach (int i in list)
                GC.KeepAlive(i);
            Assert.AreEqual(2, factory.TotalReaderCount);

            Assert.AreEqual(0, list.IndexOf(51));
            Assert.AreEqual(3, factory.TotalReaderCount);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(4, factory.TotalReaderCount);
        }

        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposed()
        {
            SynchronizedList<int> list = new SynchronizedList<int>(new SimpleReadWriteLocking());
            list.Dispose();
            list.Add(1);
        }
    }
}
