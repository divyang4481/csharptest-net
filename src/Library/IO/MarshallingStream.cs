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
using System.Runtime.InteropServices;

namespace CSharpTest.Net.IO
{
    /// <summary> A stream that marshals bytes from unmanaged memory </summary>
    public class MarshallingStream : BaseStream
    {
        readonly IntPtr _ptrBytes;
        readonly bool _readOnly;
        readonly long _length;
        long _position;

        /// <summary> Constructs a stream that marshals bytes from unmanaged memory </summary>
        public MarshallingStream(IntPtr ptrBytes, bool readOnly, int start, int length)
        {
            _ptrBytes = start == 0 ? ptrBytes : new IntPtr(ptrBytes.ToInt64() + start);
            _readOnly = readOnly;
            _length = length;
            _position = 0;
        }
        /// <summary> Constructs a stream that marshals bytes from unmanaged memory </summary>
        public MarshallingStream(IntPtr ptrBytes, bool readOnly, int length)
            : this(ptrBytes, readOnly, 0, length)
        { }

// disables non-comment warning
#pragma warning disable 1591

        protected override void Dispose(bool disposing)
        {
            _position = -1;
            base.Dispose(disposing);
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return !_readOnly; } }

        public override long Length
        {
            get
            {
                CheckDisposed();
                return _length;
            }
        }

        public override long Position
        {
            get
            {
                CheckDisposed();
                return _position;
            }
            set
            {
                CheckDisposed();
                _position = Check.InRange<long>(value, 0, _length);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            if (origin == SeekOrigin.Current) offset = _position + offset;
            else if (origin == SeekOrigin.End) offset = _length + offset;
            return this.Position = offset;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            
            long bytesAvail = _length - _position;
            count = (int)Math.Min(count, bytesAvail);
            if (count <= 0) return 0;

            IntPtr pOffset = new IntPtr(_ptrBytes.ToInt64() + _position);
            Marshal.Copy(pOffset, buffer, offset, count);
            _position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            Check.Assert<InvalidOperationException>(CanWrite);

            long bytesAvail = _length - _position;
            Check.InRange<long>(count, 0, bytesAvail);

            IntPtr pOffset = new IntPtr(_ptrBytes.ToInt64() + _position);
            Marshal.Copy(buffer, offset, pOffset, count);
            _position += count;
        }

        private void CheckDisposed() { Check.Assert(_position >= 0, DisposedException); }
        private Exception DisposedException() { return new ObjectDisposedException(this.GetType().FullName); }
    }
}
