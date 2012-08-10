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
using System.IO;
using System.Threading;
using CSharpTest.Net.Threading;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestWaitAndContinue
    {
        /// <summary>
        /// Demonstration of the work and continue interface.
        /// </summary>
        class SampleWork : IWaitAndContinue
        {
            public ManualResetEvent Cancel = new ManualResetEvent(false);
            public ManualResetEvent Ready = new ManualResetEvent(false);
            public ManualResetEvent CompletedEvent = new ManualResetEvent(false);
            public ManualResetEvent Sleeping = new ManualResetEvent(false);
            public Mutex Mutex = null;

            private bool _complete, _cancelled;
            public bool Cancelled { get { return _cancelled; } set { _cancelled = value; } }
            public bool Completed { get { return _complete; } set { _complete = value; CompletedEvent.Set(); } }

            public bool WorkThrows, Disposed, MutexAcquired, SleepForever;

            public int HandleCount { get { return Mutex != null ? 3 : 2; } }

            public void CopyHandles(WaitHandle[] array, int offset)
            {
                array[offset] = Cancel;
                array[offset + 1] = Ready;
                if(Mutex != null) array[offset + 2] = Mutex;
            }

            public void ContinueProcessing(WaitHandle handleSignaled)
            {
                if (WorkThrows) 
                    throw new ArgumentOutOfRangeException("WorkThrows");
                if (SleepForever)
                {
                    Sleeping.Set();
                    while (SleepForever) Thread.Sleep(100);
                }
                if (ReferenceEquals(Cancel, handleSignaled))
                    Completed = Cancelled = true;
                else if (ReferenceEquals(Ready, handleSignaled))
                    Completed = true;
                else if (ReferenceEquals(Mutex, handleSignaled))
                {
                    Completed = MutexAcquired = true;
                    Mutex.ReleaseMutex();
                }
                else
                    throw new ArgumentException();
            }

            public void Dispose()
            {
                //We should set Completed = true
                Disposed = true;
                Cancel.Close();
                Ready.Close();
                if (Mutex != null) Mutex.Close();
            }
        }

        [Test]
        public void TestCompletedEnqueue()
        {
            WaitAndContinueList work = new WaitAndContinueList();
            SampleWork item = new SampleWork();
            item.Completed = true;
            Assert.IsFalse(item.Disposed);
            work.AddWork(item);
            Assert.IsTrue(work.IsEmpty);
            Assert.IsTrue(item.Disposed);
        }

        [Test]
        public void TestPerformWork()
        {
            WaitAndContinueList work = new WaitAndContinueList();
            SampleWork item1, item2;
            IWaitAndContinue signaled;

            work.AddWork(item1 = new SampleWork());
            work.AddWork(item2 = new SampleWork());

            Assert.AreEqual(2, work.Count);
            Assert.IsFalse(work.IsEmpty);

            //normally done as a loop: while(work.Perform(timeout)) { }
            Assert.IsFalse(work.PerformWork(1));
            item2.Ready.Set();
            Assert.IsTrue(work.PerformWork(0));
            Assert.IsTrue(item2.Completed);
            Assert.IsTrue(item2.Disposed);

            Assert.IsFalse(work.PerformWork(1));
            item1.Cancel.Set();
            Assert.IsTrue(work.PerformWork(0, out signaled));
            Assert.IsTrue(ReferenceEquals(signaled, item1));
            Assert.IsTrue(item1.Completed);
            Assert.IsTrue(item1.Cancelled);
            Assert.IsTrue(item1.Disposed);

            Assert.IsTrue(work.IsEmpty);
            Assert.AreEqual(0, work.Count);
            Assert.IsFalse(work.PerformWork(1));
        }

        [Test]
        public void TestPerformWorkThrows()
        {
            WaitAndContinueList work = new WaitAndContinueList();
            SampleWork item = new SampleWork();
            item.WorkThrows = true;
            work.AddWork(item);

            Assert.IsFalse(work.IsEmpty);
            Assert.IsFalse(work.PerformWork(1));
            item.Ready.Set();
            try
            {
                work.PerformWork(0);
                Assert.Fail("Expected exception.");
            }
            catch (ArgumentOutOfRangeException ae)
            {
                Assert.AreEqual("WorkThrows", ae.ParamName);
            }
            Assert.IsTrue(work.IsEmpty);
            Assert.IsTrue(item.Disposed);
            Assert.IsFalse(item.Completed); // incomplete, but still disposed... abandond due to exception.
        }

        [Test]
        public void TestPerformByAbandonMutex()
        {
            Mutex abandondMutex = new Mutex();
            Thread t = new Thread(delegate() { abandondMutex.WaitOne(); });
            t.Start();
            t.Join();

            WaitAndContinueList work = new WaitAndContinueList();
            SampleWork item = new SampleWork();
            item.Mutex = abandondMutex;

            Assert.IsFalse(item.Disposed);
            work.AddWork(item);
            Assert.IsTrue(work.PerformWork(0));
            Assert.IsTrue(work.IsEmpty);
            Assert.IsTrue(item.Disposed);
            Assert.IsTrue(item.MutexAcquired);
        }

        [Test]
        public void TestPerformDisposedWork()
        {
            WaitAndContinueList work = new WaitAndContinueList();
            SampleWork item = new SampleWork();

            Assert.IsFalse(item.Disposed);
            work.AddWork(item);

            Assert.IsFalse(work.PerformWork(0));
            item.Dispose();

            Assert.IsFalse(work.PerformWork(0));

            item.Completed = true;//Normally this would be set in the Dispose method of the WorkItem, but we are testing
            Assert.IsFalse(work.PerformWork(0));

            Assert.IsTrue(work.IsEmpty);
            Assert.IsTrue(item.Disposed);
        }

        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestAddWorkToDisposedList()
        {
            WaitAndContinueList work = new WaitAndContinueList();
            work.Dispose();
            work.AddWork(new SampleWork());
        }
        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestAddWorkListToDisposedList()
        {
            WaitAndContinueList work = new WaitAndContinueList();
            work.Dispose();
            work.AddWork(new WaitAndContinueList());
        }

        [Test]
        public void TestWaitAndContinueWorker()
        {
            WaitAndContinueWorker work;
            using (work = new WaitAndContinueWorker())
            {
                work.OnError += delegate(object o, ErrorEventArgs e) { throw new Exception("Failed.", e.GetException()); };

                SampleWork item = new SampleWork();
                work.AddWork(item);

                Assert.IsFalse(work.IsEmpty);

                item.Ready.Set();
                Assert.IsTrue(item.CompletedEvent.WaitOne(100, false));
                Assert.IsTrue(item.Completed);
                Assert.IsFalse(item.Cancelled);

                while (!item.Disposed)
                    Thread.Sleep(0);

                Assert.IsTrue(item.Disposed);
                Assert.IsTrue(work.IsEmpty);
            }
            Assert.IsTrue(work.IsEmpty);
        }

        [Test]
        public void TestCompleteWork()
        {
            using (WaitAndContinueWorker work = new WaitAndContinueWorker())
            {
                work.OnError += delegate(object o, ErrorEventArgs e) { throw new Exception("Failed.", e.GetException()); };

                SampleWork item = new SampleWork();
                work.AddWork(item);

                Assert.IsFalse(work.IsEmpty);
                work.Complete(true, 10);

                Assert.IsFalse(item.Completed);
                Assert.IsTrue(item.Disposed);
            }
        }

        [Test]
        public void TestAbortWork()
        {
            using (WaitAndContinueWorker work = new WaitAndContinueWorker())
            {
                work.OnError += delegate(object o, ErrorEventArgs e) { throw new Exception("Failed.", e.GetException()); };

                SampleWork item = new SampleWork();
                work.AddWork(item);

                Assert.IsFalse(work.IsEmpty);
                work.Abort();

                Assert.IsFalse(item.Completed);
                Assert.IsTrue(item.Disposed);
            }
        }

        [Test]
        public void TestHardAbort()
        {
            using (WaitAndContinueWorker work = new WaitAndContinueWorker())
            {
                work.OnError += delegate(object o, ErrorEventArgs e) { throw new Exception("Failed.", e.GetException()); };

                SampleWork item = new SampleWork();
                item.SleepForever = true;
                work.AddWork(item);

                item.Ready.Set();
                Assert.IsTrue(item.Sleeping.WaitOne(1000, false));
                work.Abort();

                Assert.IsFalse(item.Completed);
                Assert.IsTrue(item.Disposed);
            }
        }

        [Test]
        public void TestWorkerException()
        {
            using (WaitAndContinueWorker work = new WaitAndContinueWorker())
            {
                SampleWork item = new SampleWork();
                item.WorkThrows = true;
                work.AddWork(item);

                ManualResetEvent mreError = new ManualResetEvent(false);
                Exception error = null;
                work.OnError += delegate(object o, ErrorEventArgs e) { error = e.GetException(); mreError.Set(); };

                item.Ready.Set();
                Assert.IsTrue(mreError.WaitOne(1000, false));
                Assert.IsTrue(error is ArgumentOutOfRangeException);
                Assert.IsTrue(((ArgumentOutOfRangeException)error).ParamName == "WorkThrows");
            }
        }

        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestAddWorkToDisposedWorker()
        {
            WaitAndContinueWorker work = new WaitAndContinueWorker();
            work.Complete(false, 100);
            work.Dispose();

            SampleWork item = new SampleWork();
            work.AddWork(item);
        }
        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestAddWorkListToDisposedWorker()
        {
            WaitAndContinueWorker work = new WaitAndContinueWorker();
            work.Complete(false, 100);
            work.Dispose();

            WaitAndContinueList list = new WaitAndContinueList();
            list.AddWork(new SampleWork());

            work.AddWork(list);
        }
    }
}
