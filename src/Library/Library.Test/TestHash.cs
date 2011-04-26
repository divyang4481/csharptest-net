#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Crypto;
using System.IO;
using System.Security.Cryptography;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    [Category("TestHash")]
    public partial class TestHash
    {
        readonly byte[] TestData = Guid.NewGuid().ToByteArray();
        Stream TestDataStream() { return new MemoryStream(TestData); }

        [Test]
        public void TestCopy()
        {
            Hash hash = Hash.MD5(TestData);
            Hash copy = Hash.FromBytes(hash.ToArray());
            Assert.AreEqual(hash, copy);
            Assert.AreEqual(hash.ToString(), copy.ToString());

            copy = Hash.FromString(hash.ToString());
            Assert.AreEqual(hash, copy);
            Assert.AreEqual(hash.ToString(), copy.ToString());
        }

        [Test]
        public void TestMD5()
        {
            Hash hash = Hash.MD5(TestData);
            Assert.AreEqual(MD5.Create().ComputeHash(TestData), hash.ToArray());
            Hash hash2 = Hash.MD5(TestDataStream());

            Assert.AreEqual(hash, hash2);
            Assert.IsTrue(hash == hash2);
            Assert.IsTrue(hash.Equals(hash2));
            Assert.IsTrue(hash.Equals((object)hash2));
            Assert.AreEqual(hash.GetHashCode(), hash2.GetHashCode());
            Assert.AreEqual(hash.Length, hash2.Length);
            Assert.AreEqual(hash.ToString(), hash2.ToString());
            Assert.AreEqual(hash.ToArray(), hash2.ToArray());
            Assert.IsFalse(hash != hash2);
        }

        [Test]
        public void TestSHA1()
        {
            Hash hash = Hash.SHA1(TestData);
            Assert.AreEqual(SHA1.Create().ComputeHash(TestData), hash.ToArray());
            Hash hash2 = Hash.SHA1(TestDataStream());

            Assert.AreEqual(hash, hash2);
            Assert.IsTrue(hash == hash2);
            Assert.IsTrue(hash.Equals(hash2));
            Assert.IsTrue(hash.Equals((object)hash2));
            Assert.AreEqual(hash.GetHashCode(), hash2.GetHashCode());
            Assert.AreEqual(hash.Length, hash2.Length);
            Assert.AreEqual(hash.ToString(), hash2.ToString());
            Assert.AreEqual(hash.ToArray(), hash2.ToArray());
            Assert.IsFalse(hash != hash2);
        }

        [Test]
        public void TestSHA256()
        {
            Hash hash = Hash.SHA256(TestData);
            Assert.AreEqual(SHA256.Create().ComputeHash(TestData), hash.ToArray());
            Hash hash2 = Hash.SHA256(TestDataStream());

            Assert.AreEqual(hash, hash2);
            Assert.IsTrue(hash == hash2);
            Assert.IsTrue(hash.Equals(hash2));
            Assert.IsTrue(hash.Equals((object)hash2));
            Assert.AreEqual(hash.GetHashCode(), hash2.GetHashCode());
            Assert.AreEqual(hash.Length, hash2.Length);
            Assert.AreEqual(hash.ToString(), hash2.ToString());
            Assert.AreEqual(hash.ToArray(), hash2.ToArray());
            Assert.IsFalse(hash != hash2);
        }

        [Test]
        public void TestSHA384()
        {
            Hash hash = Hash.SHA384(TestData);
            Assert.AreEqual(SHA384.Create().ComputeHash(TestData), hash.ToArray());
            Hash hash2 = Hash.SHA384(TestDataStream());

            Assert.AreEqual(hash, hash2);
            Assert.IsTrue(hash == hash2);
            Assert.IsTrue(hash.Equals(hash2));
            Assert.IsTrue(hash.Equals((object)hash2));
            Assert.AreEqual(hash.GetHashCode(), hash2.GetHashCode());
            Assert.AreEqual(hash.Length, hash2.Length);
            Assert.AreEqual(hash.ToString(), hash2.ToString());
            Assert.AreEqual(hash.ToArray(), hash2.ToArray());
            Assert.IsFalse(hash != hash2);
        }

        [Test]
        public void TestSHA512()
        {
            Hash hash = Hash.SHA512(TestData);
            Assert.AreEqual(SHA512.Create().ComputeHash(TestData), hash.ToArray());
            Hash hash2 = Hash.SHA512(TestDataStream());

            Assert.AreEqual(hash, hash2);
            Assert.IsTrue(hash == hash2);
            Assert.IsTrue(hash.Equals(hash2));
            Assert.IsTrue(hash.Equals((object)hash2));
            Assert.AreEqual(hash.GetHashCode(), hash2.GetHashCode());
            Assert.AreEqual(hash.Length, hash2.Length);
            Assert.AreEqual(hash.ToString(), hash2.ToString());
            Assert.AreEqual(hash.ToArray(), hash2.ToArray());
            Assert.IsFalse(hash != hash2);
        }

		[Test]
		public void TestHashStream()
		{
			Random r = new Random();
			byte[][] test =
			new byte[][]
				{
					new byte[300], 
					new byte[500], 
					new byte[1000], 
				};
			using(HashStream hs = new HashStream(new SHA256Managed()))
			using(MemoryStream ms = new MemoryStream())
			{
				Assert.IsTrue(hs.CanWrite);
				Assert.IsFalse(hs.CanRead);

				foreach (byte[] bytes in test)
				{
					r.NextBytes(bytes);
					ms.Write(bytes, 0, bytes.Length);
					hs.Write(bytes, 0, bytes.Length);
				}

				Hash expect = Hash.SHA256(ms.ToArray());
				Hash actual = hs.Close();

				Assert.AreEqual(expect, actual);
				Assert.AreEqual(expect.ToArray(), actual.ToArray());
				Assert.AreEqual(expect.ToString(), actual.ToString());
			}
		}

		[Test, ExpectedException(typeof(ObjectDisposedException))]
		public void TestHashStreamDisposed()
		{
			using(HashStream hs = new HashStream(new SHA256Managed()))
			{
				hs.Close();
				hs.Close(); //<- fails, already closed and/or disposed
			}
		}
    }
}
