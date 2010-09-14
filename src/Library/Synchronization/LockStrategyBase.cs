using System;
using System.Threading;
using CSharpTest.Net.Bases;

namespace CSharpTest.Net.Synchronization
{
	/// <summary>
	/// Provides a base class for locking implementations
	/// </summary>
	[System.Diagnostics.DebuggerNonUserCode]
	public abstract class LockStrategyBase : ILockStrategy
	{
		/// <summary>
		/// Converts a TimeSpan timeout into an acceptable timout value: 
		/// either Timout.Infinite or: greater than or equal to 0 and less than int.MaxValue
		/// </summary>
		protected static Int32 ToMilliseconds(TimeSpan timeout)
		{
			int ivalue = (int)Math.Min(int.MaxValue, timeout.TotalMilliseconds);
			return ivalue == int.MaxValue ? Timeout.Infinite : ivalue < 0 ? 0 : ivalue;
		}

		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		protected abstract bool TryRead(int timeout, out IReadLock readLock);
		/// <summary>
		/// Returns true if the lock was successfully obtained within the timeout specified
		/// </summary>
		protected abstract bool TryWrite(int timeout, out IWriteLock writeLock);

		IReadLock ILockStrategy.Read()
		{
			IReadLock readLock;
			if (!TryRead(Timeout.Infinite, out readLock) || readLock == null)
				throw new InvalidOperationException();
			return readLock;
		}

		bool ILockStrategy.TryRead(TimeSpan timeout, out IReadLock readLock)
		{
			if (!TryRead(ToMilliseconds(timeout), out readLock))
				return false;
			if(readLock == null)
				throw new InvalidOperationException();
			return true;
		}

		IReadLock ILockStrategy.TryRead(TimeSpan timeout)
		{
			IReadLock readLock;
			if (!TryRead(ToMilliseconds(timeout), out readLock))
				throw new TimeoutException();
			if (readLock == null)
				throw new InvalidOperationException();
			return readLock;
		}

		IWriteLock ILockStrategy.Write()
		{
			IWriteLock writeLock;
			if (!TryWrite(Timeout.Infinite, out writeLock) || writeLock == null)
				throw new InvalidOperationException();
			return writeLock;
		}

		bool ILockStrategy.TryWrite(TimeSpan timeout, out IWriteLock writeLock)
		{
			if (!TryWrite(ToMilliseconds(timeout), out writeLock))
				return false;
			if (writeLock == null)
				throw new InvalidOperationException();
			return true;
		}

		IWriteLock ILockStrategy.TryWrite(TimeSpan timeout)
		{
			IWriteLock writeLock;
			if (!TryWrite(ToMilliseconds(timeout), out writeLock))
				throw new TimeoutException();
			if (writeLock == null)
				throw new InvalidOperationException();
			return writeLock;
		}

		/// <summary>
		/// provides an abstract base for implementing a disposable read lock, Dispose will be
		/// called once and only once
		/// </summary>
		protected abstract class ReadLock : Disposable, IReadLock
		{
			/// <summary>
			/// Returns true if the lock was successfully obtained within the timeout specified
			/// </summary>
			protected abstract bool TryWrite(int timeout, out IWriteLock writeLock);

			#region IReadLock Members

			IWriteLock IReadLock.Write()
			{
				IWriteLock writeLock;
				if (!TryWrite(Timeout.Infinite, out writeLock) || writeLock == null)
					throw new InvalidOperationException();
				return writeLock;
			}

			bool IReadLock.TryWrite(TimeSpan timeout, out IWriteLock writeLock)
			{
				if (!TryWrite(ToMilliseconds(timeout), out writeLock))
					return false;
				if (writeLock == null)
					throw new InvalidOperationException();
				return true;
			}

			IWriteLock IReadLock.TryWrite(TimeSpan timeout)
			{
				IWriteLock writeLock;
				if (!TryWrite(ToMilliseconds(timeout), out writeLock))
					throw new TimeoutException();
				if (writeLock == null)
					throw new InvalidOperationException();
				return writeLock;
			}

			#endregion
		}

		/// <summary>
		/// provides an abstract base for implementing a disposable read lock, Dispose will be
		/// called once and only once
		/// </summary>
		protected abstract class WriteLock : Disposable, IWriteLock
		{
		}
	}
}
