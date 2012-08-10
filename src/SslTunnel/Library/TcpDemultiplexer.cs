#region Copyright 2009-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.SslTunnel
{
	class TcpDemultiplexer : TcpForwardingBase
	{
		Dictionary<int, TcpClient> _targets;

		public TcpDemultiplexer(TcpServer server)
			: base(server)
		{
			_targets = new Dictionary<int, TcpClient>();
		}

		public override void Dispose()
		{
			base.Dispose();
			try
			{
				foreach (TcpClient client in _targets.Values)
					client.Dispose();
			}
			catch (Exception e) { Log.Error(e); }
			finally { _targets.Clear(); }
		}

		public void Add(int port, TcpClient client)
		{
			_targets.Add(port, client);
		}

		protected override TcpClient OnConnectTarget(SslServer.ConnectedEventArgs args)
		{
			byte[] bytes = new byte[4];
			int read = args.Stream.Read(bytes, 0, 4);
			int origPort = ((int)bytes[0] << 24) | ((int)bytes[1] << 16) | ((int)bytes[2] << 8) | ((int)bytes[3]);
			return _targets[origPort].Clone();
		}

		protected override void OnConnectionEstablished(SslServer.ConnectedEventArgs args, TcpClient client)
		{
		}
	}
}
