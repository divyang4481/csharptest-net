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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using CSharpTest.Net.Data;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestDbGuid
    {
        [Test]
        public void GuidCompare()
        {
            DbGuid a = DbGuid.NewGuid();
            DbGuid b = DbGuid.NewGuid();
            Assert.AreEqual(-1, a.CompareTo(b));
            Assert.AreEqual(-1, a.ToGuid().CompareTo(b.ToGuid()));
        }

        [Test]
        public void TestRoundTripGuidToDbGuid()
        {
            Guid testA = Guid.NewGuid();
            DbGuid dbtest = (DbGuid)testA;
            Guid testB = dbtest.ToGuid();
            Assert.AreEqual(testA, testB);
        }

        [Test]
        public void TestRoundTripDbGuidToGuid()
        {
            DbGuid testA = DbGuid.NewGuid();
            Guid dbtest = testA;
            DbGuid testB = (DbGuid)dbtest;
            Assert.AreEqual(testA, testB);
        }

        [Test]
        public void TestRoundTripBytes()
        {
            DbGuid testA = DbGuid.NewGuid();
            byte[] bytes = testA.ToByteArray();
            DbGuid testB = new DbGuid(bytes);
            Assert.AreEqual(testA, testB);
        }

        [Test]
        public void TestRoundTripByteOffset()
        {
            DbGuid testA = DbGuid.NewGuid();
            byte[] bytes = new byte[1024];
            testA.ToByteArray(bytes, 512);
            DbGuid testB = new DbGuid(bytes, 512);
            Assert.AreEqual(testA, testB);
        }

        [Test]
        public void TestRoundTrip64Bit()
        {
            DbGuid testA = DbGuid.NewGuid();
            DbGuid testB = new DbGuid(testA.High64, testA.Low64);
            Assert.AreEqual(testA, testB);
        }

        [Test]
        public void TestDiscreteCtor()
        {
            DbGuid testA = new DbGuid(0x01020304, 0x0506, 0x0708, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80);
            Guid testB = new Guid(0x01020304, 0x0506, 0x0708, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80);
            Assert.AreEqual(testB, testA.ToGuid());
        }

        [Test]
        public void TestEmpty()
        {
            Assert.AreEqual(Guid.Empty, DbGuid.Empty.ToGuid());
            Assert.AreEqual(DbGuid.Empty, new DbGuid(Guid.Empty));
            Assert.AreEqual(Guid.Empty, new DbGuid().ToGuid());
            Assert.AreEqual(DbGuid.Empty, new DbGuid());
            Assert.AreEqual(DbGuid.Empty, new DbGuid(0, 0));
            Assert.AreEqual(0L, DbGuid.Empty.High64);
            Assert.AreEqual(0L, DbGuid.Empty.Low64);
        }

        [Test]
        public void TestDateTime()
        {
            // Note: DateTime precision is +/- 1ms
            DateTime start = DateTime.UtcNow.AddMilliseconds(-1);
            DbGuid test = DbGuid.NewGuid();
            Assert.IsTrue(test.ToDateTimeUtc() > start, "expected {0} > {1}", test.ToDateTimeUtc(), start);
            DateTime now = DateTime.UtcNow.AddMilliseconds(1);
            Assert.IsTrue(test.ToDateTimeUtc() <= now, "expected {0} <= {1}", test.ToDateTimeUtc(), now);
        }

        [Test]
        public void TestRoundTripSqlGuid()
        {
            DbGuid testA = DbGuid.NewGuid();
            DbGuid dbtest = testA.ToSqlGuid();
            Assert.AreEqual(dbtest, testA);
            Assert.AreEqual(dbtest.GetHashCode(), testA.GetHashCode());
            Assert.AreEqual(0, dbtest.CompareTo(testA));
            DbGuid testB = dbtest.ToSequenceGuid();
            Assert.AreEqual(testA, testB);
        }

        [Test]
        public void TestToString()
        {
            Guid test = Guid.NewGuid();
            Assert.AreEqual(test.ToString(), new DbGuid(test).ToString());
            Assert.AreEqual(test.ToString("N"), new DbGuid(test).ToString("N"));
            Assert.AreEqual(test.ToString("B", CultureInfo.InvariantCulture), new DbGuid(test).ToString("B", CultureInfo.InvariantCulture));
        }

        [Test]
        public void TestSerializer()
        {
            DbGuid[] ids = new DbGuid[16];
            MemoryStream stream = new MemoryStream();
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = DbGuid.NewGuid();
                DbGuid.Serializer.WriteTo(ids[i], stream);
            }
            stream.Position = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                DbGuid test = DbGuid.Serializer.ReadFrom(stream);
                Assert.AreEqual(ids[i], test);
            }
            Assert.AreEqual(-1, stream.ReadByte());
        }

        [Test, Explicit]
        public void TestGenerationPerf()
        {
            var timer = Stopwatch.StartNew();
            int total = 0, target = 0;
            while (timer.ElapsedMilliseconds < 1000)
            {
                for (target += 1000000; total < target; total++)
                {
                    DbGuid.NewGuid();
                }
            }
            timer.Stop();
            Console.WriteLine("Generated {0:n0} guids in {1:n2} seconds, averaging {2:n2} per second.",
                total, timer.Elapsed.TotalSeconds, total / timer.Elapsed.TotalSeconds);
        }

        [Test]
        public void TestSequence()
        {
            DbGuid last = DbGuid.NewGuid();
            for (int i = 0; i < 10000; i++)
            {
                DbGuid test = DbGuid.NewGuid();
                TestSequence(last, test);
                TestSequence(last, test.ToSqlGuid());
                TestSequence(last.ToSqlGuid(), test);
                TestSequence(last.ToSqlGuid(), test.ToSqlGuid());
                Assert.IsTrue(last.CompareTo(test) < 0);
                last = test;
            }
        }

        private void TestSequence(DbGuid small, DbGuid big)
        {
            Assert.IsTrue(small != big);
            Assert.IsTrue(big != small);

            Assert.IsTrue(small < big);
            Assert.IsTrue(big > small);

            Assert.IsTrue(small <= big);
            Assert.IsTrue(big >= small);

            Assert.IsFalse(small == big);
            Assert.IsFalse(big == small);

            Assert.IsFalse(big < small);
            Assert.IsFalse(small > big);
            
            Assert.IsFalse(big <= small);
            Assert.IsFalse(small >= big);
            
            Assert.IsTrue(small.CompareTo(big) < 0);
            Assert.IsTrue(big.CompareTo(small) > 0);

            Assert.IsTrue(DbGuid.Comparer.Compare(small, big) < 0);
            Assert.IsTrue(DbGuid.Comparer.Compare(big, small) > 0);
            
            Assert.IsTrue(small.ToString().CompareTo(big.ToString()) < 0);
        }

        [Test]
        public void TestEquality()
        {
            DbGuid testA = DbGuid.NewGuid();
            TestEquality(testA, testA, true);
            TestEquality(testA, DbGuid.NewGuid(), false);
            TestEquality(testA, new DbGuid(Guid.NewGuid()), false);
            TestEquality(testA, testA.ToSqlGuid(), true);
            TestEquality(testA, testA.ToSqlGuid().ToSequenceGuid(), true);
        }

        private void TestEquality(DbGuid testA, DbGuid testB, bool expect)
        {
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(expect, testA.GetHashCode() == testB.GetHashCode());
                Assert.AreEqual(expect, DbGuid.Comparer.GetHashCode(testA) == DbGuid.Comparer.GetHashCode(testB));

                Assert.AreEqual(expect, testA == testB);
                Assert.AreEqual(!expect, testA != testB);

                Assert.AreEqual(expect, ((object) testA).Equals(testB));
                Assert.AreEqual(expect, ((IEquatable<DbGuid>) testA).Equals(testB));

                Assert.AreEqual(expect, ((IComparable)testA).CompareTo(testB) == 0);
                Assert.AreEqual(expect, ((IComparable<DbGuid>)testA).CompareTo(testB) == 0);

                Assert.AreEqual(expect, DbGuid.Comparer.Equals(testA, testB));
                Assert.AreEqual(expect, DbGuid.Comparer.Compare(testA, testB) == 0);

                Assert.AreEqual(expect, testA.ToString() == testB.ToString());
                Assert.AreEqual(expect, testA.ToString("N") == testB.ToString("N"));
                Assert.AreEqual(expect, testA.ToString("N", null) == testB.ToString("N", null));

                //swap and try again
                DbGuid tmp = testA;
                testA = testB;
                testB = tmp;
            }
        }
    }
}
