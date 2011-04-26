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
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading;

namespace CSharpTest.Net.SslTunnel
{
    /// <summary>
    /// TcpServer implementation that uses SSL
    /// </summary>
	public class SslServer : TcpServer
	{
		readonly X509Certificate _cert;
		readonly SslCertValidator _certVerify;
        /// <summary>
        /// Constructs the server with the specified certificate and optionally allowable client certificates
        /// </summary>
		public SslServer(string bindingName, int bindingPort, X509Certificate certificate, ExpectedCertificate[] allowClients)
			: base(bindingName, bindingPort)
		{
			_cert = certificate;
			_certVerify = new SslCertValidator(allowClients);
		}
        /// <summary>
        /// Establishes the SSL connection for this client.
        /// </summary>
		protected override Stream ConnectClient(System.Net.Sockets.TcpClient client)
		{
			// A client has connected. Create the 
			// SslStream using the client's network stream.
			SslStream sslStream = new SslStream(client.GetStream(), false, RemoteCertificateValidationCallback, LocalCertificateSelectionCallback);

			// Authenticate the server but don't require the client to authenticate.
			sslStream.AuthenticateAsServer(_cert, _certVerify.CertRequired, SslProtocols.Default, false);
			//
			// Display the properties and settings for the authenticated stream.
			DisplaySslInfo(sslStream);

			if (!sslStream.IsEncrypted)
				throw new ApplicationException("Unable to establish encryption");
			
			return sslStream;
		}

		bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			if(!_certVerify.CertRequired )
				sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNotAvailable;

			return _certVerify.IsValid(sender, certificate, chain, sslPolicyErrors);
		}

		X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			return _cert;
		}

		static void DisplaySslInfo(SslStream stream)
		{
			StringWriter sw = new StringWriter();
			sw.WriteLine();
			//sw.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
			//sw.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
			//sw.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
			//sw.WriteLine("Protocol: {0}", stream.SslProtocol);
			sw.WriteLine("Is authenticated: {0}", stream.IsAuthenticated);
			sw.WriteLine("Is signed: {0}", stream.IsSigned);
			sw.WriteLine("Is encrypted: {0}", stream.IsEncrypted);
			//sw.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);
			//sw.WriteLine("Local cert: {0}", stream.LocalCertificate);
			//sw.WriteLine("Remote cert: {0}", stream.RemoteCertificate);
			Log.Verbose(sw.ToString());
		}
	}
}