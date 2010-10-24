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
using System.IO;
using System.Security.Cryptography;
using CSharpTest.Net.Formatting;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Base class for encryption/decryption classes
    /// </summary>
    [System.Diagnostics.DebuggerNonUserCode]
    public abstract class CryptoKey : IEncryptDecrypt, IDisposable
    {
        private bool _disposed;
        /// <summary> </summary>
        protected CryptoKey() { _disposed = false; }

        /// <summary> </summary>
        ~CryptoKey()
        {
            try { this.Dispose(false); }
            catch { }
            _disposed = true;
        }

        /// <summary> Throws ObjectDisposedException if the object has been disposed </summary>
        protected T Assert<T>(T t) { Check.Assert(_disposed == false, delegate() { return new ObjectDisposedException(GetType().Name); }); return t; }

        /// <summary> Clears any secure memory associated with this object </summary>
        public void Dispose()
        {
            this.Dispose(true);
            _disposed = true;
        }

        /// <summary> Clears any secure memory associated with this object </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);
        }

        /// <summary> Wraps the stream with a cryptographic stream </summary>
        public abstract Stream Encrypt(Stream stream);
        /// <summary> Wraps the stream with a cryptographic stream </summary>
        public abstract Stream Decrypt(Stream stream);

        /// <summary> Encrypts the given bytes </summary>
        public abstract byte[] Encrypt(byte[] blob);
        /// <summary> Decrypts the given bytes </summary>
        public abstract byte[] Decrypt(byte[] blob);

        /// <summary>
        /// Encrypts the encoded text and returns the base-64 encoded result
        /// </summary>
        public string Encrypt(string text) { return Encrypt(text, ByteEncoding.Base64); }
        /// <summary>
        /// Encrypts the encoded text and returns the base-64 encoded result
        /// </summary>
        public string Encrypt(string text, ByteEncoding encoding)
        {
            try { return Check.NotNull(encoding).EncodeBytes(this.Encrypt(Encoding.UTF8.GetBytes(Check.NotNull(text)))); }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }
        /// <summary>
        /// Decrypts the base-64 encoded bytes, decrypts the data and returns the string
        /// </summary>
        public string Decrypt(string text) { return Decrypt(text, ByteEncoding.Base64); }
        /// <summary>
        /// Decrypts the base-64 encoded bytes, decrypts the data and returns the string
        /// </summary>
        public string Decrypt(string text, ByteEncoding encoding)
        {
            try { return Encoding.UTF8.GetString(this.Decrypt(Check.NotNull(encoding).DecodeBytes(Check.NotNull(text)))); }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }

        /// <summary>
        /// Used to ensure generality in excpetions raised from cryptographic routines.
        /// </summary>
        /// <example>catch { throw CryptographicException(); }</example>
        protected virtual Exception CryptographicException()
        {
            return new CryptographicException();
        }
    }
}
