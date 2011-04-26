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
using System.Security;
using CSharpTest.Net.Crypto;
using System.IO;
using System.Text;
using CSharpTest.Net.IO;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public partial class TestSecureString
    {
        protected const string TEST_PASSWORD = "This is the password\u1235!";

        IEnumerable<SecureString> MakeStrings()
        {
            yield return SecureStringUtils.Create(TEST_PASSWORD);
            yield return SecureStringUtils.Create(TEST_PASSWORD.ToCharArray());
            yield return SecureStringUtils.Create(new MemoryStream(Encoding.Unicode.GetBytes(TEST_PASSWORD)));
            yield return SecureStringUtils.Create(new MemoryStream(Encoding.UTF8.GetBytes(TEST_PASSWORD)), Encoding.UTF8);
            yield return SecureStringUtils.AppendAll(new SecureString(), TEST_PASSWORD);
        }

        [Test]
        public void TestSecureStringReader()
        {
            foreach (SecureString ss in MakeStrings())
            {
                Assert.AreEqual(TEST_PASSWORD, SecureStringUtils.ToTextReader(ss).ReadToEnd());

                StringBuilder sb = new StringBuilder();
                using (TextReader r = SecureStringUtils.ToTextReader(ss))
                {
                    while (r.Peek() != -1)
                    {
                        sb.Append((char)r.Read());
                        int i = r.Read();
                        if (i != -1)
                            sb.Append((char)i);
                    }
                }
                Assert.AreEqual(TEST_PASSWORD, sb.ToString());

                char[] buffer = new char[TEST_PASSWORD.Length];
                using (TextReader r = new UnicodeReader(SecureStringUtils.ToStream(ss)))
                {
                    for (int i = 0; i < buffer.Length; i++)
                        Assert.AreEqual(1, r.ReadBlock(buffer, i, buffer.Length - i));
                    Assert.AreEqual(0, r.ReadBlock(buffer, 0, buffer.Length));
                }
                Assert.AreEqual(TEST_PASSWORD, new string(buffer));
            }
        }

        [Test]
        public void TestSecureStringToChars()
        {
            foreach (SecureString ss in MakeStrings())
            {
                Assert.AreEqual(TEST_PASSWORD, new String(SecureStringUtils.ToCharArray(ss)));
            }
        }

        [Test]
        public void TestSecureStringCopyChars()
        {
            foreach (SecureString ss in MakeStrings())
            {
                char[] temp = new char[TEST_PASSWORD.Length];
                SecureStringUtils.CopyChars(ss, 0, temp, 0, temp.Length);
                Assert.AreEqual(TEST_PASSWORD, new String(temp));

                temp = new char[TEST_PASSWORD.Length];
                SecureStringUtils.CopyChars(ss, 4, temp, 4, temp.Length - 4);
                Assert.AreNotEqual(TEST_PASSWORD, new String(temp));
                SecureStringUtils.CopyChars(ss, 0, temp, 0, 4);
                Assert.AreEqual(TEST_PASSWORD, new String(temp));
            }
        }

        [Test]
        public void TestSecureStringToByteArray()
        {
            foreach (SecureString ss in MakeStrings())
            {
                Assert.AreEqual(Encoding.Unicode.GetBytes(TEST_PASSWORD), SecureStringUtils.ToByteArray(ss));
                Assert.AreEqual(Encoding.UTF8.GetBytes(TEST_PASSWORD), SecureStringUtils.ToByteArray(ss, Encoding.UTF8));
            }
        }

        [Test]
        public void TestSecureStringToStream()
        {
            byte[] expect = Encoding.Unicode.GetBytes(TEST_PASSWORD);
            foreach (SecureString ss in MakeStrings())
            {
                Assert.AreEqual(expect, IOStream.ReadAllBytes(SecureStringUtils.ToStream(ss)));
                Assert.AreEqual(TEST_PASSWORD, IOStream.ReadAllText(SecureStringUtils.ToStream(ss), Encoding.Unicode));
            }
        }
    }
}
