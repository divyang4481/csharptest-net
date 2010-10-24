#region Copyright 2009-2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CSharpTest.Net.Crypto;
using CSharpTest.Net.IO;
using NUnit.Framework;
using CSharpTest.Net.Formatting;

#pragma warning disable 1591, 618
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public partial class TestSafe64Encoding
    {
        void TestEncoderAgainstBase64(int repeat, int size)
        {
            Random rand = new Random();
            byte[] data = new byte[size];

            while (repeat-- > 0)
            {
                rand.NextBytes(data);
                string testB64 = Convert.ToBase64String(data);
                string testAsc = Safe64Encoding.EncodeBytes(data);

                Assert.AreEqual(testB64.Replace('+', '-').Replace('/', '_').Replace("=", ""), testAsc);
                Assert.AreEqual(0, BinaryComparer.Compare(data, Safe64Encoding.DecodeBytes(testAsc)));
                Assert.AreEqual(0, BinaryComparer.Compare(data, AsciiEncoder.DecodeBytes(testAsc)));
            }
        }

        [Test]
        public void TestSafe64Encoding_sz1024()
        { TestEncoderAgainstBase64(100, 1024); }

        [Test]
        public void TestSafe64Encoding_sz1025()
        { TestEncoderAgainstBase64(100, 1025); }

        [Test]
        public void TestSafe64Encoding_sz1026()
        { TestEncoderAgainstBase64(100, 1026); }

        [Test]
        public void TestSafe64Encoding_sz1027()
        { TestEncoderAgainstBase64(100, 1027); }

        [Test]
        public void TestSafe64Encoding_sz8192()
        { TestEncoderAgainstBase64(100, 8192); }

        [Test]
        public void TestSafe64EncodingLargeArray()
        {
            Random rand = new Random();
            byte[] data = new byte[0x400000];

            rand.NextBytes(data);
            string testAsc = Safe64Encoding.EncodeBytes(data);
            Assert.AreEqual(0, BinaryComparer.Compare(data, Safe64Encoding.DecodeBytes(testAsc)));
            testAsc = AsciiEncoder.EncodeBytes(data);
            Assert.AreEqual(0, BinaryComparer.Compare(data, AsciiEncoder.DecodeBytes(testAsc)));
        }

        [Test]
        public void TestSafe64EncodingAllChars()
        {
            //char count must by multiple of 4 for the compare to work
            string encoded = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
            byte[] data = Safe64Encoding.DecodeBytes(encoded);
            Assert.AreEqual(encoded, Safe64Encoding.EncodeBytes(data));
            data = AsciiEncoder.DecodeBytes(encoded);
            Assert.AreEqual(encoded, AsciiEncoder.EncodeBytes(data));
            data = AsciiEncoder.DecodeBytes(Encoding.ASCII.GetBytes(encoded));
            Assert.AreEqual(encoded, AsciiEncoder.EncodeBytes(data));
        }

        [Test]
        public void TestSafe64StreamRead()
        {
            string encoded = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
            byte[] data = Safe64Encoding.DecodeBytes(encoded);
            byte[] test;
            using (Stream io = new Safe64Stream(new MemoryStream(Encoding.ASCII.GetBytes(encoded)), CryptoStreamMode.Read))
                test = IOStream.Read(io, data.Length);

            Assert.AreEqual(data, test);
        }

        [Test]
        public void TestSafe64StreamWrite()
        {
            byte[] data = new byte[222];
            new Random().NextBytes(data);
            using (Stream mem = new MemoryStream())
            {
                using (Stream io = new Safe64Stream(new NonClosingStream(mem), CryptoStreamMode.Write))
                    io.Write(data, 0, data.Length);


                Assert.AreEqual((long)Math.Ceiling((data.Length * 8) / 6d), mem.Position);
                mem.Position = 0;
                string test = new StreamReader(mem).ReadToEnd();
                Assert.AreEqual(Safe64Encoding.EncodeBytes(data), test);
            }
        }

        [Test]
        public void TestSafe64TransformProperties()
        {
            using (ICryptoTransform xform = new Safe64Stream.Transform(CryptoStreamMode.Read))
            {
                Assert.AreEqual(4, xform.InputBlockSize);
                Assert.AreEqual(3, xform.OutputBlockSize);
                Assert.AreEqual(true, xform.CanReuseTransform);
                Assert.AreEqual(true, xform.CanTransformMultipleBlocks);
            }
            using (ICryptoTransform xform = new Safe64Stream.Transform(CryptoStreamMode.Write))
            {
                Assert.AreEqual(3, xform.InputBlockSize);
                Assert.AreEqual(4, xform.OutputBlockSize);
                Assert.AreEqual(true, xform.CanReuseTransform);
                Assert.AreEqual(true, xform.CanTransformMultipleBlocks);
            }
        }

        [Test, ExpectedException(typeof(IndexOutOfRangeException))]
        public void TestBadInputCharacter()
        {
            byte[] trash = new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'\0' };
            Safe64Encoding.DecodeBytes(trash);
            Assert.Fail();
        }
    }

    [TestFixture]
    public class TestBase64Stream
    {
        [Test]
        public void TestBase64StreamRead()
        {
            string encoded = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+/9w==";
            byte[] data = Convert.FromBase64String(encoded);
            byte[] test;
            using (Stream io = new Base64Stream(new MemoryStream(Encoding.ASCII.GetBytes(encoded)), CryptoStreamMode.Read))
                test = IOStream.Read(io, data.Length);
            Assert.AreEqual(data, test);

            using (Stream io = new Base64Stream(new MemoryStream(Encoding.ASCII.GetBytes(encoded.TrimEnd('='))), CryptoStreamMode.Read))
                test = IOStream.Read(io, data.Length);
            Assert.AreEqual(data, test);
        }

        [Test]
        public void TestBase64StreamWrite()
        {
            byte[] data = new byte[256];
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)i;

            using (Stream mem = new MemoryStream())
            {
                using (Stream io = new Base64Stream(new NonClosingStream(mem), CryptoStreamMode.Write))
                    io.Write(data, 0, data.Length);


                Assert.AreEqual(((data.Length + 2) / 3 * 4), mem.Position);
                mem.Position = 0;
                string test = new StreamReader(mem).ReadToEnd();
                Assert.AreEqual(Convert.ToBase64String(data), test);
            }
        }

        [Test]
        public void TestBasee64TransformProperties()
        {
            using (ICryptoTransform xform = new Base64Stream.Transform(CryptoStreamMode.Read))
            {
                Assert.AreEqual(4, xform.InputBlockSize);
                Assert.AreEqual(3, xform.OutputBlockSize);
                Assert.AreEqual(true, xform.CanReuseTransform);
                Assert.AreEqual(true, xform.CanTransformMultipleBlocks);
            }
            using (ICryptoTransform xform = new Base64Stream.Transform(CryptoStreamMode.Write))
            {
                Assert.AreEqual(3, xform.InputBlockSize);
                Assert.AreEqual(4, xform.OutputBlockSize);
                Assert.AreEqual(true, xform.CanReuseTransform);
                Assert.AreEqual(true, xform.CanTransformMultipleBlocks);
            }
        }
    }
}
