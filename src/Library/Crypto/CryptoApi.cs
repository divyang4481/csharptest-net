#region Copyright 2008-2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Text;
using System.Security.Cryptography;

namespace CSharpTest.Net.Crypto
{
	/// <summary>
	/// provies some simplified access to encryption routines
	/// </summary>
	public static class Encryption
	{
		/// <summary>Encrypts data for the current user</summary>
		public static readonly IEncryptDecrypt CurrentUser = new DataProtector(DataProtectionScope.CurrentUser);
		/// <summary>Encrypts data for the this machine</summary>
		public static readonly IEncryptDecrypt LocalMachine = new DataProtector(DataProtectionScope.LocalMachine);

		#region private class DataProtector
		private class DataProtector : IEncryptDecrypt
		{
			private readonly DataProtectionScope _scope;

			internal DataProtector(DataProtectionScope scope)
			{ _scope = scope; }

			public byte[] Encrypt(byte[] blob)
			{
				return ProtectedData.Protect(blob, null, _scope);
			}

			public string Encrypt(string text)
			{
				return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(text)));
			}

			public byte[] Decrypt(byte[] blob)
			{
				return ProtectedData.Unprotect(blob, null, _scope);
			}

			public string Decrypt(string text)
			{
				return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(text)));
			}
		}
		#endregion
	}
}
