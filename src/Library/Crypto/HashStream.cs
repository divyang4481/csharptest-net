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
using System.Security.Cryptography;
using CSharpTest.Net.IO;

namespace CSharpTest.Net.Crypto
{
    /// <summary> Represents a writtable stream for computing the hash value without retaining the data </summary>
    public sealed class HashStream : BaseStream
    {
        private readonly HashAlgorithm _algo;
        private bool _closed;
        private static readonly byte[] EmptyBytes = new byte[0];

        /// <summary> Represents a writtable stream for computing the hash value without retaining the data </summary>
        public HashStream(HashAlgorithm algo)
        {
            _algo = algo;
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite { get { return true; } }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _algo.TransformBlock(buffer, 0, buffer.Length, buffer, 0);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream"/> and optionally releases the managed resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _closed = true;
            base.Dispose(disposing);
        }

        /// <summary> Represents a writtable stream for computing the hash value without retaining the data </summary>
        /// <returns> The hash code computed by the series of Write(...) calls </returns>
        public new Hash Close()
        {
            if (_closed)
                throw new ObjectDisposedException(GetType().FullName);
            try
            {
                _algo.TransformFinalBlock(EmptyBytes, 0, 0);
                return Hash.FromBytes(_algo.Hash);
            }
            finally 
            {
                _closed = true;
                _algo.Initialize();
                base.Close();
            }
        }
    }
}
