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

namespace CSharpTest.Net.Crypto
{
	/// <summary>
	/// Provided an implementation very similiar to that of the Rfc2898DeriveBytes with the following
	/// excpetions: 1) any hash size can be used, 2) original key bytes are always hashed, 3) bytes
	/// generated are always rounded to hash size, thus GetBytes(4) + GetBytes(4) != GetBytes(8)
	/// </summary>
	public class HashDerivedBytes<THash> : DeriveBytes, IPasswordDerivedBytes
		where THash : HMAC, new()
	{
		// Fields
        private readonly HMAC _hashAlgo;
		private uint _iterations;
		private byte[] _salt;
		private int _block;

        #region Create() - Workaround for unpatched .Net 2.0
        static HMAC Create()
        {
            if (typeof(THash) == typeof(global::System.Security.Cryptography.HMACSHA512))
                return new HMAC<SHA512Managed>();
            if (typeof(THash) == typeof(global::System.Security.Cryptography.HMACSHA384))
                return new HMAC<SHA384Managed>();
            return new THash();
        }

        class HMAC<T> : HMAC where T : HashAlgorithm, new()
        {
            public HMAC()
            {
                HashAlgorithm h = new T();
                
                HashName = typeof(T).Name.Replace("Managed", "");
                HashSizeValue = h.HashSize;
                BlockSizeValue = 128;
                base.Key = Crypto.Salt.CreateBytes(Crypto.Salt.Size.b128);
            }
        }
        #endregion

        private HashDerivedBytes(HMAC algo, Stream password, Salt salt, int iterations)
        {
			_block = 1;
			_hashAlgo = algo;
			Check.Assert<ArgumentException>(_hashAlgo.CanReuseTransform);
			// delta from RFC2898 - key with a hash of password, it would happen anyway if password.Length > 32
			HashAlgorithm ha = (HashAlgorithm)CryptoConfig.CreateFromName(_hashAlgo.HashName);
			_hashAlgo.Key = ha.ComputeHash(password);
			_salt = salt.ToArray();
			_iterations = (uint)Check.InRange(iterations, 1, int.MaxValue);
		}

        /// <summary>
		/// Constructs the byte generation routine with the specified key, salt, and iteration count
		/// </summary>
		public HashDerivedBytes(THash algo, Stream password, Salt salt, int iterations)
            : this((HMAC)algo, password, salt, iterations)
		{ }
		
		/// <summary>
		/// Constructs the byte generation routine with the specified key, salt, and iteration count
		/// </summary>
		public HashDerivedBytes(Stream password, Salt salt, int iterations)
			: this(Create(), password, salt, iterations)
		{ }

		/// <summary>
		/// Constructs the byte generation routine with the specified key, salt, and iteration count
		/// </summary>
		public HashDerivedBytes(bool clear, byte[] password, Salt salt, int iterations)
            : this(Create(), new MemoryStream(Check.NotNull(password), 0, password.Length, false, false), salt, iterations)
		{ 
            if (clear) Array.Clear(password, 0, password.Length);
		}

		/// <summary>
		/// Constructs the byte generation routine with the specified key, salt, and iteration count
		/// </summary>
		public HashDerivedBytes(byte[] password, Salt salt, int iterations)
			: this(false, password, salt, iterations)
		{ }

		private byte[] ComputeBlock(int block, int size)
		{
			byte[] inputBuffer = BitConverter.GetBytes(block);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(inputBuffer, 0, 4);

			_hashAlgo.TransformBlock(_salt, 0, _salt.Length, _salt, 0);
			_hashAlgo.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
			byte[] hash = _hashAlgo.Hash;
			_hashAlgo.Initialize();

			byte[] result = hash;
			for (int i = 2; i <= _iterations; i++)
			{
				hash = _hashAlgo.ComputeHash(hash);
				for (int j = 0; j < size; j++)
				{
					result[j] = (byte)(result[j] ^ hash[j]);
				}
			}
			return result;
		}

		///<summary>
		///     Returns a pseudo-random key from a password, salt and iteration count.
		///</summary>
		public override byte[] GetBytes(int cb)
		{
			Check.Assert<ArgumentOutOfRangeException>(cb > 0);

			int blockSize = Check.InRange(_hashAlgo.HashSize / 8, 8, 64);
			if (cb == blockSize)
				return ComputeBlock(_block++, cb);

			byte[] buffer = new byte[cb];
			for (int offset = 0; offset < cb; offset += blockSize)
			{
				int size = Math.Min(blockSize, cb - offset);
				Array.Copy(ComputeBlock(_block++, blockSize), 0, buffer, offset, size);
			}
			return buffer;
		}

		/// <summary>
		/// Disposes of the object
		/// </summary>
		public void Dispose()
		{
			_block = -1;
			_salt = new byte[8];
			_iterations = 1;
			_hashAlgo.Clear();
		}

		///<summary>
		///     Resets the state of the operation.
		///</summary>
		public override void Reset()
		{
			_block = 1;
		}

		///<summary>
		///     Gets or sets the number of iterations for the operation.
		///</summary>
		public int IterationCount
		{
			get
			{
				return (int)_iterations;
			}
			set
			{
				Check.Assert<ArgumentOutOfRangeException>(value > 0);
				_iterations = (uint)value;
				Reset();
			}
		}

		///<summary>
		///     Gets or sets the key salt value for the operation.
		///</summary>
		public byte[] Salt
		{
			get
			{
				return (byte[])_salt.Clone();
			}
			set
			{
				Check.ArraySize(value, 8, int.MaxValue);
				_salt = (byte[])value.Clone();
				Reset();
			}
		}
	}
}
