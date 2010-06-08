using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// This encoding produces a 'url' safe string from bytes, similar to base64 encoding yet
    /// it replaces '+' with '-', '/' with '_' and omits padding.
    /// </summary>
    public class AsciiEncoder
    {
        internal static readonly char[] chTable64;
        internal static readonly byte[] chValue64;
        static AsciiEncoder()
        {
            chTable64 = new char[] { 
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 
                'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 
                'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 
                'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_', 
            };
            chValue64 = new byte[(int)'z' + 1];
            for (int i = 0; i < 64; i++)
                chValue64[(int)chTable64[i]] = (byte)i;
        }

        /// <summary> Returns the original byte array provided when the encoding was performed </summary>
        public static byte[] DecodeBytes(string data)
        {
            return DecodeBytes(System.Text.Encoding.ASCII.GetBytes(data));
        }
        /// <summary> Decodes the ascii text from the bytes provided into the original byte array </summary>
        public static byte[] DecodeBytes(byte[] inData)
        {
            int length = inData.Length;
            byte[] outBytes = new byte[(length * 6) >> 3];

            int leftover = length % 4;
            int stop = (length - leftover);
            int index = 0;
            int pos;
            for (pos = 0; pos < stop; pos += 4)
            {
                outBytes[index] = (byte)((chValue64[inData[pos]] << 2) | (chValue64[inData[pos + 1]] >> 4));
                outBytes[index + 1] = (byte)(((chValue64[inData[pos + 1]]) << 4) | (chValue64[inData[pos + 2]] >> 2));
                outBytes[index + 2] = (byte)(((chValue64[inData[pos + 2]]) << 6) | (chValue64[inData[pos + 3]]));
                index += 3;
            }

            if (leftover == 2)
                outBytes[index] = (byte)((chValue64[inData[pos]] << 2) | (chValue64[inData[pos + 1]] >> 4));
            else if (leftover == 3)
            {
                outBytes[index] = (byte)((chValue64[inData[pos]] << 2) | (chValue64[inData[pos + 1]] >> 4));
                outBytes[index + 1] = (byte)(((chValue64[inData[pos + 1]]) << 4) | (chValue64[inData[pos + 2]] >> 2));
            }

            return outBytes;
        }
        /// <summary> Returns a encoded string of ascii characters that are URI safe </summary>
        public static string EncodeBytes(byte[] inData)
        {
            int length = inData.Length;
            char[] outChars = new char[(int)Math.Ceiling((length << 3) / 6d)];
            
            int leftover = length % 3;
            int stop = (length - leftover);
            int index = 0;
            int pos;
            for (pos = 0; pos < stop; pos += 3)
            {
                outChars[index] = chTable64[(inData[pos] & 0xfc) >> 2];
                outChars[index + 1] = chTable64[((inData[pos] & 3) << 4) | ((inData[pos + 1] & 240) >> 4)];
                outChars[index + 2] = chTable64[((inData[pos + 1] & 15) << 2) | ((inData[pos + 2] & 0xc0) >> 6)];
                outChars[index + 3] = chTable64[inData[pos + 2] & 0x3f];
                index += 4;
            }

            switch (leftover)
            {
                case 1:
                    outChars[index] = chTable64[(inData[pos] & 0xfc) >> 2];
                    outChars[index + 1] = chTable64[(inData[pos] & 3) << 4];
                    break;

                case 2:
                    outChars[index] = chTable64[(inData[pos] & 0xfc) >> 2];
                    outChars[index + 1] = chTable64[((inData[pos] & 3) << 4) | ((inData[pos + 1] & 240) >> 4)];
                    outChars[index + 2] = chTable64[(inData[pos + 1] & 15) << 2];
                    break;
            }
            return new String(outChars);
        }
    }
}
