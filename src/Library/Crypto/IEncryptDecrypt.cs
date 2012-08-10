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
using CSharpTest.Net.Formatting;

namespace CSharpTest.Net.Crypto
{
	/// <summary>
	/// A simple interface for encrypting and decrypting strings, obtain an instance through the
	/// static Encryption inteface calss.
	/// </summary>
	public interface IEncryptDecrypt : IDisposable
    {
        /// <summary>Encrypts a stream of data</summary>
        Stream Encrypt(Stream stream);
        /// <summary> Decrypts a stream of data </summary>
        Stream Decrypt(Stream stream);

		/// <summary>Encrypts a raw data block as a set of bytes</summary>
		byte[] Encrypt(byte[] blob);
		/// <summary>Encrypts a string and encodes the result in base-64 encoded text</summary>
		string Encrypt(string text);
		/// <summary>Encrypts a string and encodes the result in specified encoding format</summary>
        string Encrypt(string text, ByteEncoding encoding);

		/// <summary>Decrypts a raw data block as a set of bytes</summary>
		byte[] Decrypt(byte[] blob);
		/// <summary>Decrypts a string from base-64 encoded text</summary>
		string Decrypt(string text);
		/// <summary>Decrypts a string from the specified encoding format</summary>
        string Decrypt(string text, ByteEncoding encoding);
	}
}
