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
using System.IO;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.Security;
using System.Diagnostics;
using System.Xml;

#pragma warning disable 1591
namespace CSharpTest.Net.SslTunnel.Test
{
	[TestFixture]
	public partial class TestClientServer
	{
		TestCert _clientCert, _serverCert;

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

		public string Respose { get { return _stdout.ToString(); } }

		[Test]
		public void TestSslServerMain()
		{
			AppDomain.CurrentDomain.ExecuteAssemblyByName(
				typeof(SslTunnel.Server.Commands).Assembly.GetName(),
				AppDomain.CurrentDomain.Evidence,
				new string[] { "help" }
				);

			Assert.IsTrue(Respose.Contains("SslTunnel.Server.exe"));
			Assert.IsTrue(Respose.Contains("Copyright"));
			Assert.IsTrue(Respose.Contains("DUMPCERT"));
			Assert.IsTrue(Respose.Contains("MAKECERT"));
			Assert.IsTrue(Respose.Contains("INSTALL"));
			Assert.IsTrue(Respose.Contains("UNINSTALL"));
		}

		[Test]
		public void TestSslServerInstall()
		{
			SslTunnel.Server.Commands.Install();
		}

		[Test]
		public void TestSslServerUninstall()
		{
			SslTunnel.Server.Commands.Uninstall();
		}

		[Test]
		public void TestCreateRemoveCert()
		{
			string line;
			const string testName = "trash.localhost.nunit";

			SslTunnel.Server.Commands.RemoveCert(testName);
			File.Delete(testName + ".cer");

			try
			{
				bool testsubj = false, testacl = false;

				Assert.IsFalse(File.Exists(testName + ".cer"));
				_stdout.GetStringBuilder().Length = 0;
				SslTunnel.Server.Commands.MakeCert(testName);
				using (StringReader rdr = new StringReader(Respose))
				{
					while (null != (line = rdr.ReadLine()))
					{
						if (!testsubj && line.StartsWith("Subject"))
						{
							Assert.AreEqual(line, "Subject = CN=" + testName);
							testsubj = true;
						}
						if (!testacl && (line.Contains("Allow") && line.Contains("NETWORK SERVICE")))
						{
							Assert.IsTrue(line.Contains("FullControl"));
							testacl = true;
						}
					}
				}
				Assert.IsTrue(testsubj, "Subject not found.");
				Assert.IsTrue(testacl, "ACE entry not found.");
				Assert.IsTrue(File.Exists(testName + ".cer"));

				X509Certificate2 cert = new X509Certificate2(testName + ".cer");
				Assert.AreEqual("CN=" + testName, cert.Subject);
				Assert.AreEqual("CN=" + testName, cert.Issuer);

				SslTunnel.Server.Commands.RemoveCert(testName);
			}
			finally
			{
				File.Delete(testName + ".cer");
				SslTunnel.Server.Commands.RemoveCert(testName);
			}

		}

		[Test]
		public void TestCommandRun()
		{
			string exeFile = typeof(SslTunnel.Server.Commands).Assembly.Location;
			Assert.IsTrue(File.Exists(exeFile));
			File.Copy(exeFile, Path.ChangeExtension(exeFile, ".nunittest.exe"), true);
			exeFile = Path.ChangeExtension(exeFile, ".nunittest.exe");
			int testPort = TestPort.Create();

			using (TextWriter config = File.CreateText(exeFile + ".config"))
			{
				config.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
				config.WriteLine("<configuration>");
				config.WriteLine("  <configSections>");
				config.WriteLine("	  <section name=\"TunnelConfig\" type=\"CSharpTest.Net.SslTunnel.Config, SslTunnel.Library\" />");
				config.WriteLine("  </configSections>");
				config.WriteLine("  <TunnelConfig>");
				config.WriteLine("	  <listener ip=\"127.0.0.1\" port=\"{0}\" serverCertFile=\"{1}\">", testPort, _serverCert.CertificateFile);
				config.WriteLine("	    <target ip=\"google.com\" port=\"80\" />");
				config.WriteLine("	  </listener>");
				config.WriteLine("  </TunnelConfig>");
				config.WriteLine("</configuration>");
			}

			ServicePointManager.ServerCertificateValidationCallback =
				delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
				{
					if (certificate.GetPublicKeyString() == _serverCert.Certificate.GetPublicKeyString())
						return true;
					return sslPolicyErrors == SslPolicyErrors.None;
				};
			try
			{
				try
				{
					using (TcpClient client = new TcpClient("127.0.0.1", testPort))
					{
						client.Connect();
						client.Stream.WriteByte(0);
					}

					Assert.Fail("Connection should fail before we call Run()");
				}
				catch (SocketException e)
				{
					Assert.AreEqual(SocketError.ConnectionRefused, e.SocketErrorCode);
				}

				AppDomainSetup setup = new AppDomainSetup();
				setup.ApplicationBase = Path.GetDirectoryName(exeFile);
				setup.ApplicationName = Path.GetFileName(exeFile);
				setup.ConfigurationFile = exeFile + ".config";
				AppDomain domain = AppDomain.CreateDomain("SslTunnel", AppDomain.CurrentDomain.Evidence, setup);
				BlockingReader blockRead = new BlockingReader(Environment.NewLine);
				Thread thread = null;
				try
				{
					thread = new Thread(
						delegate()
						{
							SetConsoleInput domainConsole = (SetConsoleInput)domain.CreateInstanceAndUnwrap(typeof(SetConsoleInput).Assembly.FullName, typeof(SetConsoleInput).FullName);
							domainConsole.SetInput(blockRead);
							domainConsole.SetOutput(_stdout);
							domainConsole.SetError(_stderror);
							domain.ExecuteAssembly(exeFile, AppDomain.CurrentDomain.Evidence, new string[] { "run" });
						});
					thread.Name = setup.ApplicationName;
					thread.Start();

					while (thread.IsAlive && Respose.Contains("Press [Enter] to quit...") == false && Respose.Contains("127.0.0.1") == false)
						Thread.Sleep(100);

					WebClient client = new WebClient();
					string response = client.DownloadString("https://127.0.0.1:" + testPort);
					Assert.IsTrue(response.Contains("<html"));
					Assert.IsTrue(response.Contains("google.com"));
				}
				finally
				{
					try
					{
						if (thread != null)
						{
							blockRead.WaitHandle.Set();
							Assert.IsTrue(thread.Join(TimeSpan.FromMinutes(1)));
						}
						AppDomain.Unload(domain);
						File.Delete(exeFile + ".config");
						File.Delete(exeFile);
					}
					catch (Exception e) { Trace.TraceWarning(e.ToString()); }
				}
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback = null;
			}
		}

		class BlockingReader : StringReader
		{
			public readonly ManualResetEvent WaitHandle = new ManualResetEvent(false);
			public BlockingReader(string s) : base(s) { }
			public override string ReadLine()
			{
				WaitHandle.WaitOne();
				return base.ReadLine();
			}
		}

		class SetConsoleInput : MarshalByRefObject
		{
			public SetConsoleInput() { }

			public void SetInput(TextReader io) { Console.SetIn(io); }
			public void SetOutput(TextWriter io) { Console.SetOut(io); }
			public void SetError(TextWriter io) { Console.SetError(io); }
		}
	}
}
