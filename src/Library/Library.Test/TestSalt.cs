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
using CSharpTest.Net.IO;
using System.Text;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public partial class TestSalt
    {
        [Test]
        public void TestCreateSalt()
        {
            Salt s = new Salt();
            Assert.AreEqual(32, s.Length);
            Assert.AreNotEqual(new byte[32], s.ToArray());

            Salt s8 = new Salt(Salt.Size.b64);
            Assert.AreEqual(8, s8.Length);
            Assert.AreEqual(Salt.Size.b64, s8.BitSize);
            
            Salt s16 = new Salt(Salt.Size.b128);
            Assert.AreEqual(16, s16.Length);
            Assert.AreEqual(Salt.Size.b128, s16.BitSize);

            Salt s32 = new Salt(Salt.Size.b256);
            Assert.AreEqual(32, s32.Length);
            Assert.AreEqual(Salt.Size.b256, s32.BitSize);

            Salt s64 = new Salt(Salt.Size.b512);
            Assert.AreEqual(64, s64.Length);
            Assert.AreEqual(Salt.Size.b512, s64.BitSize);
        }

        [Test]
        public void TestSaltCopy()
        {
            Salt s = new Salt();
            byte[] bytes = new byte[s.Length];
            s.CopyTo(bytes, 0);
            Assert.AreEqual(s.ToArray(), bytes);
            Assert.AreEqual(s.GetHashCode(), Salt.FromBytes(bytes).GetHashCode());

			Salt strcpy = Salt.FromString(s.ToString());
			Assert.AreEqual(s.ToArray(), strcpy.ToArray());
			Assert.AreEqual(s.ToString(), strcpy.ToString());
			Assert.AreEqual(s.GetHashCode(), strcpy.GetHashCode());
		}

        [Test]
        public void TestSaltStream()
        {
            Salt s = new Salt();
            Assert.AreEqual(s.ToArray(), IOStream.ReadAllBytes(s.ToStream()));
        }

        [Test]
        public void TestCreateSaltBytes()
        {
            byte[] test = new byte[16];
            Assert.AreEqual(new byte[16], test);
            Salt.CreateBytes(test);
            Assert.AreNotEqual(new byte[16], test);
            Assert.AreNotEqual(Salt.CreateBytes(Salt.Size.b128), test);
        }

        [Test]
        public void TestEquality()
        {
            Salt s = new Salt();
            Salt scopy = Salt.FromBytes(s.ToArray());

            Assert.AreEqual(s, scopy);
            Assert.IsTrue(s.Equals(scopy));
            Assert.IsTrue(s.Equals((object)scopy));
            Assert.IsTrue(s == scopy);
            Assert.IsFalse(s != scopy);
            Assert.AreEqual(s.GetHashCode(), scopy.GetHashCode());

            scopy = new Salt();
            Assert.AreNotEqual(s, scopy);
            Assert.IsFalse(s.Equals(scopy));
            Assert.IsFalse(s.Equals((object)scopy));
            Assert.IsFalse(s == scopy);
            Assert.IsTrue(s != scopy);
            Assert.AreNotEqual(s.GetHashCode(), scopy.GetHashCode());
        }

        [Test]
        public void TestSaltGetData()
        {
            Salt s = new Salt();
            byte[] bytes = new byte[1024];
            new Random().NextBytes(bytes);

            SaltedData sd = s.GetData(bytes);
            Assert.AreEqual(s, sd.Salt);
            Assert.AreEqual(bytes, sd.GetDataBytes());

            sd = s.GetData(new MemoryStream(bytes));
            Assert.AreEqual(s, sd.Salt);
            Assert.AreEqual(bytes, sd.GetDataBytes());
        }

        [Test]
        public void TestSaltFromBytes()
        {
            byte[] testValid = Encoding.ASCII.GetBytes("12345678");
            Assert.AreEqual(Salt.Size.b64, Salt.FromBytes(testValid).BitSize);
            Assert.AreEqual(testValid, Salt.FromBytes(testValid).ToArray());
            testValid = null;

            //now test with an odd number of bytes, should always hash to Sha256
            byte[] notValid = Encoding.ASCII.GetBytes("0123456789");
            Assert.AreNotEqual(Salt.Size.b64, Salt.FromBytes(notValid).BitSize);
            Assert.AreNotEqual(testValid, Salt.FromBytes(notValid).ToArray());

            Assert.AreEqual(Salt.Size.b256, Salt.FromBytes(notValid).BitSize);
            Assert.AreEqual(Hash.SHA256(notValid).ToArray(), Salt.FromBytes(notValid).ToArray());
        }

        [Test]
        public void TestSaltedDataWithDefaultSize()
        {
            Salt s = new Salt();
            byte[] testData = new byte[8];
            new Random().NextBytes(testData);
            byte[] tmp;

            using (SaltedData sd = new SaltedData(s, testData))
            {
                Assert.AreEqual(40, sd.Length);
                Assert.AreEqual(s, sd.Salt);
                Assert.AreEqual(testData, sd.GetDataBytes());

                tmp = sd.ToArray();
                Assert.AreEqual(40, tmp.Length);
                Assert.AreEqual(tmp, IOStream.ReadAllBytes(sd.ToStream()));
            }

            using (SaltedData sd = new SaltedData(tmp))
            {
                Assert.AreEqual(s, sd.Salt);
                Assert.AreEqual(testData, sd.GetDataBytes());
                Assert.AreEqual(tmp, sd.ToArray());
            }

            using (SaltedData sd = new SaltedData(new MemoryStream(tmp)))
            {
                Assert.AreEqual(s, sd.Salt);
                Assert.AreEqual(testData, sd.GetDataBytes());
                Assert.AreEqual(tmp, sd.ToArray());
            }
        }
        [Test]
        public void TestSaltedDataWithSpecificSize()
        {
            Salt s = new Salt(Salt.Size.b64);
            byte[] testData = new byte[8];
            new Random().NextBytes(testData);
            byte[] tmp;

            using (SaltedData sd = new SaltedData(s, testData))
            {
                Assert.AreEqual(16, sd.Length);
                Assert.AreEqual(s, sd.Salt);
                Assert.AreEqual(testData, sd.GetDataBytes());

                tmp = sd.ToArray();
                Assert.AreEqual(16, tmp.Length);
                Assert.AreEqual(tmp, IOStream.ReadAllBytes(sd.ToStream()));
            }

            using (SaltedData sd = new SaltedData(s, new MemoryStream(testData)))
            {
                Assert.AreEqual(s, sd.Salt);
                Assert.AreEqual(testData, sd.GetDataBytes());
                Assert.AreEqual(tmp, sd.ToArray());
            }

            using (SaltedData sd = new SaltedData(tmp, Salt.Size.b64))
            {
                Assert.AreEqual(s, sd.Salt);
                Assert.AreEqual(testData, sd.GetDataBytes());
                Assert.AreEqual(tmp, sd.ToArray());
            }
        }
        [Test]
        public void TestSaltedDataStream()
        {
            Salt s = new Salt(Salt.Size.b64);
            byte[] testData = new byte[8];

            byte[] test1 = new SaltedData(s, testData).ToArray();
            Assert.AreEqual(16, test1.Length);
            byte[] test2 = IOStream.ReadAllBytes(new SaltedData(s, testData).ToStream());
            Assert.AreEqual(16, test2.Length);
            byte[] test3 = IOStream.ReadAllBytes(SaltedData.CombineStream(s, new MemoryStream(testData)));
            Assert.AreEqual(16, test3.Length);

            Assert.AreEqual(test1, test2);
            Assert.AreEqual(test1, test3);
            Assert.AreEqual(test2, test3);
        }
    }
}
