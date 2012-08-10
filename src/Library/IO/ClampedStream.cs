#region Copyright 2011-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.IO
{
    /// <summary>
    /// Provides a stream that clamps the usage of the input stream to a specific range, length or offset
    /// </summary>
    public class ClampedStream : Stream
    {
        private readonly Stream _rawStream;

        private readonly bool _disposeOfStream;
        private readonly long _startPosition;
        private readonly long _limitPosition;
        private bool _disposed;
        private long _current;

        /// <summary>
        /// Creates a stream that limits the users ability to modify data to the specified range
        /// </summary>
        /// <param name="rawStream">The stream to use for read/write</param>
        /// <param name="start">The position in the stream that should start the range of allowed bytes</param>
        /// <param name="length">The maximum length that can be read from the stream</param>
        public ClampedStream(Stream rawStream, long start, long length)
            : this(rawStream, start, length, true) { }

        /// <summary>
        /// Creates a stream that limits the users ability to modify data to the specified range
        /// </summary>
        /// <param name="rawStream">The stream to use for read/write</param>
        /// <param name="start">The position in the stream that should start the range of allowed bytes</param>
        /// <param name="length">The maximum length that can be read from the stream</param>
        /// <param name="disposeOfStream">True to dispose of the rawStream when this stream is disposed</param>
        public ClampedStream(Stream rawStream, long start, long length, bool disposeOfStream)
        {
            _rawStream = rawStream;
            _disposeOfStream = disposeOfStream;
            _startPosition = start;

            Check.InRange(start, 0, long.MaxValue);
            Check.InRange(length, 0, long.MaxValue);

            if (length < long.MaxValue)
            {
                if (((ulong)start) + ((ulong)length) > long.MaxValue)
                    throw new ArgumentOutOfRangeException();
                _limitPosition = start + length;
            }
            else _limitPosition = long.MaxValue;

            if (_rawStream.CanSeek)
                _current = _rawStream.Seek(_startPosition, SeekOrigin.Begin);
            else if (_startPosition > 0)
                throw new ArgumentException("Unable to seek to the offset required.");
        }

        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
        /// </summary>
        public override void Close() 
        {
            if(_disposeOfStream)
                _rawStream.Close();

            base.Close();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream"/> and optionally releases the managed resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing && _disposeOfStream)
                    _rawStream.Dispose();
            }
            base.Dispose(disposing);
        }

        private void IsNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead { get { return _rawStream.CanRead; } }
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek { get { return _rawStream.CanSeek; } }
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite { get { return _rawStream.CanWrite; } }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            IsNotDisposed();
            if(_rawStream.CanWrite)
                _rawStream.Flush();
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            IsNotDisposed();
            if (origin == SeekOrigin.Begin)
                offset += _startPosition;
            else if (origin == SeekOrigin.Current)
                offset += _rawStream.Position;
            else if (origin == SeekOrigin.End)
                offset += Math.Min(_limitPosition, _rawStream.Length);
            else
                throw new ArgumentOutOfRangeException("origin");

            if (offset < _startPosition)
            {
                _rawStream.Seek(-1, SeekOrigin.Begin);
                throw new ArgumentOutOfRangeException("offset");
            }
            if (offset > _limitPosition)
                throw new ArgumentOutOfRangeException("offset", "Attempt to seek past end of stream.");

            _current = _rawStream.Seek(offset, SeekOrigin.Begin);
            return _current - _startPosition;
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown when length != long.MaxValue</exception>
        public override void SetLength(long value)
        {
            IsNotDisposed();
            Check.Assert<NotSupportedException>(_limitPosition == long.MaxValue); 
            Check.InRange(value, 0, _limitPosition - _startPosition);

            value += _startPosition;
            value = Math.Min(_limitPosition, value);

            _rawStream.SetLength(value);
        }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        public override long Length
        {
            get { return Math.Max(0, Math.Min(_limitPosition, _rawStream.Length) - _startPosition); }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get { IsNotDisposed(); return _current - _startPosition; }
            set { IsNotDisposed(); Seek(value, SeekOrigin.Begin); }
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            IsNotDisposed();
            Check.Assert<NotSupportedException>(CanRead);
            long remaining = _limitPosition - _current;
            if (remaining < count)
                count = (int)remaining;
            if (count <= 0)
                return 0;

            int amt = _rawStream.Read(buffer, offset, count);
            _current += amt;
            return amt;
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        public override int ReadByte()
        {
            IsNotDisposed();
            Check.Assert<NotSupportedException>(CanRead);
            long remaining = _limitPosition - _current;
            if (remaining < 1)
                return -1;
            
            int result = _rawStream.ReadByte();
            _current++;
            return result;
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            IsNotDisposed();
            Check.Assert<NotSupportedException>(CanWrite);
            long remaining = _limitPosition - _current;
            if (remaining < count)
                throw new InvalidOperationException("Attempt to write past end of stream.");

            _rawStream.Write(buffer, offset, count);
            _current += count;
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        public override void WriteByte(byte value)
        {
            IsNotDisposed();
            Check.Assert<NotSupportedException>(CanWrite);
            long remaining = _limitPosition - _current;
            if (remaining < 1)
                throw new InvalidOperationException("Attempt to write past end of stream.");

            base.WriteByte(value);
            _current++;
        }
    }
}