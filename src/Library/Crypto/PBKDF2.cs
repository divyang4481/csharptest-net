#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Security.Cryptography;
using System.Reflection;

namespace CSharpTest.Net.Crypto
{
	/// <summary>
	/// Provided an implementation of Rfc2898DeriveBytes accessable via the IPasswordDerivedBytes
	/// interface.  One primary difference in GetBytes() ensures that the number of bytes
	/// generated are always rounded to hash size, thus GetBytes(4) + GetBytes(4) != GetBytes(8)
	/// </summary>
	public class PBKDF2 : System.Security.Cryptography.Rfc2898DeriveBytes, IPasswordDerivedBytes
	{
		/// <summary>
		/// Constructs the Rfc2898DeriveBytes implementation.
		/// </summary>
		public PBKDF2(byte[] password, Salt salt, int iterations)
			: base(password, salt.ToArray(), iterations)
		{ }

		/// <summary>
		/// Overloaded, The base implementation is broken for length > 20, further the RFC doesnt 
		/// support lenght > 20 and stipulates that the operation should fail.
		/// </summary>
		public override byte[] GetBytes(int cb)
		{
			byte[] buffer = new byte[cb];
			for (int i = 0; i < cb; i += 20)
			{
				int step = Math.Min(20, cb - i);
				Array.Copy(base.GetBytes(20), 0, buffer, i, step);
			}
			return buffer;
		}

#if NET20 || NET35 // NOTE: .NET 4.0 finally implemented
		/// <summary>
		/// Disposes of the object
		/// </summary>
		public void Dispose()
		{
			base.Salt = new byte[8];
			base.IterationCount = 1;

			//The base doesn't clear the key'd hash, which contains the password in clear text when < 20 bytes
			FieldInfo f_hmacsha1 = GetType().BaseType.GetField("m_hmacsha1", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
			if (f_hmacsha1 != null)
			{
				HMACSHA1 m_hmacsha1 = f_hmacsha1.GetValue(this) as HMACSHA1;
				m_hmacsha1.Clear();
			}
		}
#endif
    }
}
