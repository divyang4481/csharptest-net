#region Copyright 2009-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading;


namespace CSharpTest.Net.SslTunnel
{
    /// <summary>
    /// A wrapper around the TcpListener 
    /// </summary>
	public class TcpServer : IDisposable
	{
		readonly string _bindingName;
		readonly int _bindingPort;
		IPEndPoint _localEndpoint;

		readonly Thread _server;

		readonly ManualResetEvent _shutdown;
		readonly ManualResetEvent _ready;

		readonly List<IDisposable> _resources;

        /// <summary>
        /// Constructs a server using the the given ip or host name and port number
        /// </summary>
		public TcpServer(string bindingName, int bindingPort)
		{
			_resources = new List<IDisposable>(new IDisposable[] { _shutdown = new ManualResetEvent(false), _ready = new ManualResetEvent(false) });
			_bindingPort = bindingPort;
			_bindingName = bindingName;
			_localEndpoint = null;

			_server = new Thread(RunServer);
			_server.Name = String.Format("TcpServer ({0}:{1}", _bindingName, _bindingPort);
			_server.SetApartmentState(ApartmentState.MTA);
		}
        /// <summary>
        /// Starts listening for requests
        /// </summary>
		public void Start()
		{
			_server.Start();

			WaitHandle[] handles = new WaitHandle[] { _ready, _shutdown };
            if (0 != WaitHandle.WaitAny(handles, new TimeSpan(0, 1, 0), true))
				throw new ApplicationException("Failed to start service thread.");
		}
        /// <summary>
        /// Stops listening for requests
        /// </summary>
		public void Stop()
		{
			_shutdown.Set();
			_server.Join(10000);
			_server.Abort();
			_server.Join();
		}
        /// <summary>
        /// Disposes of the server, stops listening if needed.
        /// </summary>
		public virtual void Dispose()
		{
			try
			{
				Stop();
			}
			catch (ThreadAbortException) { throw; }
			catch (Exception e) { Log.Error(e); }

			for (int i = _resources.Count - 1; i >= 0; i--)
			{
				_resources[i].Dispose();
				_resources.RemoveAt(i);
			}
		}

		// The certificate parameter specifies the name of the file 
		// containing the machine certificate.
		void RunServer()
		{
			try
			{
				IPAddress[] addy = System.Net.Dns.GetHostAddresses(_bindingName);

				if (addy == null || addy.Length == 0)
					throw new ApplicationException("Invalid server name:" + _bindingName);

				_localEndpoint = new IPEndPoint(addy[0], _bindingPort);

				// Create a TCP/IP (IPv4) socket and listen for incoming connections.
				TcpListener listener = new TcpListener(_localEndpoint);
				listener.Start(100);

				Log.Verbose("Started listening on {0}", listener.LocalEndpoint.ToString());
				_ready.Set();

				WaitHandle[] handles = new WaitHandle[] { (WaitHandle)null, _shutdown };

				while (true)
				{
					IAsyncResult result = listener.BeginAcceptTcpClient(OnConnect, listener);
					handles[0] = result.AsyncWaitHandle;

                    if (0 != WaitHandle.WaitAny(handles, -1, true))
					{
						listener.Stop();
						break;
					}
				}
			}
			catch (ThreadAbortException) { throw; }
			catch (Exception e)
			{
				Log.Error(e);
			}
			finally
			{
				_shutdown.Set();
			}
		}

		void OnConnect(IAsyncResult ar)
		{
			TcpListener listener = null;
			System.Net.Sockets.TcpClient client = null;
			try
			{
				listener = (TcpListener)ar.AsyncState;

				client = listener.EndAcceptTcpClient(ar);
				client.NoDelay = true;
				client.ReceiveTimeout = client.SendTimeout = TcpSettings.ActivityTimeout;

				Log.Info("Client connected from {0} to {1}", client.Client.RemoteEndPoint, client.Client.LocalEndPoint);
				ProcessClient(client);
			}
			catch (ThreadAbortException) { throw; }
			catch (ObjectDisposedException)
			{
				Log.Verbose("Connection terminated by client.");
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}

        /// <summary>
        /// Allows customization of the connection handshake (SSL)
        /// </summary>
		protected virtual Stream ConnectClient(System.Net.Sockets.TcpClient client)
		{
			return client.GetStream();
		}

		void ProcessClient(System.Net.Sockets.TcpClient client)
		{
			Stream dataStream = null;
			try
			{
				// A client has connected. Create the 
				// SslStream using the client's network stream.
				dataStream = ConnectClient(client);
				// Set timeouts for the read and write.
				dataStream.ReadTimeout = TcpSettings.ReadTimeout;
				dataStream.WriteTimeout = TcpSettings.WriteTimeout;

				if (Connected != null)
					Connected(this, new ConnectedEventArgs(this, client, dataStream));
			}
			catch (IOException) { }
			catch (ThreadAbortException) { throw; }
			catch (AuthenticationException e)
			{
				Log.Error(e);
				Log.Verbose("Authentication failed - closing the connection.");
			}
			finally
			{
				// The client stream will be closed with the dataStream
				// because we specified this behavior when creating
				// the dataStream.
				if(dataStream != null) dataStream.Close();
				client.Close();
			}
		}
        /// <summary>
        /// The event argument used to handling inbound connections
        /// </summary>
		public class ConnectedEventArgs : System.EventArgs
		{
			readonly TcpServer _server;
			readonly System.Net.Sockets.TcpClient _client;
			readonly Stream _stream;

            internal ConnectedEventArgs(TcpServer server, System.Net.Sockets.TcpClient client, Stream stream)
			{
				_server = server;
				_client = client;
				_stream = stream;
			}
            /// <summary>
            /// Closes the connection on this client
            /// </summary>
			public void Close()
			{
                _client.Close();
			}
            /// <summary>
            /// Returns the local endpoint the client connected to
            /// </summary>
			public IPEndPoint LocalEndPoint { get { return _server._localEndpoint; } }
            /// <summary>
            /// Returns the remote endpoint of the client connection
            /// </summary>
			public IPEndPoint RemoteEndPoint { get { return (IPEndPoint)_client.Client.RemoteEndPoint; } }
            /// <summary>
            /// The network stream for this connection
            /// </summary>
			public Stream Stream { get { return _stream; } }
		}
        /// <summary>
        /// Raised when a client establishes a connection.
        /// </summary>
		public event EventHandler<ConnectedEventArgs> Connected;
	}
}