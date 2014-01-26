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
using NUnit.Framework;
using CSharpTest.Net.Collections;
using System.Collections;
using System.IO;
using CSharpTest.Net.IO;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public partial class TestStream
    {
        class DisposableFlag : IDisposable
        {
            public bool Disposed = false;
            void IDisposable.Dispose() { this.Disposed = true; }
        }

        [Test]
        public void TestDisposingStream()
        {
            DisposableFlag f = new DisposableFlag();
            Assert.IsFalse(f.Disposed);

            using (Stream s = new DisposingStream(Stream.Null).WithDisposeOf(f))
                s.Close();

            Assert.IsTrue(f.Disposed);
        }

        [Test]
        public void TestNonClosingStream()
        {
            MemoryStream ms = new MemoryStream(new byte[5]);
            Assert.AreEqual(5, ms.Length);

            using (Stream s = new NonClosingStream(ms))
                s.Close();

            Assert.AreEqual(5, ms.Length);
            Assert.AreEqual(0, ms.Position);
            Assert.IsTrue(ms.CanRead);
            Assert.AreEqual(5, ms.Read(new byte[10], 0, 10));
        }

        [Test]
        public void TestCombinedStream()
        {
            DisposableFlag f1 = new DisposableFlag();
            DisposableFlag f2 = new DisposableFlag();
            Assert.IsFalse(f1.Disposed || f2.Disposed);

            Stream ms1 = new DisposingStream(new MemoryStream(Encoding.ASCII.GetBytes("Hello"))).WithDisposeOf(f1);
            Stream ms2 = new DisposingStream(new MemoryStream(Encoding.ASCII.GetBytes("There"))).WithDisposeOf(f2);
            Assert.AreEqual(ms1.Length, ms2.Length);

            int size = (int)ms1.Length;
            byte[] bytes = new byte[size * 2];

            using (Stream cs = new CombinedStream(ms1, ms2))
            {
                Assert.IsTrue(cs.CanRead);

                Assert.IsFalse(f1.Disposed);
                Assert.AreEqual(size, cs.Read(bytes, 0, size));
                Assert.IsFalse(f1.Disposed); //still not disposed util read of 0 bytes
                Assert.AreEqual(1, cs.Read(bytes, size, 1));//read 1 more byte
                Assert.IsTrue(f1.Disposed);
                //now finish the second one...
                Assert.IsFalse(f2.Disposed);
                Assert.AreEqual(size - 1, cs.Read(bytes, size + 1, size - 1));
                Assert.IsFalse(f2.Disposed);//still not done
                Assert.AreEqual(-1, cs.ReadByte());
                Assert.IsTrue(f2.Disposed);
            }

            Assert.AreEqual("HelloThere", Encoding.ASCII.GetString(bytes));

            //both were disposed
            Assert.IsTrue(f1.Disposed && f2.Disposed);
        }

        [Test]
        public void TestCombinedStreamDisposee()
        {
            DisposableFlag f1 = new DisposableFlag();
            DisposableFlag f2 = new DisposableFlag();
            Assert.IsFalse(f1.Disposed || f2.Disposed);

            Stream ms1 = new DisposingStream(new MemoryStream(Encoding.ASCII.GetBytes("Hello"))).WithDisposeOf(f1);
            Stream ms2 = new DisposingStream(new MemoryStream(Encoding.ASCII.GetBytes("There"))).WithDisposeOf(f2);

            using (Stream cs = new CombinedStream(ms1, ms2))
            { }

            Assert.IsTrue(f1.Disposed && f2.Disposed);//even though not read, we did dispose?
        }

        [Test]
        public void TestMarshalStream()
        {
            byte[] dataIn = new byte[1024];
            byte[] copy;
            new Random().NextBytes(dataIn);
            GCHandle h = GCHandle.Alloc(dataIn, GCHandleType.Pinned);
            try
            {
                using (MarshallingStream stream = new MarshallingStream(h.AddrOfPinnedObject(), false, dataIn.Length))
                {
                    Assert.IsTrue(stream.CanRead);
                    Assert.IsTrue(stream.CanWrite);
                    Assert.IsTrue(stream.CanSeek);

                    copy = new byte[(int)stream.Length];
                    IOStream.Read(stream, copy, (int)stream.Length);
                    Assert.AreEqual(dataIn, copy);
                    Assert.AreEqual(stream.Length, stream.Position);
                    stream.Position = 0;
                    Assert.AreEqual(0L, stream.Position);

                    stream.Write(new byte[dataIn.Length], 0, dataIn.Length);
                    Assert.AreEqual(new byte[dataIn.Length], dataIn);

                    stream.Seek(0, SeekOrigin.Begin);
                    Assert.AreEqual(0, stream.Position);
                    stream.Write(copy, 0, dataIn.Length);
                    Assert.AreEqual(dataIn, copy);

                    stream.Seek(-10, SeekOrigin.Current);
                    Assert.AreEqual(stream.Length - 10, stream.Position);

                    stream.Seek(-stream.Length, SeekOrigin.End);
                    Assert.AreEqual(0, stream.Position);

                    stream.WriteByte(123);
                    stream.Position = 0;
                    Assert.AreEqual(123, stream.ReadByte());
                }

                using (MarshallingStream stream = new MarshallingStream(h.AddrOfPinnedObject(), true, dataIn.Length))
                {
                    Assert.IsFalse(stream.CanWrite);

                    copy = new byte[(int)stream.Length];
                    IOStream.Read(stream, copy, (int)stream.Length);
                    Assert.AreEqual(dataIn, copy);

                    try
                    {
                        stream.Write(new byte[10], 0, 10);
                        Assert.Fail();
                    }
                    catch (InvalidOperationException) { }

                    stream.Dispose();
                    try
                    {
                        stream.Read(new byte[10], 0, 10);
                        Assert.Fail();
                    }
                    catch (ObjectDisposedException) { }
                }
            }
            finally { h.Free(); }
        }
    }

	[TestFixture]
    public partial class TestBaseStream
	{
        class Test : BaseStream { }
        [Test]
        public void TestNonExcpetionMembers()
        {
            BaseStream bs = new Test();

            Assert.IsFalse(bs.CanRead);
            Assert.IsFalse(bs.CanWrite);
            Assert.IsFalse(bs.CanSeek);
            Assert.IsFalse(bs.CanTimeout);

            bs.Flush();
            bs.Close();
            bs.Dispose();
        }

        [Test, DebuggerNonUserCode, ExpectedException(typeof(NotSupportedException))]
        public void TestLength() { long value = new Test().Length; }
        [Test, DebuggerNonUserCode, ExpectedException(typeof(NotSupportedException))]
        public void TestSetLength() { new Test().SetLength(0); }

        [Test, DebuggerNonUserCode, ExpectedException(typeof(NotSupportedException))]
        public void TestGetPosition() { long value = new Test().Position; }
        [Test, DebuggerNonUserCode, ExpectedException(typeof(NotSupportedException))]
        public void TestSetPosition() { new Test().Position = 1; }

        [Test, DebuggerNonUserCode, ExpectedException(typeof(NotSupportedException))]
        public void TestSeek() { new Test().Seek(0, SeekOrigin.Begin); }

        [Test, DebuggerNonUserCode, ExpectedException(typeof(NotSupportedException))]
        public void TestRead() { new Test().Read(new byte[1], 0, 1); }
        [Test, DebuggerNonUserCode, ExpectedException(typeof(NotSupportedException))]
        public void TestWrite() { new Test().Write(new byte[1], 0, 1); }

        [Test, DebuggerNonUserCode, ExpectedException(typeof(NotSupportedException))]
        public void TestReadByte() { new Test().ReadByte(); }
        [Test, DebuggerNonUserCode, ExpectedException(typeof(NotSupportedException))]
        public void TestWriteByte() { new Test().WriteByte(1); }
    }

    [TestFixture]
    public partial class TestAggregateStream
    {
        class Test : AggregateStream 
        {
            public Test() { }
            public Test(Stream s) : base(s) { }
        }

        [Test]
        public void TestNullStream()
        {
            Test s = new Test();
            s.SetLength(20);
            Assert.IsTrue(s.CanRead);
            Assert.IsTrue(s.CanWrite);
            Assert.IsTrue(s.CanSeek);
            Assert.IsFalse(s.CanTimeout);

            Assert.AreEqual(0L, s.Position);
            Assert.AreEqual(0L, s.Length);

            s.Position = 1;
            Assert.AreEqual(0L, s.Position);

            s.Seek(1, SeekOrigin.Begin);
            Assert.AreEqual(0L, s.Position);

            s.Write(new byte[1], 0, 1);
            Assert.AreEqual(0, s.Read(new byte[1], 0, 1));

            s.Flush();
            s.Close();
            s.Dispose();
        }

        [Test]
        public void TestMemStream()
        {
            Test s = new Test(new MemoryStream());

            Assert.IsTrue(s.CanRead);
            Assert.IsTrue(s.CanWrite);
            Assert.IsTrue(s.CanSeek);
            Assert.IsFalse(s.CanTimeout);

            Assert.AreEqual(0L, s.Position);
            Assert.AreEqual(0L, s.Length);

            s.Write(new byte[1], 0, 1);
            Assert.AreEqual(0, s.Read(new byte[1], 0, 1));

            Assert.AreEqual(1L, s.Position);
            s.Position = 0;
            Assert.AreEqual(1, s.Read(new byte[10], 0, 10));
            Assert.AreEqual(1L, s.Position);

            s.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(0L, s.Position);

            Assert.AreEqual(1L, s.Length);
            s.SetLength(20);
            Assert.AreEqual(20L, s.Length);
            
            s.Flush();
            s.Close();
            s.Dispose();
        }
    }



}
