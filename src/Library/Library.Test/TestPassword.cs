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
using System.Reflection;
using NUnit.Framework;
using CSharpTest.Net.Crypto;
using System.Text;
using System.IO;
using CSharpTest.Net.IO;
using System.Security;
using System.Diagnostics;
using System.Security.Cryptography;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public partial class TestPassword : TestEncryption
    {
        public TestPassword() : base(new PasswordKey(TEST_PASSWORD)) { }

		[Test, Explicit]
		public void TestPerfKeyGen()
		{
			int iter = 100;
			Salt salt = new Salt();
			byte[] passbytes = Password.Encoding.GetBytes(TEST_PASSWORD);
			IPasswordDerivedBytes[] types = new IPasswordDerivedBytes[] {
				new PBKDF2(passbytes, salt, iter),
				new HashDerivedBytes<HMACMD5>(passbytes, salt, iter),
				new HashDerivedBytes<HMACSHA1>(passbytes, salt, iter),
				new HashDerivedBytes<HMACSHA256>(passbytes, salt, iter),
				new HashDerivedBytes<HMACSHA384>(passbytes, salt, iter),
				new HashDerivedBytes<HMACSHA512>(passbytes, salt, iter),
			};

			foreach (IPasswordDerivedBytes db in types)
			{
				byte[] key;
				
				Stopwatch w = new Stopwatch();
				w.Start();
				for (int i = 0; i < 100; i++)
					key = new PasswordKey(db, salt).CreateKey().Key;
				w.Stop();

				Console.Error.WriteLine("{0,10}  {1}", w.ElapsedMilliseconds, db.GetType().Name);
			}
		}

        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposal()
        {
            string test;
            CryptoKey key = new PasswordKey(TEST_PASSWORD);
            test = key.Encrypt("Test");
            key.Dispose();

            key.Decrypt(test);
            Assert.Fail();
        }

        [Test]
        public void TestSetIv()
        {
            using (PasswordKey key = new PasswordKey("bla"))
            {
                Assert.AreEqual(AESCryptoKey.ProcessDefaultIV, key.IV);
                byte[] newIv = Guid.NewGuid().ToByteArray();
                key.IV = newIv;

                Assert.AreEqual(newIv, key.IV);
                Assert.AreEqual(newIv, key.CreateKey().IV);
            }
        }

        [Test]
        public void TestClearBytes()
        {
            byte[] password = Encoding.UTF8.GetBytes(TEST_PASSWORD);
            PasswordKey pk1 = new PasswordKey(true, password);
            Assert.AreEqual(new byte[password.Length], password);

            password = Encoding.UTF8.GetBytes(TEST_PASSWORD);
            pk1 = new PasswordKey(false, password);
            Assert.AreEqual(Encoding.UTF8.GetBytes(TEST_PASSWORD), password);
        }

        [Test]
        public void TestRecreate()
        {
            PasswordKey pk1 = new PasswordKey(TEST_PASSWORD);
            PasswordKey pk2 = new PasswordKey(SecureStringUtils.Create(TEST_PASSWORD));
            Assert.AreNotEqual(pk1.CreateKey().Key, pk2.CreateKey().Key);

            pk2.Salt = pk1.Salt;
            Assert.AreEqual(pk1.CreateKey().Key, pk2.CreateKey().Key);
            pk1.CreateKey();
            Assert.AreEqual(pk1.CreateKey().Key, pk2.CreateKey().Key);

            pk1.Salt = new Salt();
            Assert.AreNotEqual(pk1.Salt, pk2.Salt);
            Assert.AreNotEqual(pk1.CreateKey().Key, pk2.CreateKey().Key);
            Assert.AreEqual(pk1.CreateKey().Key, pk2.CreateKey(pk1.Salt).Key);
            pk2.Salt = pk1.Salt;
            Assert.AreEqual(pk1.Salt, pk2.Salt);
            Assert.AreEqual(pk1.CreateKey().Key, pk2.CreateKey().Key);
        }

        [Test]
        public void TestUnique()
        {
            PasswordKey pk1 = new PasswordKey(TEST_PASSWORD);
            PasswordKey pk2 = new PasswordKey(TEST_PASSWORD);

            Assert.AreNotEqual(pk1.Salt, pk2.Salt);
            Assert.AreNotEqual(pk1.CreateKey().Key, pk2.CreateKey().Key);

            pk2.Salt = pk1.Salt;
            Assert.AreEqual(pk1.Salt, pk2.Salt);
            Assert.AreEqual(pk1.CreateKey().Key, pk2.CreateKey().Key);
        }

        [Test]
        public void TestUniqueDerives()
        {
            PasswordKey pk1 = new PasswordKey(TEST_PASSWORD);
            PasswordKey pk2 = new PasswordKey(TEST_PASSWORD);
            pk2.Salt = pk1.Salt;
            Assert.AreEqual(pk1.CreateKey().Key, pk2.CreateKey().Key);
            pk2.IterationCount /= 2;
            Assert.AreEqual(pk1.Salt, pk2.Salt);
            Assert.AreNotEqual(pk1.CreateKey().Key, pk2.CreateKey().Key);
        }

        [Test]
        public void TestDecodeFromNewSalt()
        {
            byte[] bytes;
            byte[] svalue = Encoding.ASCII.GetBytes("some text value");

            using (PasswordKey pk = new PasswordKey(TEST_PASSWORD))
                bytes = pk.Encrypt(svalue);

            using (PasswordKey pk = new PasswordKey(TEST_PASSWORD))
                bytes = pk.Decrypt(bytes);

            Assert.AreEqual(svalue, bytes);
        }

        [Test]
        public void TestPasswordRead()
        {
            byte[] pwdBytes = Guid.NewGuid().ToByteArray();

            using (Password pwd = new Password(false, pwdBytes))
            {
                Assert.AreEqual(pwdBytes, IOStream.ReadAllBytes(pwd.ReadBytes()));
            }
            using (Password pwd = new Password(TEST_PASSWORD))
            {
                Assert.AreEqual(TEST_PASSWORD, pwd.ReadText().ReadToEnd());
            }
        }
        [Test]
        public void TestPasswordToFromSecureString()
        {
            using (Password p1 = new Password(TEST_PASSWORD))
            using (SecureString sstr = p1.ToSecureString())
            using (Password p2 = new Password(sstr))
            {
                Assert.AreEqual(p1, p2);
            }
        }
        [Test]
        public void TestPasswordHash()
        {
            byte[] pwdBytes = Guid.NewGuid().ToByteArray();

            using (Password pwd = new Password(false, pwdBytes))
            {
                using (PasswordHash hash = pwd.CreateHash())
                    Assert.IsTrue(hash.VerifyPassword(pwdBytes));
                using (PasswordHash hash = new PasswordHash(pwd))
                    Assert.IsTrue(hash.VerifyPassword(pwdBytes));
            }

            using (Password pwd = new Password(false, pwdBytes))
            {
                using (PasswordHash hash = new PasswordHash(false, pwdBytes, new Salt()))
                    Assert.AreEqual(hash, pwd.CreateHash(hash.Salt));
            }
        }
        [Test]
        public void TestPasswordEquality()
        {
            using (Password pwd1 = new Password(TEST_PASSWORD))
            {
                using (Password pwd2 = new Password(TEST_PASSWORD))
                {
                    Assert.AreEqual(pwd1, pwd2);
                    Assert.IsTrue(Password.Equals(pwd1, pwd2));
                    Assert.IsTrue(pwd1 == pwd2);
                    Assert.IsFalse(pwd1 != pwd2);
                    Assert.IsTrue(pwd1.Equals(pwd2));
                    Assert.IsTrue(pwd1.Equals((object)pwd2));
                    Assert.AreEqual(pwd1.GetHashCode(), pwd2.GetHashCode());
                }

                Assert.IsFalse(pwd1 == new Password("Not the same"));
                Assert.IsFalse(pwd1 == null);
                Assert.IsFalse(null == pwd1);
                Assert.IsFalse(pwd1.Equals(null));

                Assert.AreEqual(pwd1.GetHashCode(), Password.GetHashCode(pwd1));
            }
        }
        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestPasswordEmpty()
        {
            new Password(String.Empty);
        }

        [Test]
        public void TestPassword2()
        {
            const int count = 1000;
            Random rand = new Random();
            byte[] bytes = new byte[100];
            rand.NextBytes(bytes);
            Stopwatch time = new Stopwatch();
            time.Start();

            for (int i = 0; i < count; i++)
            {
                bytes = new byte[rand.Next(450) + 50];
                rand.NextBytes(bytes);
                using (Password pwd = new Password(false, bytes))
                {
                    byte[] copy = IOStream.ReadAllBytes(pwd.ReadBytes());
                    Assert.AreEqual(bytes, copy);
                }
            }
            time.Stop();
            Trace.WriteLine(time.Elapsed.ToString(), count.ToString() + " Passwords");
        }

    }
}
