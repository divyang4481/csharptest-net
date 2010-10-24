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

namespace CSharpTest.Net.Crypto
{
	/// <summary>
	/// Provides an interface for abstracting the password derivation routine used
	/// for password key derivation
	/// </summary>
	public interface IPasswordDerivedBytes : IDisposable
	{
		///<summary>
		///     Gets or sets the number of iterations for the operation.
		///</summary>
		int IterationCount { get; set; }
		///
		///<summary>
		///     Gets or sets the key salt value for the operation.
		///</summary>
		byte[] Salt { get; set; }

		///<summary>
		///     Returns a pseudo-random key from a password, salt and iteration count.
		///</summary>
		byte[] GetBytes(int cb);
		///<summary>
		///     Resets the state of the operation.
		///</summary>
		void Reset();
	}
}
