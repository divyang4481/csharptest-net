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
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;

namespace CSharpTest.Net.SslTunnel
{
	public class SslCertValidator
	{
		readonly List<ExpectedCertificate> _allowed;
		public SslCertValidator(params ExpectedCertificate[] allow)
		{
			_allowed = new List<ExpectedCertificate>();
			if (allow != null)
				_allowed.AddRange(allow);
		}

		public bool CertRequired
		{
			get { return _allowed.Count > 0; }
		}

		public bool IsValid(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			DebugDumpCertificate(certificate);

			foreach(ExpectedCertificate allow in _allowed)
			{
				if (IsMatch(allow, certificate, chain, sslPolicyErrors))
					return true;
			}

			if (_allowed.Count == 0 && sslPolicyErrors == SslPolicyErrors.None)
				return true;

			Log.Error("Cert error: {0} on {1}", sslPolicyErrors, certificate == null ? "null" : certificate.Subject);
			return false;
		}

		bool IsMatch(ExpectedCertificate allow, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			if (allow.IgnoredErrors == IgnorePolicyErrors.All || allow.IgnoredErrors == IgnorePolicyErrors.ChainErrors)
				sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
			if (allow.IgnoredErrors == IgnorePolicyErrors.All || allow.IgnoredErrors == IgnorePolicyErrors.NameMismatch)
				sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;

			if (sslPolicyErrors != SslPolicyErrors.None)
				return false;

			if (!String.IsNullOrEmpty(allow.IssuedTo) &&
				false == StringComparer.Ordinal.Equals(allow.IssuedTo, certificate.Subject))
				return false;

			if (!String.IsNullOrEmpty(allow.Hash) &&
				false == StringComparer.Ordinal.Equals(allow.Hash, certificate.GetCertHashString()))
				return false;

			if (!String.IsNullOrEmpty(allow.PublicKey) &&
				false == StringComparer.Ordinal.Equals(allow.PublicKey, certificate.GetPublicKeyString()))
				return false;

			return true;
		}

		public static void DebugDumpCertificate(X509Certificate certificate)
		{
			StringWriter sw = new StringWriter();
			DebugDumpCertificate(certificate, sw);
			Log.Verbose(sw.ToString());
		}

		public static void DebugDumpCertificate(X509Certificate certificate, TextWriter sw)
		{
			if (certificate != null)
			{
				sw.WriteLine("Issuer = {0}", certificate.Issuer);
				sw.WriteLine("Subject = {0}", certificate.Subject);
				sw.WriteLine("SerialNumber = {0}", certificate.GetSerialNumberString());
				sw.WriteLine("CertHash = {0}", certificate.GetCertHashString());
				sw.WriteLine("EffectiveDate = {0}", certificate.GetEffectiveDateString());
				sw.WriteLine("ExpirationDate = {0}", certificate.GetExpirationDateString());
				sw.WriteLine("Format = {0}", certificate.GetFormat());
				sw.WriteLine("KeyAlgorithm = {0}", certificate.GetKeyAlgorithm());
				sw.WriteLine("KeyParameters = {0}", certificate.GetKeyAlgorithmParametersString());
				sw.WriteLine("PublicKey = {0}", certificate.GetPublicKeyString());
				//sw.WriteLine("RawCert = {0}", certificate.GetRawCertDataString());
			}
			else
				sw.WriteLine("No certificate available.");
		}
	}
}
