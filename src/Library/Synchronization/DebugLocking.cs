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
using System.Diagnostics;
using System.Threading;

namespace CSharpTest.Net.Synchronization
{
    /// <summary>
    /// Creates a tracking/assertion wrapper around an implementation of an ILockStrategy to verify lock state before 
    /// and after acquisition and release of both reader and writer locks.
    /// </summary>
    public class DebugLocking<T> : DebugLocking
        where T : ILockStrategy, new()
    {
        /// <summary> Constructs the lock tracking object </summary>
        public DebugLocking() : base(new T())
        { }
        /// <summary> Constructs the lock tracking object </summary>
        public DebugLocking(bool captureStack, int limitTimeout, int limitNestedReaders, bool concurrentReads, int limitNestedWriters)
            : base(new T(), captureStack, limitTimeout, limitNestedReaders, concurrentReads, limitNestedWriters)
        { }
    }

    /// <summary>
    /// Creates a tracking/assertion wrapper around an implementation of an ILockStrategy to verify lock state before 
    /// and after acquisition and release of both reader and writer locks.
    /// </summary>
    public class DebugLocking : ILockStrategy
    {
        bool _disposed;

        readonly bool _captureStack;
        readonly int _limitTimeout;
        readonly int _limitNestedReaders;
        readonly bool _concurrentReads;
        readonly int _limitNestedWriters;

        readonly ILockStrategy _lock;

        DebugLockTracker _writer;
        readonly Dictionary<Thread, DebugLockTracker> _readers;

        /// <summary> Constructs the lock tracking object </summary>
        public DebugLocking(ILockStrategy lck)
            : this(lck, false, 30000, 0, false, 0)
        { }
        /// <summary> Constructs the lock tracking object </summary>
        public DebugLocking(ILockStrategy lck, bool captureStack, int limitTimeout, int limitNestedReaders, bool concurrentReads, int limitNestedWriters)
        {
            _captureStack = captureStack;
            _limitTimeout = limitTimeout < 0 ? int.MaxValue : limitTimeout;
            _limitNestedReaders = limitNestedReaders;
            _concurrentReads = concurrentReads;
            _limitNestedWriters = limitNestedWriters;
            _lock = Check.NotNull(lck);
            _readers = new Dictionary<Thread, DebugLockTracker>();
        }

        /// <summary> Capture the stack on every lock aquisition and release </summary>
        public bool CaptureStack { get { return _captureStack; } }

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

        /// <summary> Returns the total number of current readers for this thread </summary>
        public int LocalReaderCount 
        {
            get
            {
                DebugLockTracker reader;
                lock (_readers)
                    return _readers.TryGetValue(Thread.CurrentThread, out reader) ? reader.Count : 0;
            }
        }

        /// <summary> Returns the total number of current writers for this thread </summary>
        public int LocalWriterCount 
        {
            get 
            {
                DebugLockTracker writer = _writer;
                return writer != null && ReferenceEquals(writer.Owner, Thread.CurrentThread) ? writer.Count : 0;
            }
        }

        /// <summary> Changes every time a write lock is aquired.  If WriteVersion == 0, no write locks have been issued. </summary>
        public int WriteVersion { get { return _lock.WriteVersion; } }

        /// <summary> Disposes of this lock </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _lock.Dispose();

                DebugAssertionFailedException.Assert(CurrentReaderCount == 0, "Lock disposed with active readers.");
                DebugAssertionFailedException.Assert(CurrentWriterCount == 0, "Lock disposed with active writers.");
            }
        }

        private void AssertValid()
        {
            if (_disposed)
                throw new ObjectDisposedException(String.Format("{0}({1})", GetType(), _lock.GetType()));
        }

        private int MaxTimeout(int timeout) { return Math.Min(_limitTimeout, timeout < 0 ? int.MaxValue : timeout); }

        private static void AddCount(ref int maxValue, ref int currentValue, ref int totalValue)
        {
            Interlocked.Increment(ref totalValue);
            int newMax = Interlocked.Increment(ref currentValue);
            int oldMax;
            while (newMax > (oldMax = maxValue))
                Interlocked.CompareExchange(ref maxValue, newMax, oldMax);
        }

        /// <summary>
        /// Returns true if the lock was successfully obtained within the timeout specified
        /// </summary>
        public bool TryRead(int timeout)
        {
            AssertValid();
            Thread thread = Thread.CurrentThread;
            int myWriteCount = 0;
            DebugLockTracker reader, writer = _writer;

            lock (_readers)
                if (!_readers.TryGetValue(thread, out reader))
                    _readers.Add(thread, reader = new DebugLockTracker(thread));
            int myReadCount = reader.Count;

            if (writer != null && ReferenceEquals(writer.Owner, thread))
                myWriteCount = writer.Count;

            DebugAssertionFailedException.Assert((myReadCount + myWriteCount) <= _limitNestedReaders, "Current thread already holds max read locks.");

            if (!_lock.TryRead(MaxTimeout(timeout)))
            {
                DebugAssertionFailedException.Assert(timeout < _limitTimeout, "Possible dead-lock in read lock, timeout limit reached.");
                return false;
            }

            writer = _writer;
            DebugAssertionFailedException.Assert(_concurrentReads || (writer == null || myWriteCount > 0), "Read lock acquired while writer lock exists.");

            reader.AddLock(CaptureStack);
            AddCount(ref MaxReaderCount, ref CurrentReaderCount, ref TotalReaderCount);
            return true;
        }

        /// <summary>
        /// Releases a read lock
        /// </summary>
        public void ReleaseRead()
        {
            AssertValid();
            Thread thread = Thread.CurrentThread;
            DebugLockTracker reader;

            lock (_readers)
                DebugAssertionFailedException.Assert(_readers.TryGetValue(thread, out reader) && reader.Count > 0, "Unable to release an unacquired read lock.");

            DebugLockTracker writer = _writer;
            DebugAssertionFailedException.Assert(_concurrentReads || writer == null || ReferenceEquals(writer.Owner, thread), "Read lock release while writer lock exists.");

            _lock.ReleaseRead();
            reader.ReleaseLock(CaptureStack);
            Interlocked.Decrement(ref CurrentReaderCount);
        }

        /// <summary>
        /// Returns true if the lock was successfully obtained within the timeout specified
        /// </summary>
        public bool TryWrite(int timeout)
        {
            AssertValid();
            Thread thread = Thread.CurrentThread;
            int myWriteCount = 0, myReadCount = 0;
            DebugLockTracker reader, writer = _writer;

            lock (_readers)
                if (_readers.TryGetValue(thread, out reader))
                    myReadCount = reader.Count;

            if (writer != null && ReferenceEquals(writer.Owner, thread))
                myWriteCount = writer.Count;

            DebugAssertionFailedException.Assert(myWriteCount > 0 || myReadCount == 0, "Potential dead-lock in acquire writer while reading.");
            DebugAssertionFailedException.Assert(myWriteCount <= _limitNestedWriters, "Current thread already holds max write locks.");

            if (!_lock.TryWrite(MaxTimeout(timeout)))
            {
                DebugAssertionFailedException.Assert(timeout < _limitTimeout, "Possible dead-lock in write lock, timeout limit reached.");
                return false;
            }

            DebugAssertionFailedException.Assert(myWriteCount > 0 || _writer == null, "Write lock acquired while write lock exists.");
            DebugAssertionFailedException.Assert(_concurrentReads || myReadCount == CurrentReaderCount, "Write lock acquired while reader lock exists.");

            _writer = writer = _writer ?? new DebugLockTracker(thread);

            writer.AddLock(CaptureStack);
            AddCount(ref MaxWriterCount, ref CurrentWriterCount, ref TotalWriterCount);
            return true;
        }

        /// <summary>
        /// Releases a writer lock
        /// </summary>
        public void ReleaseWrite()
        {
            AssertValid();
            Thread thread = Thread.CurrentThread;

            DebugLockTracker writer = _writer;
            DebugAssertionFailedException.Assert(writer != null && ReferenceEquals(writer.Owner, thread) && writer.Count > 0, "Unable to release an unacquired write lock.");

            if (writer != null)
            {
                writer.ReleaseLock(CaptureStack);
                if(writer.Count == 0)
                    _writer = null;
            }
            _lock.ReleaseWrite();
            Interlocked.Decrement(ref CurrentWriterCount);
        }

        class DebugLockTracker
        {
            const int MaxDepth = 64;

            public int Count;
            public readonly Thread Owner;
            readonly StackTrace[] _acquiredFrom;
            readonly StackTrace[] _releasedFrom;

            public DebugLockTracker(Thread owner)
            {
                Owner = owner;
                Count = 0;
                _acquiredFrom = new StackTrace[MaxDepth];
                _releasedFrom = new StackTrace[MaxDepth];
            }

            public void AddLock(bool captureStack)
            {
                _acquiredFrom[Count] = captureStack ? new StackTrace(2, false) : null;
                _releasedFrom[Count] = null;
                Count++;
            }

            public void ReleaseLock(bool captureStack)
            {
                Count--;
                _releasedFrom[Count] = captureStack ? new StackTrace(2, false) : null;
            }
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
