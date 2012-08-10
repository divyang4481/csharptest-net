#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Provides a wrapper around encrypting with public keys from Certificats or CSP
    /// </summary>
    public class RSAPublicKey : AsymmetricKey
    {
        private readonly RSACryptoServiceProvider _rsaKey;

        /// <summary> Creates the key from the information provided </summary>
        public static RSAPublicKey FromParameters(RSAParameters parameters)
        {
            if (parameters.D != null)
                return new RSAPrivateKey(parameters);
            return new RSAPublicKey(parameters);
        }
        /// <summary> Creates the key from the information provided </summary>
        public static RSAPublicKey FromBytes(byte[] bytes)
        {
            RSACryptoServiceProvider key = new RSACryptoServiceProvider();
            key.ImportCspBlob(bytes);
            if (key.PublicOnly)
                return new RSAPublicKey(key);
            return new RSAPrivateKey(key);
        }
        /// <summary> Creates the key from the information provided </summary>
        public static RSAPublicKey FromStore(string name) 
        {
            CspParameters p = new CspParameters();
            p.Flags = CspProviderFlags.NoPrompt | CspProviderFlags.UseExistingKey;
            p.KeyContainerName = name; 
            return FromStore(p); 
        }
        /// <summary> Creates the key from the information provided </summary>
        public static RSAPublicKey FromStore(CspParameters parameters)
        {
            RSACryptoServiceProvider key = new RSACryptoServiceProvider(parameters);
            if (key.PublicOnly)
                return new RSAPublicKey(key);
            return new RSAPrivateKey(key);
        }
        /// <summary> Creates the key from the information provided </summary>
        public static RSAPublicKey FromXml(string xml)
        { return RSAPublicKey.FromXml(new XmlTextReader(new StringReader(xml))); }

        /// <summary> Create RSAPublicKey with the provided key </summary>
        public RSAPublicKey(X509Certificate2 certificate)
            : this(Check.IsAssignable<RSACryptoServiceProvider>(certificate.PublicKey.Key))
        { }

        /// <summary> Create RSAPublicKey with the provided key </summary>
        public RSAPublicKey(RSAParameters keyInfo)
        {
            Check.NotNull(keyInfo);
            Check.NotNull(keyInfo.Modulus);
            Check.NotNull(keyInfo.Exponent);

            _rsaKey = new RSACryptoServiceProvider();
            _rsaKey.ImportParameters(keyInfo);
        }

        /// <summary> Create RSAPublicKey with the provided key </summary>
        public RSAPublicKey(RSACryptoServiceProvider keyInfo)
        {
            _rsaKey = keyInfo;
        }

        /// <summary> Clears the key </summary>
        protected override void Dispose(bool disposing)
        {
            _rsaKey.Clear();
            base.Dispose(disposing);
        }

        /// <summary> Returns the key to use for encryption/decryption </summary>
        protected RSACryptoServiceProvider RSAKey { get { return Assert(_rsaKey); } }

        /// <summary> 
        /// For this type of padding, block size is (key byte length - 11) 
        /// see http://msdn.microsoft.com/en-us/library/system.security.cryptography.rsacryptoserviceprovider.encrypt.aspx 
        /// </summary>
        protected override int BlockSize { get { return (_rsaKey.KeySize / 8) - 11; } }
        /// <summary> Output size </summary>
        protected override int TransformSize { get { return (_rsaKey.KeySize / 8); } }

        /// <summary> Encrypts the given bytes </summary>
        protected override byte[] EncryptBlock(byte[] blob)
        {
            return RSAKey.Encrypt(blob, false);
        }

        /// <summary> Decrypts the given bytes </summary>
        protected override byte[] DecryptBlock(byte[] blob)
        {
            Check.Assert<InvalidOperationException>(Assert(IsPrivateKey));
            return RSAKey.Decrypt(blob, false);
        }

        /// <summary> Returns True if this object is also an RSAPrivateKey </summary>
        public bool IsPrivateKey { get { return this is RSAPrivateKey; } }

        /// <summary> Returns the public/private key information </summary>
        public RSAParameters ExportParameters()
        {
            return RSAKey.ExportParameters(IsPrivateKey);
        }

        /// <summary> Creates the key from the information provided </summary>
        public static RSAPublicKey FromXml(XmlReader xrdr)
        {
            RSAParameters param = new RSAParameters();
            while (xrdr.Read())
            {
                if (xrdr.NodeType == XmlNodeType.Element)
                {
                    if (xrdr.LocalName == "Modulus") param.Modulus = Convert.FromBase64String(xrdr.ReadElementString());
                    if (xrdr.LocalName == "Exponent") param.Exponent = Convert.FromBase64String(xrdr.ReadElementString());
                }
            }
            Check.Assert<FormatException>(param.Modulus != null && param.Exponent != null);
            return FromParameters(param);
        }
        /// <summary> Returns the key information </summary>
        public virtual void ToXml(XmlWriter xml)
        {
            RSAParameters param = ExportParameters();
            xml.WriteStartElement("RSAKeyValue");
            xml.WriteElementString("Modulus", Convert.ToBase64String(param.Modulus));
            xml.WriteElementString("Exponent", Convert.ToBase64String(param.Exponent));
            xml.WriteEndElement();
        }

        /// <summary> Returns the key information </summary>
        public String ToXml()
        {
            using (StringWriter sw = new StringWriter())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.ConformanceLevel = ConformanceLevel.Document;
                settings.Encoding = System.Text.Encoding.ASCII;
                settings.CloseOutput = false;
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;
                using (XmlWriter xwtr = XmlWriter.Create(sw, settings))
                    ToXml(xwtr);
                return sw.ToString();
            }
        }

        /// <summary> Returns a CspBlob standard binary key definition </summary>
        public byte[] ToArray() { return RSAKey.ExportCspBlob(IsPrivateKey); }

        /// <summary>
        /// Writes a copy of this key into the local Csp store for the current user
        /// </summary>
        public void WriteToStore(string name) { WriteToStore(name, CspProviderFlags.NoPrompt); }

        /// <summary>
        /// Writes a copy of this key into the local Csp store with the given options
        /// </summary>
        public void WriteToStore(string name, CspProviderFlags flags)
        {
            CspParameters cp = new CspParameters();
            cp.KeyContainerName = name;
            cp.Flags = flags;
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(cp);
            csp.ImportCspBlob(RSAKey.ExportCspBlob(IsPrivateKey));
            csp.PersistKeyInCsp = true;
            csp.Clear();
        }

        /// <summary>
        /// Removes the key from the Csp store if it was fetch with RSAPublicKey.FromStore(...)
        /// </summary>
        public bool DeleteFromStore()
        {
            bool removing = RSAKey.PersistKeyInCsp;
            RSAKey.PersistKeyInCsp = false;
            return removing;
        }

        /// <summary>
        /// Signs the provided Hash code with the private key and returns the hash signature
        /// </summary>
        public bool VerifyHash(byte[] signature, Hash hashBytes)
        {
            return RSAKey.VerifyHash(hashBytes.ToArray(), hashBytes.AlgorithmOID, signature);
        }
    }
}
