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
using System;
using System.Text;
using CSharpTest.Net.Collections;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestLinkList : TestGenericCollection<LListNode<int>.LList, LListNode<int>>
    {
        protected override LListNode<int>[] GetSample()
        {
            return new[] 
            {
                new LListNode<int>(1),
                new LListNode<int>(3),
                new LListNode<int>(5),
                new LListNode<int>(7),
                new LListNode<int>(9),
                new LListNode<int>(13),
            };
        }

        [Test]
        public void TestEnumerator()
        {
            LListNode<string>.LList list = new LListNode<string>.LList();
            list.AddLast("B");
            list.AddFirst("A");
            System.Collections.Generic.IEnumerator<LListNode<string>> e = list.GetEnumerator();
            using (e)
            {
                Assert.IsTrue(e.MoveNext());
                Assert.IsTrue(ReferenceEquals(e.Current, ((System.Collections.IEnumerator)e).Current));
                Assert.AreEqual("A", e.Current.Value);
                list.Remove(e.Current);

                Assert.IsTrue(e.MoveNext());
                Assert.IsTrue(ReferenceEquals(e.Current, ((System.Collections.IEnumerator)e).Current));
                Assert.AreEqual("B", e.Current.Value);
                list.Remove(e.Current);

                Assert.IsFalse(e.MoveNext());

                Assert.IsTrue(list.IsEmpty);
                list.AddLast(String.Empty);

                e.Reset();
                Assert.IsTrue(e.MoveNext());
                Assert.AreEqual(String.Empty, e.Current.Value);
                Assert.IsFalse(e.MoveNext());

                try
                {
                    GC.KeepAlive(e.Current);
                    Assert.Fail();
                }
                catch (InvalidOperationException)
                { }
            }

            //now e is disposed...
            try
            {
                e.MoveNext();
                Assert.Fail();
            }
            catch (ObjectDisposedException) { }
            try
            {
                GC.KeepAlive(e.Current);
                Assert.Fail();
            }
            catch (ObjectDisposedException) { }
            try
            {
                e.Reset();
                Assert.Fail();
            }
            catch (ObjectDisposedException) { }
        }

        [Test]
        public void TestAddFirst()
        {
            LListNode<string>.LList list = new LListNode<string>.LList();
            Assert.IsTrue(list.IsEmpty);
            list.AddLast("B");
            Assert.IsFalse(list.IsEmpty);
            list.AddFirst("A");
            Assert.AreEqual("A", list.First.Value);
            Assert.AreEqual("B", list.Last.Value);
        }

        [Test]
        public void TestLinks()
        {
            LListNode<string> empty = new LListNode<string>();
            LListNode<string> valueA = new LListNode<string>("A");
            LListNode<string> valueB = new LListNode<string>("B");
            LListNode<string>.LList list = new LListNode<string>.LList();

            list.AddFirst(empty);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(empty, list.First);
            Assert.AreEqual(empty, list.Last);
            Assert.IsNull(empty.Next);
            Assert.IsNull(empty.Previous);

            list.AddLast(valueB);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(empty, list.First);
            Assert.AreEqual(valueB, list.Last);

            Assert.AreEqual(valueB, empty.Next);
            Assert.IsNull(empty.Previous);

            Assert.AreEqual(empty, valueB.Previous);
            Assert.IsNull(valueB.Next);

            list.AddFirst(valueA);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(valueA, list.First);
            Assert.AreEqual(valueB, list.Last);

            Assert.AreEqual(valueB, empty.Next);
            Assert.AreEqual(valueA, empty.Previous);

            Assert.AreEqual(empty, valueA.Next);
            Assert.IsNull(valueA.Previous);

            Assert.AreEqual(empty, valueB.Previous);
            Assert.IsNull(valueB.Next);

            list.Remove(empty);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(valueA, list.First);
            Assert.AreEqual(valueB, list.Last);

            Assert.AreEqual(valueB, valueA.Next);
            Assert.IsNull(valueA.Previous);

            Assert.AreEqual(valueA, valueB.Previous);
            Assert.IsNull(valueB.Next);
        }
    }
}
