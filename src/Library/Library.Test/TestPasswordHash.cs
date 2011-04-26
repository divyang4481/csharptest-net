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
using System.Text;
using System.IO;
using System.Security;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public partial class TestPasswordHash
    {
        const string TEST_PASSWORD = "This is the password\u1235!";

        [Test]
        public void TestCreateHash()
        {
            using (PasswordHash pwd1 = new PasswordHash(TEST_PASSWORD))
            using (PasswordHash pwd2 = new PasswordHash(TEST_PASSWORD))
            {
                Assert.AreNotEqual(pwd1, pwd2);
                Assert.AreNotEqual(pwd1.Salt.ToArray(), pwd2.Salt.ToArray());
                Assert.AreNotEqual(pwd1.ToArray(), pwd2.ToArray());
            }
        }
        [Test]
        public void TestClearBytes()
        {
            byte[] password = new byte[16];
            new Random().NextBytes(password);

            using (PasswordHash pwd1 = new PasswordHash(false, password))
                Assert.IsTrue(pwd1.VerifyPassword(password));

            using (PasswordHash pwd1 = new PasswordHash(true, password))
                Assert.AreEqual(new byte[16], password);
        }
        [Test]
        public void TestCreateHashFromBytes()
        {
            using (PasswordHash pwd1 = new PasswordHash(TEST_PASSWORD))
            using (PasswordHash pwd2 = PasswordHash.FromBytes(pwd1.ToArray()))
            {
                Assert.AreEqual(pwd1, pwd2);
                Assert.AreEqual((256 / 8) + pwd1.Salt.Length, pwd2.Length);
                Assert.AreEqual(pwd1.Length, pwd2.Length);
                Assert.AreEqual(pwd1.Salt.ToArray(), pwd2.Salt.ToArray());
                Assert.AreEqual(pwd1.ToArray(), pwd2.ToArray());
            }
        }
        [Test]
        public void TestCreateHashFromString()
        {
            using (PasswordHash pwd1 = new PasswordHash(TEST_PASSWORD))
            using (PasswordHash pwd2 = PasswordHash.FromString(pwd1.ToString()))
            {
                Assert.AreEqual(pwd1, pwd2);
                Assert.AreEqual(pwd1.Salt.ToArray(), pwd2.Salt.ToArray());
                Assert.AreEqual(pwd1.ToArray(), pwd2.ToArray());
            }
        }
        [Test]
        public void TestValidatePassword()
        {
            using (PasswordHash pwd1 = new PasswordHash(TEST_PASSWORD))
            {
                Assert.IsFalse(pwd1.VerifyPassword(TEST_PASSWORD + " "));
                Assert.IsFalse(pwd1.VerifyPassword(TEST_PASSWORD.Substring(0, TEST_PASSWORD.Length-1)));
                Assert.IsFalse(pwd1.VerifyPassword(""));

                Assert.IsTrue(pwd1.VerifyPassword(TEST_PASSWORD));
                Assert.IsTrue(pwd1.VerifyPassword(Password.Encoding.GetBytes(TEST_PASSWORD)));
                Assert.IsTrue(pwd1.VerifyPassword(new MemoryStream(Password.Encoding.GetBytes(TEST_PASSWORD))));
                Assert.IsTrue(pwd1.VerifyPassword(new Password(TEST_PASSWORD)));
            }
        }
        [Test]
        public void TestEquality()
        {
            using (PasswordHash pwd1 = new PasswordHash(TEST_PASSWORD))
            using (PasswordHash pwd2 = new PasswordHash(TEST_PASSWORD, pwd1.Salt))
            {
                Assert.AreEqual(pwd1, pwd2);
                Assert.AreEqual(pwd1.Salt.ToArray(), pwd2.Salt.ToArray());
                Assert.AreEqual(pwd1.ToArray(), pwd2.ToArray());

                Assert.IsTrue(pwd1 == pwd2);
                Assert.IsTrue(pwd1 == pwd2);
                Assert.IsFalse(pwd1 != pwd2);
                Assert.IsTrue(pwd1.Equals(pwd2));
                Assert.IsTrue(pwd1.Equals((object)pwd2));
                Assert.AreEqual(0, pwd1.CompareTo(pwd2));
                Assert.AreEqual(pwd1.GetHashCode(), pwd2.GetHashCode());
                Assert.AreEqual(pwd1.ToString(), pwd2.ToString());

                PasswordHash nil = null;
                Assert.IsFalse(pwd1 < nil);
                Assert.IsTrue(pwd1 > nil);
                Assert.AreEqual(1, pwd1.CompareTo(null));
            }
        }
        [Test]
        public void TestConstruction()
        {
            using (PasswordHash pwd1 = new PasswordHash(TEST_PASSWORD))
            {
                //no salt provided
                using (PasswordHash pwd2 = new PasswordHash(TEST_PASSWORD))
                    Assert.AreNotEqual(pwd1, pwd2);
                using (PasswordHash pwd2 = new PasswordHash(false, Password.Encoding.GetBytes(TEST_PASSWORD)))
                    Assert.AreNotEqual(pwd1, pwd2);
                //identical salt
                using (PasswordHash pwd2 = new PasswordHash(TEST_PASSWORD, pwd1.Salt))
                    Assert.AreEqual(pwd1, pwd2);
                using (PasswordHash pwd2 = new PasswordHash(false, Password.Encoding.GetBytes(TEST_PASSWORD), pwd1.Salt))
                    Assert.AreEqual(pwd1, pwd2);
                using (PasswordHash pwd2 = new PasswordHash(new MemoryStream(Password.Encoding.GetBytes(TEST_PASSWORD)), pwd1.Salt))
                    Assert.AreEqual(pwd1, pwd2);

                using (PasswordHash pwd2 = new PasswordHash(new Password(TEST_PASSWORD), pwd1.Salt))
                    Assert.AreEqual(pwd1, pwd2);
                using (SecureString sstr = SecureStringUtils.Create(TEST_PASSWORD))
                {
                    using (PasswordHash pwd2 = new PasswordHash(sstr, pwd1.Salt))
                        Assert.AreEqual(pwd1, pwd2);
                    using (PasswordHash pwd2 = new PasswordHash(sstr))
                        Assert.AreNotEqual(pwd1, pwd2);
                }
            }
        }
    }
}
