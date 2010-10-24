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
    /// The hexidecimal encoding for the bytes using the following characters: 0-9, a-f
    /// </summary>
    public static class HexEncoding
    {
		static readonly int[] HexValues;
		static readonly byte[] HexChars;
        static HexEncoding()
		{
			HexChars = Encoding.ASCII.GetBytes("0123456789abcdef");
			HexValues = new int[103];
			for (int i = 0; i < 10; i++) HexValues['0' + i] = i;
			for (int i = 0; i < 6; i++) HexValues['a' + i] = 10 + i;
			for (int i = 0; i < 6; i++) HexValues['A' + i] = 10 + i;
		}

		/// <summary> Transforms a sequence of characters from '0' - '9' and 'a' - 'b' in the binary values </summary>
        public static string EncodeBytes(byte[] input)
        { return EncodeBytes(input, 0, Check.NotNull(input).Length); }
        /// <summary> Transforms a sequence of characters from '0' - '9' and 'a' - 'b' in the binary values </summary>
        public static string EncodeBytes(byte[] input, int start, int length)
        {
            byte[] output = new byte[length * 2];
            int len = EncodeBytes(input, start, length, output, 0);
            return Encoding.ASCII.GetString(output, 0, len);
        }
        /// <summary> Transforms a sequence of characters from '0' - '9' and 'a' - 'b' in the binary values </summary>
        public static int EncodeBytes(byte[] input, int start, int length, byte[] output, int offset)
		{
			Check.NotNull(input);
			Check.InRange(start, 0, input.Length);
			Check.InRange(length, 0, input.Length - start);
            Check.InRange(output.Length, (length * 2) + offset, int.MaxValue);

			int pos = offset;
			for (int i = 0; i < length; i++)
			{
				byte b = input[i + start];
                output[pos++] = (HexChars[(b >> 4) & 0x0f]);
                output[pos++] = (HexChars[b & 0x0f]);
			}
			return pos - offset;
		}

        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static byte[] DecodeBytes(string input)
        { return DecodeBytes(input, 0, Check.NotNull(input).Length); }
        /// <summary> Returns the original byte array provided when the encoding was performed </summary>
        public static byte[] DecodeBytes(string input, int start, int length)
        { return DecodeBytes(System.Text.Encoding.ASCII.GetBytes(Check.NotNull(input)), start, length); }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static byte[] DecodeBytes(byte[] input)
        { return DecodeBytes(input, 0, Check.NotNull(input).Length); }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static byte[] DecodeBytes(byte[] input, int start, int length)
        {
            byte[] results = new byte[length / 2];
            int used = DecodeBytes(input, start, length, results, 0);
            if (used != results.Length)
                Array.Resize(ref results, used);
            return results;
        }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static int DecodeBytes(byte[] input, int start, int length, byte[] results, int offset)
		{
			Check.NotNull(input);
			Check.InRange(start, 0, input.Length);
			Check.InRange(length, 0, input.Length - start);
            Check.InRange(results.Length, offset + (length / 2), int.MaxValue);

            int pos = offset;
            int end = start + length;
            int i = start;
            while(i < end)
			{
				byte ch1 = input[i++];
                if (Char.IsWhiteSpace((Char)ch1) || ch1 == '-')
                    continue;
                if (i >= input.Length)
                    throw new FormatException();
                byte ch2 = input[i++];

                if (((ch1 >= '0' && ch1 <= '9') || (ch1 >= 'a' && ch1 <= 'f') || (ch1 >= 'A' && ch1 <= 'F')) &&
					((ch2 >= '0' && ch2 <= '9') || (ch2 >= 'a' && ch2 <= 'f') || (ch2 >= 'A' && ch2 <= 'F')))
                    results[pos++] = (byte)(HexValues[ch1] << 4 | HexValues[ch2]);
				else
					throw new FormatException();
			}

            return pos - offset;
		}
    }
}
