#region Copyright 2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using CSharpTest.Net.Bases;

namespace CSharpTest.Net.IO
{
    /// <summary>
    /// Provides a single-threaded writer to a stream
    /// </summary>
    public class BackgroundWriter : Disposable
    {
        // The reason everything is contained in the WorkerState class is so that we can fall
        // out of scope and properly dispose of the running thread and stream if someone forgot
        // to call the dispose/close method.  If the worker thread had a reference to us, then
        // this class would live forever and so would the thread and stream.
        private readonly WorkerState _state;
        private readonly ManualResetEvent _flush;
        private readonly Action<Stream> _flushAsync;

        /// <summary>
        /// Create the writer and thread
        /// </summary>
        public BackgroundWriter(Stream stream) : this(stream, true) { }
        /// <summary>
        /// Create the writer and thread
        /// </summary>
        public BackgroundWriter(Stream stream, bool closeStream)
        {
            _flush = new ManualResetEvent(false);
            _flushAsync = s => s.Flush();
            _state = new WorkerState(stream, closeStream);
        }

        /// <summary> Closes the worker thread </summary>
        protected override void Dispose(bool disposing)
        {
            _state.Stop();
            lock (_flush)
                _flush.Close();
        }

        /// <summary>
        /// Stops the worker thread after completing the pending writes.
        /// </summary>
        public void Close()
        { Dispose(); }

        /// <summary>
        /// Enqueues a flush command and returns immediately
        /// </summary>
        public void BeginFlush()
        {
            Perform(_flushAsync);
        }

        /// <summary>
        /// Waits for all pending writes and flushes the stream prior to returning
        /// </summary>
        public void Flush()
        {
            lock(_flush)
            {
                _flush.Reset();
                Perform(_flushAsync, _flush);
                _flush.WaitOne();
            }
        }

        /// <summary>
        /// Perform an action on the worker thread with the stream
        /// </summary>
        public void Perform(Action<Stream> ioAction)
        {
            _state.Enqueue(new IoTask { IoAction = ioAction });
        }

        /// <summary>
        /// Perform an action on the worker thread with the stream and sets the signal
        /// </summary>
        public void Perform(Action<Stream> ioAction, EventWaitHandle signal)
        {
            _state.Enqueue(new IoTask { IoAction = ioAction, Signal = signal });
        }

        /// <summary>
        /// Write a series of bytes to the stream at the current position
        /// </summary>
        public void Write(byte[] buffer, int offset, int length)
        {
            Write(-1, buffer, offset, length, null);
        }

        /// <summary>
        /// Write a series of bytes to the stream at the current position and sets the signal
        /// </summary>
        public void Write(byte[] buffer, int offset, int length, EventWaitHandle signal)
        {
            Write(-1, buffer, offset, length, signal);
        }

        /// <summary>
        /// Write a series of bytes to the stream at the specified position
        /// </summary>
        public void Write(long position, byte[] buffer, int offset, int length)
        {
            Write(position, buffer, offset, length, null);
        }

        /// <summary>
        /// Write a series of bytes to the stream at the specified position and sets the signal
        /// </summary>
        public void Write(long position, byte[] buffer, int offset, int length, EventWaitHandle signal)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (length < 0 || offset + length > buffer.Length)
                throw new ArgumentOutOfRangeException("length");
            if (length > 0)
                _state.Enqueue(new IoTask { Position = position, Bytes = buffer, Offset = offset, Length = length, Signal = signal });
        }

        /// <summary>
        /// Returns a number of bytes up to length that is pending a write at the position specified and
        /// copies those bytes into buffer the offset provided.
        /// </summary>
        public int Read(long position, byte[] buffer, int offset, int length)
        {
            IoTask work = _state.First();
            int bytesFound = 0;
            while (work != null)
            {
                if (work.Position == position)
                {
                    Buffer.BlockCopy(work.Bytes, work.Offset, buffer, offset, bytesFound = Math.Min(length, work.Length));
                }
                work = work.Next;
            }

            return bytesFound;
        }

        class IoTask
        {
            public byte[] Bytes;
            public int Offset, Length;
            public long Position = -1;
            public Action<Stream> IoAction;
            public EventWaitHandle Signal;

            public IoTask Next;
        }

        class WorkerState
        {
            private const int MemoryLimit = 0x080000;

            IoTask _first;
            IoTask _last;
            private bool _disposed;
            private int _lagging; //keeps track of the worst lag volume
            private readonly Stream _output;
            private readonly bool _closeStream;
            private readonly Thread _worker;
            private readonly ManualResetEvent _wakeup;
            private readonly ManualResetEvent _stop;

            public WorkerState(Stream output, bool closeStream)
            {
                _output = output;
                _closeStream = closeStream;
                _first = _last = new IoTask();
                _wakeup = new ManualResetEvent(false);
                _stop = new ManualResetEvent(false);

                _worker = new Thread(WriterThread);
                _worker.IsBackground = true;
                _worker.SetApartmentState(ApartmentState.MTA);
                _worker.Name = GetType().Name;
                _worker.Start();
            }

            public void Stop()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _stop.Set();
                    _worker.Join();

                    _stop.Close();
                    _wakeup.Close();
                }
            }

            void WriterThread()
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    WaitHandle[] waits = new WaitHandle[] { _stop, _wakeup };
                    while (true)
                    {
                        int result = WaitHandle.WaitAny(waits);
                        _wakeup.Reset();
                        while (PerformWrite(ref buffer))
                        { }

                        if (result != 1)
                            break;
                    }

                    _output.Flush();
                    // Console.WriteLine("Thread {0} write lag = {1}", Thread.CurrentThread.ManagedThreadId, _lagging);
                    Debug.Write(
                        String.Format("Thread {0} write lag = {1}", Thread.CurrentThread.ManagedThreadId, _lagging),
                        GetType().Name
                        );
                }
                catch (ThreadAbortException) { throw; }
                catch
                {
                    _first = null;
                    Interlocked.Exchange(ref _last, null);
                }
                finally
                {
                    if (_closeStream)
                        _output.Close();
                }
            }

            private bool PerformWrite(ref byte[] buffer)
            {
                IoTask start = _first;
                IoTask next = Interlocked.CompareExchange(ref start.Next, null, null);
                if (next == null)
                    return false;//nothing to do, _first has always been processed

                IoTask stop = start = next;

                bool hasSignals = start.Signal != null;
                int byteLen = stop.Length;
                long startpos = stop.Position;
                long position = stop.Position + byteLen;

                while (null != (next = Interlocked.CompareExchange(ref start.Next, null, null)))
                {
                    //see if both are append-only (position < 0)
                    if (startpos < 0 && next.Position >= 0)
                        break;
                    //see if the next write immediately follows this
                    if (startpos >= 0 && next.Position != position)
                        break;
                    //see if this write will overflow our max memory buffer limit
                    if (next.Length + byteLen > MemoryLimit)
                        break;
                    if (next.IoAction != null)
                        break;

                    byteLen += next.Length;
                    position += next.Length;
                    hasSignals |= next.Signal != null;
                    stop = next;
                }

                if (start.IoAction != null)
                {
                    start.IoAction(_output);
                    start.IoAction = null;
                }
                else if (ReferenceEquals(start, stop))
                {
                    if (startpos >= 0)
                        _output.Position = startpos;

                    _output.Write(start.Bytes, start.Offset, start.Length);
                }
                else //buffer and write multiple items...
                {
                    if (buffer.Length < byteLen)
                        Array.Resize(ref buffer, byteLen + 8192);

                    int counter = 0;
                    int offset = 0;
                    IoTask current = start;
                    while (true)
                    {
                        counter++;
                        Buffer.BlockCopy(current.Bytes, current.Offset, buffer, offset, current.Length);
                        offset += current.Length;

                        if (ReferenceEquals(current, stop))
                            break;
                        current = current.Next;
                    }

                    _lagging = Math.Max(_lagging, counter);

                    if (startpos >= 0)
                        _output.Position = startpos;

                    _output.Write(buffer, 0, offset);
                }

                while (hasSignals)
                {
                    if (start.Signal != null)
                    {
                        start.Signal.Set();
                        start.Signal = null;
                    }
                    if (ReferenceEquals(start, stop))
                        break;
                    start = start.Next;
                }

                _first = stop;
                return true;
            }

            public void Enqueue(IoTask task)
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().FullName);

                IoTask last = _last, newLast;
                while (last != null && !ReferenceEquals(last, newLast = Interlocked.CompareExchange(ref _last, task, last)))
                    last = newLast;

                if (last == null)
                    throw new IOException();

                IoTask prev = Interlocked.Exchange(ref last.Next, task);
                if (prev != null)
                    throw new IOException();

                _wakeup.Set();
            }

            public IoTask First()
            {
                return _first;
            }
        }
    }
}
