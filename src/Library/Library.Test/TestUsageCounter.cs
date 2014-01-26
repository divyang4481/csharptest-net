#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Runtime.InteropServices;
using System.Threading;
using CSharpTest.Net.Threading;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestUsageCounter
    {
        [Test]
        public void TestSingleCounter()
        {
            bool bcalled;
            ThreadStart call = delegate() { bcalled = true; };

            using (UsageCounter counter = new UsageCounter("some global name"))
            {
                counter.TotalCount(delegate(int count) { Assert.AreEqual(0, count); });
                bcalled = false;
                counter.Increment(call);
                Assert.IsTrue(bcalled);
                counter.TotalCount(delegate(int count) { Assert.AreEqual(1, count); });

                bcalled = false;
                counter.Increment(call);
                Assert.IsFalse(bcalled);
                counter.TotalCount(delegate(int count) { Assert.AreEqual(2, count); });

                bcalled = false;
                counter.Decrement(call);
                Assert.IsFalse(bcalled);
                counter.TotalCount(delegate(int count) { Assert.AreEqual(1, count); });

                bcalled = false;
                counter.Decrement(call);
                Assert.IsTrue(bcalled);
                counter.TotalCount(delegate(int count) { Assert.AreEqual(0, count); });
            }
        }

        [Test, ExpectedException(typeof(SemaphoreFullException))]
        public void TestTooManyDecrements()
        {
            using (UsageCounter counter = new UsageCounter("some global name"))
            {
                counter.Increment();
                counter.TotalCount(delegate(int count) { Assert.AreEqual(1, count); });
                counter.Decrement();
                counter.TotalCount(delegate(int count) { Assert.AreEqual(0, count); });
                counter.Decrement();
            }
        }

        [Test]
        public void TestNestedCounters()
        {
            bool bcalled;
            ThreadStart call = delegate() { bcalled = true; };

            using (UsageCounter counter = new UsageCounter("some global name"))
            {
                using (UsageCounter counter2 = new UsageCounter("some global name"))
                    counter2.Increment();
                counter.TotalCount(delegate(int count) { Assert.AreEqual(1, count); });

                bcalled = false;
                counter.Decrement(call);
                Assert.IsTrue(bcalled);
                counter.TotalCount(delegate(int count) { Assert.AreEqual(0, count); });
            }
        }

        [Test]
        public void TestMultipleCounters()
        {
            using (UsageCounter counter = new UsageCounter("some global name"))
            {
                counter.Increment();
                counter.TotalCount(delegate(int count) { Assert.AreEqual(1, count); });
            }

            //Someone has to hold onto at least one counter, or all will be cleared
            using (UsageCounter counter = new UsageCounter("some global name"))
                counter.TotalCount(delegate(int count) { Assert.AreEqual(0, count); });
        }

        [Test]
        public void TestMultipleNestedCounters()
        {
            using (UsageCounter counter = new UsageCounter("some global name"))
            {
                using (UsageCounter counter2 = new UsageCounter("some global name"))
                    counter2.Increment();
                counter.TotalCount(delegate(int count) { Assert.AreEqual(1, count); });

                using (UsageCounter counter2 = new UsageCounter("some global name"))
                {
                    counter2.TotalCount(delegate(int count) { Assert.AreEqual(1, count); });
                    counter2.Decrement();
                }
                counter.TotalCount(delegate(int count) { Assert.AreEqual(0, count); });
            }
        }

        class Value { public int Number; }

        [Test]
        public void TestEventArguments()
        {
            Value val = new Value();
            using (UsageCounter counter = new UsageCounter("some global name"))
            {
                counter.Increment(delegate(Value v) { v.Number++; }, val);
                counter.Increment(delegate(Value v) { v.Number++; }, val);
                counter.Decrement(delegate(Value v) { v.Number++; }, val);
                counter.Decrement(delegate(Value v) { v.Number++; }, val);
                Assert.AreEqual(2, val.Number);
            }
        }

        [Test]
        public void TestInstanceCount()
        {
            using (UsageCounter counter = new UsageCounter("some global name"))
            {
                Assert.AreEqual(0, counter.InstanceCount);
                counter.Increment();
                Assert.AreEqual(1, counter.InstanceCount);
                counter.Increment();
                Assert.AreEqual(2, counter.InstanceCount);
                using (UsageCounter copy = new UsageCounter("some global name"))
                    Assert.AreEqual(0, copy.InstanceCount);
                counter.Decrement();
                Assert.AreEqual(1, counter.InstanceCount);
                counter.Decrement();
                Assert.AreEqual(0, counter.InstanceCount);
            }

        }

        [Test]
        public void TestInstanceName()
        {
            using (UsageCounter counter = new UsageCounter("some global name"))
                Assert.AreEqual("some global name", counter.Name);
            using (UsageCounter counter = new UsageCounter(@"{0}\Item-{1}", "Global", 1))
                Assert.AreEqual(@"Global\Item-1", counter.Name);
        }
    }
}
