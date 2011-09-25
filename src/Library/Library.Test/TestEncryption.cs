#region Copyright 2009-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using CSharpTest.Net.Formatting;
using NUnit.Framework;
using CSharpTest.Net.Crypto;
using System.IO;
using CSharpTest.Net.IO;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestRtlProcessKey : TestEncryption
    { public TestRtlProcessKey() : base(new RtlProcessKey()) { } }

    [TestFixture]
    public class TestDataPotectionUser : TestEncryption
    { public TestDataPotectionUser() : base(LocalHostKey.CurrentUser) { } }

    [TestFixture]
    public class TestDataPotectionUserSalted : TestEncryption
    { public TestDataPotectionUserSalted() : base(LocalHostKey.CurrentUser.WithSalt(new Salt())) { } }

    [TestFixture]
    public class TestDataPotectionMachine : TestEncryption
    { public TestDataPotectionMachine() : base(LocalHostKey.LocalMachine) { } }

    [TestFixture]
    public class TestDataPotectionMachineSalted : TestEncryption
    { public TestDataPotectionMachineSalted() : base(LocalHostKey.LocalMachine.WithSalt(new Salt())) { } }

    [TestFixture]
    public class TestAESEncryption : TestEncryption
    {
        public TestAESEncryption() : base(new AESCryptoKey()) { }

        [Test]
        public void TestCopyKeyAndIV()
        {
            byte[] ivrandom = new byte[16];
            new Random().NextBytes(ivrandom);

            using (AESCryptoKey k1 = new AESCryptoKey())
            using (AESCryptoKey k2 = new AESCryptoKey(k1.Key, k1.IV))
            using (AESCryptoKey kBadIv = new AESCryptoKey(k1.Key, ivrandom))
            {
                Assert.AreEqual(k1.Key, k2.Key);
                Assert.AreEqual(k1.IV, k2.IV);
                Assert.AreEqual("test", k2.Decrypt(k1.Encrypt("test")));

                Assert.AreEqual(k1.Key, kBadIv.Key);
                Assert.AreNotEqual(k1.IV, kBadIv.IV);
                try
                {   //one of two possible outcomes, junk or exception
                    Assert.AreNotEqual("test", kBadIv.Decrypt(k1.Encrypt("test")));
                }
                catch (System.Security.Cryptography.CryptographicException) { }
            }
        }
        
        [Test]
        public void TestCopyReadWriteBytes()
        {
            byte[] ivrandom = new byte[16];
            new Random().NextBytes(ivrandom);

            using (AESCryptoKey k1 = new AESCryptoKey())
            using (AESCryptoKey k2 = AESCryptoKey.FromBytes(k1.ToArray()))
            {
                Assert.AreEqual(k1.Key, k2.Key);
                Assert.AreEqual(k1.IV, k2.IV);
            }
        }

        [Test]
        public void TestSimpleEncryptDecrypt()
        {
            using (AESCryptoKey k1 = new AESCryptoKey())
            {
                byte[] test = new byte[240];
                byte[] cypher1 = k1.Encrypt(test);
                byte[] cypher2 = k1.Encrypt(test);
                Assert.AreEqual(cypher1, cypher2);
                Assert.AreEqual(test, k1.Decrypt(cypher1));
            }
        }

        [Test]
        public void TestDefaultIv()
        {
            Assembly asm = (Assembly.GetEntryAssembly() ?? typeof(AESCryptoKey).Assembly);
            byte[] pk = Encoding.UTF8.GetBytes(asm.GetName().Name);
            string expect = Hash.MD5(pk).ToString();
            Assert.AreEqual(expect, Convert.ToBase64String(AESCryptoKey.ProcessDefaultIV));
        }

        [Test]
        public void TestChangeDefaultIv()
        {
            byte[] original = AESCryptoKey.ProcessDefaultIV;
            try
            {
                Assert.AreEqual(original, new AESCryptoKey().IV);

                byte[] newIv = new byte[16];
                new Random().NextBytes(newIv);
                AESCryptoKey.ProcessDefaultIV = newIv;

                Assert.AreEqual(newIv, AESCryptoKey.ProcessDefaultIV);
                Assert.AreEqual(newIv, new AESCryptoKey().IV);
            }
            finally
            {
                AESCryptoKey.ProcessDefaultIV = original;
            }
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestBadInputKey()
        {
            new AESCryptoKey(new byte[5], new byte[16]);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestBadInputIV()
        {
            new AESCryptoKey(new byte[32], new byte[10]);
        }

        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposal()
        {
            string test;
            CryptoKey key = new AESCryptoKey();
            test = key.Encrypt("Test");
            key.Dispose();

            key.Decrypt(test);
            Assert.Fail();
        }
    }

	public abstract class TestEncryption
    {
        protected const string TEST_PASSWORD = "This is the password\u1235!";
        protected readonly IEncryptDecrypt Encryptor;

        protected TestEncryption(IEncryptDecrypt encryptor) { this.Encryptor = encryptor; }

		[Test]
		public void TestEncryptDecryptString()
		{
            string svalue = TEST_PASSWORD;

			string esbyuser = Encryptor.Encrypt(svalue);
			Assert.AreNotEqual(svalue, esbyuser);
			Assert.AreEqual(svalue, Encryptor.Decrypt(esbyuser));
        }

        [Test]
        public void TestEncryptDecryptStringBase64()
        {
            string svalue = TEST_PASSWORD;

            string esbyuser = Encryptor.Encrypt(svalue, ByteEncoding.Base64);
            Assert.AreNotEqual(svalue, esbyuser);
            Assert.AreEqual(svalue, Encryptor.Decrypt(esbyuser, ByteEncoding.Base64));
        }

        [Test]
        public void TestEncryptDecryptStringSafe64()
        {
            string svalue = TEST_PASSWORD;

            string esbyuser = Encryptor.Encrypt(svalue, ByteEncoding.Safe64);
            Assert.AreNotEqual(svalue, esbyuser);
            Assert.AreEqual(svalue, Encryptor.Decrypt(esbyuser, ByteEncoding.Safe64));
        }

        [Test]
        public void TestEncryptDecryptStringHex()
        {
            string svalue = TEST_PASSWORD;

            string esbyuser = Encryptor.Encrypt(svalue, ByteEncoding.Hex);
            Assert.AreNotEqual(svalue, esbyuser);
            Assert.AreEqual(svalue, Encryptor.Decrypt(esbyuser, ByteEncoding.Hex));
        }

		[Test]
		public void TestEncryptDecryptBytes()
		{
            byte[] svalue = Encoding.ASCII.GetBytes(TEST_PASSWORD);

			byte[] esbyuser = Encryptor.Encrypt(svalue);
			Assert.AreNotEqual(Encoding.ASCII.GetString(svalue), Encoding.ASCII.GetString(esbyuser));
            Assert.AreEqual(Encoding.ASCII.GetString(svalue), Encoding.ASCII.GetString(Encryptor.Decrypt(esbyuser)));
		}

        [Test]
        public void TestEncryptDecryptStream()
        {
            byte[] result;
            byte[] data = new byte[ushort.MaxValue];
            Random rand = new Random();
            rand.NextBytes(data);

            using (MemoryStream final = new MemoryStream())
            using (MemoryStream ms = new MemoryStream())
            {
                using (Stream e = Encryptor.Encrypt(new NonClosingStream(ms)))
                {
                    for (int i = 0; i < data.Length; )
                    {
                        int count = Math.Max(i + 1, Math.Min(data.Length, i + 1 + rand.Next(2000)));
                        e.Write(data, i, count - i);
                        i = count;
                    }
                    e.Flush();
                }

                using (Stream d = Encryptor.Decrypt(new MemoryStream(ms.ToArray())))
                {
                    for (int i = 0; i < data.Length; )
                    {
                        int count = Math.Max(i + 1, Math.Min(data.Length, i + 1 + rand.Next(2000)));
                        byte[] tmp = IOStream.Read(d, count - i);
                        final.Write(tmp, 0, tmp.Length);
                        i = count;
                    }
                }

                result = final.ToArray();
            }

            Assert.AreEqual(data.Length, result.Length);
            Assert.IsTrue(BinaryComparer.Equals(data, result));
        }
	}

    [TestFixture]
    public class TestPassthrough
    {
        protected const string TEST_PASSWORD = "This is the password\u1235!";

        [Test]
        public void TestExactPaasstrough()
        {
            string value = TEST_PASSWORD;

            Encryption.Passthrough.Dispose();//ignores

            Assert.AreEqual(value, Encryption.Passthrough.Encrypt(value));
            Assert.AreEqual(value, Encryption.Passthrough.Encrypt(value, ByteEncoding.Hex));
            Assert.AreEqual(value, Encryption.Passthrough.Decrypt(value));
            Assert.AreEqual(value, Encryption.Passthrough.Decrypt(value, ByteEncoding.Hex));

            byte[] bytes = Password.Encoding.GetBytes(value);
            Assert.AreEqual(bytes, Encryption.Passthrough.Encrypt(bytes));
            Assert.AreEqual(bytes, Encryption.Passthrough.Decrypt(bytes));

            Assert.AreEqual(bytes, IOStream.ReadAllBytes(Encryption.Passthrough.Encrypt(new MemoryStream(bytes))));
            Assert.AreEqual(bytes, IOStream.ReadAllBytes(Encryption.Passthrough.Decrypt(new MemoryStream(bytes))));
        }
    }
}
