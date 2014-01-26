#region Copyright 2012-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using System.Threading;
using NUnit.Framework;
using CSharpTest.Net.IO;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestBackgroundWriter
    {
        #region TestFixture SetUp/TearDown
        [TestFixtureSetUp]
        public virtual void Setup()
        {
        }

        [TestFixtureTearDown]
        public virtual void Teardown()
        {
        }
        #endregion

        #region TestStream
        class TestStream : AggregateStream
        {
            public bool Flushed;
            public bool Disposed;
            public TestStream() : this(new MemoryStream()) { }
            public TestStream(Stream stream) : base(stream)
            { }
            public override void Flush() { Flushed = true; }
            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        [Test]
        public void TestWriteAndFlush()
        {
            using (TempFile temp = new TempFile())
            using (TestStream io = new TestStream(temp.Open()))
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                const int sz = 1000;
                const int iter = 10000;
                var bytes = new byte[sz];
                for (int i = 0; i < iter; i++)
                    wtr.Write(bytes, 0, sz);
                wtr.Flush();
                Assert.IsTrue(io.Flushed);
                Assert.AreEqual(sz * iter, io.Position);
                Assert.AreEqual(sz * iter, io.Length);
            }
        }
        [Test]
        public void TestWriteAndSignal()
        {
            using (ManualResetEvent mre = new ManualResetEvent(false))
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Write(new byte[100], 0, 100, mre);
                Assert.IsTrue(mre.WaitOne(60000, false));
                Assert.AreEqual(100, io.Position);
                Assert.AreEqual(100, io.Length);
            }
        }
        [Test]
        public void TestWriteOffset()
        {
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Write(new byte[100], 0, 100);
                wtr.Write(1, new byte[] { 42 }, 0, 1);
                wtr.Flush();
                Assert.AreEqual(2, io.Position);
                Assert.AreEqual(100, io.Length);
                io.Position = 1;
                Assert.AreEqual(42, io.ReadByte());
            }
        }
        [Test]
        public void TestWriteAndReadOffset()
        {
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Write(0L, new byte[100], 0, 100);
                wtr.Perform(s => Thread.Sleep(50));
                wtr.Write(100L, new byte[] { 99 }, 0, 1);
                wtr.Write(100L, new byte[] { 42, 43 }, 0, 2);

                // Read scans the pending writes for writes at the provided offset and returns the last result
                byte[] read = new byte[100];
                Assert.AreEqual(2, wtr.Read(100L, read, 0, 100));
                Assert.AreEqual(42, (int)read[0]);
                Assert.AreEqual(43, (int)read[1]);
            }
        }
        [Test]
        public void TestWriteOffsetAndSignal()
        {
            using (ManualResetEvent mre = new ManualResetEvent(false))
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Write(new byte[100], 0, 100);
                wtr.Write(1, new byte[] { 42 }, 0, 1, mre);
                Assert.IsTrue(mre.WaitOne(60000, false));
                Assert.AreEqual(2, io.Position);
                Assert.AreEqual(100, io.Length);
                io.Position = 1;
                Assert.AreEqual(42, io.ReadByte());
            }
        }
        [Test]
        public void TestWriteFileAsyncFlush()
        {
            using (ManualResetEvent mre = new ManualResetEvent(false))
            using (TempFile temp = new TempFile())
            using (TestStream io = new TestStream(temp.Open()))
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                const int sz = 1000;
                const int iter = 10000;
                var bytes = new byte[sz];
                for (int i = 0; i < iter; i++)
                    wtr.Write(bytes, 0, sz);
                wtr.BeginFlush();
                Assert.IsFalse(io.Flushed);

                wtr.Perform(s => mre.Set());
                Assert.IsTrue(mre.WaitOne(60000, false));

                Assert.IsTrue(io.Flushed);
                Assert.AreEqual(sz * iter, io.Position);
                Assert.AreEqual(sz * iter, io.Length);
            }
        }
        [Test]
        public void TestPerformAction()
        {
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Perform(s => s.Write(new byte[100], 0, 100));
                wtr.Flush();
                Assert.AreEqual(100, io.Position);
                Assert.AreEqual(100, io.Length);
            }
        }
        [Test]
        public void TestPerformActionAndSignal()
        {
            using (ManualResetEvent mre = new ManualResetEvent(false))
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Perform(s => s.Write(new byte[100], 0, 100), mre);
                Assert.IsTrue(mre.WaitOne(60000, false));
                Assert.AreEqual(100, io.Position);
                Assert.AreEqual(100, io.Length);
            }
        }
        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestClosedRaisesError()
        {
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Close();
                wtr.Write(new byte[100], 0, 100);
            }
        }
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullBuffer()
        {
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Write(null, 0, 1);
            }
        }
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInvalidOffset()
        {
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Write(new byte[10], 11, 1);
            }
        }
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInvalidLength()
        {
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io))
            {
                wtr.Write(new byte[10], 5, 50);
            }
        }
        [Test]
        public void TestDisposeAndLeaveStreamOpen()
        {
            using (TestStream io = new TestStream())
            using (BackgroundWriter wtr = new BackgroundWriter(io, false))
            {
                wtr.Dispose();
                Assert.IsFalse(io.Disposed);
                io.Write(new byte[1], 0, 1);
            }
        }
        [Test]
        public void TestGCDisposesThread()
        {
            TestStream io = new TestStream();
            Thread worker = null;
            try
            {
                if (io.Disposed == false)
                {
                    new BackgroundWriter(io)
                        .Perform(s => worker = Thread.CurrentThread);
                }
            }
            finally
            {
                GC.Collect(0, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
            }

            Assert.IsNotNull(worker);
            Assert.IsFalse(worker.IsAlive);
            Assert.IsTrue(io.Disposed);
        }
    }
}
