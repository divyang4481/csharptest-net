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
using System.Text;
using System.Configuration;
using System.Diagnostics;

namespace CSharpTest.Net.AppConfig
{
	/// <summary>
	/// Provides a store for user settings in the same format as appSettings
	/// </summary>
	public sealed partial class UserSettingsSection : ConfigurationSection
	{
		private static readonly ConfigurationProperty __Settings = new ConfigurationProperty(String.Empty, typeof(KeyValueConfigurationCollection), new KeyValueConfigurationCollection(), ConfigurationPropertyOptions.IsDefaultCollection);
		private static readonly ConfigurationProperty __Sections = new ConfigurationProperty("sections", typeof(UserSettingsSubSectionCollection), new UserSettingsSubSectionCollection(), ConfigurationPropertyOptions.None);

		/// <summary>
		/// The name of the user section: userSettings
		/// </summary>
		public const string SECTION_NAME = "userSettings";

		/// <summary>
		/// if available, returns the default userSettings from the app's configuration file
		/// </summary>
		public static UserSettingsSection DefaultSettings
		{
			get { return ConfigurationManager.GetSection(SECTION_NAME) as UserSettingsSection; }
		}

		/// <summary>
		/// Retrieves the current UserSettingsSection from the default configuration
		/// </summary>
		public static UserSettingsSection UserSettings
		{ 
			get
			{
				Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
				return UserSettingsFrom(cfg);
			} 
		}

		/// <summary>
		/// Retrieves the current UserSettingsSection from the specified configuration, if none
		/// exists a new one is created.  If a previous version of the userSettings exists they
		/// will be copied to the UserSettingsSection.
		/// </summary>
		public static UserSettingsSection UserSettingsFrom(Configuration config)
		{
			UserSettingsSection settings = null;

			try { settings = (UserSettingsSection)config.Sections[SECTION_NAME]; }
			catch(InvalidCastException)
			{ config.Sections.Remove(SECTION_NAME); }

			if (settings == null)
			{
				settings = new UserSettingsSection();
				settings.SectionInformation.AllowExeDefinition = ConfigurationAllowExeDefinition.MachineToLocalUser;
				settings.SectionInformation.RestartOnExternalChanges = false;
				settings.SectionInformation.Type = String.Format("{0}, {1}", typeof(UserSettingsSection).FullName, typeof(UserSettingsSection).Assembly.GetName().Name);

				UpgradeUserSettings(config, settings);

				config.Sections.Add(SECTION_NAME, settings);
			}
			else if (!config.HasFile)
				UpgradeUserSettings(config, settings);

			if (settings.IsModified())
			{
				try { config.Save(); }
				catch (Exception e) { Trace.TraceError("{1}\r\n{0}", e, "Failed to save configuration."); }
			}

			return settings;
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
		/// Retrieves a collection of named sections within the userSettings container
		/// </summary>
		[ConfigurationProperty("sections")]
		public UserSettingsSubSectionCollection Sections
		{
			get { return (UserSettingsSubSectionCollection)base[__Sections]; }
		}

		/// <summary>
		/// Overloaded to ignore namespace declaration and useage so that we can identify the XSD
		/// file that should be used.
		/// </summary>
		protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
		{
			if (name.IndexOf(':') > 0 || name.StartsWith("xmlns:"))
				return true;
			return base.OnDeserializeUnrecognizedAttribute(name, value);
		}

		/// <summary>
		/// Overloaded to insert namespace declaration of xml schema and include our own schema file
		/// aut0magically.  This is a development aid that is not enforced durring read of the xml.
		/// </summary>
		protected override void PreSerialize(System.Xml.XmlWriter writer)
		{
			if (writer != null)
			{
				// Write the namespace declaration.
				writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
				writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", null, "http://csharptest.net/downloads/schema/userSettings.xsd");
			}
			base.PreSerialize(writer);
		}

		/// <summary>
		/// Gets or sets a key/value pair in the collection of settings
		/// </summary>
		public new string this[string name]
		{
			get { KeyValueConfigurationElement kv = Settings[name]; return kv == null ? null : kv.Value; }
			set
			{
				KeyValueConfigurationElement kv = Settings[name];
				if (kv == null) Settings.Add(kv = new KeyValueConfigurationElement(name, value));
				else kv.Value = value;
			}
		}

		/// <summary>
		/// Deep copy of all settings from one configuration to another.
		/// </summary>
		public void CopyFrom(UserSettingsSection otherSection)
		{
			foreach (KeyValueConfigurationElement from in otherSection.Settings)
			{
				KeyValueConfigurationElement to = this.Settings[from.Key];
				if (to == null)
					this.Settings.Add(from.Key, from.Value);
				else
					to.Value = from.Value;
			}

			this.Sections.CopyFrom(otherSection.Sections);
		}
	}
}
