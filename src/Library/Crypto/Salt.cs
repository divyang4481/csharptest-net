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
using System.IO;
using System.Text;
using CSharpTest.Net.Bases;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Represents a random sequence of bytes used to combine with hash and encryption
    /// values to provide an extra level of security.
    /// </summary>
    public class Salt : Equatable<Salt>
    {
        /// <summary> Size of a salt-key in bits </summary>
        public enum Size : int
        {
            /// <summary> 64-bit, 8-byte salt value </summary>
            b64 = 64,
            /// <summary> 128-bit, 16-byte salt value </summary>
            b128 = 128,
            /// <summary> 256-bit, 32-byte salt value </summary>
            b256 = 256,
            /// <summary> 512-bit, 64-byte salt value </summary>
			b512 = 512,
			/// <summary> 1024-bit, 128-byte salt value </summary>
			b1024 = 1024,
        }

        /// <summary> The size of the salt if unspecified </summary>
		public const Size DefaultSize = Size.b256;

        readonly static RandomNumberGenerator _rng = new RNGCryptoServiceProvider();
        readonly byte[] _salt;
        /// <summary> Creates a new Salt of DefaultSize </summary>
        public Salt() : this(CreateBytes(DefaultSize), false) { }
        /// <summary> Creates a new Salt of the specified size</summary>
        public Salt(Size szBits) : this(CreateBytes(szBits), false) { }
        /// <summary> Creates a new Salt using the specified bytes </summary>
        internal Salt(byte[] salt, bool copy)
        {
            _salt = !copy ? salt : (byte[])salt.Clone();
        }

        /// <summary> Creates salt from the provided bytes or a hash of the bytes </summary>
        /// <param name="bytes">An array of random bytes of 8, 16, 32, or 64 bytes long </param>
        public static Salt FromBytes(byte[] bytes)
        {
            int len = Check.NotEmpty(bytes).Length;
			if (len != 8 && len != 16 && len != 32 && len != 64 && len != 128)
                bytes = SHA256.Create().ComputeHash(bytes);
            return new Salt(bytes, true);
        }
        
        /// <summary> Returns the size of the salt in bits </summary>
        public Size BitSize { get { return (Size)(_salt.Length * 8); } }

        /// <summary> returns the total length of the salt in bytes </summary>
        public int Length { get { return _salt.Length; } }

        /// <summary> Returns the salt as an array of bytes </summary>
        public byte[] ToArray() { return (byte[])_salt.Clone(); }

		/// <summary> Returns the base64 encoding of the salt </summary>
		public override string ToString()
		{
			return Convert.ToBase64String(_salt);
		}

		/// <summary>
		/// Recreates a salt value from a string
		/// </summary>
		public static Salt FromString(string input)
		{ return new Salt(Convert.FromBase64String(input), false); }

        /// <summary> Copy the salt to the specified offset in the byte array </summary>
        public void CopyTo(byte[] array, int offset)
        { _salt.CopyTo(array, offset); }

        /// <summary> Returns the salt as a stream </summary>
        public Stream ToStream()
        { return new MemoryStream(_salt, 0, _salt.Length, false, false); }

        /// <summary> Returns the salt combined with a copy of the speicified data </summary>
        public SaltedData GetData(byte[] data)
        { return new SaltedData(this, data); }

        /// <summary> Returns the salt combined with a copy of the speicified data as a stream </summary>
        public SaltedData GetData(Stream data)
        { return new SaltedData(this, data); }

        /// <summary> Creates n bytes of data usable as a salt </summary>
        public static byte[] CreateBytes(Size szBits)
        {
            byte[] salt = new byte[(int)szBits / 8];
            CreateBytes(salt);
            return salt;
        }

        /// <summary> Creates n bytes of data usable as a salt </summary>
        public static void CreateBytes(byte[] bits)
        { _rng.GetBytes(bits); }

        /// <summary> Returns true if the two Salts are using the same data </summary>
        public override bool Equals(Salt other)
        {
            return ((object)other) != null && this.Length == other.Length &&
                BinaryComparer.Equals(_salt, other._salt);
        }

        /// <summary>
        /// Returns the hash code of the current salt
        /// </summary>
        protected override int HashCode
        { get { return BinaryComparer.GetHashCode(_salt); } }
    }
}
