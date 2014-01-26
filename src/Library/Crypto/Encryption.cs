#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using CSharpTest.Net.Formatting;
using CSharpTest.Net.IO;

namespace CSharpTest.Net.Crypto
{
    /// <summary>
    /// Maintains backwards compatibility for access to the encryption api
    /// </summary>
    public static class Encryption
    {
		/// <summary>Encrypts data for the current user</summary>
		public static readonly IEncryptDecrypt CurrentUser = LocalHostKey.CurrentUser;
		/// <summary>Encrypts data for the this machine</summary>
		public static readonly IEncryptDecrypt LocalMachine = LocalHostKey.LocalMachine;

		/// <summary>Implements the encryption api but does not change any data</summary>
		public static readonly IEncryptDecrypt Passthrough = new PassthroughTransform();

        private class PassthroughTransform : IEncryptDecrypt
        {
            public void Dispose()
            { }

            public Stream Encrypt(Stream stream)
            { return stream; }

            public Stream Decrypt(Stream stream)
            { return stream; }

            public byte[] Encrypt(byte[] blob)
            { return blob; }

            public string Encrypt(string text)
            { return text; }

            public string Encrypt(string text, ByteEncoding encoding)
            { return text; }

            public byte[] Decrypt(byte[] blob)
            { return blob; }

            public string Decrypt(string text)
            { return text; }

            public string Decrypt(string text, ByteEncoding encoding)
            { return text; }
        }
    }
}
