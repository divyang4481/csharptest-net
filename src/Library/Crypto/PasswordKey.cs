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
using System.Security;
using System.Security.Cryptography;
using System.IO;
using CSharpTest.Net.IO;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Creates an in-memory object that can be used for salted password encryption without
    /// storing the password in memory (based on Rfc2898DeriveBytes, SHA1 hash of password
    /// is stored)
    /// </summary>
    public class PasswordKey : CryptoKey
    {
        /// <summary> Adjusts the number of repetitions used when deriving password bytes </summary>
        public const int DefaultIterations = 8192;

        readonly IPasswordDerivedBytes _derivedBytes;
        private Salt _salt;
        private byte[] _iv;

        /// <summary> Creates the password from the given bytes and salt </summary>
		public PasswordKey(IPasswordDerivedBytes derivedBytes, Salt salt)
		{
			_derivedBytes = derivedBytes;
			_salt = salt;
            _iv = null;
		}

        /// <summary> Creates the password from the given bytes and salt </summary>
        public PasswordKey(bool clear, byte[] bytes, Salt salt)
			: this(new HashDerivedBytes<HMACSHA256>(clear, Check.NotEmpty(bytes), salt, DefaultIterations), salt)
        { }

        #region CTor overloads
        /// <summary> Creates the password from the given password bytes </summary>
        public PasswordKey(bool clear, byte[] bytes)
            : this(clear, bytes, new Salt())
        {
        }
        /// <summary> Creates the password from the given password </summary>
        public PasswordKey(string data)
            : this(true, Password.Encoding.GetBytes(data))
        { }
        /// <summary> Creates the password from the given password </summary>
        public PasswordKey(SecureString data)
            : this(true, SecureStringUtils.ToByteArray(data, Password.Encoding))
        { }
        #endregion

        /// <summary>
        /// Returns the derived bytes algorithm for this instance or throws ObjectDisposedException
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        protected IPasswordDerivedBytes DerivedBytes { get { return Assert(_derivedBytes); } }

        /// <summary> Removes the memory representation of this password and key </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        protected override void Dispose(bool disposing)
        {
            _derivedBytes.Salt = new byte[8];
            _derivedBytes.Reset();
			_salt = null;
            base.Dispose(disposing);
        }

        /// <summary> Returns the key generated with the current password and salt </summary>
        public AESCryptoKey CreateKey()
        {
			return CreateKey(_salt, IV);
        }

        /// <summary> Returns the key generated with the current password and the provided salt </summary>
        public AESCryptoKey CreateKey(Salt salt)
        {
			DerivedBytes.Salt = salt.ToArray();
			DerivedBytes.Reset();
			byte[] key = DerivedBytes.GetBytes(32);
			return new AESCryptoKey(key, IV);
        }

        /// <summary> Returns the key generated with the current password and the provided salt </summary>
        public AESCryptoKey CreateKey(Salt salt, byte[] iv)
        {
			DerivedBytes.Salt = salt.ToArray();
			DerivedBytes.Reset();
			byte[] key = DerivedBytes.GetBytes(32);
			return new AESCryptoKey(key, iv);
        }

        /// <summary> Sets or Gets the IV used when deriving the encryption key </summary>
        public virtual byte[] IV
        {
            get { if (_iv != null) return (byte[])_iv.Clone(); return AESCryptoKey.ProcessDefaultIV; }
            set { _iv = Check.ArraySize(value, 16, 16); }
        }

        /// <summary> Sets or Gets the salt used with deriving the encryption key </summary>
        public virtual Salt Salt
        {
            get { return Assert(_salt); }
			set { _salt = Check.NotNull(value); }
        }

        /// <summary> Sets or Gets the iterations used with deriving the encryption key </summary>
        public virtual int IterationCount
        {
            get { return DerivedBytes.IterationCount; }
            set { DerivedBytes.IterationCount = Check.InRange(value, 1, int.MaxValue); }
        }

        /// <summary> Encrypts the stream with the current password and salt </summary>
        public override Stream Encrypt(Stream stream)
        {
            try
            {
                Salt salt = this.Salt;
                stream.Write(salt.ToArray(), 0, salt.Length);

                AESCryptoKey key = CreateKey();
                return new DisposingStream(key.Encrypt(stream))
                    .WithDisposeOf(key);
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }
        
        /// <summary> Decrypts the stream with the current password and salt </summary>
        public override Stream Decrypt(Stream stream)
        {
            try { return Decrypt(stream, this.Salt.BitSize); }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }

        /// <summary> Decrypts the stream with the current password and salt </summary>
        public Stream Decrypt(Stream stream, Salt.Size szSaltSize)
        {
            try
            {
                Salt salt = new Salt(IOStream.Read(stream, (int)szSaltSize / 8), false);

                AESCryptoKey key = CreateKey(salt);
                return new DisposingStream(key.Decrypt(stream))
                    .WithDisposeOf(key);
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }

        /// <summary> Encrypts the bytes with the current password and salt </summary>
        public override byte[] Encrypt(byte[] blob)
        {
            try
            {
                using (AESCryptoKey key = CreateKey())
                    return new SaltedData(Salt, key.Encrypt(blob)).ToArray();
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }

        /// <summary> Decrypts the bytes with the current password and salt </summary>
        public override byte[] Decrypt(byte[] blob)
        {
            try { return Decrypt(blob, this.Salt.BitSize); }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }

        /// <summary> Decrypts the bytes with the current password and salt </summary>
        public byte[] Decrypt(byte[] blob, Salt.Size szSaltSize)
        {
            try
            {
                using (SaltedData data = new SaltedData(blob, szSaltSize))
                using (AESCryptoKey key = CreateKey(data.Salt))
                    return key.Decrypt(data.GetDataBytes());
            }
            catch (InvalidOperationException) { throw; }
            catch { throw CryptographicException(); }
        }
    }
}
