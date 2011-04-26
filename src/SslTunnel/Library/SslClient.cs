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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading;

namespace CSharpTest.Net.SslTunnel
{
    /// <summary>
    /// Creates a TcpClient that uses SSL
    /// </summary>
	public class SslClient : TcpClient
	{
		readonly X509Certificate _cert;
		readonly SslCertValidator _certVerify;
		SslStream _sslStream;
        /// <summary>
        /// Creates the client with the specified client certificiate and the expected server information
        /// </summary>
		public SslClient(string serverName, int bindingPort, X509Certificate certificate, ExpectedCertificate expectedCert)
			: this(serverName, bindingPort, certificate, new SslCertValidator(expectedCert))
		{ }
        /// <summary>
        /// Creates the client with the specified client certificiate and a certificate validator
        /// </summary>
        private SslClient(string serverName, int bindingPort, X509Certificate certificate, SslCertValidator validator)
			: base(serverName, bindingPort)
		{
			_cert = certificate;
			_certVerify = validator;
		}
        /// <summary>
        /// Clones the client connection
        /// </summary>
		public override TcpClient Clone()
		{
			return new SslClient(ServerName, ServerPort, _cert, _certVerify);
		}
        /// <summary>
        /// Establishes the client SSL connection
        /// </summary>
		protected override Stream ConnectServer(System.Net.Sockets.TcpClient client)
		{
			Log.Verbose("Connected, SSL Nego...");
			// Create an SSL stream that will close the client's stream.
			_sslStream = new SslStream(base.ConnectServer(client), false, _certVerify.IsValid, LocalCertificateSelectionCallback);

			X509CertificateCollection allCerts = new X509CertificateCollection();
			if(_cert != null) allCerts.Add(_cert);

			_sslStream.AuthenticateAsClient(base.ServerName, allCerts, SslProtocols.Default, false);
			return _sslStream;
		}

		X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals(base.ServerName, targetHost))
				return _cert;
			return null;
		}
	}
}
