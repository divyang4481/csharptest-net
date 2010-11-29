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

namespace CSharpTest.Net.Threading
{
    /// <summary>
    /// Provides a counter that fires a delegate on first usage and last release.  For the counts
    /// to be maintained someone must hold an instance of one or more of these objects.
    /// </summary>
    public class UsageCounter : IDisposable
    {
        private const int MaxCount = int.MaxValue;
        private const int Timeout = 120000;

        int _myCount;
        readonly string _name;
        readonly Mutex _lock;
        readonly Semaphore _count;

        /// <summary> Creates a composite name with the format and arguments specified </summary>
        public UsageCounter(string nameFormat, params object[] arguments)
            : this(String.Format(nameFormat, arguments))
        { }

        /// <summary> The name used for the global object </summary>
        public UsageCounter(string name)
        {
            _myCount = 0;
            _name = name;
            _lock = new Mutex(false, name + ".Lock");
            _count = new Semaphore(MaxCount, MaxCount, name + ".Count");
        }

        /// <summary> Releases the resources but does not decrement counts </summary>
        public void Dispose()
        {
            _count.Close();
            _lock.Close();
        }

        /// <summary> Returns the name specified when this instance was created </summary>
        public string Name { get { return _name; } }

        /// <summary> Returns the number of times Increment() has been called on this instance </summary>
        public int InstanceCount { get { return _myCount; } }

        /// <summary> Calls the provided delegate inside lock with the current count value </summary>
        public void TotalCount(Action<int> action)
        {
            Check.NotNull(action);

            if (!_lock.WaitOne(Timeout, false))
                throw new TimeoutException();
            try
            {
                if (!_count.WaitOne(Timeout, false))
                    throw new TimeoutException();
                int counter = 1 + _count.Release();
                action(MaxCount - counter);
            }
            finally
            {
                _lock.ReleaseMutex();
            }
        }

        /// <summary> Increments the counter by one </summary>
        public void Increment()
        { Increment(null); }

        #region Increment<T>(Action<T> e, T arg)

        private class TAction<T>
        {
            readonly Action<T> e;
            readonly T arg;

            public TAction(Action<T> e, T arg)
            { this.e = e; this.arg = arg; }
            public void Fire()
            { e(arg); }
        }

        /// <summary> Delegate fired inside lock if this is the first Increment() call on the name provided </summary>
        public void Increment<T>(Action<T> e, T arg)
        { Increment(new TAction<T>(e, arg).Fire); }

        #endregion

        /// <summary> Delegate fired inside lock if this is the first Increment() call on the name provided </summary>
        public void Increment(ThreadStart beginUsage)
        {
            if (!_lock.WaitOne(Timeout, false))
                throw new TimeoutException();
            try
            {
                if (!_count.WaitOne(Timeout, false))
                    throw new TimeoutException();

                if (!_count.WaitOne(Timeout, false))
                {
                    _count.Release();
                    throw new TimeoutException();
                }

                _myCount++;
                int counter = 1 + _count.Release();

                //if this is the first call
                if (beginUsage != null && counter == (MaxCount - 1))
                    beginUsage();
            }
            finally
            {
                _lock.ReleaseMutex();
            }
        }

        /// <summary> Decrements the counter by one </summary>
        public void Decrement()
        { Decrement(null); }

        /// <summary> Delegate fired inside lock if the Decrement() count reaches zero </summary>
        public void Decrement<T>(Action<T> e, T arg)
        { Decrement(new TAction<T>(e, arg).Fire); }

        /// <summary> Delegate fired inside lock if the Decrement() count reaches zero </summary>
        public void Decrement(ThreadStart endUsage)
        {
            if (!_lock.WaitOne(Timeout, false))
                throw new TimeoutException();
            try
            {
                _myCount--;
                int counter = 1 + _count.Release();

                //if this is the last decrement expected
                if (endUsage != null && counter == MaxCount)
                    endUsage();
            }
            finally
            {
                _lock.ReleaseMutex();
            }
        }
    }
}
