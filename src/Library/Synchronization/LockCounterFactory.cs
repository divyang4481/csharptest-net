#region Copyright 2011-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Threading;

namespace CSharpTest.Net.Synchronization
{
    /// <summary>
    /// Creates a debugging lock factory that can track locks allocated and all acquired/released read/write locks
    /// </summary>
    public class LockCounterFactory<T> : LockCounterFactory
        where T : ILockStrategy, new()
    {
        /// <summary> Constructs the lock tracking factory </summary>
        public LockCounterFactory()
            : base(new LockFactory<T>())
        { }
    }

    /// <summary>
    /// Creates a debugging lock factory that can track locks allocated and all acquired/released read/write locks
    /// </summary>
    public class LockCounterFactory : ILockFactory
    {
        readonly ILockFactory _factory;

        /// <summary> Constructs the lock tracking factory </summary>
        public LockCounterFactory(ILockFactory factory)
        { _factory = factory; }

        /// <summary> Constructs the lock wrapped in a DebugLocking instance </summary>
        public virtual ILockStrategy Create()
        {
            return new LockCounting(this, _factory.Create());
        }

        /// <summary> Returns the highest number of concurrent reads </summary>
        public int MaxReaderCount;

        /// <summary> Returns the highest number of concurrent writes (aka max recursive count) </summary>
        public int MaxWriterCount;

        /// <summary> Returns the total number of current readers for all threads </summary>
        public int CurrentReaderCount;

        /// <summary> Returns the total number of current writers for all threads </summary>
        public int CurrentWriterCount;

        /// <summary> Returns the total number of read locks acquired </summary>
        public int TotalReaderCount;

        /// <summary> Returns the total number of write locks acquired </summary>
        public int TotalWriterCount;

        /// <summary> Asserts that none of the locks handed out are currently locked for read or write by any thread </summary>
        public void GlobalAssertNoLocks()
        {
            DebugAssertionFailedException.Assert(CurrentWriterCount == 0, "One or more threads are still writing.");
            DebugAssertionFailedException.Assert(CurrentReaderCount == 0, "One or more threads are still reading.");
        }

        class LockCounting : ILockStrategy
        {
            readonly LockCounterFactory _factory;
            readonly ILockStrategy _lock;

            public LockCounting(LockCounterFactory factory, ILockStrategy lck)
            {
                _factory = factory;
                _lock = lck;
            }
            public void Dispose() { _lock.Dispose(); }

            public int WriteVersion { get { return _lock.WriteVersion; } }

            private static void AddCount(ref int maxValue, ref int currentValue, ref int totalValue)
            {
                Interlocked.Increment(ref totalValue);
                int newMax = Interlocked.Increment(ref currentValue);
                int oldMax;
                while (newMax > (oldMax = maxValue))
                    Interlocked.CompareExchange(ref maxValue, newMax, oldMax);
            }

            public bool TryRead(int timeout)
            {
                if (!_lock.TryRead(timeout)) return false;
                AddCount(ref _factory.MaxReaderCount, ref _factory.CurrentReaderCount, ref _factory.TotalReaderCount);
                return true;
            }

            public void ReleaseRead()
            {
                _lock.ReleaseRead();
                Interlocked.Decrement(ref _factory.CurrentReaderCount);
            }

            public bool TryWrite(int timeout)
            {
                if (!_lock.TryWrite(timeout)) return false;
                AddCount(ref _factory.MaxWriterCount, ref _factory.CurrentWriterCount, ref _factory.TotalWriterCount);
                return true;
            }

            public void ReleaseWrite()
            {
                _lock.ReleaseWrite();
                Interlocked.Decrement(ref _factory.CurrentWriterCount);
            }

            #region ILockStrategy Members

            /// <summary>
            /// Returns a reader lock that can be elevated to a write lock
            /// </summary>
            /// <exception cref="System.TimeoutException"/>
            public ReadLock Read(int timeout) { return ReadLock.Acquire(this, timeout); }

            /// <summary>
            /// Returns a reader lock that can be elevated to a write lock
            /// </summary>
            public ReadLock Read() { return ReadLock.Acquire(this, -1); }

            /// <summary>
            /// Returns a read and write lock
            /// </summary>
            /// <exception cref="System.TimeoutException"/>
            public WriteLock Write(int timeout) { return WriteLock.Acquire(this, timeout); }

            /// <summary>
            /// Returns a read and write lock
            /// </summary>
            public WriteLock Write() { return WriteLock.Acquire(this, -1); }

            #endregion
        }
    }
}