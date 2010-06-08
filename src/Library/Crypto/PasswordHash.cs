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
using System.IO;
using CSharpTest.Net.IO;
using System.Text;
using System.Collections;
using CSharpTest.Net.Bases;
using System.Security;

namespace CSharpTest.Net.Crypto
{
    /// <summary> Creates a salted hash </summary>
    public sealed class PasswordHash : Comparable<PasswordHash>, IDisposable
    {
        readonly SaltedData _hash;
		/// <summary> Defines the derived hash iteration count used </summary>
		public const int StandardIterations = 1024;

        /// <summary> recreates a hash from a base-64 encoded string </summary>
        public static PasswordHash FromString(string hash)
        { return FromBytes(Convert.FromBase64String(hash)); }

        /// <summary> recreates a hash from the bytes returned by ToArray() </summary>
        public static PasswordHash FromBytes(byte[] hash)
        { return new PasswordHash(new SaltedData(hash)); }

        /// <summary> Recreates a hash </summary>
        private PasswordHash(SaltedData hash)
        {
            _hash = hash;
        }

        /// <summary> Creates a salted hash from the given bytes and salt </summary>
        public PasswordHash(Stream bytes, Salt salt)
        {
            using (bytes)
			using (HashDerivedBytes<HMACSHA256> hashBytes = new HashDerivedBytes<HMACSHA256>(bytes, salt, StandardIterations))
				_hash = new SaltedData(salt, hashBytes.GetBytes(32));
        }

        #region CTor overloads
        /// <summary> Creates a salted hash from the given password </summary>
        public PasswordHash(Password bytes)
            : this(bytes.ReadBytes(), new Salt())
        { }
        /// <summary> Creates a salted hash from the given password and salt </summary>
        public PasswordHash(Password bytes, Salt salt)
            : this(bytes.ReadBytes(), salt)
        { }
        /// <summary> Creates the hash from the given bytes and salt </summary>
        public PasswordHash(bool clear, byte[] bytes, Salt salt)
            : this(new MemoryStream(bytes, 0, bytes.Length, false, false), salt)
        { if (clear) Array.Clear(bytes, 0, bytes.Length); }
        /// <summary> Creates the hash from the given bytes </summary>
        public PasswordHash(bool clear, byte[] bytes)
            : this(clear, bytes, new Salt())
        { }
        /// <summary> Creates the hash from the given data and salt </summary>
        public PasswordHash(string data, Salt salt)
            : this(true, Password.Encoding.GetBytes(data), salt)
        { }
        /// <summary> Creates the hash from the given data and salt </summary>
        public PasswordHash(string data)
            : this(true, Password.Encoding.GetBytes(data), new Salt())
        { }
        /// <summary> Creates the hash from the given data and salt </summary>
        public PasswordHash(SecureString data, Salt salt)
            : this(true, SecureStringUtils.ToByteArray(data, Password.Encoding), salt)
        { }
        /// <summary> Creates the hash from the given data and salt </summary>
        public PasswordHash(SecureString data)
            : this(true, SecureStringUtils.ToByteArray(data, Password.Encoding), new Salt())
        { }
        #endregion

        /// <summary> Disposes of hash bytes </summary>
        public void Dispose()
        { _hash.Dispose(); }

        /// <summary> Gets the hash </summary>
        protected override int HashCode { get { return BinaryComparer.GetHashCode(_hash.ToArray()); } }

        /// <summary> Compares the hash </summary>
        public override int CompareTo(PasswordHash other)
        {
            if (((object)other) == null)
                return 1;
            return BinaryComparer.Compare(_hash.ToArray(), other._hash.ToArray());
        }

        /// <summary> returns the salted-hash length in bytes </summary>
        public int Length { get { return _hash.Length; } }
        /// <summary> returns the salt used to create this hash </summary>
        public Salt Salt { get { return _hash.Salt; } }
        /// <summary> returns the salted-hash bytes </summary>
        public byte[] ToArray() { return _hash.ToArray(); }

        #region bool VerifyPassword(Password password)
        /// <summary> Returns true if the provided password matches this hash </summary>
        public bool VerifyPassword(Password password)
        {
            PasswordHash hash = password.CreateHash(Salt);
            return this.Equals(hash);
        }
        /// <summary> Returns true if the provided password matches this hash </summary>
        public bool VerifyPassword(Stream password)
        {
            PasswordHash hash = new PasswordHash(password, this.Salt);
            return this.Equals(hash);
        }
        /// <summary> Returns true if the provided password matches this hash </summary>
        public bool VerifyPassword(byte[] password)
        {
            using (Stream pb = new MemoryStream(password, 0, password.Length, false, false))
                return VerifyPassword(pb);
        }
        /// <summary> Returns true if the provided password matches this hash </summary>
        public bool VerifyPassword(string password)
        {
            byte[] tmp = Password.Encoding.GetBytes(password);
            try { return VerifyPassword(tmp); }
            finally { Array.Clear(tmp, 0, tmp.Length); }
        }
        #endregion

        #region Object overrides
        /// <summary> Returns the hash as a base-64 encoded string </summary>
        public override string ToString()
        { return Convert.ToBase64String(_hash.ToArray()); }
        #endregion
    }
}
