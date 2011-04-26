#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using System.Threading;

namespace CSharpTest.Net.Threading
{
    /// <summary>
    /// Represents a single worker thread that processes IWaitAndContinue work items
    /// </summary>
    public class WaitAndContinueWorker : IDisposable
    {
        readonly Thread _worker;
        readonly WorkerControl _control;
        readonly WaitAndContinueList _work;

        /// <summary> Raised when an uncaught exception is thrown while processing the work queue </summary>
        public event ErrorEventHandler OnError;

        /// <summary> Constructs a thread to process IWaitAndContinue work items </summary>
        public WaitAndContinueWorker()
        {
            _control = new WorkerControl();
            _work = new WaitAndContinueList();
            _work.AddWork(_control);

            _worker = new Thread(Run);
            _worker.SetApartmentState(ApartmentState.MTA);
            _worker.IsBackground = true;
            _worker.Name = GetType().Name;
            _worker.Start();
        }

        /// <summary> Returns true if the work queue is empty </summary>
        public bool IsEmpty { get { return _control.HasQuit || _work.Count <= 1; } }

        /// <summary> Adds a unit of work to the list </summary>
        public void AddWork(IWaitAndContinue item)
        {
            if (_control.HasQuit) throw new ObjectDisposedException(GetType().FullName);
            _work.AddWork(item);
            _control.Modified();
        }
        /// <summary> Adds a unit of work to the list </summary>
        public void AddWork(WaitAndContinueList list)
        {
            if (_control.HasQuit) throw new ObjectDisposedException(GetType().FullName);
            _work.AddWork(list);
            _control.Modified();
        }

        /// <summary>
        /// Exits the worker thread and, if complete is true, waits for the remaining 
        /// tasks to complete
        /// </summary>
        public bool Complete(bool complete, int timeout)
        {
            if (!_control.HasQuit)
            {
                _control.Quit();
                if (!_worker.Join(timeout <= 0 ? timeout : Math.Max(1000, timeout)))
                {
                    _worker.Abort();
                    _worker.Join();
                    complete = false;
                }
            }

            _control.Dispose();
            try
            {
                while (complete && !_work.IsEmpty && _work.PerformWork(timeout))
                { }
            }
            finally
            {
                _work.Dispose();
            }
            return complete;
        }

        /// <summary>
        /// Terminates all work by aborting the worker thread even if work is in progress
        /// </summary>
        public void Abort()
        {
            if (_worker.IsAlive)
            {
                _control.Quit();
                if (!_worker.Join(100))
                    _worker.Abort();
                _worker.Join();
            }
            _control.Dispose();
            _work.Dispose();
        }

        /// <summary> Disposes of the worker thread and all pending work </summary>
        public void Dispose()
        {
            Complete(false, 0);
        }

        void Run()
        {
            while (!_control.HasQuit)
            {
                try
                {
                    IWaitAndContinue ignore;
                    _work.PerformWork(Timeout.Infinite, out ignore);
                }
                catch (ThreadAbortException) { return; }
                catch (Exception ex)
                {
                    ErrorEventHandler h = OnError;
                    if (h != null)
                        h(this, new ErrorEventArgs(ex));
                }
            }
        }

        class WorkerControl : IWaitAndContinue
        {
            readonly ManualResetEvent _quit = new ManualResetEvent(false);
            readonly AutoResetEvent _modified = new AutoResetEvent(false);

            byte _hasQuit;
            bool _disposed;
            
            public void Dispose()
            {
                _hasQuit = 1;
                _disposed = true;
                _quit.Close();
                _modified.Close();
            }

            public bool HasQuit { get { return _hasQuit == 1; } }

            public void Quit()
            {
                Thread.VolatileWrite(ref _hasQuit, 1);
                try { if (!_disposed) _quit.Set(); }
                catch (ObjectDisposedException)
                { return; }
            }

            public void Modified()
            {
                try { if(!_disposed) _modified.Set(); }
                catch (ObjectDisposedException)
                { return; }
            }

            bool IWaitAndContinue.Completed { get { return _disposed; } }
            int IWaitAndContinue.HandleCount { get { return 2; } }

            void IWaitAndContinue.CopyHandles(WaitHandle[] array, int offset)
            {
                array[offset] = _quit;
                array[offset + 1] = _modified;
            }

            void IWaitAndContinue.ContinueProcessing(WaitHandle handleSignaled)
            { }
        }
    }
}
