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
using CSharpTest.Net.Utils;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Net.Security;
using System.Xml;

namespace CSharpTest.Net.SslTunnel
{
	class Config : XmlConfiguration<TunnelConfig>
	{
		public const string SCHEMA_NAME = "SslTunnel.xsd";
		public Config() : base(SCHEMA_NAME) { }
	}

	[XmlRoot("TunnelConfig")]
	public class TunnelConfig
	{
		public TunnelConfig() : this(new TunnelListenerBase[0]) { }
		public TunnelConfig(params TunnelListenerBase[] install)
		{
			Listeners = install;
		}

		public void Add(TunnelListener service) { AddListener(service); }
		public void Add(TunnelMultiplexer service) { AddListener(service); }
		public void Add(TunnelDemultiplexer service) { AddListener(service); }

		private void AddListener<T>(T newservice)
			where T : TunnelListenerBase
		{
			List<TunnelListenerBase> list = new List<TunnelListenerBase>(Listeners);
			list.Add(newservice);
			Listeners = list.ToArray();
		}

		[XmlElement("listener", typeof(TunnelListener))]
		[XmlElement("multiplexer", typeof(TunnelMultiplexer))]
		[XmlElement("demultiplexer", typeof(TunnelDemultiplexer))]
		public TunnelListenerBase[] Listeners;

		public static TunnelConfig Load()
		{
			return Config.ReadConfig("TunnelConfig");
		}
		public static TunnelConfig Load(XmlReader reader)
		{
			return Config.ReadXml(Config.SCHEMA_NAME, reader);
		}

		public IDisposable Start()
		{
			RunConfig runner = new RunConfig(this);
			runner.Start();
			return runner;
		}
	}

	/// <summary>
	/// A common base class for the server listener types, listener, mux, and demux
	/// </summary>
	public abstract class TunnelListenerBase
	{
		protected TunnelListenerBase(string certificate, string password, params ExpectedCertificate[] accept)
		{
			ServerCertificate = certificate;
			ServerCertPassword = password;
			AllowedClients = accept;
		}

		[XmlAttribute("serverCertFile")]
		public string ServerCertificate;
		[XmlAttribute("serverCertPassword")]
		public string ServerCertPassword;

		[XmlElement("accept")]
		public ExpectedCertificate[] AllowedClients;
	}

	/// <summary>
	/// A simple one-to-one port forwarding protocol
	/// </summary>
	public class TunnelListener : TunnelListenerBase
	{
		public TunnelListener() : this("localhost", 0, new TunnelSender()) { }
		public TunnelListener(string endpoint, int port, TunnelSender target)
			: this(endpoint, port, target, null, null, new ExpectedCertificate[0])
		{ }
		public TunnelListener(string endpoint, int port, TunnelSender target, string certificate, string password, params ExpectedCertificate[] accept)
			: base(certificate, password, accept)
		{
			IpEndpoint = endpoint;
			Port = port;
			Target = target;
		}

		[XmlAttribute("ip")]
		public string IpEndpoint;

		[XmlAttribute("port")]
		public int Port;

		[XmlElement("target")]
		public TunnelSender Target;
	}

	/// <summary>
	/// A many-to-one port forwarding protocol that embeds the originating port into the communication
	/// so that the demultiplexer on the other end can forward to the appropriate ip and port
	/// </summary>
	public class TunnelMultiplexer : TunnelListenerBase
	{
		public TunnelMultiplexer() : this("localhost", new int[0], new TunnelSender()) { }
		public TunnelMultiplexer(string endpoint, int[] ports, TunnelSender target)
			: this(endpoint, ports, target, null, null, new ExpectedCertificate[0])
		{ }
		public TunnelMultiplexer(string endpoint, int[] ports, TunnelSender target, string certificate, string password, params ExpectedCertificate[] accept)
			: base(certificate, password, accept)
		{
			IpEndpoint = endpoint;
			Ports = new AddPort[ports.Length];
			for (int i = 0; i < ports.Length; i++)
				Ports[i] = new AddPort(ports[i]);
			Target = target;
		}

		[XmlAttribute("ip")]
		public string IpEndpoint;

		[XmlElement("add")]
		public AddPort[] Ports;

		[XmlElement("target")]
		public TunnelSender Target;
	}

	/// <summary>
	/// A one-to-many port forwarding protocol that receives requests from a multiplexer and dispatches
	/// the the data stream to the services selected by the originating port.
	/// </summary>
	public class TunnelDemultiplexer : TunnelListenerBase
	{
		public TunnelDemultiplexer() : this("localhost", 0, new TunnelSenderFromPort[0]) { }
		public TunnelDemultiplexer(string endpoint, int port, TunnelSenderFromPort[] target)
			: this(endpoint, port, target, null, null, new ExpectedCertificate[0])
		{ }
		public TunnelDemultiplexer(string endpoint, int port, TunnelSenderFromPort[] targets, string certificate, string password, params ExpectedCertificate[] accept)
			: base(certificate, password, accept)
		{
			IpEndpoint = endpoint;
			Port = port;
			Targets = targets;
		}

		[XmlAttribute("ip")]
		public string IpEndpoint;

		[XmlAttribute("port")]
		public int Port;
	
		[XmlElement("target")]
		public TunnelSenderFromPort[] Targets;
	}

	public class AddPort
	{
		public AddPort() : this(0) { }
		public AddPort(int port) { Port = port; }

		[XmlAttribute("port")]
		public int Port;
	}

	public class TunnelSenderFromPort : TunnelSender
	{
		public TunnelSenderFromPort() : this(0, "localhost", 0, false, null, null, null) { }
		public TunnelSenderFromPort(int originalPort, string endpoint, int port, bool useSsl) : this(originalPort, endpoint, port, useSsl, null, null, null) { }
		public TunnelSenderFromPort(int originalPort, string endpoint, int port, bool useSsl, ExpectedCertificate expected, string certificate, string password)
			: base(endpoint, port, useSsl, expected, certificate, password)
		{
			OriginalPort = originalPort;
		}

		[XmlAttribute("forwardingPort")]
		public int OriginalPort;
	}

	/// <summary>
	/// A service endpoint to connect to, setting UseSsl=true or provide either a client certificate or an expected
	/// server certificate to ensure the connection is secured with ssl.
	/// </summary>
	public class TunnelSender
	{
		public TunnelSender() : this("localhost", 0, false, null, null, null) { }
		public TunnelSender(string endpoint, int port, bool useSsl) : this(endpoint, port, useSsl, null, null, null) { }
		public TunnelSender(string endpoint, int port, bool useSsl, ExpectedCertificate expected, string certificate, string password)
		{
			IpEndpoint = endpoint;
			Port = port;
			_useSsl = useSsl;
			ExpectedCert = expected;
			ClientCertificate = certificate;
			ClientCertPassword = password;
		}

		bool _useSsl;

		[XmlAttribute("ip")]
		public string IpEndpoint;

		[XmlAttribute("port")]
		public int Port;

		[XmlAttribute("ssl"), DefaultValue(false)]
		public bool UseSsl 
		{
			get { return _useSsl || !String.IsNullOrEmpty(ClientCertificate) || ExpectedCert != null; } 
			set { _useSsl = value; } 
		}
	
		[XmlAttribute("clientCertFile")]
		public string ClientCertificate;
		[XmlAttribute("clientCertPassword")]
		public string ClientCertPassword;

		[XmlElement("expect")]
		public ExpectedCertificate ExpectedCert;
	}

	public class ExpectedCertificate
	{
		public ExpectedCertificate() { }
		public ExpectedCertificate(string publicKey, IgnorePolicyErrors ignore)
		{
			this.PublicKey = publicKey;
			this.IgnoredErrors = ignore;
		}

		[XmlAttribute("issuedTo")]
		public string IssuedTo;
		[XmlAttribute("hash")]
		public string Hash;
		[XmlAttribute("publicKey")]
		public string PublicKey;
		[XmlAttribute("ignoreErrors"), DefaultValue(IgnorePolicyErrors.None)]
		public IgnorePolicyErrors IgnoredErrors;
	}

	public enum IgnorePolicyErrors
	{
		None,
		NameMismatch,
		ChainErrors,
		All
	}
}
