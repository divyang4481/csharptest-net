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
using System.Net.Security;

#pragma warning disable 1591
namespace CSharpTest.Net.SslTunnel.Test
{
	[TestFixture]
	[Category("TestCertValidator")]
	public partial class TestCertValidator
	{
		TestCert _clientCert, _serverCert;
		readonly System.Security.Cryptography.X509Certificates.X509Chain EmptyX509Chain = new System.Security.Cryptography.X509Certificates.X509Chain();

		#region TestFixture SetUp/TearDown
		[TestFixtureSetUp]
		public virtual void Setup()
		{
			_clientCert = new TestCert("client.localhost.nunit");
			_serverCert = new TestCert("server.localhost.nunit");
		}
		#endregion

		[Test]
		public void TestAllowAnyValidCert()
		{
			SslCertValidator validator = new SslCertValidator(
			);

			Assert.IsFalse(validator.CertRequired);
			Assert.IsTrue(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.None));
			Assert.IsFalse(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateChainErrors));
		}

		[Test]
		public void TestAllowAnyTrustedCert()
		{
			ExpectedCertificate allowed = new ExpectedCertificate();
			allowed.IgnoredErrors = IgnorePolicyErrors.NameMismatch;
			SslCertValidator validator = new SslCertValidator(allowed);

			Assert.IsTrue(validator.CertRequired);
			Assert.IsTrue(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateNameMismatch));
			Assert.IsFalse(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateChainErrors));
		}

		[Test]
		public void TestValidUnexpectedCert()
		{
			ExpectedCertificate allowed = new ExpectedCertificate("Some public key", IgnorePolicyErrors.None);
			SslCertValidator validator = new SslCertValidator(allowed);

			Assert.IsTrue(validator.CertRequired);
			Assert.IsFalse(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.None));
		}

		[Test]
		public void TestValidAndExpectedCert()
		{
			ExpectedCertificate allowed = new ExpectedCertificate(_clientCert.Certificate.GetPublicKeyString(), IgnorePolicyErrors.None);
			SslCertValidator validator = new SslCertValidator(allowed);

			Assert.IsTrue(validator.CertRequired);
			Assert.IsTrue(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.None));
			Assert.IsFalse(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateChainErrors));
		}

		[Test]
		public void TestAllowByIssuer()
		{
			ExpectedCertificate allowed = new ExpectedCertificate();
			allowed.IssuedTo = _clientCert.Certificate.Subject;
			allowed.IgnoredErrors = IgnorePolicyErrors.All;
			SslCertValidator validator = new SslCertValidator(allowed);

			Assert.IsTrue(validator.CertRequired);
			Assert.IsTrue(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateChainErrors));
		}

		[Test]
		public void TestAllowByHash()
		{
			ExpectedCertificate allowed = new ExpectedCertificate();
			allowed.Hash = _clientCert.Certificate.GetCertHashString();
			allowed.IgnoredErrors = IgnorePolicyErrors.All;
			SslCertValidator validator = new SslCertValidator(allowed);

			Assert.IsTrue(validator.CertRequired);
			Assert.IsTrue(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateChainErrors));
		}

		[Test]
		public void TestAllowByPublicKey()
		{
			ExpectedCertificate allowed = new ExpectedCertificate();
			allowed.PublicKey = _clientCert.Certificate.GetPublicKeyString();
			allowed.IgnoredErrors = IgnorePolicyErrors.All;
			SslCertValidator validator = new SslCertValidator(allowed);

			Assert.IsTrue(validator.CertRequired);
			Assert.IsTrue(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateChainErrors));
		}

		[Test]
		public void TestDenyByIssuer()
		{
			ExpectedCertificate allowed = new ExpectedCertificate();
			allowed.IssuedTo = _serverCert.Certificate.Subject;
			allowed.IgnoredErrors = IgnorePolicyErrors.All;
			SslCertValidator validator = new SslCertValidator(allowed);

			Assert.IsTrue(validator.CertRequired);
			Assert.IsFalse(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateChainErrors));
		}

		[Test]
		public void TestDenyByHash()
		{
			ExpectedCertificate allowed = new ExpectedCertificate();
			allowed.Hash = _serverCert.Certificate.GetCertHashString();
			allowed.IgnoredErrors = IgnorePolicyErrors.All;
			SslCertValidator validator = new SslCertValidator(allowed);

			Assert.IsTrue(validator.CertRequired);
			Assert.IsFalse(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateChainErrors));
		}

		[Test]
		public void TestDenyByPublicKey()
		{
			ExpectedCertificate allowed = new ExpectedCertificate();
			allowed.PublicKey = _serverCert.Certificate.GetPublicKeyString();
			allowed.IgnoredErrors = IgnorePolicyErrors.All;
			SslCertValidator validator = new SslCertValidator(allowed);

			Assert.IsTrue(validator.CertRequired);
			Assert.IsFalse(validator.IsValid(null, _clientCert.Certificate, EmptyX509Chain, SslPolicyErrors.RemoteCertificateChainErrors));
		}
	}
}
