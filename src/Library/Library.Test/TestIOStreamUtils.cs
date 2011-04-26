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
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using CSharpTest.Net.IO;
using System.Security.Cryptography;
using CSharpTest.Net.Crypto;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public partial class TestIOStreamUtils
    {
        #region TestFixture SetUp/TearDown
        [TestFixtureSetUp]
        public virtual void Setup()
        {
        }

        [TestFixtureTearDown]
        public virtual void Teardown()
        {
        }
        #endregion

        Stream FragmentStream(byte[] bytes, int segmentsize)
        {
            List<Stream> all = new List<Stream>();
            for( int pos = 0; pos < bytes.Length; pos += segmentsize )
                all.Add(new MemoryStream(bytes, pos, Math.Min(bytes.Length - pos, segmentsize), false));

            return new CombinedStream(all);
        }

        [Test]
        public void TestReadAll()
        {
            Assert.AreEqual("Hello!", IOStream.ReadAllText(new MemoryStream(Encoding.ASCII.GetBytes("Hello!")), Encoding.ASCII));
            Assert.AreEqual("Hello!", IOStream.ReadAllText(new MemoryStream(Encoding.UTF7.GetBytes("Hello!")), Encoding.UTF7));
            Assert.AreEqual("Hello!", IOStream.ReadAllText(new MemoryStream(Encoding.UTF32.GetBytes("Hello!")), Encoding.UTF32));

            byte[] testdata = new byte[2048];
            new Random().NextBytes(testdata);
            string testString = Convert.ToBase64String(testdata);

            Assert.AreEqual(testString, Convert.ToBase64String(IOStream.ReadAllBytes(new MemoryStream(testdata))));

            //that all works, now let's fragment the stream and ensure correct behavior as well
            Stream fragments = FragmentStream(testdata, 16);
            Assert.AreEqual(testString, Convert.ToBase64String(IOStream.ReadAllBytes(fragments)));

            //text version:
            fragments = FragmentStream(Encoding.ASCII.GetBytes(testString), 16);
            Assert.AreEqual(testString, IOStream.ReadAllText(fragments, Encoding.ASCII));
        }

        [Test]
        public void TestReadNBytes()
        {
            byte[] testdata = new byte[2048];
            new Random().NextBytes(testdata);

            byte[] copy = new byte[testdata.Length];
            IOStream.Read(FragmentStream(testdata, 16), copy);
            Assert.AreEqual(Convert.ToBase64String(testdata), Convert.ToBase64String(copy));

            byte[] copyb = new byte[testdata.Length / 2];
            using (Stream s = FragmentStream(testdata, 16))
            {
                IOStream.Read(s, copy, copy.Length - copyb.Length);
                IOStream.Read(s, copyb);
            }
            Array.Copy(copyb, 0, copy, copy.Length - copyb.Length, copyb.Length);
            Assert.AreEqual(Convert.ToBase64String(testdata), Convert.ToBase64String(copy));

            copy = IOStream.Read(FragmentStream(testdata, 16), testdata.Length);
            Assert.AreEqual(Convert.ToBase64String(testdata), Convert.ToBase64String(copy));
        }

        [Test, ExpectedException(typeof(IOException))]
        public void TestExceptionIfReadPastEnd()
        {
            Stream m = new MemoryStream(new byte[5]);
            byte[] test = new byte[10];
            IOStream.Read(m, test, 10);//read more bytes than available generates exception
        }

        [Test]
        public void TestCompressFile()
        {
            byte[] testData = new byte[ushort.MaxValue];
            new Random().NextBytes(testData);
            Array.Clear(testData, 0, testData.Length / 2);
            Hash compressedHash;
            Hash hash = Hash.SHA512(testData);

            using (TempFile f = new TempFile())
            {
                f.WriteAllBytes(testData);
                Assert.AreEqual((long)testData.Length, f.Length);

                IOStream.Compress(f.TempPath);
                Assert.IsTrue(f.Length < testData.Length);
                compressedHash = Hash.SHA512(f.Read());
                Assert.IsFalse(compressedHash == hash);

                IOStream.Decompress(f.TempPath);
                Assert.AreEqual((long)testData.Length, f.Length);
                Assert.IsTrue(hash == Hash.SHA512(f.Read()));
            }

            MemoryStream ms = new MemoryStream(testData), gz = new MemoryStream();
            IOStream.Compress(ms, gz);

            Assert.IsTrue(compressedHash == Hash.SHA512(gz.ToArray()));
            ms = new MemoryStream();
            gz.Position = 0;
            IOStream.Decompress(gz, ms);

            Assert.IsTrue(hash == Hash.SHA512(ms.ToArray()));
        }
    }
}
