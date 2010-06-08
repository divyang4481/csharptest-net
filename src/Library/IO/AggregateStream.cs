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
using System.Collections.Generic;
using System.IO;

// disables non-comment warning
#pragma warning disable 1591

namespace CSharpTest.Net.IO
{
    /// <summary>
    /// Provides a base-class that aggregates another stream object
    /// </summary>
    public abstract class AggregateStream : Stream
    {
        Stream _stream;

        /// <summary> Creates the wrapper without an underlying stream </summary>
        protected AggregateStream() { _stream = null; }
        /// <summary> Creates the wrapper with the underlying stream </summary>
        protected AggregateStream(Stream io) { _stream = io; }

        /// <summary> Disposes of this.Stream </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _stream != null)
                _stream.Dispose();

            base.Dispose(disposing);
        }

        /// <summary> Allows modifying the underlying stream </summary>
        protected virtual Stream Stream { get { return _stream ?? Stream.Null; } set { _stream = value; } }
        
        public override bool CanRead { get { return Stream.CanRead; } }
        public override bool CanSeek { get { return Stream.CanSeek; } }
        public override bool CanWrite { get { return Stream.CanWrite; } }

        public override long Length { get { return Stream.Length; } }
        public override long Position
        {
            get { return Stream.Position; }
            set { Stream.Position = value; }
        }

        public override void Flush()
        {
            Stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }
    }
}
