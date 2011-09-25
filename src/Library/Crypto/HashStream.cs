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
using System.Security.Cryptography;
using CSharpTest.Net.IO;
using System.IO;

namespace CSharpTest.Net.Crypto
{
    /// <summary> Represents a writtable stream for computing the hash value without retaining the data </summary>
    public sealed class HashStream : AggregateStream
    {
        private static readonly byte[] EmptyBytes = new byte[0];

        private readonly HashAlgorithm _algo;
        private CryptoStream _hashStream;
        private bool _closed;

        /// <summary> Represents a writtable stream for computing the hash value without retaining the data </summary>
        public HashStream(HashAlgorithm algo) 
            : this(algo, Stream.Null)
        { }

        /// <summary> Wraps an existing stream while computing a hash on all bytes read from/written to the stream</summary>
        public HashStream(HashAlgorithm algo, Stream underlyingStream)
            : base(underlyingStream)
        {
            _algo = algo;
            _hashStream = new CryptoStream(Stream.Null, _algo, CryptoStreamMode.Write);
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite { get { return true; } }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current 
        /// position within this stream by the number of bytes written.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _hashStream.Write(buffer, offset, count);
            base.Write(buffer, offset, count);
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current 
        /// position within this stream by the number of bytes written.
        /// </summary>
        public override void WriteByte(byte value)
        {
            _hashStream.WriteByte(value);
            base.WriteByte(value);
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position 
        /// within the stream by the number of bytes read.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int amount = base.Read(buffer, offset, count);
            _hashStream.Write(buffer, offset, amount);
            return amount;
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        public override int ReadByte()
        {
            int value = base.ReadByte();
            if(value >= 0)
                _hashStream.WriteByte((byte)value);
            return value;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream"/> and optionally releases the managed resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _closed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Can be called once, and only once, to obtain the hash generated while reading/writing.  After this is
        /// called the stream will reset the hash and start computing a new hash value.
        /// </summary>
        public Hash FinalizeHash()
        {
            try
            {
                _hashStream.FlushFinalBlock();
                return Hash.FromBytes(_algo.Hash);
            }
            finally
            {
                _algo.Initialize();
                _hashStream = new CryptoStream(Stream.Null, _algo, CryptoStreamMode.Write);
            }
        }

        /// <summary> Represents a writtable stream for computing the hash value without retaining the data </summary>
        /// <returns> The hash code computed by the series of Write(...) calls </returns>
        public new Hash Close()
        {
            if (_closed)
                throw new ObjectDisposedException(GetType().FullName);
            try { return FinalizeHash(); }
            finally
            {
                _closed = true;
                base.Close();
            }
        }

        /// <summary>
        /// Change the underlying stream that is being written to / read from without affecting the current hash
        /// </summary>
        public void ChangeStream(Stream stream)
        {
            base.Stream = stream ?? Stream.Null;
        }
    }
}
