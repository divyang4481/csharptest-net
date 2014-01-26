#region Copyright 2011-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Text;
using CSharpTest.Net.IO;
using NUnit.Framework;

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestCrc64
    {
        private static void AssertCrc64(string input, ulong expected)
        {
            //Can not use ASCII encoding since it does not support 8-bit characters: resumé
            byte[] bytes = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
                bytes[i] = unchecked((byte)input[i]);
            AssertCrc64(bytes, expected);
        }
        private static void AssertCrc64(byte[] input, ulong expectedu)
        {
            long expected = unchecked((long) expectedu);
            Crc64 crc = new Crc64(input);
            Assert.AreEqual(expected, crc.Value);

            crc = new Crc64(0);
            crc.Add(input);
            Assert.AreEqual(expected, crc.Value);

            crc = new Crc64();
            crc.Add(input, 0, input.Length);
            Assert.AreEqual(expected, crc.Value);

            crc = new Crc64();
            foreach (byte b in input)
                crc.Add(b);
            Assert.AreEqual(expected, crc.Value);
        }

        [Test]
        public void TestKnown123456789()
        { AssertCrc64("123456789", 0x995dc9bbdf1939faul); }

        [Test]
        public void TestKnownResume1()
        { AssertCrc64("resume", 0xd68a044c92bb8a32ul); }

        [Test]
        public void TestKnownResume2()
        {
            //the string is "resumé" with the accented 'é'; however, I did not want to depend on proper text encoding.
            AssertCrc64("resum" + (char)233, 0x9397175445ad6725ul); 
        }

        [Test]
        public void TestKnownBytesLeadingZero()
        { AssertCrc64(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x28, 0x86, 0x4d, 0x7f, 0x99 }, 0x578b800a3b157c37ul); }

        [Test]
        public void TestKnownBytesLeading0Xff()
        { AssertCrc64(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x28, 0xc5, 0x5e, 0x45, 0x7a }, 0xaafb709ed67a8193ul); }

        [Test]
        public void TestKnownBytesSequence()
        { AssertCrc64(Encoding.UTF8.GetBytes("TEST"), 0xe10c672b18cfcdd0ul); }

        [Test]
        public void TestOperatorEquality()
        {
            Crc64 empty = new Crc64();
            Crc64 value = new Crc64("Hello");
            Crc64 copy = new Crc64(value.Value);

            Assert.IsTrue(value == copy);
            Assert.IsFalse(value == empty);
            Assert.IsTrue(value == copy.Value);
            Assert.IsFalse(value == empty.Value);

            Assert.IsFalse(value != copy);
            Assert.IsTrue(value != empty);
            Assert.IsFalse(value != copy.Value);
            Assert.IsTrue(value != empty.Value);
        }
        [Test]
        public void TestCrc64Equals()
        {
            Crc64 empty = new Crc64();
            Crc64 value = new Crc64("Hello");
            Crc64 copy = new Crc64(value.Value);

            Assert.IsTrue(value.Equals(copy));
            Assert.IsFalse(value.Equals(empty));
            Assert.IsTrue(value.Equals(copy.Value));
            Assert.IsFalse(value.Equals(empty.Value));

            Assert.IsTrue(value.Equals((object)copy));
            Assert.IsFalse(value.Equals((object)empty));
            Assert.IsTrue(value.Equals((object)copy.Value));
            Assert.IsFalse(value.Equals((object)empty.Value));
        }

        [Test]
        public void TestHashValue()
        {
            Crc64 crc = new Crc64();
            Assert.AreEqual(0, crc.Value);
            Assert.AreEqual(0, crc.GetHashCode());

            crc.Add(0x1b);

            Assert.AreNotEqual(0, crc.Value);
            Assert.AreEqual((int)crc.Value ^ (int)(crc.Value >> 32), crc.GetHashCode());
        }

        [Test]
        public void TestOperatorPlusBytes()
        {
            Crc64 all = new Crc64(new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8 });
            Crc64 crc = new Crc64();
            Assert.AreEqual(0, crc.Value);
            crc += new byte[] { 0x1, 0x2, 0x3, 0x4 };
            crc += 0x5;
            crc += 0x6;
            crc += 0x7;
            crc += 0x8;
            Assert.AreEqual(all.Value, crc.Value);
        }

        [Test]
        public void TestOperatorPlusString()
        {
            Crc64 all = new Crc64("hello there world");
            Crc64 crc = new Crc64();
            Assert.AreEqual(0, crc.Value);
            crc += "hello ";
            crc += "there ";
            crc += "world";
            Assert.AreEqual(all.Value, crc.Value);
        }

        [Test]
        public void TestAddByteRange()
        {
            Crc64 all = new Crc64(new byte[] { 0x2, 0x3, 0x4, 0x5, 0x6 });
            Crc64 crc = new Crc64();
            Assert.AreEqual(0, crc.Value);
            crc.Add(new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8 }, 1, 5);
            Assert.AreEqual(all.Value, crc.Value);
        }

        [Test]
        public void TestToString()
        {
            Assert.AreEqual("0000000000000000", new Crc64().ToString());
            Assert.AreEqual("0000000000100100", new Crc64(0x00100100).ToString());
            Assert.AreEqual("FFFFFFFFF0100100", new Crc64(unchecked((int)0xF0100100)).ToString());
            Assert.AreEqual("F010010011100110", new Crc64(unchecked((long)0xF010010011100110ul)).ToString());
        }
    }
}
