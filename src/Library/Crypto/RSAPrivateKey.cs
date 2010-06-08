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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Provides a wrapper around encrypting/decrypting with public/private key pairs from Certificats or CSP
    /// </summary>
    public class RSAPrivateKey : RSAPublicKey
    {
        /// <summary> The default key size in bits to use when constructing a new keypair </summary>
        public const int DefaultKeySize = 1024;
        /// <summary> The minimum allowed value for an RSA key </summary>
        public const int MinKeySize = 384;
        /// <summary> The maximum allowed value for an RSA key </summary>
        public const int MaxKeySize = 16384;

        /// <summary> Creates the key from the information provided </summary>
        public new static RSAPrivateKey FromParameters(RSAParameters parameters)
        { return (RSAPrivateKey)RSAPublicKey.FromParameters(parameters); }
        /// <summary> Creates the key from the information provided </summary>
        public new static RSAPrivateKey FromBytes(byte[] bytes)
        { return (RSAPrivateKey)RSAPublicKey.FromBytes(bytes); }
        /// <summary> Creates the key from the information provided </summary>
        public new static RSAPrivateKey FromXml(XmlReader xrdr)
        { return (RSAPrivateKey)RSAPublicKey.FromXml(xrdr); }
        /// <summary> Creates the key from the information provided </summary>
        public new static RSAPrivateKey FromXml(string xml)
        { return RSAPrivateKey.FromXml(new XmlTextReader(new StringReader(xml))); }
        /// <summary> Creates the key from the information provided </summary>
        public new static RSAPrivateKey FromStore(string name)
        { return (RSAPrivateKey)RSAPublicKey.FromStore(name); }
        /// <summary> Creates the key from the information provided </summary>
        public new static RSAPrivateKey FromStore(CspParameters parameters)
        { return (RSAPrivateKey)RSAPublicKey.FromStore(parameters); }

        /// <summary> Create RSAPrivateKey with a new keypair of (DefaultKeySize) bit length </summary>
        public RSAPrivateKey()
            : this(DefaultKeySize) { }

        // <summary> Create RSAPrivateKey with a new keypair of (keySize) bit length </summary>
        /// <param name="keySize">the bit-size of the key to generate, 384 - 16384 in increments of 8</param>
        public RSAPrivateKey(int keySize)
            : this(new RSACryptoServiceProvider(Check.InRange(keySize & 0x0FFF8, MinKeySize, MaxKeySize)))
        { }

        /// <summary> Create RSAPrivateKey with the provided key </summary>
        public RSAPrivateKey(X509Certificate2 certificate)
            : this(CertToPrivateKey(certificate)) { }

        /// <summary> Create RSAPrivateKey with the provided key </summary>
        public RSAPrivateKey(RSAParameters keyInfo)
            : this(new RSACryptoServiceProvider())
        {
            Check.NotNull(keyInfo);
            Check.NotNull(keyInfo.D);
            Check.NotNull(keyInfo.DP);
            Check.NotNull(keyInfo.DQ);
            Check.NotNull(keyInfo.Exponent);
            Check.NotNull(keyInfo.InverseQ);
            Check.NotNull(keyInfo.Modulus);
            Check.NotNull(keyInfo.P);
            Check.NotNull(keyInfo.Q);

            RSAKey.ImportParameters(keyInfo);
        }

        /// <summary> Create RSAPrivateKey with the provided key </summary>
        public RSAPrivateKey(RSACryptoServiceProvider keyInfo)
            : base(keyInfo) 
        { }

        /// <summary> Extract private key from certificate </summary>
        private static RSACryptoServiceProvider CertToPrivateKey(X509Certificate2 cert)
        {
            Check.IsEqual(true, cert.HasPrivateKey);
            return Check.IsAssignable<RSACryptoServiceProvider>(cert.PrivateKey);
        }

        /// <summary>
        /// Returns only the public key of this public/private key pair
        /// </summary>
        public RSAPublicKey PublicKey
        {
            get { return new RSAPublicKey(RSAKey.ExportParameters(false)); }
        }

        /// <summary>
        /// Signs the provided Hash code with the private key and returns the hash signature
        /// </summary>
        public byte[] SignHash(Hash hashBytes)
        {
            return RSAKey.SignHash(hashBytes.ToArray(), hashBytes.AlgorithmOID);
        }
    }
}
