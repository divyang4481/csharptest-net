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
using System.Net;
using NUnit.Framework;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CSharpTest.Net.SslTunnel.Test
{
	class TcpTestServer : IDisposable
	{
		TcpServer _testserver;
		int _testport;

		public TcpTestServer()
		{
			_testport = TestPort.Create();
			_testserver = new TcpServer(IPAddress.Loopback.ToString(), _testport);
			_testserver.Connected += Connected;
			_testserver.Start();
		}

		public void Dispose()
		{
			_testserver.Stop();
			_testserver.Dispose();
		}

		public string HostName { get { return IPAddress.Loopback.ToString(); } }
		public int Port { get { return _testport; } }

		void Connected(object sender, TcpServer.ConnectedEventArgs e)
		{
			try
			{
				System.Security.Cryptography.MD5 MD5 = System.Security.Cryptography.MD5.Create();

				using (ReaderWriter io = new ReaderWriter(e.Stream))
				{
					while (true)
					{
						byte[] bytes = io.Read();
						if (bytes.Length == 0)
							break;
						io.Write(MD5.ComputeHash(bytes));
					}
				}
			}
			finally { e.Close(); }
		}

		//Client TEST
		public static void Test(string host, int port, int repeate, params int[] sizes)
		{
			System.Security.Cryptography.MD5 MD5 = System.Security.Cryptography.MD5.Create();

			int sent = 0;
			Random rand = new Random();
			Stopwatch watch = new Stopwatch();
			watch.Start();

			using (TcpClient client = new TcpClient(host, port))
			{
				client.Connect();
				using (ReaderWriter io = new ReaderWriter(client.Stream))
				{
					for (int count = 0; count < repeate; count++)
					{
						int size = sizes[count % sizes.Length];
						byte[] bytes = new byte[size];

						rand.NextBytes(bytes);
						byte[] md5 = MD5.ComputeHash(bytes);
						
						io.Write(bytes);
						byte[] resultMd5 = io.Read();

						Assert.AreEqual(Convert.ToBase64String(md5), Convert.ToBase64String(resultMd5));
						sent++;
					}

					io.Write(new byte[0] {});
				}
			}

			watch.Stop();
			Trace.TraceInformation("Sent {0} requests to port {1} in {2}", sent, port, watch.Elapsed);
		}
	}

	class ReaderWriter : IDisposable
	{
		readonly Stream _io;
		public ReaderWriter(Stream io)
		{
			_io = io;
		}

		void IDisposable.Dispose() { _io.Dispose(); }

		private byte[] Read(int length)
		{
			byte[] results = new byte[length];
			int count = 0;

			while (count < length)
				count += _io.Read(results, count, length - count);
			return results;
		}

		public byte[] Read()
		{
			byte[] data = Read(5);

			Assert.AreEqual(byte.MaxValue, data[0]);
			int length = (0x00FF & data[1]) | ((0x00FF & data[2]) << 8) | ((0x00FF & data[3]) << 16) | ((0x00FF & data[4]) << 24);
			Assert.IsTrue(length >= 0 && length < 10000000);

			byte[] results = Read(length);
			return results;
		}

		public void Write(byte[] bytes)
		{
			byte[] size = new byte[5] { byte.MaxValue, (byte)((bytes.Length) & 0x00ff), (byte)((bytes.Length >> 8) & 0x00ff), (byte)((bytes.Length >> 16) & 0x00ff), (byte)((bytes.Length >> 24) & 0x00ff) };
			_io.Write(size, 0, size.Length);
			_io.Write(bytes, 0, bytes.Length);
			_io.Flush();
		}
	}
}
