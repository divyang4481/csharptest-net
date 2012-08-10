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
using System.IO;
using System.Text;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Bufferless stream reader for Unicode data
    /// </summary>
    public class UnicodeReader : TextReader
    {
        readonly Encoding _encoding;
        readonly Stream _stream;
        int _peek = -1;

        /// <summary>
        /// Bufferless stream reader for Unicode data
        /// </summary>
        public UnicodeReader(Stream stream) : this(stream, Encoding.Unicode) { }
        /// <summary>
        /// Bufferless stream reader for Unicode data
        /// </summary>
        public UnicodeReader(Stream stream, Encoding encoding)
        {
            Check.Assert<ArgumentException>(
                Object.ReferenceEquals(encoding, Encoding.Unicode) ||
                Object.ReferenceEquals(encoding, Encoding.BigEndianUnicode)
                );
            _encoding = encoding;
            _stream = stream;
        }

        /// <summary>
        /// Disposes the underlying stream
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _peek = -1;
            _stream.Dispose();
            base.Dispose(disposing);
        }

        int Next()
        {
            if (_peek >= 0)
            {
                int ch = _peek;
                _peek = -1;
                return ch;
            }
            byte[] tmp = new byte[2];
            Char[] chars = new Char[1];
            try
            {
                if (_stream.Read(tmp, 0, 2) != 2 || _encoding.GetChars(tmp, 0, 2, chars, 0) != 1)
                    return -1;
                return chars[0];
            }
            finally
            {
                tmp[0] = tmp[1] = 0;
                chars[0] = Char.MinValue;
            }
        }

        /// <summary> Returns the next character </summary>
        public override int Peek()
        {
            return _peek = Next();
        }

        /// <summary> Returns the next character </summary>
        public override int Read()
        {
            return Next();
        }

        /// <summary> Reads one character </summary>
        public override int Read(char[] buffer, int index, int count)
        {
            Check.ArraySize(buffer, index + count, int.MaxValue);
            int next;
            if (count > 0 && -1 != (next = Next()))
            {
                buffer[index] = (Char)next;
                return 1;
            }
            return 0;
        }

        /// <summary> Reads one character </summary>
        public override int ReadBlock(char[] buffer, int index, int count)
        { return Read(buffer, index, count); }
    }
}
