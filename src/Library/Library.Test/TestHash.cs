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
using CSharpTest.Net.IO;
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
        public void TestCombinedSameHash()
        {
            Random r = new Random();
            byte[] a = new byte[10], b = new byte[10];
            r.NextBytes(a);
            r.NextBytes(b);

            Hash h1 = Hash.SHA256(a);
            Hash h2 = Hash.SHA256(b);
            Hash h3 = h1.Combine(h2);

            Assert.AreEqual(h3.ToString(), Hash.SHA256(
                new CombinedStream(
                    new MemoryStream(h1.ToArray()),
                    new MemoryStream(h2.ToArray())
                    )).ToString());
        }

        [Test]
        public void TestCombinedDifferentHash()
        {
            Random r = new Random();
            byte[] a = new byte[10], b = new byte[10];
            r.NextBytes(a);
            r.NextBytes(b);

            Hash h1 = Hash.SHA256(a);
            Hash h2 = Hash.SHA1(b);
            Hash h3 = h1.Combine(h2);

            Assert.AreEqual(h3.ToString(), Hash.SHA256(
                new CombinedStream(
                    new MemoryStream(h1.ToArray()),
                    new MemoryStream(Hash.SHA256(h2.ToArray()).ToArray()) //the dissimilar hash is first made the same length
                    )).ToString());
        }

        [Test]
        public void TestCombinedWithBytes()
        {
            Random r = new Random();
            byte[] a = new byte[10], b = new byte[32];
            r.NextBytes(a);
            r.NextBytes(b);

            Hash h1 = Hash.SHA256(a);
            Hash h3 = h1.Combine(b);

            Assert.AreEqual(h3.ToString(), Hash.SHA256(
                new CombinedStream(
                    new MemoryStream(h1.ToArray()),
                    new MemoryStream(Hash.SHA256(b).ToArray())//bytes combined are hashed first
                    )).ToString());
        }

        [Test]
        public void TestCreateAlgorithms()
        {
            foreach(int sz in new int [] { 16, 20, 32, 48, 64 })
            {
                Hash test = Hash.FromBytes(new byte[sz]);
                HashAlgorithm ha = test.CreateAlgorithm();
                Assert.IsNotNull(ha);
                Assert.AreEqual(sz, ha.ComputeHash(new byte[0]).Length);
            }
        }

        [Test]
        public void TestHashStreamRead()
        {
            Random r = new Random();
            byte[] bytes = new byte[1000];
            r.NextBytes(bytes);

            using (HashStream hs = new HashStream(new SHA256Managed(), new MemoryStream(bytes)))
            {
                for (int i = 0; i < 5; i++)
                
                while (hs.Position < hs.Length)
                {
                    hs.ReadByte();
                    int amt = r.Next(255);
                    byte[] tmp = new byte[amt];
                    hs.Read(tmp, 0, tmp.Length);
                }

                Hash expect = Hash.SHA256(bytes);
                Hash actual = hs.FinalizeHash();

                Assert.AreEqual(expect, actual);
                Assert.AreEqual(expect.ToArray(), actual.ToArray());
                Assert.AreEqual(expect.ToString(), actual.ToString());

                //still valid after FinalizeHash(); however, hash is restarted
                hs.Position = 0;
                IOStream.Read(hs, bytes.Length);
                actual = hs.FinalizeHash();

                Assert.AreEqual(expect, actual);
                Assert.AreEqual(expect.ToArray(), actual.ToArray());
                Assert.AreEqual(expect.ToString(), actual.ToString());
            }
        }

        [Test]
		public void TestHashStreamWrite()
		{
			Random r = new Random();
			byte[][] test =
			new byte[][]
				{
					new byte[300], 
					new byte[1], 
					new byte[500], 
					new byte[11],
					new byte[1], 
					new byte[1000], 
				};
            using (HashStream hs = new HashStream(new SHA256Managed()))
            using (MemoryStream ms = new MemoryStream())
            using (HashStream hsWrap = new HashStream(new SHA256Managed(), ms))
			{
				Assert.IsTrue(hs.CanWrite);
                long len = 0;
				foreach (byte[] bytes in test)
				{
                    len += bytes.Length;
					r.NextBytes(bytes);
                    hsWrap.Write(bytes, 0, bytes.Length);
					hs.Write(bytes, 0, bytes.Length);
				}
                for (int i = 0; i < 5; i++)
                {
                    len += 1;
                    byte val = (byte)r.Next(255);
                    hsWrap.WriteByte(val);
                    hs.WriteByte(val);
                }

			    Assert.AreEqual(len, ms.Position);
				Hash expect = Hash.SHA256(ms.ToArray());
				Hash actual = hs.Close();

				Assert.AreEqual(expect, actual);
				Assert.AreEqual(expect.ToArray(), actual.ToArray());
				Assert.AreEqual(expect.ToString(), actual.ToString());

                //wrapped test
                actual = hsWrap.FinalizeHash();
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
