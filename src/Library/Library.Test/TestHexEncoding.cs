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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CSharpTest.Net.Formatting;
using CSharpTest.Net.Crypto;
using CSharpTest.Net.IO;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestHexEncoding
    {
        const string AllHex = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                                "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f" +
                                "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f" +
                                "606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f" +
                                "808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f" +
                                "a0a1a2a3a4a5a6a7a8a9aaabacadaeafb0b1b2b3b4b5b6b7b8b9babbbcbdbebf" +
                                "c0c1c2c3c4c5c6c7c8c9cacbcccdcecfd0d1d2d3d4d5d6d7d8d9dadbdcdddedf" +
                                "e0e1e2e3e4e5e6e7e8e9eaebecedeeeff0f1f2f3f4f5f6f7f8f9fafbfcfdfeff";

        [Test]
        public void TestReadHexStream()
        {
            using (Stream io = new HexStream(new MemoryStream(Encoding.ASCII.GetBytes(AllHex)), CryptoStreamMode.Read))
            {
                byte[] results = IOStream.ReadAllBytes(io);
                Assert.AreEqual(AllHex.Length / 2, results.Length);
                for (int i = 0; i < 256; i++)
                    Assert.AreEqual(i, results[i]);
            }
            using (Stream io = new HexStream(new MemoryStream(Encoding.ASCII.GetBytes(AllHex + " ")), CryptoStreamMode.Read))
            {
                byte[] results = IOStream.ReadAllBytes(io);
                Assert.AreEqual(AllHex.Length / 2, results.Length);
                for (int i = 0; i < 256; i++)
                    Assert.AreEqual(i, results[i]);
            }
        }
        [Test]
        public void TestWriteHexStream()
        {
            using (Stream mem = new MemoryStream())
            {
                using (Stream io = new HexStream(new NonClosingStream(mem), CryptoStreamMode.Write))
                    io.Write(HexEncoding.DecodeBytes(AllHex), 0, AllHex.Length / 2);

                Assert.AreEqual(AllHex.Length, mem.Position);
                mem.Position = 0;
                string test = new StreamReader(mem).ReadToEnd();
                Assert.AreEqual(AllHex, test);
            }
        }
        [Test]
        public void TestHexTransformProperties()
        {
            using (ICryptoTransform xform = new HexStream.Transform(CryptoStreamMode.Read))
            {
                Assert.AreEqual(2, xform.InputBlockSize);
                Assert.AreEqual(1, xform.OutputBlockSize);
                Assert.AreEqual(true, xform.CanReuseTransform);
                Assert.AreEqual(true, xform.CanTransformMultipleBlocks);
            }
            using (ICryptoTransform xform = new HexStream.Transform(CryptoStreamMode.Write))
            {
                Assert.AreEqual(1, xform.InputBlockSize);
                Assert.AreEqual(2, xform.OutputBlockSize);
                Assert.AreEqual(true, xform.CanReuseTransform);
                Assert.AreEqual(true, xform.CanTransformMultipleBlocks);
            }
        }
        [Test]
        public void TestToFromHex()
        {
            byte[] all = new byte[256];
            for (int i = 0; i < all.Length; i++)
                all[i] = (byte)i;

            Assert.AreEqual(AllHex, HexEncoding.EncodeBytes(all));
            Assert.AreEqual(0, BinaryComparer.Compare(all, HexEncoding.DecodeBytes(AllHex)));
            Assert.AreEqual(0, BinaryComparer.Compare(all, HexEncoding.DecodeBytes(AllHex.ToUpper())));
        }
        [Test]
        public void TestToFromHexPartial()
        {
            const string hex = "0102030405060708090a0b0c0d0e0f10";
            byte[] bin = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

            Assert.AreEqual(hex.Substring(4, 10), HexEncoding.EncodeBytes(bin, 2, 5));
            Assert.AreEqual(0, BinaryComparer.Compare(new byte[] { 3, 4, 5, 6 }, HexEncoding.DecodeBytes(hex, 4, 8)));
        }
        [Test]
        public void TestFromHexWithSpace()
        {
            const string hex = "01-0203-0405\t0607\r\n0809\r0a0b0c\n0d0e0f10 ";
            byte[] bin = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

            Assert.AreEqual(0, BinaryComparer.Compare(bin, HexEncoding.DecodeBytes(hex)));
            Assert.AreEqual(0, BinaryComparer.Compare(bin, HexEncoding.DecodeBytes(Encoding.ASCII.GetBytes(hex))));
        }
        [Test, ExpectedException(typeof(FormatException))]
        public void TestFromHexWithIllegalCharacter()
        {
            const string hex = "0x01";
            HexEncoding.DecodeBytes(hex);
        }
        [Test, ExpectedException(typeof(FormatException))]
        public void TestFromHexWithUnevenCount()
        {
            const string hex = "0a 0";
            HexEncoding.DecodeBytes(hex);
        }
    }
}
