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
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CSharpTest.Net.Formatting
{
    /// <summary> Provides reading and writing to a stream of base-64 characters. </summary>
    public class Base64Stream : CryptoStream
    {
        /// <summary> Provides reading and writing to a stream of base-64 characters. </summary>
        public Base64Stream(Stream stream, CryptoStreamMode mode)
            : base(stream, new Transform(mode), mode) 
        { }

        /// <summary> Provides a crypto-transform used to read/write to a stream of base-64 characters. </summary>
        public class Transform : ICryptoTransform
        {
            readonly CryptoStreamMode _mode;
            /// <summary> Provides a crypto-transform used to read/write to a stream of base-64 characters. </summary>
            public Transform(CryptoStreamMode mode) { _mode = mode; }

            void IDisposable.Dispose() { }
            bool ICryptoTransform.CanReuseTransform { get { return true; } }
            bool ICryptoTransform.CanTransformMultipleBlocks { get { return true; } }
            int ICryptoTransform.InputBlockSize { get { return _mode == CryptoStreamMode.Read ? 4 : 3; } }
            int ICryptoTransform.OutputBlockSize { get { return _mode == CryptoStreamMode.Read ? 3 : 4; } }

            int ICryptoTransform.TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
            {
                if( _mode == CryptoStreamMode.Read )
                {
                    char[] chars = Encoding.ASCII.GetChars(inputBuffer, inputOffset, inputCount);
                    byte[] bytes = Convert.FromBase64CharArray(chars, 0, chars.Length);
                    bytes.CopyTo(outputBuffer, outputOffset);
                    return bytes.Length;
                }
                else
                {
                    string temp = Convert.ToBase64String(inputBuffer, inputOffset, inputCount);
                    Encoding.ASCII.GetBytes(temp, 0, temp.Length, outputBuffer, outputOffset);
                    return temp.Length;
                }
            }
            byte[] ICryptoTransform.TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                if (inputCount == 0)
                    return new byte[0];
                if (_mode == CryptoStreamMode.Read)
                {
                    char[] chars = new char[(inputCount + 3) & ~3];
                    int used = Encoding.ASCII.GetChars(inputBuffer, inputOffset, inputCount, chars, 0);
                    for (; used < chars.Length; used++)
                        chars[used] = '=';
                    return Convert.FromBase64CharArray(chars, 0, chars.Length);
                }
                else
                {
                    return Encoding.ASCII.GetBytes(Convert.ToBase64String(inputBuffer, inputOffset, inputCount));
                }
            }
        }
    }
}
