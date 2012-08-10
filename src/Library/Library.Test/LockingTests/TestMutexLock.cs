#region Copyright 2011-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Threading;
using CSharpTest.Net.Synchronization;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test.LockingTests
{
    [TestFixture]
    public class TestMutexLock
    {
        public bool CanLock(Mutex mtx)
        {
            bool bLocked = false;
            Thread t = new Thread(delegate() { try { bLocked = mtx.WaitOne(0, false); } catch (AbandonedMutexException) { bLocked = true; } });
            t.Start();
            t.Join();
            return bLocked;
        }

        [Test]
        public void TestMutexIsNew()
        {
            using (MutexLock lck = new MutexLock(Guid.NewGuid().ToString()))
                Assert.IsTrue(lck.WasNew);
        }
        [Test]
        public void TestMutexIsLocked()
        {
            using (MutexLock lck = new MutexLock(Guid.NewGuid().ToString()))
            {
                Assert.IsTrue(lck.IsLocked);
                lck.Dispose();
                Assert.IsFalse(lck.IsLocked);
            }
        }
        [Test]
        public void TestMutexLockByName()
        {
            using (new MutexLock("MutexLock.TestMutexLockByName"))
            {
                Assert.IsFalse(CanLock(new Mutex(false, "MutexLock.TestMutexLockByName")));
            }
        }
        [Test]
        public void TestMutexLockByFormattedName()
        {
            using (new MutexLock("MutexLock.{0}", "TestMutexLockByFormattedName"))
            {
                Assert.IsFalse(CanLock(new Mutex(false, "MutexLock.TestMutexLockByFormattedName")));
            }
        }
        [Test]
        public void TestMutexLockTimeout()
        {
            Exception error = null;
            using (new MutexLock("MutexLock.TestMutexLockTimeout"))
            {
                Thread t = new Thread(
                    delegate()
                    {
                        try
                        {
                            using (new MutexLock(1, "MutexLock.{0}", "TestMutexLockTimeout"))
                            { }
                        }
                        catch (Exception e) { error = e; }
                    }
                );
                t.Start();
                t.Join();
            }

            Assert.IsTrue(error is TimeoutException);
        }
        [Test]
        public void TestMutexAbandond()
        {
            using (Mutex mtx = new Mutex())
            {
                Thread t = new Thread(
                    delegate()
                    { GC.KeepAlive(new MutexLock(1, mtx)); }
                    );
                t.Start();
                t.Join();

                //So the previous thread abandoned the mutex...
                using (MutexLock lck = new MutexLock(mtx))
                {
                    Assert.IsTrue(lck.WasAbandonded);
                    Assert.AreEqual(mtx, lck.MutexHandle);
                }
            }
        }
    }
}
