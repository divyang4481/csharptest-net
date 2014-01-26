#region Copyright 2013-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.IO
{
    /// <summary> Provides a simple CRC64 checksum for a set of bytes using algorithm (CRC-64/XZ) </summary>
    [System.Diagnostics.DebuggerDisplay("{Value:X8}")]
    public struct Crc64 : IEquatable<Crc64>, IEquatable<long>
    {
        static readonly ulong[] Table;
        static Crc64()
        {
            Table = new ulong[256];
            for (uint i = 0; i < 256; i++)
            {
                ulong crc = i;
                for (uint j = 0; j < 8; j++)
                    crc = (crc >> 1) ^ (((crc & 1) == 1) ? 0xC96C5795D7870F42 : 0);
                //Note: table ordinal and value inverted to omit -1 start and end operation
                Table[~i & 0x0ff] = ~crc;
            }
        }

        ulong _crc64;

        /// <summary> Resumes the computation of a CRC64 value </summary>
        public Crc64(long crc)
        { _crc64 = unchecked((ulong)crc); }

        /// <summary> Initailizes the Crc64 value to the checksum of the string as a series of 16-bit values </summary>
        public Crc64(string text)
        { _crc64 = 0; Add(text); }

        /// <summary> Initailizes the Crc64 value to the checksum of the bytes provided </summary>
        public Crc64(byte[] bytes)
        { _crc64 = 0; Add(bytes, 0, bytes.Length); }

        /// <summary> Returns the computed CRC64 value as a Hex string </summary>
        public override string ToString() { return String.Format("{0:X16}", Value); }

        /// <summary> Returns the computed CRC64 value </summary>
        public long Value { get { return unchecked((long)_crc64); } }

        /// <summary> Adds a byte to the checksum </summary>
        public void Add(byte b)
        {
            _crc64 = (((~_crc64 >> 8) & 0x00FFFFFFFFFFFFFF) ^ Table[(_crc64 ^ b) & 0x0ff]);
        }

        /// <summary> Adds a byte to the checksum </summary>
        public static Crc64 operator +(Crc64 chksum, byte b)
        {
            chksum.Add(b);
            return chksum;
        }

        /// <summary> Adds an entire array of bytes to the checksum </summary>
        public void Add(byte[] bytes)
        { Add(bytes, 0, Check.NotNull(bytes).Length); }

        /// <summary> Adds a range from an array of bytes to the checksum </summary>
        public void Add(byte[] bytes, int start, int length)
        {
            Check.NotNull(bytes);
            int end = start + length;

            unchecked
            {
                for (int i = start; i < end; i++)
                    _crc64 = (((~_crc64 >> 8) & 0x00FFFFFFFFFFFFFF) ^ Table[(_crc64 ^ bytes[i]) & 0x0ff]);
            }
        }

        /// <summary> Adds an entire array of bytes to the checksum </summary>
        public static Crc64 operator +(Crc64 chksum, byte[] bytes)
        {
            chksum.Add(bytes, 0, bytes.Length);
            return chksum;
        }

        /// <summary> Adds a string to the checksum as a series of 16-bit values (big endian) </summary>
        public void Add(string text)
        {
            unchecked
            {
                foreach (char ch in Check.NotNull(text))
                {
                    _crc64 = (((~_crc64 >> 8) & 0x00FFFFFFFFFFFFFF) ^ Table[((int) _crc64 ^ ((byte) ch >> 8)) & 0x0ff]);
                    _crc64 = (((~_crc64 >> 8) & 0x00FFFFFFFFFFFFFF) ^ Table[(_crc64 ^ ((byte) ch)) & 0x0ff]);
                }
            }
        }

        /// <summary> Adds a string to the checksum as a series of 16-bit values </summary>
        public static Crc64 operator +(Crc64 chksum, string text)
        {
            chksum.Add(text);
            return chksum;
        }

        /// <summary> Extracts the correct hash code </summary>
        public override int GetHashCode() { return (int)(Value ^ (Value >> 32)); }

        /// <summary> Returns true if the other object is equal to this one </summary>
        public override bool Equals(object obj)
        {
            return 
                (obj is Crc64 && _crc64 == ((Crc64)obj)._crc64) ||
                (obj is long && Value == ((long)obj));
        }

        /// <summary> Returns true if the other object is equal to this one </summary>
        public bool Equals(Crc64 other) { return _crc64 == other._crc64; }
        /// <summary> Returns true if the CRC64 provided is equal to this one </summary>
        public bool Equals(long crc64) { return Value == crc64; }

        /// <summary> Compares the two objects for equality </summary>
        public static bool operator ==(Crc64 x, Crc64 y) { return x._crc64 == y._crc64; }
        /// <summary> Compares the two objects for equality </summary>
        public static bool operator !=(Crc64 x, Crc64 y) { return x._crc64 != y._crc64; }

        /// <summary> Compares the two objects for equality </summary>
        public static bool operator ==(Crc64 x, long y) { return x.Value == y; }
        /// <summary> Compares the two objects for equality </summary>
        public static bool operator !=(Crc64 x, long y) { return x.Value != y; }

    }
}