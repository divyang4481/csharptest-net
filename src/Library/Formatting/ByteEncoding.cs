#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.Formatting
{
    /// <summary> Defines a type of formatting for encoding byte[] to a string value </summary>
    public abstract class ByteEncoding
    {
        /// <summary>Encodes a set of bytes and returns the encoded text as a string</summary>
        public abstract string EncodeBytes(byte[] input);
        /// <summary>Decodes the string provided and returns the original set of bytes</summary>
        public abstract byte[] DecodeBytes(string input);

        /// <summary> Standard base-64 padded encoding using the following characters: a-z, A-Z, 0-9, +, /, = </summary>
        public static readonly ByteEncoding Base64 = new Base64Impl();
        class Base64Impl : ByteEncoding
        {
            public override string EncodeBytes(byte[] input) { return Convert.ToBase64String(input); }
            public override byte[] DecodeBytes(string input) { return Convert.FromBase64String(input); }
        }

        /// <summary> A modified base-64 non-padded encoding using the following characters: a-z, A-Z, 0-9, -, _ </summary>
        public static readonly ByteEncoding Safe64 = new Safe64Impl();
        class Safe64Impl : ByteEncoding
        {
            public override string EncodeBytes(byte[] input) { return Safe64Encoding.EncodeBytes(input); }
            public override byte[] DecodeBytes(string input) { return Safe64Encoding.DecodeBytes(input); }
        }
       
        /// <summary> The hexidecimal encoding for the bytes using the following characters: 0-9, a-f </summary>
        public static readonly ByteEncoding Hex = new HexImpl();
        class HexImpl : ByteEncoding
        {
            public override string EncodeBytes(byte[] input) { return HexEncoding.EncodeBytes(input); }
            public override byte[] DecodeBytes(string input) { return HexEncoding.DecodeBytes(input); }
        }
    }
}