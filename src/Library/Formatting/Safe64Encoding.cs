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
using System.Text;

namespace CSharpTest.Net.Formatting
{
    /// <summary>
    /// This encoding produces a 'url' safe string from bytes, similar to base64 encoding yet
    /// it replaces '+' with '-', '/' with '_' and omits padding.
    /// </summary>
    public static class Safe64Encoding
    {
        internal static readonly byte[] chTable64;
        internal static readonly byte[] chValue64;
        const int MIN = '-';
        const int MAX = 'z' + 1;

        static Safe64Encoding()
        {
            chTable64 = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");
            chValue64 = new byte[MAX - MIN];
            for (byte i = 0; i < 64; i++)
                chValue64[chTable64[i] - MIN] = i;
        }

        /// <summary> Returns a encoded string of ascii characters that are URI safe </summary>
        public static string EncodeBytes(byte[] input)
        { return EncodeBytes(input, 0, Check.NotNull(input).Length); }
        /// <summary> Returns a encoded string of ascii characters that are URI safe </summary>
        public static string EncodeBytes(byte[] input, int start, int length)
        {
            byte[] output = new byte[(int)Math.Ceiling((length << 3) / 6d)];
            int len = EncodeBytes(input, start, length, output, 0);
            return Encoding.ASCII.GetString(output, 0, len);
        }
        /// <summary> Returns a encoded string of ascii characters that are URI safe </summary>
        public static int EncodeBytes(byte[] input, int start, int length, byte[] output, int offset)
        {
            if (output.Length < (offset + ((int)Math.Ceiling((length << 3) / 6d))))
                throw new ArgumentOutOfRangeException();
            int leftover = length % 3;
            int stop = start + (length - leftover);
            int index = offset;
            int pos;
            for (pos = start; pos < stop; pos += 3)
            {
                output[index] = chTable64[(input[pos] & 0xfc) >> 2];
                output[index + 1] = chTable64[((input[pos] & 3) << 4) | ((input[pos + 1] & 240) >> 4)];
                output[index + 2] = chTable64[((input[pos + 1] & 15) << 2) | ((input[pos + 2] & 0xc0) >> 6)];
                output[index + 3] = chTable64[input[pos + 2] & 0x3f];
                index += 4;
            }

            switch (leftover)
            {
                case 1:
                    output[index] = chTable64[(input[pos] & 0xfc) >> 2];
                    output[index + 1] = chTable64[(input[pos] & 3) << 4];
                    index += 2;
                    break;

                case 2:
                    output[index] = chTable64[(input[pos] & 0xfc) >> 2];
                    output[index + 1] = chTable64[((input[pos] & 3) << 4) | ((input[pos + 1] & 240) >> 4)];
                    output[index + 2] = chTable64[(input[pos + 1] & 15) << 2];
                    index += 3;
                    break;
            }
            return index - offset;
        }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static byte[] DecodeBytes(string input)
        { return DecodeBytes(input, 0, Check.NotNull(input).Length); }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static byte[] DecodeBytes(string input, int start, int length)
        { return DecodeBytes(System.Text.Encoding.ASCII.GetBytes(Check.NotNull(input)), start, length); }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static byte[] DecodeBytes(byte[] input)
        { return DecodeBytes(input, 0, Check.NotNull(input).Length); }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static byte[] DecodeBytes(byte[] input, int start, int length)
        {
            byte[] results = new byte[(length * 6) >> 3];
            int used = DecodeBytes(input, start, length, results, 0);
            if (used != results.Length)
                Array.Resize(ref results, used);
            return results;
        }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static int DecodeBytes(byte[] input, int start, int length, byte[] output, int offset)
        {
            if (output.Length < (offset + ((length * 6) >> 3)))
                throw new ArgumentOutOfRangeException();

            int leftover = length % 4;
            int stop = start + (length - leftover);
            int index = offset;
            int pos;
            for (pos = start; pos < stop; pos += 4)
            {
                output[index] = (byte)((chValue64[input[pos] - MIN] << 2) | (chValue64[input[pos + 1] - MIN] >> 4));
                output[index + 1] = (byte)(((chValue64[input[pos + 1] - MIN]) << 4) | (chValue64[input[pos + 2] - MIN] >> 2));
                output[index + 2] = (byte)(((chValue64[input[pos + 2] - MIN]) << 6) | (chValue64[input[pos + 3] - MIN]));
                index += 3;
            }

            if (leftover == 2)
            {
                output[index] = (byte)((chValue64[input[pos] - MIN] << 2) | (chValue64[input[pos + 1] - MIN] >> 4));
                index += 1;
            }
            else if (leftover == 3)
            {
                output[index] = (byte)((chValue64[input[pos] - MIN] << 2) | (chValue64[input[pos + 1] - MIN] >> 4));
                output[index + 1] = (byte)(((chValue64[input[pos + 1] - MIN]) << 4) | (chValue64[input[pos + 2] - MIN] >> 2));
                index += 2;
            }

            return index - offset;
        }
    }
}
