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
using NUnit.Framework;
using System.Net;

#pragma warning disable 1591
namespace CSharpTest.Net.SslTunnel.Test
{
	[TestFixture]
	public sealed class TestMuxSslTunnel : TestTunnelBase
	{
		int _mixInPort;
		public override void Setup()
		{
			base.Setup();
			_mixInPort = TestPort.Create();
		}

		//<multiplexer ip="LoopBack">
		//  <add port="_mixInPort"/>
		//  <add port="ClientPort"/>
		//  <target ip="LoopBack" port="ServerPort" ssl="true" clientCertFile="ClientCert.CertificateFile">
		//    <expect 
		//      publicKey="ServerCert.Certificate.GetPublicKeyString()"
		//      ignoreErrors="All" />
		//  </target>
		//</multiplexer>
		protected override void AddClient(TunnelConfig config)
		{
			config.Add(new TunnelMultiplexer(
				LoopBack, new int[] { _mixInPort, ClientPort },
				new TunnelSender(LoopBack, ServerPort, true,
					new ExpectedCertificate(ServerCert.Certificate.GetPublicKeyString(), IgnorePolicyErrors.All),
					ClientCert.CertificateFile, null
				)
			));
		}

		//<demultiplexer ip="LoopBack" port="ServerPort" serverCertFile="ServerCert.CertificateFile">
		//  <accept ignoreErrors="All" publicKey="ClientCert.Certificate.GetPublicKeyString()" />
		//  <target forwardingPort="_mixInPort" ip="192.168.1.1" port="24" ssl="true" />
		//  <target forwardingPort="ClientPort" ip="endpoint.Address" port="endpoint.Port" ssl="false" />
		//</demultiplexer>
		protected override void AddServer(TunnelConfig config, IPEndPoint endpoint)
		{
			config.Add(new TunnelDemultiplexer(
				LoopBack, ServerPort,
				new TunnelSenderFromPort[] 
				{
					new TunnelSenderFromPort(_mixInPort, "192.168.1.1", 24, true), //<= never connected
					new TunnelSenderFromPort(ClientPort, endpoint.Address.ToString(), endpoint.Port, false),
				},
				ServerCert.CertificateFile, null,
				new ExpectedCertificate[] { 
					new ExpectedCertificate(ClientCert.Certificate.GetPublicKeyString(), IgnorePolicyErrors.All)
				}
			));
		}
	}
}
