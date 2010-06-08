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
	/// Creates a stream over an array of byte arrays in memory to reduce use of the LOH and array resizing operation.
	/// </summary>
	public class SegmentedMemoryStream : Stream
	{
		readonly List<byte[]> _contents;
		readonly int _segmentSize;
		long _length;
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
			_segmentSize = segmentSize;
			_contents = new List<byte[]>();
			_length = 0;
			_position = 0;
		}

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return true; } }
		public override bool CanWrite { get { return true; } }

		public override void Flush()
		{ }

		protected override void Dispose(bool disposing)
		{
			_contents.Clear();
			_length = _position = -1;
			base.Dispose(disposing);
		}

		public override long Length
		{
			get { AssertOpen(); return _length; }
		}

		private void AssertOpen()
		{
			if (_length < 0) throw new ObjectDisposedException(GetType().FullName);
		}

		private void OffsetToIndex(long offset, out int arrayIx, out int arrayOffset)
		{
			arrayIx = (int)(offset / _segmentSize);
			arrayOffset = (int)(offset % _segmentSize);
		}

		public override void SetLength(long value)
		{
			AssertOpen();
			Check.InRange<long>(value, 0L, int.MaxValue);

			int arrayIx, arrayOffset;
			OffsetToIndex(value, out arrayIx, out arrayOffset);

			int chunksRequired = arrayIx + (arrayOffset > 0 ? 1 : 0);
			while (_contents.Count < chunksRequired)
				_contents.Add(new byte[_segmentSize]);

			_length = value;
		}

		public override long Position
		{
			get { return _position; }
			set
			{
				AssertOpen();
				Check.InRange<long>(value, 0L, int.MaxValue);
				if (value > _length)
					SetLength(value);
				_position = value;
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			if (origin == SeekOrigin.End)
				offset = _length + offset;
			if (origin == SeekOrigin.Current)
				offset = _position + offset;
			return Position = offset;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			AssertOpen();
			int total = 0, arrayIx, arrayOffset;
			if ((_length - _position) < count)
				count = (int)(_length - _position);

			while (count > 0)
			{
				OffsetToIndex(_position, out arrayIx, out arrayOffset);
				int amt = Math.Min(_segmentSize - arrayOffset, count);

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
			if ((_position + count) > _length)
				SetLength(_position + count);

			int arrayIx, arrayOffset;
			while (count > 0)
			{
				OffsetToIndex(_position, out arrayIx, out arrayOffset);
				int amt = Math.Min(_segmentSize - arrayOffset, count);

				byte[] chunk = _contents[arrayIx];
				Array.Copy(buffer, offset, chunk, arrayOffset, amt);
				count -= amt;
				offset += amt;
				_position += amt;
			}
		}
	}
}