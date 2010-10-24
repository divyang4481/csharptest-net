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
using System.Configuration;

namespace CSharpTest.Net.AppConfig
{
	/// <summary>
	/// Provides a store for user settings in the same format as appSettings
	/// </summary>
	public sealed class UserSettingsSubSection : ConfigurationElement, IComparable<UserSettingsSubSection>
	{
		private static readonly ConfigurationProperty __Settings = new ConfigurationProperty(String.Empty, typeof(KeyValueConfigurationCollection), new KeyValueConfigurationCollection(), ConfigurationPropertyOptions.IsDefaultCollection);

		/// <summary>
		/// Returns the key name of the section within the userSettings collection
		/// </summary>
		[ConfigurationProperty("name", IsRequired = true, IsKey = true)]
		public string Name
		{
			get { return (string)base["name"]; }
			set { base["name"] = value; }
		}

		/// <summary>
		/// Retrieves the collection of key/value settings
		/// </summary>
		[ConfigurationProperty("", IsDefaultCollection = true)]
		public KeyValueConfigurationCollection Settings
		{
			get { return (KeyValueConfigurationCollection)base[__Settings]; }
		}

		/// <summary>
		/// Gets or sets a key/value pair in the collection of settings
		/// </summary>
		public new string this[string name]
		{
			get { KeyValueConfigurationElement kv = Settings[name]; return kv == null ? null : kv.Value; }
			set { 
				KeyValueConfigurationElement kv = Settings[name];
				if (kv == null) Settings.Add(kv = new KeyValueConfigurationElement(name, value));
				else kv.Value = value;
			}
		}

		/// <summary>
		/// Provides key comparison between two sections
		/// </summary>
		public int CompareTo(UserSettingsSubSection other)
		{
			return StringComparer.Ordinal.Compare(this.Name, other.Name);
		}

		/// <summary>
		/// Deep copy of all settings from one configuration to another.
		/// </summary>
		public void CopyFrom(UserSettingsSubSection other)
		{
			foreach (KeyValueConfigurationElement from in other.Settings)
			{
				KeyValueConfigurationElement to = this.Settings[from.Key];
				if (to == null)
					this.Settings.Add(from.Key, from.Value);
				else
					to.Value = from.Value;
			}
		}
	}

}
