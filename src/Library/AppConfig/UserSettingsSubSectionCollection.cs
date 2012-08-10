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
using System.Configuration;

namespace CSharpTest.Net.AppConfig
{
	/// <summary>
	/// Provides a collection of name keyed sections that contain more key/value settings
	/// </summary>
	public sealed class UserSettingsSubSectionCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// Constructs a collection of named sections
		/// </summary>
		public UserSettingsSubSectionCollection()
			: base(StringComparer.Ordinal)
		{
			base.AddElementName = "section";
			base.ClearElementName = "clear";
			base.RemoveElementName = "remove";
		}

		/// <summary>
		/// Gets the type of the System.Configuration.ConfigurationElementCollection.
		/// </summary>
		public override ConfigurationElementCollectionType CollectionType
		{ get { return ConfigurationElementCollectionType.AddRemoveClearMap; } }

		#region Protected Overrides
		/// <summary> creates a new UserSettingsSubSection </summary>
		protected override ConfigurationElement CreateNewElement()
		{
			return new UserSettingsSubSection();
		}

		/// <summary> creates a new UserSettingsSubSection </summary>
		protected override ConfigurationElement CreateNewElement(string elementName)
		{
			UserSettingsSubSection secton = new UserSettingsSubSection();
			secton.Name = elementName;
			return secton;
		}

		/// <summary> Returns the name of the UserSettingsSubSection </summary>
		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((UserSettingsSubSection)element).Name;
		}

		#endregion

		/// <summary>
		/// Adds a new section with the specified name
		/// </summary>
		public UserSettingsSubSection Add(string name)
		{
			UserSettingsSubSection section = (UserSettingsSubSection)CreateNewElement(name);
			BaseAdd(section);
			return section;
		}

		/// <summary>
		/// Removes the specified collection by name
		/// </summary>
		public void Remove(string name)
		{
			BaseRemove(name);
		}

		/// <summary>
		/// Clears all elements from the collection
		/// </summary>
		public void Clear()
		{
			BaseClear();
		}

		/// <summary>
		/// Returns the specified collection by name if it exists, or null if not found
		/// </summary>
		public new UserSettingsSubSection this[string name]
		{ get { return (UserSettingsSubSection)BaseGet(name); } }

		/// <summary>
		/// Deep copy of all settings from one configuration to another.
		/// </summary>
		public void CopyFrom(UserSettingsSubSectionCollection other)
		{
			foreach (UserSettingsSubSection from in other)
			{
				UserSettingsSubSection to = this[from.Name];
				if (to == null)
					to = this.Add(from.Name);
				to.CopyFrom(from);
			}
		}
	}

}
