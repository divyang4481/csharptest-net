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
using System.Text;
using System.Reflection;
using System.IO;
using CSharpTest.Net.IO;
using CSharpTest.Net.Utils;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Interfaces;
using Cryp = System.Security.Cryptography.CryptoStream;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Provides AES-256 bit encryption using a global IV (Init vector) based on the current process' entry
    /// assembly.
    /// </summary>
    public class AESCryptoKey : CryptoKey
    {
        private static readonly byte[] _iv = DefaultIV();
        /// <summary> Creates a default IV for the crypto provider if AESCryptoKey.CryptoIV is not set </summary>
        private static byte[] DefaultIV()
        {
            Assembly asmKey = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
            AssemblyName asmName = asmKey.GetName();
            byte[] pk = Encoding.UTF8.GetBytes(asmName.Name);
            return Hash.MD5(pk).ToArray();
        }

        readonly SymmetricAlgorithm _key;

        /// <summary> Creates a new key </summary>
        public AESCryptoKey()
        {
            _key = new RijndaelManaged();
            _key.Padding = PaddingMode.PKCS7;
            _key.KeySize = 256;
            _key.IV = _iv;
            _key.GenerateKey();
        }
        /// <summary> Creates an object representing the specified key </summary>
        public AESCryptoKey(byte[] key)
            : this()
        {
            _key.Key = Check.ArraySize(key, 32, 32);
        }
        /// <summary> Creates an object representing the specified key and init vector </summary>
        public AESCryptoKey(byte[] key, byte[] iv)
            : this(key)
        {
            _key.IV = Check.ArraySize(iv, 16, 16);
        }

        /// <summary> Returns the algorithm key or throws ObjectDisposedException </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        protected SymmetricAlgorithm Algorithm
        {
            get { return Assert(_key); }
        }

        /// <summary> Disposes of the key </summary>
        protected override void Dispose(bool disposing)
        {
            _key.Clear();
            base.Dispose(disposing);
        }

        /// <summary> Returns the AES 256 bit key this object was created with </summary>
        public byte[] Key { get { return Algorithm.Key; } }

        /// <summary> Returns the AES 256 bit key this object was created with </summary>
        public byte[] IV { get { return Algorithm.IV; } }

        /// <summary>Encrypts a stream of data</summary>
        public override Stream Encrypt(Stream stream)
        {
            try
            {
                ICryptoTransform xform = Algorithm.CreateEncryptor();
                return new DisposingStream(new CryptoStream(stream, xform, CryptoStreamMode.Write))
                    .WithDisposeOf(xform);
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }
        /// <summary> Decrypts a stream of data </summary>
        public override Stream Decrypt(Stream stream)
        {
            try
            {
                ICryptoTransform xform = Algorithm.CreateDecryptor();
                return new DisposingStream(new CryptoStream(stream, xform, CryptoStreamMode.Read))
                    .WithDisposeOf(xform);
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }
        /// <summary>Encrypts a raw data block as a set of bytes</summary>
        public override byte[] Encrypt(byte[] blob)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream io = Encrypt(new NonClosingStream(ms)))
                        io.Write(blob, 0, blob.Length);

                    return ms.ToArray();
                }
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }
        /// <summary>Decrypts a raw data block as a set of bytes</summary>
        public override byte[] Decrypt(byte[] blob)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream io = Decrypt(new MemoryStream(blob)))
                        IOStream.CopyStream(io, ms);

                    return ms.ToArray();
                }
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }
    }
}