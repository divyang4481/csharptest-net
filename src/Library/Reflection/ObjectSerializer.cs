#region Copyright 2009-2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using CSharpTest.Net.Serialization;

namespace CSharpTest.Net.Reflection
{
	/// <summary>
	/// Container for a serializer of object properties
	/// </summary>
	public class ObjectSerializer : PropertySerializer
	{
		readonly object _instance;

		/// <summary>
		/// Constructs a 'bag-o-property' serializer for the given object instance.
		/// </summary>
		/// <param name="instance">The instance whos properties are to be serialized</param>
		/// <param name="namePaths">optionally named paths to the properties to seralize</param>
		public ObjectSerializer(object instance, params string[] namePaths)
			: base(instance == null ? null : instance.GetType(), namePaths)
		{
			_instance = Check.NotNull(instance);
		}

		/// <summary>
		/// Writes all properties to the specified proeprty serialization
		/// </summary>
		public void Serialize(INameValueStore storage) { base.Serialize(_instance, storage); }

		/// <summary>
		/// Reads all properties from the specified proeprty serialization
		/// </summary>
		public void Deserialize(INameValueStore storage) { base.Deserialize(_instance, storage); }
	}
}
