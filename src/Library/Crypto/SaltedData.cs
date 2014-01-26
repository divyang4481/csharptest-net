#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using CSharpTest.Net.IO;

namespace CSharpTest.Net.Crypto
{
    /// <summary> Represents the combination of Salt and Data together </summary>
    public class SaltedData : IDisposable
    {
        readonly Salt _salt;
        readonly byte[] _data;

        /// <summary> Seperates the salt from the data provided </summary>
        public SaltedData(Stream saltedData) : this(IOStream.ReadAllBytes(saltedData), Salt.DefaultSize) { }
        /// <summary> Seperates the salt from the data provided </summary>
        public SaltedData(byte[] saltedData) : this(saltedData, Salt.DefaultSize) { }
        /// <summary> Seperates the salt from the data provided </summary>
        public SaltedData(byte[] saltedData, Salt.Size szSalt)
        {
            byte[] salt = Check.ArraySize(new byte[(int)szSalt / 8], 8, 64);
            Array.Copy(saltedData, 0, salt, 0, salt.Length);
            _salt = new Salt(salt, false);

            _data = new byte[saltedData.Length - salt.Length];
            Array.Copy(saltedData, salt.Length, _data, 0, _data.Length);
        }

        /// <summary> Combines the salt with the data provided </summary>
        public SaltedData(Salt salt, Stream data)
            : this(salt, IOStream.ReadAllBytes(data))
        { }
        /// <summary> Combines the salt with the data provided </summary>
        public SaltedData(Salt salt, byte[] data)
        {
            _salt = salt;
            _data = (byte[])data.Clone();
        }

        /// <summary> Attempts to clear the data from memory </summary>
        public void Dispose()
        { Array.Clear(_data, 0, _data.Length); }

        /// <summary> Returns the total length of Salt + Data </summary>
        public int Length { get { return _salt.Length + _data.Length; } }

        /// <summary> Returns the Salt being used. </summary>
        public Salt Salt { get { return _salt; } }

        /// <summary> Returns a copy of the data bytes </summary>
        public byte[] GetDataBytes()
        { return (byte[])_data.Clone(); }

        /// <summary> Returns a stream of just the data </summary>
        public Stream GetDataStream()
        { return new MemoryStream(_data, 0, _data.Length, false, false); }

        /// <summary> Returns the Array of Salt and Data together </summary>
        public byte[] ToArray()
        {
            byte[] result = new byte[this.Length];
            Salt.CopyTo(result, 0);
            Array.Copy(_data, 0, result, Salt.Length, _data.Length);
            return result;
        }

        /// <summary> Returns the Salt and Data as a stream </summary>
        public Stream ToStream()
        {
            return SaltedData.CombineStream(this.Salt, this.GetDataStream());
        }

        /// <summary> Returns a stream that combines the salt and data </summary>
        public static Stream CombineStream(Salt salt, Stream data)
        {
            return new IO.CombinedStream(salt.ToStream(), data);
        }
    }
}
