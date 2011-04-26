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
using System.Threading;
using System.IO;

namespace CSharpTest.Net.SslTunnel
{
    /// <summary>
    /// An abstract class for handling the connection of clients and establishing a forwarding 
    /// route to another tcp connection.
    /// </summary>
	public abstract class TcpForwardingBase : IDisposable
	{
        private string _logDirectory;
        readonly List<TcpServer> _servers;

        /// <summary>
        /// Constructs the instance without a server
        /// </summary>
		protected TcpForwardingBase()
		{
            _logDirectory = null;
			_servers = new List<TcpServer>();
		}
        /// <summary>
        /// Constructs the instance with a server
        /// </summary>
		public TcpForwardingBase(TcpServer server)
			: this()
		{
			AddServer(server);
		}
        /// <summary>
        /// Provided with a directory it will log the communications there
        /// </summary>
        public void SetLogDirectory(string directory)
        {
            if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                throw new DirectoryNotFoundException(String.Format("The directory '{0}' does not exist.", directory));
            _logDirectory = directory;
        }

        /// <summary>
        /// Adds another server to forward connections for
        /// </summary>
		protected void AddServer(TcpServer server)
		{
			_servers.Add(server);
			server.Connected += server_Connected;
		}
        /// <summary>
        /// Stops and disposes of all servers
        /// </summary>
		public virtual void Dispose()
		{
			try
			{
				foreach (TcpServer server in _servers)
				{
					server.Stop();
					server.Dispose();
				}
			}
			catch (Exception e) { Log.Error(e); }
			finally { _servers.Clear(); }
		}
        /// <summary>
        /// Called as a connection is established to determin the target of the forwarding
        /// </summary>
		protected abstract TcpClient OnConnectTarget(SslServer.ConnectedEventArgs args);
        /// <summary>
        /// Called to handle a handshake when connecting to the forwarding target host 
        /// </summary>
		protected abstract void OnConnectionEstablished(SslServer.ConnectedEventArgs args, TcpClient client);
        /// <summary>
        /// Starts all servers
        /// </summary>
		public virtual IDisposable Start()
		{
			foreach (TcpServer server in _servers)
				server.Start();
			return this;
		}

		void server_Connected(object sender, SslServer.ConnectedEventArgs args)
		{
			string connectionInfo = args.RemoteEndPoint.ToString();

			try
			{
				using (TcpClient client = OnConnectTarget(args))
                using (BinaryLogging logging = new BinaryLogging(_logDirectory, args.RemoteEndPoint, args.LocalEndPoint))
				{
					client.Connect();
					connectionInfo = String.Format("{0} => {1}", args.RemoteEndPoint, client.Client.RemoteEndPoint);
					string revConnectionInfo = String.Format("{1} => {0}", args.RemoteEndPoint, client.Client.RemoteEndPoint);

					OnConnectionEstablished(args, client);

                    StreamRedirect recv = new StreamRedirect(args.Stream, client.Stream, connectionInfo, logging.FromServer);
                    StreamRedirect send = new StreamRedirect(client.Stream, args.Stream, revConnectionInfo, logging.FromClient);

					Log.Verbose("Streaming from {0}", connectionInfo);
                    WaitHandle.WaitAny(new WaitHandle[] { send.WaitClosed, recv.WaitClosed }, -1, true);
				}
			}
			catch (ThreadAbortException) { throw; }
			catch (Exception e)
			{
				Log.Error(e);
			}
			finally
			{
				args.Close();
				Log.Verbose("Streaming shutdown from {0}", connectionInfo);
			}
		}
	}
}
