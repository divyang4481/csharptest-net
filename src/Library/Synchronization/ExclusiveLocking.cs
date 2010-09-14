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
using System.Threading;
using CSharpTest.Net.Bases;

namespace CSharpTest.Net.Synchronization
{
	/// <summary>
	/// wraps the reader/writer lock 
	/// </summary>
	public class ExclusiveLocking : LockStrategyBase
	{
		private readonly object _lck;
		/// <summary>
		/// wraps the reader/writer lock
		/// </summary>
		public ExclusiveLocking() : this(new Object())
		{ }
		/// <summary>
		/// wraps the reader/writer lock
		/// </summary>
		public ExclusiveLocking(Object lck)
		{
			_lck = lck;
		}

		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		protected override bool TryRead(int timeout, out IReadLock readLock)
		{
			if (Monitor.TryEnter(_lck, timeout))
			{
				readLock = new ReadLock(_lck);
				return true;
			}

			readLock = null;
			return false;
		}

		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		protected override bool TryWrite(int timeout, out IWriteLock writeLock)
		{
			if (Monitor.TryEnter(_lck, timeout))
			{
				writeLock = new WriteLock(_lck);
				return true;
			}

			writeLock = null;
			return false;
		}

		new class ReadLock : LockStrategyBase.ReadLock
		{
			private readonly Object _lck;

			public ReadLock(Object lck)
			{
				_lck = lck;
			}
			protected override bool TryWrite(int timeout, out IWriteLock writeLock)
			{
				if (Monitor.TryEnter(_lck, timeout))
				{
					writeLock = new WriteLock(_lck);
					return true;
				}

				writeLock = null;
				return false;
			}
			protected override void Dispose(bool disposing)
			{
				Monitor.Exit(_lck);
			}
		}
		new class WriteLock : LockStrategyBase.WriteLock
		{
			private readonly Object _lck;

			public WriteLock(Object lck)
			{
				_lck = lck;
			}
			protected override void Dispose(bool disposing)
			{
				Monitor.Exit(_lck);
			}
		}
	}
}
