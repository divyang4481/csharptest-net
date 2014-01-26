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
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;

namespace CSharpTest.Net.IO
{
    /// <summary>
    /// Hosts an TcpListener on a dedicated set of worker threads, providing a clean shutdown
    /// on dispose.
    /// </summary>
    public class TcpServer : IDisposable
    {
        private TcpListener _listener;
        private Thread _listenerThread;
        private readonly Thread[] _workers;
        private readonly ManualResetEvent _stop, _ready;
        private readonly List<TcpClientConnection> _clients;
        private readonly Queue<TcpClientConnection> _queue; 

        /// <summary>
        /// Constructs the TcpServer with a fixed thread-pool size.
        /// </summary>
        public TcpServer(int maxThreads)
        {
            _workers = new Thread[maxThreads];
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _clients = new List<TcpClientConnection>();
            _queue = new Queue<TcpClientConnection>();
            _listener = null;
            _listenerThread = null;
        }

        /// <summary>
        /// Exposes a WaitHandle that can be used to signal other threads that the server is shutting down.
        /// </summary>
        public WaitHandle ShutdownEvent
        {
            get { return _stop; }
        }

        /// <summary>
        /// Raised when an unhandled exception occurs.
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError;

        /// <summary>
        /// Performs the processing of the request on one of the worker threads
        /// </summary>
        public event EventHandler<TcpClientEventArgs> OnDataRecieved;

        /// <summary>
        /// Performs the processing of the request on one of the worker threads
        /// </summary>
        public event EventHandler<TcpClientEventArgs> OnClientConnect;

        /// <summary>
        /// Performs the processing of the request on one of the worker threads
        /// </summary>
        public event EventHandler<TcpClientEventArgs> OnClientClosed;

        /// <summary>
        /// </summary>
        public void Start(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
            _listener.Start();
            _listenerThread = new Thread(HandleRequests);
            _listenerThread.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Stop(); 
        }

        /// <summary>
        /// Stops the server and all worker threads.
        /// </summary>
        public void Stop()
        {
            var listener = Interlocked.Exchange(ref _listener, null);
            var listenerThread = Interlocked.Exchange(ref _listenerThread, null);

            if (listener == null)
                return;

            _stop.Set();
            try { listenerThread.Join(); }
            catch { }
            finally { _listenerThread = null; }

            foreach (Thread worker in _workers)
            {
                try { worker.Join(); }
                catch { }
            }

            lock (_clients)
            {
                foreach (TcpClientConnection client in _clients)
                    try { client.Client.Close(); }
                    catch { }
            }

            try { listener.Stop(); }
            catch { }
            finally { _listener = null; }
        }

        private void HandleRequests()
        {
            try
            {
                while (!_stop.WaitOne(0, false))
                {
                    var context = _listener.BeginAcceptTcpClient(null, null);

                    if (0 == WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle }))
                        return;

                    try
                    {
                        TcpClientConnection client = new TcpClientConnection(this, _listener.EndAcceptTcpClient(context));

                        EventHandler<TcpClientEventArgs> handler = OnClientConnect;
                        if (handler != null)
                            handler(this, client);

                        lock (_clients)
                        {
                            _clients.Add(client);
                        }
                        lock (_queue)
                        {
                            _queue.Enqueue(client);
                            _ready.Set();
                        }
                    }
                    catch { return; }
                }
            }
            catch { _stop.Set(); }
        }

        private void DataRecieved(IAsyncResult ar)
        {
            TcpClientConnection client = ar.AsyncState as TcpClientConnection;
            if (client == null) return;
            bool close = false;
            try
            {
                int count = client.Stream.EndRead(ar);
                if (count <= 0)
                    close = true;
                else
                {
                    client.ReadOffset += count;
                    client.AsyncRead = null;
                    lock (_queue)
                    {
                        _queue.Enqueue(client);
                        _ready.Set();
                    }
                }
            }
            catch
            {
                close = true;
            }

            if (close)
            {
                using (client.Client)
                {
                    lock (_clients)
                        _clients.Remove(client);
                }
            }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                TcpClientConnection client;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                        client = _queue.Dequeue();
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }

                try
                {
                    ProcessRequest(client);
                    client.ErrorCount = 0;
                }
                catch (Exception ex)
                {
                    EventHandler<ErrorEventArgs> e = OnError;
                    if (e != null)
                        try { e(client, new ErrorEventArgs(ex)); }
                        catch { }

                    if (client.ErrorCount++ > 10)
                    {
                    }
                }
                finally
                {
                    if (!client.IsClosed)
                    {
                        var readAmt = client.ReadBuffer.Length - client.ReadOffset;
                        if (readAmt <= 0)
                        {
                            var newsize = client.ReadBuffer.Length + 1024;
                            if (newsize > 8192)
                                newsize = client.ReadBuffer.Length + 8192;
                            Array.Resize(ref client.ReadBuffer, newsize);
                            readAmt = newsize - client.ReadOffset;
                        }
                        try
                        {
                            client.AsyncRead = client.Stream.BeginRead(client.ReadBuffer, client.ReadOffset,
                                                                       readAmt, DataRecieved, client);
                        }
                        catch
                        {
                            client.Close();
                        }
                    }
                }
            }
        }

        private void ProcessRequest(TcpClientConnection client)
        {
            EventHandler<TcpClientEventArgs> handler = OnDataRecieved;
            if (handler == null)
                client.Close();
            else
            {
                if (client.BytesDesired <= client.BytesAvailable)
                {
                    client.BytesDesired = 0;
                    handler(this, client);
                }
            }

            if (client.ReadOffset < ushort.MaxValue / 2 && client.ReadBuffer.Length > ushort.MaxValue)
                Array.Resize(ref client.ReadBuffer, client.ReadOffset + 1024);

            if (!client.IsClosed)
            {
                try
                {
                    client.FlushWrite();
                }
                catch
                { client.Close(); }
            }
        }

        private class TcpClientConnection : TcpClientEventArgs
        {
            public readonly NetworkStream Stream;
            public int ErrorCount;
            public int ReadOffset;
            public byte[] ReadBuffer;
            public int WriteOffset;
            public byte[] WriteBuffer;
            public IAsyncResult AsyncRead;

            public TcpClientConnection(TcpServer host, TcpClient client)
                : base(host, client)
            {
                Stream = client.GetStream();
                ErrorCount = 0;
                ReadOffset = 0;
                ReadBuffer = new byte[1024];
                WriteOffset = 0;
                WriteBuffer = new byte[0];
                AsyncRead = null;
            }

            public bool IsClosed
            {
                get { return WriteBuffer == null || ReadBuffer == null; }
            }

            public override void Close()
            {
                try
                {
                    EventHandler<TcpClientEventArgs> handler = Host.OnClientClosed;
                    if (handler != null)
                        try { handler(Host, this); } catch { }

                    using (Client)
                    using (Stream)
                    {
                        lock (Host._clients)
                            Host._clients.Remove(this);
                    }
                }
                catch { }
                finally
                {
                    AsyncRead = null;
                    ErrorCount = -1;
                    ReadOffset = WriteOffset = 0;
                    ReadBuffer = WriteBuffer = null;
                }
            }

            public override byte[] GetBuffer() { return ReadBuffer; }
            public override int BytesAvailable
            {
                get { return ReadOffset; }
                protected set { ReadOffset = value; }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (count > 8192)
                {
                    FlushWrite();
                    Stream.Write(buffer, offset, count);
                    return;
                }

                int required = WriteOffset + count;
                if (WriteBuffer.Length < required)
                    Array.Resize(ref WriteBuffer, required);

                Buffer.BlockCopy(buffer, offset, WriteBuffer, WriteOffset, count);
                WriteOffset += count;
            }

            public void FlushWrite()
            {
                try
                {
                    Stream.Write(WriteBuffer, 0, WriteOffset);
                }
                finally
                {
                    WriteOffset = 0;
                    if (WriteBuffer.Length > ushort.MaxValue)
                        WriteBuffer = new byte[0];
                }
            }
        }
    }

    /// <summary>
    /// Provides a buffered state of the client connection
    /// </summary>
    public abstract class TcpClientEventArgs : EventArgs
    {
        /// <summary>
        /// Returns the TcpServer
        /// </summary>
        public readonly TcpServer Host;
        /// <summary>
        /// Returns the TcpClient for this connection, should not be used directly.
        /// </summary>
        public readonly TcpClient Client;

        /// <summary>
        /// ctor for TcpClientEventArgs
        /// </summary>
        protected TcpClientEventArgs(TcpServer host, TcpClient client)
        {
            Host = host;
            Client = client;
        }

        /// <summary>
        /// Sets or Gets custom user information associated with this connection.
        /// </summary>
        public object UserData { get; set; }
        /// <summary>
        /// Sets or Gets the number of bytes required to fulfill the request.  The event will not be notified again
        /// until the required bytes have been read from the socket.
        /// </summary>
        public int BytesDesired { get; set; }
        /// <summary>
        /// Returns the number of bytes currently available.
        /// </summary>
        public abstract int BytesAvailable { get; protected set; }
        /// <summary>
        /// Returns the buffer being used for reading (not a copy, be careful)
        /// </summary>
        public abstract byte[] GetBuffer();
        /// <summary>
        /// Writes a response to the client
        /// </summary>
        public abstract void Write(byte[] buffer, int offset, int count);
        /// <summary>
        /// Reads (and consumes) the number of bytes specified
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, BytesAvailable);
            Buffer.BlockCopy(GetBuffer(), 0, buffer, offset, count);
            ConsumeBytes(count);
            return count;
        }
        /// <summary>
        /// Consumes (removes from buffer) the number of bytes specified
        /// </summary>
        public void ConsumeBytes(int count)
        {
            if (count == 0)
                return;
            if (count > BytesAvailable)
                throw new ArgumentOutOfRangeException();
            if (count < BytesAvailable)
                Buffer.BlockCopy(GetBuffer(), count, GetBuffer(), 0, BytesAvailable - count);
        
            BytesAvailable -= count;
        }
        /// <summary>
        /// Closes the connection.
        /// </summary>
        public abstract void Close();
    }
}
