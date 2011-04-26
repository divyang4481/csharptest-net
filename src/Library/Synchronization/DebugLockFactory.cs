#region Copyright 2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.Synchronization
{
    /// <summary>
    /// Creates a debugging lock factory that can track locks allocated and all acquired/released read/write locks
    /// </summary>
    public class DebugLockFactory<T> : DebugLockFactory
        where T: ILockStrategy, new()
    {
        /// <summary> Constructs the lock tracking factory </summary>
        public DebugLockFactory() : base(new LockFactory<T>())
        { }
        /// <summary> Constructs the lock tracking factory </summary>
        public DebugLockFactory(bool captureStack, int limitTimeout, int limitNestedReaders, bool concurrentReads, int limitNestedWriters)
            : base(new LockFactory<T>(), captureStack, limitTimeout, limitNestedReaders, concurrentReads, limitNestedWriters)
        { }
    }

    /// <summary>
    /// Creates a debugging lock factory that can track locks allocated and all acquired/released read/write locks
    /// </summary>
    public class DebugLockFactory : LockCounterFactory
    {
        class Counts { public int Read, Write; }
        [ThreadStatic]
        static Dictionary<DebugLockFactory, Counts> _threadCounts;
        
        private bool _captureStack;
        private int _limitTimeout;
        private int _limitNestedReaders;
        private bool _concurrentReads;
        private int _limitNestedWriters;
        
        /// <summary> Constructs the lock tracking factory </summary>
        public DebugLockFactory(ILockFactory factory) : this(factory, false, 30000, 0, false, 0)
        { }
        /// <summary> Constructs the lock tracking factory </summary>
        public DebugLockFactory(ILockFactory factory, bool captureStack, int limitTimeout, int limitNestedReaders, bool concurrentReads, int limitNestedWriters)
            : base(factory)
        {
            _captureStack = captureStack;
            _limitTimeout = limitTimeout;
            _limitNestedReaders = limitNestedReaders;
            _concurrentReads = concurrentReads;
            _limitNestedWriters = limitNestedWriters;
        }

        /// <summary> Constructs the lock wrapped in a DebugLocking instance </summary>
        public override ILockStrategy Create()
        {
            DebugLocking l = new DebugLocking(base.Create(), _captureStack, _limitTimeout, _limitNestedReaders, true, _limitNestedWriters);
            return new DebugLockCounting(this, l);
        }

        /// <summary> Toggle if the entire stack is captured on lock aquisition/release for newly created locks </summary>
        public bool CaptureStack { get { return _captureStack; } set { _captureStack = value; } }
        /// <summary> Toggle if reads are allowed even if write lock was acquired </summary>
        public bool ConcurrentReads { get { return _concurrentReads; } set { _concurrentReads = value; } }
        /// <summary> Timeout limit for newly created locks </summary>
        public int LimitTimeout { get { return _limitTimeout; } set { _limitTimeout = Check.InRange(value, -1, int.MaxValue); } }
        /// <summary> Reader nesting limit for newly created locks </summary>
        public int LimitNestedReaders { get { return _limitNestedReaders; } set { _limitNestedReaders = Check.InRange(value, 0, 64); } }
        /// <summary> Writer nesting limit for newly created locks </summary>
        public int LimitNestedWriters { get { return _limitNestedWriters; } set { _limitNestedWriters = Check.InRange(value, 0, 64); } }

        /// <summary> Returns the total number of current readers for this thread </summary>
        public int LocalReaderCount
        {
            get
            { 
                Counts counts;
                return _threadCounts != null && _threadCounts.TryGetValue(this, out counts) ? counts.Read : 0;
            }
        }

        /// <summary> Returns the total number of current writers for this thread </summary>
        public int LocalWriterCount
        {
            get
            {
                Counts counts;
                return _threadCounts != null && _threadCounts.TryGetValue(this, out counts) ? counts.Read : 0;
            }
        }

        /// <summary> Asserts that none of the locks handed out are currently locked for read or write by this thread </summary>
        public void LocalAssertNoLocks()
        {
            DebugAssertionFailedException.Assert(LocalWriterCount == 0, "The current thread is still writing.");
            DebugAssertionFailedException.Assert(LocalReaderCount == 0, "The current thread is still reading.");
        }

        class DebugLockCounting : ILockStrategy
        {
            readonly DebugLockFactory _factory;
            readonly ILockStrategy _lock;

            public DebugLockCounting(DebugLockFactory factory, ILockStrategy lck) 
            { 
                _factory = factory;
                _lock = lck;
            }
            public void Dispose() { _lock.Dispose(); }

            public int WriteVersion { get { return _lock.WriteVersion; } }

            private void AddThreadCount(int read, int write)
            {
                Counts counts;
                if (_threadCounts == null) _threadCounts = new Dictionary<DebugLockFactory, Counts>();
                if (!_threadCounts.TryGetValue(_factory, out counts))
                    _threadCounts.Add(_factory, counts = new Counts());
                counts.Read += read;
                counts.Write += write;
            }

            public bool TryRead(int timeout)
            {
                if (!_lock.TryRead(timeout)) return false;
                AddThreadCount(1, 0);
                return true;
            }

            public void ReleaseRead()
            {
                _lock.ReleaseRead();
                AddThreadCount(-1, 0);
            }

            public bool TryWrite(int timeout)
            {
                if (!_lock.TryWrite(timeout)) return false;
                AddThreadCount(0, 1);
                return true;
            }

            public void ReleaseWrite()
            {
                _lock.ReleaseWrite();
                AddThreadCount(0, -1);
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