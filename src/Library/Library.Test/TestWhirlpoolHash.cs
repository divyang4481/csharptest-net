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
using System.Text;
using NUnit.Framework;
using CSharpTest.Net.Crypto;
using System.Security.Cryptography;
using System.Diagnostics;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestWhirlpoolHash
    {
        static byte[] FromHex(string data)
        {
            data = data.Replace(" ", "");
            byte[] bytes = new byte[data.Length / 2];
            for (int i = 0; i < data.Length; i += 2)
            {
                bytes[i / 2] = Byte.Parse(data.Substring(i, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            return bytes;
        }

        [Test]
        public void TestRJ()
        {
            RijndaelManaged m = new RijndaelManaged();
            KeySizes[] sz = m.LegalKeySizes;
        }


        [Test]
        public void Test()
        {
            //result values taken from executing makeISOTestVectors() in the original 'c' code.
            WhirlpoolManaged alg = new WhirlpoolManaged();
            Assert.AreEqual(
                FromHex("19FA61D75522A466 9B44E39C1D2E1726 C530232130D407F8 9AFEE0964997F7A7 3E83BE698B288FEB CF88E3E03C4F0757 EA8964E59B63D937 08B138CC42A66EB3"),
                alg.ComputeHash(System.Text.Encoding.ASCII.GetBytes(""))
            );
            Assert.AreEqual(
                FromHex("8ACA2602792AEC6F 11A67206531FB7D7 F0DFF59413145E69 73C45001D0087B42 D11BC645413AEFF6 3A42391A39145A59 1A92200D560195E5 3B478584FDAE231A"),
                alg.ComputeHash(System.Text.Encoding.ASCII.GetBytes("a"))
            );
            Assert.AreEqual(
                FromHex("4E2448A4C6F486BB 16B6562C73B4020B F3043E3A731BCE72 1AE1B303D97E6D4C 7181EEBDB6C57E27 7D0E34957114CBD6 C797FC9D95D8B582 D225292076D4EEF5"),
                alg.ComputeHash(System.Text.Encoding.ASCII.GetBytes("abc"))
            );
            Assert.AreEqual(
                FromHex("378C84A4126E2DC6 E56DCC7458377AAC 838D00032230F53C E1F5700C0FFB4D3B 8421557659EF55C1 06B4B52AC5A4AAA6 92ED920052838F33 62E86DBD37A8903E"),
                alg.ComputeHash(System.Text.Encoding.ASCII.GetBytes("message digest"))
            );
            Assert.AreEqual(
                FromHex("F1D754662636FFE9 2C82EBB9212A484A 8D38631EAD4238F5 442EE13B8054E41B 08BF2A9251C30B6A 0B8AAE86177AB4A6 F68F673E7207865D 5D9819A3DBA4EB3B"),
                alg.ComputeHash(System.Text.Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz"))
            );
            Assert.AreEqual(
                FromHex("DC37E008CF9EE69B F11F00ED9ABA2690 1DD7C28CDEC066CC 6AF42E40F82F3A1E 08EBA26629129D8F B7CB57211B9281A6 5517CC879D7B9621 42C65F5A7AF01467"),
                alg.ComputeHash(System.Text.Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"))
            );
            Assert.AreEqual(
                FromHex("466EF18BABB0154D 25B9D38A6414F5C0 8784372BCCB204D6 549C4AFADB601429 4D5BD8DF2A6C44E5 38CD047B2681A51A 2C60481E88C5A20B 2C2A80CF3A9A083B"),
                alg.ComputeHash(System.Text.Encoding.ASCII.GetBytes("12345678901234567890123456789012345678901234567890123456789012345678901234567890"))
            );
            Assert.AreEqual(
                FromHex("2A987EA40F917061 F5D6F0A0E4644F48 8A7A5A52DEEE6562 07C562F988E95C69 16BDC8031BC5BE1B 7B947639FE050B56 939BAAA0ADFF9AE6 745B7B181C3BE3FD"),
                alg.ComputeHash(System.Text.Encoding.ASCII.GetBytes("abcdbcdecdefdefgefghfghighijhijk"))
            );
            Assert.AreEqual(
                FromHex("0C99005BEB57EFF5 0A7CF005560DDF5D 29057FD86B20BFD6 2DECA0F1CCEA4AF5 1FC15490EDDC47AF 32BB2B66C34FF9AD 8C6008AD677F7712 6953B226E4ED8B01"),
                alg.ComputeHash(System.Text.Encoding.ASCII.GetBytes(new String('a', 1000000)))
            );
        }

        [Test, Explicit]
        public void SpeedTest()
        {
            byte[] speedInput = System.Text.Encoding.ASCII.GetBytes("12345678901234567890123456789012345678901234567890123456789012345678901234567890");

            Stopwatch sw;

            HashAlgorithm sha512 = SHA512.Create();
            WhirlpoolManaged alg = new WhirlpoolManaged();

            for (int rep = 3; rep > 0; rep--)
            {
                sw = new Stopwatch();
                sw.Start();
                for (int i = 0; i < 100000; i++)
                    sha512.ComputeHash(speedInput);
                sw.Stop();
                Console.WriteLine("100000 SHA512 in     {0}", sw.Elapsed);

                sw = new Stopwatch();
                sw.Start();
                for (int i = 0; i < 100000; i++)
                    alg.ComputeHash(speedInput);
                sw.Stop();
                Console.WriteLine("100000 WhirlpoolT in {0}", sw.Elapsed);
            }
        }

    }
}
