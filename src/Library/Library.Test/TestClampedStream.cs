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
using System.IO;
using CSharpTest.Net.IO;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestClampedStream
    {
        private static byte[] SequencedBytes(int len)
        {
            byte[] bytes = new byte[len];
            for(int i=0; i < len; i++ )
                bytes[i] = (byte)i;
            return bytes;
        }

        [Test]
        public void TestClampedCanI()
        {
            MemoryStream ms = new MemoryStream();
            using (ClampedStream stream = new ClampedStream(ms, 0, long.MaxValue))
            {
                Assert.IsTrue(stream.CanRead);
                Assert.IsTrue(stream.CanWrite);
                Assert.IsTrue(stream.CanSeek);
                stream.Dispose();
                Assert.IsFalse(stream.CanRead);
                Assert.IsFalse(stream.CanWrite);
                Assert.IsFalse(stream.CanSeek);
            }
            ms = new MemoryStream(new byte[10], false);
            using (ClampedStream stream = new ClampedStream(ms, 3, 5))
            {
                Assert.IsTrue(stream.CanRead);
                Assert.IsFalse(stream.CanWrite);
                Assert.IsTrue(stream.CanSeek);
            }
        }
        [Test]
        public void TestClampedDispose()
        {
            MemoryStream ms = new MemoryStream();
            new ClampedStream(ms, 0, long.MaxValue, false).Dispose();
            Assert.IsTrue(ms.CanRead);

            new ClampedStream(ms, 0, long.MaxValue, true).Dispose();
            Assert.IsFalse(ms.CanRead);
        }
        [Test]
        public void TestClampedFlush()
        {
            MemoryStream ms = new MemoryStream();
            BufferedStream bs = new BufferedStream(ms, 1024);

            using (Stream s = new ClampedStream(bs, 0, long.MaxValue, false))
            {
                s.WriteByte(1);
                Assert.AreEqual(0, ms.Position);
                s.Flush();
                Assert.AreEqual(1, ms.Position);
                ms.Position = 0;
                Assert.AreEqual(1, ms.ReadByte());
            }
        }
        [Test]
        public void TestLength()
        {
            using (Stream s = new ClampedStream(new MemoryStream(SequencedBytes(20), false), 2, 10))
                Assert.AreEqual(10, s.Length);
            using (Stream s = new ClampedStream(new MemoryStream(SequencedBytes(10), false), 2, 10))
                Assert.AreEqual(8, s.Length);
            using (Stream s = new ClampedStream(new MemoryStream(SequencedBytes(10), false), 2, 0))
                Assert.AreEqual(0, s.Length);
            using (Stream s = new ClampedStream(new MemoryStream(SequencedBytes(10), false), 12, 10))
                Assert.AreEqual(0, s.Length);
        }
        [Test]
        public void TestPosition()
        {
            using (Stream s = new ClampedStream(new MemoryStream(SequencedBytes(20), false), 2, 10))
            {
                Assert.AreEqual(0, s.Position);
                Assert.AreEqual(2, s.ReadByte());
                s.Position = 0;
                Assert.AreEqual(0, s.Position);
                Assert.AreEqual(2, s.ReadByte());

                s.Position = 10;
                Assert.AreEqual(-1, s.ReadByte());
                Assert.AreEqual(10, s.Position);

                Assert.AreEqual(0, s.Read(new byte[10], 0, 10));
                Assert.AreEqual(10, s.Position);
            }
        }
        [Test]
        public void TestSetLength()
        {
            MemoryStream ms = new MemoryStream();
            using (Stream s = new ClampedStream(ms, 2, long.MaxValue))
            {
                s.SetLength(20);
                Assert.AreEqual(22, ms.Length);
            }
        }
        [Test]
        public void TestSeek()
        {
            MemoryStream ms = new MemoryStream(SequencedBytes(100));
            using (Stream s = new ClampedStream(ms, 10, 80))
            {
                Assert.AreEqual(0, s.Seek(0, SeekOrigin.Begin));
                Assert.AreEqual(0, s.Position);
                Assert.AreEqual(10, s.Seek(10, SeekOrigin.Begin));
                Assert.AreEqual(10, s.Position);
                Assert.AreEqual(20, s.Seek(10, SeekOrigin.Current));
                Assert.AreEqual(20, s.Position);
                Assert.AreEqual(15, s.Seek(-5, SeekOrigin.Current));
                Assert.AreEqual(15, s.Position);
                Assert.AreEqual(70, s.Seek(-10, SeekOrigin.End));
                Assert.AreEqual(70, s.Position);
                Assert.AreEqual(80, s.Seek(0, SeekOrigin.End));
                Assert.AreEqual(80, s.Position);
            }
        }
        [Test]
        public void TestReadWrite()
        {
            using (MemoryStream ms = new MemoryStream(SequencedBytes(20)))
            using (Stream s = new ClampedStream(ms, 10, 5))
            {
                byte[] test = new byte[200];
                Assert.AreEqual(5, s.Read(test, 0, 200));
                Array.Resize(ref test, 5);
                Assert.AreEqual(new byte[] { 10, 11, 12, 13, 14 }, test);
                s.Position = 0;
                s.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
                s.Position = 0;
                Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, IOStream.ReadAllBytes(s));
            }
        }

        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestSetLengthNotSupported()
        {
            MemoryStream ms = new MemoryStream(SequencedBytes(20));
            using (Stream s = new ClampedStream(ms, 2, 10))
            {
                s.SetLength(20);
                Assert.Fail();
            }
        }
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestOffsetsExceedLong()
        {
            new ClampedStream(new MemoryStream(), long.MaxValue, 10).Dispose();
            Assert.Fail();
        }
        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestNonSeekingStreamStart()
        {
            using (Stream s = new NonSeekingStream(new MemoryStream()))
            {
                Assert.IsFalse(s.CanSeek);
                new ClampedStream(s, 1, 10).Dispose();
                Assert.Fail();
            }
        }
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInvalidSeekFrom()
        {
            new ClampedStream(new MemoryStream(), 0, 10)
                .Seek(0, (SeekOrigin)256);
            Assert.Fail();
        }
        [Test, ExpectedException(typeof(IOException))]
        public void TestInvalidSeekBeforeBegin()
        {
            using(Stream s = new ClampedStream(new MemoryStream(new byte[255]), 2, 10))
                s.Seek(-1, SeekOrigin.Begin);
            Assert.Fail();
        }
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInvalidSeekPastEnd()
        {
            using (Stream s = new ClampedStream(new MemoryStream(new byte[255]), 2, 10))
                s.Seek(1, SeekOrigin.End);
            Assert.Fail();
        }
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestWritePastEnd()
        {
            using (Stream s = new ClampedStream(new MemoryStream(new byte[255]), 2, 10))
                s.Write(new byte[20], 0, 20);
            Assert.Fail();
        }
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestWritePastEndByte()
        {
            using (Stream s = new ClampedStream(new MemoryStream(new byte[255]), 2, 10))
            {
                s.Seek(0, SeekOrigin.End);
                s.WriteByte(1);
            }
            Assert.Fail();
        }
        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestIsDispose()
        {
            Stream s = new ClampedStream(new MemoryStream(), 0, long.MaxValue, false);
            s.Dispose();
            s.Seek(0, SeekOrigin.Begin);
            Assert.Fail();
        }

        class NonSeekingStream : AggregateStream
        {
            public NonSeekingStream(Stream s) : base(s)
            { }

            public override bool CanSeek
            { get { return false; } }
        }
    }
}
