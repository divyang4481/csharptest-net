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

namespace CSharpTest.Net.Formatting
{
    /// <summary> Provides reading and writing to a stream of hexidecimal characters. </summary>
    public class HexStream : CryptoStream
    {
        /// <summary> Provides reading and writing to a stream of hexidecimal characters. </summary>
        public HexStream(Stream stream, CryptoStreamMode mode)
            : base(stream, new Transform(mode), mode) 
        { }

        /// <summary> Provides a crypto-transform used to read/write to a stream of hexidecimal characters. </summary>
        public class Transform : ICryptoTransform
        {
            readonly CryptoStreamMode _mode;
            /// <summary> Provides a crypto-transform used to read/write to a stream of hexidecimal characters. </summary>
            public Transform(CryptoStreamMode mode) { _mode = mode; }

            void IDisposable.Dispose() { }
            bool ICryptoTransform.CanReuseTransform { get { return true; } }
            bool ICryptoTransform.CanTransformMultipleBlocks { get { return true; } }
            int ICryptoTransform.InputBlockSize { get { return _mode == CryptoStreamMode.Read ? 2 : 1; } }
            int ICryptoTransform.OutputBlockSize { get { return _mode == CryptoStreamMode.Read ? 1 : 2; } }

            int ICryptoTransform.TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
            {
                return _mode == CryptoStreamMode.Read 
                    ? HexEncoding.DecodeBytes(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset)
                    : HexEncoding.EncodeBytes(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
            }
            byte[] ICryptoTransform.TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                int size = _mode == CryptoStreamMode.Read ? (inputCount / 2) : (inputCount * 2);
                byte[] output = new byte[size];
                int pos = ((ICryptoTransform)this).TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
                if (pos != output.Length)
                    Array.Resize(ref output, pos);
                return output;
            }
        }
    }
}
