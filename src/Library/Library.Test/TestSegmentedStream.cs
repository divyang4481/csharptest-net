#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.IO;
using System.IO;

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public partial class TestSegmentedStream
	{
		[Test, ExpectedException(typeof(ObjectDisposedException))]
		public void TestDisposed()
		{
			SegmentedMemoryStream ms = new SegmentedMemoryStream();
			ms.Dispose();
			ms.Position = 5;
		}

		[Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestSeekError()
		{
			SegmentedMemoryStream ms = new SegmentedMemoryStream();
			ms.Position = -1;
		}

		[Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestBadLength()
		{
			SegmentedMemoryStream ms = new SegmentedMemoryStream();
			ms.SetLength(-1);
		}

		[Test]
		public void TestBasics()
		{
			using (SegmentedMemoryStream ms = new SegmentedMemoryStream())
			{
				Assert.IsTrue(ms.CanRead);
				Assert.IsTrue(ms.CanWrite);
				Assert.IsTrue(ms.CanSeek);
				ms.Flush();
			}
		}

		[Test]
		public void TestSetLength()
		{
			using (SegmentedMemoryStream ms = new SegmentedMemoryStream())
			{
				Assert.AreEqual(0L, ms.Length);
				Assert.AreEqual(0L, ms.Position);
				ms.SetLength(10);

				Assert.AreEqual(0L, ms.Position);
				Assert.AreEqual(10L, ms.Length);
				Assert.AreEqual(10L, ms.Seek(0, System.IO.SeekOrigin.End));
			}
		}

		[Test]
		public void TestSeek()
		{
			using (SegmentedMemoryStream ms = new SegmentedMemoryStream())
			{
				Assert.AreEqual(0L, ms.Length);
				Assert.AreEqual(0L, ms.Position);
				Assert.AreEqual(0L, ms.Seek(0, System.IO.SeekOrigin.Begin));
				Assert.AreEqual(0L, ms.Seek(0, System.IO.SeekOrigin.Current));
				Assert.AreEqual(0L, ms.Seek(0, System.IO.SeekOrigin.End));

				ms.Position = 42L;
				Assert.AreEqual(42L, ms.Length);
				Assert.AreEqual(42L, ms.Position);
				Assert.AreEqual(42L, ms.Seek(42, System.IO.SeekOrigin.Begin));
				Assert.AreEqual(42L, ms.Seek(0, System.IO.SeekOrigin.Current));
				Assert.AreEqual(42L, ms.Seek(0, System.IO.SeekOrigin.End));

				ms.Position = 0;
				Assert.AreEqual(0L, ms.Position);
				Assert.AreEqual(0L, ms.Seek(0, System.IO.SeekOrigin.Begin));
				Assert.AreEqual(0L, ms.Seek(0, System.IO.SeekOrigin.Current));
				Assert.AreEqual(0L, ms.Seek(-42, System.IO.SeekOrigin.End));
			}
		}

		[Test]
		public void TestReadWrite()
		{
			using (SegmentedMemoryStream ms = new SegmentedMemoryStream(5))
			{
				Write(ms, "12345");
				Write(ms, "abcd");
				Write(ms, "ABCDEF");
				Write(ms, "12345");
				Write(ms, "");

				ms.Flush();
				Assert.AreEqual(20L, ms.Length);
				Assert.AreEqual(20L, ms.Position);

				ms.Position = 0;
				Assert.AreEqual("12345abcdABCDEF12345", Read(ms, (int)ms.Length));

				ms.Position = 5;
				Assert.AreEqual("a", Read(ms, 1));
				Assert.AreEqual("bcd", Read(ms, 3));
				Assert.AreEqual("ABCDEF", Read(ms, 6));
				Assert.AreEqual("12345", Read(ms, 5));
				Assert.AreEqual(0, ms.Read(new byte[10], 0, 10));
			}
		}

		private void Write(Stream s, string value)
		{
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
			long pos = s.Position;
			s.Write(bytes, 0, bytes.Length);
			Assert.AreEqual(pos + bytes.Length, s.Position);
		}

		private string Read(Stream s, int count)
		{
			byte[] bytes = new byte[count];
			Assert.AreEqual(count, s.Read(bytes, 0, count));
			return System.Text.Encoding.ASCII.GetString(bytes);
		}
	}
}
