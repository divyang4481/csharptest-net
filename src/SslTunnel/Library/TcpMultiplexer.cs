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

namespace CSharpTest.Net.SslTunnel
{
	class TcpMultiplexer : TcpForwardingBase
	{
		TcpClient _target;

		public TcpMultiplexer(TcpClient target)
		{
			_target = target;
		}

		public override void Dispose()
		{
			base.Dispose();
			_target.Dispose();
		}

		public void Add(TcpServer server) { base.AddServer(server); }

		protected override TcpClient OnConnectTarget(SslServer.ConnectedEventArgs args)
		{
			return _target.Clone();
		}

		protected override void OnConnectionEstablished(SslServer.ConnectedEventArgs args, TcpClient client)
		{
			int port = args.LocalEndPoint.Port;
			byte[] bytes = new byte[4]
			{
				(byte)(0x00ff & (port >> 24)),
				(byte)(0x00ff & (port >> 16)),
				(byte)(0x00ff & (port >> 8)),
				(byte)(0x00ff & (port))
			};

			client.Stream.Write(bytes, 0, 4);
		}
	}
}
