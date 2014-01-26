#region Copyright 2009-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Text;
using System.Net.Sockets;

namespace CSharpTest.Net.SslTunnel
{
    /// <summary>
    /// Wraps the System.Net.Sockets.TcpClient
    /// </summary>
	public class TcpClient : IDisposable
	{
		readonly string _bindingName;
		readonly int _bindingPort;
		readonly System.Net.Sockets.TcpClient _client;
		Stream _dataStream;

		readonly List<IDisposable> _resources;

        /// <summary>
        /// Creates a tcp client to the given server name/ip and port
        /// </summary>
		public TcpClient(string serverName, int bindingPort)
		{
			_resources = new List<IDisposable>();
			_bindingPort = bindingPort;
			_bindingName = serverName;
			_client = new System.Net.Sockets.TcpClient();
			_resources.Add(_client);
		}
        /// <summary>
        /// Creates a new client connection to the same server endpoint
        /// </summary>
		public virtual TcpClient Clone()
		{
			return new TcpClient(_bindingName, _bindingPort);
		}
        /// <summary>
        /// Estabilishes the connection with the server
        /// </summary>
		public void Connect()
		{
			// Create a TCP/IP client socket.
			_client.Connect(_bindingName, _bindingPort);
			_client.ReceiveTimeout = _client.SendTimeout = TcpSettings.ActivityTimeout;
			_client.NoDelay = true;
			
			_dataStream = ConnectServer(_client);
			_resources.Add(_dataStream);
			// Set timeouts for the read and write to 1 minute.
			_dataStream.ReadTimeout = TcpSettings.ReadTimeout;
			_dataStream.WriteTimeout = TcpSettings.WriteTimeout;
		}
        /// <summary>
        /// Allows customization of the connection handshake (SSL)
        /// </summary>
		protected virtual Stream ConnectServer(System.Net.Sockets.TcpClient client)
		{
			return client.GetStream();
		}
        /// <summary>
        /// Disposes of the client connection.
        /// </summary>
		public void Dispose()
		{
			for (int i = _resources.Count - 1; i >= 0; i--)
			{
				_resources[i].Dispose();
				_resources.RemoveAt(i);
			}
		}
        /// <summary>
        /// Returns the server name that this client connects to
        /// </summary>
		public string ServerName { get { return _bindingName; } }
        /// <summary>
        /// Returns the port that this client connects to
        /// </summary>
		public int ServerPort { get { return _bindingPort; } }
		/// <summary>
		/// Returns the underlying client soket after a call to Connect()
		/// </summary>
		public Socket Client { get { return _client.Client; } }
        /// <summary>
        /// Returns the network stream after a call to Connect()
        /// </summary>
		public Stream Stream { get { return _dataStream; } }
	}
}
