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
using System.IO;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.Security;
using System.Diagnostics;

#pragma warning disable 1591
namespace CSharpTest.Net.SslTunnel.Test
{
	public abstract class TestTunnelBase
	{
		protected readonly string LoopBack = IPAddress.Loopback.ToString();
		TestCert _clientCert, _serverCert;
		int _clientPort, _serverPort;

		TextWriter _origout = null, _origerror = null;
		StringWriter _stdout = new StringWriter();
		StringWriter _stderror = new StringWriter();

		#region TestFixture SetUp/TearDown
		[TestFixtureSetUp]
		public virtual void Setup()
		{
			_origout = Console.Out;
			Console.SetOut(_stdout);
			_origerror = Console.Error;
			Console.SetError(_stderror);

			_clientPort = TestPort.Create();
			_serverPort = TestPort.Create();

			_clientCert = new TestCert("client.localhost.nunit");
			_serverCert = new TestCert("server.localhost.nunit");
		}

		[TestFixtureTearDown]
		public virtual void Teardown()
		{
			if (_origout != null)
				Console.SetOut(_origout);
			if (_origerror != null)
				Console.SetError(_origerror);
		}

		[SetUp]
		public void SetupTest()
		{
			_stdout.GetStringBuilder().Length = 0;
			_stderror.GetStringBuilder().Length = 0;
		}
		#endregion

		protected int ClientPort { get { return _clientPort; } }
		protected int ServerPort { get { return _serverPort; } }
		protected TestCert ClientCert { get { return _clientCert; } }
		protected TestCert ServerCert { get { return _serverCert; } }

		protected abstract void AddClient(TunnelConfig config);
		protected abstract void AddServer(TunnelConfig config, IPEndPoint endpoint);

		string Respose { get { return _stdout.ToString(); } }

		[Test]
		public void TestChainForward()
		{
			using (TcpTestServer testserver = new TcpTestServer())
			{
				string loopback = IPAddress.Loopback.ToString();
				TunnelConfig config = new TunnelConfig();
				AddClient(config);
				AddServer(config, new IPEndPoint(IPAddress.Loopback, testserver.Port));

				using (config.Start())
				{
					//Send to the clientPort, forwards to serverPort, forwards to testserver.Port
					TcpTestServer.Test(loopback, _clientPort, 10, 50, 500, 5000, 50000, 500000);
				}
			}
		}

		[Test]
		public void TestConcurrentChainForward()
		{
			using (TcpTestServer testserver = new TcpTestServer())
			{
				string loopback = IPAddress.Loopback.ToString();
				TunnelConfig config = new TunnelConfig();
				AddClient(config);
				AddServer(config, new IPEndPoint(IPAddress.Loopback, testserver.Port));

				using (config.Start())
				{
					int nCompleted = 0;
					ManualResetEvent mreGO = new ManualResetEvent(false);
					ThreadStart tstart = new ThreadStart(
						delegate() 
						{
							mreGO.WaitOne();
							TcpTestServer.Test(loopback, _clientPort, 10, 50, 500, 5000, 50000, 500000); 
							Interlocked.Increment(ref nCompleted); 
						}
					);

					Thread[] threads = new Thread[5];
					for (int i = 0; i < threads.Length; i++)
					{
						threads[i] = new Thread(tstart);
						threads[i].Name = String.Format("TcpTestClient({0})", i);
						threads[i].Start();
					}

					Thread.Sleep(100);
					mreGO.Set();

					foreach (Thread t in threads)
						t.Join();

					Assert.AreEqual(threads.Length, nCompleted);
				}
			}
		}
	}
}
