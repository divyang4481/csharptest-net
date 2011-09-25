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
        private static byte[] _iv;
        /// <summary> Creates a default IV for the crypto provider if AESCryptoKey.CryptoIV is not set </summary>
        private static byte[] DefaultIV()
        {
            System.Diagnostics.Trace.TraceWarning("The default IV has been deprecated, please set the AESCryptoKey.ProcessDefaultIV value.");

            Assembly asmKey = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
            AssemblyName asmName = asmKey.GetName();
            byte[] pk = Encoding.UTF8.GetBytes(asmName.Name);
            return Hash.MD5(pk).ToArray();
        }

        /// <summary>
        /// Used to define the IV for AES keys created in this process, by default this is MD5(UTF8(Name)) where
        /// Name is the short-name of either the entry-point assembly, or "CSharpTest.Net.Library" if undefined.
        /// </summary>
        /// <remarks>
        /// The process default IV is used with AESCryptoKey instances that are created without explicitly
        /// providing the IV value for the key.  This is done internally when using the Password class' 
        /// CreateKey(...), Encrypt(...), or Decrypt(...) methods.  While this worked well enough for some
        /// programs, this has proven to be a flawed approach as the entry-point assembly can change.  For example
        /// if another .NET process call Assembly.Execute() on your executable.  
        /// 
        /// Applications are advised that they should capture the existing value and store that in App.Config, 
        /// and set the following prior to using this class, or the Password class.  The entry-points related
        /// to this that have been marked Obsolete() will be removed in the long-term and by capturing this
        /// value and manually using it you can ensure your application will continue to function properly.
        /// </remarks>
        public static byte[] ProcessDefaultIV
        {
            get { return (byte[])(_iv ?? (_iv = DefaultIV())).Clone(); }
            set { _iv = Check.ArraySize(value, 16, 16); }
        }

        readonly SymmetricAlgorithm _key;

        /// <summary> Creates a new key </summary>
        public AESCryptoKey()
        {
            _key = new RijndaelManaged();
            _key.Padding = PaddingMode.PKCS7;
            _key.KeySize = 256;
            _key.IV = _iv ?? ProcessDefaultIV;
            _key.GenerateKey();
        }

        /// <summary> Creates an object representing the specified key </summary>
        [Obsolete("Please use the overload that accepts IV bytes.")]
        public AESCryptoKey(byte[] key)
        {
            _key = new RijndaelManaged();
            _key.Padding = PaddingMode.PKCS7;
            _key.KeySize = 256;
            _key.IV = _iv ?? ProcessDefaultIV;
            _key.Key = Check.ArraySize(key, 32, 32);
        }

        /// <summary> Creates an object representing the specified key and init vector </summary>
        public AESCryptoKey(byte[] key, byte[] iv)
        {
            _key = new RijndaelManaged();
            _key.Padding = PaddingMode.PKCS7;
            _key.KeySize = 256;
            _key.Key = Check.ArraySize(key, 32, 32);
            _key.IV = Check.ArraySize(iv, 16, 16);
        }

        /// <summary>
        /// Serializes the KEY and IV to a single array of bytes.  Use FromByteArray() to restore.
        /// </summary>
        public static AESCryptoKey FromBytes(byte[] serializedBytes)
        {
            Check.ArraySize(serializedBytes, 48, 48);
            byte[] key = new byte[32], iv = new byte[16];
            Buffer.BlockCopy(serializedBytes, 0, key, 0, 32);
            Buffer.BlockCopy(serializedBytes, 32, iv, 0, 16);
            return new AESCryptoKey(key, iv);
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

        /// <summary>
        /// Serializes the KEY and IV to a single array of bytes.  Use FromByteArray() to restore.
        /// </summary>
        public byte[] ToArray()
        {
            byte[] result = new byte[32 + 16];
            Buffer.BlockCopy(Key, 0, result, 0, 32);
            Buffer.BlockCopy(IV, 0, result, 32, 16);
            return result;
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