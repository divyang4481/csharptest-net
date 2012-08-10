#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
#define SUPPORT_RECURSION
using System;
using System.Threading;

namespace CSharpTest.Net.Synchronization
{
    /// <summary>
    /// provides a simple and fast, writer only lock, request for read immediatly return true.
    /// </summary>
    public class ReservedWriteLocking<T> : ReservedWriteLocking
        where T : ILockStrategy, new()
    {
        /// <summary> Constructs the reader-writer lock using a new T() </summary>
        public ReservedWriteLocking() : base(new T())
        { }
    }

    /// <summary>
    /// provides a simple and fast, writer only lock, request for read immediatly return true.
    /// </summary>
    public class ReservedWriteLocking : ILockStrategy
    {
        /// <summary> The syncronization object writers and potential readers use to lock </summary>
        object _sync;
        /// <summary> The underlying lock that will be acquired directly by Read() and after the 2nd call to Write() </summary>
        ILockStrategy _lock;
        /// <summary> The current count of the calls to Write() </summary>
        int _writeCount;

        /// <summary>
        /// Constructs the reader-writer lock using the lock provided
        /// </summary>
        public ReservedWriteLocking(ILockStrategy lck)
        {
            _sync = this;
            _lock = Check.NotNull(lck);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            object exit = Interlocked.Exchange(ref _sync, null);
            if (exit == null)
                return;

            _lock.Dispose();
            Check.Assert<InvalidOperationException>(_writeCount == 0);
        }
        
        /// <summary> Changes every time a write lock is aquired.  If WriteVersion == 0, no write locks have been issued. </summary>
        public int WriteVersion { get { return _lock.WriteVersion; } }

        /// <summary>
        /// Returns true if the lock was successfully obtained within the timeout specified
        /// </summary>
        public bool TryRead(int millisecondsTimeout)
        {
            return _lock.TryRead(millisecondsTimeout);
        }

        /// <summary>
        /// Releases a read lock
        /// </summary>
        public void ReleaseRead()
        {
            _lock.ReleaseRead();
        }

        /// <summary>
        /// The first call reserves the Write lock for the current thread but does not stop reader access
        /// until the write lock is acquired again.
        /// </summary>
        public virtual bool TryWrite(int millisecondsTimeout)
        {
            if (_sync == null) throw new ObjectDisposedException(GetType().FullName);
            // First obtain the 'writer lock':

            if (!Monitor.TryEnter(_sync, millisecondsTimeout))
                return false;
            if (_writeCount == 0)
            {
                _writeCount++;
                return true;
            }
            if (_lock.TryWrite(millisecondsTimeout))
            {
                _writeCount++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Releases a writer lock
        /// </summary>
        public void ReleaseWrite()
        {
            Check.Assert<InvalidOperationException>(_writeCount > 0);
            _writeCount--;

            if (_writeCount > 0)
                _lock.ReleaseWrite();

            Monitor.Exit(_sync);
        }

        /// <summary>
        /// Returns a reader lock that can be elevated to a write lock
        /// </summary>
        public ReadLock Read() { return ReadLock.Acquire(this, -1); }

        /// <summary>
        /// Returns a reader lock that can be elevated to a write lock
        /// </summary>
        /// <exception cref="System.TimeoutException"/>
        public ReadLock Read(int millisecondsTimeout) { return ReadLock.Acquire(this, millisecondsTimeout); }

        /// <summary>
        /// Returns a read and write lock
        /// </summary>
        public WriteLock Write() { return WriteLock.Acquire(this, -1); }

        /// <summary>
        /// Returns a read and write lock
        /// </summary>
        /// <exception cref="System.TimeoutException"/>
        public WriteLock Write(int millisecondsTimeout) { return WriteLock.Acquire(this, millisecondsTimeout); }
    }
}
