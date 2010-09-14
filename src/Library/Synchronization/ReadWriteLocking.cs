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
	public class ReadWriteLocking : LockStrategyBase
	{
		private readonly ReaderWriterLock _lock;
		/// <summary>
		/// wraps the reader/writer lock
		/// </summary>
		public ReadWriteLocking() : this(new ReaderWriterLock())
		{ }
		/// <summary>
		/// wraps the reader/writer lock
		/// </summary>
		public ReadWriteLocking(ReaderWriterLock lck)
		{ _lock = lck; }

		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		protected override bool TryRead(int timeout, out IReadLock readLock)
		{
			try
			{
				_lock.AcquireReaderLock(timeout);
				readLock = new ReadLock(_lock);
				return true;
			}
			catch (ApplicationException)
			{
				readLock = null;
				return false;
			}
		}

		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		protected override bool TryWrite(int timeout, out IWriteLock writeLock)
		{
			try
			{
				_lock.AcquireWriterLock(timeout);
				writeLock = new WriteLock(_lock);
				return true;
			}
			catch (ApplicationException)
			{
				writeLock = null;
				return false;
			}
		}

		new class ReadLock : LockStrategyBase.ReadLock
		{
			private readonly ReaderWriterLock _lck;

			public ReadLock(ReaderWriterLock lck)
			{
				_lck = lck;
			}
			protected override bool TryWrite(int timeout, out IWriteLock writeLock)
			{
				try
				{
					writeLock = new WriteLock(_lck, _lck.UpgradeToWriterLock(timeout));
					return true;
				}
				catch(ApplicationException)
				{
					writeLock = null;
					return false;
				}
			}
			protected override void Dispose(bool disposing)
			{
				_lck.ReleaseReaderLock();
			}
		}

		new class WriteLock : LockStrategyBase.WriteLock
		{
			private readonly ReaderWriterLock _lck;
			private readonly bool _upgrade;
			private LockCookie _cookie;

			public WriteLock(ReaderWriterLock lck)
			{
				_lck = lck;
				_upgrade = false;
			}
			public WriteLock(ReaderWriterLock lck, LockCookie cookie)
			{
				_lck = lck;
				_upgrade = true;
				_cookie = cookie;	
			}
			protected override void Dispose(bool disposing)
			{
				if(_upgrade)
					_lck.DowngradeFromWriterLock(ref _cookie);
				else
					_lck.ReleaseWriterLock();
			}
		}
	}
}
