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
using System.IO;
using System.Security.Cryptography.X509Certificates;

#pragma warning disable 1591
namespace CSharpTest.Net.SslTunnel.Test
{
	public class TestCert
	{
		public readonly string HostName;
		public readonly string CertificateFile;
		public readonly X509Certificate2 Certificate;

		public TestCert(string hostName)
		{
			HostName = hostName;

			CertificateFile = Path.Combine(Path.GetTempPath(), HostName + ".cer");
			if (!File.Exists(CertificateFile))
			{
				string path = Environment.CurrentDirectory;
				try
				{
					Environment.CurrentDirectory = Path.GetDirectoryName(CertificateFile);
					SslTunnel.Server.Commands.RemoveCert(HostName);
					SslTunnel.Server.Commands.MakeCert(HostName);
				}
				finally
				{ Environment.CurrentDirectory = path; }
			}

			Certificate = new X509Certificate2(CertificateFile);
		}
	}

}
