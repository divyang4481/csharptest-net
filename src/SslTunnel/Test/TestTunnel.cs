#region Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using NUnit.Framework;
using System.Net;

#pragma warning disable 1591
namespace CSharpTest.Net.SslTunnel.Test
{
	[TestFixture]
	public sealed class TestTunnel : TestTunnelBase
	{
		//<listener ip="LoopBack" port="ClientPort">
		//  <target ip="LoopBack" port="ServerPort" ssl="false" />
		//</listener>
		protected override void AddClient(TunnelConfig config)
		{
			config.Add(new TunnelListener(
				LoopBack, ClientPort,
				new TunnelSender(LoopBack, ServerPort, false)
			));
		}

		//<listener ip="LoopBack" port="ServerPort">
		//  <target ip="endpoint.Address" port="endpoint.Port" ssl="false" />
		//</listener>
		protected override void AddServer(TunnelConfig config, IPEndPoint endpoint)
		{
			config.Add(new TunnelListener(
				LoopBack, ServerPort,
				new TunnelSender(endpoint.Address.ToString(), endpoint.Port, false)
			));
		}

		[Test]
		public void TestTcpTestServerVerification()
		{
			using (TcpTestServer testserver = new TcpTestServer())
			{
				//Simply verify our TcpTestServer is working without external influence.
				TcpTestServer.Test(testserver.HostName, testserver.Port, 10, 50, 500, 5000, 50000, 500000);
			}
		}

		[Test]
		public void TestSingleForward()
		{
			using (TcpTestServer testserver = new TcpTestServer())
			{
				TunnelConfig config = new TunnelConfig();
				AddServer(config, new IPEndPoint(IPAddress.Loopback, testserver.Port));

				using (config.Start())
				{
					//Send to the serverPort, forwards to testserver.Port
					TcpTestServer.Test(LoopBack, ServerPort, 10, 50, 500, 5000, 50000, 500000);
				}
			}
		}
	}
}
