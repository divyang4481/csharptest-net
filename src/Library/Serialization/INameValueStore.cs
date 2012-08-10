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

namespace CSharpTest.Net.Serialization
{
	/// <summary>
	/// provides a basic inteface for a reader/writer of string name/value pairs
	/// </summary>
	public interface INameValueStore
	{
		/// <summary>
		/// returns true if the property was successfully retireved into the output
		/// variable 'value'
		/// </summary>
		/// <param name="path">Optional path for context information</param>
		/// <param name="name">The name of the property</param>
		/// <param name="value">Returns the output value if available</param>
		/// <returns>true if successful or false if data not available</returns>
		bool Read(string path, string name, out string value);

		/// <summary>
		/// Writes the given property by name
		/// </summary>
		/// <param name="path">Optional path for context information</param>
		/// <param name="name">The name of the property</param>
		/// <param name="value">The value to store</param>
		void Write(string path, string name, string value);

		/// <summary>
		/// Removes a property from the storage by name
		/// </summary>
		/// <param name="path">Optional path for context information</param>
		/// <param name="name">The name of the property to remove</param>
		void Delete(string path, string name);
	}
}
