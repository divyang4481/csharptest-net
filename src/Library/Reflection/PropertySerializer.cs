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
using CSharpTest.Net.Serialization;
using System.Diagnostics;

namespace CSharpTest.Net.Reflection
{
	/// <summary>
	/// Represents a set of properties that can be iterated, read, or written to an IPropertyStorage
	/// instance.
	/// </summary>
	public class PropertySerializer<T> : PropertySerializer
	{
		/// <summary>
		/// Creates a property serializer for the specified type T and optionally the properties specified.
		/// </summary>
		public PropertySerializer(params string[] namePaths)
			: base(typeof(T), namePaths)
		{ }

		/// <summary>
		/// Writes all properties to the specified proeprty serialization
		/// </summary>
		public void Serialize(T instance, INameValueStore rawstorage) { base.Serialize(instance, rawstorage); }
		/// <summary>
		/// Reads all properties from the specified proeprty serialization
		/// </summary>
		public void Deserialize(T instance, INameValueStore rawstorage) { base.Deserialize(instance, rawstorage); }

#pragma warning disable 1691 //1691 = not a valid warning number, 809 below is defined in the 3.x tools:
#pragma warning disable 809 //809 = Obsolete member overrides non-obsolete member

		/// <summary> Hides the base class method </summary>
		[Obsolete("This method should not be called, use the typed method.", true)]
		[System.ComponentModel.Browsable(false)]
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public new void Serialize(object i, INameValueStore s) { throw new NotSupportedException(); }
		/// <summary> Hides the base class method </summary>
		[Obsolete("This method should not be called, use the typed method.", true)]
		[System.ComponentModel.Browsable(false)]
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public new void Deserialize(object i, INameValueStore s) { throw new NotSupportedException(); }
	}

	/// <summary>
	/// Represents a set of properties that can be iterated, read, or written to an IPropertyStorage
	/// instance.
	/// </summary>
	public class PropertySerializer
	{
		bool _continueOnError;
		readonly Type _type;
		readonly List<string> _members;

		/// <summary>
		/// Creates a property serializer for the specified type and optionally the properties specified.
		/// </summary>
		public PropertySerializer(Type typeOfInstance, params string[] namePaths)
		{
			_type = Check.NotNull(typeOfInstance);
			_members = new List<string>();
			_continueOnError = false;

			foreach (string name in Check.NotNull(namePaths))
				AddMember(name);
		}

		/// <summary>
		/// Gets or sets a value that controls whether exceptions are swallowed and logged durring
		/// serialization or deserialization routines.
		/// </summary>
		public bool ContinueOnError
		{
			get { return _continueOnError; }
			set { _continueOnError = value; }
		}

		/// <summary>
		/// Adds a single item to the named property collection, again can be a nested property by
		/// using a path or dotted notation "ClientRectangle.X".
		/// </summary>
		public void AddMember(string sdatapath)
		{
			//validate:
			PropertyType prop = PropertyType.TraverseProperties(_type, Check.NotEmpty(sdatapath));
			//add:
			_members.Add(sdatapath);
		}

		/// <summary>
		/// Writes all properties to the specified proeprty serialization
		/// </summary>
		public virtual void Serialize(object instance, INameValueStore rawstorage)
		{
			Check.NotNull(instance);
			Check.NotNull(rawstorage);

			//Log.Verbose("Saving {0} properties for {1} in {2}", _members.Count, instance.GetType(), rawstorage.GetType());

			Storage storage = new Storage(rawstorage, _type.FullName);
			foreach (string sdatapath in _members)
			{
				try
				{
					PropertyValue prop = PropertyValue.TraverseProperties(instance, sdatapath);
					storage.SetValue(sdatapath, prop.Type, prop.Value);
				}
				catch (Exception e)
				{
					Trace.TraceError("Unable to serialize property {0} on {1}\r\n{2}", sdatapath, instance.GetType(), e);
					if (false == _continueOnError)
						throw;
				}
			}
		}

		/// <summary>
		/// Reads all properties from the specified proeprty serialization
		/// </summary>
		public virtual void Deserialize(object instance, INameValueStore rawstorage)
		{
			Check.NotNull(instance);
			Check.NotNull(rawstorage);

			//Log.Verbose("Reading {0} properties for {1} in {2}", _members.Count, instance.GetType(), rawstorage.GetType());

			Storage storage = new Storage(rawstorage, _type.FullName);
			foreach (string sdatapath in _members)
			{
				try
				{
					PropertyValue prop = PropertyValue.TraverseProperties(instance, sdatapath);
					object oval = null;
					if (storage.TryGetValue(sdatapath, prop.Type, out oval))
						prop.Value = oval;
				}
				catch (Exception e)
				{
					Trace.TraceError("Unable to deserialize property {0} on {1}\r\n{2}", sdatapath, instance.GetType(), e);
					if (false == _continueOnError)
						throw;
				}
			}
		}
	}
}
