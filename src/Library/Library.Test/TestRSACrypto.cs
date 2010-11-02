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
using System.Text;
using NUnit.Framework;
using CSharpTest.Net.Crypto;
using System.Security;
using System.Runtime.InteropServices;
using CSharpTest.Net.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    public class TestRSAPrivateKeyFromCert : TestEncryption
    { public TestRSAPrivateKeyFromCert() : base(new RSAPrivateKey(TestRSACrypto.TestCertPrivateKey())) { } }

    [TestFixture]
    public class TestRSACrypto : TestEncryption
    {
        public TestRSACrypto() : base(new RSAPrivateKey()) { } 

        #region TestCertPrivateKey() / TestCertPublicKey()
        public static X509Certificate2 TestCertPrivateKey()
        {
            byte[] rawdata = Resources.roktesting_pfx;
            return new X509Certificate2(rawdata, "password");
        }

        public static X509Certificate2 TestCertPublicKey()
        {
            byte[] rawdata = Resources.roktesting_cer;
            return new X509Certificate2(rawdata);
        }
        #endregion

        [Test]
        public void TestCertificates()
        {
            X509Certificate2 privateKey = TestCertPrivateKey();
            Assert.IsTrue(privateKey.HasPrivateKey);
            X509Certificate2 publicKey = TestCertPublicKey();
            Assert.IsFalse(publicKey.HasPrivateKey);
        }

        [Test]
        public void TestPublicKeyExport()
        {
            RSAPublicKey pk = new RSAPrivateKey().PublicKey;
            string xml = pk.ToXml();

            RSAPublicKey copy = RSAPublicKey.FromXml(xml);
            Assert.AreEqual(xml, copy.ToXml());

            byte[] bytes = pk.ToArray();
            Assert.AreEqual(148, bytes.Length);

            copy = RSAPublicKey.FromBytes(bytes);
            Assert.AreEqual(bytes, copy.ToArray());

            copy = RSAPublicKey.FromParameters(pk.ExportParameters());
            Assert.AreEqual(bytes, copy.ToArray());
        }

        [Test]
        public void TestPrivateKeyExport()
        {
            RSAPrivateKey pk = new RSAPrivateKey();
            string xml = pk.ToXml();

            RSAPrivateKey copy = RSAPrivateKey.FromXml(xml);
            Assert.AreEqual(xml, copy.ToXml());

            byte[] bytes = pk.ToArray();
            Assert.AreEqual(596, bytes.Length);

            copy = RSAPrivateKey.FromBytes(bytes);
            Assert.AreEqual(bytes, copy.ToArray());

            copy = RSAPrivateKey.FromParameters(pk.ExportParameters());
            Assert.AreEqual(bytes, copy.ToArray());
        }

        [Test]
        public void TestPKICertificate()
        {
            byte[] rawdata = new byte[8001];
            new Random().NextBytes(rawdata);

            byte[] cypher;
            using (RSAPublicKey publicKey = new RSAPublicKey(TestCertPublicKey()))
                cypher = publicKey.Encrypt(rawdata);

            using (RSAPrivateKey privateKey = new RSAPrivateKey(TestCertPrivateKey()))
                Assert.AreEqual(rawdata, privateKey.Decrypt(cypher));
        }

        [Test]
        public void TestKeyStoreAddRemove()
        {
            string keyname = this.GetType().FullName + ".TestKeyCreateAndDelete";
            using (RSAPrivateKey key = new RSAPrivateKey())
            {
                Assert.IsFalse(key.DeleteFromStore());

                key.WriteToStore(keyname);

				CspParameters cp = new CspParameters();
				cp.KeyContainerName = keyname;
				cp.Flags = CspProviderFlags.UseExistingKey;

				using (RSAPrivateKey key2 = RSAPrivateKey.FromStore(cp))
					Assert.AreEqual(key.ToXml(), key2.ToXml());

				using (RSAPrivateKey key2 = RSAPrivateKey.FromStore(keyname))
                {
                    Assert.AreEqual(key.ToXml(), key2.ToXml());
                    Assert.IsTrue(key2.DeleteFromStore());
                    key2.Dispose();
                }
            }
        }

        [Test]
        public void TestSignAndVerifyHash()
        {
            byte[] data = new byte[100];
            new Random().NextBytes(data);

            foreach (HashAlgorithm ha in new HashAlgorithm[] { MD5.Create(), SHA1.Create(), SHA256.Create(), SHA384.Create(), SHA512.Create() })
            {
                Hash hash = Hash.FromBytes(ha.ComputeHash(data));
                Assert.AreEqual(CryptoConfig.MapNameToOID(ha.GetType().FullName), hash.AlgorithmOID);

                using (RSAPrivateKey key = new RSAPrivateKey())
                {
                    byte[] sig = key.SignHash(hash);

                    using (RSAPublicKey pub = key.PublicKey)
                    {
                        Assert.IsTrue(pub.VerifyHash(sig, Hash.FromBytes(ha.ComputeHash(data))));
                        data[0] = (byte)~data[0];
                        Assert.IsFalse(pub.VerifyHash(sig, Hash.FromBytes(ha.ComputeHash(data))));
                    }
                }
            }
        }

        [Test, ExpectedException(typeof(CryptographicException))]
        public void TestKeyNonExistingKey()
        {
            RSAPrivateKey.FromStore(Guid.NewGuid().ToString()).Dispose();
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestPublicKeyDecrypt()
        {
            using (RSAPublicKey publicKey = new RSAPublicKey(TestCertPublicKey()))
            {
                byte[] data = publicKey.Encrypt(new byte[100]);
                publicKey.Decrypt(data);
            }
        }

        [Test]
        public void TestFilesAllowedList()
        {
            RSAParameters key = new RSACryptoServiceProvider().ExportParameters(true);
            string check;
            if (true)
            {
                List<string> files = new List<string>();
                files.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.exe"));
                files.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll"));
                using (StringWriter sw = new StringWriter())
                {
                    foreach (string file in files)
                    {
                        byte[] hash = SHA1.Create().ComputeHash(File.ReadAllBytes(file));
                        sw.WriteLine("{0}:{1}", Path.GetFileName(file), Convert.ToBase64String(hash));
                    }

                    RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
                    csp.ImportParameters(key);
                    byte[] sig = csp.SignData(Encoding.UTF8.GetBytes(sw.ToString()), SHA1.Create());
                    sw.WriteLine(Convert.ToBase64String(sig));

                    check = sw.ToString();
                }
            }
            if (true)
            {
                Dictionary<string, string> files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.exe"))
                    files.Add(Path.GetFileName(file), file);
                foreach (string file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll"))
                    files.Add(Path.GetFileName(file), file);

                string fileInfo = check.Substring(0, check.TrimEnd().Length - 172);
                string sigInfo = check.Substring(fileInfo.Length);

                RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
                csp.ImportParameters(key);
                if (!csp.VerifyData(Encoding.UTF8.GetBytes(fileInfo), SHA1.Create(), Convert.FromBase64String(sigInfo)))
                    throw new InvalidDataException();

                using (StringReader rdr = new StringReader(fileInfo))
                {
                    char[] split = new char[] { ':' };
                    string line;
                    while (!String.IsNullOrEmpty(line = rdr.ReadLine()))
                    {
                        string[] l = line.Split(split, 2);
                        string filepath;
                        if (!files.TryGetValue(l[0], out filepath))
                            throw new FileNotFoundException("Missing manifest file.", l[0]);

                        byte[] hash = SHA1.Create().ComputeHash(File.ReadAllBytes(filepath));
                        if (l[1] != Convert.ToBase64String(hash))
                            throw new InvalidDataException();

                        files.Remove(l[0]);
                    }
                }

                if (files.Count > 0)
                    throw new InvalidDataException();
            }
        }
    }
}
