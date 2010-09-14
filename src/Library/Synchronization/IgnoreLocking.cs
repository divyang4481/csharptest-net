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

namespace CSharpTest.Net.Synchronization
{
	/// <summary>
	/// Provides the reader/writer locking interface, but resolves to a no-op and no actual
	/// locking is performed.
	/// </summary>
	public class IgnoreLocking : LockStrategyBase
	{
		private static readonly IgnoreAll Ignore = new IgnoreAll();
		
		/// <summary> Returns a singleton lock </summary>
		public static readonly ILockStrategy Instance = new IgnoreLocking();

		class IgnoreAll : ReadLock, IWriteLock
		{
			protected override bool TryWrite(int timeout, out IWriteLock writeLock)
			{
				writeLock = Ignore;
				return true;
			}

			protected override void Dispose(bool disposing)
			{ }
		}

		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		protected override bool TryRead(int timeout, out IReadLock readLock)
		{
			readLock = Ignore;
			return true;
		}

		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		protected override bool TryWrite(int timeout, out IWriteLock writeLock)
		{
			writeLock = Ignore;
			return true;
		}
	}
}
