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
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using CSharpTest.Net.Crypto;

#pragma warning disable 1591

namespace CSharpTest.Net.Library.Test
{
	[TestFixture]
	public partial class TestEncryption
	{
		[Test]
		public void TestEncryptDecryptString()
		{
			string svalue = "some text value";

			string esbyuser = Encryption.CurrentUser.Encrypt(svalue);
			string esbyhost = Encryption.LocalMachine.Encrypt(svalue);

			Assert.AreNotEqual(svalue, esbyuser);
			Assert.AreNotEqual(svalue, esbyhost);
			Assert.AreNotEqual(esbyhost, esbyuser);

			Assert.AreEqual(svalue, Encryption.CurrentUser.Decrypt(esbyuser));
			Assert.AreEqual(svalue, Encryption.LocalMachine.Decrypt(esbyhost));
		}

		[Test]
		public void TestEncryptDecryptBytes()
		{
			byte[] svalue = Encoding.ASCII.GetBytes("some text value");

			byte[] esbyuser = Encryption.CurrentUser.Encrypt(svalue);
			byte[] esbyhost = Encryption.LocalMachine.Encrypt(svalue);

			Assert.AreNotEqual(Encoding.ASCII.GetString(svalue), Encoding.ASCII.GetString(esbyuser));
			Assert.AreNotEqual(Encoding.ASCII.GetString(svalue), Encoding.ASCII.GetString(esbyhost));
			Assert.AreNotEqual(Encoding.ASCII.GetString(esbyhost), Encoding.ASCII.GetString(esbyuser));

			Assert.AreEqual(Encoding.ASCII.GetString(svalue), Encoding.ASCII.GetString(Encryption.CurrentUser.Decrypt(esbyuser)));
			Assert.AreEqual(Encoding.ASCII.GetString(svalue), Encoding.ASCII.GetString(Encryption.LocalMachine.Decrypt(esbyhost)));
		}
	}
}
