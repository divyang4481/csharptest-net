#region Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public partial class TestAsciiEncoding
    {
        void TestEncoderAgainstBase64(int repeat, int size)
        {
            Random rand = new Random();
            byte[] data = new byte[size];

            while (repeat-- > 0)
            {
                rand.NextBytes(data);
                string testB64 = Convert.ToBase64String(data);
                string testAsc = AsciiEncoder.EncodeBytes(data);

                Assert.AreEqual(testB64.Replace('+', '-').Replace('/', '_').Replace("=", ""), testAsc);
                Assert.AreEqual(0, BinaryComparer.Compare(data, AsciiEncoder.DecodeBytes(testAsc)));
            }
        }

        [Test]
        public void TestAsciiEncoder_sz1024()
        { TestEncoderAgainstBase64(100, 1024); }

        [Test]
        public void TestAsciiEncoder_sz1025()
        { TestEncoderAgainstBase64(100, 1025); }

        [Test]
        public void TestAsciiEncoder_sz1026()
        { TestEncoderAgainstBase64(100, 1026); }

        [Test]
        public void TestAsciiEncoder_sz1027()
        { TestEncoderAgainstBase64(100, 1027); }

        [Test]
        public void TestAsciiEncoder_sz8192()
        { TestEncoderAgainstBase64(100, 8192); }

        [Test]
        public void TestAsciiEncodeLargeArray()
        {
            Random rand = new Random();
            byte[] data = new byte[0x400000];

            rand.NextBytes(data);
            string testAsc = AsciiEncoder.EncodeBytes(data);
            Assert.AreEqual(0, BinaryComparer.Compare(data, AsciiEncoder.DecodeBytes(testAsc)));
        }

        [Test]
        public void TestAsciiEncodeAllChars()
        {
            //char count must by multiple of 4 for the compare to work
            string encoded = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
            byte[] data = AsciiEncoder.DecodeBytes(encoded);
            Assert.AreEqual(encoded, AsciiEncoder.EncodeBytes(data));
        }

        [Test, ExpectedException(typeof(IndexOutOfRangeException))]
        public void TestBadInputCharacter()
        {
            byte[] trash = new byte[] { (byte)'a', (byte)'b', (byte)'c', 'z' + 1 };
            AsciiEncoder.DecodeBytes(trash);
            Assert.Fail();
        }
    }
}
