#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.Synchronization
{
    /// <summary>
    /// Creates a lock on a mutex that can be released via the Dispose() method, for use in a using statement
    /// </summary>
    public class MutexLock : IDisposable
    {
        readonly Mutex _mutex;
        readonly bool _wasNew, _wasAbandonded;
        bool _locked, _disposeMutex;

        /// <summary> Creates and locks the named mutex </summary>
        public MutexLock(string name) : this(Timeout.Infinite, name) { }
        /// <summary> Creates and locks the named mutex </summary>
        public MutexLock(string format, params object[] args) : this(Timeout.Infinite, String.Format(format, args)) { }
        /// <summary> Creates and locks the named mutex or throws TimeoutException </summary>
        /// <exception cref="System.TimeoutException"> Raises System.TimeoutException if mutex was not obtained. </exception>
        public MutexLock(int timeout, string format, params object[] args) : this(timeout, String.Format(format, args)) { }
        /// <summary> Creates and locks the named mutex or throws TimeoutException </summary>
        /// <exception cref="System.TimeoutException"> Raises System.TimeoutException if mutex was not obtained. </exception>
        public MutexLock(int timeout, string name)
        {
            _mutex = new Mutex(true, name, out _wasNew);
            _locked = _wasNew;
            _disposeMutex = true;
            Lock(timeout, ref _wasAbandonded);
        }

        /// <summary> Locks the provided mutex </summary>
        public MutexLock(Mutex mutex) : this(Timeout.Infinite, mutex) { }
        /// <summary> Locks the provided mutex or throws TimeoutException </summary>
        /// <exception cref="System.TimeoutException"> Raises System.TimeoutException if mutex was not obtained. </exception>
        public MutexLock(int timeout, Mutex mutex)
        {
            _wasNew = false;
            _mutex = Check.NotNull(mutex);
            _disposeMutex = false;
            Lock(timeout, ref _wasAbandonded);
        }

        /// <summary> Returns the mutex </summary>
        public Mutex MutexHandle { get { return _mutex; } }
        /// <summary> Returns true if this object holds a lock on the mutex </summary>
        public bool IsLocked { get { return _locked; } }
        /// <summary> Returns true if this object created a new named mutex </summary>
        public bool WasNew { get { return _wasNew; } }
        /// <summary> Returns true if the lock was obtained from an abandoned mutex </summary>
        public bool WasAbandonded { get { return _wasAbandonded; } }

        private void Lock(int timeout, ref bool wasAbandoned)
        {
            if (!_locked)
            {
                wasAbandoned = false;
                try
                {
                    _locked = _mutex.WaitOne(timeout, false);
                    if (!_locked) throw new TimeoutException();
                }
                catch (AbandonedMutexException) 
                { _locked = true; wasAbandoned = true; }
            }
        }

        /// <summary> Releases the lock on the mutex, and if created by this closes the mutex </summary>
        public void Dispose()
        {
            if (_locked)
            {
                _mutex.ReleaseMutex();
                _locked = false;
            }
            if (_disposeMutex)
            {
                _mutex.Close();
                _disposeMutex = false;
            }
        }
    }
}
