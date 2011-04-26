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
using NUnit.Framework;
using System.Xml;
using System.IO;

#pragma warning disable 1591
namespace CSharpTest.Net.SslTunnel.Test
{
	[TestFixture]
	public partial class TestSampleConfig
	{
		[Test]
		public void TestConfigMultiplexer()
		{
			TunnelConfig config = null;
			using (XmlReader rdr = new XmlTextReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SslTunnel.Test.Sample.config")))
				config = TunnelConfig.Load(rdr);
			Assert.IsNotNull(config);
			Assert.AreEqual(4, config.Listeners.Length);

			//<multiplexer ip="127.0.0.1">
			Assert.AreEqual(typeof(TunnelMultiplexer), config.Listeners[0].GetType());
			Assert.AreEqual("127.0.0.1", ((TunnelMultiplexer)config.Listeners[0]).IpEndpoint);
			//  <accept ?
			Assert.IsNull(((TunnelMultiplexer)config.Listeners[0]).AllowedClients);
			//  <add port="10080"/>
			Assert.AreEqual(10080, ((TunnelMultiplexer)config.Listeners[0]).Ports[0].Port);
			//  <add port="10081"/>
			Assert.AreEqual(10081, ((TunnelMultiplexer)config.Listeners[0]).Ports[1].Port);
			//  <target ip="127.0.0.1" port="10443" clientCertFile="client.localhost.nunit.cer" clientCertPassword="password">
			Assert.AreEqual("127.0.0.1", ((TunnelMultiplexer)config.Listeners[0]).Target.IpEndpoint);
			Assert.AreEqual(10443, ((TunnelMultiplexer)config.Listeners[0]).Target.Port);
			Assert.AreEqual("client.localhost.nunit.cer", ((TunnelMultiplexer)config.Listeners[0]).Target.ClientCertificate);
			Assert.AreEqual("password", ((TunnelMultiplexer)config.Listeners[0]).Target.ClientCertPassword);
			//    <!--expect publicKey = server.localhost.nunit.cer-->
			//    <expect ignoreErrors="All" publicKey="30818902818100A3F81009F73AC50EDA186F8EDBB846C63A8BB8F0E8C25179DEA8FA376372E9394D470B071A76AA0F8D6250B98B8665FF2C03097D7055080AD237F1038404C99F44F2235BC319FFEBF70505225DAD4D47A1868FC92B4E9DEECA06F7BC5171CD96603B35AA6F7816DE294885E0AEF5B62EA981983822174CFDF2C46F392276DA8F0203010001" />
			Assert.AreEqual("30818902818100A3F81009F73AC50EDA186F8EDBB846C63A8BB8F0E8C25179DEA8FA376372E9394D470B071A76AA0F8D6250B98B8665FF2C03097D7055080AD237F1038404C99F44F2235BC319FFEBF70505225DAD4D47A1868FC92B4E9DEECA06F7BC5171CD96603B35AA6F7816DE294885E0AEF5B62EA981983822174CFDF2C46F392276DA8F0203010001",
				((TunnelMultiplexer)config.Listeners[0]).Target.ExpectedCert.PublicKey);
			Assert.AreEqual(IgnorePolicyErrors.All, ((TunnelMultiplexer)config.Listeners[0]).Target.ExpectedCert.IgnoredErrors);
			//  </target>
			//</multiplexer>
		}

		[Test]
		public void TestConfigDemultiplexer()
		{
			TunnelConfig config = null;
			using (XmlReader rdr = new XmlTextReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SslTunnel.Test.Sample.config")))
				config = TunnelConfig.Load(rdr);
			Assert.IsNotNull(config);
			Assert.AreEqual(4, config.Listeners.Length);

			//<demultiplexer ip="127.0.0.1" port="10443" serverCertFile="server.localhost.nunit.cer" serverCertPassword="password">
			Assert.AreEqual(typeof(TunnelDemultiplexer), config.Listeners[1].GetType());
			Assert.AreEqual("127.0.0.1", ((TunnelDemultiplexer)config.Listeners[1]).IpEndpoint);
			Assert.AreEqual(10443, ((TunnelDemultiplexer)config.Listeners[1]).Port);
			Assert.AreEqual("server.localhost.nunit.cer", ((TunnelDemultiplexer)config.Listeners[1]).ServerCertificate);
			Assert.AreEqual("password", ((TunnelDemultiplexer)config.Listeners[1]).ServerCertPassword);
			//<accept issuedTo="client.localhost.nunit"
			Assert.AreEqual(1, ((TunnelDemultiplexer)config.Listeners[1]).AllowedClients.Length);
			Assert.AreEqual("client.localhost.nunit", ((TunnelDemultiplexer)config.Listeners[1]).AllowedClients[0].IssuedTo);
			//  hash="68D757B929A1A91806C098C9BF89297EAB16675D"
			Assert.AreEqual("68D757B929A1A91806C098C9BF89297EAB16675D", ((TunnelDemultiplexer)config.Listeners[1]).AllowedClients[0].Hash);
			//  publicKey="30818902818100CF2B2CCF01956ADE2725104674FE107173446061A694BEFF31FDCAC134EA69125E1704CD0BBFBE1806F29909C60416FFEF811A03C8A3A248CD7086F7BDE959BC29DE4999A8A44191BCE8102DDC56E245476B91F773C2E7A47C32BC3935AF8766082F391E165976A46D6A57B609540B92E4BE681EC4E2EFB8C3F1C12B5709DFE90203010001"
			Assert.AreEqual("30818902818100CF2B2CCF01956ADE2725104674FE107173446061A694BEFF31FDCAC134EA69125E1704CD0BBFBE1806F29909C60416FFEF811A03C8A3A248CD7086F7BDE959BC29DE4999A8A44191BCE8102DDC56E245476B91F773C2E7A47C32BC3935AF8766082F391E165976A46D6A57B609540B92E4BE681EC4E2EFB8C3F1C12B5709DFE90203010001", 
				((TunnelDemultiplexer)config.Listeners[1]).AllowedClients[0].PublicKey);
			//  ignoreErrors="All" />
			Assert.AreEqual(IgnorePolicyErrors.All, ((TunnelDemultiplexer)config.Listeners[1]).AllowedClients[0].IgnoredErrors);
			Assert.AreEqual(2, ((TunnelDemultiplexer)config.Listeners[1]).Targets.Length);
			//  <target forwardingPort="10080" ip="google.com" port="443" ssl="true" clientCertFile="client.google.com.cer" />
			Assert.AreEqual(10080, ((TunnelDemultiplexer)config.Listeners[1]).Targets[0].OriginalPort);
			Assert.AreEqual("google.com", ((TunnelDemultiplexer)config.Listeners[1]).Targets[0].IpEndpoint);
			Assert.AreEqual(443, ((TunnelDemultiplexer)config.Listeners[1]).Targets[0].Port);
			Assert.AreEqual(true, ((TunnelDemultiplexer)config.Listeners[1]).Targets[0].UseSsl);
			Assert.AreEqual("client.google.com.cer", ((TunnelDemultiplexer)config.Listeners[1]).Targets[0].ClientCertificate);
			//  <target forwardingPort="10081" ip="yahoo.com" port="80" />
			Assert.AreEqual(10081, ((TunnelDemultiplexer)config.Listeners[1]).Targets[1].OriginalPort);
			Assert.AreEqual("yahoo.com", ((TunnelDemultiplexer)config.Listeners[1]).Targets[1].IpEndpoint);
			Assert.AreEqual(80, ((TunnelDemultiplexer)config.Listeners[1]).Targets[1].Port);
			Assert.AreEqual(false, ((TunnelDemultiplexer)config.Listeners[1]).Targets[1].UseSsl);
			//</demultiplexer>
		}

		[Test]
		public void TestConfigListener1()
		{
			TunnelConfig config = null;
			using (XmlReader rdr = new XmlTextReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SslTunnel.Test.Sample.config")))
				config = TunnelConfig.Load(rdr);
			Assert.IsNotNull(config);
			Assert.AreEqual(4, config.Listeners.Length);

			//<listener ip="127.0.0.1" port="11080">
			Assert.AreEqual(typeof(TunnelListener), config.Listeners[2].GetType());
			Assert.AreEqual("127.0.0.1", ((TunnelListener)config.Listeners[2]).IpEndpoint);
			Assert.AreEqual(11080, ((TunnelListener)config.Listeners[2]).Port);
			//  <target ip="127.0.0.1" port="11443" ssl="true" clientCertFile="client.localhost.nunit.cer">
			Assert.AreEqual("127.0.0.1", ((TunnelListener)config.Listeners[2]).Target.IpEndpoint);
			Assert.AreEqual(11443, ((TunnelListener)config.Listeners[2]).Target.Port);
			Assert.AreEqual(true, ((TunnelListener)config.Listeners[2]).Target.UseSsl);
			Assert.AreEqual("client.localhost.nunit.cer", ((TunnelListener)config.Listeners[2]).Target.ClientCertificate);
			//    <!--expect publicKey = server.localhost.nunit.cer-->
			//    <expect ignoreErrors="All" publicKey="30818902818100A3F81009F73AC50EDA186F8EDBB846C63A8BB8F0E8C25179DEA8FA376372E9394D470B071A76AA0F8D6250B98B8665FF2C03097D7055080AD237F1038404C99F44F2235BC319FFEBF70505225DAD4D47A1868FC92B4E9DEECA06F7BC5171CD96603B35AA6F7816DE294885E0AEF5B62EA981983822174CFDF2C46F392276DA8F0203010001" />
			Assert.AreEqual(IgnorePolicyErrors.All, ((TunnelListener)config.Listeners[2]).Target.ExpectedCert.IgnoredErrors);
			Assert.AreEqual("30818902818100A3F81009F73AC50EDA186F8EDBB846C63A8BB8F0E8C25179DEA8FA376372E9394D470B071A76AA0F8D6250B98B8665FF2C03097D7055080AD237F1038404C99F44F2235BC319FFEBF70505225DAD4D47A1868FC92B4E9DEECA06F7BC5171CD96603B35AA6F7816DE294885E0AEF5B62EA981983822174CFDF2C46F392276DA8F0203010001", 
				((TunnelListener)config.Listeners[2]).Target.ExpectedCert.PublicKey);
			//  </target>
			//</listener>
		}

		[Test]
		public void TestConfigListener2()
		{
			TunnelConfig config = null;
			using (XmlReader rdr = new XmlTextReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SslTunnel.Test.Sample.config")))
				config = TunnelConfig.Load(rdr);
			Assert.IsNotNull(config);
			Assert.AreEqual(4, config.Listeners.Length);

			//<listener ip="127.0.0.1" port="11443" serverCertFile="server.localhost.nunit.cer" serverCertPassword="password">
			Assert.AreEqual(typeof(TunnelListener), config.Listeners[3].GetType());
			Assert.AreEqual("127.0.0.1", ((TunnelListener)config.Listeners[3]).IpEndpoint);
			Assert.AreEqual(11443, ((TunnelListener)config.Listeners[3]).Port);
			Assert.AreEqual("server.localhost.nunit.cer", ((TunnelListener)config.Listeners[3]).ServerCertificate);
			Assert.AreEqual("password", ((TunnelListener)config.Listeners[3]).ServerCertPassword);
			//  <!--only allow name/hash/publicKey = client.localhost.nunit.cer-->
			//  <accept issuedTo="client.localhost.nunit"
			Assert.AreEqual(1, ((TunnelListener)config.Listeners[3]).AllowedClients.Length);
			Assert.AreEqual("client.localhost.nunit", ((TunnelListener)config.Listeners[3]).AllowedClients[0].IssuedTo);
			//    hash="68D757B929A1A91806C098C9BF89297EAB16675D"
			Assert.AreEqual("68D757B929A1A91806C098C9BF89297EAB16675D", ((TunnelListener)config.Listeners[3]).AllowedClients[0].Hash);
			//    publicKey="30818902818100CF2B2CCF01956ADE2725104674FE107173446061A694BEFF31FDCAC134EA69125E1704CD0BBFBE1806F29909C60416FFEF811A03C8A3A248CD7086F7BDE959BC29DE4999A8A44191BCE8102DDC56E245476B91F773C2E7A47C32BC3935AF8766082F391E165976A46D6A57B609540B92E4BE681EC4E2EFB8C3F1C12B5709DFE90203010001"
			Assert.AreEqual("30818902818100CF2B2CCF01956ADE2725104674FE107173446061A694BEFF31FDCAC134EA69125E1704CD0BBFBE1806F29909C60416FFEF811A03C8A3A248CD7086F7BDE959BC29DE4999A8A44191BCE8102DDC56E245476B91F773C2E7A47C32BC3935AF8766082F391E165976A46D6A57B609540B92E4BE681EC4E2EFB8C3F1C12B5709DFE90203010001", 
				((TunnelListener)config.Listeners[3]).AllowedClients[0].PublicKey);
			//    ignoreErrors="All" />
			Assert.AreEqual(IgnorePolicyErrors.All, ((TunnelListener)config.Listeners[3]).AllowedClients[0].IgnoredErrors);
			//  <target ip="google.com" port="443" ssl="true" />
			Assert.AreEqual("google.com", ((TunnelListener)config.Listeners[3]).Target.IpEndpoint);
			Assert.AreEqual(443, ((TunnelListener)config.Listeners[3]).Target.Port);
			Assert.AreEqual(true, ((TunnelListener)config.Listeners[3]).Target.UseSsl);
			//</listener>
		}

	}
}
