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
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test.LockingTests
{
    [TestFixture]
    public class TestDebugLocking
    {
        [Test]
        public void TestReaderCounts()
        {
            using (DebugLocking<SimpleReadWriteLocking> l = new DebugLocking<SimpleReadWriteLocking>(
                false, 0, 1, false, 1))
            {
                Assert.IsFalse(l.CaptureStack);

                Assert.AreEqual(0, l.CurrentReaderCount);
                Assert.AreEqual(0, l.LocalReaderCount);
                Assert.AreEqual(0, l.MaxReaderCount);

                using (l.Read())
                {
                    Assert.AreEqual(1, l.CurrentReaderCount);
                    Assert.AreEqual(1, l.LocalReaderCount);
                    using (l.Read(0))
                    {
                        Assert.AreEqual(2, l.CurrentReaderCount);
                        Assert.AreEqual(2, l.LocalReaderCount);
                    }
                }

                Assert.AreEqual(0, l.CurrentReaderCount);
                Assert.AreEqual(0, l.LocalReaderCount);
                Assert.AreEqual(2, l.MaxReaderCount);
            }
        }
        [Test]
        public void TestWriterCounts()
        {
            using (DebugLocking<SimpleReadWriteLocking> l = new DebugLocking<SimpleReadWriteLocking>(
                false, 0, 1, false, 1))
            {
                Assert.IsFalse(l.CaptureStack);

                Assert.AreEqual(0, l.CurrentWriterCount);
                Assert.AreEqual(0, l.LocalWriterCount);
                Assert.AreEqual(0, l.MaxWriterCount);

                using (l.Write())
                {
                    Assert.AreEqual(1, l.CurrentWriterCount);
                    Assert.AreEqual(1, l.LocalWriterCount);
                    using (l.Write(0))
                    {
                        Assert.AreEqual(2, l.CurrentWriterCount);
                        Assert.AreEqual(2, l.LocalWriterCount);
                    }
                }

                Assert.AreEqual(0, l.CurrentWriterCount);
                Assert.AreEqual(0, l.LocalWriterCount);
                Assert.AreEqual(2, l.MaxWriterCount);
            }
        }
        [Test]
        public void TestDebugCaptureStack()
        {
            using (DebugLocking lck = new DebugLocking<IgnoreLocking>())
                Assert.IsFalse(lck.CaptureStack);
            using (DebugLocking lck = new DebugLocking<IgnoreLocking>(true, 0, 0, false, 0))
                Assert.IsTrue(lck.CaptureStack);
        }
        [Test]
        public void TestDebugFactoryProperties()
        {
            DebugLockFactory<SimpleReadWriteLocking> factory = new DebugLockFactory<SimpleReadWriteLocking>(
                true, -1, 1, true, 1);

            Assert.AreEqual(true, factory.CaptureStack);
            factory.CaptureStack = false;
            Assert.AreEqual(false, factory.CaptureStack);

            Assert.AreEqual(true, factory.ConcurrentReads);
            factory.ConcurrentReads = false;
            Assert.AreEqual(false, factory.ConcurrentReads);

            Assert.AreEqual(-1, factory.LimitTimeout);
            factory.LimitTimeout = 1000;
            Assert.AreEqual(1000, factory.LimitTimeout);

            Assert.AreEqual(1, factory.LimitNestedReaders);
            factory.LimitNestedReaders = 10;
            Assert.AreEqual(10, factory.LimitNestedReaders);

            Assert.AreEqual(1, factory.LimitNestedWriters);
            factory.LimitNestedWriters = 5;
            Assert.AreEqual(5, factory.LimitNestedWriters);
        }
        [Test]
        public void TestDebugFactoryRecursiveOptions()
        {
            DebugLockFactory<SimpleReadWriteLocking> factory = new DebugLockFactory<SimpleReadWriteLocking>(
                false, 0, 1, false, 1);

            using (ILockStrategy lck = factory.Create())
            {
                using (lck.Write())
                using (lck.Write()) //second lock, allow recurse 1 time as per constructor
                {
                    try { using (lck.Write()) { Assert.Fail(); } }
                    catch (Exception ex)
                    {
                        Assert.IsTrue(ex is DebugAssertionFailedException);//nesting prohibited by debug lock
                    }
                }

                using (lck.Read())
                using (lck.Read()) //second lock, allow recurse 1 time as per constructor
                {
                    try { using (lck.Read()) { Assert.Fail(); } }
                    catch (Exception ex)
                    {
                        Assert.IsTrue(ex is DebugAssertionFailedException);//nesting prohibited by debug lock
                    }
                }
            }
        }
    }
}
