#region Copyright 2009-2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Security.Cryptography.X509Certificates;

namespace CSharpTest.Net.SslTunnel
{
	class RunConfig : IDisposable
	{
		readonly TunnelConfig _config;
		readonly List<IDisposable> _running = new List<IDisposable>();

		public RunConfig(TunnelConfig config)
		{
			_config = config;
		}

		public void Start()
		{
			foreach (TunnelListenerBase listenerBase in _config.Listeners)
			{
				if (listenerBase is TunnelListener)
					_running.Add(MakeRedirect((TunnelListener)listenerBase));
				else if (listenerBase is TunnelDemultiplexer)
					_running.Add(MakeDemux((TunnelDemultiplexer)listenerBase));
				else if (listenerBase is TunnelMultiplexer)
					_running.Add(MakeMux((TunnelMultiplexer)listenerBase));
				else
					throw new InvalidOperationException();
			}
		}

		private IDisposable MakeRedirect(TunnelListener config)
		{
            TcpRedirector redir = new TcpRedirector(
                MakeListener(config.IpEndpoint, config.Port, config),
                MakeSender(config.Target)
                );
            redir.SetLogDirectory(config.MonitoringDirectory);
            redir.Start();
            return redir;
		}

		private IDisposable MakeDemux(TunnelDemultiplexer config)
		{
			TcpDemultiplexer demux = new TcpDemultiplexer(
				MakeListener(config.IpEndpoint, config.Port, config)
				);
			foreach(TunnelSenderFromPort target in config.Targets)
                demux.Add(target.OriginalPort, MakeSender(target));
            demux.SetLogDirectory(config.MonitoringDirectory);
			return demux.Start();
		}

		private IDisposable MakeMux(TunnelMultiplexer config)
		{
			TcpMultiplexer mux = new TcpMultiplexer(MakeSender(config.Target));
			foreach (AddPort addport in config.Ports)
                mux.Add(MakeListener(config.IpEndpoint, addport.Port, config));
            mux.SetLogDirectory(config.MonitoringDirectory);
			return mux.Start();
		}

		private TcpServer MakeListener(string ip, int port, TunnelListenerBase config)
		{
			TcpServer server = null;
			if (String.IsNullOrEmpty(config.ServerCertificate))
				server = new TcpServer(ip, port);
			else
			{
				X509Certificate cert = new X509Certificate(config.ServerCertificate, config.ServerCertPassword);
				server = new SslServer(ip, port, cert, config.AllowedClients);
			}
			return server;
		}

		private TcpClient MakeSender(TunnelSender target)
		{
			TcpClient client = null;
			if (!target.UseSsl)
				client = new TcpClient(target.IpEndpoint, target.Port);
			else
			{
				X509Certificate cert = null;
				if (!String.IsNullOrEmpty(target.ClientCertificate))
					cert = new X509Certificate(target.ClientCertificate, target.ClientCertPassword);

				client = new SslClient(target.IpEndpoint, target.Port, cert, target.ExpectedCert);
			}
			return client;
		}

		public void Stop()
		{
			for (int i = _running.Count - 1; i >= 0; i--)
			{
				_running[i].Dispose();
				_running.RemoveAt(i);
			}
		}

		public void Dispose()
		{
			Stop();
		}
	}
}
