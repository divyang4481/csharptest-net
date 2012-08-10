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
using CSharpTest.Net.Crypto;
using System.IO;
using System.Security.Cryptography;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
	#region IPasswordDerivedBytes derivation tests
	[TestFixture]
	public class TestPBKDF2DerivedBytes : TestPasswordDerivedBytes
	{
		protected override IPasswordDerivedBytes DerivedBytes(string input)
		{ return new PBKDF2(Password.Encoding.GetBytes(input), DefaultSalt, DefaultIterations); }

		//verify similar behavior to RFC
		[Test]
		public void TestRfcCompatible()
		{
			using (IPasswordDerivedBytes pd = DerivedBytes(TEST_PASSWORD))
			{
				pd.IterationCount = 1000;
				Rfc2898DeriveBytes pbytes = new Rfc2898DeriveBytes(Password.Encoding.GetBytes(TEST_PASSWORD), DefaultSalt.ToArray(), 1000);
				Assert.AreEqual(pd.GetBytes(20), pbytes.GetBytes(20));
				Assert.AreEqual(pd.GetBytes(35), pbytes.GetBytes(35));

				byte[] test1 = pd.GetBytes(40);
				byte[] test2 = pbytes.GetBytes(40);

				//'RFC' implementation resuses 5 bytes already generated with 'GetBytes(35)', if you always call Reset() these behave the same
				Assert.AreNotEqual(test1, test2); 

				Buffer.BlockCopy(test2, 5, test2, 0, 35); //strip the first five bytes
				Array.Resize(ref test1, 35);
				Array.Resize(ref test2, 35);

				Assert.AreEqual(test1, test2); //now they should be the same
			}
		}
    }
    [TestFixture]
    public class TestRfc2898DeriveBytes : TestPasswordDerivedBytes
    {
        protected override IPasswordDerivedBytes DerivedBytes(string input)
        { return new TestRfc2898(Password.Encoding.GetBytes(input), DefaultSalt.ToArray(), DefaultIterations); }

        class TestRfc2898 : Rfc2898DeriveBytes, IPasswordDerivedBytes
        {
            public TestRfc2898(byte[] getBytes, byte[] toArray, int defaultIterations) : base(getBytes, toArray, defaultIterations)
            { }
            void IDisposable.Dispose() {}
        }

        [Test]
        public void TestRfcRandom()
        {
            Rfc2898DeriveBytes pd1 = new Rfc2898DeriveBytes(Password.Encoding.GetBytes(TEST_PASSWORD), DefaultSalt.ToArray(), 10);
            using (IPasswordDerivedBytes pd2 = new TestRfc2898(Password.Encoding.GetBytes(TEST_PASSWORD), DefaultSalt.ToArray(), 10))
            {
                Assert.AreEqual(pd1.GetBytes(20), pd2.GetBytes(20));
                Assert.AreEqual(pd1.GetBytes(35), pd2.GetBytes(35));
                Assert.AreEqual(pd1.GetBytes(16), pd2.GetBytes(16));

                Random r = new Random();
                for(int i=0; i < 1000; i++)
                {
                    int size = r.Next(2, 60);
                    Assert.AreEqual(pd1.GetBytes(size), pd2.GetBytes(size));
                }
            }
        }
    }
	[TestFixture]
	public class TestMD5DerivedBytes : TestPasswordDerivedBytes
	{
		protected override IPasswordDerivedBytes DerivedBytes(string input)
		{ return new HashDerivedBytes<HMACMD5>(new HMACMD5(), new MemoryStream(Password.Encoding.GetBytes(input)), DefaultSalt, DefaultIterations); }
	}
	[TestFixture]
	public class TestSHA1DerivedBytes : TestPasswordDerivedBytes
	{
		protected override IPasswordDerivedBytes DerivedBytes(string input)
		{ return new HashDerivedBytes<HMACSHA1>(Password.Encoding.GetBytes(input), DefaultSalt, DefaultIterations); }

		//verify similar behavior to RFC
		[Test]
		public void TestRfcCompatible()
		{
			using (IPasswordDerivedBytes pd = DerivedBytes(TEST_PASSWORD))
			{
				pd.IterationCount = 1000;
				byte[] sha1Hash = SHA1.Create().ComputeHash(Password.Encoding.GetBytes(TEST_PASSWORD));
				Rfc2898DeriveBytes pbytes = new Rfc2898DeriveBytes(sha1Hash, DefaultSalt.ToArray(), 1000);
				Assert.AreEqual(pd.GetBytes(20), pbytes.GetBytes(20));
				Assert.AreEqual(pd.GetBytes(35), pbytes.GetBytes(35));

				byte[] test1 = pd.GetBytes(40);
				byte[] test2 = pbytes.GetBytes(40);

				//'RFC' implementation resuses 5 bytes already generated with 'GetBytes(35)', if you call reset these behave the same
				Assert.AreNotEqual(test1, test2);

				Buffer.BlockCopy(test2, 5, test2, 0, 35); //strip the first five bytes
				Array.Resize(ref test1, 35);
				Array.Resize(ref test2, 35);

				Assert.AreEqual(test1, test2); //now they should be the same
			}
		}
	}
	[TestFixture]
	public class TestSHA256DerivedBytes : TestPasswordDerivedBytes
	{
		protected override IPasswordDerivedBytes DerivedBytes(string input)
		{ return new HashDerivedBytes<HMACSHA256>(Password.Encoding.GetBytes(input), DefaultSalt, DefaultIterations); }
	}
	[TestFixture]
	public class TestSHA384DerivedBytes : TestPasswordDerivedBytes
	{
		protected override IPasswordDerivedBytes DerivedBytes(string input)
		{ return new HashDerivedBytes<HMACSHA384>(Password.Encoding.GetBytes(input), DefaultSalt, DefaultIterations); }
	}
	[TestFixture]
	public class TestSHA512DerivedBytes : TestPasswordDerivedBytes
	{
		protected override IPasswordDerivedBytes DerivedBytes(string input)
		{ return new HashDerivedBytes<HMACSHA512>(Password.Encoding.GetBytes(input), DefaultSalt, DefaultIterations); }
	}
	#endregion

    public abstract class TestPasswordDerivedBytes
    {
		protected const string TEST_PASSWORD = "TEST_PASSWORD";
		protected readonly Salt DefaultSalt = new Salt();
		protected readonly int DefaultIterations = PasswordKey.DefaultIterations;

		protected abstract IPasswordDerivedBytes DerivedBytes(string input);

		[Test]
		public void TestReset()
		{
			using (IPasswordDerivedBytes pd = DerivedBytes(TEST_PASSWORD))
			{
				pd.IterationCount = 10;
				byte[] bytes = pd.GetBytes(1000);
				Assert.AreNotEqual(bytes, pd.GetBytes(bytes.Length));
				pd.Reset();
				Assert.AreEqual(bytes, pd.GetBytes(bytes.Length));
			}
		}

		[Test]
		public void TestIteration()
		{
			using (IPasswordDerivedBytes pd = DerivedBytes(TEST_PASSWORD))
			{
				byte[] bytes = pd.GetBytes(4);

				pd.IterationCount *= 2;
				Assert.AreNotEqual(bytes, pd.GetBytes(bytes.Length));
				pd.IterationCount /= 2;
				Assert.AreEqual(bytes, pd.GetBytes(bytes.Length));
			}
		}

		[Test]
		public void TestSalt()
		{
			using (IPasswordDerivedBytes pd = DerivedBytes(TEST_PASSWORD))
			{
				Assert.AreEqual(DefaultSalt.ToArray(), pd.Salt);
				byte[] bytes = pd.GetBytes(4);

				pd.Salt = new Salt().ToArray();
				Assert.AreNotEqual(bytes, pd.GetBytes(bytes.Length));
				Assert.AreNotEqual(DefaultSalt.ToArray(), pd.Salt);

				pd.Salt = DefaultSalt.ToArray();
				Assert.AreEqual(bytes, pd.GetBytes(bytes.Length));
				Assert.AreEqual(DefaultSalt.ToArray(), pd.Salt);
			}
		}
	}
}
