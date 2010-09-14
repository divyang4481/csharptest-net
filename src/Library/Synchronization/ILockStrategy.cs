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
	/// An interface that allows reader/writer locking with the using() statement
	/// </summary>
	public interface ILockStrategy
	{
		/// <summary>
		/// Returns a reader lock that can be elevated to a write lock
		/// </summary>
		IReadLock Read();
		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		bool TryRead(TimeSpan timeout, out IReadLock readLock);
		/// <summary>
		/// Returns the lock if it was successfully obtained within the timeout specified
		/// throws System.TimeoutException
		/// </summary>
		IReadLock TryRead(TimeSpan timeout);
		/// <summary>
		/// Returns a read and write lock
		/// </summary>
		IWriteLock Write();
		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		bool TryWrite(TimeSpan timeout, out IWriteLock writeLock);
		/// <summary>
		/// Returns the lock if it was successfully obtained within the timeout specified
		/// throws System.TimeoutException
		/// </summary>
		IWriteLock TryWrite(TimeSpan timeout);
	}

	/// <summary>
	/// Allows a read lock to be disposed or elevated to a write lock
	/// </summary>
	public interface IReadLock : IDisposable
	{
		/// <summary>
		/// Elevate to a writer lock
		/// </summary>
		IWriteLock Write();
		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		bool TryWrite(TimeSpan timeout, out IWriteLock writeLock);
		/// <summary>
		/// Returns the lock if it was successfully obtained within the timeout specified
		/// throws System.TimeoutException
		/// </summary>
		IWriteLock TryWrite(TimeSpan timeout);
	}

	/// <summary>
	/// Allows a write lock to be disposed
	/// </summary>
	public interface IWriteLock : IDisposable
	{ }
}
