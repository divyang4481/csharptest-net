#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

// disables non-comment warning
#pragma warning disable 1591

namespace CSharpTest.Net.IO
{
    /// <summary>
	/// Creates a stream over an array of byte arrays in memory to reduce use of the LOH and array resizing operation.
	/// </summary>
	public class SegmentedMemoryStream : Stream
    {
        #region Contents
        class Contents
        {
            readonly int _segmentSize;
            byte[][] _segments;
            int _segmentCount;
            long _length;

            public Contents(int segmentSize)
            {
                _segmentSize = segmentSize;
                _segments = new byte[16][];
            }

            public int SegmentSize { get { return _segmentSize; } }
            public long Length { get { return _length; } set { _length = value; } }

            public byte[] this[int index] { get { return _segments[index]; } }

            public void GrowTo(int newSegmentCount)
            {
                lock (this)
                {
                    while (_segments.Length < newSegmentCount)
                        Array.Resize(ref _segments, _segments.Length << 1);
                    while (_segmentCount < newSegmentCount)
                    {
                        _segments[_segmentCount] = new byte[_segmentSize];
                        _segmentCount++;
                    }
                }
            }
        }
        #endregion
        readonly Contents _contents;
		long _position;

		/// <summary>
		/// Creates a memory stream that uses 32k segments for storage
		/// </summary>
		public SegmentedMemoryStream()
			: this(short.MaxValue)
		{ }
		/// <summary>
		/// Create a memory stream that uses the specified size of segments
		/// </summary>
		public SegmentedMemoryStream(int segmentSize)
		{
			_contents = new Contents(segmentSize);
			_position = 0;
		}
        /// <summary> Creates a 'clone' of the stream sharing the same contents </summary>
        protected SegmentedMemoryStream(SegmentedMemoryStream from)
        {
            _contents = from._contents;
            _position = 0;
        }

	    public override bool CanRead { get { return _position >= 0; } }
        public override bool CanSeek { get { return _position >= 0; } }
        public override bool CanWrite { get { return _position >= 0; } }

		public override void Flush()
		{ }

		protected override void Dispose(bool disposing)
		{
			_position = -1;
			base.Dispose(disposing);
		}

		public override long Length
		{
			get { AssertOpen(); return _contents.Length; }
		}

		private void AssertOpen()
		{
            if (_position < 0) throw new ObjectDisposedException(GetType().FullName);
		}

		private void OffsetToIndex(long offset, out int arrayIx, out int arrayOffset)
		{
			arrayIx = (int)(offset / _contents.SegmentSize);
            arrayOffset = (int)(offset % _contents.SegmentSize);
		}

		public override void SetLength(long value)
		{
			AssertOpen();
			Check.InRange(value, 0L, int.MaxValue);

			int arrayIx, arrayOffset;
			OffsetToIndex(value, out arrayIx, out arrayOffset);

            int chunksRequired = arrayIx + (arrayOffset > 0 ? 1 : 0);
            _contents.GrowTo(chunksRequired);
            _contents.Length = value;
		}

	    public override long Position
		{
			get { return _position; }
			set
			{
				AssertOpen();
				Check.InRange(value, 0L, int.MaxValue);
				if (value > _contents.Length)
					SetLength(value);
				_position = value;
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			if (origin == SeekOrigin.End)
                offset = _contents.Length + offset;
			if (origin == SeekOrigin.Current)
				offset = _position + offset;
			return Position = offset;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			AssertOpen();
			int total = 0;
            if ((_contents.Length - _position) < count)
                count = (int)(_contents.Length - _position);

			while (count > 0)
			{
                int arrayIx, arrayOffset;
				OffsetToIndex(_position, out arrayIx, out arrayOffset);
                int amt = Math.Min(_contents.SegmentSize - arrayOffset, count);

				byte[] chunk = _contents[arrayIx];
				Array.Copy(chunk, arrayOffset, buffer, offset, amt);
				count -= amt;
				offset += amt;
				total += amt;
				_position += amt;
			}
			return total;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			AssertOpen();
            if ((_position + count) > _contents.Length)
				SetLength(_position + count);

			while (count > 0)
			{
			    int arrayIx, arrayOffset;
				OffsetToIndex(_position, out arrayIx, out arrayOffset);
                int amt = Math.Min(_contents.SegmentSize - arrayOffset, count);

				byte[] chunk = _contents[arrayIx];
				Array.Copy(buffer, offset, chunk, arrayOffset, amt);
				count -= amt;
				offset += amt;
				_position += amt;
			}
		}
	}
}